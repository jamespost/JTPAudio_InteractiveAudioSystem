using System.Collections;
using System.Collections.Generic;
using System.Linq; // Required for functions like OrderBy() and FirstOrDefault()
using UnityEngine;

/// <summary>
/// The central hub for all audio operations. This version includes a priority system
/// that allows more important sounds to steal voices from less important ones, ensuring
/// critical gameplay cues are always heard.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static AudioManager Instance { get; private set; }

    // --- Inspector References ---
    [Header("Data Assets")]
    [Tooltip("A list of all GameSwitch assets to initialize the switch database.")]
    [SerializeField] private List<GameSwitch> gameSwitches = new List<GameSwitch>();

    [Header("AudioSource Pooling")]
    [Tooltip("The initial number of AudioSources to create in the pool. This is the max concurrent sounds before voice stealing occurs.")]
    [SerializeField] private int initialPoolSize = 16;
    [Tooltip("If a sound has the highest priority but all sources are busy with sounds of the same priority, can we create a new temporary source?")]
    [SerializeField] private bool canPoolGrow = true;

    // --- Private Fields ---
    private Dictionary<string, AudioEvent> eventDictionary;
    private Dictionary<string, string> switchDatabase;

    // We now track all sources, not just the available ones, to manage priorities.
    private List<ActiveSound> sourcePool;
    private GameObject poolParent;

    #region --- Unity Lifecycle Methods ---

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        InitializeEventDatabase();
        InitializeSwitchDatabase();
        InitializeAudioSourcePool();
    }

    #endregion

    #region --- Initialization ---

    private void InitializeEventDatabase()
    {
        eventDictionary = new Dictionary<string, AudioEvent>();
        AudioEvent[] allEvents = Resources.LoadAll<AudioEvent>("");
        foreach (AudioEvent audioEvent in allEvents)
        {
            if (eventDictionary.ContainsKey(audioEvent.eventID))
            {
                Debug.LogWarning($"AudioManager: Duplicate event ID '{audioEvent.eventID}' found. Overwriting.");
            }
            eventDictionary[audioEvent.eventID] = audioEvent;
        }
    }

    private void InitializeSwitchDatabase()
    {
        switchDatabase = new Dictionary<string, string>();
        foreach (GameSwitch sw in gameSwitches)
        {
            if (switchDatabase.ContainsKey(sw.switchID))
            {
                Debug.LogWarning($"AudioManager: Duplicate switch ID '{sw.switchID}' found. Overwriting.");
            }
            switchDatabase[sw.switchID] = sw.defaultValue;
        }
    }

    private void InitializeAudioSourcePool()
    {
        sourcePool = new List<ActiveSound>(initialPoolSize);
        poolParent = new GameObject("AudioSourcePool");
        poolParent.transform.SetParent(this.transform);

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePoolObject();
        }
    }

    #endregion

    #region --- Public API ---

    public void PostEvent(string eventName, GameObject sourceObject)
    {
        if (!eventDictionary.TryGetValue(eventName, out AudioEvent audioEvent))
        {
            Debug.LogWarning($"AudioManager: Could not find event with ID '{eventName}'. Is it in a Resources folder?");
            return;
        }

        // Get an available source based on the event's priority.
        ActiveSound sound = GetSourceFromPool(audioEvent.priority);
        if (sound == null)
        {
            // This now means the new sound wasn't important enough to steal a voice.
            return;
        }

        // Configure the source
        sound.priority = audioEvent.priority;
        sound.source.outputAudioMixerGroup = audioEvent.mixerGroup;

        if (audioEvent.attachToSource)
        {
            sound.transform.SetParent(sourceObject.transform);
            sound.transform.localPosition = Vector3.zero;
        }
        else
        {
            sound.transform.position = sourceObject.transform.position;
        }

        // Play the sound via its container
        audioEvent.container.Play(sound.source);

        if (!sound.source.loop)
        {
            StartCoroutine(ReturnSourceToPoolAfterPlay(sound));
        }
    }

    public void SetSwitch(string switchId, string value)
    {
        if (switchDatabase.ContainsKey(switchId))
            switchDatabase[switchId] = value;
    }

    public string GetSwitchValue(string switchId)
    {
        if (switchDatabase.TryGetValue(switchId, out string value))
            return value;
        return string.Empty;
    }

    #endregion

    #region --- Pooling & Priority Logic ---

    /// <summary>
    /// Finds an available AudioSource or steals one from a lower-priority sound.
    /// </summary>
    private ActiveSound GetSourceFromPool(AudioEvent.EventPriority priority)
    {
        // 1. Try to find a genuinely inactive source first. This is the cheapest option.
        var inactiveSource = sourcePool.FirstOrDefault(s => !s.source.isPlaying);
        if (inactiveSource != null)
        {
            return inactiveSource;
        }

        // 2. If the pool is full, try to find a source to steal.
        // We order all active sources by their priority (ascending) and then by time played (so we steal the oldest sound of the lowest priority).
        var lowestPrioritySound = sourcePool
            .Where(s => s.source.isPlaying) // Only consider active sources
            .OrderBy(s => s.priority)     // Find the one with the lowest priority value
            .ThenBy(s => s.source.time)   // If priorities are equal, pick the one that has been playing longest
            .FirstOrDefault();            // Get the first one in the sorted list

        if (lowestPrioritySound != null && priority > lowestPrioritySound.priority)
        {
            // If our new sound is more important, steal the source from the low-priority sound.
            Debug.Log($"VOICE STEALING: New sound with priority '{priority}' is stealing voice from sound with priority '{lowestPrioritySound.priority}'.");
            lowestPrioritySound.source.Stop(); // Stop the old sound
            // Un-parent it immediately to prevent it from moving with an object it no longer belongs to.
            lowestPrioritySound.transform.SetParent(poolParent.transform, worldPositionStays: false);
            return lowestPrioritySound;        // Return its source for the new sound to use
        }

        // 3. If we can't find a voice to steal and the pool can grow, create a new one.
        if (canPoolGrow)
        {
            Debug.LogWarning("Pool is full and no voice was available to steal. Growing pool size. Consider increasing initial pool size if this happens often.");
            return CreatePoolObject();
        }

        // 4. If all else fails, we can't play the sound.
        Debug.LogWarning($"Could not play sound with priority '{priority}'. Pool is full and no lower-priority sounds were available to steal.");
        return null;
    }

    /// <summary>
    /// This is now just a cleanup coroutine. The source is never truly "removed" from the pool list.
    /// It simply gets its properties reset.
    /// </summary>
    private IEnumerator ReturnSourceToPoolAfterPlay(ActiveSound sound)
    {
        yield return new WaitUntil(() => sound == null || !sound.source.isPlaying || !sound.gameObject.activeInHierarchy);

        if (sound != null && sound.gameObject.activeInHierarchy)
        {
            // Un-parent the sound and reset its properties for the next use.
            sound.transform.SetParent(poolParent.transform, worldPositionStays: false);
            sound.source.loop = false;
        }
    }

    /// <summary>
    /// Creates a new GameObject with all necessary components and adds it to the pool list.
    /// </summary>
    private ActiveSound CreatePoolObject()
    {
        GameObject newSourceGO = new GameObject($"Pooled_AudioSource_{sourcePool.Count}");
        newSourceGO.transform.SetParent(poolParent.transform);

        AudioSource newSource = newSourceGO.AddComponent<AudioSource>();
        newSource.spatialBlend = 1.0f;
        newSource.playOnAwake = false;

        // Add our helper component to track its state.
        ActiveSound activeSound = newSourceGO.AddComponent<ActiveSound>();

        sourcePool.Add(activeSound);
        return activeSound;
    }

    #endregion
}

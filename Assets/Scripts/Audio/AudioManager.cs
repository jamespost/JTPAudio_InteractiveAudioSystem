using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The central hub for all audio operations in the game. It manages a pool of AudioSources
/// and uses ScriptableObject-based events to play sounds.
/// This version automatically finds and registers all AudioEvent assets from any 'Resources' folder
/// and correctly handles attaching sounds to moving GameObjects.
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
    [Tooltip("The initial number of AudioSources to create in the pool.")]
    [SerializeField] private int initialPoolSize = 10;
    [Tooltip("Whether the pool can create new AudioSources if all are currently busy.")]
    [SerializeField] private bool canPoolGrow = true;

    // --- Private Fields ---
    private Dictionary<string, AudioEvent> eventDictionary;
    private Dictionary<string, string> switchDatabase;
    private Queue<AudioSource> sourcePool;
    private GameObject poolParent;

    #region --- Unity Lifecycle Methods ---

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Found more than one AudioManager in the scene. Destroying the newest one.");
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
                Debug.LogWarning($"AudioManager: Duplicate event ID '{audioEvent.eventID}' found. Overwriting previous entry.");
            }
            eventDictionary[audioEvent.eventID] = audioEvent;
        }
        Debug.Log($"AudioManager: Automatically registered {eventDictionary.Count} audio events from Resources.");
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
        Debug.Log($"AudioManager: Initialized with {switchDatabase.Count} game switches.");
    }

    private void InitializeAudioSourcePool()
    {
        sourcePool = new Queue<AudioSource>();
        poolParent = new GameObject("AudioSourcePool");
        poolParent.transform.SetParent(this.transform);

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateAndPoolSource();
        }
        Debug.Log($"AudioManager: AudioSource pool created with {sourcePool.Count} sources.");
    }

    #endregion

    #region --- Public API ---

    public void PostEvent(string eventName, GameObject sourceObject)
    {
        if (!eventDictionary.TryGetValue(eventName, out AudioEvent audioEvent))
        {
            Debug.LogWarning($"AudioManager: Could not find event with ID '{eventName}'. Is the asset in a Resources folder?");
            return;
        }

        AudioSource source = GetSourceFromPool();
        if (source == null)
        {
            Debug.LogWarning($"AudioManager: No available AudioSources to play event '{eventName}'.");
            return;
        }

        source.outputAudioMixerGroup = audioEvent.mixerGroup;

        // --- NEW LOGIC: Attach to source or play at position ---
        if (audioEvent.attachToSource)
        {
            // Parent the source and reset its local position to ensure it's centered on the parent.
            source.transform.SetParent(sourceObject.transform);
            source.transform.localPosition = Vector3.zero;
        }
        else
        {
            // Place the source at the object's position, but don't parent it.
            source.transform.position = sourceObject.transform.position;
        }
        // --- End Modification ---

        audioEvent.container.Play(source);

        // A simple check to prevent auto-pooling looping sounds.
        // A more robust system for looping sounds would be needed for full production.
        if (!source.loop)
        {
            StartCoroutine(ReturnSourceToPoolAfterPlay(source));
        }
    }

    public void SetSwitch(string switchId, string value)
    {
        if (switchDatabase.ContainsKey(switchId))
        {
            switchDatabase[switchId] = value;
        }
        else
        {
            Debug.LogWarning($"AudioManager: Tried to set unknown switch '{switchId}'.");
        }
    }

    public string GetSwitchValue(string switchId)
    {
        if (switchDatabase.TryGetValue(switchId, out string value))
        {
            return value;
        }

        Debug.LogWarning($"AudioManager: Tried to get unknown switch '{switchId}'. Returning empty string.");
        return string.Empty;
    }

    #endregion

    #region --- Pooling Logic ---

    private AudioSource GetSourceFromPool()
    {
        if (sourcePool.Count > 0)
        {
            AudioSource source = sourcePool.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }

        if (canPoolGrow)
        {
            Debug.Log("AudioManager: Pool is empty, creating a new AudioSource.");
            return CreateAndPoolSource(pool: false);
        }

        return null;
    }

    private void ReturnSourceToPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.loop = false; // Reset loop state

        // --- CRITICAL CHANGE: Un-parent the source before returning to pool ---
        source.transform.SetParent(poolParent.transform, worldPositionStays: false);

        source.gameObject.SetActive(false);
        sourcePool.Enqueue(source);
    }

    private AudioSource CreateAndPoolSource(bool pool = true)
    {
        GameObject newSourceGO = new GameObject($"Pooled_AudioSource_{sourcePool.Count}");
        newSourceGO.transform.SetParent(poolParent.transform);
        AudioSource newSource = newSourceGO.AddComponent<AudioSource>();

        newSource.spatialBlend = 1.0f;
        newSource.playOnAwake = false;

        if (pool)
        {
            newSourceGO.SetActive(false);
            sourcePool.Enqueue(newSource);
        }

        return newSource;
    }

    private IEnumerator ReturnSourceToPoolAfterPlay(AudioSource source)
    {
        // Wait until the source is no longer playing OR the source has been destroyed/deactivated elsewhere.
        yield return new WaitUntil(() => source == null || !source.isPlaying || !source.gameObject.activeInHierarchy);

        // Final check to ensure the source still exists and is part of our scene before trying to pool it.
        if (source != null && source.gameObject.activeInHierarchy)
        {
            ReturnSourceToPool(source);
        }
    }

    #endregion
}

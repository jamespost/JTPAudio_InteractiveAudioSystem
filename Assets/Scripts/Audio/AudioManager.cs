using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The central hub for all audio operations in the game. It manages a pool of AudioSources
/// and uses ScriptableObject-based events to play sounds.
/// This class uses the Singleton pattern to ensure only one instance exists.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static AudioManager Instance { get; private set; }

    // --- Inspector References ---
    [Header("Data Assets")]
    [Tooltip("A list of all AudioEvent ScriptableObjects to be loaded into the system.")]
    [SerializeField] private List<AudioEvent> initialEvents = new List<AudioEvent>();
    [Tooltip("A list of all GameSwitch assets to initialize the switch database.")]
    [SerializeField] private List<GameSwitch> gameSwitches = new List<GameSwitch>();

    [Header("AudioSource Pooling")]
    [Tooltip("The initial number of AudioSources to create in the pool.")]
    [SerializeField] private int initialPoolSize = 10;
    [Tooltip("Whether the pool can create new AudioSources if all are currently busy.")]
    [SerializeField] private bool canPoolGrow = true;

    // --- Private Fields ---
    private Dictionary<string, AudioEvent> eventDictionary;
    private Dictionary<string, string> switchDatabase; // Holds the current value for each switch ID
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
        InitializeSwitchDatabase(); // New for Phase 2
        InitializeAudioSourcePool();
    }

    #endregion

    #region --- Initialization ---

    private void InitializeEventDatabase()
    {
        eventDictionary = new Dictionary<string, AudioEvent>();
        foreach (AudioEvent audioEvent in initialEvents)
        {
            if (eventDictionary.ContainsKey(audioEvent.eventID))
            {
                Debug.LogWarning($"AudioManager: Duplicate event ID '{audioEvent.eventID}' found. Overwriting.");
            }
            eventDictionary[audioEvent.eventID] = audioEvent;
        }
        Debug.Log($"AudioManager: Initialized with {eventDictionary.Count} audio events.");
    }

    /// <summary>
    /// Initializes the switch database with the default values from the GameSwitch assets.
    /// </summary>
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
            Debug.LogWarning($"AudioManager: Could not find event with ID '{eventName}'.");
            return;
        }

        AudioSource source = GetSourceFromPool();
        if (source == null)
        {
            Debug.LogWarning($"AudioManager: No available AudioSources to play event '{eventName}'.");
            return;
        }

        source.transform.position = sourceObject.transform.position;
        source.outputAudioMixerGroup = audioEvent.mixerGroup;

        audioEvent.container.Play(source);

        StartCoroutine(ReturnSourceToPoolAfterPlay(source));
    }

    /// <summary>
    /// Sets the current value for a specified Game Switch.
    /// </summary>
    /// <param name="switchId">The ID of the switch to change.</param>
    /// <param name="value">The new value to set (e.g., "Grass").</param>
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

    /// <summary>
    /// Gets the current value of a specified Game Switch. Used by SwitchContainers.
    /// </summary>
    /// <param name="switchId">The ID of the switch to query.</param>
    /// <returns>The current value of the switch.</returns>
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
        // For simple containers, this works fine. Sequence 'PlayAll' would need a different approach.
        yield return new WaitUntil(() => !source.isPlaying);

        ReturnSourceToPool(source);
    }

    #endregion
}

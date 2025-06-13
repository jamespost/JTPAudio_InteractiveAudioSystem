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
    [Header("Event & Database Settings")]
    [Tooltip("A list of all AudioEvent ScriptableObjects to be loaded into the system.")]
    [SerializeField] private List<AudioEvent> initialEvents = new List<AudioEvent>();

    [Header("AudioSource Pooling")]
    [Tooltip("The initial number of AudioSources to create in the pool.")]
    [SerializeField] private int initialPoolSize = 10;
    [Tooltip("Whether the pool can create new AudioSources if all are currently busy.")]
    [SerializeField] private bool canPoolGrow = true;

    // --- Private Fields ---
    private Dictionary<string, AudioEvent> eventDictionary;
    private Queue<AudioSource> sourcePool;
    private GameObject poolParent; // An empty GameObject to hold the pooled sources for a clean hierarchy.

    #region --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // --- Singleton Initialization ---
        // Ensure there is only one instance of the AudioManager.
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Found more than one AudioManager in the scene. Destroying the newest one.");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        // Make the AudioManager persist across scene loads.
        DontDestroyOnLoad(this.gameObject);

        // --- System Initialization ---
        InitializeEventDatabase();
        InitializeAudioSourcePool();
    }

    #endregion

    #region --- Initialization ---

    /// <summary>
    /// Populates the event dictionary for fast, string-based lookups at runtime.
    /// </summary>
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
    /// Creates the initial pool of AudioSource components.
    /// </summary>
    private void InitializeAudioSourcePool()
    {
        sourcePool = new Queue<AudioSource>();

        // Create a parent object to keep the hierarchy clean.
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

    /// <summary>
    /// The primary method for playing a sound. Game code calls this with an event name.
    /// </summary>
    /// <param name="eventName">The unique string ID of the AudioEvent to play.</param>
    /// <param name="sourceObject">The GameObject that is the source of the sound (for 3D positioning).</param>
    public void PostEvent(string eventName, GameObject sourceObject)
    {
        // 1. Find the event in the database.
        if (!eventDictionary.TryGetValue(eventName, out AudioEvent audioEvent))
        {
            Debug.LogWarning($"AudioManager: Could not find event with ID '{eventName}'.");
            return;
        }

        // 2. Get an available AudioSource from the pool.
        AudioSource source = GetSourceFromPool();
        if (source == null)
        {
            // This happens if the pool is empty and can't grow.
            Debug.LogWarning($"AudioManager: No available AudioSources to play event '{eventName}'.");
            return;
        }

        // 3. Configure the AudioSource based on the event data.
        source.transform.position = sourceObject.transform.position;
        source.outputAudioMixerGroup = audioEvent.mixerGroup;
        // NOTE: More properties like volume, pitch, 3D settings would be set here later.

        // 4. Tell the event's container to play the sound.
        audioEvent.container.Play(source);

        // 5. Start a coroutine to return the AudioSource to the pool after it finishes playing.
        StartCoroutine(ReturnSourceToPoolAfterPlay(source));
    }

    #endregion

    #region --- Pooling Logic ---

    /// <summary>
    /// Retrieves an AudioSource from the pool. Creates a new one if the pool is empty and allowed to grow.
    /// </summary>
    /// <returns>An available AudioSource, or null if none are available.</returns>
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
            return CreateAndPoolSource(pool: false); // Create a source but don't add it to the pool queue yet.
        }

        return null;
    }

    /// <summary>
    /// Returns an AudioSource to the pool so it can be reused.
    /// </summary>
    /// <param name="source">The AudioSource to return.</param>
    private void ReturnSourceToPool(AudioSource source)
    {
        source.Stop();
        source.clip = null; // Clear the clip reference.
        source.gameObject.SetActive(false);
        sourcePool.Enqueue(source);
    }

    /// <summary>
    /// Creates a new GameObject with an AudioSource component and adds it to the pool.
    /// </summary>
    /// <param name="pool">Whether to immediately add the new source to the pool queue.</param>
    /// <returns>The newly created AudioSource.</returns>
    private AudioSource CreateAndPoolSource(bool pool = true)
    {
        GameObject newSourceGO = new GameObject($"Pooled_AudioSource_{sourcePool.Count}");
        newSourceGO.transform.SetParent(poolParent.transform);
        AudioSource newSource = newSourceGO.AddComponent<AudioSource>();

        // --- Default Settings ---
        // Set to 3D sound by default. This can be overridden by the event later.
        newSource.spatialBlend = 1.0f;
        newSource.playOnAwake = false;

        if (pool)
        {
            newSourceGO.SetActive(false);
            sourcePool.Enqueue(newSource);
        }

        return newSource;
    }

    /// <summary>
    /// A coroutine that waits for an AudioSource to finish playing its clip, then returns it to the pool.
    /// </summary>
    private IEnumerator ReturnSourceToPoolAfterPlay(AudioSource source)
    {
        // Wait until the clip has finished playing.
        // A small buffer is added to ensure it fully completes.
        yield return new WaitForSeconds(source.clip.length + 0.1f);

        ReturnSourceToPool(source);
    }

    #endregion
}

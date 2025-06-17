using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// The central hub for all audio operations. This version includes a smooth crossfade
/// during voice stealing to prevent audio clicks and pops, giving you a cleaner mix.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Data Assets")]
    [Tooltip("A list of all GameSwitch ScriptableObjects to register them with the system.")]
    [SerializeField] private List<GameSwitch> gameSwitches = new List<GameSwitch>();
    [Tooltip("A list of all GameParameter ScriptableObjects to register them with the system.")]
    [SerializeField] private List<GameParameter> gameParameters = new List<GameParameter>();
    [Tooltip("A list of all AudioState ScriptableObjects to register them with the system.")]
    [SerializeField] private List<AudioState> audioStates = new List<AudioState>();

    [Header("Mixer")]
    [Tooltip("The main AudioMixer for the project. Required for states and parameters to function.")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("AudioSource Pooling")]
    [Tooltip("The maximum number of concurrent sounds before voice stealing occurs.")]
    [SerializeField] private int initialPoolSize = 16;
    [Tooltip("Can the pool create a new temporary source if all voices are busy?")]
    [SerializeField] private bool canPoolGrow = true;
    [Tooltip("The time in seconds to fade out a stolen voice to prevent clicks. A very small value between 0.02s to 0.05s is recommended.")]
    [SerializeField] private float voiceStealFadeTime = 0.02f;


    private Dictionary<string, AudioEvent> eventDictionary;
    private Dictionary<string, string> switchDatabase;
    private Dictionary<string, float> parameterDatabase;
    private Dictionary<string, AudioState> stateDatabase;

    private List<ActiveSound> sourcePool;
    private GameObject poolParent;

    private void Awake()
    {
        // Null reference check for the singleton instance
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        // Initialize all systems
        InitializeEventDatabase();
        InitializeSwitchDatabase();
        InitializeParameterDatabase();
        InitializeStateDatabase();
        InitializeAudioSourcePool();
    }

    #region Initialization
    private void InitializeEventDatabase()
    {
        eventDictionary = new Dictionary<string, AudioEvent>();
        AudioEvent[] allEvents = Resources.LoadAll<AudioEvent>("");
        foreach (AudioEvent audioEvent in allEvents)
        {
            if (audioEvent == null || string.IsNullOrEmpty(audioEvent.eventID)) continue;
            if (eventDictionary.ContainsKey(audioEvent.eventID)) { Debug.LogWarning($"AudioManager: Duplicate event ID '{audioEvent.eventID}' found."); }
            eventDictionary[audioEvent.eventID] = audioEvent;
        }
    }
    private void InitializeSwitchDatabase()
    {
        switchDatabase = new Dictionary<string, string>();
        foreach (GameSwitch sw in gameSwitches)
        {
            if (sw == null || string.IsNullOrEmpty(sw.switchID)) continue;
            if (switchDatabase.ContainsKey(sw.switchID)) { Debug.LogWarning($"AudioManager: Duplicate switch ID '{sw.switchID}' found."); }
            switchDatabase[sw.switchID] = sw.defaultValue;
        }
    }
    private void InitializeParameterDatabase()
    {
        parameterDatabase = new Dictionary<string, float>();
        foreach (GameParameter param in gameParameters)
        {
            if (param == null || string.IsNullOrEmpty(param.parameterID)) continue;
            if (parameterDatabase.ContainsKey(param.parameterID)) { Debug.LogWarning($"AudioManager: Duplicate parameter ID '{param.parameterID}' found."); }
            parameterDatabase[param.parameterID] = param.defaultValue;
            if (mainMixer != null && !string.IsNullOrEmpty(param.exposedMixerParameter))
            {
                mainMixer.SetFloat(param.exposedMixerParameter, param.defaultValue);
            }
        }
    }
    private void InitializeStateDatabase()
    {
        stateDatabase = new Dictionary<string, AudioState>();
        foreach (AudioState state in audioStates)
        {
            if (state == null || string.IsNullOrEmpty(state.stateID)) continue;
            if (stateDatabase.ContainsKey(state.stateID)) { Debug.LogWarning($"AudioManager: Duplicate state ID '{state.stateID}' found."); }
            stateDatabase[state.stateID] = state;
        }
    }
    private void InitializeAudioSourcePool()
    {
        sourcePool = new List<ActiveSound>(initialPoolSize);
        poolParent = new GameObject("AudioSourcePool");
        poolParent.transform.SetParent(this.transform);
        for (int i = 0; i < initialPoolSize; i++) { CreatePoolObject(); }
    }
    #endregion

    #region Public API

    public void SetState(string stateId)
    {
        if (string.IsNullOrEmpty(stateId)) return;
        if (stateDatabase.TryGetValue(stateId, out AudioState state))
        {
            if (state.snapshot != null)
            {
                GameplayLogger.Log($"Setting Audio State to '{stateId}'. Transitioning to snapshot '{state.snapshot.name}' over {state.transitionTime}s.", LogCategory.Audio);
                state.snapshot.TransitionTo(state.transitionTime);
            }
        }
    }

    public void PostEvent(string eventName, GameObject sourceObject)
    {
        if (string.IsNullOrEmpty(eventName) || sourceObject == null) return;
        if (!eventDictionary.TryGetValue(eventName, out AudioEvent audioEvent))
        {
            GameplayLogger.Log($"Failed to find Audio Event with ID '{eventName}'.", LogCategory.Error);
            return;
        }

        GameplayLogger.Log($"Received PostEvent request for '{eventName}' on object '{sourceObject.name}'. Priority: {audioEvent.priority}.", LogCategory.Audio);

        ActiveSound sound = GetSourceFromPool(audioEvent.priority);
        if (sound == null) return;

        // --- NEW LOGIC: Check if the voice was stolen ---
        if (sound.source.isPlaying)
        {
            // If the source is already playing, it's a stolen voice.
            // We start a coroutine to handle the crossfade.
            StartCoroutine(FadeOutAndPlayNew(sound, audioEvent, sourceObject));
        }
        else
        {
            // If the source was free, we can configure and play it immediately.
            ConfigureAndPlay(sound, audioEvent, sourceObject);
        }
    }

    public void SetParameter(string parameterId, float value)
    {
        if (string.IsNullOrEmpty(parameterId)) return;
        if (parameterDatabase.ContainsKey(parameterId))
        {
            GameplayLogger.Log($"Setting Parameter '{parameterId}' to value '{value}'.", LogCategory.Audio);
            parameterDatabase[parameterId] = value;
            GameParameter paramAsset = gameParameters.FirstOrDefault(p => p != null && p.parameterID == parameterId);
            if (paramAsset != null && mainMixer != null && !string.IsNullOrEmpty(paramAsset.exposedMixerParameter))
            {
                float clampedValue = Mathf.Clamp(value, paramAsset.minValue, paramAsset.maxValue);
                mainMixer.SetFloat(paramAsset.exposedMixerParameter, clampedValue);
            }
        }
    }

    public void SetSwitch(string switchId, string value)
    {
        if (string.IsNullOrEmpty(switchId)) return;
        if (switchDatabase.ContainsKey(switchId))
        {
            GameplayLogger.Log($"Setting Switch '{switchId}' to value '{value}'.", LogCategory.Audio);
            switchDatabase[switchId] = value;
        }
    }

    #endregion

    #region Pooling & Priority Logic
    public string GetSwitchValue(string switchId) { if (switchDatabase.TryGetValue(switchId, out string value)) return value; return string.Empty; }

    private ActiveSound GetSourceFromPool(AudioEvent.EventPriority priority)
    {
        // First, try to find a truly inactive source.
        var inactiveSource = sourcePool.FirstOrDefault(s => !s.source.isPlaying);
        if (inactiveSource != null) return inactiveSource;

        // If none are free, find the lowest-priority sound to steal.
        var lowestPrioritySound = sourcePool.Where(s => s.source.isPlaying).OrderBy(s => s.priority).ThenBy(s => s.source.time).FirstOrDefault();

        // Check if we can steal this voice.
        if (lowestPrioritySound != null && priority >= lowestPrioritySound.priority)
        {
            GameplayLogger.Log($"VOICE STEALING: New sound (Priority: {priority}) is culling existing sound (Priority: {lowestPrioritySound.priority}).", LogCategory.Audio);
            // We don't stop it here. We return it to be faded out.
            return lowestPrioritySound;
        }

        // If we can't steal and the pool can grow, create a new source.
        if (canPoolGrow)
        {
            GameplayLogger.Log("Pool is full and no voice was available to steal. Growing pool size.", LogCategory.System);
            return CreatePoolObject();
        }

        GameplayLogger.Log($"Could not play sound (Priority: {priority}). Pool is full and no lower-priority sounds were available.", LogCategory.Error);
        return null;
    }

    /// <summary>
    /// Configures and plays a sound on a free (non-playing) AudioSource.
    /// </summary>
    private void ConfigureAndPlay(ActiveSound sound, AudioEvent audioEvent, GameObject sourceObject)
    {
        // Set up all properties on the ActiveSound and its AudioSource
        sound.priority = audioEvent.priority;
        sound.source.outputAudioMixerGroup = audioEvent.mixerGroup;

        if (audioEvent.attachToSource)
        {
            sound.transform.SetParent(sourceObject.transform, false);
            sound.transform.localPosition = Vector3.zero;
        }
        else
        {
            sound.transform.SetParent(poolParent.transform, false);
            sound.transform.position = sourceObject.transform.position;
        }

        // Get the base settings from the container
        audioEvent.container.Play(sound.source);

        // Apply any parameter modulations
        foreach (var mod in audioEvent.modulations)
        {
            if (mod.parameter == null) continue;
            if (parameterDatabase.TryGetValue(mod.parameter.parameterID, out float currentValue))
            {
                float normalizedValue = Mathf.InverseLerp(mod.parameter.minValue, mod.parameter.maxValue, currentValue);
                float multiplier = mod.mappingCurve.Evaluate(normalizedValue);
                switch (mod.targetProperty)
                {
                    case ModulationTarget.Volume: sound.source.volume *= multiplier; break;
                    case ModulationTarget.Pitch: sound.source.pitch *= multiplier; break;
                }
            }
        }

        // Play the sound with all new settings.
        sound.source.Play();

        // If the sound isn't looping, start the process to return it to the pool when finished.
        if (!sound.source.loop)
        {
            StartCoroutine(ReturnSourceToPoolAfterPlay(sound));
        }
    }

    /// <summary>
    /// A new coroutine that handles the fade-out of a stolen voice before playing the new sound.
    /// This is the core of the click-prevention system.
    /// </summary>
    private IEnumerator FadeOutAndPlayNew(ActiveSound sound, AudioEvent newEvent, GameObject sourceObject)
    {
        float startingVolume = sound.source.volume;
        float fadeTimer = 0f;

        // Part 1: Fade out the old sound
        while (fadeTimer < voiceStealFadeTime)
        {
            // Null check inside the loop in case the object is destroyed mid-fade
            if (sound == null || sound.source == null) yield break;

            fadeTimer += Time.unscaledDeltaTime;
            sound.source.volume = Mathf.Lerp(startingVolume, 0f, fadeTimer / voiceStealFadeTime);
            yield return null;
        }

        // Ensure fade completes and stop the source
        if (sound != null && sound.source != null)
        {
            sound.source.Stop();
            // Reset volume to 1 so the next sound isn't silent.
            sound.source.volume = 1f;
        }

        // Part 2: Configure and play the new sound using the now-free source.
        ConfigureAndPlay(sound, newEvent, sourceObject);
    }

    private IEnumerator ReturnSourceToPoolAfterPlay(ActiveSound sound)
    {
        yield return new WaitUntil(() => sound == null || !sound.source.isPlaying || !sound.gameObject.activeInHierarchy);
        if (sound != null && sound.gameObject.activeInHierarchy)
        {
            sound.transform.SetParent(poolParent.transform, worldPositionStays: false);
            sound.source.loop = false;
        }
    }
    private ActiveSound CreatePoolObject()
    {
        GameObject newSourceGO = new GameObject($"Pooled_AudioSource_{sourcePool.Count}");
        newSourceGO.transform.SetParent(poolParent.transform);
        AudioSource newSource = newSourceGO.AddComponent<AudioSource>();
        newSource.spatialBlend = 1.0f;
        newSource.playOnAwake = false;
        ActiveSound activeSound = newSourceGO.AddComponent<ActiveSound>();
        sourcePool.Add(activeSound);
        return activeSound;
    }
    #endregion
}

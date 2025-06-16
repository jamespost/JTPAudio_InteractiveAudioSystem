using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// The central hub for all audio operations. This version is fully integrated
/// with the GameplayLogger to provide real-time debugging information. As a sound
/// designer, you can use the on-screen log to see exactly what the audio system is
/// doing at any moment.
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

    #region Initialization (No changes here)
    private void InitializeEventDatabase()
    {
        eventDictionary = new Dictionary<string, AudioEvent>();
        AudioEvent[] allEvents = Resources.LoadAll<AudioEvent>("");
        foreach (AudioEvent audioEvent in allEvents)
        {
            // Null reference check for the asset and its ID
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
            // Null reference check for the asset and its ID
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
            // Null reference check for the asset and its ID
            if (param == null || string.IsNullOrEmpty(param.parameterID)) continue;
            if (parameterDatabase.ContainsKey(param.parameterID)) { Debug.LogWarning($"AudioManager: Duplicate parameter ID '{param.parameterID}' found."); }
            parameterDatabase[param.parameterID] = param.defaultValue;
            // Null reference check for the mixer before trying to set a parameter
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
            // Null reference check for the asset and its ID
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

    #region Public API (with Logging)

    public void SetState(string stateId)
    {
        // Null reference check for the ID
        if (string.IsNullOrEmpty(stateId)) return;
        if (stateDatabase.TryGetValue(stateId, out AudioState state))
        {
            // Null reference check for the snapshot assigned to the state
            if (state.snapshot != null)
            {
                // --- LOGGING ---
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

        // --- LOGGING ---
        GameplayLogger.Log($"Received PostEvent request for '{eventName}' on object '{sourceObject.name}'. Priority: {audioEvent.priority}.", LogCategory.Audio);

        ActiveSound sound = GetSourceFromPool(audioEvent.priority);
        if (sound == null) return;

        sound.priority = audioEvent.priority;
        sound.source.outputAudioMixerGroup = audioEvent.mixerGroup;

        if (audioEvent.attachToSource) { sound.transform.SetParent(sourceObject.transform); sound.transform.localPosition = Vector3.zero; }
        else { sound.transform.position = sourceObject.transform.position; }

        audioEvent.container.Play(sound.source);
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
        sound.source.Play();
        if (!sound.source.loop) { StartCoroutine(ReturnSourceToPoolAfterPlay(sound)); }
    }

    public void SetParameter(string parameterId, float value)
    {
        if (string.IsNullOrEmpty(parameterId)) return;
        if (parameterDatabase.ContainsKey(parameterId))
        {
            // --- LOGGING ---
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
            // --- LOGGING ---
            GameplayLogger.Log($"Setting Switch '{switchId}' to value '{value}'.", LogCategory.Audio);
            switchDatabase[switchId] = value;
        }
    }

    #endregion

    #region Pooling & Priority Logic (with Logging)
    public string GetSwitchValue(string switchId) { if (switchDatabase.TryGetValue(switchId, out string value)) return value; return string.Empty; }
    private ActiveSound GetSourceFromPool(AudioEvent.EventPriority priority)
    {
        var inactiveSource = sourcePool.FirstOrDefault(s => !s.source.isPlaying);
        if (inactiveSource != null) return inactiveSource;

        var lowestPrioritySound = sourcePool.Where(s => s.source.isPlaying).OrderBy(s => s.priority).ThenBy(s => s.source.time).FirstOrDefault();

        // Null reference check for lowestPrioritySound
        if (lowestPrioritySound != null && priority >= lowestPrioritySound.priority)
        {
            // --- LOGGING (This is the most important log for you as a sound designer) ---
            GameplayLogger.Log($"VOICE STEALING: New sound (Priority: {priority}) is culling existing sound (Priority: {lowestPrioritySound.priority}).", LogCategory.Audio);
            lowestPrioritySound.source.Stop();
            lowestPrioritySound.transform.SetParent(poolParent.transform, worldPositionStays: false);
            return lowestPrioritySound;
        }

        if (canPoolGrow)
        {
            GameplayLogger.Log("Pool is full and no voice was available to steal. Growing pool size.", LogCategory.System);
            return CreatePoolObject();
        }

        GameplayLogger.Log($"Could not play sound (Priority: {priority}). Pool is full and no lower-priority sounds were available.", LogCategory.Error);
        return null;
    }
    private IEnumerator ReturnSourceToPoolAfterPlay(ActiveSound sound)
    {
        // Null reference check for the sound object
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// The central hub for all audio operations. This version includes a granular threat
/// system. It checks each sound source for an IAudioThreat component and uses its
/// value to dynamically adjust the sound's priority at runtime.
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
    [Tooltip("The time in seconds to fade out a stolen voice to prevent clicks. A very small value between 0.01s to 0.02s is recommended for responsive games.")]
    [SerializeField] private float voiceStealFadeTime = 0.02f;

    // --- Private Databases & Pools ---
    private Dictionary<string, AudioEvent> eventDictionary;
    private Dictionary<string, string> switchDatabase;
    private Dictionary<string, float> parameterDatabase;
    private Dictionary<string, AudioState> stateDatabase;
    private List<ActiveSound> sourcePool;
    private GameObject poolParent;

    #region Initialization
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        InitializeEventDatabase();
        InitializeSwitchDatabase();
        InitializeParameterDatabase();
        InitializeStateDatabase();
        InitializeAudioSourcePool();
    }

    private void InitializeEventDatabase() { eventDictionary = new Dictionary<string, AudioEvent>(); AudioEvent[] allEvents = Resources.LoadAll<AudioEvent>(""); foreach (AudioEvent audioEvent in allEvents) { if (audioEvent == null || string.IsNullOrEmpty(audioEvent.eventID)) continue; if (eventDictionary.ContainsKey(audioEvent.eventID)) { Debug.LogWarning($"AudioManager: Duplicate event ID '{audioEvent.eventID}' found."); } eventDictionary[audioEvent.eventID] = audioEvent; } }
    private void InitializeSwitchDatabase() { switchDatabase = new Dictionary<string, string>(); foreach (GameSwitch sw in gameSwitches) { if (sw == null || string.IsNullOrEmpty(sw.switchID)) continue; if (switchDatabase.ContainsKey(sw.switchID)) { Debug.LogWarning($"AudioManager: Duplicate switch ID '{sw.switchID}' found."); } switchDatabase[sw.switchID] = sw.defaultValue; } }
    private void InitializeParameterDatabase() { parameterDatabase = new Dictionary<string, float>(); foreach (GameParameter param in gameParameters) { if (param == null || string.IsNullOrEmpty(param.parameterID)) continue; if (parameterDatabase.ContainsKey(param.parameterID)) { Debug.LogWarning($"AudioManager: Duplicate parameter ID '{param.parameterID}' found."); } parameterDatabase[param.parameterID] = param.defaultValue; if (mainMixer != null && !string.IsNullOrEmpty(param.exposedMixerParameter)) { mainMixer.SetFloat(param.exposedMixerParameter, param.defaultValue); } } }
    private void InitializeStateDatabase() { stateDatabase = new Dictionary<string, AudioState>(); foreach (AudioState state in audioStates) { if (state == null || string.IsNullOrEmpty(state.stateID)) continue; if (stateDatabase.ContainsKey(state.stateID)) { Debug.LogWarning($"AudioManager: Duplicate state ID '{state.stateID}' found."); } stateDatabase[state.stateID] = state; } }
    private void InitializeAudioSourcePool() { sourcePool = new List<ActiveSound>(initialPoolSize); poolParent = new GameObject("AudioSourcePool"); poolParent.transform.SetParent(this.transform); for (int i = 0; i < initialPoolSize; i++) { CreatePoolObject(); } }
    #endregion

    #region Public API
    public void PostEvent(string eventName, GameObject sourceObject)
    {
        if (string.IsNullOrEmpty(eventName) || sourceObject == null) return;
        if (!eventDictionary.TryGetValue(eventName, out AudioEvent audioEvent))
        {
            GameplayLogger.Log($"Failed to find Audio Event with ID '{eventName}'.", LogCategory.Error);
            return;
        }

        // --- NEW DYNAMIC THREAT CALCULATION LOGIC ---
        float dynamicThreat = 0f;
        // Check if the source object has a component that implements our threat interface.
        IAudioThreat threatComponent = sourceObject.GetComponent<IAudioThreat>();
        if (threatComponent != null)
        {
            // If it does, get its current threat level.
            dynamicThreat = threatComponent.GetCurrentThreat();
        }

        // The final priority is the event's base priority plus a weighted value from the dynamic threat.
        // We'll give the dynamic threat a range of 100 to keep its influence significant.
        int finalPriority = (int)audioEvent.priority + Mathf.RoundToInt(dynamicThreat * 100f);
        // --- END OF NEW LOGIC ---

        GameplayLogger.Log($"PostEvent: '{eventName}' on '{sourceObject.name}'. Base Priority: {(int)audioEvent.priority}, Final Priority: {finalPriority}.", LogCategory.Audio);

        ActiveSound sound = GetSourceFromPool(finalPriority);
        if (sound == null) return;

        sound.finalPriority = finalPriority; // Store the final calculated priority on the ActiveSound component.

        if (sound.source.isPlaying)
        {
            StartCoroutine(FadeOutAndPlayNew(sound, audioEvent, sourceObject));
        }
        else
        {
            ConfigureAndPlay(sound, audioEvent, sourceObject);
        }
    }

    public void SetState(string stateId) { if (string.IsNullOrEmpty(stateId)) return; if (stateDatabase.TryGetValue(stateId, out AudioState state)) { if (state.snapshot != null) { GameplayLogger.Log($"Setting Audio State to '{stateId}'.", LogCategory.Audio); state.snapshot.TransitionTo(state.transitionTime); } } }
    public void SetParameter(string parameterId, float value) { if (string.IsNullOrEmpty(parameterId)) return; if (parameterDatabase.ContainsKey(parameterId)) { GameplayLogger.Log($"Setting Parameter '{parameterId}' to value '{value}'.", LogCategory.Audio); parameterDatabase[parameterId] = value; GameParameter paramAsset = gameParameters.FirstOrDefault(p => p != null && p.parameterID == parameterId); if (paramAsset != null && mainMixer != null && !string.IsNullOrEmpty(paramAsset.exposedMixerParameter)) { float clampedValue = Mathf.Clamp(value, paramAsset.minValue, paramAsset.maxValue); mainMixer.SetFloat(paramAsset.exposedMixerParameter, clampedValue); } } }
    public void SetSwitch(string switchId, string value) { if (string.IsNullOrEmpty(switchId)) return; if (switchDatabase.ContainsKey(switchId)) { GameplayLogger.Log($"Setting Switch '{switchId}' to value '{value}'.", LogCategory.Audio); switchDatabase[switchId] = value; } }
    public string GetSwitchValue(string switchId) { if (switchDatabase.TryGetValue(switchId, out string val)) return val; return string.Empty; }
    #endregion

    #region Pooling & Priority Logic (Updated for int priority)
    private ActiveSound GetSourceFromPool(int priority)
    {
        var inactiveSource = sourcePool.FirstOrDefault(s => !s.source.isPlaying);
        if (inactiveSource != null) return inactiveSource;

        var lowestPrioritySound = sourcePool.Where(s => s.source.isPlaying).OrderBy(s => s.finalPriority).ThenBy(s => s.source.time).FirstOrDefault();

        if (lowestPrioritySound != null && priority >= lowestPrioritySound.finalPriority)
        {
            GameplayLogger.Log($"VOICE STEALING: New sound (Priority: {priority}) culling existing (Priority: {lowestPrioritySound.finalPriority}).", LogCategory.Audio);
            return lowestPrioritySound;
        }

        if (canPoolGrow)
        {
            GameplayLogger.Log("Pool is full and no voice was available to steal. Growing pool size.", LogCategory.System);
            return CreatePoolObject();
        }

        GameplayLogger.Log($"Could not play sound (Priority: {priority}). Pool full.", LogCategory.Error);
        return null;
    }

    private void ApplyAudioSourceSettings(AudioSource source, AudioSourceSettings settings)
    {
        source.volume = settings.volume;
        source.pitch = settings.pitch;
        source.spatialBlend = settings.spatialBlend;
        source.loop = settings.loop;
        source.dopplerLevel = settings.dopplerLevel;
        source.spread = settings.spread;
        source.rolloffMode = settings.rolloffMode;
        source.minDistance = settings.minDistance;
        source.maxDistance = settings.maxDistance;
        source.priority = settings.priority;
    }

    private void ConfigureAndPlay(ActiveSound sound, AudioEvent audioEvent, GameObject sourceObject)
    {
        // Apply base settings first
        ApplyAudioSourceSettings(sound.source, audioEvent.sourceSettings);

        // Note: The ActiveSound's finalPriority is set in PostEvent now.
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

        if (!sound.source.loop)
        {
            StartCoroutine(ReturnSourceToPoolAfterPlay(sound));
        }
    }

    private IEnumerator FadeOutAndPlayNew(ActiveSound sound, AudioEvent newEvent, GameObject sourceObject) { float startingVolume = sound.source.volume; float fadeTimer = 0f; while (fadeTimer < voiceStealFadeTime) { if (sound == null || sound.source == null) yield break; fadeTimer += Time.unscaledDeltaTime; sound.source.volume = Mathf.Lerp(startingVolume, 0f, fadeTimer / voiceStealFadeTime); yield return null; } if (sound != null && sound.source != null) { sound.source.Stop(); sound.source.volume = 1f; } ConfigureAndPlay(sound, newEvent, sourceObject); }
    private IEnumerator ReturnSourceToPoolAfterPlay(ActiveSound sound) { yield return new WaitUntil(() => sound == null || !sound.source.isPlaying || !sound.gameObject.activeInHierarchy); if (sound != null && sound.gameObject.activeInHierarchy) { sound.transform.SetParent(poolParent.transform, worldPositionStays: false); sound.source.loop = false; } }
    private ActiveSound CreatePoolObject() { GameObject newSourceGO = new GameObject($"Pooled_AudioSource_{sourcePool.Count}"); newSourceGO.transform.SetParent(poolParent.transform); AudioSource newSource = newSourceGO.AddComponent<AudioSource>(); newSource.spatialBlend = 1.0f; newSource.playOnAwake = false; ActiveSound activeSound = newSourceGO.AddComponent<ActiveSound>(); sourcePool.Add(activeSound); return activeSound; }
    #endregion
}

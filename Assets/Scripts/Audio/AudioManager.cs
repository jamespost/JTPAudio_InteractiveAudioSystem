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

    [Header("Reflection System Settings")]
    [Tooltip("The number of dedicated audio sources to reserve for reflections.")]
    [SerializeField] private int numberOfReflectionSources = 3;
    // [Tooltip("The minimum cutoff frequency for the low-pass filter on reflected sounds.")]
    // [SerializeField] private float reflectionLowPassCutoffMin = 500f;
    // [Tooltip("The maximum cutoff frequency for the low-pass filter on reflected sounds.")]
    // [SerializeField] private float reflectionLowPassCutoffMax = 22000f;
    [Tooltip("The speed of sound in meters per second, used for calculating reflection delays.")]
    [SerializeField] private float speedOfSound = 343f;

    [Tooltip("Enable to draw debug visualizations for audio reflections in the scene view.")]
    [SerializeField] private bool enableReflectionDebug = false;

    [Header("Reflection System Tuning")]
    [Tooltip("How much to reduce the volume of the original sound and its reflections to prevent clipping.")]
    [Range(0f, 1f)]
    [SerializeField] private float reflectionVolumeDuck = 0.75f;
    [Tooltip("A multiplier to exaggerate or reduce the calculated delay of reflections.")]
    [Range(0.1f, 100f)]
    [SerializeField] private float reflectionDelayMultiplier = 1.0f;
    [Tooltip("How much to randomly offset the reflection point on the reflector surface to make it less static and predictable.")]
    [Range(0f, 5f)]
    [SerializeField] private float reflectionJitter = 1.0f;
    [Tooltip("How much to randomly vary the pitch of reflections to reduce phasing.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float reflectionPitchJitter = 0.05f;
    [Tooltip("Maps the normalized distance of a reflection to its low-pass filter cutoff frequency.")]
    [SerializeField] private AnimationCurve reflectionCutoffCurve = AnimationCurve.EaseInOut(0, 22000, 1, 500);

    [Header("Reflection System - Dynamic Movement")]
    [Tooltip("Enables a mode where reflection sources move towards or away from the player at the speed of sound.")]
    [SerializeField] private bool enableDynamicReflectionMovement = false;

    [Header("Dynamic Echo System")]
    [Tooltip("The layer mask for surfaces that will reflect sound for the dynamic echo system.")]
    [SerializeField] private LayerMask reflectionLayerMask;

    // [Header("Dynamic Echo System (Raycast Reflections)")]
    // [Tooltip("Maximum distance for a single ray.")]
    // [SerializeField] private float dynamicEchoMaxDistance = 50f;

    [Header("Distance-Based Reverb Settings")]
    [Tooltip("The distance at which reverb starts to be applied.")]
    [SerializeField] private float reverbMinDistance = 2f;
    [Tooltip("The distance at which reverb is at its maximum level.")]
    [SerializeField] private float reverbMaxDistance = 20f;
    [Tooltip("The minimum reverb send level.")]
    [Range(0f, 1f)]
    [SerializeField] private float minReverbLevel = 0.1f;
    [Tooltip("The maximum reverb send level.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxReverbLevel = 1.0f;

    // --- Private Databases & Pools ---
    private Dictionary<string, AudioEvent> eventDictionary;
    private Dictionary<string, string> switchDatabase;
    private Dictionary<string, float> parameterDatabase;
    private Dictionary<string, AudioState> stateDatabase;
    private List<ActiveSound> sourcePool;
    private List<ActiveSound> reflectionSourcePool;
    private GameObject poolParent;
    private AudioListener audioListener;
    private DynamicEchoSystem dynamicEchoSystem;

    public AudioListener Listener => audioListener;

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
        audioListener = FindObjectOfType<AudioListener>();
        // dynamicEchoSystem = gameObject.AddComponent<DynamicEchoSystem>();
        dynamicEchoSystem = GetComponent<DynamicEchoSystem>();
        if (dynamicEchoSystem == null)
        {
            Debug.LogError("DynamicEchoSystem component is missing! Please add it to the AudioManager GameObject.");
        }
        dynamicEchoSystem.Initialize(this, reflectionLayerMask);
    }

    private void InitializeEventDatabase() { eventDictionary = new Dictionary<string, AudioEvent>(); AudioEvent[] allEvents = Resources.LoadAll<AudioEvent>(""); foreach (AudioEvent audioEvent in allEvents) { if (audioEvent == null || string.IsNullOrEmpty(audioEvent.eventID)) continue; if (eventDictionary.ContainsKey(audioEvent.eventID)) { Debug.LogWarning($"AudioManager: Duplicate event ID '{audioEvent.eventID}' found."); } eventDictionary[audioEvent.eventID] = audioEvent; } }
    private void InitializeSwitchDatabase() { switchDatabase = new Dictionary<string, string>(); foreach (GameSwitch sw in gameSwitches) { if (sw == null || string.IsNullOrEmpty(sw.switchID)) continue; if (switchDatabase.ContainsKey(sw.switchID)) { Debug.LogWarning($"AudioManager: Duplicate switch ID '{sw.switchID}' found."); } switchDatabase[sw.switchID] = sw.defaultValue; } }
    private void InitializeParameterDatabase() { parameterDatabase = new Dictionary<string, float>(); foreach (GameParameter param in gameParameters) { if (param == null || string.IsNullOrEmpty(param.parameterID)) continue; if (parameterDatabase.ContainsKey(param.parameterID)) { Debug.LogWarning($"AudioManager: Duplicate parameter ID '{param.parameterID}' found."); } parameterDatabase[param.parameterID] = param.defaultValue; if (mainMixer != null && !string.IsNullOrEmpty(param.exposedMixerParameter)) { mainMixer.SetFloat(param.exposedMixerParameter, param.defaultValue); } } }
    private void InitializeStateDatabase() { stateDatabase = new Dictionary<string, AudioState>(); foreach (AudioState state in audioStates) { if (state == null || string.IsNullOrEmpty(state.stateID)) continue; if (stateDatabase.ContainsKey(state.stateID)) { Debug.LogWarning($"AudioManager: Duplicate state ID '{state.stateID}' found."); } stateDatabase[state.stateID] = state; } }
    private void InitializeAudioSourcePool()
    {
        sourcePool = new List<ActiveSound>(initialPoolSize);
        reflectionSourcePool = new List<ActiveSound>(numberOfReflectionSources);
        poolParent = new GameObject("AudioSourcePool");
        poolParent.transform.SetParent(this.transform);

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePoolObject(sourcePool, $"Pooled_AudioSource_{i}");
        }

        for (int i = 0; i < numberOfReflectionSources; i++)
        {
            var reflectionSource = CreatePoolObject(reflectionSourcePool, $"Reflection_AudioSource_{i}");
            reflectionSource.gameObject.AddComponent<AudioLowPassFilter>();
        }
    }
    #endregion

    #region Public API
    public void PostEvent(string eventName, GameObject sourceObject)
    {
        //Debug.Log($"[AudioManager] PostEvent called for event: {eventName} on {sourceObject?.name}");
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

        float volumeMultiplier = 1.0f;
        if (audioEvent.enableReflectionSystem)
        {
            volumeMultiplier = reflectionVolumeDuck;
            //Decide which reflection system to use
            if (audioEvent.useDynamicEchoSystem)
            {
                Debug.Log($"[AudioManager] Using DynamicEchoSystem for event: {audioEvent.name}");
                dynamicEchoSystem.GenerateEchoes(audioEvent, sourceObject.transform.position);
            }
            else
            {
                HandleReflections(audioEvent, sourceObject, sound);
            }
        }

        if (sound.source.isPlaying)
        {
            StartCoroutine(FadeOutAndPlayNew(sound, audioEvent, sourceObject, volumeMultiplier));
        }
        else
        {
            ConfigureAndPlay(sound, audioEvent, sourceObject, volumeMultiplier);
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
        // --- FIX: Clean up any destroyed sources from the pool before proceeding ---
        sourcePool.RemoveAll(item => item == null);

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
            return CreatePoolObject(sourcePool, $"Pooled_AudioSource_{sourcePool.Count}");
        }

        GameplayLogger.Log($"Could not play sound (Priority: {priority}). Pool full.", LogCategory.Error);
        return null;
    }

    private void ApplyAudioSourceSettings(AudioSource source, AudioSourceSettings settings, float volumeMultiplier = 1.0f)
    {
        source.volume = settings.volume * volumeMultiplier;
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

    private void ConfigureAndPlay(ActiveSound sound, AudioEvent audioEvent, GameObject sourceObject, float volumeMultiplier = 1.0f)
    {
        // Apply base settings first
        ApplyAudioSourceSettings(sound.source, audioEvent.sourceSettings, volumeMultiplier);

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

        if (audioEvent.enableDistanceBasedReverb)
        {
            StartCoroutine(UpdateDistanceBasedReverb(sound));
        }
        else
        {
            sound.source.reverbZoneMix = 1.0f; // Use default reverb settings
        }

        sound.source.Play();

        if (!sound.source.loop)
        {
            StartCoroutine(ReturnSourceToPoolAfterPlay(sound));
        }
    }

    public void PlayEcho(AudioEvent audioEvent, Vector3 position, float delay, float attenuation)
    {
        ActiveSound echoSound = GetSourceFromPool((int)audioEvent.priority);
        if (echoSound == null) return;

        echoSound.transform.position = position;
        ApplyAudioSourceSettings(echoSound.source, audioEvent.sourceSettings, attenuation);
        echoSound.source.outputAudioMixerGroup = audioEvent.mixerGroup;

        // Apply low-pass filter based on distance (similar to old system)
        var lowpass = echoSound.GetComponent<AudioLowPassFilter>();
        if (lowpass)
        {
            float distance = Vector3.Distance(position, audioListener.transform.position);
            float normalizedDistance = Mathf.Clamp01(distance / audioEvent.sourceSettings.maxDistance);
            float cutoff = reflectionCutoffCurve.Evaluate(normalizedDistance);
            lowpass.cutoffFrequency = cutoff;
        }

        audioEvent.container.Play(echoSound.source);
        echoSound.source.PlayDelayed(delay);

        StartCoroutine(ReturnSourceToPoolAfterPlay(echoSound));
    }

    public void PlayEchoWithFilter(AudioEvent audioEvent, Vector3 position, float delay, float attenuation, float cutoff)
    {
        ActiveSound echoSound = GetSourceFromPool((int)audioEvent.priority);
        if (echoSound == null) return;

        echoSound.transform.position = position;
        // Calculate spatial attenuation using the rolloff curve
        float listenerDistance = Vector3.Distance(position, audioListener.transform.position);
        float spatialAttenuation = 1.0f;
        var settings = audioEvent.sourceSettings;
        switch (settings.rolloffMode)
        {
            case AudioRolloffMode.Logarithmic:
                if (listenerDistance < settings.minDistance) spatialAttenuation = 1.0f;
                else if (listenerDistance > settings.maxDistance) spatialAttenuation = 0.0f;
                else spatialAttenuation = settings.minDistance / (settings.minDistance + 1.0f * (listenerDistance - settings.minDistance));
                break;
            case AudioRolloffMode.Linear:
                if (listenerDistance < settings.minDistance) spatialAttenuation = 1.0f;
                else if (listenerDistance > settings.maxDistance) spatialAttenuation = 0.0f;
                else spatialAttenuation = 1.0f - ((listenerDistance - settings.minDistance) / (settings.maxDistance - settings.minDistance));
                break;
            case AudioRolloffMode.Custom:
                // If you use a custom curve, you may want to expose it and evaluate here
                spatialAttenuation = 1.0f; // Placeholder
                break;
        }
        // Ensure the reflection is always quieter than the direct sound
        float finalAttenuation = Mathf.Min(attenuation * spatialAttenuation, 0.95f * settings.volume);
        ApplyAudioSourceSettings(echoSound.source, settings, finalAttenuation);
        echoSound.source.outputAudioMixerGroup = audioEvent.mixerGroup;

        var lowpass = echoSound.GetComponent<AudioLowPassFilter>();
        if (lowpass)
        {
            lowpass.cutoffFrequency = cutoff;
        }

        audioEvent.container.Play(echoSound.source);
        echoSound.source.PlayDelayed(delay);

        StartCoroutine(ReturnSourceToPoolAfterPlay(echoSound));
    }

    private void HandleReflections(AudioEvent audioEvent, GameObject sourceObject, ActiveSound originalSound)
    {
        if (audioListener == null) return;

        var reflectors = FindObjectsOfType<AudioReflector>();
        if (reflectors.Length == 0) return;

        var listenerPosition = audioListener.transform.position;
        var listenerForward = audioListener.transform.forward;
        var sourcePosition = sourceObject.transform.position;

        var potentialReflections = new List<(AudioReflector reflector, float pathLength, Vector3 reflectionPoint, float score)>();

        foreach (var reflector in reflectors)
        {
            Collider col = reflector.GetComponent<Collider>();
            if (col == null) continue;

            Vector3 reflectionPoint = col.ClosestPoint(sourcePosition);

            // Jitter the reflection point to make it less static and predictable
            if (reflectionJitter > 0)
            {
                Vector3 randomOffset = Random.insideUnitSphere * reflectionJitter;
                reflectionPoint = col.ClosestPoint(reflectionPoint + randomOffset);
            }

            float sourceToReflector = Vector3.Distance(sourcePosition, reflectionPoint);
            float reflectorToListener = Vector3.Distance(reflectionPoint, listenerPosition);
            float pathLength = sourceToReflector + reflectorToListener;

            // --- New Scoring Logic ---
            // Score based on being to the sides of the listener
            Vector3 toReflectionDir = (reflectionPoint - listenerPosition).normalized;
            float sideFactor = 1f - Mathf.Abs(Vector3.Dot(toReflectionDir, listenerForward)); // 1 for sides, 0 for front/back

            // Score based on distance
            float distanceFactor = Mathf.Clamp01(pathLength / (audioEvent.sourceSettings.maxDistance * 1.5f)); // Normalize distance

            // Combine scores, prioritizing side reflections
            float finalScore = (sideFactor * 0.7f) + (distanceFactor * 0.3f);
            finalScore *= Random.Range(0.75f, 1.25f); // Add some variance to the selection

            potentialReflections.Add((reflector, pathLength, reflectionPoint, finalScore));
        }

        var sortedReflectors = potentialReflections.OrderByDescending(r => r.score).Take(numberOfReflectionSources).ToList();

        for (int i = 0; i < sortedReflectors.Count; i++)
        {
            var reflection = sortedReflectors[i];
            ActiveSound reflectionSound = reflectionSourcePool[i];

            if (reflectionSound.source.isPlaying)
            {
                reflectionSound.source.Stop();
            }

            reflectionSound.transform.position = reflection.reflectionPoint;
            ApplyAudioSourceSettings(reflectionSound.source, audioEvent.sourceSettings, reflectionVolumeDuck);
            reflectionSound.source.outputAudioMixerGroup = audioEvent.mixerGroup;

            if (reflectionPitchJitter > 0)
            {
                reflectionSound.source.pitch *= 1.0f + Random.Range(-reflectionPitchJitter, reflectionPitchJitter);
            }

            // The delay is the total time it takes for the sound to travel from the source,
            // bounce off the reflector, and reach the listener. This provides a more intuitive and controllable delay.
            float delay = (reflection.pathLength / speedOfSound) * reflectionDelayMultiplier;

            if (delay < 0) delay = 0;

            // Attenuation should be based on the total distance the reflected sound travels.
            float distanceAttenuation = 1f / (1f + reflection.pathLength);
            reflectionSound.source.volume *= distanceAttenuation;

            var lowpass = reflectionSound.GetComponent<AudioLowPassFilter>();
            if (lowpass)
            {
                float normalizedDistance = Mathf.Clamp01(reflection.pathLength / (audioEvent.sourceSettings.maxDistance * 2));
                float cutoff = reflectionCutoffCurve.Evaluate(normalizedDistance);
                lowpass.cutoffFrequency = cutoff;
            }

            audioEvent.container.Play(reflectionSound.source);
            reflectionSound.source.PlayDelayed(delay);

            if (enableDynamicReflectionMovement)
            {
                StartCoroutine(MoveReflectionSource(reflectionSound, listenerPosition));
            }

            // Apply distance-based reverb to reflections if enabled for the event
            if (audioEvent.enableDistanceBasedReverb)
            {
                StartCoroutine(UpdateDistanceBasedReverb(reflectionSound));
            }
            else
            {
                // If not enabled, ensure reverb is set to the default value
                reflectionSound.source.reverbZoneMix = 1.0f;
            }

            if (enableReflectionDebug)
            {
                float markerSize = 0.25f;
                Debug.DrawLine(reflection.reflectionPoint - Vector3.up * markerSize, reflection.reflectionPoint + Vector3.up * markerSize, Color.cyan, 2f);
                Debug.DrawLine(reflection.reflectionPoint - Vector3.right * markerSize, reflection.reflectionPoint + Vector3.right * markerSize, Color.cyan, 2f);
                Debug.DrawLine(reflection.reflectionPoint - Vector3.forward * markerSize, reflection.reflectionPoint + Vector3.forward * markerSize, Color.cyan, 2f);
            }
        }
    }

    private IEnumerator MoveReflectionSource(ActiveSound reflectionSound, Vector3 listenerPosition)
    {
        // Decide randomly to move towards or away from the listener
        bool moveTowards = Random.value > 0.5f;
        Vector3 direction;

        if (moveTowards)
        {
            direction = (listenerPosition - reflectionSound.transform.position).normalized;
        }
        else
        {
            direction = (reflectionSound.transform.position - listenerPosition).normalized;
        }

        // If the direction is zero (e.g., at the listener), stop moving.
        if (direction == Vector3.zero) yield break;

        while (reflectionSound != null && reflectionSound.source != null && reflectionSound.source.isPlaying)
        {
            reflectionSound.transform.position += direction * speedOfSound * Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator UpdateDistanceBasedReverb(ActiveSound activeSound)
    {
        if (audioListener == null) yield break;

        AudioSource source = activeSound.source;
        Transform listenerTransform = audioListener.transform;

        while (source != null && source.isPlaying)
        {
            float distance = Vector3.Distance(source.transform.position, listenerTransform.position);
            float normalizedDistance = Mathf.InverseLerp(reverbMinDistance, reverbMaxDistance, distance);
            float reverbLevel = Mathf.Lerp(minReverbLevel, maxReverbLevel, normalizedDistance);
            source.reverbZoneMix = reverbLevel;

            yield return null;
        }
    }

    private IEnumerator FadeOutAndPlayNew(ActiveSound sound, AudioEvent newEvent, GameObject sourceObject, float volumeMultiplier)
    { 
        float startingVolume = sound.source.volume; 
        float fadeTimer = 0f; 
        while (fadeTimer < voiceStealFadeTime) 
        { 
            if (sound == null || sound.source == null) yield break; 
            fadeTimer += Time.unscaledDeltaTime; 
            sound.source.volume = Mathf.Lerp(startingVolume, 0f, fadeTimer / voiceStealFadeTime); 
            yield return null; 
        } 
        if (sound != null && sound.source != null) 
        { 
            sound.source.Stop(); 
            sound.source.volume = 1f; 
        } 
        ConfigureAndPlay(sound, newEvent, sourceObject, volumeMultiplier); 
    }
    private IEnumerator ReturnSourceToPoolAfterPlay(ActiveSound sound) { yield return new WaitUntil(() => sound == null || !sound.source.isPlaying || !sound.gameObject.activeInHierarchy); if (sound != null && sound.gameObject.activeInHierarchy) { sound.transform.SetParent(poolParent.transform, worldPositionStays: false); sound.source.loop = false; } }
    private ActiveSound CreatePoolObject(List<ActiveSound> pool, string name)
    {
        GameObject newSourceGO = new GameObject(name);
        newSourceGO.transform.SetParent(poolParent.transform);
        AudioSource newSource = newSourceGO.AddComponent<AudioSource>();
        newSource.spatialBlend = 1.0f;
        newSource.playOnAwake = false;
        ActiveSound activeSound = newSourceGO.AddComponent<ActiveSound>();
        pool.Add(activeSound);
        return activeSound;
    }
    #endregion
}

using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    [System.Serializable]
    public class GameplayCueEntry
    {
        [Tooltip("The tag that triggers this cue.")]
        public GameplayTag Tag;

        [Tooltip("Audio Event ID to play when the tag is added.")]
        public string OnAddAudioEvent;

        [Tooltip("Audio Event ID to play when the tag is removed.")]
        public string OnRemoveAudioEvent;

        [Tooltip("If true, the OnAddAudioEvent will be stopped when the tag is removed.")]
        public bool StopOnAddEventOnRemove;
    }

    [RequireComponent(typeof(AbilitySystemComponent))]
    public class GameplayCue : MonoBehaviour
    {
        [SerializeField] private List<GameplayCueEntry> cues = new List<GameplayCueEntry>();

        private AbilitySystemComponent asc;

        private void Awake()
        {
            asc = GetComponent<AbilitySystemComponent>();
        }

        private void OnEnable()
        {
            if (asc != null)
            {
                asc.TagContainer.OnTagAdded += HandleTagAdded;
                asc.TagContainer.OnTagRemoved += HandleTagRemoved;
            }
        }

        private void OnDisable()
        {
            if (asc != null)
            {
                asc.TagContainer.OnTagAdded -= HandleTagAdded;
                asc.TagContainer.OnTagRemoved -= HandleTagRemoved;
            }
        }

        private void HandleTagAdded(GameplayTag tag)
        {
            foreach (var cue in cues)
            {
                if (cue.Tag == tag)
                {
                    if (!string.IsNullOrEmpty(cue.OnAddAudioEvent))
                    {
                        AudioManager.Instance.PostEvent(cue.OnAddAudioEvent, gameObject);
                    }
                }
            }
        }

        private void HandleTagRemoved(GameplayTag tag)
        {
            foreach (var cue in cues)
            {
                if (cue.Tag == tag)
                {
                    if (cue.StopOnAddEventOnRemove && !string.IsNullOrEmpty(cue.OnAddAudioEvent))
                    {
                        AudioManager.Instance.StopEvent(cue.OnAddAudioEvent, gameObject);
                    }

                    if (!string.IsNullOrEmpty(cue.OnRemoveAudioEvent))
                    {
                        AudioManager.Instance.PostEvent(cue.OnRemoveAudioEvent, gameObject);
                    }
                }
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using System;

namespace GAS
{
    [System.Serializable]
    public class GameplayTagContainer
    {
        [SerializeField] private List<GameplayTag> tags = new List<GameplayTag>();

        public List<GameplayTag> Tags => tags;

        public event Action<GameplayTag> OnTagAdded;
        public event Action<GameplayTag> OnTagRemoved;

        public void AddTag(GameplayTag tag)
        {
            if (tag != null && !tags.Contains(tag))
            {
                tags.Add(tag);
                OnTagAdded?.Invoke(tag);
            }
        }

        public void RemoveTag(GameplayTag tag)
        {
            if (tags.Contains(tag))
            {
                tags.Remove(tag);
                OnTagRemoved?.Invoke(tag);
            }
        }

        public bool HasTag(GameplayTag tag)
        {
            return tags.Contains(tag);
        }

        public bool HasAny(IEnumerable<GameplayTag> otherTags)
        {
            if (otherTags == null) return false;
            foreach (var tag in otherTags)
            {
                if (tags.Contains(tag)) return true;
            }
            return false;
        }

        public bool HasAll(IEnumerable<GameplayTag> otherTags)
        {
            if (otherTags == null) return true;
            foreach (var tag in otherTags)
            {
                if (!tags.Contains(tag)) return false;
            }
            return true;
        }
        
        public void Clear()
        {
            tags.Clear();
        }
    }
}

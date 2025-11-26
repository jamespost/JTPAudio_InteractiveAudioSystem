using UnityEngine;

namespace GAS
{
    [CreateAssetMenu(menuName = "GAS/Gameplay Tag")]
    public class GameplayTag : ScriptableObject
    {
        [SerializeField] private string tagName;

        public string TagName => tagName;

        public override string ToString() => tagName;

        // Simple equality check
        public bool Matches(GameplayTag other)
        {
            return this == other;
        }
    }
}

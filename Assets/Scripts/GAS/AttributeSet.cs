using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace GAS
{
    // Base class for defining attributes on an actor
    public abstract class AttributeSet : MonoBehaviour
    {
        protected Dictionary<string, GameplayAttribute> attributeMap = new Dictionary<string, GameplayAttribute>();

        protected virtual void Awake()
        {
            InitializeAttributes();
        }

        protected virtual void InitializeAttributes()
        {
            // Reflection to find all GameplayAttribute fields and add to map
            var fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameplayAttribute))
                {
                    var attr = field.GetValue(this) as GameplayAttribute;
                    if (attr == null)
                    {
                        attr = new GameplayAttribute(0);
                        field.SetValue(this, attr);
                    }
                    attributeMap[field.Name] = attr;
                }
            }
        }

        public GameplayAttribute GetAttribute(string attributeName)
        {
            if (attributeMap.TryGetValue(attributeName, out var attr))
            {
                return attr;
            }
            return null;
        }
        
        public float GetAttributeValue(string attributeName)
        {
            var attr = GetAttribute(attributeName);
            return attr != null ? attr.CurrentValue : 0f;
        }
    }
}

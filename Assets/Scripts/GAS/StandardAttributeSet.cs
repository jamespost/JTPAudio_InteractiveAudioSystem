using UnityEngine;

namespace GAS
{
    public class StandardAttributeSet : AttributeSet
    {
        public GameplayAttribute Health;
        public GameplayAttribute MaxHealth;
        public GameplayAttribute Mana;
        public GameplayAttribute MaxMana;
        public GameplayAttribute Speed;
        public GameplayAttribute Damage;

        protected override void Awake()
        {
            // Initialize default values if they are null (though Inspector usually handles this)
            if (Health == null) Health = new GameplayAttribute(100);
            if (MaxHealth == null) MaxHealth = new GameplayAttribute(100);
            if (Mana == null) Mana = new GameplayAttribute(50);
            if (MaxMana == null) MaxMana = new GameplayAttribute(50);
            if (Speed == null) Speed = new GameplayAttribute(5);
            if (Damage == null) Damage = new GameplayAttribute(10);

            base.Awake(); // Registers them in the dictionary
        }
    }
}

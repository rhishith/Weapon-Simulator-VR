using UnityEngine;

namespace VRWeaponSimulator
{
    public enum PartType
    {
        Magazine,
        Barrel,
        Slide,
        Stock,
        Sight,
        Underbarrel,
        Muzzle
    }

    [CreateAssetMenu(fileName = "NewPartData", menuName = "Weapon Simulator/Part Data")]
    public class PartData : ScriptableObject
    {
        [Header("Identity")]
        public string partName;
        public PartType type;

        [Header("Stat Modifiers")]
        public float damageModifier = 1.0f;
        public float recoilModifier = 1.0f; // Lower is better
        public float accuracyModifier = 1.0f; // Higher is better
        public float weightModifier = 1.0f;
        
        [Header("Functional Config")]
        public int ammoCapacityBonus = 0;
        public bool isCriticalForFiring = false; // Is this part required to fire?
    }
}

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

        [Header("Simulator Info")]
        [TextArea]
        public string description;
        public float weight = 1.0f;
    }
}

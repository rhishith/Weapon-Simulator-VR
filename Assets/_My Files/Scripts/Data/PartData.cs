using UnityEngine;

namespace VRWeaponSimulator
{
    public enum PartType
    {
        Magazine = 0,
        Barrel = 1,
        Slide = 2,
        Stock = 3,
        Sight = 4,
        Underbarrel = 5,
        Muzzle = 6,
        Receiver = 7,
        Trigger = 8,
        Grip = 9,
        Rail = 10,
        Bolt = 11,
        Other = 12
    }

    [CreateAssetMenu(fileName = "NewPartData", menuName = "Weapon Simulator/Part Data")]
    public class PartData : ScriptableObject
    {
        public string partName;
        public PartType type;

        [TextArea]
        public string description;

        public float weight = 1f;
    }
}
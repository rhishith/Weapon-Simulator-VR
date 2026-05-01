using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRWeaponSimulator
{
    public class WeaponBase : MonoBehaviour
    {
        [Header("Info")]
        public string weaponName;

        [Header("State")]
        public List<WeaponPart> attachedParts = new();
        private List<WeaponPart> allParts = new();

        public bool isFullyAssembled;

        [Header("Events")]
        public UnityEvent onWeaponFullyAssembled;
        public UnityEvent onWeaponIncomplete;

        private const string PartsRootName = "Parts";

        private void Awake()
        {
            AutoCollectParts();
        }

        private void AutoCollectParts()
        {
            Transform partsRoot = transform.Find(PartsRootName);

            if (partsRoot == null)
            {
                Debug.LogError($"[WeaponBase] Parts root not found!", this);
                return;
            }

            allParts.AddRange(partsRoot.GetComponentsInChildren<WeaponPart>());
        }

        public void RegisterPart(WeaponPart part)
        {
            if (!attachedParts.Contains(part))
            {
                attachedParts.Add(part);
                UpdateAssemblyState();
            }
        }

        public void UnregisterPart(WeaponPart part)
        {
            if (attachedParts.Contains(part))
            {
                attachedParts.Remove(part);
                UpdateAssemblyState();
            }
        }

        private void UpdateAssemblyState()
        {
            bool wasFull = isFullyAssembled;

            isFullyAssembled = attachedParts.Count == allParts.Count;

            if (!wasFull && isFullyAssembled)
            {
                onWeaponFullyAssembled?.Invoke();
                Debug.Log($"{weaponName} Fully Assembled");
            }
            else if (wasFull && !isFullyAssembled)
            {
                onWeaponIncomplete?.Invoke();
                Debug.Log($"{weaponName} Incomplete");
            }
        }
    }
}
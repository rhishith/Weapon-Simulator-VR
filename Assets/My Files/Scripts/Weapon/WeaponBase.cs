using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRWeaponSimulator
{
    public class WeaponBase : MonoBehaviour
    {
        [Header("Settings")]
        public string weaponName;
        [Tooltip("The parts required for the weapon to be considered fully assembled.")]
        public List<PartType> requiredPartsForAssembly;

        [Header("Assembly State")]
        public List<WeaponPart> attachedParts = new List<WeaponPart>();
        public bool isFullyAssembled = false;
        public float totalWeight = 0f;
        
        [Header("Events")]
        public UnityEvent<WeaponPart> onPartAttached;
        public UnityEvent<WeaponPart> onPartDetached;
        public UnityEvent onWeaponFullyAssembled;
        public UnityEvent onWeaponIncomplete;

        public void RegisterPart(WeaponPart part)
        {
            if (!attachedParts.Contains(part))
            {
                attachedParts.Add(part);
                onPartAttached?.Invoke(part);
                UpdateAssemblyState();
            }
        }

        public void UnregisterPart(WeaponPart part)
        {
            if (attachedParts.Contains(part))
            {
                attachedParts.Remove(part);
                onPartDetached?.Invoke(part);
                UpdateAssemblyState();
            }
        }

        private void UpdateAssemblyState()
        {
            totalWeight = 0f;
            HashSet<PartType> currentPartTypes = new HashSet<PartType>();

            foreach (var part in attachedParts)
            {
                if (part.data == null) continue;
                
                totalWeight += part.data.weight;
                currentPartTypes.Add(part.data.type);
            }

            bool wasFullyAssembled = isFullyAssembled;
            isFullyAssembled = true;
            
            foreach (var requiredType in requiredPartsForAssembly)
            {
                if (!currentPartTypes.Contains(requiredType))
                {
                    isFullyAssembled = false;
                    break;
                }
            }

            if (!wasFullyAssembled && isFullyAssembled) 
            {
                onWeaponFullyAssembled?.Invoke();
                Debug.Log($"[{weaponName}] is fully assembled!");
            }
            else if (wasFullyAssembled && !isFullyAssembled) 
            {
                onWeaponIncomplete?.Invoke();
                Debug.Log($"[{weaponName}] is no longer fully assembled.");
            }
        }
    }
}

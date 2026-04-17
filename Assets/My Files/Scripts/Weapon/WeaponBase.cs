using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRWeaponSimulator
{
    public class WeaponBase : MonoBehaviour
    {
        [Header("Settings")]
        public string weaponName;
        public List<PartType> requiredPartsForFiring;

        [Header("State")]
        public List<WeaponPart> attachedParts = new List<WeaponPart>();
        
        [Header("Events")]
        public UnityEvent onStatsUpdated;
        public UnityEvent onWeaponFullyAssembled;
        public UnityEvent onWeaponDisassembled;

        [Header("Calculated Stats")]
        public float currentDamage = 10f;
        public float currentRecoil = 1.0f;
        public float currentAccuracy = 0.8f;
        public bool isReadyToFire = false;

        public void RegisterPart(WeaponPart part)
        {
            if (!attachedParts.Contains(part))
            {
                attachedParts.Add(part);
                CalculateStats();
            }
        }

        public void UnregisterPart(WeaponPart part)
        {
            if (attachedParts.Contains(part))
            {
                attachedParts.Remove(part);
                CalculateStats();
            }
        }

        private void CalculateStats()
        {
            // Reset to defaults (could be from a BaseWeaponData SO too)
            currentDamage = 10f;
            currentRecoil = 1.0f;
            currentAccuracy = 0.8f;

            HashSet<PartType> currentPartTypes = new HashSet<PartType>();

            foreach (var part in attachedParts)
            {
                if (part.data == null) continue;

                currentDamage *= part.data.damageModifier;
                currentRecoil *= part.data.recoilModifier;
                currentAccuracy *= part.data.accuracyModifier;
                
                currentPartTypes.Add(part.data.type);
            }

            // Check firing readiness
            bool wasReady = isReadyToFire;
            isReadyToFire = true;
            foreach (var requiredType in requiredPartsForFiring)
            {
                if (!currentPartTypes.Contains(requiredType))
                {
                    isReadyToFire = false;
                    break;
                }
            }

            if (!wasReady && isReadyToFire) onWeaponFullyAssembled?.Invoke();
            if (wasReady && !isReadyToFire) onWeaponDisassembled?.Invoke();

            onStatsUpdated?.Invoke();
        }
    }
}

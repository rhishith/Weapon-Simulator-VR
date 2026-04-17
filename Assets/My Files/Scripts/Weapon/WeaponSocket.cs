using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRWeaponSimulator
{
    public class WeaponSocket : UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor
    {
        [Header("Weapon Integration")]
        public WeaponBase weaponBase;
        public PartType acceptedPartType;

        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.AddListener(OnPartAttached);
            selectExited.AddListener(OnPartDetached);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            selectEntered.RemoveListener(OnPartAttached);
            selectExited.RemoveListener(OnPartDetached);
        }

        public override bool CanSelect(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
        {
            if (!base.CanSelect(interactable)) return false;

            // Check if the interactable is a WeaponPart and if it matches the type
            var part = interactable.transform.GetComponent<WeaponPart>();
            if (part == null) return false;

            return part.data.type == acceptedPartType;
        }

        private void OnPartAttached(SelectEnterEventArgs args)
        {
            var part = args.interactableObject.transform.GetComponent<WeaponPart>();
            if (part != null && weaponBase != null)
            {
                weaponBase.RegisterPart(part);
            }
        }

        private void OnPartDetached(SelectExitEventArgs args)
        {
            var part = args.interactableObject.transform.GetComponent<WeaponPart>();
            if (part != null && weaponBase != null)
            {
                weaponBase.UnregisterPart(part);
            }
        }
    }
}

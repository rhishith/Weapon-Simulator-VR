using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRWeaponSimulator
{
    /// <summary>
    /// Attach this to a weapon's bolt catch / slide release paddle or button.
    /// Interacting with this releases a locked slide, slamming it forward into battery.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class WeaponBoltCatch : MonoBehaviour
    {
        public WeaponBase weaponBase;
        public WeaponSlide weaponSlide;

        public bool triggerOnSelect = true;
        public bool triggerOnHover = false;

        private XRBaseInteractable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<XRBaseInteractable>();

            if (weaponBase == null)
                weaponBase = GetComponentInParent<WeaponBase>();

            if (weaponSlide == null && weaponBase != null)
                weaponSlide = weaponBase.weaponSlide;
        }

        private void OnEnable()
        {
            if (triggerOnSelect)
                _interactable.selectEntered.AddListener(OnSelectEntered);

            if (triggerOnHover)
                _interactable.hoverEntered.AddListener(OnHoverEntered);
        }

        private void OnDisable()
        {
            if (triggerOnSelect)
                _interactable.selectEntered.RemoveListener(OnSelectEntered);

            if (triggerOnHover)
                _interactable.hoverEntered.RemoveListener(OnHoverEntered);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            PressCatch();
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            PressCatch();
        }

        public void PressCatch()
        {
            if (weaponSlide == null && weaponBase != null)
                weaponSlide = weaponBase.weaponSlide;

            if (weaponSlide != null)
            {
                if (weaponSlide.isLockedBack)
                    weaponSlide.ReleaseBolt();
                else
                    weaponSlide.LockBoltOpen();
            }
        }
    }
}

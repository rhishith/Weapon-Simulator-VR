using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRWeaponSimulator
{
    /// <summary>
    /// Attach this to a physical magazine release catch/button on the weapon model.
    /// Touching or selecting this button ejects the magazine from the weapon.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class MagazineReleaseButton : MonoBehaviour
    {
        public WeaponBase weaponBase;
        public bool triggerOnHover = false;
        public bool triggerOnSelect = true;

        private XRBaseInteractable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<XRBaseInteractable>();

            if (weaponBase == null)
                weaponBase = GetComponentInParent<WeaponBase>();
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
            ReleaseMagazine();
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            ReleaseMagazine();
        }

        public void ReleaseMagazine()
        {
            if (weaponBase != null)
            {
                weaponBase.EjectMagazine();
            }
        }
    }
}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRWeaponSimulator
{
    /// <summary>
    /// Attach this to a weapon's front handguard, barrel guard, or forward grip.
    /// Provides authentic two-handed stabilization and aiming in VR when held with the offhand.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class WeaponSecondaryGrip : MonoBehaviour
    {
        [Header("Weapon References")]
        public WeaponBase weaponBase;

        [Header("Haptics")]
        public bool enableHaptics = true;
        [Range(0f, 1f)] public float gripHapticAmplitude = 0.45f;
        public float gripHapticDuration = 0.08f;

        [Header("Stabilization")]
        [Tooltip("How smoothly the weapon rotates towards the secondary hand (higher = snappier).")]
        public float rotationSmoothing = 25f;

        private XRBaseInteractable _interactable;
        private IXRInteractor _secondaryHand;
        private XRGrabInteractable _mainWeaponGrab;

        public bool IsBeingHeld => _secondaryHand != null;

        private void Awake()
        {
            _interactable = GetComponent<XRBaseInteractable>();

            if (weaponBase == null)
                weaponBase = GetComponentInParent<WeaponBase>();
        }

        private void Start()
        {
            if (weaponBase != null)
                _mainWeaponGrab = weaponBase.GetComponent<XRGrabInteractable>();
        }

        private void OnEnable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.AddListener(OnSelectEntered);
                _interactable.selectExited.AddListener(OnSelectExited);
            }
        }

        private void OnDisable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.RemoveListener(OnSelectEntered);
                _interactable.selectExited.RemoveListener(OnSelectExited);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is not XRSocketInteractor)
            {
                _secondaryHand = args.interactorObject;

                if (enableHaptics)
                    WeaponHaptics.SendImpulse(_secondaryHand, gripHapticAmplitude, gripHapticDuration);
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactorObject == _secondaryHand)
            {
                _secondaryHand = null;
            }
        }

        private void LateUpdate()
        {
            // Only stabilize when both the main weapon and this secondary grip are held
            if (_secondaryHand == null || weaponBase == null) return;

            // Check if main weapon is held
            if (_mainWeaponGrab != null && _mainWeaponGrab.interactorsSelecting.Count > 0)
            {
                var mainHand = _mainWeaponGrab.interactorsSelecting[0];

                Vector3 targetForward = (_secondaryHand.transform.position - mainHand.transform.position).normalized;
                if (targetForward.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(targetForward, mainHand.transform.up);
                    weaponBase.transform.rotation = Quaternion.Slerp(weaponBase.transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
                }
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRWeaponSimulator
{
    public class WeaponSocket : XRSocketInteractor
    {
        [Header("Weapon")]
        public WeaponBase weaponBase;

        [Header("Dependencies (Optional)")]
        public List<WeaponSocket> requiredSockets = new();

        [Header("Auto References")]
        public WeaponPart expectedPart;
        public GameObject ghostMesh;

        [Header("Haptics")]
        public bool enableHaptics = true;
        [Range(0f, 1f)] public float snapHapticAmplitude = 0.85f;
        public float snapHapticDuration = 0.12f;
        [Range(0f, 1f)] public float hoverHapticAmplitude = 0.2f;
        public float hoverHapticDuration = 0.04f;
        [Range(0f, 1f)] public float detachHapticAmplitude = 0.45f;
        public float detachHapticDuration = 0.08f;

        [Header("Magazine Specific")]
        public bool isMagazineSocket;
        public bool lockMagazineWhenAttached = true;
        public float ejectDownwardForce = 0.75f;
        public AudioClip ejectSound;
        public AudioClip snapSound;

        private InteractionLayerMask _savedMagLayers = ~0;

        private const string PartsRootName = "Parts";
        private const string GhostRootName = "Ghost Visuals";

        protected override void Awake()
        {
            base.Awake();

            if (weaponBase == null)
            {
                weaponBase = GetComponentInParent<WeaponBase>();

                if (weaponBase == null)
                {
                    Debug.LogError($"[WeaponSocket] {name} could not find WeaponBase in parents!", this);
                    return;
                }
            }

            AutoResolvePart();
            AutoResolveGhost();

            // Detect if this is a magazine socket
            if (name.Contains("Mag") || (expectedPart != null && expectedPart.data != null && expectedPart.data.type == PartType.Magazine))
            {
                isMagazineSocket = true;
            }
        }

        protected override void Start()
        {
            base.Start();

            if (hasSelection && ghostMesh != null)
                ghostMesh.SetActive(false);

            // Ensure already attached parts start kinematic to eliminate PhysX jitter
            if (hasSelection && firstInteractableSelected != null)
            {
                if (firstInteractableSelected.transform.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        private void AutoResolvePart()
        {
            if (expectedPart != null) return;
            if (weaponBase == null) return;

            Transform partsRoot = weaponBase.transform.Find(PartsRootName);
            if (partsRoot == null) return;

            Transform found = partsRoot.Find(name);
            if (found != null)
                expectedPart = found.GetComponent<WeaponPart>();
        }

        private void AutoResolveGhost()
        {
            if (ghostMesh != null) return;
            if (weaponBase == null) return;

            Transform ghostRoot = weaponBase.transform.Find(GhostRootName);
            if (ghostRoot == null) return;

            Transform found = ghostRoot.Find(name);
            if (found != null)
                ghostMesh = found.gameObject;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.AddListener(OnPartAttached);
            selectExited.AddListener(OnPartDetached);
            hoverEntered.AddListener(OnHoverEnterHaptics);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            selectEntered.RemoveListener(OnPartAttached);
            selectExited.RemoveListener(OnPartDetached);
            hoverEntered.RemoveListener(OnHoverEnterHaptics);
        }

        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            if (!base.CanSelect(interactable)) return false;
            if (expectedPart == null) return false;

            var part = interactable.transform.GetComponent<WeaponPart>();
            if (part == null || part != expectedPart) return false;

            // Dependency check (enforced only in RealisticArmorer mode)
            if (weaponBase != null && weaponBase.assemblyMode == AssemblyMode.RealisticArmorer)
            {
                foreach (var socket in requiredSockets)
                {
                    if (socket == null) continue;
                    if (!socket.hasSelection) return false;
                }
            }

            return true;
        }

        private void OnHoverEnterHaptics(HoverEnterEventArgs args)
        {
            if (!enableHaptics) return;

            // Send subtle hover feedback to the hand holding the part entering this socket
            if (args.interactableObject is XRGrabInteractable grab)
            {
                WeaponHaptics.SendImpulseToHolders(grab, hoverHapticAmplitude, hoverHapticDuration);
            }
        }

        private void OnPartAttached(SelectEnterEventArgs args)
        {
            var part = args.interactableObject.transform.GetComponent<WeaponPart>();

            if (part != null && weaponBase != null)
                weaponBase.RegisterPart(part);

            // Set kinematic to avoid multi-body physics jitter on attached weapon
            if (part != null && part.TryGetComponent<Rigidbody>(out var partRb))
            {
                partRb.isKinematic = true;
                partRb.linearVelocity = Vector3.zero;
                partRb.angularVelocity = Vector3.zero;
            }

            // Snap transform perfectly
            var t = args.interactableObject.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;

            if (ghostMesh != null)
                ghostMesh.SetActive(false);

            // Audio & Haptics
            if (snapSound != null)
                AudioSource.PlayClipAtPoint(snapSound, transform.position);
            else
                WeaponSoundManager.PlayAtPoint(WeaponSoundManager.SnapClip, transform.position, 0.7f);

            if (enableHaptics && args.interactableObject is XRGrabInteractable grab)
            {
                WeaponHaptics.SendImpulseToHolders(grab, snapHapticAmplitude, snapHapticDuration);
            }

            // If it's a locked magazine, lock grabbing so it requires release button to eject
            if (isMagazineSocket && lockMagazineWhenAttached && part != null)
            {
                if (part.TryGetComponent<XRGrabInteractable>(out var magGrab))
                {
                    _savedMagLayers = magGrab.interactionLayers;
                    magGrab.interactionLayers = 0;
                }
            }
        }

        private void OnPartDetached(SelectExitEventArgs args)
        {
            var part = args.interactableObject.transform.GetComponent<WeaponPart>();

            if (part != null && weaponBase != null)
                weaponBase.UnregisterPart(part);

            // Restore dynamic physics on detach
            if (part != null && part.TryGetComponent<Rigidbody>(out var partRb))
            {
                partRb.isKinematic = false;
            }

            if (ghostMesh != null)
                ghostMesh.SetActive(true);

            // Re-enable grab if it was locked
            if (part != null && part.TryGetComponent<XRGrabInteractable>(out var grab))
            {
                if (grab.interactionLayers.value == 0)
                {
                    grab.interactionLayers = _savedMagLayers.value != 0 ? _savedMagLayers : (InteractionLayerMask)~0;
                }
            }

            if (enableHaptics && args.interactorObject != null)
            {
                WeaponHaptics.SendImpulse(args.interactorObject, detachHapticAmplitude, detachHapticDuration);
            }
        }

        /// <summary>
        /// Ejects or drops the part from this socket (specifically designed for magazine release).
        /// </summary>
        public void EjectMagazine()
        {
            if (!hasSelection || interactablesSelected.Count == 0) return;

            var selected = interactablesSelected[0];
            if (selected == null) return;

            // Re-enable grab on interactable
            if (selected.transform.TryGetComponent<XRGrabInteractable>(out var grab))
            {
                grab.interactionLayers = _savedMagLayers.value != 0 ? _savedMagLayers : (InteractionLayerMask)~0;
            }

            // Drop from socket
            interactionManager.SelectExit(this, selected);

            // Downward ejection impulse
            if (selected.transform.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = false;
                rb.linearVelocity = -transform.up * ejectDownwardForce;
            }

            // Audio & Haptics
            if (ejectSound != null)
                AudioSource.PlayClipAtPoint(ejectSound, transform.position);
            else
                WeaponSoundManager.PlayAtPoint(WeaponSoundManager.MagEjectClip, transform.position, 0.8f);

            if (enableHaptics && weaponBase != null)
            {
                // Send haptic impulse to whoever is holding the weapon
                if (weaponBase.TryGetComponent<XRGrabInteractable>(out var weaponGrab))
                {
                    WeaponHaptics.SendImpulseToHolders(weaponGrab, snapHapticAmplitude * 0.7f, snapHapticDuration);
                }
            }

            Debug.Log($"[WeaponSocket] Ejected magazine '{selected.transform.name}' from {weaponBase?.weaponName}");
        }
    }
}

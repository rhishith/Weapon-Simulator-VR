using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRWeaponSimulator
{
    public class WeaponBase : MonoBehaviour
    {
        [Header("Info")]
        public string weaponName;

        [Header("State")]
        public List<WeaponPart> attachedParts = new();
        public List<WeaponPart> allParts = new();

        public bool isFullyAssembled;
        public bool isChambered;

        [Header("Magazine Release Input")]
        [Tooltip("Optional Input Action to eject magazine (e.g. Primary or Secondary button on holding hand)")]
        public InputActionProperty ejectMagAction;

        [Header("Sockets")]
        public WeaponSocket magazineSocket;
        public WeaponSlide weaponSlide;

        [Header("Events")]
        public UnityEvent onWeaponFullyAssembled;
        public UnityEvent onWeaponIncomplete;
        public UnityEvent onWeaponChambered;
        public UnityEvent onMagazineEjected;

        private const string PartsRootName = "Parts";
        private const string SocketsRootName = "Sockets";
        private XRGrabInteractable _grab;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            AutoCollectParts();
            AutoFindSpecialComponents();
        }

        private void Start()
        {
            UpdateAssemblyState();
        }

        private void OnEnable()
        {
            if (ejectMagAction.action != null)
            {
                ejectMagAction.action.Enable();
                ejectMagAction.action.performed += OnEjectActionPerformed;
            }
        }

        private void OnDisable()
        {
            if (ejectMagAction.action != null)
            {
                ejectMagAction.action.performed -= OnEjectActionPerformed;
                ejectMagAction.action.Disable();
            }
        }

        private void Update()
        {
            // Fallback input check for VR controllers or keyboard testing (E or Gamepad East/South)
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                EjectMagazine();
            }
        }

        private void OnEjectActionPerformed(InputAction.CallbackContext context)
        {
            EjectMagazine();
        }

        public void AutoFindSpecialComponents()
        {
            // Find magazine socket
            if (magazineSocket == null)
            {
                Transform socketsRoot = transform.Find(SocketsRootName);
                if (socketsRoot != null)
                {
                    foreach (var s in socketsRoot.GetComponentsInChildren<WeaponSocket>())
                    {
                        if (s.isMagazineSocket || s.name.Contains("Mag"))
                        {
                            magazineSocket = s;
                            break;
                        }
                    }
                }
            }

            // Find slide
            if (weaponSlide == null)
            {
                weaponSlide = GetComponentInChildren<WeaponSlide>(true);
            }
        }

        public void AutoCollectParts()
        {
            Transform partsRoot = transform.Find(PartsRootName);

            if (partsRoot == null)
            {
                Debug.LogError($"[WeaponBase] Parts root not found on {name}!", this);
                return;
            }

            allParts.Clear();
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

        public void UpdateAssemblyState()
        {
            bool wasFull = isFullyAssembled;

            isFullyAssembled = allParts.Count > 0 && attachedParts.Count >= allParts.Count;

            if (!wasFull && isFullyAssembled)
            {
                onWeaponFullyAssembled?.Invoke();
                Debug.Log($"[WeaponBase] {weaponName} Fully Assembled ({attachedParts.Count}/{allParts.Count})");
            }
            else if (wasFull && !isFullyAssembled)
            {
                onWeaponIncomplete?.Invoke();
                Debug.Log($"[WeaponBase] {weaponName} Incomplete ({attachedParts.Count}/{allParts.Count})");
            }
        }

        /// <summary>
        /// Ejects the magazine from the weapon socket.
        /// </summary>
        public void EjectMagazine()
        {
            if (magazineSocket == null)
                AutoFindSpecialComponents();

            if (magazineSocket != null && magazineSocket.hasSelection)
            {
                magazineSocket.EjectMagazine();
                onMagazineEjected?.Invoke();
            }
            else
            {
                Debug.Log($"[WeaponBase] No magazine to eject in {weaponName}.");
            }
        }

        /// <summary>
        /// Chambers a round (called by WeaponSlide when racked back and released forward).
        /// </summary>
        public void ChamberRound()
        {
            isChambered = true;
            onWeaponChambered?.Invoke();
            Debug.Log($"[WeaponBase] {weaponName} Chambered!");
        }
    }
}

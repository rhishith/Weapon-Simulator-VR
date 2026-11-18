using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRWeaponSimulator;

namespace VRWeaponSimulator.UI
{
    /// <summary>
    /// 3D World-Space HUD for the armorer workbench.
    /// Tracks weapon assembly progress, inspects held parts, and provides quick bench actions.
    /// </summary>
    public class WorkbenchHUD : MonoBehaviour
    {
        [Header("Target Weapon")]
        public WeaponBase targetWeapon;

        [Header("UI Text Displays (Optional)")]
        public TMP_Text weaponNameText;
        public TMP_Text progressStatusText;
        public TMP_Text partInspectorText;
        public Image progressBar;

        [Header("Colors")]
        public Color incompleteColor = new Color(1f, 0.6f, 0.1f);
        public Color assembledColor = new Color(0.2f, 0.9f, 0.4f);
        public Color chamberedColor = new Color(0.3f, 0.7f, 1f);

        private WeaponPart _currentlyInspectedPart;

        private void Start()
        {
            if (targetWeapon == null)
                targetWeapon = FindFirstObjectByType<WeaponBase>();

            RegisterWeaponEvents();
            UpdateDisplay();
        }

        private void OnEnable()
        {
            RegisterWeaponEvents();
        }

        private void OnDisable()
        {
            UnregisterWeaponEvents();
        }

        private void RegisterWeaponEvents()
        {
            if (targetWeapon == null) return;

            targetWeapon.onWeaponFullyAssembled.AddListener(UpdateDisplay);
            targetWeapon.onWeaponIncomplete.AddListener(UpdateDisplay);
            targetWeapon.onWeaponChambered.AddListener(UpdateDisplay);
            targetWeapon.onMagazineEjected.AddListener(UpdateDisplay);
        }

        private void UnregisterWeaponEvents()
        {
            if (targetWeapon == null) return;

            targetWeapon.onWeaponFullyAssembled.RemoveListener(UpdateDisplay);
            targetWeapon.onWeaponIncomplete.RemoveListener(UpdateDisplay);
            targetWeapon.onWeaponChambered.RemoveListener(UpdateDisplay);
            targetWeapon.onMagazineEjected.RemoveListener(UpdateDisplay);
        }

        private void Update()
        {
            // Continuously check what part the player's hands are holding for inspection
            CheckHeldPartInspection();
        }

        private void CheckHeldPartInspection()
        {
            if (targetWeapon == null) return;

            WeaponPart heldPart = null;
            foreach (var part in targetWeapon.allParts)
            {
                if (part != null && part.GrabInteractable != null)
                {
                    bool isHeldByHand = false;
                    foreach (var interactor in part.GrabInteractable.interactorsSelecting)
                    {
                        if (interactor is not UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor)
                        {
                            isHeldByHand = true;
                            break;
                        }
                    }

                    if (isHeldByHand)
                    {
                        heldPart = part;
                        break;
                    }
                }
            }

            if (heldPart != _currentlyInspectedPart)
            {
                _currentlyInspectedPart = heldPart;
                UpdateInspectorDisplay();
            }
        }

        public void UpdateDisplay()
        {
            if (targetWeapon == null) return;

            int total = targetWeapon.allParts.Count;
            int attached = targetWeapon.attachedParts.Count;
            float ratio = total > 0 ? (float)attached / total : 0f;

            if (weaponNameText != null)
                weaponNameText.text = targetWeapon.weaponName;

            if (progressStatusText != null)
            {
                if (targetWeapon.isChambered)
                {
                    progressStatusText.text = $"READY TO FIRE (Chambered)\n{attached}/{total} Parts (100%)";
                    progressStatusText.color = chamberedColor;
                }
                else if (targetWeapon.isFullyAssembled)
                {
                    progressStatusText.text = $"FULLY ASSEMBLED\n{attached}/{total} Parts (100%)";
                    progressStatusText.color = assembledColor;
                }
                else
                {
                    progressStatusText.text = $"ASSEMBLY IN PROGRESS\n{attached}/{total} Parts ({Mathf.RoundToInt(ratio * 100)}%)";
                    progressStatusText.color = incompleteColor;
                }
            }

            if (progressBar != null)
            {
                progressBar.fillAmount = ratio;
                progressBar.color = targetWeapon.isFullyAssembled ? assembledColor : incompleteColor;
            }
        }

        private void UpdateInspectorDisplay()
        {
            if (partInspectorText == null) return;

            if (_currentlyInspectedPart == null)
            {
                partInspectorText.text = "<b>Part Inspector</b>\n<i>Grab any weapon component to inspect details...</i>";
                return;
            }

            var data = _currentlyInspectedPart.data;
            if (data != null)
            {
                partInspectorText.text = $"<b>{data.partName}</b>\n" +
                                         $"Type: <color=#66CCFF>{data.type}</color>  |  Weight: <color=#FFCC00>{data.weight:F2} kg</color>\n" +
                                         $"<i>{(!string.IsNullOrEmpty(data.description) ? data.description : "Standard modular component")}</i>";
            }
            else
            {
                partInspectorText.text = $"<b>{_currentlyInspectedPart.name}</b>\n<i>No metadata attached</i>";
            }
        }

        // ==============================================================
        // BENCH QUICK ACTIONS
        // ==============================================================

        /// <summary>
        /// Field strips (disassembles) all components from the target weapon onto the bench.
        /// </summary>
        public void FieldStripWeapon()
        {
            if (targetWeapon == null) return;

            Transform socketsRoot = targetWeapon.transform.Find("Sockets");
            if (socketsRoot == null) return;

            foreach (var socket in socketsRoot.GetComponentsInChildren<WeaponSocket>())
            {
                if (socket.hasSelection && socket.firstInteractableSelected != null)
                {
                    var part = socket.firstInteractableSelected;
                    socket.interactionManager.SelectExit((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)socket, (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)part);

                    // Scatter slightly on table
                    if (part.transform.TryGetComponent<Rigidbody>(out var rb))
                    {
                        rb.isKinematic = false;
                        rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 0.5f) * 0.3f;
                    }
                }
            }

            targetWeapon.isChambered = false;
            UpdateDisplay();
            Debug.Log($"[WorkbenchHUD] Field stripped {targetWeapon.weaponName}");
        }

        /// <summary>
        /// Snaps all matching parts back into their sockets, fully assembling the weapon.
        /// </summary>
        public void AssembleAllParts()
        {
            if (targetWeapon == null) return;

            Transform socketsRoot = targetWeapon.transform.Find("Sockets");
            if (socketsRoot == null) return;

            foreach (var socket in socketsRoot.GetComponentsInChildren<WeaponSocket>())
            {
                if (!socket.hasSelection && socket.expectedPart != null)
                {
                    var grab = socket.expectedPart.GrabInteractable;
                    if (grab != null)
                    {
                        socket.interactionManager.SelectEnter((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)socket, (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
                    }
                }
            }

            UpdateDisplay();
            Debug.Log($"[WorkbenchHUD] Fully assembled {targetWeapon.weaponName}");
        }

        /// <summary>
        /// Drops the magazine from the weapon.
        /// </summary>
        public void EjectMagazine()
        {
            if (targetWeapon != null)
                targetWeapon.EjectMagazine();
        }
    }
}

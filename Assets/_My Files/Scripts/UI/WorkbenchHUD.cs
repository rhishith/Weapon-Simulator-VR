using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRWeaponSimulator;

namespace VRWeaponSimulator.UI
{
    public enum GhostDisplayMode
    {
        Proximity,
        AlwaysOn,
        Disabled
    }

    /// <summary>
    /// Interactive 3D Armorer Terminal HUD for the VR Workbench.
    /// Manages weapon spawning, live telemetry, assembly progress, and tactile workbench actions.
    /// </summary>
    public class WorkbenchHUD : MonoBehaviour
    {
        [Header("Target Weapon")]
        public WeaponBase targetWeapon;
        public Vector3 weaponSpawnPosition = new Vector3(0.0f, 1.16f, 0.80f);
        public Vector3 weaponSpawnEulerAngles = new Vector3(0f, 90f, 0f);

        [Header("Weapon Catalog")]
        public List<GameObject> availableWeaponPrefabs = new List<GameObject>();
        public int currentWeaponIndex = 0;

        [Header("Header UI")]
        public TMP_Text weaponNameText;
        public TMP_Text weaponCategoryText;
        public TMP_Text statusBadgeText;
        public Image statusBadgeBackground;
        public TMP_Text progressCountText;
        public Image progressBar;

        [Header("Component Telemetry Card")]
        public TMP_Text telemetryTitleText;
        public TMP_Text telemetryTypeBadgeText;
        public TMP_Text telemetryWeightText;
        public TMP_Text telemetryStatusText;
        public TMP_Text telemetryDescriptionText;

        [Header("Control Buttons Text")]
        public TMP_Text modeButtonText;
        public TMP_Text ghostButtonText;

        [Header("Weapon Tab Buttons")]
        public List<Button> weaponTabButtons = new List<Button>();

        [Header("Colors")]
        public Color incompleteColor = new Color(0.98f, 0.65f, 0.12f); // Amber
        public Color assembledColor = new Color(0.12f, 0.85f, 0.55f);  // Emerald Green
        public Color chamberedColor = new Color(0.15f, 0.75f, 1.0f);   // Neon Cyan
        public Color activeTabColor = new Color(0.12f, 0.85f, 0.55f);
        public Color inactiveTabColor = new Color(0.18f, 0.22f, 0.28f);

        private WeaponPart _currentlyInspectedPart;
        private GhostDisplayMode _currentGhostMode = GhostDisplayMode.Proximity;

        private void Start()
        {
            if (targetWeapon == null)
                targetWeapon = FindFirstObjectByType<WeaponBase>();

            RegisterWeaponEvents();
            UpdateDisplay();
            UpdateWeaponTabsVisual();
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
                UpdateTelemetryDisplay();
            }
        }

        public void UpdateDisplay()
        {
            if (targetWeapon == null)
            {
                if (weaponNameText != null) weaponNameText.text = "NO WEAPON SELECTED";
                if (progressCountText != null) progressCountText.text = "0 / 0 Parts";
                if (progressBar != null) progressBar.fillAmount = 0f;
                return;
            }

            int total = targetWeapon.allParts.Count;
            int attached = targetWeapon.attachedParts.Count;
            float ratio = total > 0 ? (float)attached / total : 0f;

            if (weaponNameText != null)
                weaponNameText.text = GetFormattedWeaponName(targetWeapon.weaponName);

            if (weaponCategoryText != null)
                weaponCategoryText.text = GetWeaponCategory(targetWeapon.weaponName);

            // Status Badge
            if (statusBadgeText != null)
            {
                if (targetWeapon.isChambered)
                {
                    statusBadgeText.text = "[ CHAMBERED / READY ]";
                    if (statusBadgeBackground != null) statusBadgeBackground.color = chamberedColor;
                }
                else if (targetWeapon.isFullyAssembled)
                {
                    statusBadgeText.text = "[ FULLY ASSEMBLED ]";
                    if (statusBadgeBackground != null) statusBadgeBackground.color = assembledColor;
                }
                else
                {
                    statusBadgeText.text = "[ IN PROGRESS ]";
                    if (statusBadgeBackground != null) statusBadgeBackground.color = incompleteColor;
                }
            }

            if (progressCountText != null)
            {
                progressCountText.text = $"{attached} / {total} Parts ({Mathf.RoundToInt(ratio * 100)}%)";
            }

            if (progressBar != null)
            {
                progressBar.fillAmount = ratio;
                progressBar.color = targetWeapon.isFullyAssembled ? assembledColor : incompleteColor;
            }

            // Mode & Ghost button texts
            if (modeButtonText != null)
            {
                bool isRealistic = targetWeapon.assemblyMode == AssemblyMode.RealisticArmorer;
                modeButtonText.text = isRealistic ? "MODE: REALISTIC" : "MODE: FREE SANDBOX";
            }

            if (ghostButtonText != null)
            {
                switch (_currentGhostMode)
                {
                    case GhostDisplayMode.Proximity:
                        ghostButtonText.text = "GHOSTS: PROXIMITY";
                        break;
                    case GhostDisplayMode.AlwaysOn:
                        ghostButtonText.text = "GHOSTS: ALWAYS ON";
                        break;
                    case GhostDisplayMode.Disabled:
                        ghostButtonText.text = "GHOSTS: OFF";
                        break;
                }
            }
        }

        private void UpdateTelemetryDisplay()
        {
            if (telemetryTitleText == null) return;

            if (_currentlyInspectedPart == null)
            {
                telemetryTitleText.text = "COMPONENT TELEMETRY";
                if (telemetryTypeBadgeText != null) telemetryTypeBadgeText.text = "[STANDBY]";
                if (telemetryWeightText != null) telemetryWeightText.text = "Mass: --";
                if (telemetryStatusText != null) telemetryStatusText.text = "Status: Idle";
                if (telemetryDescriptionText != null)
                    telemetryDescriptionText.text = "<i>Grab or point at any weapon component to inspect armorer specifications and technical details...</i>";
                return;
            }

            var data = _currentlyInspectedPart.data;
            telemetryTitleText.text = data != null ? data.partName : _currentlyInspectedPart.name;

            if (telemetryTypeBadgeText != null)
            {
                telemetryTypeBadgeText.text = data != null ? $"[ {data.type.ToString().ToUpper()} ]" : "[ COMPONENT ]";
            }

            if (telemetryWeightText != null)
            {
                telemetryWeightText.text = data != null ? $"Mass: <b>{data.weight:F2} kg</b>" : "Mass: 1.00 kg";
            }

            if (telemetryStatusText != null)
            {
                telemetryStatusText.text = "Status: <color=#00E5FF>Held in VR Hand</color>";
            }

            if (telemetryDescriptionText != null)
            {
                string desc = (data != null && !string.IsNullOrEmpty(data.description))
                    ? data.description
                    : GetDefaultPartDescription(telemetryTitleText.text);
                telemetryDescriptionText.text = desc;
            }
        }

        private string GetDefaultPartDescription(string partName)
        {
            if (partName.Contains("Receiver_Down")) return "Precision lower receiver chassis with fire control group and magazine well.";
            if (partName.Contains("Receiver_Up")) return "Upper receiver frame with flattop Picatinny optic rail.";
            if (partName.Contains("Receiver")) return "Main modular receiver frame housing internal firing mechanisms.";
            if (partName.Contains("Barrel")) return "Cold hammer-forged rifled barrel for ballistic velocity and stabilization.";
            if (partName.Contains("Bolt") || partName.Contains("Slide")) return "Smooth-cycling bolt assembly with breach lock and extractor.";
            if (partName.Contains("Mag")) return "Detachable box magazine with high-tension feed spring.";
            if (partName.Contains("Stock")) return "Ergonomic adjustable buttstock for shoulder stabilization and recoil control.";
            if (partName.Contains("Sight") || partName.Contains("Optic")) return "High-visibility iron sight targeting aperture.";
            if (partName.Contains("Grip") || partName.Contains("Guard")) return "Textured forward handguard offering two-handed grip stabilization.";
            if (partName.Contains("Muzzle") || partName.Contains("Brake")) return "Tactical muzzle brake reducing muzzle climb and recoil impulse.";
            if (partName.Contains("Trigger")) return "Match-grade crisp trigger assembly with safety catch.";
            return "Modular MIL-STD component designed for rapid armorer disassembly.";
        }

        private string GetFormattedWeaponName(string rawName)
        {
            if (rawName.Contains("AR_B")) return "AR-15 CQB CARBINE (AR_B)";
            if (rawName.Contains("AR_C")) return "AR-15 MODULAR RIFLE (AR_C)";
            if (rawName.Contains("Pistol_G")) return "TACTICAL 9MM PISTOL (Pistol_G)";
            if (rawName.Contains("Pistol_K")) return "COMPACT COMBAT PISTOL (Pistol_K)";
            if (rawName.Contains("Recon_P")) return "RECON-P PRECISION SNIPER";
            if (rawName.Contains("ShotGun_C")) return "COMBAT PUMP SHOTGUN (ShotGun_C)";
            if (rawName.Contains("ShotGun_E")) return "ENFORCER TACTICAL SHOTGUN (ShotGun_E)";
            if (rawName.Contains("SMG_F")) return "SUBMACHINE GUN MK-F (SMG_F)";
            if (rawName.Contains("SMG_N")) return "COMPACT PDW SUBMACHINE (SMG_N)";
            return rawName;
        }

        private string GetWeaponCategory(string rawName)
        {
            if (rawName.Contains("AR_")) return "ASSAULT RIFLE  |  5.56x45mm NATO  |  SEMI / AUTO";
            if (rawName.Contains("Pistol_")) return "SIDEARM HANDGUN  |  9x19mm PARABELLUM  |  SEMI";
            if (rawName.Contains("Recon_")) return "SNIPER PLATFORM  |  7.62x51mm NATO  |  BOLT ACTION";
            if (rawName.Contains("ShotGun_")) return "SHOTGUN PLATFORM  |  12 GAUGE BUCKSHOT  |  PUMP ACTION";
            if (rawName.Contains("SMG_")) return "SUBMACHINE GUN  |  9x19mm  |  HIGH CYCLIC RATE";
            return "MODULAR FIREARM PLATFORM";
        }

        // ==============================================================
        // WEAPON SELECTION & SPAWNING
        // ==============================================================

        public void SelectWeapon(int index)
        {
            if (availableWeaponPrefabs == null || index < 0 || index >= availableWeaponPrefabs.Count) return;

            currentWeaponIndex = index;
            GameObject prefab = availableWeaponPrefabs[index];
            if (prefab == null) return;

            UnregisterWeaponEvents();

            // Destroy current weapon on table safely
            if (targetWeapon != null)
            {
                Destroy(targetWeapon.gameObject);
                targetWeapon = null;
            }

            // Spawn new weapon
            GameObject newInstance = Instantiate(prefab, weaponSpawnPosition, Quaternion.Euler(weaponSpawnEulerAngles));
            newInstance.name = prefab.name;

            targetWeapon = newInstance.GetComponent<WeaponBase>();
            if (targetWeapon == null)
                targetWeapon = newInstance.AddComponent<WeaponBase>();

            RegisterWeaponEvents();
            ApplyGhostMode();
            UpdateDisplay();
            UpdateWeaponTabsVisual();

            StartCoroutine(AssembleOnSpawnRoutine());

            WeaponSoundManager.PlayAtPoint(WeaponSoundManager.SnapClip, weaponSpawnPosition, 0.9f);
            Debug.Log($"[WorkbenchHUD] Switched to weapon: {prefab.name}");
        }

        private System.Collections.IEnumerator AssembleOnSpawnRoutine()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            AssembleAllParts();
        }

        public void SelectWeaponByName(string prefabName)
        {
            for (int i = 0; i < availableWeaponPrefabs.Count; i++)
            {
                if (availableWeaponPrefabs[i] != null && availableWeaponPrefabs[i].name.Equals(prefabName, System.StringComparison.OrdinalIgnoreCase))
                {
                    SelectWeapon(i);
                    return;
                }
            }
        }

        private void UpdateWeaponTabsVisual()
        {
            for (int i = 0; i < weaponTabButtons.Count; i++)
            {
                if (weaponTabButtons[i] == null) continue;

                var img = weaponTabButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    img.color = (i == currentWeaponIndex) ? activeTabColor : inactiveTabColor;
                }
            }
        }

        // ==============================================================
        // BENCH QUICK ACTIONS
        // ==============================================================

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

                    if (part.transform.TryGetComponent<Rigidbody>(out var rb))
                    {
                        rb.isKinematic = false;
                        rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 0.5f) * 0.3f;
                    }
                }
            }

            targetWeapon.isChambered = false;
            UpdateDisplay();
            WeaponSoundManager.PlayAtPoint(WeaponSoundManager.MagEjectClip, transform.position, 0.8f);
            Debug.Log($"[WorkbenchHUD] Field stripped {targetWeapon.weaponName}");
        }

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
            WeaponSoundManager.PlayAtPoint(WeaponSoundManager.SnapClip, transform.position, 1.0f);
            Debug.Log($"[WorkbenchHUD] Fully assembled {targetWeapon.weaponName}");
        }

        public void EjectMagazine()
        {
            if (targetWeapon != null)
                targetWeapon.EjectMagazine();
        }

        public void CycleBoltAction()
        {
            if (targetWeapon == null) return;

            if (targetWeapon.weaponSlide != null)
            {
                if (targetWeapon.weaponSlide.isLockedBack)
                {
                    targetWeapon.weaponSlide.ReleaseBolt();
                }
                else
                {
                    targetWeapon.weaponSlide.LockBoltOpen();
                }
            }
            else
            {
                targetWeapon.ChamberRound();
            }

            UpdateDisplay();
        }

        public void ToggleAssemblyMode()
        {
            if (targetWeapon == null) return;

            targetWeapon.assemblyMode = (targetWeapon.assemblyMode == AssemblyMode.RealisticArmorer)
                ? AssemblyMode.FreeSandbox
                : AssemblyMode.RealisticArmorer;

            UpdateDisplay();
            WeaponSoundManager.PlayAtPoint(WeaponSoundManager.SnapClip, transform.position, 0.7f);
            Debug.Log($"[WorkbenchHUD] Switched mode to: {targetWeapon.assemblyMode}");
        }

        public void CycleGhostMode()
        {
            _currentGhostMode = (GhostDisplayMode)(((int)_currentGhostMode + 1) % 3);
            ApplyGhostMode();
            UpdateDisplay();
            WeaponSoundManager.PlayAtPoint(WeaponSoundManager.SnapClip, transform.position, 0.7f);
        }

        private void ApplyGhostMode()
        {
            if (targetWeapon == null) return;

            var ghostCtrl = targetWeapon.GetComponentInChildren<WeaponGhostVisualController>(true);
            Transform ghostRoot = targetWeapon.transform.Find("Ghost Visuals");

            switch (_currentGhostMode)
            {
                case GhostDisplayMode.Proximity:
                    if (ghostRoot != null) ghostRoot.gameObject.SetActive(true);
                    if (ghostCtrl != null)
                    {
                        ghostCtrl.useProximityFading = true;
                        ghostCtrl.showAllGhostsWhenIncomplete = false;
                    }
                    break;

                case GhostDisplayMode.AlwaysOn:
                    if (ghostRoot != null) ghostRoot.gameObject.SetActive(true);
                    if (ghostCtrl != null)
                    {
                        ghostCtrl.useProximityFading = false;
                        ghostCtrl.showAllGhostsWhenIncomplete = true;
                    }
                    break;

                case GhostDisplayMode.Disabled:
                    if (ghostRoot != null) ghostRoot.gameObject.SetActive(false);
                    if (ghostCtrl != null)
                    {
                        ghostCtrl.useProximityFading = false;
                        ghostCtrl.showAllGhostsWhenIncomplete = false;
                    }
                    break;
            }
        }
    }
}

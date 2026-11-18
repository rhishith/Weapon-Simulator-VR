using System.Collections.Generic;
using UnityEngine;

namespace VRWeaponSimulator
{
    /// <summary>
    /// Manages ghost visuals for weapon assembly.
    /// Supports proximity-based fading so ghost hints only appear when the player
    /// is actively holding the matching part near its target socket.
    /// </summary>
    public class WeaponGhostVisualController : MonoBehaviour
    {
        [Header("References")]
        public WeaponBase weaponBase;
        public GameObject ghostRoot;

        [Header("Proximity Settings")]
        [Tooltip("If true, detached ghosts only illuminate when the player holds the matching part near the weapon.")]
        public bool useProximityFading = true;

        [Tooltip("Distance in meters within which the ghost will appear when holding the matching part.")]
        public float proximityDistance = 0.5f;

        [Header("Visual Options")]
        [Tooltip("If true, all ghosts show continuously when weapon is incomplete.")]
        public bool showAllGhostsWhenIncomplete = false;

        private float _checkTimer = 0f;
        private const float CheckInterval = 0.05f; // 20Hz update is plenty for distance checks

        private void Awake()
        {
            if (ghostRoot == null)
                ghostRoot = gameObject;

            if (weaponBase == null)
                weaponBase = GetComponentInParent<WeaponBase>();
        }

        private void OnEnable()
        {
            if (weaponBase == null) return;

            weaponBase.onWeaponFullyAssembled.AddListener(OnFullyAssembled);
            weaponBase.onWeaponIncomplete.AddListener(OnIncomplete);

            RefreshVisibility();
        }

        private void OnDisable()
        {
            if (weaponBase == null) return;

            weaponBase.onWeaponFullyAssembled.RemoveListener(OnFullyAssembled);
            weaponBase.onWeaponIncomplete.RemoveListener(OnIncomplete);
        }

        private void Update()
        {
            if (!useProximityFading || showAllGhostsWhenIncomplete || weaponBase == null || weaponBase.isFullyAssembled)
                return;

            _checkTimer += Time.deltaTime;
            if (_checkTimer < CheckInterval) return;
            _checkTimer = 0f;

            UpdateProximityGhosts();
        }

        private void UpdateProximityGhosts()
        {
            Transform socketsRoot = weaponBase.transform.Find("Sockets");
            Transform ghostRootT = ghostRoot.transform;
            if (socketsRoot == null || ghostRootT == null) return;

            foreach (var socket in socketsRoot.GetComponentsInChildren<WeaponSocket>())
            {
                if (socket == null) continue;

                Transform ghostPart = ghostRootT.Find(socket.name);
                if (ghostPart == null) continue;

                // If already attached, ghost should always stay hidden
                if (socket.hasSelection)
                {
                    if (ghostPart.gameObject.activeSelf)
                        ghostPart.gameObject.SetActive(false);
                    continue;
                }

                // If part is detached, check if held by player near this socket
                bool shouldShow = false;
                if (socket.expectedPart != null && socket.expectedPart.GrabInteractable != null)
                {
                    bool isGrabbedByHand = false;
                    foreach (var interactor in socket.expectedPart.GrabInteractable.interactorsSelecting)
                    {
                        if (interactor is not UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor)
                        {
                            isGrabbedByHand = true;
                            break;
                        }
                    }

                    if (isGrabbedByHand)
                    {
                        float dist = Vector3.Distance(socket.expectedPart.transform.position, socket.transform.position);
                        if (dist <= proximityDistance)
                        {
                            shouldShow = true;
                        }
                    }
                }

                if (ghostPart.gameObject.activeSelf != shouldShow)
                {
                    ghostPart.gameObject.SetActive(shouldShow);
                }
            }
        }

        private void OnFullyAssembled()
        {
            if (ghostRoot != null)
                ghostRoot.SetActive(false);
        }

        private void OnIncomplete()
        {
            if (ghostRoot != null)
                ghostRoot.SetActive(true);

            if (showAllGhostsWhenIncomplete)
            {
                // Show all unattached ghosts
                Transform socketsRoot = weaponBase.transform.Find("Sockets");
                if (socketsRoot != null)
                {
                    foreach (var s in socketsRoot.GetComponentsInChildren<WeaponSocket>())
                    {
                        Transform g = ghostRoot.transform.Find(s.name);
                        if (g != null)
                            g.gameObject.SetActive(!s.hasSelection);
                    }
                }
            }
        }

        public void RefreshVisibility()
        {
            if (weaponBase == null) return;

            if (weaponBase.isFullyAssembled)
                OnFullyAssembled();
            else
                OnIncomplete();
        }
    }
}

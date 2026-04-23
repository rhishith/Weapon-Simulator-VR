using UnityEngine;

namespace VRWeaponSimulator
{
    /// <summary>
    /// Attach this to any GameObject (e.g. your "Ghost Visuals" parent under each weapon).
    /// It listens to WeaponBase assembly events and shows/hides the ghost accordingly.
    /// </summary>
    public class WeaponGhostVisualController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The WeaponBase this ghost belongs to.")]
        public WeaponBase weaponBase;

        [Tooltip("The root GameObject of the ghost visuals to show/hide. Defaults to this GameObject if left empty.")]
        public GameObject ghostRoot;

        private void Awake()
        {
            // Default ghostRoot to this GameObject if not assigned
            if (ghostRoot == null)
                ghostRoot = gameObject;
        }

        private void OnEnable()
        {
            if (weaponBase == null)
            {
                Debug.LogWarning($"[WeaponGhostVisualController] No WeaponBase assigned on {gameObject.name}.", this);
                return;
            }

            weaponBase.onWeaponFullyAssembled.AddListener(HideGhost);
            weaponBase.onWeaponIncomplete.AddListener(ShowGhost);

            // Sync state immediately in case the weapon is already assembled on enable
            RefreshVisibility();
        }

        private void OnDisable()
        {
            if (weaponBase == null) return;

            weaponBase.onWeaponFullyAssembled.RemoveListener(HideGhost);
            weaponBase.onWeaponIncomplete.RemoveListener(ShowGhost);
        }

        private void HideGhost()
        {
            if (ghostRoot != null)
                ghostRoot.SetActive(false);
        }

        private void ShowGhost()
        {
            if (ghostRoot != null)
                ghostRoot.SetActive(true);
        }

        /// <summary>
        /// Syncs the ghost visibility to the current assembly state of the weapon.
        /// Useful on scene load or prefab instantiation.
        /// </summary>
        private void RefreshVisibility()
        {
            if (weaponBase.isFullyAssembled)
                HideGhost();
            else
                ShowGhost();
        }
    }
}

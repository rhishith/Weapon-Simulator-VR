using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRWeaponSimulator
{
    public class WeaponUIController : MonoBehaviour
    {
        [Header("References")]
        public WeaponBase targetWeapon;
        
        [Header("UI Elements")]
        public TextMeshProUGUI weaponNameText;
        public Image damageBar;
        public Image accuracyBar;
        public Image recoilBar;
        public TextMeshProUGUI statusText;

        [Header("Styling")]
        public Color readyColor = Color.cyan;
        public Color notReadyColor = Color.red;

        private void OnEnable()
        {
            if (targetWeapon != null)
            {
                targetWeapon.onStatsUpdated.AddListener(UpdateUI);
            }
            UpdateUI();
        }

        private void OnDisable()
        {
            if (targetWeapon != null)
            {
                targetWeapon.onStatsUpdated.RemoveListener(UpdateUI);
            }
        }

        public void UpdateUI()
        {
            if (targetWeapon == null) return;

            if (weaponNameText) weaponNameText.text = targetWeapon.weaponName;

            // Simple 0-1 normalization for bars (assuming max values for demo)
            if (damageBar) damageBar.fillAmount = targetWeapon.currentDamage / 50f;
            if (accuracyBar) accuracyBar.fillAmount = targetWeapon.currentAccuracy;
            if (recoilBar) recoilBar.fillAmount = 1f - (targetWeapon.currentRecoil / 2f); // Lower recoil = more bar

            if (statusText)
            {
                if (targetWeapon.isReadyToFire)
                {
                    statusText.text = "SYSTEM READY";
                    statusText.color = readyColor;
                }
                else
                {
                    statusText.text = "INCOMPLETE";
                    statusText.color = notReadyColor;
                }
            }
        }
    }
}

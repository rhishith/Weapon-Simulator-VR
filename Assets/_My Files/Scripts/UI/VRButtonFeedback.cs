using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRWeaponSimulator;

namespace VRWeaponSimulator.UI
{
    /// <summary>
    /// Adds procedural audio and haptic feedback to UI elements when hovered or clicked in VR.
    /// </summary>
    public class VRButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [Header("Haptics")]
        public bool enableHaptics = true;
        [Range(0f, 1f)] public float hoverAmplitude = 0.15f;
        public float hoverDuration = 0.03f;
        [Range(0f, 1f)] public float clickAmplitude = 0.45f;
        public float clickDuration = 0.06f;

        [Header("Audio")]
        public bool enableAudio = true;
        public float hoverVolume = 0.25f;
        public float clickVolume = 0.8f;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (enableAudio)
            {
                WeaponSoundManager.PlayAtPoint(WeaponSoundManager.ButtonHoverClip, transform.position, hoverVolume);
            }

            if (enableHaptics && eventData is TrackedDeviceEventData trackedData)
            {
                if (trackedData.interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor xrInteractor)
                {
                    WeaponHaptics.SendImpulse(xrInteractor, hoverAmplitude, hoverDuration);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (enableAudio)
            {
                WeaponSoundManager.PlayAtPoint(WeaponSoundManager.ButtonClickClip, transform.position, clickVolume);
            }

            if (enableHaptics && eventData is TrackedDeviceEventData trackedData)
            {
                if (trackedData.interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor xrInteractor)
                {
                    WeaponHaptics.SendImpulse(xrInteractor, clickAmplitude, clickDuration);
                }
            }
        }
    }
}

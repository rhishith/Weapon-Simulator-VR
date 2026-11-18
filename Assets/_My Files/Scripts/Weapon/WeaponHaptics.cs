using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRWeaponSimulator
{
    /// <summary>
    /// Utility class to send VR haptic impulses across XR Interaction Toolkit 3.x interactors.
    /// </summary>
    public static class WeaponHaptics
    {
        /// <summary>
        /// Sends a haptic impulse to the specified interactor (if it supports input haptics).
        /// </summary>
        public static void SendImpulse(IXRInteractor interactor, float amplitude, float duration)
        {
            if (interactor is XRBaseInputInteractor inputInteractor)
            {
                inputInteractor.SendHapticImpulse(Mathf.Clamp01(amplitude), Mathf.Max(0.01f, duration));
            }
        }

        /// <summary>
        /// Sends a haptic impulse to any hand/interactor currently grabbing or selecting the interactable.
        /// </summary>
        public static void SendImpulseToHolders(XRGrabInteractable grab, float amplitude, float duration)
        {
            if (grab == null) return;

            foreach (var interactor in grab.interactorsSelecting)
            {
                SendImpulse(interactor, amplitude, duration);
            }
        }
    }
}

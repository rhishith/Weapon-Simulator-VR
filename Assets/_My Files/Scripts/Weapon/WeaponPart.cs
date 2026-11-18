using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRWeaponSimulator
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class WeaponPart : MonoBehaviour
    {
        [Header("Data")]
        public PartData data;

        [Header("Haptics")]
        public bool enableGrabHaptics = true;
        [Range(0f, 1f)] public float grabHapticAmplitude = 0.35f;
        public float grabHapticDuration = 0.06f;

        [Header("Audio (Optional)")]
        public AudioClip grabSound;
        public AudioClip dropSound;

        private XRGrabInteractable _grab;
        public XRGrabInteractable GrabInteractable => _grab;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();

            if (data == null)
            {
                Debug.LogWarning($"[WeaponPart] {name} has no PartData assigned!", this);
            }
        }

        private void OnEnable()
        {
            if (_grab != null)
            {
                _grab.selectEntered.AddListener(OnSelectEntered);
            }
        }

        private void OnDisable()
        {
            if (_grab != null)
            {
                _grab.selectEntered.RemoveListener(OnSelectEntered);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Only trigger grab haptic if grabbed by a hand/controller (not a socket)
            if (args.interactorObject is not UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor)
            {
                if (enableGrabHaptics)
                {
                    WeaponHaptics.SendImpulse(args.interactorObject, grabHapticAmplitude, grabHapticDuration);
                }

                if (grabSound != null)
                {
                    AudioSource.PlayClipAtPoint(grabSound, transform.position);
                }
                else
                {
                    WeaponSoundManager.PlayAtPoint(WeaponSoundManager.GrabClip, transform.position, 0.4f);
                }
            }
        }
    }
}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRWeaponSimulator
{
    [RequireComponent(typeof(WeaponBase))]
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class WeaponFireHandler : MonoBehaviour
    {
        [Header("Firing Specs")]
        public Transform muzzleExit;
        public float fireRate = 0.1f;
        public float range = 100f;
        public LayerMask hitLayers;

        [Header("Effects")]
        public GameObject muzzleFlashPrefab;
        public AudioClip fireSound;
        public float hapticIntensity = 0.5f;
        public float hapticDuration = 0.1f;

        private WeaponBase _weaponBase;
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabInteractable;
        private AudioSource _audioSource;
        private float _nextFireTime;

        private void Awake()
        {
            _weaponBase = GetComponent<WeaponBase>();
            _grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            _grabInteractable.activated.AddListener(OnTriggerPulled);
        }

        private void OnDisable()
        {
            _grabInteractable.activated.RemoveListener(OnTriggerPulled);
        }

        private void OnTriggerPulled(ActivateEventArgs args)
        {
            if (Time.time >= _nextFireTime)
            {
                TryFire(args.interactorObject);
                _nextFireTime = Time.time + fireRate;
            }
        }

        private void TryFire(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
        {
            if (!_weaponBase.isReadyToFire)
            {
                Debug.Log($"[{_weaponBase.weaponName}] Firing failed: Missing required parts!");
                // Play a 'click' sound for empty/fail
                return;
            }

            // Execute Fire
            Debug.Log($"[{_weaponBase.weaponName}] Firing!");
            
            // Raycast
            if (Physics.Raycast(muzzleExit.position, muzzleExit.forward, out RaycastHit hit, range, hitLayers))
            {
                Debug.Log($"Hit: {hit.collider.name}");
                // Apply damage logic here
            }

            // Visuals/Audio
            if (muzzleFlashPrefab) Instantiate(muzzleFlashPrefab, muzzleExit.position, muzzleExit.rotation);
            if (fireSound) _audioSource.PlayOneShot(fireSound);

            // Haptics
            TriggerHaptics(interactor);
        }

        private void TriggerHaptics(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
        {
            if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
            {
                controllerInteractor.xrController.SendHapticImpulse(hapticIntensity, hapticDuration);
            }
        }
    }
}

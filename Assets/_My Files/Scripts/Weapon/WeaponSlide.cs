using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRWeaponSimulator
{
    /// <summary>
    /// Handles bolt / slide racking interaction on firearms.
    /// When attached to its socket, grabbing the slide constrains movement along the rack axis
    /// and snaps back forward on release, triggering chambering and tactile haptics.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(WeaponPart))]
    public class WeaponSlide : MonoBehaviour
    {
        [Header("Weapon References")]
        public WeaponBase weaponBase;
        public WeaponSocket socket;

        [Header("Racking Parameters")]
        [Tooltip("Local axis along which the bolt/slide travels backwards when racked.")]
        public Vector3 localRackAxis = new Vector3(0, 0, -1);

        [Tooltip("Maximum distance in meters the bolt/slide can be pulled back.")]
        public float maxRackDistance = 0.065f;

        [Tooltip("Spring return speed when released.")]
        public float springReturnSpeed = 15f;

        [Tooltip("Threshold fraction of travel (0 to 1) required to chamber a round.")]
        [Range(0.5f, 0.95f)] public float chamberThreshold = 0.85f;

        [Header("Haptics")]
        public bool enableHaptics = true;
        [Range(0f, 1f)] public float rackBackHaptic = 0.75f;
        [Range(0f, 1f)] public float returnHaptic = 0.55f;

        [Header("Audio (Optional)")]
        public AudioClip rackBackSound;
        public AudioClip returnSound;

        [Header("Slide Lock / Bolt Catch")]
        [Tooltip("If true, racking the slide can lock it open in the rear position.")]
        public bool canLockBack = true;
        public bool isLockedBack = false;

        [Header("Events")]
        public UnityEvent onSlideRackedBack;
        public UnityEvent onSlideReturnedForward;
        public UnityEvent onBoltLockedBack;
        public UnityEvent onBoltReleased;

        private XRGrabInteractable _grab;
        private WeaponPart _part;
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private float _currentRackDistance = 0f;
        private bool _wasChamberThresholdReached = false;
        private IXRInteractor _lastHandInteractor;

        public bool IsAttachedToSocket => socket != null && socket.hasSelection && socket.interactablesSelected.Contains(_grab);
        public float CurrentRackDistance => _currentRackDistance;
        public float NormalizedRackDistance => Mathf.Clamp01(_currentRackDistance / maxRackDistance);

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _part = GetComponent<WeaponPart>();
            _initialLocalPosition = Vector3.zero;
            _initialLocalRotation = Quaternion.identity;

            if (weaponBase == null)
                weaponBase = GetComponentInParent<WeaponBase>();
        }

        private void Start()
        {
            ResolveSocket();
        }

        private void ResolveSocket()
        {
            if (socket == null && weaponBase != null)
            {
                Transform socketsRoot = weaponBase.transform.Find("Sockets");
                if (socketsRoot != null)
                {
                    Transform socketT = socketsRoot.Find(name);
                    if (socketT != null)
                        socket = socketT.GetComponent<WeaponSocket>();
                }
            }
        }

        private void OnEnable()
        {
            _grab.selectEntered.AddListener(OnSelectEntered);
            _grab.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            _grab.selectEntered.RemoveListener(OnSelectEntered);
            _grab.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is not XRSocketInteractor)
            {
                _lastHandInteractor = args.interactorObject;
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactorObject == _lastHandInteractor)
            {
                _lastHandInteractor = null;
            }
        }

        private void Update()
        {
            if (socket == null)
            {
                ResolveSocket();
                if (socket == null) return;
            }

            // Only constrain racking while the slide is attached to the weapon
            if (!IsAttachedToSocket)
            {
                _currentRackDistance = 0f;
                return;
            }

            // Check if grabbed by a player hand
            bool isGrabbedByHand = false;
            IXRInteractor handInteractor = null;

            foreach (var interactor in _grab.interactorsSelecting)
            {
                if (interactor is not XRSocketInteractor)
                {
                    isGrabbedByHand = true;
                    handInteractor = interactor;
                    break;
                }
            }

            if (isGrabbedByHand && handInteractor != null)
            {
                // Project hand position along the rack axis
                Vector3 worldAxis = socket.transform.TransformDirection(localRackAxis.normalized);
                Vector3 toHand = handInteractor.transform.position - socket.transform.position;
                float projectedDist = Vector3.Dot(toHand, worldAxis);

                _currentRackDistance = Mathf.Clamp(projectedDist, 0f, maxRackDistance);

                // Check chamber threshold
                if (_currentRackDistance >= maxRackDistance * chamberThreshold && !_wasChamberThresholdReached)
                {
                    _wasChamberThresholdReached = true;
                    onSlideRackedBack?.Invoke();

                    if (rackBackSound != null)
                        AudioSource.PlayClipAtPoint(rackBackSound, transform.position);
                    else
                        WeaponSoundManager.PlayAtPoint(WeaponSoundManager.RackBackClip, transform.position, 0.75f);

                    if (enableHaptics && handInteractor != null)
                        WeaponHaptics.SendImpulse(handInteractor, rackBackHaptic, 0.08f);
                }
            }
            else
            {
                // If locked open in the rear position, hold at max distance until released
                if (isLockedBack)
                {
                    _currentRackDistance = maxRackDistance;
                }
                else if (_currentRackDistance > 0f)
                {
                    // Spring return forward
                    _currentRackDistance = Mathf.MoveTowards(_currentRackDistance, 0f, springReturnSpeed * Time.deltaTime);

                    if (_currentRackDistance <= 0.001f)
                    {
                        _currentRackDistance = 0f;

                        // Return into battery completed
                        if (_wasChamberThresholdReached)
                        {
                            _wasChamberThresholdReached = false;

                            if (weaponBase != null)
                                weaponBase.ChamberRound();

                            onSlideReturnedForward?.Invoke();

                            if (returnSound != null)
                                AudioSource.PlayClipAtPoint(returnSound, transform.position);
                            else
                                WeaponSoundManager.PlayAtPoint(WeaponSoundManager.RackForwardClip, transform.position, 0.85f);

                            if (enableHaptics && _lastHandInteractor != null)
                                WeaponHaptics.SendImpulse(_lastHandInteractor, returnHaptic, 0.06f);

                            // Also send haptic to whoever is holding the main weapon
                            if (enableHaptics && weaponBase != null)
                            {
                                if (weaponBase.TryGetComponent<XRGrabInteractable>(out var gunGrab))
                                    WeaponHaptics.SendImpulseToHolders(gunGrab, returnHaptic * 0.7f, 0.05f);
                            }
                        }
                    }
                }
            }

            // Apply position to transform relative to socket
            Vector3 worldRackOffset = socket.transform.TransformDirection(localRackAxis.normalized) * _currentRackDistance;
            transform.position = socket.transform.position + worldRackOffset;
            transform.rotation = socket.transform.rotation;
        }

        /// <summary>
        /// Locks the bolt open in the rear position.
        /// </summary>
        public void LockBoltOpen()
        {
            if (!canLockBack) return;
            isLockedBack = true;
            _currentRackDistance = maxRackDistance;
            onBoltLockedBack?.Invoke();
            Debug.Log($"[WeaponSlide] Bolt locked open on {weaponBase?.weaponName}");
        }

        /// <summary>
        /// Releases the bolt catch, slamming the slide forward into battery.
        /// </summary>
        public void ReleaseBolt()
        {
            if (!isLockedBack) return;
            isLockedBack = false;
            _wasChamberThresholdReached = true;
            onBoltReleased?.Invoke();

            if (enableHaptics && weaponBase != null)
            {
                if (weaponBase.TryGetComponent<XRGrabInteractable>(out var gunGrab))
                    WeaponHaptics.SendImpulseToHolders(gunGrab, returnHaptic, 0.08f);
            }

            Debug.Log($"[WeaponSlide] Bolt released forward on {weaponBase?.weaponName}");
        }
    }
}

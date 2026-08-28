using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRWeaponSimulator
{
    public class WeaponSocket : UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor
    {
        [Header("Weapon Integration")]
        public WeaponBase weaponBase;
        public PartType acceptedPartType;

        [Header("Visuals")]
        [Tooltip("Auto-resolved by finding a child inside 'Ghost Visuals' that shares this socket's exact name. e.g. Socket 'AR_B_Mag' finds Ghost 'AR_B_Mag'. Manual assignment overrides auto-resolve.")]
        public GameObject ghostMesh;

        private const string GhostVisualsName = "Ghost Visuals";

        protected override void Awake()
        {
            base.Awake();
            AutoResolveGhostMesh();
        }

        private void AutoResolveGhostMesh()
        {
            // Skip if already manually assigned in the Inspector
            if (ghostMesh != null) return;

            if (weaponBase == null)
            {
                Debug.LogWarning($"[WeaponSocket] '{name}': weaponBase is not assigned — cannot auto-resolve ghost mesh.", this);
                return;
            }

            Transform ghostVisualsRoot = weaponBase.transform.Find(GhostVisualsName);
            if (ghostVisualsRoot == null)
            {
                Debug.LogWarning($"[WeaponSocket] '{name}': No child named '{GhostVisualsName}' found under '{weaponBase.name}'.", this);
                return;
            }

            // Socket 'AR_B_Mag' looks for a child named 'AR_B_Mag' inside Ghost Visuals
            Transform found = ghostVisualsRoot.Find(gameObject.name);
            if (found == null)
            {
                Debug.LogWarning($"[WeaponSocket] '{name}': Could not find a matching ghost named '{gameObject.name}' inside '{GhostVisualsName}'. " +
                                 $"Make sure the ghost child has the exact same name as this socket.", this);
                return;
            }

            ghostMesh = found.gameObject;
            Debug.Log($"[WeaponSocket] '{name}': Auto-resolved ghost mesh -> '{ghostMesh.name}' inside '{GhostVisualsName}'.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.AddListener(OnPartAttached);
            selectExited.AddListener(OnPartDetached);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            selectEntered.RemoveListener(OnPartAttached);
            selectExited.RemoveListener(OnPartDetached);
        }

        public override bool CanSelect(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
        {
            if (!base.CanSelect(interactable)) return false;

            var part = interactable.transform.GetComponent<WeaponPart>();
            if (part == null) return false;

            return part.data.type == acceptedPartType;
        }

        private void OnPartAttached(SelectEnterEventArgs args)
        {
            var part = args.interactableObject.transform.GetComponent<WeaponPart>();
            if (part != null && weaponBase != null)
                weaponBase.RegisterPart(part);

            if (ghostMesh != null)
                ghostMesh.SetActive(false);
        }

        private void OnPartDetached(SelectExitEventArgs args)
        {
            var part = args.interactableObject.transform.GetComponent<WeaponPart>();
            if (part != null && weaponBase != null)
                weaponBase.UnregisterPart(part);

            if (ghostMesh != null)
                ghostMesh.SetActive(true);
        }
    }
}
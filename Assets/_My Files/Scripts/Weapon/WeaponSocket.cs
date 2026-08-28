using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRWeaponSimulator
{
    public class WeaponSocket : UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor
    {
        [Header("Weapon")]
        public WeaponBase weaponBase;

        [Header("Dependencies (Optional)")]
        public List<WeaponSocket> requiredSockets = new();

        [Header("Auto References")]
        private WeaponPart expectedPart;
        private GameObject ghostMesh;

        private const string PartsRootName = "Parts";
        private const string GhostRootName = "Ghost Visuals";

        protected override void Awake()
        {
            base.Awake();

if (weaponBase == null)
{
    weaponBase = GetComponentInParent<WeaponBase>();

    if (weaponBase == null)
    {
        Debug.LogError($"[WeaponSocket] {name} could not find WeaponBase in parents!", this);
        return;
    }
}

            AutoResolvePart();
            AutoResolveGhost();
        }

        // 🔥 AUTO FIND PART
        private void AutoResolvePart()
        {
            Transform partsRoot = weaponBase.transform.Find(PartsRootName);

            if (partsRoot == null)
            {
                Debug.LogError($"[WeaponSocket] '{name}' Parts root not found!", this);
                return;
            }

            Transform found = partsRoot.Find(name);

            if (found == null)
            {
                Debug.LogError($"[WeaponSocket] '{name}' matching part not found in Parts!", this);
                return;
            }

            expectedPart = found.GetComponent<WeaponPart>();

            if (expectedPart == null)
            {
                Debug.LogError($"[WeaponSocket] '{name}' has no WeaponPart component!", this);
            }
        }

        // 🔥 AUTO FIND GHOST
        private void AutoResolveGhost()
        {
            Transform ghostRoot = weaponBase.transform.Find(GhostRootName);

            if (ghostRoot == null)
            {
                Debug.LogWarning($"[WeaponSocket] '{name}' Ghost root not found.", this);
                return;
            }

            Transform found = ghostRoot.Find(name);

            if (found != null)
                ghostMesh = found.gameObject;
            else
                Debug.LogWarning($"[WeaponSocket] '{name}' ghost not found.", this);
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

            if (expectedPart == null) return false;

            var part = interactable.transform.GetComponent<WeaponPart>();
            if (part == null) return false;

            // 🔥 EXACT OBJECT MATCH
            if (part != expectedPart)
                return false;

            // 🔥 Dependency check
            foreach (var socket in requiredSockets)
            {
                if (socket == null) continue;
                if (!socket.hasSelection)
                    return false;
            }

            return true;
        }

        private void OnPartAttached(SelectEnterEventArgs args)
        {
            var part = args.interactableObject.transform.GetComponent<WeaponPart>();

            if (part != null && weaponBase != null)
                weaponBase.RegisterPart(part);

            // 🔥 Perfect snap
            var t = args.interactableObject.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;

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
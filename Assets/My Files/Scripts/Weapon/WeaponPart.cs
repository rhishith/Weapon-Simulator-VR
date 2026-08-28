using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRWeaponSimulator
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class WeaponPart : MonoBehaviour
    {
        public PartData data;

        private XRGrabInteractable _grab;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();

            if (data == null)
            {
                Debug.LogError($"[WeaponPart] {name} has no PartData assigned!", this);
            }
        }
    }
}
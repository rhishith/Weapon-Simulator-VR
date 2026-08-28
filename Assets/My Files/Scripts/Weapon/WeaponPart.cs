using UnityEngine;


namespace VRWeaponSimulator
{
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class WeaponPart : MonoBehaviour
    {
        public PartData data;
        
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabInteractable;

        private void Awake()
        {
            _grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        // Potential for future expansion: Haptics when grabbing, visual highlights, etc.
    }
}

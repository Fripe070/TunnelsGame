#region

using UnityEngine;

#endregion

namespace Interactions
{
    public class PlatformSwitch : MonoBehaviour, IInteractive
    {
        public PlatformController toControl;
        public bool buttonPushedDown;
        private GameObject _button;
        
        public string InteractionText => buttonPushedDown ? "Turn off" : "Turn on";

        public void Interact(PlayerController player)
        {
            buttonPushedDown = !buttonPushedDown;
            toControl.isUp = !toControl.isUp;
        }
        
        private void Start()
        {
            _button = transform.GetChild(0).gameObject;
        }

        private void Update()
        {
            // Why the z axis? Because blender!
            _button.transform.localPosition = new Vector3(0, 0, buttonPushedDown ? -0.25f : 0f); 
        }
    }
}
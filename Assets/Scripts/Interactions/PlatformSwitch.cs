#region

using System;
using UnityEngine;

#endregion

namespace Interactions
{
    public class PlatformSwitch : MonoBehaviour, IInteractive
    {
        public PlatformController[] toControl;
        public bool buttonPushedDown;
        private GameObject _button;

        private void Start()
        {
            _button = transform.GetChild(0).gameObject;
        }

        private void Update()
        {
            // Why the z axis? Because blender!
            _button.transform.localPosition = new Vector3(0, 0, buttonPushedDown ? -0.25f : 0f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            foreach (PlatformController platform in toControl) 
                Gizmos.DrawLine(transform.position, platform.transform.position);
        }

        private void OnDrawGizmosSelected()
        {
            // Highlight platform it's bound to
            Gizmos.color = Color.green;
            foreach (PlatformController platform in toControl)
                Gizmos.DrawCube(platform.transform.position, platform.transform.localScale);
        }

        public string InteractionText => buttonPushedDown ? "Turn off" : "Turn on";

        public void Interact(PlayerController player)
        {
            buttonPushedDown = !buttonPushedDown;
            foreach (PlatformController platform in toControl) platform.isUp = !platform.isUp;
        }
    }
}
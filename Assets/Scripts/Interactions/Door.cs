#region Imports

using UnityEngine;

#endregion

namespace Interactions
{
    public class Door : MonoBehaviour, IInteractive
    {
        public GameObject hinge;
        public float openAngle = 90;
        public bool open;
        
        public string InteractionText => open ? "Close" : "Open";

        public void Interact(PlayerController player)
        {
            open = !open;
            transform.RotateAround(hinge.transform.position, hinge.transform.up, open ? openAngle : -openAngle);
        }
    }
}
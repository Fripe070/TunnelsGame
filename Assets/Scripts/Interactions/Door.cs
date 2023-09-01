using UnityEngine;

namespace Interactions
{
    public class Door : MonoBehaviour, IInteractible
    {
        // public GameObject hinge;
        // public float openAngle = 90;
        public bool open = false;
        
        public string InteractionText => open ? "Close" : "Open";

        public void Interact(PlayerController player)
        {
            open = !open;
            enabled = open;
            // transform.RotateAround(hinge.transform.position, Vector3.up, open ? openAngle : -openAngle);
        }
    }
}
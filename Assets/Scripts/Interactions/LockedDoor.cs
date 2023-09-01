using UnityEngine;

namespace Interactions
{
    public class LockedDoor : MonoBehaviour, IInteractible
    {
        // public GameObject hinge;
        // public float openAngle = 90;
        public bool locked = false;
        public bool open = false;
        
        public string InteractionText
        {
            get
            {
                if (locked) return "Locked";
                return open ? "Close" : "Open";
            }
        }

        public void Interact(PlayerController player)
        {
            //TODO: Implement keys and locking
            if (locked) return;
            open = !open;
            enabled = open;
            // transform.RotateAround(hinge.transform.position, Vector3.up, open ? openAngle : -openAngle);
        }
    }
}
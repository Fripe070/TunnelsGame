using UnityEngine;

namespace Interactions
{
    public class LockedDoor : MonoBehaviour, IInteractible
    {
        public GameObject hinge;
        public float openAngle = 90;
        public bool locked = true;
        public bool open;
        
        public string InteractionText
        {
            get
            {
                if (locked) return "Locked";
                return open ? "Close" : "Open";
            }
        }

        public new void Interact(PlayerController player)
        {
            //TODO: Implement keys
            if (locked) return;
            open = !open;
            transform.RotateAround(hinge.transform.position, hinge.transform.up, open ? openAngle : -openAngle);
        }
    }
}
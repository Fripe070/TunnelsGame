﻿#region

using UnityEngine;

#endregion

namespace Interactions
{
    public class LockedDoor : MonoBehaviour, IInteractive
    {
        public GameObject key;
        public bool spawnKeyOnFirstInteract;
        public GameObject hinge;
        public float openAngle = 90;
        public bool locked = true;
        public bool open;

        private void Start()
        {
            if (spawnKeyOnFirstInteract && key != null) key.SetActive(false);
        }

        public string InteractionText
        {
            get
            {
                if (spawnKeyOnFirstInteract)
                {
                    key.SetActive(true);
                    spawnKeyOnFirstInteract = false;
                }

                if (locked) return key != null ? "Locked\nRequires a key" : "Locked";
                return open ? "Close" : "Open";
            }
        }

        public void Interact(PlayerController player)
        {
            if (locked) return;
            open = !open;
            transform.RotateAround(hinge.transform.position, hinge.transform.up, open ? openAngle : -openAngle);
        }
    }
}
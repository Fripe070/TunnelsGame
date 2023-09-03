#region

using Interactions;
using UnityEngine;

#endregion

namespace items
{
    [RequireComponent(typeof(Collider))]
    public class Key : MonoBehaviour
    {
        public LockedDoor doorToUnlock;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;


            doorToUnlock.locked = false;
            Destroy(gameObject);
        }
    }
}
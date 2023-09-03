#region

using UnityEngine;

#endregion

namespace DeathZones
{
    public class TeleportZone : Zone
    {
        public Vector3 destination;
        public float damage;

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(destination, 0.25f);
        }

        public override void OnTriggerEnter(Collider other)
        {
            var charController = other.GetComponent<CharacterController>();
            if (charController != null) charController.enabled = false;
            other.transform.position = destination;
            if (charController != null) charController.enabled = true;
            
            var playerController = other.GetComponent<PlayerController>();
            if (playerController != null) playerController.Damage(damage);
        }
    }
}
#region

using UnityEngine;

#endregion

namespace DeathZones
{
    public class TeleportZone : Zone
    {
        public Vector3 destination;

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(destination, 0.25f);
        }

        public override void OnTriggerEnter(Collider other)
        {
            var controller = other.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            other.transform.position = destination;
            if (controller != null) controller.enabled = true;
        }
    }
}
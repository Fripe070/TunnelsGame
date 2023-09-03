#region

using UnityEngine;

#endregion

namespace DeathZones
{
    public class KillZone : Zone
    {
        public override void OnTriggerEnter(Collider other)
        {
            var player = other.gameObject.GetComponent<PlayerController>();
            if (player) player.Die();
        }
    }
}

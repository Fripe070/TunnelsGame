#region Imports

using UnityEngine;

#endregion

namespace DeathZones
{
    public abstract class Zone : MonoBehaviour
    {
        public abstract void OnTriggerEnter(Collider other);
    }
}
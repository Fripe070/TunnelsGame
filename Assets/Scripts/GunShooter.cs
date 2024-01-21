using Unity.Netcode;
using UnityEngine;

public class GunShooter : NetworkBehaviour
{
    [SerializeField]
    private Transform cameraTransform;
    [SerializeField]
    private float fireRange = 100f;
    [SerializeField]
    private float damage = 10f;
    [SerializeField]
    private GameObject hitEffectPrefab;
    
    private ulong[] _target = new ulong[1];
    
    private void FixedUpdate()
    {
        if (!IsOwner) return;

        // Left click
        if (Input.GetMouseButtonDown(0))
        {
            ShootServerRpc();
        }
    }
    
    [ServerRpc]
    public void ShootServerRpc()
    {
        if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit, fireRange)) 
            return;
        
#if UNITY_EDITOR
        Debug.Log($"Hit {hit.collider.gameObject.name}");
#endif
        
        if (hit.collider is null) return;
        if (hit.collider.gameObject.CompareTag("Player"))
            hit.collider.gameObject.GetComponent<PlayerController>().DamageClientRpc(damage);
        else 
            SpawnHitParticleClientRpc(hit.point, Quaternion.LookRotation(hit.normal));
    }
    
    
    [ClientRpc]
    private void SpawnHitParticleClientRpc(Vector3 hitLocation, Quaternion hitRotation)
    {
        var hitEffect = Instantiate(hitEffectPrefab, hitLocation, hitRotation);
        Destroy(hitEffect, 1f);

#if UNITY_EDITOR
        Debug.DrawLine(cameraTransform.position, hitLocation, Color.red, 1f);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        // Towards destination
        Debug.DrawRay(transform.position, cameraTransform.forward * fireRange, Color.blue);
    }
}
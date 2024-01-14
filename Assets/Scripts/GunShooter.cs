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
    
    private void FixedUpdate()
    {
        if (!IsOwner) return;

        // Left click
        if (Input.GetMouseButton(0))
        {
            ShootServerRpc();
        }
    }
    
    [ServerRpc]
    public void ShootServerRpc()
    {
        // Raycast and see if anything is hit
        if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit, fireRange)) return;
        
#if UNITY_EDITOR
        Debug.Log($"Hit {hit.collider.gameObject.name}");
#endif
        
        // If no collider, return
        if (hit.collider == null) return;
        
        if (hit.collider.gameObject.CompareTag("Player"))
        {
            var playerController = hit.collider.gameObject.GetComponent<PlayerController>();
            playerController.health -= damage;
        }
        
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
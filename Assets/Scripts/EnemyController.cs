#region

using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

#endregion

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    public float defaultSpeed = 3f;
    public float chaseSpeed = 6f;
    public float maxViewDistance = 6f;
    public float maxGoalDistance = 100f;
    
    private NavMeshAgent _agent;
    private GameObject _player;
    private bool _chasingPlayer;

    // Start is called before the first frame update
    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _player = GameObject.FindGameObjectWithTag("Player");
        _agent.speed = defaultSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        _agent.speed = _chasingPlayer ? chaseSpeed : defaultSpeed;
        
        // Prioritize chasing the player over anything else
        if (CanSee(_player))
        {
            if (!_chasingPlayer) StartChase();
            
            _chasingPlayer = true;
            _agent.SetDestination(_player.transform.position);
            return;
        }

        // Stop chasing
        if (_chasingPlayer && HasArrived())
        {
            _chasingPlayer = false;
            _agent.speed = defaultSpeed;
        }
        
        // For non-chase behaviour, we wait for the current navigation to be completed first
        if (!HasArrived()) return;
        
        
        // Randomly go to the player's position, so we don't get too far away from them
        if (Random.Range(0, 100) < 5)
        {
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation // Gets called rarely so idc
            Debug.Log("Randomly going to player");
            _agent.SetDestination(_player.transform.position);
            return;
        }
        
        // Wander around
        _agent.SetDestination(RandomNavSphere(transform.position, maxGoalDistance, -1));
    }

    private void StartChase()
    {
        //TODO: Add notice sound
    }

    private bool CanSee(GameObject target)
    {
        Vector3 position = transform.position;
        Vector3 targetPosition = target.transform.position + target.transform.lossyScale.y * Vector3.up;
        Vector3 direction = targetPosition - position;
        float distance = direction.magnitude;
        
        if (distance > maxViewDistance) return false;
        
        var ray = new Ray(position, direction); 
        Physics.Raycast(ray, out RaycastHit hit, distance);
        
        bool canSee = hit.transform == target.transform;
        return canSee;
    }
    
    private bool HasArrived()
    {
        if (!_agent.hasPath) return true;
        return _agent.remainingDistance <= _agent.stoppingDistance;
    }

    private static Vector3 RandomNavSphere(Vector3 origin, float distance, int layerMask) {
        Vector3 randomDirection = Random.insideUnitSphere * distance;
        randomDirection += origin;
        NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, distance, layerMask);
        return navHit.position;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            player.Die();
        }
    }

    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Vector3 position = transform.position;
        
        // Agro state
        if (CanSee(_player)) Gizmos.color = Color.red;
        else if (_chasingPlayer) Gizmos.color = Color.yellow;
        else if (_agent.hasPath) Gizmos.color = Color.blue;
        else Gizmos.color = Color.gray;
        Gizmos.DrawSphere(new Vector3(
            position.x, 
            position.y + transform.localScale.y / 2,
            position.z
        ), (float)(Math.Max(transform.localScale.x, transform.localScale.z) / 2 + 0.05));
    }
    
    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Vector3 position = transform.position;
        
        // Towards destination
        Debug.DrawRay(position, _agent.destination - position, Color.blue);
        
        // View distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, maxViewDistance);
        
        // Goal distance
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(position, maxGoalDistance);
    }
}

#region

using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Netcode;
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
    public float godSpeed = 20f;
    
    public float maxViewDistance = 6f;
    public float maxGoalDistance = 100f;
    public float damage = 20f;
    public AudioSource godChaseSound;

    private NavMeshAgent _agent;
    private bool _isChasing;
    private GameObject[] _players;
    private GameObject _currentTarget = null;
    
    [NonSerialized]
    public GameObject GodTarget;

    // Start is called before the first frame update
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        _agent.speed = defaultSpeed;
    }

    // Update is called once per frame
    private void Update()
    {
        godChaseSound.enabled = GodTarget is not null && !CanSee(GodTarget);
        if (GodTarget is not null)
        {
            _agent.speed = godSpeed;
            _agent.SetDestination(GodTarget.transform.position);
            return;
        }
        
        _players = GameObject.FindGameObjectsWithTag("Player");
        _agent.speed = _isChasing ? chaseSpeed : defaultSpeed;
        
        // Prioritize chasing the player over anything else
        if (_currentTarget != null && CanSee(_currentTarget))
        {
            if (!_isChasing) StartChase();

            _isChasing = true;
            _agent.SetDestination(_currentTarget.transform.position);
            return;
        }
        _currentTarget = GetVisible(_players);

        // Stop chasing
        if (_isChasing && HasArrived())
        {
            _isChasing = false;
            _agent.speed = defaultSpeed;
        }

        // For non-chase behaviour, we wait for the current navigation to be completed first
        if (!HasArrived()) return;
        
        // Randomly go to the player's position, so we don't get too far away from them
        if (_players.Length > 0 && Random.Range(0, 100) < 5)
        {
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation // Gets called rarely so idc
            Debug.Log("Randomly going to player");
            // Randomly chose a player form the list of players
            _agent.SetDestination(_players[Random.Range(0, _players.Length)].transform.position);
            return;
        }

        // Wander around
        _agent.SetDestination(RandomNavSphere(transform.position, maxGoalDistance, -1));
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            var player = other.gameObject.GetComponent<PlayerController>();
            player.health -= damage * Time.fixedDeltaTime;
        }
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
        var distance = direction.magnitude;

        if (distance > maxViewDistance) return false;

        var ray = new Ray(position, direction);
        Physics.Raycast(ray, out RaycastHit hit, distance);

        var canSee = hit.transform == target.transform;
        return canSee;
    }
    
    private GameObject GetVisible(GameObject[] targets)
    {
        foreach (GameObject target in targets){
            if (CanSee(target)){
                return target;
            }
        }
        return null;
    }

    private bool HasArrived()
    {
        if (!_agent.hasPath) return true;
        return _agent.remainingDistance <= _agent.stoppingDistance;
    }

    private static Vector3 RandomNavSphere(Vector3 origin, float distance, int layerMask)
    {
        Vector3 randomDirection = Random.insideUnitSphere * distance;
        randomDirection += origin;
        NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, distance, layerMask);
        return navHit.position;
    }

#if UNITY_EDITOR
    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Vector3 position = transform.position;

        // Agro state
        if (GetVisible(_players) != null) Gizmos.color = Color.red;
        else if (_currentTarget) Gizmos.color = Color.yellow;
        else if (_agent.hasPath) Gizmos.color = Color.blue;
        else Gizmos.color = Color.gray;
        Gizmos.DrawSphere(new Vector3(
            position.x,
            position.y + transform.localScale.y / 2,
            position.z
        ), (float)(Math.Max(transform.localScale.x, transform.localScale.z) / 2 + 0.05));
        
        var corners = _agent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Debug.DrawLine(corners[i], corners[i + 1], Color.green);
        }
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
#endif
}
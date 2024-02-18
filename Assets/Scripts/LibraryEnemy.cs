using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class LibraryEnemy : MonoBehaviour
{
    public float maxViewDistance;
    public float maxSpotDistance;
    
    public float wanderSpeed;
    public float hideSpeed;
    public float stalkSpeed;
    public float huntSpeed;
    
    public float hideForSeconds = 1;

    private float _scaredTimer;
    
    private NavMeshAgent _agent;
    private GameObject[] _players;
    private PlayerController _currentTarget;
    private GameObject _spottedBy;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        // TODO: Increase to like 10 or something
        InvokeRepeating(nameof(UpdatePlayers), 0, 1);
    }

    // I should really be listening for events instead of doing this, but I can't be bothered
    private void UpdatePlayers()
    {
        _players = GameObject.FindGameObjectsWithTag("Player");
    }

    void Update()
    {
        Debug.DrawLine(transform.position, _agent.destination, Color.blue);
        
        if (_players.Length == 0) return;
        
        if (_currentTarget is not null)
            Debug.DrawLine(transform.position, _currentTarget.transform.position, Color.yellow);
        
        // If already scared, keep running
        if (_scaredTimer > 0 && _spottedBy is not null)
        {
            _scaredTimer -= Time.deltaTime;
            HideFrom(_spottedBy.transform.position);
            return;
        }
        
        // Initial spotted check
        _spottedBy = _players.FirstOrDefault(player => IsSpotted(player.GetComponent<PlayerController>()));
        if (_spottedBy is not null)
        {
            _scaredTimer = Math.Max(hideForSeconds, 0.001f);
            _agent.ResetPath();
            HideFrom(_spottedBy.transform.position);
            return;
        }
        
        if (_currentTarget is not null)
        {
            Stalk();
            return;
        }
        
        // Check if we can get a new target
        _currentTarget = GetVisible(_players)?.GetComponent<PlayerController>();
        
        // If we can't find any players, wander
        if (_currentTarget is null)
        {
            Wander();
        }
    }

    private void Wander()
    {
        _agent.speed = wanderSpeed;
        if (!HasArrived()) return;
        _agent.SetDestination(RandomNavSphere(transform.position, maxViewDistance));
    }
    
    private void Stalk()
    {
        var rangeToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);
        
        _agent.speed = stalkSpeed;
        _agent.SetDestination(_currentTarget.transform.position);
    }
    
    private void HideFrom(Vector3 position)
    {
        _agent.speed = hideSpeed;
        if (!HasArrived()) return;

        // Choose a point further away from the player
        float currentDistance = Vector3.Distance(transform.position, position);
        var fleeTo = RandomNavSphere(position, maxViewDistance);
        for (var i = 0; i < 10; i++)  // Give up after 10 failed attempts and just go to a random point
        {
            if (Vector3.Distance(fleeTo, position) > currentDistance) break;
            fleeTo = RandomNavSphere(position, maxViewDistance);
        }
        
        _agent.SetDestination(fleeTo);
    }
    
    
    private bool IsSpotted(PlayerController player)
    {
        if (Vector3.Distance(transform.position, player.transform.position) > maxSpotDistance) return false;
        return player.CanSeePosition(transform.position + Vector3.up * 0.5f);
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

    private static Vector3 RandomNavSphere(Vector3 origin, float radius, int layerMask = -1)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += origin;
        NavMesh.SamplePosition(randomDirection, out var navHit, radius, layerMask);
        return navHit.position;
    }

    // ReSharper disable once Unity.InefficientPropertyAccess
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, maxViewDistance);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxSpotDistance);
        
        var corners = _agent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Debug.DrawLine(corners[i], corners[i + 1], Color.green);
        }
    }
}

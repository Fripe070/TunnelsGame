using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class LibraryEnemy : MonoBehaviour
{
    public float maxViewDistance;
    public float maxSpotDistance;
    public float huntRange;
    
    public float wanderSpeed;
    public float hideSpeed;
    public float stalkSpeed;
    public float huntSpeed;
    
    public float hideForSeconds = 1;
    public float huntFreezeTime = 5;
    
    public Color huntColor = Color.red;
    public Color scaredColor = Color.blue;
    public AudioSource huntSound;

    private float _scaredTimer;
    private float _huntFreezeTimer;
    private bool _isHunting;
    
    private NavMeshAgent _agent;
    private GameObject[] _players;
    private PlayerController _currentTarget;
    private GameObject _spottedBy;
    
    private Renderer _renderer;
    private Light _light;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        // TODO: Increase to like 10 or something
        InvokeRepeating(nameof(UpdatePlayers), 0, 1);

        _renderer = GetComponentInChildren<Renderer>();
        _light = GetComponentInChildren<Light>();
    }

    // I should really be listening for events instead of doing this, but I can't be bothered
    private void UpdatePlayers()
    {
        _players = GameObject.FindGameObjectsWithTag("Player");
    }

    void Update()
    {
        _renderer.transform.localPosition = Vector3.zero;
        _renderer.material.color = Color.white;
        _light.enabled = false;
        if (huntSound.enabled)
            huntSound.enabled = false;
        
        Debug.DrawLine(transform.position, _agent.destination, Color.blue);
        
        if (_players.Length < 1) return;
        
        if (_currentTarget is not null)
            Debug.DrawLine(transform.position, _currentTarget.transform.position, Color.yellow);
        
        #region Hunting logic
        if (_isHunting || _huntFreezeTimer > 0)
        {
            _renderer.material.color = huntColor;
            if (!huntSound.enabled)
                _light.enabled = true;
            huntSound.enabled = true;
            if (_huntFreezeTimer > 0)
            {
                _agent.ResetPath();
                _huntFreezeTimer -= Time.deltaTime;
                _isHunting = true;
                // Visually vibrate a bit
                _renderer.gameObject.transform.position = transform.position + Random.insideUnitSphere * 0.1f;
                _light.color = huntColor;
                return;
            }
            
            if (_currentTarget is null)
            {
                _isHunting = false;
                return;
            }
            _agent.speed = huntSpeed;
            _agent.SetDestination(_currentTarget.transform.position);

            Debug.Log(_agent.path.status);
            if (_agent.path.status == NavMeshPathStatus.PathPartial) _isHunting = false;
            return;
        }
        
        #endregion

        #region Scared logic
        // If already scared, keep running
        if (_scaredTimer > 0 && _spottedBy is not null)
        {
            _scaredTimer -= Time.deltaTime;
            HideFrom(_spottedBy.transform.position);
            _renderer.material.color = scaredColor;
            return;
        }
        
        // Initial spotted check
        _spottedBy = _players.FirstOrDefault(player => HasBeenSpottedBy(player.GetComponent<PlayerController>()));
        if (_spottedBy is not null)
        {
            _currentTarget = _spottedBy.GetComponent<PlayerController>();
            if (Vector3.Distance(transform.position, _spottedBy.transform.position) < huntRange)
            {
                _huntFreezeTimer = huntFreezeTime;
                return;
            }
            _scaredTimer = Math.Max(hideForSeconds, 0.001f);
            _agent.ResetPath();
            HideFrom(_spottedBy.transform.position);
            _renderer.material.color = scaredColor;
            return;
        }
        #endregion
        
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
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var target = other.GetComponent<PlayerController>();
        if (target is null) return;
        
        target.Die();
        _isHunting = false;
        _huntFreezeTimer = 0;
    }
    

    private void Wander()
    {
        _agent.speed = wanderSpeed;
        if (!HasArrived()) return;

        if (_players.Length > 0 && Random.value < 0.05f)
        {
            _agent.SetDestination(_players[Random.Range(0, _players.Length)].transform.position);
            return;
        }
        _agent.SetDestination(RandomNavSphere(transform.position, maxViewDistance));
    }
    
    private void Stalk()
    {
        _agent.speed = stalkSpeed;
        _agent.SetDestination(_currentTarget.transform.position);
    }
    
    private void HideFrom(Vector3 position)
    {
        _agent.speed = hideSpeed;
        if (!HasArrived()) return;

        for (var i = 0; i < 5; i++)
        {
            FindPath();
            // Try avoid paths where a lot of time is spent in the open
            float exposedDistance = ExposedDistanceBetween(_agent.path.corners, position);
            if (exposedDistance < 100) break;
        }
        // If we don't find any by now, who cares
        return;

        float ExposedDistanceBetween(IEnumerable<Vector3> points, Vector3 playerPosition)
        {
            return points
                .Where(point => LineOfSightBetween(point, playerPosition))
                .Sum(point => Vector3.Distance(point, playerPosition));
        }
        
        void FindPath()
        {
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
    }
    
    
    private bool HasBeenSpottedBy(PlayerController player)
    {
        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance > maxSpotDistance) return false;
        
        return player.CanSeePosition(transform.position + Vector3.up * 0.5f);
    }
    
    private bool LineOfSightBetween(Vector3 start, Vector3 end, Func<RaycastHit, bool> predicate = null)
    {
        var direction = end - start;
        float distance = direction.magnitude;

        if (distance > maxViewDistance) return false;

        var ray = new Ray(start, direction);
        Physics.Raycast(ray, out var hit, distance);
        
        if (predicate is not null && !predicate(hit)) return false;
        return true;
    }
    
    private bool CanISee(GameObject target)
    {
        return LineOfSightBetween(
            transform.position,
            target.transform.position + target.transform.lossyScale.y * Vector3.up,
            hit => hit.transform == target.transform
        );
    }
    
    private GameObject GetVisible(GameObject[] targets)
    {
        foreach (GameObject target in targets){
            if (CanISee(target)){
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

    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, maxViewDistance);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxSpotDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, huntRange);
        
        if (!Application.isPlaying) return;
        var corners = _agent.path.corners;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Debug.DrawLine(corners[i], corners[i + 1], Color.green);
        }
    }
}

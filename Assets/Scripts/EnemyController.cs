using System;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    public float defaultSpeed = 3f;
    public float chaseSpeed = 6f;
    public AudioSource audioSource;
    
    private NavMeshAgent _agent;
    private GameObject _player = null;
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
        Audio();
        
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
        
        // Wander around
        _agent.SetDestination(RandomNavSphere(transform.position, 100, -1));
    }

    private void StartChase()
    {
        _agent.speed = chaseSpeed;
    }

    private void Audio()
    {
        
    }
    

    public void FixedUpdate()
    {
        if (_chasingPlayer || !HasArrived()) return;
        
        // Randomly chase the player anyways
        if (CanSee(_player) && UnityEngine.Random.Range(0, 100) < 5)
        {
            Debug.Log("Randomly chasing player");
            _chasingPlayer = true;
            _agent.SetDestination(_player.transform.position);
        }
    }

    private bool CanSee(GameObject target)
    {
        Vector3 position = transform.position;
        Vector3 targetPosition = target.transform.position + target.transform.lossyScale.y * Vector3.up;
        Vector3 direction = targetPosition - position;
        float distance = direction.magnitude;
        
        var ray = new Ray(position, direction); 
        Physics.Raycast(ray, out RaycastHit hit, distance);
        
        bool canSee = hit.transform == target.transform;
        
        if (Selection.Contains (gameObject)) Debug.DrawRay(
            position, 
            hit.point - position, 
            canSee ? Color.green : Color.red);
        
        return canSee;
    }
    
    private bool HasArrived()
    {
        if (!_agent.hasPath) return true;
        return _agent.remainingDistance <= _agent.stoppingDistance;
    }

    private static Vector3 RandomNavSphere(Vector3 origin, float distance, int layerMask) {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * distance;
        
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
        // Unity complains that it's not assigned if I don't do this
        if (!Application.isPlaying) return;
        
        Vector3 position = transform.position;
        
        // Towards destination
        Debug.DrawRay(position, _agent.destination - position, Color.blue);
        
        // Agro state
        Gizmos.color = _chasingPlayer ? Color.green : Color.gray;
        Gizmos.DrawSphere(new Vector3(
            position.x, 
            position.y + transform.localScale.y / 2,
            position.z
        ), (float)(Math.Max(transform.localScale.x, transform.localScale.z) / 2 + 0.05));
    }
}

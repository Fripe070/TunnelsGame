
using System;
using JetBrains.Annotations;
using UnityEngine;

public class MonkeyGod : MonoBehaviour
{
    [SerializeField]
    private float turnSpeed = 1f;
    [SerializeField]
    private float maxViewDistance = 6f;
    [SerializeField] 
    private EnemyController enemyServant;
    private GameObject[] _players;
    private GameObject _target;

    private void Update()
    {
        _players = GameObject.FindGameObjectsWithTag("Player");
        _target = GetClosestVisible();
        // We want to remove its target if the monkey can't see anyone, hence the null check being afterwards
        enemyServant.GodTarget = _target;
        
        if (_target is null) return;
        
        // Slowly rotate to point towards the player
        var targetRotation = Quaternion.LookRotation(_target.transform.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        
    }

    [CanBeNull]
    private GameObject GetClosestVisible()
    {
        if (_players.Length == 0) return null;
        Gizmos.color = Color.blue;

        GameObject closestPlayer = _players[0];
        // We don't need to take the square root, since the squared distances will be in the same order
        var closestDistance = Vector3.SqrMagnitude(transform.position - closestPlayer.transform.position);
        foreach (var player in _players)
        {
            var distance = Vector3.SqrMagnitude(transform.position - player.transform.position);
            if (distance >= closestDistance) continue;
            closestDistance = distance;
            closestPlayer = player;
        }
        return CanSee(closestPlayer) ? closestPlayer : null;
    }

    private bool CanSee(GameObject target)
    {
        var direction = target.transform.position - transform.position;
        // if (direction.magnitude > maxViewDistance) return false;
        if (!Physics.Raycast(transform.position, direction, out var hit, maxViewDistance)) return false;
        return hit.collider.gameObject == target;
    }

    private void OnDrawGizmos()
    {
        if (_target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _target.transform.position);
        }
    }
}

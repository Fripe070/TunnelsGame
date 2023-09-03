#region Imports

using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#endregion

public class PlatformController : MonoBehaviour
{
    public bool isUp = true;
    public float moveSpeed = 1f;
    
    [SerializeField] private float moveDownBy;
    private float _upHeight;
    
    private void Awake()
    {
        _upHeight = transform.position.y;
    }

    private void Start()
    {
        if (isUp) return;
        transform.position += Vector3.down * moveDownBy;
    }

    private void Update()
    {
        Vector3 moveTo = transform.position;
        moveTo.y = isUp ? _upHeight : _upHeight - moveDownBy;
        var step = moveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, moveTo, step);
    }

    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void OnDrawGizmos()
    {
        Vector3 scale = transform.localScale * 1.01f; // Removes z-fighting on the sides
        scale.y = transform.localScale.y;
        
        Gizmos.color = isUp ? Color.grey : Color.green;
        Gizmos.DrawCube(transform.position + Vector3.down * moveDownBy, scale);
    }
}
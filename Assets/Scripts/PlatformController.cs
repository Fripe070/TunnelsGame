using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    public bool isUp = true;
    
    [SerializeField] private float moveDownBy;
    private float _upHeight;
    
    private void Awake()
    {
        _upHeight = transform.position.y;
    }

    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    private void OnDrawGizmos()
    {
        Vector3 scale = transform.localScale * 1.01f; // Removes z-fighting on the sides
        scale.y = transform.localScale.y;
        
        Gizmos.color = Color.grey;
        Gizmos.DrawCube(transform.position + Vector3.down * moveDownBy, scale);
    }
}
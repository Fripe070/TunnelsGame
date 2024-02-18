using UnityEngine;

public class Billboard : MonoBehaviour
{
    public bool lockX, lockY, lockZ;
    
    private Camera mainCamera;

    void LateUpdate()
    {
        if (mainCamera is null)
        {
            mainCamera = Camera.main;
            if (mainCamera is null) return;
        }
        
        Vector3 lookAt = mainCamera.transform.position;
        if (lockX) lookAt.x = transform.position.x;
        if (lockY) lookAt.y = transform.position.y;
        if (lockZ) lookAt.z = transform.position.z;
        
        transform.LookAt(lookAt);
        transform.Rotate(90, 0, 0);
    }
    
}
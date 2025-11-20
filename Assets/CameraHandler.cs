using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float smoothSpeed = 0.25f;

    private Vector3 velocity = Vector3.zero;

    private void FixedUpdate() 
    {
        Vector3 targetPosition = cameraTarget.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);    
        transform.LookAt(cameraTarget);
        
    }
}

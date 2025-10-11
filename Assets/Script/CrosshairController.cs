using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    [Tooltip("The FirePoint transform from the gun.")]
    public Transform gunFirePoint;
    [Tooltip("The main VR camera.")]
    public Camera vrCamera;
    [Tooltip("How far the crosshair sits when not hitting anything.")]
    public float defaultDistance = 5f;
    [Tooltip("How smoothly the crosshair moves to its target position.")]
    public float smoothSpeed = 15f;

    void Start()
    {
        // If the VR Camera isn't set, try to find it automatically.
        if (vrCamera == null)
        {
            vrCamera = Camera.main;
        }
    }

    void Update()
    {
        // Ensure the fire point is set before proceeding
        if (gunFirePoint == null || vrCamera == null) return;

        RaycastHit hit;
        Vector3 targetPosition;

        // Cast a ray from the gun's fire point
        if (Physics.Raycast(gunFirePoint.position, gunFirePoint.forward, out hit))
        {
            // If we hit something, the target is the hit point
            targetPosition = hit.point;
        }
        else
        {
            // If we don't hit anything, place the crosshair at a default distance
            targetPosition = gunFirePoint.position + gunFirePoint.forward * defaultDistance;
        }

        // Smoothly move the crosshair's position to the target
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // Make the crosshair always face the VR camera
        transform.rotation = Quaternion.LookRotation(transform.position - vrCamera.transform.position);
    }
}
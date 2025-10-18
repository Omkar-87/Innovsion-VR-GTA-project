using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    public Transform mechAimTarget;
    public Camera mainCamera;

    private RectTransform crosshairRectTransform;

    void Start()
    {
        crosshairRectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (mechAimTarget == null || mainCamera == null) return;

        // 1. Get the 3D world position
        Vector3 targetPoint = mechAimTarget.position + mechAimTarget.forward * 100f;

        // --- THIS IS THE FIX ---

        // 2. Get the screen point as a Vector3
        // The .z value tells us if the point is in front of or behind the camera
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(targetPoint);

        // 3. Check if the target is in front of the camera (z > 0)
        if (screenPoint.z > 0)
        {
            // It's in front: Set the position and make sure it's enabled
            crosshairRectTransform.position = new Vector2(screenPoint.x, screenPoint.y);

            if (!crosshairRectTransform.gameObject.activeInHierarchy)
                crosshairRectTransform.gameObject.SetActive(true);
        }
        else
        {
            // It's behind: Disable the crosshair so it doesn't flip out
            if (crosshairRectTransform.gameObject.activeInHierarchy)
                crosshairRectTransform.gameObject.SetActive(false);
        }
    }
}
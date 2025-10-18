using UnityEngine;
using UnityEngine.InputSystem;

public class TurretController : MonoBehaviour
{
    [Header("Input")]
    // We only need this one action now
    public InputActionProperty thumbstickAction;

    [Header("Settings")]
    public float turnSpeed = 45f;
    public float pitchSpeed = 30f;
    public float minPitch = -30f;
    public float maxPitch = 30f;

    private float currentPitch = 0f;

    void Update()
    {
        // 1. Read the full Vector2 value from the thumbstick
        Vector2 input = thumbstickAction.action.ReadValue<Vector2>();

        // 2. Read Turn Input (Left/Right) from the .x value
        float turnInput = input.x;
        transform.Rotate(Vector3.up, turnInput * turnSpeed * Time.deltaTime);

        // 3. Read Pitch Input (Up/Down) from the .y value
        float pitchInput = input.y;

        // 4. Calculate and clamp the pitch
        currentPitch += -pitchInput * pitchSpeed * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // 5. Apply the pitch rotation
        ApplyPitchToChildren(currentPitch);
    }

    void ApplyPitchToChildren(float pitch)
    { 
        // Find all the children that need to tilt
        Transform aimTarget = transform.Find("AimTarget");
        Transform cameraOffset = transform.Find("Camera Offset");
        Transform cockpit = transform.Find("FirstPerson_Cockpit");
        Transform gunLeft = transform.Find("M1911 Handgun Left");
        Transform gunRight = transform.Find("M1911 Handgun Right");

        // Apply the pitch rotation
        Quaternion pitchRotation = Quaternion.Euler(pitch, 0, 0);
        if (aimTarget != null) aimTarget.localRotation = pitchRotation;
        if (cameraOffset != null) cameraOffset.localRotation = pitchRotation;
        if (cockpit != null) cockpit.localRotation = pitchRotation;
        if (gunLeft != null) gunLeft.localRotation = pitchRotation;
        if (gunRight != null) gunRight.localRotation = pitchRotation;
    }
}
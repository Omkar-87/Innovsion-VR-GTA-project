using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPitchController : MonoBehaviour
{
    [Header("Input")]
    public InputActionProperty pitchAction; // The action for the right joystick vertical axis

    [Header("Settings")]
    public float pitchSpeed = 30f;
    public float minPitch = -30f; // How far down you can aim
    public float maxPitch = 30f; // How far up you can aim

    private float currentPitch = 0f;

    void Update()
    {
        // 1. Read the vertical input
        float pitchInput = pitchAction.action.ReadValue<float>();
        if (Mathf.Abs(pitchInput) < 0.1f) return; // Add a small deadzone

        // 2. Calculate rotation
        float rotationAmount = -pitchInput * pitchSpeed * Time.deltaTime;

        // 3. Add to the total pitch and clamp it
        currentPitch += rotationAmount;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // 4. Apply the rotation to this object
        transform.localRotation = Quaternion.Euler(currentPitch, 0, 0);
    }
}
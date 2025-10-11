using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GunController : MonoBehaviour
{
    // ... (all your existing variables are here) ...
    [Header("Shooting")]
    public Transform firePoint;
    public InputActionProperty shootAction;
    public float fireRate = 1f;
    public float maxDistance = 100f;
    public int damage = 10;
    private float nextFireTime = 0f;
    [Header("Recoil Settings")]
    public float recoilKickback = 0.05f;
    public float recoilUpKick = 5.0f;
    public float returnSpeed = 10f;
    [Header("Effects (Prefabs)")]
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;
    [Tooltip("Specify time to destroy the effect objects")]
    public float destroyTimer = 2f;
    [Header("Haptic Feedback")]
    [Tooltip("Intensity of the controller vibration.")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.6f;
    [Tooltip("Duration of the controller vibration.")]
    public float hapticDuration = 0.1f;
    private Coroutine stopRumbleCoroutine;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    // --- End of variables ---

    void Start()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }

    void Update()
    {
        if (shootAction.action.IsPressed() && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * returnSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, originalRotation, Time.deltaTime * returnSpeed);
    }

    void Shoot()
    {
        Debug.Log("Shoot() method called."); // DEBUG
        TriggerHaptics();

        // The rest of your shoot logic...
        if (muzzleFlashPrefab != null && firePoint != null)
        {
            GameObject tempFlash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
            Destroy(tempFlash, destroyTimer);
        }
        RaycastHit hit;
        if (firePoint != null && Physics.Raycast(firePoint.position, firePoint.forward, out hit, maxDistance))
        {
            if (impactEffectPrefab != null)
            {
                GameObject impactInstance = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactInstance, destroyTimer);
            }
            Health targetHealth = hit.transform.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
            }
        }
        transform.localPosition -= Vector3.forward * recoilKickback;
        transform.localRotation *= Quaternion.Euler(-recoilUpKick, 0, 0);
    }

    private void TriggerHaptics()
    {
        Debug.Log("TriggerHaptics() called."); // DEBUG

        var device = shootAction.action.activeControl?.device;

        if (device == null)
        {
            Debug.LogWarning("Haptics failed: Device is null. Is the shootAction assigned in the Inspector?"); // DEBUG
            return;
        }

        // --- THIS IS THE MOST IMPORTANT CHECK ---
        if (device is Gamepad gamepad)
        {
            Debug.Log("Device is a Gamepad: " + gamepad.displayName); // DEBUG
            if (stopRumbleCoroutine != null)
            {
                StopCoroutine(stopRumbleCoroutine);
            }
            stopRumbleCoroutine = StartCoroutine(RumbleCoroutine(gamepad, hapticIntensity, hapticDuration));
        }
        else
        {
            // If this message appears, your controller is NOT being seen as a "Gamepad"
            Debug.LogWarning("Haptics failed: Device is NOT a Gamepad. Device type is: " + device.GetType().Name); // DEBUG
        }
    }

    private IEnumerator RumbleCoroutine(Gamepad gamepad, float intensity, float duration)
    {
        Debug.Log("RumbleCoroutine started with intensity " + intensity + " for " + duration + "s."); // DEBUG
        gamepad.SetMotorSpeeds(intensity, intensity);
        yield return new WaitForSeconds(duration);
        gamepad.SetMotorSpeeds(0f, 0f);
        stopRumbleCoroutine = null;
    }
}
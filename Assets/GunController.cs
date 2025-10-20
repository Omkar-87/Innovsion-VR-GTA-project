using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Netcode;

public class GunController : NetworkBehaviour
{
    [Header("Shooting")]
    public Transform aimTarget;
    public LayerMask shootableLayers;
    public Transform firePoint;
    public InputActionProperty shootAction;
    public float fireRate = 1f;
    public float maxDistance = 100f;
    public int damage = 10;
    private float nextFireTime = 0f;

    [Header("Ammo")]
    public int maxAmmo = 30;
    public int currentAmmo; // Made public to be accessed by UI
    public float reloadTime = 1.5f;
    public InputActionProperty reloadAction;
    private bool isReloading = false;

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
    [Range(0f, 1f)]
    public float hapticIntensity = 0.6f;
    public float hapticDuration = 0.1f;
    private Coroutine stopRumbleCoroutine;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        currentAmmo = maxAmmo;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }

    // Called when the object becomes enabled and active.
    void OnEnable()
    {
        isReloading = false;
    }

    void Update()
    {
        if (!IsOwner) return;

        // If reloading, nothing else can happen.
        if (isReloading) return;

        // Check for reload input if current ammo is less than max.
        if (reloadAction.action.WasPressedThisFrame() && currentAmmo < maxAmmo)
        {
            StartCoroutine(Reload());
            return; // Exit update early to prevent shooting while starting to reload.
        }

        // Check for shooting input.
        if (shootAction.action.IsPressed() && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }
            else
            {
                // Optional: Play an "empty clip" sound here.
                Debug.Log("Out of ammo!");
            }
        }

        // Recoil return
        transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * returnSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, originalRotation, Time.deltaTime * returnSpeed);
    }

    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");

        // You can trigger a reload animation here.

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
    }

    void Shoot()
    {
        // Safety checks
        if (!IsOwner || currentAmmo <= 0 || isReloading) return;

        currentAmmo--; // Decrease ammo count.

        if (aimTarget == null)
        {
            Debug.LogError("Aim Target is not assigned in GunController!");
            return;
        }

        TriggerHaptics();

        // Muzzle Flash
        if (muzzleFlashPrefab != null && firePoint != null)
        {
            GameObject tempFlash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
            Destroy(tempFlash, destroyTimer);
        }

        // Raycasting logic
        RaycastHit aimHit;
        Vector3 targetPoint;
        if (Physics.Raycast(aimTarget.position, aimTarget.forward, out aimHit, maxDistance, shootableLayers))
        {
            targetPoint = aimHit.point;
        }
        else
        {
            targetPoint = aimTarget.position + aimTarget.forward * maxDistance;
        }

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        RaycastHit gunHit;
        if (firePoint != null && Physics.Raycast(firePoint.position, shootDirection, out gunHit, maxDistance, shootableLayers))
        {
            if (impactEffectPrefab != null)
            {
                GameObject impactInstance = Instantiate(impactEffectPrefab, gunHit.point, Quaternion.LookRotation(gunHit.normal));
                Destroy(impactInstance, destroyTimer);
            }

            Health targetHealth = gunHit.transform.GetComponent<Health>();
            if (targetHealth != null)
            {
                DealDamageServerRpc(targetHealth.GetComponent<NetworkObject>().NetworkObjectId, damage);
            }
        }

        // Apply recoil
        transform.localPosition -= Vector3.forward * recoilKickback;
        transform.localRotation *= Quaternion.Euler(-recoilUpKick, 0, 0);
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong targetId, int damage)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObject))
        {
            if (targetObject != null)
            {
                Health targetHealth = targetObject.GetComponent<Health>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(damage);
                }
            }
        }
    }

    // --- Haptics Functions ---
    private void TriggerHaptics()
    {
        var device = shootAction.action.activeControl?.device;
        if (device is Gamepad gamepad)
        {
            if (stopRumbleCoroutine != null)
            {
                StopCoroutine(stopRumbleCoroutine);
            }
            stopRumbleCoroutine = StartCoroutine(RumbleCoroutine(gamepad, hapticIntensity, hapticDuration));
        }
    }

    private IEnumerator RumbleCoroutine(Gamepad gamepad, float intensity, float duration)
    {
        gamepad.SetMotorSpeeds(intensity, intensity);
        yield return new WaitForSeconds(duration);
        gamepad.SetMotorSpeeds(0f, 0f);
        stopRumbleCoroutine = null;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Netcode;
using UnityEngine.Animations; // Note: This 'using' statement is not actually used in this script.

public class GunController : NetworkBehaviour
{
    [Header("Shooting")]
    public Transform aimTarget; // CHANGED: This is the critical part
    public LayerMask shootableLayers; // ADDED: Make sure to set this in the Inspector
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
    [Range(0f, 1f)]
    public float hapticIntensity = 0.6f;
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
        // Only the owner can shoot
        if (!IsOwner) return;

        if (shootAction.action.IsPressed() && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }

        // Recoil return
        transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, Time.deltaTime * returnSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, originalRotation, Time.deltaTime * returnSpeed);
    }

    void Shoot()
    {
        if (!IsOwner) return;

        if (aimTarget == null)
        {
            Debug.LogError("Aim Target is not assigned in GunController!");
            return;
        }

        TriggerHaptics();

        if (muzzleFlashPrefab != null && firePoint != null)
        {
            GameObject tempFlash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
            Destroy(tempFlash, destroyTimer);
        }


        RaycastHit aimHit;
        Vector3 targetPoint;
        if (Physics.Raycast(aimTarget.position, aimTarget.forward, out aimHit, maxDistance, shootableLayers))
        {
            targetPoint = aimHit.point; // We hit something
        }
        else
        {
            targetPoint = aimTarget.position + aimTarget.forward * maxDistance; // We hit nothing, aim into the distance
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

            Debug.Log(gunHit.transform.name);
            Health targetHealth = gunHit.transform.GetComponent<Health>();
            if (targetHealth != null)
            {
                Debug.Log("<color=yellow>Hit</color> " + targetHealth.gameObject.name);
                DealDamageServerRpc(targetHealth.GetComponent<NetworkObject>().NetworkObjectId, damage);
            }
        }
        // --- End of new logic ---

        // Apply recoil
        transform.localPosition -= Vector3.forward * recoilKickback;
        transform.localRotation *= Quaternion.Euler(-recoilUpKick, 0, 0);
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong targetId, int damage)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObject))
        {
            Debug.Log("<color=magenta>Hit with</color> " + targetId);
            if (targetObject != null)
            {
                Health targetHealth = targetObject.GetComponent<Health>();
                if (targetHealth != null)
                {
                    ulong id = NetworkManager.Singleton.LocalClientId;
                    Debug.Log($"<color=red>Server dealing damage, called from client {id}</color>");
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
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Netcode;

public class GunController : NetworkBehaviour
{
    [Header("Core References")]
    public Transform aimTarget;            
    public Transform firePoint;            
    public Transform gunGraphicsTransform; 
    public Camera mainCamera;              

    [Header("Input Actions")]
    public InputActionProperty shootAction;
    public InputActionProperty reloadAction; 

    [Header("Shooting Stats")]
    public LayerMask shootableLayers;  
    public float fireRate = 10f;       
    public float maxDistance = 100f;
    public int damage = 2;             

    [Header("Ammo")]
    public int maxAmmo = 50;
    public int currentAmmo;            
    public float reloadTime = 1.5f;
    private bool isReloading = false;

    [Header("Recoil (Applied to Graphics)")]
    public float recoilKickback = 0.03f;
    public float recoilUpKick = 2.0f;
    public float returnSpeed = 15f;

    [Header("Bobbing (Applied to Graphics)")]
    public float bobSpeed = 2.0f;
    public float bobAmount = 0.005f;
    private Vector3 bobbingOffset = Vector3.zero;

    [Header("Camera Shake")]
    public float shakeDuration = 0.08f;
    public float shakeMagnitude = 0.005f;
    private Coroutine cameraShakeCoroutine;

    [Header("Effects (Prefabs)")]
    public GameObject muzzleFlashPrefab; 
    public GameObject impactEffectPrefab;
    public float destroyTimer = 1.5f;    

    [Header("Haptic Feedback")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.4f;
    public float hapticDuration = 0.05f;
    private Coroutine stopRumbleCoroutine;

    private float nextFireTime = 0f;
    private Vector3 graphicsOriginalLocalPosition;
    private Quaternion graphicsOriginalLocalRotation;


    void Start()
    {
        currentAmmo = maxAmmo;
        isReloading = false;

        if (gunGraphicsTransform != null)
        {
            graphicsOriginalLocalPosition = gunGraphicsTransform.localPosition;
            graphicsOriginalLocalRotation = gunGraphicsTransform.localRotation;
        }
        else if (IsOwner)
        {
            Debug.LogError($"[{gameObject.name}] Gun Graphics Transform is not assigned!", this);
        }

        if (IsOwner && mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) Debug.LogError($"[{gameObject.name}] Main Camera not found! Assign it or tag it 'MainCamera'.", this);
        }
    }

    void OnEnable()
    {
        isReloading = false;
    }

    void Update()
    {
        if (!IsOwner) return;

        ApplyBobbingAndRecoilReturn();
        HandleInput();
    }

    void HandleInput()
    {

        if (!isReloading && currentAmmo < maxAmmo && reloadAction.action.WasPressedThisFrame())
        {
            StartCoroutine(Reload_Local());
            return;
        }

        if (isReloading) return;

        if (shootAction.action.ReadValue<float>() > 0.1f && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0 && IsSpawned)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }
            else if (currentAmmo <= 0)
            {
                // dO Something to indicate out of ammo
            }
        }
    }

    void Shoot()
    {
        // Local checks
        if (gunGraphicsTransform == null || aimTarget == null || !IsSpawned || currentAmmo <= 0 || isReloading) return;

        currentAmmo--;

        TriggerHaptics();
        TriggerCameraShake();
        ApplyRecoil();

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

        bool didHit = false;
        Vector3 hitPoint = Vector3.zero;
        Vector3 hitNormal = Vector3.forward;
        ulong hitTargetId = 0; // 0 means no NetworkObject hit

        RaycastHit gunHit;
        if (firePoint != null && Physics.Raycast(firePoint.position, shootDirection, out gunHit, maxDistance, shootableLayers))
        {
            didHit = true;
            hitPoint = gunHit.point;
            hitNormal = gunHit.normal;

            Health targetHealth = gunHit.transform.GetComponent<Health>();
            NetworkObject targetNetworkObject = gunHit.transform.GetComponent<NetworkObject>(); // Get NetworkObject too
            if (targetHealth != null && targetNetworkObject != null) // Check both exist
            {
                hitTargetId = targetNetworkObject.NetworkObjectId;
                Debug.Log($"[{gameObject.name}] Locally hit NetworkObject {gunHit.transform.name} with ID {hitTargetId}");
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Locally hit {gunHit.transform.name}, but it lacks Health or NetworkObject component.");
            }
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Local raycast did not hit anything.");
        }

        // ---Request Server Actions---
        // Tell server about the shot, where it hit (if it did), and who it hit (if applicable)
        // Server will then trigger effects and damage RPCs
        ShootServerRpc(didHit, hitPoint, Quaternion.LookRotation(hitNormal), hitTargetId);
    }

    // Local sequence for reloading
    IEnumerator Reload_Local()
    {
        isReloading = true;
        Debug.Log($"[{gameObject.name}] Starting local reload sequence...");
        // TODO: Play Reload Animation/Sound locally HERE

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo; // Refill ammo locally
        isReloading = false;
        Debug.Log($"[{gameObject.name}] Local reload sequence finished. Ammo refilled.");
    }


    void ApplyBobbingAndRecoilReturn()
    {
        if (gunGraphicsTransform == null) return;
        float bobSin = Mathf.Sin(Time.time * bobSpeed);
        bobbingOffset = new Vector3(0, bobSin * bobAmount, 0);
        Vector3 targetLocalPosition = graphicsOriginalLocalPosition + bobbingOffset;
        gunGraphicsTransform.localPosition = Vector3.Lerp(gunGraphicsTransform.localPosition, targetLocalPosition, Time.deltaTime * returnSpeed);
        gunGraphicsTransform.localRotation = Quaternion.Slerp(gunGraphicsTransform.localRotation, graphicsOriginalLocalRotation, Time.deltaTime * returnSpeed);
    }

    void ApplyRecoil()
    {
        if (gunGraphicsTransform == null) return;
        gunGraphicsTransform.localPosition -= gunGraphicsTransform.forward * recoilKickback;
        gunGraphicsTransform.localRotation *= Quaternion.Euler(-recoilUpKick, Random.Range(-recoilUpKick * 0.5f, recoilUpKick * 0.5f), 0);
    }

    void TriggerCameraShake()
    {
        if (mainCamera != null)
        {
            if (cameraShakeCoroutine != null) StopCoroutine(cameraShakeCoroutine);
            cameraShakeCoroutine = StartCoroutine(ShakeCamera());
        }
    }

    IEnumerator ShakeCamera()
    {
        if (mainCamera == null) yield break;
        Vector3 originalCamPos = mainCamera.transform.localPosition;
        float elapsed = 0.0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            // Safety check in case camera parent changes during shake
            if (mainCamera != null) mainCamera.transform.localPosition = originalCamPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Safety check before resetting
        if (mainCamera != null) mainCamera.transform.localPosition = originalCamPos;
        cameraShakeCoroutine = null;
    }

    // --- NETWORKING (RPCs) ---

    // Owner calls this, runs on Server
    [ServerRpc]
    private void ShootServerRpc(bool didHit, Vector3 hitPoint, Quaternion hitRotation, ulong hitTargetId)
    {
        SpawnMuzzleFlashClientRpc();

        if (didHit)
        {
            SpawnImpactEffectClientRpc(hitPoint, hitRotation);

            if (hitTargetId != 0) // Check if we hit a valid NetworkObject
            {
                ApplyDamage(hitTargetId, damage);
            }
        }
    }

    private void ApplyDamage(ulong targetId, int damageAmount)
    {
        if (!IsServer) return; // Only server executes this

        // Use NetworkManager's SpawnManager to find the object
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObject))
        {
            Health targetHealth = targetObject.GetComponent<Health>();
            if (targetHealth != null)
            {
                Debug.Log($"SERVER: Applying {damageAmount} damage to {targetObject.name} (ID: {targetId})");
                targetHealth.TakeDamage(damageAmount); // Call TakeDamage on the server's instance
            }
            else
            {
                Debug.LogWarning($"SERVER: Hit target {targetObject.name} (ID: {targetId}) but it has no Health component.");
            }
        }
        else
        {
            Debug.LogWarning($"SERVER: Could not find hit target with NetworkObjectId {targetId} to apply damage.");
        }
    }

    // Server calls this, runs on ALL Clients
    [ClientRpc]
    private void SpawnMuzzleFlashClientRpc()
    {
        Debug.Log($"CLIENT {NetworkManager.Singleton.LocalClientId}: Received SpawnMuzzleFlashClientRpc for object {gameObject.name}. IsOwner={IsOwner}");

        if (firePoint == null)
        {
            Debug.LogError($"CLIENT {NetworkManager.Singleton.LocalClientId}: firePoint is NULL on {gameObject.name}!");
            return;
        }
        if (muzzleFlashPrefab == null)
        {
            Debug.LogError($"CLIENT {NetworkManager.Singleton.LocalClientId}: muzzleFlashPrefab is NULL on {gameObject.name}!");
            return;
        }
        Debug.Log($"CLIENT {NetworkManager.Singleton.LocalClientId}: Spawning muzzle flash at {firePoint.position} for {gameObject.name}.");
        SpawnEffect(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
    }

    // Server calls this, runs on ALL Clients
    [ClientRpc]
    private void SpawnImpactEffectClientRpc(Vector3 position, Quaternion rotation)
    {
        Debug.Log($"CLIENT {NetworkManager.Singleton.LocalClientId}: Received SpawnImpactEffectClientRpc for object {gameObject.name}. IsOwner={IsOwner}");
        if (impactEffectPrefab == null)
        {
            Debug.LogError($"CLIENT {NetworkManager.Singleton.LocalClientId}: impactEffectPrefab is NULL on {gameObject.name}!");
            return;
        }
        SpawnEffect(impactEffectPrefab, position, rotation);
    }

    // Helper to spawn effects locally (called by ClientRpcs)
    private void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (effectPrefab != null)
        {
            GameObject instance = Instantiate(effectPrefab, position, rotation, parent);
            Destroy(instance, destroyTimer);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Attempted to spawn an effect, but the prefab was null!");
        }
    }

    private void TriggerHaptics()
    {
        if (!IsOwner) return;
        var device = shootAction.action.activeControl?.device;
        if (device is Gamepad gamepad)
        {
            if (stopRumbleCoroutine != null) StopCoroutine(stopRumbleCoroutine);
            stopRumbleCoroutine = StartCoroutine(RumbleCoroutine(gamepad, hapticIntensity, hapticDuration));
        }
    }

    private IEnumerator RumbleCoroutine(Gamepad gamepad, float intensity, float duration)
    {
        if (gamepad == null) yield break; // Safety check
        
        gamepad.SetMotorSpeeds(intensity, intensity);
        yield return new WaitForSeconds(duration);
        if (gamepad != null && gamepad.added) gamepad.SetMotorSpeeds(0f, 0f);
        
        stopRumbleCoroutine = null;
        
    }
} // End of class
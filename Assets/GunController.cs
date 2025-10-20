using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Netcode;

// Ensure this script is on the GUN object itself (the one with the firePoint).
public class GunController : NetworkBehaviour
{
    [Header("Core References")]
    public Transform aimTarget;            // Assign the object determining aim direction
    public Transform firePoint;            // Assign the empty GameObject at the gun barrel's end
    public Transform gunGraphicsTransform; // Drag the GUN's VISUAL MODEL/Mesh parent object here
    public Camera mainCamera;              // Assign the player's main camera (optional, can find by tag)

    [Header("Input Actions")]
    public InputActionProperty shootAction; // Assign your shoot input action reference
    public InputActionProperty reloadAction; // Assign your reload input action reference

    [Header("Shooting Stats")]
    public LayerMask shootableLayers;   // Set this in Inspector (e.g., Everything except Player layer)
    public float fireRate = 10f;        // Shots per second
    public float maxDistance = 100f;
    public int damage = 2;              // Damage dealt per hit

    [Header("Ammo")]
    public int maxAmmo = 50;
    public int currentAmmo;             // Local ammo count, not synced
    public float reloadTime = 1.5f;
    private bool isReloading = false;     // Local reload state

    [Header("Recoil (Applied to Graphics)")]
    public float recoilKickback = 0.03f;
    public float recoilUpKick = 2.0f;
    public float returnSpeed = 15f;

    [Header("Bobbing (Applied to Graphics)")]
    public float bobSpeed = 2.0f;
    public float bobAmount = 0.005f;
    private Vector3 bobbingOffset = Vector3.zero;

    [Header("Camera Shake")]
    public Transform shakeOffsetTransform; // <<< ADDED: Assign the 'ShakeOffset' empty GameObject here
    public float shakeDuration = 0.08f;
    public float shakeMagnitude = 0.005f;
    private Coroutine cameraShakeCoroutine;

    [Header("Effects (Prefabs)")]
    public GameObject muzzleFlashPrefab; // Assign particle effect prefab
    public GameObject impactEffectPrefab;  // Assign particle effect prefab
    public float destroyTimer = 1.5f;     // How long effects last

    [Header("Haptic Feedback")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.4f;
    public float hapticDuration = 0.05f;
    private Coroutine stopRumbleCoroutine;

    // Internal state
    private float nextFireTime = 0f;
    private Vector3 graphicsOriginalLocalPosition;
    private Quaternion graphicsOriginalLocalRotation;
    private Vector3 shakeOffsetOriginalLocalPosition = Vector3.zero; // Store shake offset original pos

    // --- INITIALIZATION ---

    void Start()
    {
        // Initialize ammo locally
        currentAmmo = maxAmmo;
        isReloading = false;

        // Cache original graphics transform if assigned
        if (gunGraphicsTransform != null)
        {
            graphicsOriginalLocalPosition = gunGraphicsTransform.localPosition;
            graphicsOriginalLocalRotation = gunGraphicsTransform.localRotation;
        }
        else if (IsOwner)
        {
            Debug.LogError($"[{gameObject.name}] Gun Graphics Transform is not assigned!", this);
        }

        // Cache shake offset original position if assigned
        if (shakeOffsetTransform != null)
        {
            shakeOffsetOriginalLocalPosition = shakeOffsetTransform.localPosition;
        }
        else if (IsOwner) // Only owner needs shake offset
        {
            Debug.LogError($"[{gameObject.name}] Shake Offset Transform is not assigned!", this);
        }

        // Find main camera if not assigned (still useful for checks, even if shake uses offset)
        if (IsOwner && mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) Debug.LogWarning($"[{gameObject.name}] Main Camera not found/assigned. Camera checks might fail.", this);
        }
    }

    void OnEnable()
    {
        isReloading = false; // Reset reload state when enabled/respawned
    }

    // --- UPDATE LOOP (Owner Only for Input/Visuals) ---

    void Update()
    {
        // Only the owner processes input and local visual effects
        if (!IsOwner) return;

        // Apply visual bobbing and recoil return locally
        ApplyBobbingAndRecoilReturn();
        HandleInput();
    }

    // Separated input handling for clarity
    void HandleInput()
    {
        // --- Handle Reloading Input ---
        if (!isReloading && currentAmmo < maxAmmo && reloadAction.action.WasPressedThisFrame())
        {
            StartCoroutine(Reload_Local());
            return;
        }

        // --- Handle Shooting Input ---
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
                // Optional: Play empty clip sound locally
            }
        }
    }

    // --- CORE ACTIONS ---

    // Called locally by the owner
    void Shoot()
    {
        if (gunGraphicsTransform == null || aimTarget == null || !IsSpawned || currentAmmo <= 0 || isReloading) return;

        currentAmmo--;

        // --- Local Visuals & Feedback (Instant) ---
        TriggerHaptics();
        TriggerCameraShake(); // This now shakes the offset object
        ApplyRecoil();

        // --- Determine Hit Info Locally ---
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
        ulong hitTargetId = 0;

        RaycastHit gunHit;
        if (firePoint != null && Physics.Raycast(firePoint.position, shootDirection, out gunHit, maxDistance, shootableLayers))
        {
            didHit = true;
            hitPoint = gunHit.point;
            hitNormal = gunHit.normal;

            Health targetHealth = gunHit.transform.GetComponent<Health>();
            NetworkObject targetNetworkObject = gunHit.transform.GetComponent<NetworkObject>();
            if (targetHealth != null && targetNetworkObject != null)
            {
                hitTargetId = targetNetworkObject.NetworkObjectId;
            }
        }

        // --- Request Server Actions ---
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


    // --- VISUAL/LOCAL EFFECTS (Owner Only) ---

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
        // Check shakeOffsetTransform before starting
        if (shakeOffsetTransform != null)
        {
            if (cameraShakeCoroutine != null) StopCoroutine(cameraShakeCoroutine);
            cameraShakeCoroutine = StartCoroutine(ShakeCameraOffset()); // Renamed function
        }
        else if (IsOwner) // Only warn the owner
        {
            Debug.LogWarning($"[{gameObject.name}] Trying to shake camera, but Shake Offset Transform is not assigned!");
        }
    }

    // --- MODIFIED: Shakes the Offset Transform ---
    IEnumerator ShakeCameraOffset() // Renamed
    {
        if (shakeOffsetTransform == null) yield break; // Safety check using the correct variable

        // Use the cached original position
        // Vector3 originalShakePos = shakeOffsetOriginalLocalPosition; // Already stored in Start()
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            // Apply to shakeOffsetTransform
            if (shakeOffsetTransform != null)
                shakeOffsetTransform.localPosition = shakeOffsetOriginalLocalPosition + new Vector3(x, y, 0);
            else
                yield break; // Exit if object is gone

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset shakeOffsetTransform
        if (shakeOffsetTransform != null) shakeOffsetTransform.localPosition = shakeOffsetOriginalLocalPosition;
        cameraShakeCoroutine = null;
    }
    // ---------------------------------------------

    // --- NETWORKING (RPCs) ---

    [ServerRpc]
    private void ShootServerRpc(bool didHit, Vector3 hitPoint, Quaternion hitRotation, ulong hitTargetId)
    {
        SpawnMuzzleFlashClientRpc(); // Tell clients to show flash

        if (didHit)
        {
            SpawnImpactEffectClientRpc(hitPoint, hitRotation); // Tell clients to show impact

            if (hitTargetId != 0)
            {
                ApplyDamage(hitTargetId, damage); // Server applies damage
            }
        }
    }

    // Server-only helper to apply damage
    private void ApplyDamage(ulong targetId, int damageAmount)
    {
        if (!IsServer) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetObject))
        {
            Health targetHealth = targetObject.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damageAmount);
            }
        }
    }

    [ClientRpc]
    private void SpawnMuzzleFlashClientRpc()
    {
        // Add checks before spawning
        if (firePoint != null && muzzleFlashPrefab != null)
        {
            SpawnEffect(muzzleFlashPrefab, firePoint.position, firePoint.rotation); // No parent
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Client {NetworkManager.Singleton.LocalClientId}: Failed to spawn muzzle flash (missing firePoint or prefab).");
        }
    }

    [ClientRpc]
    private void SpawnImpactEffectClientRpc(Vector3 position, Quaternion rotation)
    {
        if (impactEffectPrefab != null)
        {
            SpawnEffect(impactEffectPrefab, position, rotation);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Client {NetworkManager.Singleton.LocalClientId}: Failed to spawn impact effect (missing prefab).");
        }
    }

    // Helper to spawn effects locally (called by ClientRpcs)
    private void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        // Prefab null check moved here for central logging
        if (effectPrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] Attempted to spawn an effect, but the effect prefab is null!");
            return;
        }
        GameObject instance = Instantiate(effectPrefab, position, rotation, parent);
        Destroy(instance, destroyTimer);
    }

    // --- Haptics (Local Only - Owner) ---
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
        if (gamepad == null) yield break;
    
            gamepad.SetMotorSpeeds(intensity, intensity);
            yield return new WaitForSeconds(duration);
            if (gamepad != null && gamepad.added) gamepad.SetMotorSpeeds(0f, 0f);
        

            stopRumbleCoroutine = null;
        
    }
} // End of class
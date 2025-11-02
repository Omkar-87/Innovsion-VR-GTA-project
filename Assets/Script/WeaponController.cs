using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WeaponController : MonoBehaviour
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
    public Transform shakeOffsetTransform;
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

    // Internal state
    private float nextFireTime = 0f;
    private Vector3 graphicsOriginalLocalPosition;
    private Quaternion graphicsOriginalLocalRotation;
    private Vector3 shakeOffsetOriginalLocalPosition = Vector3.zero;

    void Start()
    {
        currentAmmo = maxAmmo;
        isReloading = false;

        if (gunGraphicsTransform != null)
        {
            graphicsOriginalLocalPosition = gunGraphicsTransform.localPosition;
            graphicsOriginalLocalRotation = gunGraphicsTransform.localRotation;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Gun Graphics Transform is not assigned!", this);
        }

        if (shakeOffsetTransform != null)
        {
            shakeOffsetOriginalLocalPosition = shakeOffsetTransform.localPosition;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Shake Offset Transform is not assigned!", this);
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) Debug.LogWarning($"[{gameObject.name}] Main Camera not found/assigned. Camera checks might fail.", this);
        }
    }

    void OnEnable()
    {
        isReloading = false;
    }

    void Update()
    {
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
            if (currentAmmo > 0)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }
            else if (currentAmmo <= 0)
            {
                // EMPTY...EMPTY SOUND
            }
        }
    }

    void Shoot()
    {
        if (gunGraphicsTransform == null || aimTarget == null || currentAmmo <= 0 || isReloading) return;

        currentAmmo--;

        TriggerHaptics();
        TriggerCameraShake();
        ApplyRecoil();
        SpawnMuzzleFlash();

        RaycastHit aimHit;
        Vector3 targetPoint;
        if (Physics.Raycast(aimTarget.position, aimTarget.forward, out aimHit, maxDistance, shootableLayers)) { targetPoint = aimHit.point; }
        else { targetPoint = aimTarget.position + aimTarget.forward * maxDistance; }
        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        RaycastHit gunHit;
        if (firePoint != null && Physics.Raycast(firePoint.position, shootDirection, out gunHit, maxDistance, shootableLayers))
        {
            SpawnImpactEffect(gunHit.point, Quaternion.LookRotation(gunHit.normal));

            // --- UPDATED ---
            // Look for a Health component
            Health targetHealth = gunHit.transform.GetComponent<Health>();
            if (targetHealth != null)
            {
                Debug.Log($"[{gameObject.name}] Locally hit Player {gunHit.transform.name}");
                targetHealth.TakeDamage(damage);
            }

            // Look for a Barrel component
            Barrel targetBarrel = gunHit.transform.GetComponent<Barrel>();
            if (targetBarrel != null)
            {
                Debug.Log($"[{gameObject.name}] Locally hit Barrel {gunHit.transform.name}");
                targetBarrel.TakeDamage(damage);
            }
            // --- END OF UPDATED SECTION ---
        }
    }

    IEnumerator Reload_Local()
    {
        isReloading = true;
        Debug.Log($"[{gameObject.name}] Starting local reload sequence...");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
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
        if (shakeOffsetTransform != null)
        {
            if (cameraShakeCoroutine != null) StopCoroutine(cameraShakeCoroutine);
            cameraShakeCoroutine = StartCoroutine(ShakeCameraOffset());
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Trying to shake camera, but Shake Offset Transform is not assigned!");
        }
    }

    IEnumerator ShakeCameraOffset()
    {
        if (shakeOffsetTransform == null) yield break;

        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            if (shakeOffsetTransform != null)
                shakeOffsetTransform.localPosition = shakeOffsetOriginalLocalPosition + new Vector3(x, y, 0);
            else
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (shakeOffsetTransform != null) shakeOffsetTransform.localPosition = shakeOffsetOriginalLocalPosition;
        cameraShakeCoroutine = null;
    }

    private void SpawnMuzzleFlash()
    {
        if (firePoint != null && muzzleFlashPrefab != null)
        {
            SpawnEffect(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Failed to spawn muzzle flash (missing firePoint or prefab).");
        }
    }

    private void SpawnImpactEffect(Vector3 position, Quaternion rotation)
    {
        if (impactEffectPrefab != null)
        {
            SpawnEffect(impactEffectPrefab, position, rotation);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Failed to spawn impact effect (missing prefab).");
        }
    }

    private void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (effectPrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] Attempted to spawn an effect, but the effect prefab is null!");
            return;
        }
        GameObject instance = Instantiate(effectPrefab, position, rotation, parent);
        Destroy(instance, destroyTimer);
    }

    private void TriggerHaptics()
    {
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
}
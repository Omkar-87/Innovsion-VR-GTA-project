using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
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

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public GameObject impactEffectPrefab;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

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
        // Play muzzle flash effect
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        RaycastHit hit;
        if (firePoint != null && Physics.Raycast(firePoint.position, firePoint.forward, out hit, maxDistance))
        {
            Debug.Log("Hit: " + hit.transform.name);

            // Instantiate impact effect at the hit point, rotated to face away from the surface
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

#if UNITY_EDITOR
            Debug.DrawRay(firePoint.position, firePoint.forward * hit.distance, Color.red, 1f);
#endif

            Health targetHealth = hit.transform.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
            }
        }
        else if (firePoint != null)
        {
#if UNITY_EDITOR
            Debug.DrawRay(firePoint.position, firePoint.forward * maxDistance, Color.green, 1f);
#endif
        }

        // Apply recoil impulse
        transform.localPosition -= Vector3.forward * recoilKickback;
        transform.localRotation *= Quaternion.Euler(-recoilUpKick, 0, 0);
    }
}
using UnityEngine;

public class AIWeaponController : MonoBehaviour
{
    [Header("Core References")]
    public Transform firePoint;

    [Header("Shooting Stats")]
    public LayerMask shootableLayers;
    public float fireRate = 5f;
    public float maxDistance = 100f;
    public int damage = 1;

    [Header("Effects (Prefabs)")]
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;
    public float destroyTimer = 1.5f;

    private float nextFireTime = 0f;

    // This is the only public method. The AI "Brain" calls this.
    public void Shoot(Vector3 targetPosition)
    {
        if (Time.time < nextFireTime)
        {
            return; // Still cooling down
        }

        nextFireTime = Time.time + 1f / fireRate;

        // Calculate shoot direction
        Vector3 shootDirection = (targetPosition - firePoint.position).normalized;

        // Spawn muzzle flash
        SpawnMuzzleFlash();

        // Perform the raycast
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, shootDirection, out hit, maxDistance, shootableLayers))
        {
            // Spawn impact effect
            SpawnImpactEffect(hit.point, Quaternion.LookRotation(hit.normal));

            // Check if we hit the player
            Health playerHealth = hit.transform.GetComponent<Health>();
            if (playerHealth != null)
            {
                // Make sure we're not hitting another AI
                if (hit.transform.GetComponent<EnemyAI>() == null)
                {
                    Debug.Log($"AI hit player {hit.transform.name}");
                    playerHealth.TakeDamage(damage);
                }
            }
        }
    }

    private void SpawnMuzzleFlash()
    {
        if (firePoint != null && muzzleFlashPrefab != null)
        {
            SpawnEffect(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
        }
    }

    private void SpawnImpactEffect(Vector3 position, Quaternion rotation)
    {
        if (impactEffectPrefab != null)
        {
            SpawnEffect(impactEffectPrefab, position, rotation);
        }
    }

    private void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation)
    {
        GameObject instance = Instantiate(effectPrefab, position, rotation);
        Destroy(instance, destroyTimer);
    }
}

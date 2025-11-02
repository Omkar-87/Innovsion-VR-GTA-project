using UnityEngine;

public class Barrel : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] int damageAmt = 50;
    [SerializeField] float blastRadius = 5f;
    [SerializeField] int startingHealth = 10;

    [Header("Effects")]
    [SerializeField] GameObject explosionEffectPrefab;
    [SerializeField] float effectDestroyTimer = 3f;

    private int currentHealth;
    private bool exploded = false;

    void Start()
    {
        currentHealth = startingHealth;
        exploded = false;
    }

    public void TakeDamage(int damageAmount)
    {
        if (exploded || currentHealth <= 0) return;

        currentHealth -= damageAmount;
        Debug.Log($"Barrel took {damageAmount} damage. Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Debug.Log("Barrel exploding.");

        // 1. Spawn the visual effect
        if (explosionEffectPrefab != null)
        {
            GameObject instance = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(instance, effectDestroyTimer);
        }

        // 2. Find and damage nearby objects
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, blastRadius);

        foreach (Collider hitCollider in hitColliders)
        {
            // --- UPDATED ---
            // Look for a player/mech with Health
            Health playerHealth = hitCollider.GetComponent<Health>();
            if (playerHealth != null)
            {
                Debug.Log($"Barrel explosion damaging player {hitCollider.name}");
                playerHealth.TakeDamage(damageAmt);
            }

            // Look for another barrel for chain reactions
            Barrel barrelHealth = hitCollider.GetComponent<Barrel>();
            if (barrelHealth != null && barrelHealth != this) // Don't make it explode itself again
            {
                Debug.Log($"Barrel explosion damaging other barrel {hitCollider.name}");
                barrelHealth.TakeDamage(damageAmt);
            }
            // --- END OF UPDATED SECTION ---
        }

        // 3. Destroy this barrel
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }
}
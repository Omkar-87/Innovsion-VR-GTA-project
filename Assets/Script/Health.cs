using UnityEngine;
using UnityEngine.UI; // REQUIRED for Slider
using UnityEngine.AI; // Required for NavMeshAgent

public class Health : MonoBehaviour
{
    [Header("Health Stats")]
    [SerializeField] int maxHealth = 100;
    private int currentHealth;

    [Header("UI Link")]
    public Slider healthSlider; // Public slot for your health bar

    public delegate void DamageTakenDelegate();
    public event DamageTakenDelegate OnDamaged;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        else
        {
            if (gameObject.CompareTag("Player"))
            {
                Debug.LogWarning("Player's Health script is missing a healthSlider reference!");
            }
        }
    }

    public bool IsAlive()
    {
        return !isDead;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"{gameObject.name} took {damageAmount} damage. Health: {currentHealth}");

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (OnDamaged != null)
        {
            OnDamaged();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} has died!");

        // --- THIS IS THE CORRECTED SECTION ---
        // It now looks for the "EnemyAI" script you are using
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.enabled = false; // Disable the "brain"
            if (GetComponent<NavMeshAgent>() != null) GetComponent<NavMeshAgent>().enabled = false; // Disable the "feet"
            if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false; // Disable the "body"

            // Optional: Destroy the AI after a few seconds
            // Destroy(gameObject, 5f);
        }
        // --- END OF CORRECTED SECTION ---

        // If this is the Player, we'd tell the GameManager
        if (gameObject.CompareTag("Player"))
        {
            Debug.Log("Player has died! Game Over logic would go here.");
            // Example of what you had (requires a GameManager script):
            /*
            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.PlayerDied();
            }
            */
        }
    }
}
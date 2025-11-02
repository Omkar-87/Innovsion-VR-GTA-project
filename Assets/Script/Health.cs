using UnityEngine;
using System.Collections;
using UnityEngine.UI; // For Slider, if you add it
using UnityEngine.AI; // Required for NavMeshAgent

public class Health : MonoBehaviour
{
    [SerializeField] int maxHealth = 100;
    private int currentHealth;

    // This is an event that other scripts (like our AI) can listen to.
    public delegate void DamageTakenDelegate();
    public event DamageTakenDelegate OnDamaged;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Helper property for other scripts to check if dead
    public bool IsAlive()
    {
        return !isDead;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return; // Already dead

        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} took {damageAmount} damage. Health: {currentHealth}");

        // Fire the OnDamaged event to notify listeners (our AI)
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

        // If this is an AI, we can disable its components
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.enabled = false;
            if (GetComponent<NavMeshAgent>() != null) GetComponent<NavMeshAgent>().enabled = false;
            if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;
        }

        // If this is the Player, we'd tell the GameManager
        if (gameObject.CompareTag("Player"))
        {
            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.PlayerDied();
            }
        }

        // You could also just destroy the AI after a delay
        // if (ai != null) Destroy(gameObject, 5f);
    }
}


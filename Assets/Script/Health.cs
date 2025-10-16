using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    public int maxHealth = 100;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);
    //public int currentHealth;

    void Start()
    {
        //currentHealth = maxHealth;
    }

    public void TakeDamage (int damageAmount)
    {

        if (!IsServer) return;

        currentHealth.Value -= damageAmount;
        

        Debug.Log(gameObject.name + " took " + damageAmount + " damage. Current health: " + currentHealth);

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " has died.");
        Destroy(gameObject);
    }
}
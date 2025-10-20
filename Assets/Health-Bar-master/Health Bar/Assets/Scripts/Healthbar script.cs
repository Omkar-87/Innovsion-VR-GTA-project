using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Image

public class PlayerHealthManager : MonoBehaviour
{
    [Header("Health Stats")]
    [Range(0, 1000)] // For a slider in the Inspector
    public float maxHealth = 1000f;
    [Range(0, 1000)] // For a slider in the Inspector
    public float currentHealth;

    [Header("UI Connections")]
    public Image healthFillImage; // Assign the HealthFill GameObject here

    // This function runs when the script first loads
    void Start()
    {
        // Set health to full at the start
        currentHealth = maxHealth;

        // Ensure UI is updated on start
        UpdateHealthUI();
    }

    // This is just for testing. Press Space to take damage, R to heal.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TakeDamage(50); // Example: Take 50 damage
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Heal(50); // Example: Heal 50 health
        }
    }

    // Call this function from other scripts (like an enemy) to deal damage
    public void TakeDamage(float amount)
    {
        currentHealth -= amount; // Reduce health

        // Make sure health doesn't go below 0
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        // Update the visual health bar
        UpdateHealthUI();

        // Check for death
        if (currentHealth == 0)
        {
            Debug.Log("Player has died!");
            // Add death logic here (e.g., game over screen)
        }
    }

    // Call this function to heal the player
    public void Heal(float amount)
    {
        currentHealth += amount; // Increase health

        // Make sure health doesn't go above max
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        UpdateHealthUI();
    }

    // This function updates the visual health bar
    private void UpdateHealthUI()
    {
        // Calculate the fill amount (a value between 0 and 1) for the Image component
        healthFillImage.fillAmount = currentHealth / maxHealth;
    }
}
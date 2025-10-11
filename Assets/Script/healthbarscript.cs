using UnityEngine;
using UnityEngine.UI; // Required to control UI elements like a Slider

public class TargetHealth : MonoBehaviour
{
    public float health = 100f;
    public Slider healthBarSlider; // This will hold our UI health bar

    private float maxHealth;

    void Start()
    {
        maxHealth = health;
        // Set up the health bar when the game starts
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = health;
        }
    }

    // This function will be called by the gun script
    public void TakeDamage(float amount)
    {
        health -= amount;

        // Update the UI slider's value
        if (healthBarSlider != null)
        {
            healthBarSlider.value = health;
        }

        if (health <= 0f)
        {
            Destroy(gameObject); // Destroy the object when health is zero
        }
    }
}
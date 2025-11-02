using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

// Changed from NetworkBehaviour to MonoBehaviour
public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;

    // Changed from NetworkVariable to a private int
    private int currentHealth;

    [Header("UI (Assign These in Inspector - Optional)")]
    public Slider healtbarFP;
    public Slider healthbarTP;
    public Image hitEffect;

    public GameManager gameManager;
    private Coroutine hitEffectCoroutine;

    public void FindGameManager()
    {
        GameObject gmObject = GameObject.FindGameObjectWithTag("GameController");
        if (gmObject != null)
        {
            gameManager = gmObject.GetComponent<GameManager>();
        }
    }

    // Changed from OnNetworkSpawn to Start
    void Start()
    {
        // Find the GameManager if it's not set
        if (gameManager == null)
        {
            FindGameManager();
        }

        if (gameManager == null)
        {
            Debug.LogError($"[{gameObject.name}] GameManager component not found in scene!", this);
        }

        // Set health and update UI
        currentHealth = maxHealth;
        UpdateHealthUI(currentHealth, currentHealth); // (previousValue, newValue)

        if (healtbarFP != null) healtbarFP.maxValue = maxHealth;
        if (healthbarTP != null) healthbarTP.maxValue = maxHealth;
    }

    // This is the public method your WeaponController will call
    public void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0) return; // Already dead

        int previousHealth = currentHealth;
        currentHealth -= damageAmount;

        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"{gameObject.name} took {damageAmount} damage. New health: {currentHealth}");

        // Manually call the UI update
        UpdateHealthUI(previousHealth, currentHealth);

        if (currentHealth <= 0 && previousHealth > 0)
        {
            Die();
        }
    }

    private void UpdateHealthUI(int previousValue, int newValue)
    {
        if (healtbarFP != null)
        {
            healtbarFP.value = newValue;
        }
        if (healthbarTP != null)
        {
            healthbarTP.value = newValue;
        }

        // Check if this script is on the local player by checking the camera/UI tags
        // (A simple way to know if it's "our" UI)
        bool isOurPlayer = (healtbarFP != null || hitEffect != null);

        // If we took damage, show the hit effect
        if (isOurPlayer && newValue < previousValue && newValue > 0 && hitEffect != null)
        {
            if (hitEffectCoroutine != null) StopCoroutine(hitEffectCoroutine);
            hitEffectCoroutine = StartCoroutine(ShowHitEffect());
        }
    }

    IEnumerator ShowHitEffect()
    {
        if (hitEffect == null) yield break;

        hitEffect.color = new Color(1f, 0f, 0f, 0.4f);
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.4f, 0f, elapsed / duration);
            hitEffect.color = new Color(hitEffect.color.r, hitEffect.color.g, hitEffect.color.b, alpha);
            yield return null;
        }
        hitEffect.color = new Color(hitEffect.color.r, hitEffect.color.g, hitEffect.color.b, 0f);
        hitEffectCoroutine = null;
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} has died. Notifying GameManager.");

        // We can just call the GameManager directly
        if (gameManager != null)
        {
            // You will need to update your GameManager to have a public PlayerDied()
            // or PlayerDied(ulong ownerId) method.
            // For an offline game, you might not even need the ID.

            // gameManager.PlayerDied(); // <--- Change this to match your offline GameManager
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Cannot notify GameManager of death - reference is null!");
        }

        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        // You might want to disable the whole object or just the controls
        // e.g., GetComponent<PlayerMovement>().enabled = false;
    }

    // Removed OnNetworkDespawn
}
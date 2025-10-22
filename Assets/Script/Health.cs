using UnityEngine;
using UnityEngine.UI;       
using Unity.Netcode;
using System.Collections;   
using UnityEngine.SceneManagement;

public class Health : NetworkBehaviour
{

    [Header("Health Settings")]
    public int maxHealth = 100;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("UI (Assign These in Inspector - Optional)")]
    public Slider healtbarFP;    
    public Slider healthbarTP;   
    public Image hitEffect;      

    public GameManager gameManager;
    private Coroutine hitEffectCoroutine;

    public void FindGameManager()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }
    public override void OnNetworkSpawn()
    {
        if (gameManager == null)
        {
            Debug.LogError($"[{gameObject.name}] GameManager component not found in scene!", this);
        }

        currentHealth.OnValueChanged += UpdateHealthUI;

        UpdateHealthUI(currentHealth.Value, currentHealth.Value);

        if (healtbarFP != null) healtbarFP.maxValue = maxHealth;
        if (healthbarTP != null) healthbarTP.maxValue = maxHealth;
    }

    public override void OnNetworkDespawn()
    {
        if (currentHealth != null)
        {
            currentHealth.OnValueChanged -= UpdateHealthUI;
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

        if (IsOwner && newValue < previousValue && newValue > 0 && hitEffect != null)
        {
            if (hitEffectCoroutine != null) StopCoroutine(hitEffectCoroutine);
            hitEffectCoroutine = StartCoroutine(ShowHitEffect());
        }
    }


    public void TakeDamage(int damageAmount)
    {
        if (!IsServer || currentHealth.Value <= 0) return;

        int previousHealth = currentHealth.Value;
        currentHealth.Value -= damageAmount;

        if (currentHealth.Value < 0) currentHealth.Value = 0;

        Debug.Log($"{gameObject.name} (Owner: {OwnerClientId}) took {damageAmount} damage. New health: {currentHealth.Value}");

        if (currentHealth.Value <= 0 && previousHealth > 0)
        {
            Die();
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
        Debug.Log($"{gameObject.name} (Owner: {OwnerClientId}) has died. Notifying GameManager.");

        if (IsServer)
        {
            if (gameManager != null)
            {
                gameManager.PlayerDied(OwnerClientId);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] Cannot notify GameManager of death - reference is null!");
            }
        }

        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
    }

}
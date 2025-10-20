using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Health : NetworkBehaviour
{
    [Header("Scene Settings")]
    public string gameOverSceneName = "Round Over";

    [Header("Health Settings")]
    public int maxHealth = 100;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);

    [Header("UI")]
    public Slider healtbarFP;
    public Slider healthbarTP;
    public Image hitEffect;

    private void Update()
    {
        healtbarFP.value = currentHealth.Value;
        healthbarTP.value = currentHealth.Value;
    }
    public void TakeDamage(int damageAmount)
    {
        if (!IsServer) return;
        if (currentHealth.Value <= 0) return;

        currentHealth.Value -= damageAmount;

        Debug.Log($"{gameObject.name} took {damageAmount} damage. Current health: {currentHealth.Value}");
        StartCoroutine(ShowHitEffect());
        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    IEnumerator ShowHitEffect()
    {
        hitEffect.color = new Color(hitEffect.color.r, hitEffect.color.g, hitEffect.color.b, 1.0f);
        yield return new WaitForSeconds(0.7f);
        float t = 1.0f;
        while (t >= 0.0f)
        {
            t -= 0.2f;
            yield return new WaitForSeconds(0.1f);
            hitEffect.color = new Color(hitEffect.color.r, hitEffect.color.g, hitEffect.color.b, Mathf.Max(0.0f, t));
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} has died. Triggering Game Over.");

        if (IsServer)
        {
            ulong losingPlayerId = OwnerClientId;

            NotifyGameOverClientRpc(losingPlayerId);

            StartCoroutine(LoadGameOverSceneAfterDelay());
        }
    }

    private System.Collections.IEnumerator LoadGameOverSceneAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        NetworkManager.Singleton.SceneManager.LoadScene(gameOverSceneName, LoadSceneMode.Single);
    }


    [ClientRpc]
    private void NotifyGameOverClientRpc(ulong losingPlayerId)
    {
        bool localPlayerWon = NetworkManager.Singleton.LocalClientId != losingPlayerId;

        GameResultData.DidWin = localPlayerWon;

        Debug.Log($"Game Over! Local Client Won: {localPlayerWon}");

    }
}

public static class GameResultData
{
    public static bool DidWin = false;
    // public static float MatchTime = 0f; // timer
}
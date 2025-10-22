using System.Collections;
using System.Collections.Generic; // Needed for List
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement; // Needed for GameResultData reference
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class GameManager : NetworkBehaviour
{
    [Header("UI References")]
    public TMP_Text countdownText;
    public GameObject countdownPanel;
    public TMP_Text matchTimerText;

    [Header("Game Settings")]
    public int countdownDuration = 3;
    public float matchDuration = 120f;
    public string gameOverSceneName = "Round Over";
    
    private NetworkVariable<int> startCountdownValue = new NetworkVariable<int>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server);

    private NetworkVariable<float> matchTimeRemaining = new NetworkVariable<float>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> isMatchOver = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool countdownStarted = false;


    public override void OnNetworkSpawn()
    {
        startCountdownValue.OnValueChanged += HandleStartCountdownChanged;
        matchTimeRemaining.OnValueChanged += HandleMatchTimerChanged;
        isMatchOver.OnValueChanged += HandleMatchOverChanged;

        HandleStartCountdownChanged(0, startCountdownValue.Value);
        HandleMatchTimerChanged(0, matchTimeRemaining.Value);

        if (IsServer && !countdownStarted)
        {
            StartCoroutine(StartRoundSequence_Server());
        }

        SetPlayerInputEnabled(false);

        
        foreach(var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            client.PlayerObject.gameObject.GetComponent<Health>().FindGameManager();
        }
    }

    public override void OnNetworkDespawn()
    {
        startCountdownValue.OnValueChanged -= HandleStartCountdownChanged;
        matchTimeRemaining.OnValueChanged -= HandleMatchTimerChanged;
        isMatchOver.OnValueChanged -= HandleMatchOverChanged;
    }

    private void HandleStartCountdownChanged(int previousValue, int newValue)
    {
        if (countdownText == null || countdownPanel == null) return;

        if (newValue > 0)
        {
            countdownPanel.SetActive(true);
            countdownText.text = newValue.ToString();
        }
        else if (newValue == 0)
        {
            countdownText.text = "RUMBLE!!";
            SetPlayerInputEnabled(true);
        }
        else // newValue < 0
        {
            countdownPanel.SetActive(false);
        }
    }

    private void HandleMatchTimerChanged(float previousValue, float newValue)
    {
        if (matchTimerText == null) return;

        if (newValue >= 0)
        {
            System.TimeSpan time = System.TimeSpan.FromSeconds(newValue);
            matchTimerText.text = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
            matchTimerText.gameObject.SetActive(true);
        }
        else
        {
            matchTimerText.text = "00:00";
        }
    }

    private void HandleMatchOverChanged(bool previousValue, bool newValue)
    {
        if (newValue == true)
        {
            Debug.Log("Match Over flag received by client/host.");
        }
    }


    private IEnumerator StartRoundSequence_Server()
    {
        countdownStarted = true;
        isMatchOver.Value = false;
        matchTimeRemaining.Value = matchDuration;
        startCountdownValue.Value = countdownDuration;
        Debug.Log("Server starting initial countdown...");

        while (startCountdownValue.Value > 0)
        {
            yield return new WaitForSeconds(1.0f);
            if (!IsServer) yield break;
            startCountdownValue.Value--;
        }

        yield return new WaitForSeconds(1.0f);
        if (!IsServer) yield break;
        startCountdownValue.Value = -1;

        Debug.Log("Server starting match timer...");

        while (matchTimeRemaining.Value > 0)
        {
            if (isMatchOver.Value)
            {
                Debug.Log("Match timer stopping early due to match end.");
                yield break;
            }

            yield return new WaitForSeconds(1.0f);
            if (!IsServer) yield break;

            if (!isMatchOver.Value)
            {
                matchTimeRemaining.Value--;
            }
            else
            {
                Debug.Log("Match timer stopping early due to match end (detected after wait).");
                yield break;
            }
        }

        if (!isMatchOver.Value)
        {
            isMatchOver.Value = true;
            Debug.Log("Server match timer ended. Determining winner by health.");
            DetermineWinnerByHealth();
        }
        else
        {
            Debug.Log("Match timer reached zero, but game already ended.");
        }
    }

    private void DetermineWinnerByHealth()
    {
        if (!IsServer) return;

        List<Health> playerHealths = new List<Health>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                Health health = client.PlayerObject.GetComponent<Health>();
                if (health != null)
                {
                    playerHealths.Add(health);
                }
            }
        }

        if (playerHealths.Count != 2)
        {
            Debug.LogError($"Expected 2 players with Health components, but found {playerHealths.Count}. Cannot determine winner by health.");
            EndGameClientRpc(ulong.MaxValue, ulong.MaxValue, true, matchDuration);
            StartCoroutine(LoadGameOverSceneAfterDelay());
            return;
        }

        Health player1 = playerHealths[0];
        Health player2 = playerHealths[1];

        ulong winnerId = ulong.MaxValue;
        ulong loserId = ulong.MaxValue;
        bool isTie = false;

        if (player1.currentHealth.Value > player2.currentHealth.Value)
        {
            winnerId = player1.OwnerClientId;
            loserId = player2.OwnerClientId;
            Debug.Log($"Player {winnerId} wins by health ({player1.currentHealth.Value} > {player2.currentHealth.Value})");
        }
        else if (player2.currentHealth.Value > player1.currentHealth.Value)
        {
            winnerId = player2.OwnerClientId;
            loserId = player1.OwnerClientId;
            Debug.Log($"Player {winnerId} wins by health ({player2.currentHealth.Value} > {player1.currentHealth.Value})");
        }
        else
        {
            isTie = true;
            Debug.Log($"Match ended in a TIE by health ({player1.currentHealth.Value} == {player2.currentHealth.Value})");
        }

        EndGameClientRpc(winnerId, loserId, isTie, matchDuration);
    }

    public void PlayerDied(ulong loserId)
    {
        if (!IsServer || isMatchOver.Value) return;

        isMatchOver.Value = true;

        float killTime = matchDuration - matchTimeRemaining.Value;

        ulong winnerId = ulong.MaxValue;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != loserId)
            {
                winnerId = client.ClientId;
                break;
            }
        }

        if (winnerId == ulong.MaxValue)
        {
            Debug.LogWarning($"Player {loserId} died, but couldn't determine winner. Treating as tie/error.");
            // EndGameClientRpc(ulong.MaxValue, loserId, true, killTime);
        }
        else
        {
            Debug.Log($"Player {loserId} died. Player {winnerId} wins! Kill time: {killTime:F1}s");
            EndGameClientRpc(winnerId, loserId, false, killTime);
        }


        StartCoroutine(LoadGameOverSceneAfterDelay());
    }


    [ClientRpc]
    private void EndGameClientRpc(ulong winnerId, ulong loserId, bool isTie, float finalTime)
    {
        if (isTie) { GameResultData.DidWin = false; }
        else { GameResultData.DidWin = (NetworkManager.Singleton.LocalClientId == winnerId); }
        GameResultData.MatchTime = finalTime;
        Debug.Log($"Game Over! Local Client Won: {GameResultData.DidWin}, Final Time: {finalTime:F1}s");
        SetPlayerInputEnabled(false);
    }

    private System.Collections.IEnumerator LoadGameOverSceneAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        if (IsServer)
        {
            Debug.Log($"Server loading scene: {gameOverSceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(gameOverSceneName, LoadSceneMode.Single);
        }
    }

    private void SetPlayerInputEnabled(bool isEnabled)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            TurretController turrentScript = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<TurretController>();
            GunController[] gunScripts = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentsInChildren<GunController>();
            DynamicMoveProvider moveScript = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<DynamicMoveProvider>();

            if (moveScript != null)
            {
                moveScript.enabled = isEnabled;
            }
            foreach (var gun in gunScripts)
            {
                gun.enabled = isEnabled;
            }
            if (gunScripts != null)
            {
                turrentScript.enabled = isEnabled;
            }
        }
    }
}
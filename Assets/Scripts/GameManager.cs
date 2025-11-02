using System.Collections;
using System.Collections.Generic; // Needed for List
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement; // Needed for loading scenes
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets; // For DynamicMoveProvider

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text countdownText;
    public GameObject countdownPanel;
    public TMP_Text matchTimerText;

    [Header("Game Settings")]
    public int countdownDuration = 3;
    public float matchDuration = 120f;
    public string gameOverSceneName = "Round Over";

    private int startCountdownValue;
    private float matchTimeRemaining;
    private bool isMatchOver = false;
    private bool countdownStarted = false;

    void Start()
    {
        startCountdownValue = countdownDuration;
        matchTimeRemaining = matchDuration;
        isMatchOver = false;

        UpdateCountdownUI(startCountdownValue);

        // --- FIX 1 ---
        // The method is named HandleMatchTimerChanged, not UpdateMatchTimerUI
        HandleMatchTimerChanged(matchTimeRemaining);
        // --- END FIX ---

        if (!countdownStarted)
        {
            StartCoroutine(StartRoundSequence());
        }

        SetPlayerInputEnabled(false);
    }

    public void PlayerDied()
    {
        if (isMatchOver) return;

        isMatchOver = true;
        Debug.Log("<color=red>Player has died! Game Over.</color>");

        EndGame(false);
    }

    private void EndGame(bool didWin)
    {
        float finalTime = matchDuration - matchTimeRemaining;
        GameResultData.DidWin = didWin;
        GameResultData.MatchTime = finalTime;

        Debug.Log($"Game Over! You Won: {didWin}, Final Time: {finalTime:F1}s");

        SetPlayerInputEnabled(false);

        StartCoroutine(LoadGameOverSceneAfterDelay());
    }

    private IEnumerator StartRoundSequence()
    {
        countdownStarted = true;
        isMatchOver = false;
        matchTimeRemaining = matchDuration;
        startCountdownValue = countdownDuration;
        Debug.Log("Starting initial countdown...");

        while (startCountdownValue > 0)
        {
            UpdateCountdownUI(startCountdownValue);
            yield return new WaitForSeconds(1.0f);
            startCountdownValue--;
        }

        UpdateCountdownUI(0);
        SetPlayerInputEnabled(true);
        yield return new WaitForSeconds(1.0f);

        UpdateCountdownUI(-1);
        Debug.Log("Match timer starting...");

        while (matchTimeRemaining > 0)
        {
            if (isMatchOver)
            {
                Debug.Log("Match timer stopping early, player died.");
                yield break;
            }

            // --- FIX 2 ---
            // The method is named HandleMatchTimerChanged, not UpdateMatchTimerUI
            HandleMatchTimerChanged(matchTimeRemaining);
            // --- END FIX ---

            yield return new WaitForSeconds(1.0f);
            matchTimeRemaining--;
        }

        if (!isMatchOver)
        {
            isMatchOver = true;
            Debug.Log("<color=green>Server match timer ended. Player survived! You Win!</color>");
            EndGame(true);
        }
    }

    private void UpdateCountdownUI(int newValue)
    {
        if (countdownText == null || countdownPanel == null) return;

        if (newValue > 0)
        {
            countdownPanel.SetActive(true);
            countdownText.text = newValue.ToString();
        }
        else if (newValue == 0)
        {
            countdownPanel.SetActive(true);
            countdownText.text = "RUMBLE!!";
        }
        else // newValue < 0
        {
            countdownPanel.SetActive(false);
        }
    }

    // This is the correct method name
    private void HandleMatchTimerChanged(float newValue)
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

    private IEnumerator LoadGameOverSceneAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);

        Debug.Log($"Loading scene: {gameOverSceneName}");

        SceneManager.LoadScene(gameOverSceneName, LoadSceneMode.Single);
    }

    private void SetPlayerInputEnabled(bool isEnabled)
    {
        // --- FIX 3 (Warnings) ---
        // Replaced obsolete FindObjectOfType with FindAnyObjectByType
        // and FindObjectsOfType with FindObjectsByType
        TurretController turrentScript = FindAnyObjectByType<TurretController>();

        WeaponController[] gunScripts = FindObjectsByType<WeaponController>(FindObjectsSortMode.None);

        DynamicMoveProvider moveScript = FindAnyObjectByType<DynamicMoveProvider>();
        // --- END FIX ---

        if (moveScript != null)
        {
            moveScript.enabled = isEnabled;
        }

        foreach (var gun in gunScripts)
        {
            gun.enabled = isEnabled;
        }

        if (turrentScript != null)
        {
            turrentScript.enabled = isEnabled;
        }
    }
}
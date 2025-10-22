using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

public class GameOverUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text resultText;
    public TMP_Text timeText;
    public TMP_InputField nameInput;
    public TMP_InputField phoneInput;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    void Start()
    {
        if (GameResultData.DidWin)
        {
            resultText.text = "YOU WIN !!";
        }
        else
        {
            resultText.text = "YOU LOSE !!";
        }

        if (timeText != null)
        {
            timeText.text = $"Match Time: {GameResultData.MatchTime:F1}s";
            timeText.gameObject.SetActive(true);
        }

    }

    public void SubmitAndReturn()
    {
        string playerName = nameInput.text;
        string phoneNumber = phoneInput.text;
        Debug.Log($"Player Name: {playerName}, Phone: {phoneNumber}");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            if (LobbyManager.Instance != null) Destroy(LobbyManager.Instance.gameObject);
            if (NetworkManager.Singleton != null) Destroy(NetworkManager.Singleton.gameObject);
        }
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
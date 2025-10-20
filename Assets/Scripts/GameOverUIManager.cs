using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

public class GameOverUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text resultText;
    // public TMP_Text timeText;   //Timer
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

        // if (timeText != null) timeText.gameObject.SetActive(false); // Hide timer text
    }

    public void SubmitAndReturn()
    {
        string playerName = nameInput.text;
        string phoneNumber = phoneInput.text;
        Debug.Log($"Player Name: {playerName}, Phone: {phoneNumber}");

        // Shut down the network connection ONLY IF NetworkManager exists
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();

            // Destroy persistent managers for a clean reset
            if (LobbyManager.Instance != null)
            {
                Destroy(LobbyManager.Instance.gameObject);
            }
            if (NetworkManager.Singleton != null) // Check again as it might be destroyed by LobbyManager
            {
                Destroy(NetworkManager.Singleton.gameObject);
            }
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
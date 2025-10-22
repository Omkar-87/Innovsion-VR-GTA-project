using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameOverUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text resultText;
    public TMP_Text timeText;
    public TMP_InputField nameInput;
    public TMP_InputField phoneInput;
    public Button submitButton;

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
        string time = timeText.text;
        time = time.Substring(12);

        if (submitButton != null) submitButton.interactable = false;
        Debug.Log("Starting form submission...");
        StartCoroutine(Post(playerName, phoneNumber, time));
    }

    private string formUrl = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSfjm3yUN-7g0ro8pyyWS4BedIYjfjWWook0AIim-OFDpZM8oQ/formResponse";
    private IEnumerator Post(string name, string number, string time)
    {
        WWWForm form = new WWWForm();
        form.AddField("entry.1856561751", name);
        form.AddField("entry.1584948338", number);
        form.AddField("entry.1564806056", time);

        using (UnityWebRequest www = UnityWebRequest.Post(formUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Form Submitted Succesfully");
            }
            else
            {
                Debug.Log("Error in submitting form: " + www.error);
            }

            Debug.Log("Shutting down network and returning to menu...");
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                if (LobbyManager.Instance != null) Destroy(LobbyManager.Instance.gameObject);
                if (NetworkManager.Singleton != null) Destroy(NetworkManager.Singleton.gameObject);
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
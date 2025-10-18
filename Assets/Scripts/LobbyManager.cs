using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject lobbyPanel;

    [Header("UI Text")]
    public TMP_Text joinCodeText;
    public TMP_Text player1NameText;
    public TMP_Text player2NameText;

    [Header("UI Input")]
    public TMP_InputField joinCodeInput;

    [Header("UI Buttons")]
    public GameObject startButton;

    [Header("Game Settings")]
    public string gameSceneName = "GameScene";

    private Lobby connectedLobby;
    private float lobbyPollTimer;
    private float lobbyHeartbeatTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        mainMenuPanel.SetActive(true);
        lobbyPanel.SetActive(false);

        await AuthenticatePlayerAsync();
    }

    async Task AuthenticatePlayerAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"<color=green>Signed in as: {AuthenticationService.Instance.PlayerId}</color>");
        }
        catch(Exception e)
        {
            Debug.LogException(e);
        }
    }


    public async void HostLobbyAsync()
    {
        try
        {
            // Create a Relay Allocation
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Create a Lobby
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = true,
                Data = new Dictionary<string, DataObject>
                {
                    {"JOIN_CODE_KEY", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) } // Store relay code in lobby data
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("Reef Rumble", 2, options);
            connectedLobby = lobby;

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Start the host
            NetworkManager.Singleton.StartHost();

            // Show the lobby UI
            ShowLobbyUI(true);
            UpdateLobbyUI();

            // Lobby maintenance (heartbeat and polling)
            StartCoroutine(HandleLobbyHeartbeat());
            HandleLobbyPollingAsync(); // DO NOT USE AWAIT : IT BREAKS FOR SOME REASON

            Debug.Log($"HOSTED: Lobby ID {lobby.Id} with Join Code {lobby.LobbyCode}");
            Debug.Log($"<color=red>{lobby.LobbyCode}</color>");
            joinCodeText.text = $"CODE: {lobby.LobbyCode}"; // Show the LOBBY code
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to host: {e.Message}");
        }
    }

    public async void JoinLobbyAsync()
    {
        string lobbyCode = joinCodeInput.text;

        try
        {
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            connectedLobby = lobby;

            string relayJoinCode = lobby.Data["JOIN_CODE_KEY"].Value;

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();

            ShowLobbyUI(false);

            HandleLobbyPollingAsync();

            Debug.Log($"JOINED: Lobby ID {lobby.Id}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join: {e.Message}");
        }
    }
    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private IEnumerator HandleLobbyHeartbeat()
    {
        while (connectedLobby != null && NetworkManager.Singleton.IsHost)
        {
            yield return new WaitForSeconds(15);
            LobbyService.Instance.SendHeartbeatPingAsync(connectedLobby.Id);
        }
    }

    private async Task HandleLobbyPollingAsync()
    {
        while (connectedLobby != null)
        {
            await Task.Delay(1100);

            try
            {
                connectedLobby = await LobbyService.Instance.GetLobbyAsync(connectedLobby.Id);

                UpdateLobbyUI();
            }
            catch (LobbyServiceException e)
            {
                Debug.Log($"Failed to poll lobby: {e.Message}");
                connectedLobby = null;
            }
        }
    }

    private void ShowLobbyUI(bool isHost)
    {
        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        startButton.SetActive(isHost);
    }

    private void UpdateLobbyUI()
    {
        if (connectedLobby == null) return;

        if (connectedLobby.Players.Count > 0)
        {
            player1NameText.text = $"Player 1 (Host)";
        }

        if (connectedLobby.Players.Count > 1)
        {
            player2NameText.text = $"Player 2 (Client)";
        }
    }
}

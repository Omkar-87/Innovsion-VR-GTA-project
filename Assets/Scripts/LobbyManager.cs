using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies; // Correct namespace
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; // Needed for scene management
using UnityEngine.UI; // Needed for Button

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

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
    public Button joinButton;

    [Header("Game Settings")]
    public string gameSceneName = "GameScene";
    public string mainMenuSceneName = "MainMenu";

    private Lobby connectedLobby;
    private bool isPolling = false;
    private bool isHeartbeating = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate LobbyManager found. Destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    async void Start()
    {
        if (Instance == this)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("LobbyManager Start: Already connected. Resuming Lobby UI.");
                ShowLobbyUI(NetworkManager.Singleton.IsHost);
                UpdateLobbyUI();
                ResumeLobbyMaintenance();
            }
            else
            {
                ShowMainMenuUI();
                await AuthenticatePlayerAsync();
            }
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Task.Run(async () => await LeaveLobbyAndShutdownAsync());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && connectedLobby != null)
            {
                Debug.Log("OnSceneLoaded(MainMenu): Still connected. Showing Lobby UI.");
                ShowLobbyUI(NetworkManager.Singleton.IsHost);
                UpdateLobbyUI();
                ResumeLobbyMaintenance();
            }
            else
            {
                Debug.Log("OnSceneLoaded(MainMenu): Not connected or lobby info lost. Showing Main Menu.");
                ShowMainMenuUI();
                connectedLobby = null;
                StopLobbyMaintenance();
            }
        }
        else
        {
            Debug.Log($"OnSceneLoaded({scene.name}): Stopping lobby maintenance.");
            StopLobbyMaintenance();
        }
    }
    // --------------------------

    private void ShowMainMenuUI()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
    }

    private void ShowLobbyUI(bool isHost)
    {
        if (connectedLobby == null && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.LogWarning("ShowLobbyUI called but not connected to a lobby. Showing Main Menu instead.");
            ShowMainMenuUI();
            return;
        }
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (startButton != null) startButton.SetActive(isHost);
    }

    private void ResumeLobbyMaintenance()
    {
        if (connectedLobby != null)
        {
            if (NetworkManager.Singleton.IsHost && !isHeartbeating) StartCoroutine(HandleLobbyHeartbeat());
            if (!isPolling) HandleLobbyPollingAsync();
        }
    }

    private void StopLobbyMaintenance()
    {
        StopAllCoroutines();
        isPolling = false;
        isHeartbeating = false;
    }

    async Task AuthenticatePlayerAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            // Check if already signed in
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"<color=green>Signed in as: {AuthenticationService.Instance.PlayerId}</color>");
            }
            else
            {
                Debug.Log($"<color=yellow>Already signed in as: {AuthenticationService.Instance.PlayerId}</color>");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async void HostLobbyAsync()
    {
        // Prevent accidental double-hosting
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("Already connected or hosting.");
            return;
        }

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = true,
                Data = new Dictionary<string, DataObject>
                { { "JOIN_CODE_KEY", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) } }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("ReefRumble", 2, options);
            connectedLobby = lobby;

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

            NetworkManager.Singleton.StartHost();

            ShowLobbyUI(true);
            UpdateLobbyUI();
            ResumeLobbyMaintenance();

            Debug.Log($"HOSTED: Lobby ID {lobby.Id} with Lobby Code {lobby.LobbyCode}");
            if (joinCodeText != null) joinCodeText.text = $"CODE: {lobby.LobbyCode}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to host: {e.Message}");
            await LeaveLobbyAndShutdownAsync();
            ShowMainMenuUI();
        }
    }

    public async void JoinLobbyAsync()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            Debug.LogWarning("Already connected or hosting.");
            return;
        }

        string lobbyCode = joinCodeInput?.text;
        if (string.IsNullOrEmpty(lobbyCode))
        {
            Debug.LogError("Please enter lobby code");
            return;
        }

        if (joinButton != null) joinButton.interactable = false;

        try
        {
            Debug.Log($"Attempting to join lobby with code: {lobbyCode}");
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            connectedLobby = lobby;
            Debug.Log($"Lobby joined successfully! Lobby ID: {lobby.Id}");

            string relayJoinCode = lobby.Data["JOIN_CODE_KEY"].Value;
            Debug.Log($"Got Relay code: {relayJoinCode}");

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            Debug.Log("Relay joined successfully!");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key,
                allocation.ConnectionData, allocation.HostConnectionData);

            NetworkManager.Singleton.StartClient();
            Debug.Log("Netcode client started!");

            ShowLobbyUI(false);
            ResumeLobbyMaintenance();

        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join: {e.Message}");
            if (joinButton != null) joinButton.interactable = true;
            await LeaveLobbyAndShutdownAsync();
            ShowMainMenuUI();
        }
    }

    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost || connectedLobby == null) return;

        if (connectedLobby.Players.Count != connectedLobby.MaxPlayers)
        {
            Debug.LogWarning("Waiting for more players...");
            return;
        }

        Debug.Log("Host is starting the game...");
        StopLobbyMaintenance();

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);

    }

    private IEnumerator HandleLobbyHeartbeat()
    {
        if (isHeartbeating) yield break;
        isHeartbeating = true;
        Debug.Log("Starting lobby heartbeat...");

        while (connectedLobby != null && NetworkManager.Singleton.IsHost)
        {
            yield return new WaitForSeconds(15);
            try
            {
                if (connectedLobby != null && NetworkManager.Singleton.IsHost)
                {
                    Debug.Log("Sending heartbeat ping...");
                    LobbyService.Instance.SendHeartbeatPingAsync(connectedLobby.Id);
                }
                else
                {
                    Debug.Log("Heartbeat condition no longer met, stopping.");
                    break;
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.Log($"Heartbeat failed: {e.Message}. Stopping heartbeat.");
                connectedLobby = null;
                ShowMainMenuUI();
                break;
            }
        }
        isHeartbeating = false;
        Debug.Log("Lobby heartbeat stopped.");
    }

    private async Task HandleLobbyPollingAsync()
    {
        if (isPolling) return;
        isPolling = true;
        Debug.Log("Starting lobby polling...");

        while (isPolling && connectedLobby != null)
        {
            await Task.Delay(1100); // Wait

            if (!isPolling || connectedLobby == null) break;

            try
            {
                connectedLobby = await LobbyService.Instance.GetLobbyAsync(connectedLobby.Id);
                UpdateLobbyUI();

                // Check if host left (only relevant for clients)
                if (!NetworkManager.Singleton.IsHost && connectedLobby != null && !PlayerInLobby(connectedLobby.HostId, connectedLobby))
                {
                    Debug.Log("Host left the lobby. Returning to main menu.");
                    await LeaveLobbyAndShutdownAsync();
                    ShowMainMenuUI();
                    break;
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.Log($"Failed to poll lobby: {e.Message}. Returning to main menu.");
                connectedLobby = null;
                await LeaveLobbyAndShutdownAsync();
                ShowMainMenuUI();
                break;
            }
        }
        isPolling = false;
        Debug.Log("Lobby polling stopped.");
    }


    private bool PlayerInLobby(string playerId, Lobby lobby)
    {
        if (lobby == null || lobby.Players == null) return false;
        foreach (var player in lobby.Players)
        {
            if (player.Id == playerId) return true;
        }
        return false;
    }

    public async Task LeaveLobbyAndShutdownAsync()
    {
        StopLobbyMaintenance();

        string playerId = AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
        string lobbyId = connectedLobby?.Id;

        connectedLobby = null;

        // Leave Lobby
        if (!string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(lobbyId))
        {
            try
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                {
                    Debug.Log($"Host attempting to delete lobby: {lobbyId}");
                    await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                    Debug.Log("Lobby deleted by host.");
                }
                else
                {
                    Debug.Log($"Client attempting to leave lobby: {lobbyId}");
                    await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
                    Debug.Log("Client left lobby.");
                }
            }
            catch (LobbyServiceException e)
            {
                // Log error but continue shutdown
                Debug.LogWarning($"Error leaving/deleting lobby: {e.Message}");
            }
        }

        // Shutdown NetworkManager
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Shutting down NetworkManager...");
            NetworkManager.Singleton.Shutdown();
            Debug.Log("NetworkManager shut down.");
        }
    }

    private void UpdateLobbyUI()
    {
        if (connectedLobby == null || lobbyPanel == null || !lobbyPanel.activeInHierarchy) return;

        // Reset names first
        if (player1NameText != null) player1NameText.text = "Waiting...";
        if (player2NameText != null) player2NameText.text = "Waiting...";

        if (connectedLobby.Players.Count > 0)
        {
            // Assume first player is always host for display
            if (player1NameText != null) player1NameText.text = $"Player 1 (Host)";
        }

        if (connectedLobby.Players.Count > 1)
        {
            // Assume second player is client
            if (player2NameText != null) player2NameText.text = $"Player 2 (Client)";
        }

        // Enable Start button only if host and lobby is full
        bool canStart = NetworkManager.Singleton.IsHost && connectedLobby.Players.Count == connectedLobby.MaxPlayers;
        if (startButton != null) startButton.SetActive(canStart);
    }
}
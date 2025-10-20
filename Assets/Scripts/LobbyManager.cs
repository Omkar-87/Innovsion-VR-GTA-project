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
    // --- Singleton Pattern ---
    public static LobbyManager Instance { get; private set; }
    // -------------------------

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
    public Button joinButton; // Added reference

    [Header("Game Settings")]
    public string gameSceneName = "GameScene";
    public string mainMenuSceneName = "MainMenu"; // Added reference to self

    private Lobby connectedLobby;
    private bool isPolling = false;
    private bool isHeartbeating = false;

    // --- Make Persistent ---
    void Awake()
    {
        // Implement Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate LobbyManager found. Destroying this one.");
            Destroy(gameObject); // Destroy duplicate
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Make this object persistent

        // Subscribe to scene load events ONCE
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    // -----------------------

    // --- Check State on Start ---
    async void Start()
    {
        // This logic runs only once when the *first* instance is created
        if (Instance == this) // Only run full init if this is the singleton
        {
            // If NetworkManager exists and is already connected (Host or Client)
            // it means we somehow started not in the main menu or returned unexpectedly.
            // For robustness, ensure we show the correct UI based on state.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                // We are already in a lobby, ensure UI is correct and resume maintenance
                Debug.Log("LobbyManager Start: Already connected. Resuming Lobby UI.");
                ShowLobbyUI(NetworkManager.Singleton.IsHost);
                UpdateLobbyUI(); // Update names immediately
                ResumeLobbyMaintenance();
            }
            else // Otherwise, this is the first time loading, show main menu
            {
                ShowMainMenuUI();
                await AuthenticatePlayerAsync();
            }
        }
    }
    // --------------------------

    // --- Handle Cleanup ---
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // Cleanly leave lobby and shutdown if this object is destroyed
        // Use Task.Run to avoid issues destroying async operations
        Task.Run(async () => await LeaveLobbyAndShutdownAsync());
    }
    // ----------------------

    // --- Handle Scene Transitions ---
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName) // Check against variable
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && connectedLobby != null)
            {
                // Still connected, ensure lobby UI is shown
                Debug.Log("OnSceneLoaded(MainMenu): Still connected. Showing Lobby UI.");
                ShowLobbyUI(NetworkManager.Singleton.IsHost);
                UpdateLobbyUI(); // Refresh names
                ResumeLobbyMaintenance();
            }
            else
            {
                // Not connected (or lobby info lost), ensure main menu is shown
                Debug.Log("OnSceneLoaded(MainMenu): Not connected or lobby info lost. Showing Main Menu.");
                ShowMainMenuUI();
                connectedLobby = null; // Clear lobby data
                StopLobbyMaintenance();
            }
        }
        else // If loading any OTHER scene (like the GameScene or GameOverScene)
        {
            // Stop polling/heartbeat while in another scene
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
            if (!isPolling) HandleLobbyPollingAsync(); // Fire and forget
        }
    }

    private void StopLobbyMaintenance()
    {
        StopAllCoroutines(); // Stops Heartbeat
        isPolling = false;   // Polling loop will exit
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
                IsPrivate = true, // Keep it private, rely on Lobby Code
                Data = new Dictionary<string, DataObject>
                { { "JOIN_CODE_KEY", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) } }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("MyMechGameLobby", 2, options); // Simple name
            connectedLobby = lobby;

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

            NetworkManager.Singleton.StartHost();

            ShowLobbyUI(true);
            UpdateLobbyUI();
            ResumeLobbyMaintenance(); // Start heartbeat and polling

            Debug.Log($"HOSTED: Lobby ID {lobby.Id} with Lobby Code {lobby.LobbyCode}");
            if (joinCodeText != null) joinCodeText.text = $"CODE: {lobby.LobbyCode}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to host: {e.Message}");
            await LeaveLobbyAndShutdownAsync(); // Attempt cleanup
            ShowMainMenuUI(); // Go back to main menu
        }
    }

    public async void JoinLobbyAsync()
    {
        // Prevent accidental double-joining
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

        if (joinButton != null) joinButton.interactable = false; // Disable button immediately

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
            ResumeLobbyMaintenance(); // Start polling

        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join: {e.Message}");
            if (joinButton != null) joinButton.interactable = true; // Re-enable button on failure
            await LeaveLobbyAndShutdownAsync(); // Attempt cleanup
            ShowMainMenuUI(); // Go back to main menu
        }
    }

    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost || connectedLobby == null) return;

        // Check if lobby is full (optional, but good practice)
        if (connectedLobby.Players.Count != connectedLobby.MaxPlayers)
        {
            Debug.LogWarning("Waiting for more players...");
            return;
        }

        Debug.Log("Host is starting the game...");
        StopLobbyMaintenance(); // Stop polling/heartbeat before loading

        // Load the game scene via NetworkManager
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private IEnumerator HandleLobbyHeartbeat()
    {
        if (isHeartbeating) yield break; // Prevent multiple coroutines
        isHeartbeating = true;
        Debug.Log("Starting lobby heartbeat...");

        while (connectedLobby != null && NetworkManager.Singleton.IsHost)
        {
            yield return new WaitForSeconds(15);
            try
            {
                // Check again after wait, in case we disconnected
                if (connectedLobby != null && NetworkManager.Singleton.IsHost)
                {
                    Debug.Log("Sending heartbeat ping...");
                    LobbyService.Instance.SendHeartbeatPingAsync(connectedLobby.Id);
                }
                else
                {
                    Debug.Log("Heartbeat condition no longer met, stopping.");
                    break; // Exit loop if no longer host or lobby is null
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.Log($"Heartbeat failed: {e.Message}. Stopping heartbeat.");
                connectedLobby = null; // Assume lobby is gone
                ShowMainMenuUI(); // Go back to main menu
                break; // Exit loop
            }
        }
        isHeartbeating = false;
        Debug.Log("Lobby heartbeat stopped.");
    }

    private async Task HandleLobbyPollingAsync()
    {
        if (isPolling) return; // Prevent multiple loops
        isPolling = true;
        Debug.Log("Starting lobby polling...");

        while (isPolling && connectedLobby != null)
        {
            await Task.Delay(1100); // Wait

            // Check again after wait, in case we stopped polling or disconnected
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
                await LeaveLobbyAndShutdownAsync(); // Attempt shutdown
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

    // Consolidated cleanup function
    public async Task LeaveLobbyAndShutdownAsync()
    {
        StopLobbyMaintenance(); // Stop loops

        string playerId = AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
        string lobbyId = connectedLobby?.Id; // Safely get lobby ID

        connectedLobby = null; // Clear local lobby reference immediately

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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient) // Check IsClient as IsHost might already be false after lobby deletion
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
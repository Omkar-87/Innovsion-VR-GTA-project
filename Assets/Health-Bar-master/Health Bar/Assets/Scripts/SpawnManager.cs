using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    public List<Transform> spawnPoints;
    int spawnIndex = 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        OnClientConnected(NetworkManager.Singleton.LocalClientId);
    }

    public void OnClientConnected(ulong clientId)
    {
        if (spawnPoints.Count == 0) return;

        NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

        Transform playerTransform = playerNetworkObject.transform;

        Transform spawnPoint = spawnPoints[spawnIndex];

        playerTransform.position = spawnPoint.position;
        playerTransform.rotation = spawnPoint.rotation;

        spawnIndex++;
        spawnIndex %= spawnPoints.Count;

        playerTransform.gameObject.SetActive(false);
        playerTransform.gameObject.SetActive(true);
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
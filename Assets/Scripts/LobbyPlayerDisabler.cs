using UnityEngine;
using Unity.Netcode; // Optional, but useful for checks
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class LobbyPlayerDisabler : MonoBehaviour
{
    // List the EXACT names of GameObjects or Component Types to disable
    // Ensure these match the actual names in your Player prefab
    public string[] objectNamesToDisableInLobby = {
        "FirstPerson_View",
        "ThirdPersonView",
        "MechMovementController", // Example Component Name
        "GunController",          // Example Component Name
        "TurretController",       // Example Component Name
        "Camera"                  // Disable the Camera component itself
    };

    public GameObject evenetSystem;
    // Update is called once per frame
    void Update()
    {
        BaseInput baseInput = evenetSystem.GetComponent<BaseInput>();
        if(baseInput) baseInput.enabled = false;
        // Find all active GameObjects tagged as "Player" in the current scene
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            player.SetActive(false);
            // Optional: Only disable components on player objects owned by the LOCAL client
            // NetworkObject netObj = player.GetComponent<NetworkObject>();
            // if (netObj != null && netObj.IsOwner) {
            //DisablePlayerComponents(player);
            // }
            // If you want to disable for ALL player objects in the lobby scene (including remote ones):
            // DisablePlayerComponents(player);
        }
    }

    void DisablePlayerComponents(GameObject playerObject)
    {
        // Disable Camera first
        Camera playerCamera = playerObject.GetComponentInChildren<Camera>(true); // true = include inactive
        if (playerCamera != null && playerCamera.gameObject.activeSelf)
        {
            playerCamera.gameObject.SetActive(false);
            // Debug.Log($"Disabled Camera on {playerObject.name}");
        }

        // Disable other specified objects/components
        foreach (string name in objectNamesToDisableInLobby)
        {
            if (name == "Camera") continue; // Already handled

            // Try finding child GameObject by name first
            Transform childTransform = FindDeepChild(playerObject.transform, name);
            if (childTransform != null && childTransform.gameObject.activeSelf)
            {
                childTransform.gameObject.SetActive(false);
                // Debug.Log($"Disabled GameObject {name} on {playerObject.name}");
            }
            else
            {
                // If no GameObject found, try finding component by type name
                try
                {
                    System.Type componentType = System.Type.GetType(name + ", Assembly-CSharp"); // Assumes script is in main assembly
                    if (componentType != null)
                    {
                        Component component = playerObject.GetComponentInChildren(componentType, false); // FALSE = only active
                        if (component is Behaviour behaviour && behaviour.enabled)
                        {
                            behaviour.enabled = false;
                            //  Debug.Log($"Disabled Component {name} on {playerObject.name}");
                        }
                    }
                }
                catch { /* Ignore errors finding type */ }
            }
        }
    }

    // Helper to find child transform even if nested
    private Transform FindDeepChild(Transform parent, string name)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(parent);
        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();
            if (current.name == name) return current;
            foreach (Transform child in current) queue.Enqueue(child);
        }
        return null;
    }
}
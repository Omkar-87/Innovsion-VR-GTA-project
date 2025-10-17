using Unity.Netcode;
using UnityEngine;

public class ViewManager : NetworkBehaviour
{
    // Drag your "FirstPerson_View" GameObject here
    public GameObject firstPersonView;

    // Drag your "ThirdPerson_View" GameObject here
    public GameObject thirdPersonView;

    // We'll store the layer numbers here
    private int localPlayerMechLayer;
    private int remotePlayerMechLayer;

    void Start()
    {
        // Get the integer values for our layer names
        localPlayerMechLayer = LayerMask.NameToLayer("LocalPlayerMech");
        remotePlayerMechLayer = LayerMask.NameToLayer("RemotePlayerMech");
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // I am the owner.
            // Move my third-person mech to the "LocalPlayerMech" layer,
            // which my camera is set to hide.
            SetLayerRecursively(thirdPersonView, localPlayerMechLayer);
        }
        else
        {
            // I am not the owner.
            // 1. Hide this player's cockpit and first-person items from me.
            firstPersonView.SetActive(false);

            // 2. Make sure their mech is on the "RemotePlayerMech" layer
            // (which my camera is set to show).
            SetLayerRecursively(thirdPersonView, remotePlayerMechLayer);
        }
    }

    // Helper function to set the layer for an object and all its children
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
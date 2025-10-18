using Unity.Netcode;
using UnityEngine;

public class ViewManager : NetworkBehaviour
{
    public GameObject firstPersonView;

    public GameObject thirdPersonView;

    public int localPlayerMechLayer;
    public int remotePlayerMechLayer;

    void Start()
    {
        //localPlayerMechLayer = LayerMask.NameToLayer("LocalPlayerMech");
        //remotePlayerMechLayer = LayerMask.NameToLayer("RemotePlayerMech");
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log("Moving to layer " + localPlayerMechLayer);
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
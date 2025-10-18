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
            firstPersonView.SetActive(false);
            SetLayerRecursively(thirdPersonView, remotePlayerMechLayer);
        }
    }

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
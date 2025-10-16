using Unity.Netcode;
using UnityEngine;

public class AutoCameraDisable : NetworkBehaviour
{
    public GameObject cameraObj;
    
    public override void OnNetworkSpawn()
    {
        if(!IsOwner)
            cameraObj.SetActive(false);
    }
}

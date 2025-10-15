using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // Make sure this line is present
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class NetworkInputHandler : NetworkBehaviour
{
    public ContinuousTurnProvider continuousTurnProvider;
    public DynamicMoveProvider dynamicMoveProvider;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            if (continuousTurnProvider != null) continuousTurnProvider.enabled = false;
            if (dynamicMoveProvider != null) dynamicMoveProvider.enabled = false;
        }
    }
}
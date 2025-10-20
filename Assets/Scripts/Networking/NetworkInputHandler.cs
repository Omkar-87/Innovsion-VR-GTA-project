using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion; // You need this
using UnityEngine.XR.Interaction.Toolkit; // And this
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class NetworkInputHandler : NetworkBehaviour
{
    [Header("Movement Components")]
    public CharacterController characterController;
    public DynamicMoveProvider dynamicMoveProvider;
    public XRBodyTransformer bodyTransformer;
    public LocomotionMediator locomotionMediator;
    // Add any other movement scripts (like ContinuousTurnProvider) here

    [Header("Remote Client Collider")]
    public BoxCollider capsuleCollider; // Drag your new collider here


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            // --- I AM THE OWNER ---
            // Enable all my movement scripts and my CharacterController.
            if (characterController != null) characterController.enabled = true;
            if (dynamicMoveProvider != null) dynamicMoveProvider.enabled = true;
            if (bodyTransformer != null) bodyTransformer.enabled = true;
            if (locomotionMediator != null) locomotionMediator.enabled = true;

            // Disable the simple collider, I'm using the CharacterController.
            if (capsuleCollider != null) capsuleCollider.enabled = false;
        }
        else
        {
            // --- I AM A REMOTE CLIENT (a "puppet") ---
            // Disable all my movement scripts so they don't fight the network.
            if (characterController != null) characterController.enabled = false;
            if (dynamicMoveProvider != null) dynamicMoveProvider.enabled = false;
            if (bodyTransformer != null) bodyTransformer.enabled = false;
            if (locomotionMediator != null) locomotionMediator.enabled = false;

            // Enable the simple collider so I'm still solid in the world.
            if (capsuleCollider != null) capsuleCollider.enabled = true;
        }
    }
}
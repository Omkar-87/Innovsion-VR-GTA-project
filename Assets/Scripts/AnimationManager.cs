using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationManager : NetworkBehaviour
{
    public Animator animator;

    public InputActionProperty moveAction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // Inside AnimationManager.cs

    public override void OnNetworkSpawn() // Or use Start() if not inheriting NetworkBehaviour directly
    {
        base.OnNetworkSpawn(); // If using OnNetworkSpawn

        // Log ownership status immediately
        Debug.Log($"[{gameObject.name}] OnNetworkSpawn called. IsOwner = {IsOwner}, OwnerClientId = {OwnerClientId}, LocalClientId = {NetworkManager.Singleton.LocalClientId}");

        // You might need to find the animator if the script is now on the root
        // animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator not found!");
        }
    }

    void Update()
    {
        // --- PUT THE CHECK BACK ---
        if (!IsOwner) return;
        // -------------------------

        Vector2 move = moveAction.action.ReadValue<Vector2>();
        // Only log if there's actual input to avoid spam
        if (move.sqrMagnitude > 0.01f)
        {
            Debug.Log($"Owner {OwnerClientId} Move Input: {move}");
        }
        if (animator != null)
        {
            animator.SetFloat("x", move.x);
            animator.SetFloat("y", move.y);
        }
    }
}

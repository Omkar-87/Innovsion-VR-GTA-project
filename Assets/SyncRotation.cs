using Unity.Netcode;
using UnityEngine;

public class SyncRotation : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform thirdPersonView;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        thirdPersonView.rotation = transform.rotation;
    }
}

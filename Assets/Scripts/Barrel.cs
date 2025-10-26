using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic; // Needed for List
using JetBrains.Annotations;

[RequireComponent(typeof(NetworkObject))]
public class Barrel : NetworkBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] int damageAmt = 50;
    [SerializeField] float blastRadius = 5f;
    [SerializeField] int startingHealth = 10;

    [Header("Effects")]
    [SerializeField] GameObject explosionEffectPrefab;
    [SerializeField] float effectDestroyTimer = 3f;

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server);

    private bool exploded = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = startingHealth;
            exploded = false;
        }
    }

    [ServerRpc(RequireOwnership = false)] 
    public void TakeDamage_ServerRpc(int damageAmount)
    {
        if (!IsServer || exploded || currentHealth.Value <= 0) return;

        currentHealth.Value -= damageAmount;
        Debug.Log($"Barrel {NetworkObjectId} took {damageAmount} damage. Health: {currentHealth.Value}");

        if (currentHealth.Value <= 0)
        {
            Explode_ServerRpc();
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void Explode_ServerRpc()
    {
        if (exploded) return;
        exploded = true;

        Debug.Log($"Barrel {NetworkObjectId} exploding on server.");

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, blastRadius);
        List<ulong> damagedPlayerIds = new List<ulong>();
        List<ulong> damagedBarrelIds = new List<ulong>();

        foreach (Collider hitCollider in hitColliders)
        {
            Health playerHealth = hitCollider.GetComponent<Health>();
            Barrel barellHealth = hitCollider.GetComponent<Barrel>();
            if (playerHealth != null)
            {
                ulong ownerId = playerHealth.OwnerClientId;
                if (!damagedPlayerIds.Contains(ownerId))
                {
                    Debug.Log($"Barrel explosion damaging player {ownerId}");
                    playerHealth.TakeDamage(damageAmt);
                    damagedPlayerIds.Add(ownerId);
                }
            }
            
            if(barellHealth != null)
            {
                ulong barrelId = barellHealth.OwnerClientId;
                if(!damagedBarrelIds.Contains(barrelId))
                {
                    barellHealth.TakeDamage_ServerRpc(damageAmt);
                    damagedBarrelIds.Add(barrelId);
                }
            }
        }

        Explode_ClientRpc(transform.position);
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void Explode_ClientRpc(Vector3 explosionPosition)
    {
        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId}: Playing explosion effect for barrel at {explosionPosition}");
        if (explosionEffectPrefab != null)
        {
            GameObject instance = Instantiate(explosionEffectPrefab, explosionPosition, Quaternion.identity);
            Destroy(instance, effectDestroyTimer);
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }
}
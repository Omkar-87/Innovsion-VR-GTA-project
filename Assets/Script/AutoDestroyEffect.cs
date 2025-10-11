using UnityEngine;

// This script ensures that the GameObject is destroyed after the particle system on it has finished playing.
[RequireComponent(typeof(ParticleSystem))]
public class AutoDestroyEffect : MonoBehaviour
{
    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        // Check if the particle system is still alive
        if (ps != null && !ps.IsAlive())
        {
            // If it has finished, destroy the GameObject
            Destroy(gameObject);
        }
    }
}
using UnityEngine;

public class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Get the AudioSource that’s already on this GameObject
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("[AudioManager] No AudioSource found! Please add one to the same GameObject.");
        }
        else
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    /// <summary>
    /// Plays a one-shot sound clip globally.
    /// </summary>
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Tried to play a null clip!");
            return;
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// Optionally stop all sounds playing from the global AudioSource.
    /// </summary>
    public void StopAllSounds()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
}

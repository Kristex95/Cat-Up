using UnityEngine;

public class OceanSoundManager : MonoBehaviour
{
    public Transform player; // Reference to the player transform
    public float maxVolume = 1f; // Maximum volume of the ocean sound
    public float minHeight = 0f; // Height at which the ocean sound is at maximum volume
    public float maxHeight = 100f; // Height at which the ocean sound is at minimum volume

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        AdjustVolumeBasedOnHeight();
    }

    void AdjustVolumeBasedOnHeight()
    {
        float playerHeight = player.position.y;
        float t = Mathf.InverseLerp(minHeight, maxHeight, playerHeight);
        audioSource.volume = Mathf.Lerp(maxVolume, 0, t);
    }
}

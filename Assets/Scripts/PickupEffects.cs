using UnityEngine;

public class PickupEffects : MonoBehaviour
{
    [Header("Pickup Success Effects")]
    public GameObject pickupParticleEffect;
    public AudioClip pickupSound;
    
    [Header("Pickup Fail Effects")]
    public GameObject failParticleEffect;
    public AudioClip failSound;
    
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    public void PlayPickupSuccess(Vector3 position)
    {
        // Play particle effect
        if (pickupParticleEffect != null)
        {
            GameObject effect = Instantiate(pickupParticleEffect, position, Quaternion.identity);
            Destroy(effect, 3f); // Clean up after 3 seconds
        }
        
        // Play sound
        if (pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }
    }
    
    public void PlayPickupFail(Vector3 position)
    {
        // Play particle effect
        if (failParticleEffect != null)
        {
            GameObject effect = Instantiate(failParticleEffect, position, Quaternion.identity);
            Destroy(effect, 3f); // Clean up after 3 seconds
        }
        
        // Play sound
        if (failSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(failSound);
        }
    }
}
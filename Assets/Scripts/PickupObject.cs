using UnityEngine;

public class PickupObject : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float size = 1f;
    public AudioClip pickupSFX;
    public AudioClip failSFX;
    public GameObject pickupVFX;
    public GameObject failVFX;
    
    private bool isPickedUp = false;
    private Collider[] objectColliders;
    private Rigidbody objectRigidbody;
    private AudioSource audioSource;
    
    void Start()
    {
        objectColliders = GetComponents<Collider>();
        objectRigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        
        // Ensure this object has the pickup tag
        if (!gameObject.CompareTag("Pickup"))
        {
            Debug.LogWarning($"PickupObject on {gameObject.name} should have 'Pickup' tag!");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (isPickedUp) return;
        
        // Check if the other object has a PlayerController
        var player = other.GetComponent<MonoBehaviour>();
        if (player != null && player.GetType().Name == "PlayerController")
        {
            // Use reflection to call CanPickup method
            var canPickupMethod = player.GetType().GetMethod("CanPickup");
            if (canPickupMethod != null)
            {
                bool canPickup = (bool)canPickupMethod.Invoke(player, new object[] { size });
                if (canPickup)
                {
                    PerformPickup(player);
                }
                else
                {
                    PlayFailEffects();
                }
            }
        }
    }
    
    void PerformPickup(MonoBehaviour player)
    {
        isPickedUp = true;
        
        // Play pickup sound
        if (pickupSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSFX);
        }
        
        // Play pickup VFX
        if (pickupVFX != null)
        {
            Instantiate(pickupVFX, transform.position, Quaternion.identity);
        }
        
        // Parent to player
        transform.SetParent(player.transform);
        
        // Disable all colliders
        if (objectColliders != null)
        {
            foreach (Collider col in objectColliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }
        
        // Disable rigidbody physics
        if (objectRigidbody != null)
        {
            objectRigidbody.isKinematic = true;
        }
        
        // Notify player of successful pickup using reflection
        var onPickupSuccessMethod = player.GetType().GetMethod("OnPickupSuccess");
        if (onPickupSuccessMethod != null)
        {
            onPickupSuccessMethod.Invoke(player, new object[] { this });
        }
    }
    
    void PlayFailEffects()
    {
        // Play fail sound
        if (failSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(failSFX);
        }
        
        // Play fail VFX
        if (failVFX != null)
        {
            Instantiate(failVFX, transform.position, Quaternion.identity);
        }
    }
    
    public void ResetPickup()
    {
        isPickedUp = false;
        
        // Re-enable all colliders
        if (objectColliders != null)
        {
            foreach (Collider col in objectColliders)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }
        }
        
        // Re-enable rigidbody physics
        if (objectRigidbody != null)
        {
            objectRigidbody.isKinematic = false;
        }
        
        // Unparent from player
        transform.SetParent(null);
    }
}
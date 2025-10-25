using UnityEngine;
using System.Collections;

public class PickupObject : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float size = 1f;
    public float pointValue = 10f;
    public float screenShakeIntensity = 0.1f;
    public AudioClip pickupSFX;
    public AudioClip failSFX;
    public GameObject pickupVFX;
    public GameObject failVFX;
    
    [Header("Timer Settings")]
    [Tooltip("Time in seconds before the picked up object deactivates. Set to 0 to disable auto-deactivation.")]
    public float deactivationTimer = 5f;
    [Tooltip("Duration of the shrinking animation before deactivation.")]
    public float shrinkAnimationDuration = 1f;
    [Tooltip("Animation curve for the shrinking effect.")]
    public AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    private bool isPickedUp = false;
    private Collider[] objectColliders;
    private Rigidbody objectRigidbody;
    private AudioSource audioSource;
    private Coroutine deactivationCoroutine;
    private Vector3 originalScale;
    
    void Start()
    {
        objectColliders = GetComponents<Collider>();
        objectRigidbody = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        originalScale = transform.localScale;
        
        // Ensure this object has the pickup tag
        if (!gameObject.CompareTag("Pickup"))
        {
            Debug.LogWarning($"PickupObject on {gameObject.name} should have 'Pickup' tag!");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (isPickedUp) return;
        
        // Look for PlayerController on the colliding object or its parent
        MonoBehaviour player = other.GetComponent<MonoBehaviour>();
        if (player == null || player.GetType().Name != "PlayerController")
        {
            // If not found on the colliding object, check the parent
            Transform parent = other.transform.parent;
            if (parent != null)
            {
                player = parent.GetComponent<MonoBehaviour>();
            }
        }
        
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
        
        // Parent to player's pickup parent (if it exists) or player itself
        Transform pickupParent = player.transform.Find("Pickup Parent");
        if (pickupParent != null)
        {
            transform.SetParent(pickupParent);
        }
        else
        {
            transform.SetParent(player.transform);
        }
        
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
        
        // Start deactivation timer if set
        if (deactivationTimer > 0f)
        {
            deactivationCoroutine = StartCoroutine(DeactivationTimer());
        }
        
        // Notify player of successful pickup using reflection
        var onPickupSuccessMethod = player.GetType().GetMethod("OnPickupSuccess");
        if (onPickupSuccessMethod != null)
        {
            onPickupSuccessMethod.Invoke(player, new object[] { this });
        }
    }
    
    IEnumerator DeactivationTimer()
    {
        // Wait for the main timer duration
        yield return new WaitForSeconds(deactivationTimer);
        
        // Start shrinking animation
        yield return StartCoroutine(ShrinkAndDeactivate());
    }
    
    IEnumerator ShrinkAndDeactivate()
    {
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;
        
        while (elapsedTime < shrinkAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / shrinkAnimationDuration;
            float curveValue = shrinkCurve.Evaluate(progress);
            
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, 1f - curveValue);
            
            yield return null;
        }
        
        // Ensure scale is exactly zero
        transform.localScale = Vector3.zero;
        
        // Deactivate the object
        gameObject.SetActive(false);
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
        
        // Stop deactivation coroutine if running
        if (deactivationCoroutine != null)
        {
            StopCoroutine(deactivationCoroutine);
            deactivationCoroutine = null;
        }
        
        // Reset scale to original
        transform.localScale = originalScale;
        
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
    
    public float GetPointValue()
    {
        return pointValue;
    }
    
    public float GetScreenShakeIntensity()
    {
        return screenShakeIntensity;
    }
}
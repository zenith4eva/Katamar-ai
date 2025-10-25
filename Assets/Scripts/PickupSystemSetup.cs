using UnityEngine;

[System.Serializable]
public class PickupSystemSetup : MonoBehaviour
{
    [Header("Setup Instructions")]
    [TextArea(10, 20)]
    public string setupInstructions = @"
PICKUP SYSTEM SETUP GUIDE:

1. CREATE PICKUP OBJECTS:
   - Create GameObjects for pickup items (cubes, spheres, etc.)
   - Add 'Pickup' tag to these objects
   - Add PickupObject script component
   - Add Collider (set as Trigger)
   - Add Rigidbody
   - Add AudioSource component
   - Configure size value (smaller = easier to pickup)

2. CONFIGURE PLAYER:
   - Ensure Player has PlayerController script
   - Set the 'size' value (larger = can pickup bigger objects)
   - Assign pickupParent transform (optional, defaults to player transform)

3. SETUP AUDIO/VFX:
   - Create AudioClip assets for pickup/fail sounds
   - Create Particle System prefabs for effects
   - Assign these to PickupObject components

4. TESTING:
   - Player size >= object size = pickup success
   - Player size < object size = pickup fail with effects

TIPS:
- Use different size values to create pickup difficulty progression
- Larger objects should have higher size requirements
- Consider making player size increase after successful pickups
";

    [Header("Quick Setup Tools")]
    public GameObject pickupPrefab;
    public float[] testSizes = { 0.5f, 1f, 1.5f, 2f };
    
    [ContextMenu("Create Test Pickup Objects")]
    void CreateTestPickups()
    {
        for (int i = 0; i < testSizes.Length; i++)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = $"Pickup_Size_{testSizes[i]}";
            pickup.transform.position = new Vector3(i * 2f, 1f, 0f);
            pickup.tag = "Pickup";
            
            // Add required components
            pickup.AddComponent<PickupObject>();
            pickup.AddComponent<AudioSource>();
            
            // Configure PickupObject
            PickupObject pickupScript = pickup.GetComponent<PickupObject>();
            pickupScript.size = testSizes[i];
            
            // Make collider a trigger
            Collider col = pickup.GetComponent<Collider>();
            col.isTrigger = true;
            
            // Add rigidbody
            Rigidbody rb = pickup.AddComponent<Rigidbody>();
            rb.useGravity = true;
            
            Debug.Log($"Created test pickup: {pickup.name} with size {testSizes[i]}");
        }
    }
    
    [ContextMenu("Setup Player for Pickup System")]
    void SetupPlayer()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            // Ensure player has proper setup
            if (player.size <= 0)
            {
                player.size = 1f;
                Debug.Log("Set player size to 1.0");
            }
            
            // Add pickup effects component if not present
            if (player.GetComponent<PickupEffects>() == null)
            {
                player.gameObject.AddComponent<PickupEffects>();
                Debug.Log("Added PickupEffects component to player");
            }
            
            Debug.Log("Player setup complete!");
        }
        else
        {
            Debug.LogError("No PlayerController found in scene!");
        }
    }
}
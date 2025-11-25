using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;
    private bool isFrozen = false;
    private Vector3 startPosition;
    private float spawnProtectionTime = 0.1f;
    private float spawnTimer = 0f;
    
    // Distance tracking
    private float totalDistanceTraveled = 0f;
    private Vector3 lastPosition;
    
    // Distance display reference
    private DistanceDisplay distanceDisplay;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        startPosition = transform.position;
        lastPosition = startPosition;
        
        // Better collision detection
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Get distance display reference
        distanceDisplay = FindObjectOfType<DistanceDisplay>();
        
        if (distanceDisplay == null)
        {
            Debug.LogWarning("DistanceDisplay not found in scene!");
        }
    }

    private void Update()
    {
        // Countdown grace period
        if (spawnTimer < spawnProtectionTime)
            spawnTimer += Time.deltaTime;
        
        // Calculate distance traveled this frame
        if (!isFrozen)
        {
            float frameDistance = Vector3.Distance(lastPosition, transform.position);
            totalDistanceTraveled += frameDistance;
            lastPosition = transform.position;
            
            UpdateDistanceUI();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore collisions during grace period
        if (spawnTimer < spawnProtectionTime) return;
        if (isFrozen) return;

        // Final distance calculation
        CalculateFinalDistances();
        
        // Check if hit Player B
        PlayerBHandler playerB = collision.gameObject.GetComponent<PlayerBHandler>();
        if (playerB != null)
        {
            playerB.Die();
            Debug.Log("HIT! Player B defeated!");
        }

        FreezeProjectile();
    }

    private void CalculateFinalDistances()
    {
        // Horizontal distance (X-axis only)
        float horizontalDistance = Mathf.Abs(transform.position.x - startPosition.x);
        
        // Update UI with final distances via DistanceDisplay
        if (distanceDisplay != null)
        {
            distanceDisplay.UpdateProjectileDistances(horizontalDistance, totalDistanceTraveled);
        }
        else
        {
            Debug.LogWarning("DistanceDisplay is null, cannot update projectile distances!");
        }
        
        Debug.Log($"Horizontal Distance: {horizontalDistance:F2}m");
        Debug.Log($"Trajectory Distance: {totalDistanceTraveled:F2}m");
    }

    private void UpdateDistanceUI()
    {
        if (isFrozen) return;
        
        // Calculate current horizontal distance
        float currentHorizontalDistance = Mathf.Abs(transform.position.x - startPosition.x);
        
        // Update UI in real-time via DistanceDisplay
        if (distanceDisplay != null)
        {
            distanceDisplay.UpdateProjectileDistances(currentHorizontalDistance, totalDistanceTraveled);
        }
    }

    private void FreezeProjectile()
    {
        isFrozen = true;

        // Stop physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Disable collisions
        col.enabled = false;

        // Make semi-transparent
        SetTransparency(0.3f);
    }

    private void SetTransparency(float alpha)
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        Material mat = rend.material;
        Color color = mat.color;
        color.a = alpha;
        mat.color = color;

        // Enable transparency
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;
    }

    private void OnDestroy()
    {
        // Clear projectile distances when destroyed
        if (distanceDisplay != null)
        {
            distanceDisplay.ClearProjectileDistances();
        }
    }
}
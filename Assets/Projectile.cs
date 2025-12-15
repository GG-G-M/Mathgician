using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Visual Effects")]
    [Tooltip("Effect when projectile hits ground")]
    public GameObject groundImpactEffectPrefab;
    [Tooltip("Lifetime of ground impact effect (seconds)")]
    public float groundImpactEffectDuration = 1f;
    [Tooltip("Effect when projectile hits player")]
    public GameObject playerHitEffectPrefab;
    [Tooltip("Lifetime of player hit effect (seconds)")]
    public float playerHitEffectDuration = 1f;
    [Tooltip("Midair effect attached while projectile is flying (e.g., CFXR Electrified 3)")]
    public GameObject midairEffectPrefab;
    
    private Rigidbody rb;
    private Collider col;
    private bool isFrozen = false;
    private bool hasCollided = false; // Prevent duplicate collision handling
    private Vector3 startPosition;
    private float spawnProtectionTime = 0.1f;
    private float spawnTimer = 0f;
    
    // Distance tracking
    private float totalDistanceTraveled = 0f;
    private Vector3 lastPosition;
    
    // Distance display reference
    private DistanceDisplay distanceDisplay;
    
    // Physics display reference
    private ProjectilePhysicsDisplay physicsDisplay;
    private GameObject midairEffectInstance;
    
    // Turn manager reference
    private TurnManager turnManager;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        startPosition = transform.position;
        lastPosition = startPosition;
        
        // Better collision detection for high-speed projectiles
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = true;
        
        // Get distance display reference
        distanceDisplay = FindFirstObjectByType<DistanceDisplay>();
        
        if (distanceDisplay == null)
        {
            Debug.LogWarning("DistanceDisplay not found in scene!");
        }
        
        // Get or add physics display component
        physicsDisplay = GetComponent<ProjectilePhysicsDisplay>();
        if (physicsDisplay == null)
        {
            physicsDisplay = gameObject.AddComponent<ProjectilePhysicsDisplay>();
            Debug.Log("✅ ProjectilePhysicsDisplay added automatically!");
        }

        // Attach midair effect if assigned
        if (midairEffectPrefab != null)
        {
            midairEffectInstance = Instantiate(midairEffectPrefab, transform.position, Quaternion.identity, transform);
        }
    }
    
    public void SetTurnManager(TurnManager manager)
    {
        turnManager = manager;
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
        if (hasCollided) return; // Prevent duplicate collision handling
        
        hasCollided = true; // Mark as collided immediately

        // Final distance calculation
        CalculateFinalDistances();
        
        // Check if hit a player
        PlayerHandler playerHandler = collision.gameObject.GetComponent<PlayerHandler>();
        bool hitPlayer = playerHandler != null;
        
        // Spawn appropriate visual effect
        Vector3 impactPosition = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        if (hitPlayer)
        {
            // Spawn player hit explosion effect
            if (playerHitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(playerHitEffectPrefab, impactPosition, Quaternion.identity);
                Destroy(hitEffect, playerHitEffectDuration);
            }
            
            playerHandler.Die();
            Debug.Log($"HIT! {collision.gameObject.name} has been defeated!");
        }
        else
        {
            // Spawn ground impact AOE effect
            if (groundImpactEffectPrefab != null)
            {
                GameObject groundEffect = Instantiate(groundImpactEffectPrefab, impactPosition, Quaternion.identity);
                Destroy(groundEffect, groundImpactEffectDuration);
            }
        }

        FreezeProjectile();

        // Hide projectile visuals immediately on impact
        Renderer projectileRenderer = GetComponent<Renderer>();
        if (projectileRenderer != null)
        {
            projectileRenderer.enabled = false;
        }
        // Remove midair effect
        if (midairEffectInstance != null)
        {
            Destroy(midairEffectInstance);
            midairEffectInstance = null;
        }
        
        // Notify physics display of landing to finalize timings/labels
        if (physicsDisplay != null)
        {
            physicsDisplay.OnProjectileLanded();
        }
        
        // Trigger camera shake
        TurnManager turnMgr = FindFirstObjectByType<TurnManager>();
        if (turnMgr != null && turnMgr.cameraShake != null)
        {
            turnMgr.cameraShake.TriggerShake(hitPlayer);
        }
        
        // Notify turn manager that projectile has landed
        if (turnManager != null)
        {
            turnManager.OnProjectileFinished(hitPlayer, collision.gameObject);
        }
        
        // Always switch turns after ground hit (camera behavior controlled by autoSwitchPerspective)
        if (!hitPlayer)
        {
            Invoke(nameof(TriggerTurnSwitch), 1.5f);
        }
    }
    
    // Fallback for high-speed collisions that might be missed
    private void OnCollisionStay(Collision collision)
    {
        // If we're somehow in collision but haven't handled it yet, treat it as a collision
        if (!hasCollided && !isFrozen && spawnTimer >= spawnProtectionTime)
        {
            Debug.LogWarning("Collision caught by OnCollisionStay fallback - handling now");
            OnCollisionEnter(collision);
        }
    }
    
    private void TriggerTurnSwitch()
    {
        if (turnManager != null)
        {
            // Only switch turns if game isn't over
            turnManager.SwitchTurnsAfterLanding();
        }
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

        // Stop physics completely
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        
        // ★★★ NEW: Freeze all constraints to prevent any movement ★★★
        rb.constraints = RigidbodyConstraints.FreezeAll;

        // Disable collisions
        col.enabled = false;

        // Make semi-transparent
        SetTransparency(0.3f);
        
        Debug.Log("⏸️ Projectile frozen at position: " + transform.position);
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
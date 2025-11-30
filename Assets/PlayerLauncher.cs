using UnityEngine;
using UnityEngine.UIElements;

public class PlayerLauncher : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Trail Prefab")]
    public GameObject trailPrefab;
    
    [Header("Camera")]
    public CameraHandler cameraHandler;
    
    [Header("Turn Manager")]
    public TurnManager turnManager;
    
    [Header("Game Mode Manager")]
    public GameModeManager gameModeManager;
    
    [Header("Player Settings")]
    public bool isFacingLeft = false; // Set TRUE for Player B, FALSE for Player A

    private TextField angleField;
    private TextField velocityField;
    private Button fireButton;

    private GameObject currentProjectile;
    private GameObject currentTrail;

    private void Start()
    {
        var root = uiDocument.rootVisualElement;

        angleField = root.Q<TextField>("angleField");
        velocityField = root.Q<TextField>("velocityField");
        fireButton = root.Q<Button>("fireButton");

        if (fireButton != null)
            fireButton.clicked += FireProjectile;
        else
            Debug.LogError("FireButton not found in UI Document!");
            
        // Auto-find camera if not assigned
        if (cameraHandler == null)
        {
            cameraHandler = FindFirstObjectByType<CameraHandler>();
            if (cameraHandler != null)
            {
                Debug.Log("CameraHandler auto-found!");
            }
            else
            {
                Debug.LogWarning("CameraHandler not found! Camera following won't work.");
            }
        }
        
        // Auto-find turn manager if not assigned
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
            if (turnManager != null)
            {
                Debug.Log("TurnManager auto-found!");
            }
            else
            {
                Debug.LogWarning("TurnManager not found! Turn system won't work.");
            }
        }
        
        // Auto-find game mode manager if not assigned
        if (gameModeManager == null)
        {
            gameModeManager = FindFirstObjectByType<GameModeManager>();
            if (gameModeManager != null)
            {
                Debug.Log("GameModeManager auto-found!");
            }
            else
            {
                Debug.LogWarning("GameModeManager not found! Using manual input only.");
            }
        }
    }

    private void FireProjectile()
    {
        // ★★★ CHECK 1: Verify this launcher is enabled (current turn) ★★★
        if (!enabled)
        {
            Debug.LogWarning("This launcher is disabled - not this player's turn!");
            return;
        }
        
        // ★★★ CHECK 2: Check if this player can fire ★★★
        if (turnManager != null && !turnManager.CanCurrentPlayerFire())
        {
            Debug.LogWarning("Cannot fire - not your turn or on cooldown!");
            return;
        }
        
        Debug.Log($"🔥 {gameObject.name} is firing!");
        
        // Clean up previous projectile and trail
        if (currentProjectile != null)
            Destroy(currentProjectile);
        if (currentTrail != null)
            Destroy(currentTrail);

        // Get angle and velocity from game mode manager
        float angle, velocity;
        
        if (gameModeManager != null)
        {
            angle = gameModeManager.GetAngle();
            velocity = gameModeManager.GetVelocity();
            
            if (angle < 0)
            {
                Debug.LogWarning("Invalid angle input!");
                return;
            }
            if (velocity < 0)
            {
                Debug.LogWarning("Invalid velocity input!");
                return;
            }
        }
        else
        {
            // Fallback to manual input if no game mode manager
            if (!float.TryParse(angleField.value, out angle))
            {
                Debug.LogWarning("Invalid angle input!");
                return;
            }
            if (!float.TryParse(velocityField.value, out velocity))
            {
                Debug.LogWarning("Invalid velocity input!");
                return;
            }
        }

        // Spawn projectile
        currentProjectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        
        // Apply velocity
        Rigidbody rb = currentProjectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Projectile prefab missing Rigidbody!");
            return;
        }

        // ★★★ FIX: Flip angle for Player B (facing left) ★★★
        float actualAngle = angle;
        if (isFacingLeft)
        {
            // Player B: Convert angle to fire leftward
            // 45° becomes 135° (180° - 45°)
            actualAngle = 180f - angle;
            Debug.Log($"🔄 Player B angle flipped: {angle}° → {actualAngle}°");
        }

        float rad = actualAngle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
        rb.linearVelocity = direction * velocity;
        
        Debug.Log($"🚀 Firing at angle: {actualAngle}°, velocity: {velocity} m/s, direction: {direction}");

        // Prevent collision with player
        Collider playerCol = GetComponent<Collider>();
        Collider projCol = currentProjectile.GetComponent<Collider>();
        if (playerCol != null && projCol != null)
            Physics.IgnoreCollision(playerCol, projCol);

        // Spawn and setup trail
        if (trailPrefab != null)
        {
            currentTrail = Instantiate(trailPrefab, currentProjectile.transform.position, Quaternion.identity);
            
            ProjectileTrailFollower follower = currentTrail.GetComponent<ProjectileTrailFollower>();
            if (follower == null)
            {
                follower = currentTrail.AddComponent<ProjectileTrailFollower>();
            }
            
            follower.target = currentProjectile.transform;
        }
        
        // Set the projectile's owner (for turn manager)
        Projectile projectileScript = currentProjectile.GetComponent<Projectile>();
        if (projectileScript != null && turnManager != null)
        {
            projectileScript.SetTurnManager(turnManager);
        }
        
        // Tell camera to follow this projectile
        if (cameraHandler != null)
        {
            cameraHandler.NotifyProjectileFired(currentProjectile.transform);
            Debug.Log("✅ Notified camera about new projectile!");
        }
        
        // Notify turn manager that projectile was fired
        if (turnManager != null)
        {
            turnManager.OnProjectileFired();
        }
    }
}
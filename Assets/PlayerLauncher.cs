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
    }

    private void FireProjectile()
    {
        // Clean up previous projectile and trail
        if (currentProjectile != null)
            Destroy(currentProjectile);
        if (currentTrail != null)
            Destroy(currentTrail);

        // Validate input
        if (!float.TryParse(angleField.value, out float angle))
        {
            Debug.LogWarning("Invalid angle input!");
            return;
        }
        if (!float.TryParse(velocityField.value, out float velocity))
        {
            Debug.LogWarning("Invalid velocity input!");
            return;
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

        float rad = angle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
        rb.linearVelocity = direction * velocity;

        // Prevent collision with player
        Collider playerCol = GetComponent<Collider>();
        Collider projCol = currentProjectile.GetComponent<Collider>();
        if (playerCol != null && projCol != null)
            Physics.IgnoreCollision(playerCol, projCol);

        // Spawn and setup trail (WITH DEBUG)
        if (trailPrefab != null)
        {
            currentTrail = Instantiate(trailPrefab, currentProjectile.transform.position, Quaternion.identity);
            Debug.Log($"Trail spawned: {currentTrail != null}");
            
            // Make trail follow projectile
            ProjectileTrailFollower follower = currentTrail.GetComponent<ProjectileTrailFollower>();
            if (follower == null)
            {
                follower = currentTrail.AddComponent<ProjectileTrailFollower>();
                Debug.Log("Added ProjectileTrailFollower component");
            }
            
            follower.target = currentProjectile.transform;
            Debug.Log($"Trail follower target set: {follower.target != null}");
        }
        else
        {
            Debug.LogError("Trail Prefab not assigned in PlayerLauncher!");
        }
    }
}
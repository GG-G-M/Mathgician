using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;
    private bool isFrozen = false;
    private Vector3 startPosition;
    private float spawnProtectionTime = 0.1f;
    private float spawnTimer = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        startPosition = transform.position;
        
        // Better collision detection
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Update()
    {
        // Countdown grace period
        if (spawnTimer < spawnProtectionTime)
            spawnTimer += Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore collisions during grace period
        if (spawnTimer < spawnProtectionTime) return;
        if (isFrozen) return;

        // Calculate distance traveled
        float distance = Vector3.Distance(startPosition, transform.position);
        Debug.Log($"Projectile traveled: {distance:F2} units");

        // Check if hit Player B
        PlayerBHandler playerB = collision.gameObject.GetComponent<PlayerBHandler>();
        if (playerB != null)
        {
            playerB.Die();
            Debug.Log("HIT! Player B defeated!");
        }

        FreezeProjectile();
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
}
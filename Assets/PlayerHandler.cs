using UnityEngine;

public class PlayerHandler : MonoBehaviour
{
    [Header("Player Info")]
    public string playerName = "Player"; // Set to "Player A" or "Player B" in inspector
    public bool isAlive = true;
    
    [Header("Death Effects")]
    [Tooltip("Magic circle that appears around player on death")]
    public GameObject magicCirclePrefab;
    [Tooltip("Duration before magic circle disappears")]
    public float magicCircleDuration = 3f;
    
    [Tooltip("Player death effect (Plexus AOE) that appears after magic circle")]
    public GameObject playerDeathEffectPrefab;
    [Tooltip("Delay before spawning player death effect after magic circle")]
    public float playerDeathEffectDelay = 0.5f;
    [Tooltip("Duration before player death effect disappears")]
    public float playerDeathEffectDuration = 3f;

    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        Debug.Log($"{playerName} has been hit and defeated!");
        
        // Visual feedback - make player semi-transparent
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = rend.material;
            Color color = mat.color;
            color.a = 0.3f;
            mat.color = color;
        }

        // Optional: Disable collider so it can't be hit again
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
        
        // Spawn death effects
        SpawnDeathEffects();
    }
    
    private void SpawnDeathEffects()
    {
        Vector3 playerPosition = transform.position;
        
        // Spawn magic circle surrounding the player
        if (magicCirclePrefab != null)
        {
            GameObject magicCircle = Instantiate(magicCirclePrefab, playerPosition, Quaternion.identity);
            // Position at ground level
            Vector3 circlePos = magicCircle.transform.position;
            circlePos.y = 0f;
            magicCircle.transform.position = circlePos;
            
            // Stop looping on particle systems
            StopLoopingEffects(magicCircle);
            
            Destroy(magicCircle, magicCircleDuration);
        }
        
        // Spawn player death effect slightly delayed for dramatic effect
        if (playerDeathEffectPrefab != null)
        {
            Invoke(nameof(SpawnPlayerDeathEffect), playerDeathEffectDelay);
        }
    }
    
    private void SpawnPlayerDeathEffect()
    {
        Vector3 playerPosition = transform.position;
        GameObject deathEffect = Instantiate(playerDeathEffectPrefab, playerPosition, Quaternion.identity);
        // Position at ground level
        Vector3 effectPos = deathEffect.transform.position;
        effectPos.y = 0f;
        deathEffect.transform.position = effectPos;
        
        // Stop looping on particle systems
        StopLoopingEffects(deathEffect);
        
        Destroy(deathEffect, playerDeathEffectDuration);
    }
    
    private void StopLoopingEffects(GameObject effectObject)
    {
        // Find all particle systems in the effect and set them to not loop
        ParticleSystem[] particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            var main = ps.main;
            main.loop = false;
        }
    }
    
    public string GetPlayerName()
    {
        return playerName;
    }
}
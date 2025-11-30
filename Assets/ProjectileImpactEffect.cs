using UnityEngine;

public class ProjectileImpactEffect : MonoBehaviour
{
    [Header("Impact Effects")]
    public GameObject impactEffectPrefab; // Your explosion/magic particle effect
    public AudioClip impactSound;
    public float effectDuration = 2f;
    
    private AudioSource audioSource;
    private bool hasPlayedEffect = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // Call this from Projectile.OnCollisionEnter()
    public void PlayImpactEffect(Vector3 impactPosition, Vector3 impactNormal)
    {
        if (hasPlayedEffect) return;
        hasPlayedEffect = true;
        
        // Spawn particle effect
        if (impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(impactEffectPrefab, impactPosition, Quaternion.LookRotation(impactNormal));
            
            // Auto-destroy effect after duration
            Destroy(effect, effectDuration);
            
            Debug.Log("💥 Impact effect spawned!");
        }
        
        // Play sound
        if (impactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(impactSound);
        }
    }
    
    // Alternative: Play effect at projectile's current position
    public void PlayImpactEffectHere()
    {
        PlayImpactEffect(transform.position, Vector3.up);
    }
}
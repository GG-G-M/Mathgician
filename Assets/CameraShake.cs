using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("Enable or disable camera shake")]
    public bool shakeEnabled = true;
    
    [Tooltip("Intensity of shake for ground impacts")]
    public float groundHitIntensity = 0.3f;
    
    [Tooltip("Intensity of shake for player hits")]
    public float playerHitIntensity = 0.6f;
    
    [Tooltip("Duration of shake effect")]
    public float shakeDuration = 0.3f;
    
    [Tooltip("Speed of shake oscillation")]
    public float shakeSpeed = 25f;
    
    private Vector3 originalPosition;
    private bool isShaking = false;

    public void TriggerShake(bool isPlayerHit)
    {
        if (!shakeEnabled) return;
        
        float intensity = isPlayerHit ? playerHitIntensity : groundHitIntensity;
        StartCoroutine(Shake(intensity, shakeDuration));
    }

    private IEnumerator Shake(float intensity, float duration)
    {
        if (isShaking) yield break; // Don't stack shakes
        
        isShaking = true;
        originalPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Dampening shake over time
            float currentIntensity = intensity * (1f - (elapsed / duration));
            
            // Random offset
            float x = Random.Range(-1f, 1f) * currentIntensity;
            float y = Random.Range(-1f, 1f) * currentIntensity;
            
            transform.localPosition = originalPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset to original position
        transform.localPosition = originalPosition;
        isShaking = false;
    }

    public void SetShakeEnabled(bool enabled)
    {
        shakeEnabled = enabled;
        Debug.Log($"Camera shake: {(enabled ? "ENABLED" : "DISABLED")}");
    }
}

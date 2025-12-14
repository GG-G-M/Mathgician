using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UIElements;

public class PostProcessingManager : MonoBehaviour
{
    [Header("Post Processing Volume")]
    public PostProcessVolume postProcessVolume;
    
    [Header("Effect Settings")]
    public bool bloomEnabled = true;
    public bool vignetteEnabled = true;
    public bool motionBlurEnabled = false;
    
    [Header("UI")]
    public UIDocument uiDocument;
    
    private Bloom bloom;
    private Vignette vignette;
    private MotionBlur motionBlur;
    
    private void Start()
    {
        // Try to find volume if not assigned
        if (postProcessVolume == null)
        {
            postProcessVolume = FindFirstObjectByType<PostProcessVolume>();
            if (postProcessVolume == null)
            {
                Debug.LogWarning("No Post Process Volume found in scene!");
                return;
            }
        }
        
        // Get effects from volume profile
        if (postProcessVolume.profile.TryGetSettings(out bloom))
        {
            bloom.active = bloomEnabled;
        }
        
        if (postProcessVolume.profile.TryGetSettings(out vignette))
        {
            vignette.active = vignetteEnabled;
        }
        
        if (postProcessVolume.profile.TryGetSettings(out motionBlur))
        {
            motionBlur.active = motionBlurEnabled;
        }
        
        SetupUI();
    }
    
    private void SetupUI()
    {
        if (uiDocument == null) return;
        
        var root = uiDocument.rootVisualElement;
        
        // Bloom toggle
        Toggle bloomToggle = root.Q<Toggle>("bloomToggle");
        if (bloomToggle != null)
        {
            bloomToggle.value = bloomEnabled;
            bloomToggle.RegisterValueChangedCallback(evt => SetBloom(evt.newValue));
        }
        
        // Vignette toggle
        Toggle vignetteToggle = root.Q<Toggle>("vignetteToggle");
        if (vignetteToggle != null)
        {
            vignetteToggle.value = vignetteEnabled;
            vignetteToggle.RegisterValueChangedCallback(evt => SetVignette(evt.newValue));
        }
        
        // Motion Blur toggle
        Toggle motionBlurToggle = root.Q<Toggle>("motionBlurToggle");
        if (motionBlurToggle != null)
        {
            motionBlurToggle.value = motionBlurEnabled;
            motionBlurToggle.RegisterValueChangedCallback(evt => SetMotionBlur(evt.newValue));
        }
    }
    
    public void SetBloom(bool enabled)
    {
        bloomEnabled = enabled;
        if (bloom != null)
        {
            bloom.active = enabled;
        }
        Debug.Log($"✨ Bloom: {(enabled ? "ON" : "OFF")}");
    }
    
    public void SetVignette(bool enabled)
    {
        vignetteEnabled = enabled;
        if (vignette != null)
        {
            vignette.active = enabled;
        }
        Debug.Log($"📷 Vignette: {(enabled ? "ON" : "OFF")}");
    }
    
    public void SetMotionBlur(bool enabled)
    {
        motionBlurEnabled = enabled;
        if (motionBlur != null)
        {
            motionBlur.active = enabled;
        }
        Debug.Log($"💨 Motion Blur: {(enabled ? "ON" : "OFF")}");
    }
}

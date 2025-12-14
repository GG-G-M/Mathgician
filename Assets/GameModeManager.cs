using UnityEngine;
using UnityEngine.UIElements;

public class GameModeManager : MonoBehaviour
{
    public enum GameMode
    {
        FullCalculation,
        PartialCalculation,
        PartialVelocity,
        PartialAngle
    }
    
    [Header("UI")]
    public UIDocument uiDocument;
    
    [Header("Settings")]
    public GameMode currentGameMode = GameMode.FullCalculation;
    
    [Header("Settings Manager")]
    public SettingsManager settingsManager;
    
    [Header("Randomization Ranges")]
    public float minAngle = 30f;
    public float maxAngle = 60f;
    public float minVelocity = 10f;
    public float maxVelocity = 30f;
    
    // UI Elements - Must exist in your UXML
    private DropdownField gameModeDropdown;
    private TextField angleField;
    private TextField velocityField;
    private Label providedValueLabel;
    
    // Current provided values
    private float providedAngle = 0f;
    private float providedVelocity = 0f;
    private bool angleProvided = false;
    private bool velocityProvided = false;

    private void Start()
    {
        SetupUI();
        
        // Auto-find settings manager if not assigned
        if (settingsManager == null)
        {
            settingsManager = FindFirstObjectByType<SettingsManager>();
        }
        
        ApplyGameMode();
        UpdateGameModeVisibility();
    }
    
    private void Update()
    {
        // Check control mode every frame and update visibility
        UpdateGameModeVisibility();
    }
    
    private void UpdateGameModeVisibility()
    {
        if (settingsManager == null || gameModeDropdown == null) return;
        
        // Show gamemode dropdown ONLY in Input Based mode
        bool isInputMode = settingsManager.GetControlMode() == SettingsManager.ControlMode.InputBased;
        gameModeDropdown.style.display = isInputMode ? DisplayStyle.Flex : DisplayStyle.None;
        
        if (!isInputMode)
        {
            // Reset to full calculation when switching away from input mode
            currentGameMode = GameMode.FullCalculation;
            if (gameModeDropdown.index != 0)
            {
                gameModeDropdown.index = 0;
            }
        }
    }
    
    private void SetupUI()
    {
        var root = uiDocument.rootVisualElement;
        
        angleField = root.Q<TextField>("angleField");
        velocityField = root.Q<TextField>("velocityField");
        
        // Query for game mode dropdown (must be added in UI Builder)
        gameModeDropdown = root.Q<DropdownField>("gameModeDropdown");
        
        if (gameModeDropdown != null)
        {
            gameModeDropdown.choices = new System.Collections.Generic.List<string>
            {
                "Full Calculation",
                "Partial Calculation (Random)",
                "Partial Velocity Only",
                "Partial Angle Only"
            };
            gameModeDropdown.index = 0;
            gameModeDropdown.RegisterValueChangedCallback(OnGameModeChanged);
            Debug.Log("✅ GameMode Dropdown found and configured!");
        }
        else
        {
            Debug.LogWarning("⚠️ 'gameModeDropdown' not found in UI! Add a DropdownField with name='gameModeDropdown' in UI Builder.");
        }
        
        // Query for provided value label (optional)
        providedValueLabel = root.Q<Label>("providedValueLabel");
        
        if (providedValueLabel == null)
        {
            Debug.Log("ℹ️ 'providedValueLabel' not found - using console logs only");
        }
    }
    
    private void OnGameModeChanged(ChangeEvent<string> evt)
    {
        switch (gameModeDropdown.index)
        {
            case 0:
                currentGameMode = GameMode.FullCalculation;
                break;
            case 1:
                currentGameMode = GameMode.PartialCalculation;
                break;
            case 2:
                currentGameMode = GameMode.PartialVelocity;
                break;
            case 3:
                currentGameMode = GameMode.PartialAngle;
                break;
        }
        
        ApplyGameMode();
        Debug.Log($"🎮 Game mode changed to: {currentGameMode}");
    }
    
    public void ApplyGameMode()
    {
        // Reset provided values
        angleProvided = false;
        velocityProvided = false;
        
        if (providedValueLabel != null)
        {
            providedValueLabel.style.display = DisplayStyle.None;
        }
        
        switch (currentGameMode)
        {
            case GameMode.FullCalculation:
                SetupFullCalculation();
                break;
                
            case GameMode.PartialCalculation:
                SetupPartialCalculation();
                break;
                
            case GameMode.PartialVelocity:
                SetupPartialVelocity();
                break;
                
            case GameMode.PartialAngle:
                SetupPartialAngle();
                break;
        }
    }
    
    private void SetupFullCalculation()
    {
        if (angleField != null)
        {
            angleField.SetEnabled(true);
            angleField.value = "";
            angleField.label = "Angle (degrees)";
        }
        
        if (velocityField != null)
        {
            velocityField.SetEnabled(true);
            velocityField.value = "";
            velocityField.label = "Velocity (m/s)";
        }
        
        Debug.Log("📐 Full Calculation Mode - Solve for both angle and velocity");
    }
    
    private void SetupPartialCalculation()
    {
        bool provideAngle = Random.value > 0.5f;
        
        if (provideAngle)
        {
            providedAngle = Random.Range(minAngle, maxAngle);
            angleProvided = true;
            
            if (angleField != null)
            {
                angleField.SetEnabled(false);
                angleField.value = providedAngle.ToString("F1");
                angleField.label = "Angle (PROVIDED)";
            }
            
            if (velocityField != null)
            {
                velocityField.SetEnabled(true);
                velocityField.value = "";
                velocityField.label = "Velocity (SOLVE THIS)";
            }
            
            string message = $"📊 Given: Angle = {providedAngle:F1}°";
            Debug.Log(message);
            
            if (providedValueLabel != null)
            {
                providedValueLabel.text = message;
                providedValueLabel.style.display = DisplayStyle.Flex;
            }
        }
        else
        {
            providedVelocity = Random.Range(minVelocity, maxVelocity);
            velocityProvided = true;
            
            if (velocityField != null)
            {
                velocityField.SetEnabled(false);
                velocityField.value = providedVelocity.ToString("F1");
                velocityField.label = "Velocity (PROVIDED)";
            }
            
            if (angleField != null)
            {
                angleField.SetEnabled(true);
                angleField.value = "";
                angleField.label = "Angle (SOLVE THIS)";
            }
            
            string message = $"📊 Given: Velocity = {providedVelocity:F1} m/s";
            Debug.Log(message);
            
            if (providedValueLabel != null)
            {
                providedValueLabel.text = message;
                providedValueLabel.style.display = DisplayStyle.Flex;
            }
        }
    }
    
    private void SetupPartialVelocity()
    {
        providedAngle = Random.Range(minAngle, maxAngle);
        angleProvided = true;
        
        if (angleField != null)
        {
            angleField.SetEnabled(false);
            angleField.value = providedAngle.ToString("F1");
            angleField.label = "Angle (PROVIDED)";
        }
        
        if (velocityField != null)
        {
            velocityField.SetEnabled(true);
            velocityField.value = "";
            velocityField.label = "Velocity (SOLVE THIS)";
        }
        
        string message = $"📊 Given: Angle = {providedAngle:F1}°";
        Debug.Log(message);
        
        if (providedValueLabel != null)
        {
            providedValueLabel.text = message;
            providedValueLabel.style.display = DisplayStyle.Flex;
        }
    }
    
    private void SetupPartialAngle()
    {
        providedVelocity = Random.Range(minVelocity, maxVelocity);
        velocityProvided = true;
        
        if (velocityField != null)
        {
            velocityField.SetEnabled(false);
            velocityField.value = providedVelocity.ToString("F1");
            velocityField.label = "Velocity (PROVIDED)";
        }
        
        if (angleField != null)
        {
            angleField.SetEnabled(true);
            angleField.value = "";
            angleField.label = "Angle (SOLVE THIS)";
        }
        
        string message = $"📊 Given: Velocity = {providedVelocity:F1} m/s";
        Debug.Log(message);
        
        if (providedValueLabel != null)
        {
            providedValueLabel.text = message;
            providedValueLabel.style.display = DisplayStyle.Flex;
        }
    }
    
    public float GetAngle()
    {
        if (angleProvided)
        {
            return providedAngle;
        }
        
        if (angleField != null && float.TryParse(angleField.value, out float angle))
        {
            return angle;
        }
        
        return -1f;
    }
    
    public float GetVelocity()
    {
        if (velocityProvided)
        {
            return providedVelocity;
        }
        
        if (velocityField != null && float.TryParse(velocityField.value, out float velocity))
        {
            return velocity;
        }
        
        return -1f;
    }
    
    public bool IsAngleProvided()
    {
        return angleProvided;
    }
    
    public bool IsVelocityProvided()
    {
        return velocityProvided;
    }
    
    public GameMode GetCurrentGameMode()
    {
        return currentGameMode;
    }
}
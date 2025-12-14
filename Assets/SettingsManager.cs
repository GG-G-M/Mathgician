using UnityEngine;
using UnityEngine.UIElements;

public class SettingsManager : MonoBehaviour
{
    public enum ControlMode
    {
        InputBased,      // Type angle/velocity
        DragAndLaunch    // Drag to aim and release
    }
    
    [Header("UI")]
    public UIDocument uiDocument;
    
    [Header("Current Settings")]
    public ControlMode currentControlMode = ControlMode.InputBased;
    
    // UI Elements
    private Button settingsButton;
    private VisualElement settingsPanel;
    private DropdownField controlModeDropdown;
    private Button closeSettingsButton;
    private Button refreshButton; // ★★★ NEW
    
    // References to other managers
    private PlayerLauncher[] playerLaunchers;

    private void Start()
    {
        SetupUI();
        playerLaunchers = FindObjectsByType<PlayerLauncher>(FindObjectsSortMode.None);
        ApplyControlMode();
    }
    
    private void SetupUI()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("⚠️ UIDocument not assigned to SettingsManager!");
            return;
        }
        
        var root = uiDocument.rootVisualElement;
        
        // Query for settings button and panel
        settingsButton = root.Q<Button>("settingsButton");
        settingsPanel = root.Q<VisualElement>("settingsPanel");
        closeSettingsButton = root.Q<Button>("closeSettingsButton");
        controlModeDropdown = root.Q<DropdownField>("controlModeDropdown");
        refreshButton = root.Q<Button>("refreshButton"); // ★★★ NEW
        
        // Setup settings button
        if (settingsButton != null)
        {
            settingsButton.clicked += ToggleSettingsPanel;
            Debug.Log("✅ Settings button found!");
        }
        else
        {
            Debug.LogWarning("⚠️ 'settingsButton' not found in UI! Add a Button with name='settingsButton'");
        }
        
        // Setup settings panel
        if (settingsPanel != null)
        {
            settingsPanel.style.display = DisplayStyle.None; // Hidden by default
        }
        else
        {
            Debug.LogWarning("⚠️ 'settingsPanel' not found in UI! Add a VisualElement with name='settingsPanel'");
        }
        
        // Setup close button
        if (closeSettingsButton != null)
        {
            closeSettingsButton.clicked += CloseSettingsPanel;
        }
        
        // ★★★ NEW: Setup refresh button
        if (refreshButton != null)
        {
            refreshButton.clicked += RefreshEverything;
            Debug.Log("✅ Refresh button found!");
        }
        else
        {
            Debug.LogWarning("⚠️ 'refreshButton' not found in UI! Add a Button with name='refreshButton'");
        }
        
        // Setup control mode dropdown
        if (controlModeDropdown != null)
        {
            controlModeDropdown.choices = new System.Collections.Generic.List<string>
            {
                "Input Based (Type Values)",
                "Drag and Launch"
            };
            controlModeDropdown.index = (int)currentControlMode;
            controlModeDropdown.RegisterValueChangedCallback(OnControlModeChanged);
            Debug.Log("✅ Control mode dropdown configured!");
        }
        else
        {
            Debug.LogWarning("⚠️ 'controlModeDropdown' not found in settingsPanel!");
        }
    }
    
    private void ToggleSettingsPanel()
    {
        if (settingsPanel == null) return;
        
        bool isVisible = settingsPanel.style.display == DisplayStyle.Flex;
        settingsPanel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        
        Debug.Log(isVisible ? "⚙️ Settings closed" : "⚙️ Settings opened");
    }
    
    private void CloseSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.style.display = DisplayStyle.None;
            Debug.Log("⚙️ Settings closed");
        }
    }
    
    private void OnControlModeChanged(ChangeEvent<string> evt)
    {
        currentControlMode = (ControlMode)controlModeDropdown.index;
        ApplyControlMode();
        Debug.Log($"🎮 Control mode changed to: {currentControlMode}");
    }
    
    private void ApplyControlMode()
    {
        if (playerLaunchers == null)
        {
            playerLaunchers = FindObjectsByType<PlayerLauncher>(FindObjectsSortMode.None);
        }
        
        foreach (PlayerLauncher launcher in playerLaunchers)
        {
            if (launcher != null)
            {
                launcher.SetControlMode(currentControlMode);
            }
        }
        
        Debug.Log($"✅ Applied control mode to ALL players: {currentControlMode}");
    }
    
    public ControlMode GetControlMode()
    {
        return currentControlMode;
    }
    
    // ★★★ NEW: Refresh everything
    private void RefreshEverything()
    {
        Debug.Log("🔄🔄🔄 REFRESHING EVERYTHING! 🔄🔄🔄");
        
        // Re-find all player launchers
        playerLaunchers = FindObjectsByType<PlayerLauncher>(FindObjectsSortMode.None);
        
        // Re-apply current control mode to all players
        ApplyControlMode();
        
        // Force each launcher to refresh its drag controller
        foreach (PlayerLauncher launcher in playerLaunchers)
        {
            if (launcher != null)
            {
                DragLaunchController dragController = launcher.GetComponent<DragLaunchController>();
                if (dragController != null)
                {
                    // Toggle off and on to force refresh
                    dragController.enabled = false;
                    dragController.enabled = (currentControlMode == ControlMode.DragAndLaunch) && launcher.enabled;
                    Debug.Log($"   🔄 Refreshed {launcher.gameObject.name} drag controller: {dragController.enabled}");
                }
            }
        }
        
        Debug.Log("✅ Refresh complete!");
    }
}
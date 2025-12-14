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
    
    [Header("Managers")]
    public PostProcessingManager postProcessingManager;
    
    [Header("Current Settings")]
    public ControlMode currentControlMode = ControlMode.InputBased;
    
    // UI Elements
    private Button settingsButton;
    private VisualElement settingsPanel;
    private DropdownField controlModeDropdown;
    private Button closeSettingsButton;
    
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
        
        // Setup anti-aliasing dropdown
        DropdownField aaDropdown = root.Q<DropdownField>("antiAliasingDropdown");
        if (aaDropdown != null)
        {
            aaDropdown.choices = new System.Collections.Generic.List<string>
            {
                "None",
                "FXAA",
                "SMAA"
            };
            aaDropdown.index = (int)QualitySettings.antiAliasing;
            aaDropdown.RegisterValueChangedCallback(evt =>
            {
                int aaMode = aaDropdown.index;
                QualitySettings.antiAliasing = aaMode;
                Debug.Log($"Anti-Aliasing set to: {aaDropdown.value}");
            });
        }

        // Predicted impact toggle removed per request
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

    // NEW: Setting for predicted impact marker
    // Display setting removed; predicted impact visuals disabled
    
    // ★★★ NEW: Refresh everything
    private void RefreshEverything()
    {
        Debug.Log("🔄🔄🔄 REFRESHING EVERYTHING! 🔄🔄🔄");
        
        // Re-find all player launchers
        playerLaunchers = FindObjectsByType<PlayerLauncher>(FindObjectsSortMode.None);
        
        // Find camera handler
        CameraHandler cam = FindFirstObjectByType<CameraHandler>();
        
        // 1. Reset camera state FIRST
        if (cam != null)
        {
            // Force camera back to current target player
            TurnManager turnManager = FindFirstObjectByType<TurnManager>();
            if (turnManager != null)
            {
                Transform currentPlayer = turnManager.playerA; // Default to player A
                if (turnManager.playerB != null)
                {
                    // Determine current player based on turn state
                    currentPlayer = turnManager.GetCurrentPlayer();
                }
                
                // Reset camera to current player with fresh offset
                cam.SwitchToTargetPreserveZoom(currentPlayer);
                Debug.Log($"📷 Camera reset to {currentPlayer.name}");
            }
            
            // Refresh camera follow state with full rebind
            cam.HardRefreshFollowState();
            Debug.Log("📷 Camera follow state hard refreshed");
        }
        
        // 2. Re-apply current control mode to all players
        ApplyControlMode();
        
        // 3. Force each launcher to refresh its drag controller
        foreach (PlayerLauncher launcher in playerLaunchers)
        {
            if (launcher != null)
            {
                // Ensure camera handler reference is set
                if (launcher.cameraHandler == null)
                {
                    launcher.cameraHandler = cam;
                }
                
                DragLaunchController dragController = launcher.GetComponent<DragLaunchController>();
                if (dragController != null)
                {
                    // Force full reset
                    dragController.enabled = false;
                    dragController.enabled = (currentControlMode == ControlMode.DragAndLaunch) && launcher.enabled;
                    Debug.Log($"   🔄 Refreshed {launcher.gameObject.name} drag controller: {dragController.enabled}");
                }
            }
        }
        
        // 4. Clean up any orphaned projectiles
        Projectile[] projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        foreach (Projectile proj in projectiles)
        {
            if (proj != null && proj.GetComponent<Rigidbody>().isKinematic)
            {
                Debug.Log($"🗑️ Cleaning up frozen projectile: {proj.name}");
                Destroy(proj.gameObject);
            }
        }
        
        Debug.Log("✅ Refresh complete - all systems reset!");
    }
    
    // Helper to get current player transform
    private Transform GetCurrentPlayerTransform()
    {
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager != null)
        {
            return turnManager.GetCurrentPlayer();
        }
        return null;
    }
}
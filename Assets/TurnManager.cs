using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class TurnManager : MonoBehaviour
{
    [Header("Players")]
    public Transform playerA;
    public Transform playerB;
    public PlayerLauncher playerALauncher;
    public PlayerLauncher playerBLauncher;
    
    [Header("Player Handlers")]
    public PlayerHandler playerAHandler;
    public PlayerHandler playerBHandler;
    
    [Header("Camera")]
    public CameraHandler cameraHandler;
    public CameraShake cameraShake;
    
    [Header("UI")]
    public UIDocument uiDocument;
    
    [Header("Game Mode Manager")]
    public GameModeManager gameModeManager;
    
    [Header("Settings Manager")]
    public SettingsManager settingsManager;
    
    [Header("Turn Settings")]
    public float fireCooldown = 2f;
    [Tooltip("Automatically switch camera to next player after projectile lands")]
    public bool autoSwitchPerspective = true;
    
    private bool isPlayerATurn = true;
    private bool canFire = true;
    private float cooldownTimer = 0f;
    
    private Button fireButton;
    private Label turnIndicatorLabel;
    
    // Game Over UI
    private VisualElement gameOverPanel;
    private Label winnerLabel;
    private Button resetGameButton;
    
    private bool gameOver = false;

    private void Start()
    {
        SetupUI();
        SetupPlayerHandlers();
        SwitchToPlayerA();
    }
    
    private void SetupUI()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("⚠️ UIDocument not assigned to TurnManager!");
            return;
        }
        
        var root = uiDocument.rootVisualElement;
        fireButton = root.Q<Button>("fireButton");
        
        if (fireButton == null)
        {
            Debug.LogWarning("⚠️ fireButton not found in UI!");
        }
        
        // Query for optional turn indicator label
        turnIndicatorLabel = root.Q<Label>("turnIndicator");
        
        if (turnIndicatorLabel == null)
        {
            Debug.Log("ℹ️ turnIndicator label not found in UI - using console logs only");
        }
        
        // Setup Game Over UI
        gameOverPanel = root.Q<VisualElement>("gameOverPanel");
        winnerLabel = root.Q<Label>("winnerLabel");
        resetGameButton = root.Q<Button>("resetGameButton");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.style.display = DisplayStyle.None;
        }
        
        if (resetGameButton != null)
        {
            resetGameButton.clicked += ResetGame;
        }
        
        // Setup timing settings UI
        Toggle autoSwitchToggle = root.Q<Toggle>("autoSwitchPerspectiveToggle");
        
        if (autoSwitchToggle != null)
        {
            autoSwitchToggle.value = autoSwitchPerspective;
            autoSwitchToggle.RegisterValueChangedCallback(evt =>
            {
                autoSwitchPerspective = evt.newValue;
                Debug.Log($"📷 Auto switch perspective: {(autoSwitchPerspective ? "ENABLED" : "DISABLED")}");
            });
        }
        
        // Setup camera shake toggle
        Toggle shakeToggle = root.Q<Toggle>("cameraShakeToggle");
        if (shakeToggle != null && cameraShake != null)
        {
            shakeToggle.value = cameraShake.shakeEnabled;
            shakeToggle.RegisterValueChangedCallback(evt =>
            {
                cameraShake.SetShakeEnabled(evt.newValue);
            });
        }
        
        UpdateTurnUI();
    }
    
    private void SetupPlayerHandlers()
    {
        if (playerA != null && playerAHandler == null)
        {
            playerAHandler = playerA.GetComponent<PlayerHandler>();
            if (playerAHandler == null)
            {
                playerAHandler = playerA.gameObject.AddComponent<PlayerHandler>();
                playerAHandler.playerName = "Player A";
            }
        }
        
        if (playerB != null && playerBHandler == null)
        {
            playerBHandler = playerB.GetComponent<PlayerHandler>();
            if (playerBHandler == null)
            {
                playerBHandler = playerB.gameObject.AddComponent<PlayerHandler>();
                playerBHandler.playerName = "Player B";
            }
        }
    }

    private void Update()
    {
        if (gameOver) return;
        
        if (!canFire && cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            
            if (cooldownTimer <= 0f)
            {
                canFire = true;
                
                if (fireButton != null)
                {
                    fireButton.SetEnabled(true);
                }
                
                UpdateTurnUI();
                Debug.Log("✅ Cooldown finished - Ready to fire!");
            }
        }
    }
    
    public void OnProjectileFired()
    {
        if (gameOver) return;
        
        canFire = false;
        cooldownTimer = fireCooldown;
        
        if (fireButton != null)
        {
            fireButton.SetEnabled(false);
        }
        
        Debug.Log($"🚀 Projectile fired! Cooldown started: {fireCooldown}s");
    }
    
    public void OnProjectileFinished(bool hitPlayer, GameObject hitObject)
    {
        if (gameOver) return;
        
        if (hitPlayer && hitObject != null)
        {
            PlayerHandler hitHandler = hitObject.GetComponent<PlayerHandler>();
            if (hitHandler != null)
            {
                if (hitHandler == playerAHandler)
                {
                    DeclareVictory("Player B");
                }
                else if (hitHandler == playerBHandler)
                {
                    DeclareVictory("Player A");
                }
                return;
            }
        }
        
        // Return camera to the OTHER player (the one about to play next) if auto switch is enabled
        if (autoSwitchPerspective && cameraHandler != null)
        {
            Transform otherPlayer = isPlayerATurn ? playerB : playerA;
            cameraHandler.ReturnToPlayer(otherPlayer);
            Debug.Log($"📷 Camera returning to {(isPlayerATurn ? "Player B" : "Player A")} after landing");
        }
        else if (!autoSwitchPerspective)
        {
            Debug.Log($"📷 Auto switch OFF - camera stays at current position");
        }
    }
    
    public void SwitchTurnsAfterLanding()
    {
        if (gameOver) return;
        
        Debug.Log("⏳ Switching turns after projectile landed...");
        SwitchTurns();
    }
    
    private void SwitchTurns()
    {
        if (gameOver) return;
        
        canFire = true;
        cooldownTimer = 0f;
        
        if (fireButton != null)
        {
            fireButton.SetEnabled(true);
        }
        
        isPlayerATurn = !isPlayerATurn;
        
        if (isPlayerATurn)
        {
            SwitchToPlayerA();
        }
        else
        {
            SwitchToPlayerB();
        }
        
        // Generate new values for partial modes
        if (gameModeManager != null)
        {
            gameModeManager.ApplyGameMode();
        }
    }
    
    private void SwitchToPlayerA()
    {
        Debug.Log("🔵 Switching to Player A's turn...");
        
        if (playerALauncher != null)
        {
            playerALauncher.enabled = true;
            Debug.Log("   ✅ Player A launcher ENABLED");
        }
        if (playerBLauncher != null)
        {
            playerBLauncher.enabled = false;
            Debug.Log("   ❌ Player B launcher DISABLED");
        }
        
        if (cameraHandler != null && playerA != null)
        {
            cameraHandler.SwitchToTargetPreserveZoom(playerA);
            cameraHandler.HardRefreshFollowState();
        }
        
        UpdateTurnUI();
        
        // ★★★ CRITICAL FIX: Re-apply control mode after turn switch
        Invoke(nameof(RefreshControlModeAfterSwitch), 0.2f);
    }
    
    private void SwitchToPlayerB()
    {
        Debug.Log("🔴 Switching to Player B's turn...");
        
        if (playerBLauncher != null)
        {
            playerBLauncher.enabled = true;
            Debug.Log("   ✅ Player B launcher ENABLED");
        }
        if (playerALauncher != null)
        {
            playerALauncher.enabled = false;
            Debug.Log("   ❌ Player A launcher DISABLED");
        }
        
        if (cameraHandler != null && playerB != null)
        {
            cameraHandler.SwitchToTargetPreserveZoom(playerB);
            cameraHandler.HardRefreshFollowState();
        }
        
        UpdateTurnUI();
        
        // ★★★ CRITICAL FIX: Re-apply control mode after turn switch
        Invoke(nameof(RefreshControlModeAfterSwitch), 0.2f);
    }
    
    // ★★★ NEW: Force refresh control mode after turn switches
    private void RefreshControlModeAfterSwitch()
    {
        if (gameModeManager != null)
        {
            gameModeManager.ApplyGameMode();
        }
        
        // Force refresh the current player's launcher
        PlayerLauncher currentLauncher = isPlayerATurn ? playerALauncher : playerBLauncher;
        if (currentLauncher != null)
        {
            // Use assigned settings manager or find it
            if (settingsManager == null)
            {
                settingsManager = FindFirstObjectByType<SettingsManager>();
            }
            
            if (settingsManager != null)
            {
                currentLauncher.SetControlMode(settingsManager.GetControlMode());
                
                // ★★★ ADDED: Force toggle the drag controller like Refresh button does
                DragLaunchController dragController = currentLauncher.GetComponent<DragLaunchController>();
                if (dragController != null)
                {
                    dragController.enabled = false;
                    dragController.enabled = (settingsManager.GetControlMode() == SettingsManager.ControlMode.DragAndLaunch);
                    Debug.Log($"🔄 Forced drag controller refresh: {dragController.enabled}");
                }
                
                Debug.Log($"🔄 Forced control mode refresh: {settingsManager.GetControlMode()}");
            }
            else
            {
                Debug.LogWarning("⚠️ SettingsManager not found!");
            }
        }
    }
    
    public bool GetIsPlayerATurn()
    {
        return isPlayerATurn;
    }
    
    public Transform GetCurrentPlayer()
    {
        return isPlayerATurn ? playerA : playerB;
    }
    
    private void UpdateTurnUI()
    {
        string currentPlayer = isPlayerATurn ? "Player A" : "Player B";
        string status = canFire ? "Ready to Fire!" : "Waiting...";
        string playerIcon = isPlayerATurn ? "🔵" : "🔴";
        
        Debug.Log($"{playerIcon} {currentPlayer}'s Turn - {status}");
        
        // Update UI labels if they exist
        if (turnIndicatorLabel != null)
        {
            turnIndicatorLabel.text = $"{currentPlayer}'s Turn {status}";
            turnIndicatorLabel.style.color = isPlayerATurn ? new Color(0.3f, 0.7f, 1f) : new Color(1f, 0.3f, 0.3f);
        }
    }
    
    private void DeclareVictory(string winner)
    {
        gameOver = true;
        
        if (fireButton != null)
        {
            fireButton.SetEnabled(false);
        }
        
        Debug.Log($"🎉🏆 GAME OVER - {winner} WINS! 🏆🎉");
        
        // Show Game Over panel after 5 second delay (let death animation play)
        Invoke(nameof(ShowGameOverPanel), 5f);
        
        // Update UI labels if they exist
        if (turnIndicatorLabel != null)
        {
            turnIndicatorLabel.text = $"🎉 {winner} WINS! 🎉";
            turnIndicatorLabel.style.color = Color.green;
            // Keep font size the same, don't make it bigger
        }
    }
    
    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.style.display = DisplayStyle.Flex;
            
            if (winnerLabel != null)
            {
                // Extract winner name from turn indicator
                string winner = isPlayerATurn ? "Player A" : "Player B";
                winnerLabel.text = $"{winner} Wins!";
            }
        }
    }
    
    private void ResetGame()
    {
        Debug.Log("🔄 Resetting game...");
        
        // Hide game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.style.display = DisplayStyle.None;
        }
        
        // Reset game state
        gameOver = false;
        isPlayerATurn = true;
        canFire = true;
        cooldownTimer = 0f;
        
        // Restore players
        if (playerAHandler != null)
        {
            playerAHandler.isAlive = true;
            // Restore transparency
            Renderer rendA = playerAHandler.GetComponent<Renderer>();
            if (rendA != null)
            {
                Material matA = rendA.material;
                Color colorA = matA.color;
                colorA.a = 1f;
                matA.color = colorA;
            }
            // Re-enable collider
            Collider colA = playerAHandler.GetComponent<Collider>();
            if (colA != null) colA.enabled = true;
        }
        
        if (playerBHandler != null)
        {
            playerBHandler.isAlive = true;
            // Restore transparency
            Renderer rendB = playerBHandler.GetComponent<Renderer>();
            if (rendB != null)
            {
                Material matB = rendB.material;
                Color colorB = matB.color;
                colorB.a = 1f;
                matB.color = colorB;
            }
            // Re-enable collider
            Collider colB = playerBHandler.GetComponent<Collider>();
            if (colB != null) colB.enabled = true;
        }
        
        // Destroy all existing projectiles
        GameObject[] projectiles = GameObject.FindGameObjectsWithTag("Projectile");
        foreach (GameObject proj in projectiles)
        {
            Destroy(proj);
        }
        
        // Re-enable fire button
        if (fireButton != null)
        {
            fireButton.SetEnabled(true);
        }
        
        // Switch back to Player A
        SwitchToPlayerA();
        
        Debug.Log("✅ Game reset complete!");
    }
    
    public bool CanCurrentPlayerFire()
    {
        return canFire && !gameOver;
    }
    
    public bool IsPlayerATurn()
    {
        return isPlayerATurn;
    }
}
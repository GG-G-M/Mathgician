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
    
    [Header("UI")]
    public UIDocument uiDocument;
    
    [Header("Game Mode Manager")]
    public GameModeManager gameModeManager;
    
    [Header("Settings Manager")]
    public SettingsManager settingsManager;
    
    [Header("Turn Settings")]
    public float fireCooldown = 2f;
    
    private bool isPlayerATurn = true;
    private bool canFire = true;
    private float cooldownTimer = 0f;
    
    private Button fireButton;
    private Label turnIndicatorLabel;
    
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
            cameraHandler.cameraTarget = playerA;
            // ★★★ USE REMEMBERED OFFSET instead of startOffset
            cameraHandler.transform.position = playerA.position + cameraHandler.GetCurrentOffset();
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
            cameraHandler.cameraTarget = playerB;
            // ★★★ USE REMEMBERED OFFSET instead of startOffset
            cameraHandler.transform.position = playerB.position + cameraHandler.GetCurrentOffset();
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
        
        // Update UI labels if they exist
        if (turnIndicatorLabel != null)
        {
            turnIndicatorLabel.text = $"🎉 {winner} WINS! 🎉";
            turnIndicatorLabel.style.color = Color.green;
            turnIndicatorLabel.style.fontSize = 32;
        }
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
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
    
    [Header("Turn Settings")]
    public float fireCooldown = 2f;
    
    private bool isPlayerATurn = true;
    private bool canFire = true;
    private float cooldownTimer = 0f;
    
    private Button fireButton;
    private Label turnIndicatorLabel;
    private Label cooldownLabel;
    
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
        
        // Query for existing UI labels (optional - won't create if not found)
        turnIndicatorLabel = root.Q<Label>("turnIndicator");
        cooldownLabel = root.Q<Label>("cooldownLabel");
        
        if (turnIndicatorLabel == null)
        {
            Debug.Log("ℹ️ turnIndicator label not found in UI - using console logs only");
        }
        
        if (cooldownLabel == null)
        {
            Debug.Log("ℹ️ cooldownLabel not found in UI - using console logs only");
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
        
        if (cooldownLabel != null)
        {
            cooldownLabel.style.display = DisplayStyle.None;
        }
        
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
        if (playerALauncher != null)
            playerALauncher.enabled = true;
        if (playerBLauncher != null)
            playerBLauncher.enabled = false;
        
        if (cameraHandler != null && playerA != null)
        {
            cameraHandler.cameraTarget = playerA;
            cameraHandler.transform.position = playerA.position + cameraHandler.startOffset;
        }
        
        UpdateTurnUI();
        Debug.Log("🔵 Player A's Turn");
    }
    
    private void SwitchToPlayerB()
    {
        if (playerBLauncher != null)
            playerBLauncher.enabled = true;
        if (playerALauncher != null)
            playerALauncher.enabled = false;
        
        if (cameraHandler != null && playerB != null)
        {
            cameraHandler.cameraTarget = playerB;
            cameraHandler.transform.position = playerB.position + cameraHandler.startOffset;
        }
        
        UpdateTurnUI();
        Debug.Log("🔴 Player B's Turn");
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
        
        if (cooldownLabel != null)
        {
            cooldownLabel.style.display = DisplayStyle.None;
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
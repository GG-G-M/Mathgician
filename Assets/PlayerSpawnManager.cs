using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Existing Players")]
    [Tooltip("Drag Player A from hierarchy")]
    public Transform playerA;
    [Tooltip("Drag Player B from hierarchy")]
    public Transform playerB;
    
    [Header("Spawn Settings")]
    [Tooltip("Minimum horizontal separation between players")]
    public float minSeparation = 20f;
    [Tooltip("Maximum horizontal separation between players")]
    public float maxSeparation = 40f;
    [Tooltip("Random height variation range")]
    public float heightVariation = 5f;
    [Tooltip("Base ground level")]
    public float groundLevel = 0f;
    
    [Header("Spawn Bounds")]
    [Tooltip("Leftmost spawn position for Player A")]
    public float minX = -30f;
    [Tooltip("Rightmost spawn position for Player B")]
    public float maxX = 30f;
    
    [Header("References")]
    public TurnManager turnManager;
    public CameraHandler cameraHandler;
    public GameModeManager gameModeManager;
    public SettingsManager settingsManager;

    private void Start()
    {
        SpawnPlayers();
    }
    
    public void SpawnPlayers()
    {
        if (playerA == null || playerB == null)
        {
            Debug.LogError("Player A or Player B not assigned in PlayerSpawnManager!");
            return;
        }
        
        // Random separation between players
        float separation = Random.Range(minSeparation, maxSeparation);
        
        // Random positions within bounds
        float playerAX = Random.Range(minX, minX + (maxX - minX) * 0.4f); // Left side
        float playerBX = playerAX + separation; // Right side, separated
        
        // Ensure Player B doesn't exceed max bounds
        if (playerBX > maxX)
        {
            playerBX = maxX;
            playerAX = playerBX - separation;
        }
        
        // Random height variation
        float playerAY = groundLevel + Random.Range(0f, heightVariation);
        float playerBY = groundLevel + Random.Range(0f, heightVariation);
        
        Vector3 posA = new Vector3(playerAX, playerAY, 0f);
        Vector3 posB = new Vector3(playerBX, playerBY, 0f);
        
        // Reposition existing players
        playerA.position = posA;
        playerB.position = posB;
        
        Debug.Log($"🔵 Player A repositioned to {posA}");
        Debug.Log($"🔴 Player B repositioned to {posB}");
        
        // Wire up references to managers
        WireUpManagers();
    }
    
    private void WireUpManagers()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }
        
        if (cameraHandler == null)
        {
            cameraHandler = FindFirstObjectByType<CameraHandler>();
        }
        
        if (gameModeManager == null)
        {
            gameModeManager = FindFirstObjectByType<GameModeManager>();
        }
        
        if (settingsManager == null)
        {
            settingsManager = FindFirstObjectByType<SettingsManager>();
        }
        
        // Assign players to TurnManager
        if (turnManager != null && playerA != null && playerB != null)
        {
            turnManager.playerA = playerA;
            turnManager.playerB = playerB;
            
            turnManager.playerALauncher = playerA.GetComponent<PlayerLauncher>();
            turnManager.playerBLauncher = playerB.GetComponent<PlayerLauncher>();
            
            turnManager.playerAHandler = playerA.GetComponent<PlayerHandler>();
            turnManager.playerBHandler = playerB.GetComponent<PlayerHandler>();
            
            Debug.Log("✅ Players assigned to TurnManager");
        }
        
        // Assign Player A as initial camera target
        if (cameraHandler != null && playerA != null)
        {
            cameraHandler.cameraTarget = playerA;
            cameraHandler.SwitchToTargetPreserveZoom(playerA);
            Debug.Log("✅ Camera target set to Player A");
        }
        
        // Refresh managers to recognize new players
        if (turnManager != null)
        {
            // Force turn manager to reinitialize with new players
            turnManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }
    }
    
    // Call this to respawn players at new random positions
    public void RespawnPlayers()
    {
        SpawnPlayers();
    }
}

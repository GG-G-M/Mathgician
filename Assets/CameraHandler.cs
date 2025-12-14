using UnityEngine;
using UnityEngine.UIElements;

public class CameraHandler : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform cameraTarget;
    public Vector3 startOffset = new Vector3(0, 0, -10);
    public float smoothSpeed = 0.01f;

    [Header("Drag & Scroll Settings")]
    public float dragSpeed = 20f;
    public float scrollSpeed = 20f;
    public float minZoom = 3f;
    public float maxZoom = 40f;

    [Header("UI Document")]
    public UIDocument uiDocument;

    private Vector3 velocity = Vector3.zero;
    private Vector3 initialPosition;
    private Transform currentTarget;
    private Transform lastPlayerTarget; // Remember which player to return to
    private Label followModeIndicator;
    private bool followModeEnabled = false;
    private Toggle followProjectileToggle;
    
    private bool isInFreeMode = false;
    private Vector3 freeModePosition;
    private bool isDragging = false;
    private Vector3 dragStartPosition;
    
    private Vector3 currentOffset;
    private Vector3 playerOffset; // Separate offset for players to preserve zoom
    
    private VisualElement settingsPanel;
    private VisualElement formulaPanel;
    
    private PlayerLauncher[] playerLaunchers;

    private void Start()
    {
        if (cameraTarget == null)
        {
            Debug.LogError("Camera target not assigned!");
            return;
        }

        currentTarget = cameraTarget;
        lastPlayerTarget = cameraTarget;
        currentOffset = startOffset;
        playerOffset = startOffset; // Initialize player offset
        initialPosition = cameraTarget.position + currentOffset;
        transform.position = initialPosition;

        SetupUIToggle();
        
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            settingsPanel = root.Q<VisualElement>("settingsPanel");
            formulaPanel = root.Q<VisualElement>("formulaPanel");
        }
        
        playerLaunchers = FindObjectsByType<PlayerLauncher>(FindObjectsSortMode.None);
    }

    private void SetupUIToggle()
    {
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            followProjectileToggle = root.Q<Toggle>("followProjectileToggle");
            followModeIndicator = root.Q<Label>("followModeIndicator");
            
            if (followProjectileToggle != null)
            {
                followProjectileToggle.label = "Follow Projectile";
                // Preserve existing toggle value instead of forcing false
                followModeEnabled = followProjectileToggle.value;
                followProjectileToggle.RegisterValueChangedCallback(OnFollowToggleChanged);
                UpdateFollowIndicator();
            }
            else
            {
                Debug.LogWarning("followProjectileToggle not found in UI Document!");
            }
        }
    }

    private void UpdateFollowIndicator()
    {
        if (followModeIndicator == null) return;
        if (followModeEnabled)
        {
            followModeIndicator.text = "📷 Follow Mode ON";
            followModeIndicator.style.display = DisplayStyle.Flex;
        }
        else
        {
            followModeIndicator.style.display = DisplayStyle.None;
        }
    }

    // Public refresh to stabilize camera state after UI/setting refreshes
    public void HardRefreshFollowState()
    {
        // Rebind UI toggle if needed
        if (uiDocument != null && followProjectileToggle == null)
        {
            SetupUIToggle();
        }
        
        // Sync mode from current toggle value
        if (followProjectileToggle != null)
        {
            followModeEnabled = followProjectileToggle.value;
        }

        // Ensure we have a player target
        if (lastPlayerTarget == null)
        {
            lastPlayerTarget = cameraTarget;
        }

        // Recompute player offset based on current position
        if (lastPlayerTarget != null)
        {
            playerOffset = transform.position - lastPlayerTarget.position;
        }

        if (followModeEnabled)
        {
            // If follow is enabled, try to find current projectile
            FindAndFollowExistingProjectile();
        }
        else
        {
            // Otherwise, return to player with preserved zoom
            ReturnToPlayer(lastPlayerTarget);
        }
    }

    private void OnFollowToggleChanged(ChangeEvent<bool> evt)
    {
        followModeEnabled = evt.newValue;
        
        if (followModeEnabled)
        {
            Debug.Log("✅ Follow mode ENABLED");
            isInFreeMode = false;
            
            if (currentTarget != null)
            {
                // Save player offset before potentially switching to projectile
                if (currentTarget.GetComponent<Projectile>() == null)
                {
                    playerOffset = transform.position - currentTarget.position;
                }
            }
            
            FindAndFollowExistingProjectile();
        }
        else
        {
            Debug.Log("❌ Follow mode DISABLED");
            
            if (currentTarget != null && currentTarget.GetComponent<Projectile>() != null)
            {
                playerOffset = transform.position - currentTarget.position;
                
                isInFreeMode = true;
                freeModePosition = transform.position;
                currentTarget = null;
                
                Debug.Log("🔒 Camera locked at current position");
            }
        }
        UpdateFollowIndicator();
    }

    private void FindAndFollowExistingProjectile()
    {
        Projectile[] projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        
        if (projectiles.Length > 0)
        {
            Projectile newestProjectile = null;
            int highestInstanceID = int.MinValue;
            
            foreach (Projectile proj in projectiles)
            {
                if (proj != null && proj.gameObject.activeInHierarchy && IsValidProjectile(proj.transform))
                {
                    int id = proj.GetInstanceID();
                    if (id > highestInstanceID)
                    {
                        highestInstanceID = id;
                        newestProjectile = proj;
                    }
                }
            }
            
            if (newestProjectile != null)
            {
                currentTarget = newestProjectile.transform;
                isInFreeMode = false;
                Debug.Log($"🎯 Found and following existing projectile: {newestProjectile.name}");
            }
            else
            {
                Debug.Log("No valid projectiles found to follow");
            }
        }
        else
        {
            Debug.Log("No projectiles in scene yet - waiting for launch");
        }
    }

    public void NotifyProjectileFired(Transform projectileTransform)
    {
        // Remember current zoom before following projectile
        if (currentTarget != null && currentTarget.GetComponent<Projectile>() == null)
        {
            playerOffset = currentOffset;
        }
        
        // Automatically follow if toggle is enabled
        if (followModeEnabled)
        {
            currentTarget = projectileTransform;
            isInFreeMode = false;
            Debug.Log($"🎯 Camera NOW FOLLOWING: {projectileTransform.name}");
        }
        else
        {
            Debug.Log("Toggle is OFF - not following projectile");
        }
    }

    private void Update()
    {
        HandleDrag();
        HandleScroll();
        HandleReset();
        
        if (followModeEnabled && currentTarget != null && currentTarget.GetComponent<Projectile>() != null)
        {
            if (!IsValidProjectile(currentTarget))
            {
                Debug.Log("Projectile stopped/destroyed - entering free roam mode");
                
                UpdateCurrentOffset();
                
                currentTarget = null;
                isInFreeMode = true;
                freeModePosition = transform.position;
                
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }

    private void LateUpdate()
    {
        if (isInFreeMode)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
            return;
        }
        else if (currentTarget != null && !isDragging)
        {
            // Use playerOffset for players, but center projectile horizontally while preserving zoom
            bool targetIsProjectile = currentTarget.GetComponent<Projectile>() != null;
            Vector3 followOffset;
            
            if (targetIsProjectile)
            {
                // Force projectile to center: zero out X completely, preserve Y/Z zoom
                followOffset = new Vector3(0f, playerOffset.y, playerOffset.z);
                
                // Direct position update for projectile centering (no smooth damp to avoid drift)
                Vector3 targetPosition = currentTarget.position + followOffset;
                transform.position = targetPosition;
            }
            else
            {
                // Use full player offset with smooth damping
                followOffset = playerOffset;
                Vector3 targetPosition = currentTarget.position + followOffset;
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);
            }
            
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (currentTarget == null && !isDragging && !followModeEnabled)
        {
            currentTarget = cameraTarget;
        }
    }

    private void HandleDrag()
    {
        if (IsAnyPanelOpen())
        {
            if (isDragging)
            {
                isDragging = false;
            }
            return;
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            // ★★★ PRIORITY CHECK: Let drag-launch controller have first priority
            if (IsDragLaunchModeActive() && IsMouseOverPlayer())
            {
                Debug.Log("🎯 Yielding to DragLaunchController - camera drag disabled");
                isDragging = false; // Ensure camera doesn't grab control
                return; // Let the player's drag controller handle it
            }
            
            if (!IsClickingOnToggle())
            {
                dragStartPosition = Input.mousePosition;
                isDragging = true;
                isInFreeMode = true;
                freeModePosition = transform.position;
                
                UpdateCurrentOffset();
                currentTarget = null;
                
                transform.rotation = Quaternion.Euler(0, 0, 0);
                Debug.Log("🔒 Started camera drag");
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            // ★★★ Stop camera drag if mouse moves over player during drag
            if (IsDragLaunchModeActive() && IsMouseOverPlayer())
            {
                isDragging = false;
                Debug.Log("🛑 Camera drag cancelled - moved over player");
                return;
            }
            
            Vector3 currentPos = Input.mousePosition;
            Vector3 difference = Camera.main.ScreenToViewportPoint(dragStartPosition - currentPos);
            
            Vector3 move = new Vector3(difference.x * dragSpeed, difference.y * dragSpeed, 0);
            transform.Translate(move, Space.World);
            
            dragStartPosition = currentPos;
            freeModePosition = transform.position;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private void HandleScroll()
    {
        if (IsAnyPanelOpen()) return;
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 zoomDir = transform.forward * scroll * scrollSpeed;
            transform.position += zoomDir;

            // Update playerOffset if currently following a player
            if (currentTarget != null && currentTarget.GetComponent<Projectile>() == null)
            {
                playerOffset = transform.position - currentTarget.position;
            }
            // Also update currentOffset for backward compatibility
            if (currentTarget != null)
            {
                currentOffset = transform.position - currentTarget.position;
            }

            float currentHeight = transform.position.y;
            if (currentHeight < minZoom)
            {
                transform.position = new Vector3(transform.position.x, minZoom, transform.position.z);
            }
            else if (currentHeight > maxZoom)
            {
                transform.position = new Vector3(transform.position.x, maxZoom, transform.position.z);
            }
            
            if (!isInFreeMode)
            {
                isInFreeMode = true;
                currentTarget = null;
                Debug.Log("🔒 Entered free roam mode (zooming)");
            }
            
            freeModePosition = transform.position;
        }
    }

    private void HandleReset()
    {
        if (Input.GetMouseButtonDown(2))
        {
            ReturnToPlayerA();
            
            if (followProjectileToggle != null)
            {
                followProjectileToggle.value = false;
            }
        }
    }

    private void ReturnToPlayerA()
    {
        currentTarget = cameraTarget;
        isInFreeMode = false;
        followModeEnabled = false;
        transform.position = cameraTarget.position + currentOffset;
        
        transform.rotation = Quaternion.Euler(0, 0, 0);
        
        Debug.Log("🏠 Camera returned to Player");
    }
    
    private void UpdateCurrentOffset()
    {
        if (currentTarget != null)
        {
            currentOffset = transform.position - currentTarget.position;
            Debug.Log($"📏 Offset updated: {currentOffset}");
        }
    }

    public Vector3 GetCurrentOffset()
    {
        return currentOffset;
    }

    // Preserve current zoom/offset when switching targets
    public void SwitchToTargetPreserveZoom(Transform newTarget)
    {
        if (newTarget == null) return;
        // Keep current camera position; update offset relative to the new target
        currentTarget = newTarget;
        cameraTarget = newTarget;
        lastPlayerTarget = newTarget; // Remember player target
        playerOffset = transform.position - newTarget.position;
        currentOffset = playerOffset;
        isInFreeMode = false;
        Debug.Log($"📷 Switched target with preserved zoom. Offset: {playerOffset}");
    }
    
    // Return camera to a specific player after projectile lands
    public void ReturnToPlayer(Transform targetPlayer)
    {
        if (targetPlayer != null)
        {
            currentTarget = targetPlayer;
            lastPlayerTarget = targetPlayer;
            isInFreeMode = false;
            // Smoothly transition back with preserved zoom
            Vector3 targetPos = targetPlayer.position + playerOffset;
            velocity = Vector3.zero; // Reset velocity for smooth damp
            Debug.Log($"📷 Returning to {targetPlayer.name} with preserved zoom");
        }
    }

    public void ExitFreeMode()
    {
        if (isInFreeMode)
        {
            isInFreeMode = false;
            Debug.Log("📷 Exited free mode for drag-launch");
        }
    }

    private bool IsValidProjectile(Transform potentialTarget)
    {
        if (potentialTarget == null) return false;
        
        Projectile projectile = potentialTarget.GetComponent<Projectile>();
        if (projectile == null) return false;
        
        Rigidbody rb = potentialTarget.GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic)
        {
            return false;
        }
        
        return potentialTarget.gameObject.activeInHierarchy;
    }

    private bool IsClickingOnToggle()
    {
        if (uiDocument == null || followProjectileToggle == null) return false;
        
        var root = uiDocument.rootVisualElement;
        Vector2 mousePosition = Input.mousePosition;
        var pickedElement = root.panel.Pick(mousePosition);
        return pickedElement != null && (pickedElement == followProjectileToggle || followProjectileToggle.Contains(pickedElement));
    }
    
    private bool IsAnyPanelOpen()
    {
        bool settingsOpen = settingsPanel != null && settingsPanel.style.display == DisplayStyle.Flex;
        bool formulaOpen = formulaPanel != null && formulaPanel.style.display == DisplayStyle.Flex;
        return settingsOpen || formulaOpen;
    }
    
    private bool IsDragLaunchModeActive()
    {
        if (playerLaunchers == null) return false;
        
        foreach (var launcher in playerLaunchers)
        {
            if (launcher != null && launcher.enabled)
            {
                DragLaunchController dragController = launcher.GetComponent<DragLaunchController>();
                if (dragController != null && dragController.enabled)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    private bool IsMouseOverPlayer()
    {
        if (Camera.main == null) return false;
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            PlayerLauncher launcher = hit.collider.GetComponent<PlayerLauncher>();
            if (launcher == null)
            {
                launcher = hit.collider.GetComponentInParent<PlayerLauncher>();
            }
            
            if (launcher != null)
            {
                DragLaunchController dragController = launcher.GetComponent<DragLaunchController>();
                if (dragController != null && dragController.enabled)
                {
                    Debug.Log($"🎯 Raycast hit player with drag-launch: {hit.collider.gameObject.name}");
                    return true;
                }
            }
        }
        
        return false;
    }
}
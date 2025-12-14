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
    private bool followModeEnabled = false;
    private Toggle followProjectileToggle;
    
    private bool isInFreeMode = false;
    private Vector3 freeModePosition;
    private bool isDragging = false;
    private Vector3 dragStartPosition;
    
    private Vector3 currentOffset;
    
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
        currentOffset = startOffset;
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
            
            if (followProjectileToggle != null)
            {
                followProjectileToggle.label = "Follow Projectile";
                followProjectileToggle.value = false;
                followProjectileToggle.RegisterValueChangedCallback(OnFollowToggleChanged);
            }
            else
            {
                Debug.LogWarning("followProjectileToggle not found in UI Document!");
            }
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
                currentOffset = transform.position - currentTarget.position;
            }
            
            FindAndFollowExistingProjectile();
        }
        else
        {
            Debug.Log("❌ Follow mode DISABLED");
            
            if (currentTarget != null && currentTarget.GetComponent<Projectile>() != null)
            {
                UpdateCurrentOffset();
                
                isInFreeMode = true;
                freeModePosition = transform.position;
                currentTarget = null;
                
                Debug.Log("🔒 Camera locked at current position");
            }
        }
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
        UpdateCurrentOffset();
        
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
            Vector3 targetPosition = currentTarget.position + currentOffset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);
            
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
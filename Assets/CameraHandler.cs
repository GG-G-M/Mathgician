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
    
    // ★★★ NEW: Remember current zoom offset ★★★
    private Vector3 currentOffset;

    private void Start()
    {
        if (cameraTarget == null)
        {
            Debug.LogError("Camera target not assigned!");
            return;
        }

        currentTarget = cameraTarget;
        currentOffset = startOffset; // Initialize with default offset
        initialPosition = cameraTarget.position + currentOffset;
        transform.position = initialPosition;

        SetupUIToggle();
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
            
            // ★★★ UPDATE: Calculate current offset before following ★★★
            if (currentTarget != null)
            {
                currentOffset = transform.position - currentTarget.position;
            }
            
            // Try to find and follow an existing projectile immediately
            FindAndFollowExistingProjectile();
        }
        else
        {
            Debug.Log("❌ Follow mode DISABLED");
            
            // If we were following a projectile, STAY at current position
            if (currentTarget != null && currentTarget.GetComponent<Projectile>() != null)
            {
                // ★★★ UPDATE: Save current offset before entering free mode ★★★
                UpdateCurrentOffset();
                
                // Enter free mode and lock current camera position
                isInFreeMode = true;
                freeModePosition = transform.position;
                currentTarget = null;
                
                Debug.Log("🔒 Camera locked at current position");
            }
        }
    }

    // Find and follow any existing projectile in the scene
    private void FindAndFollowExistingProjectile()
    {
        Projectile[] projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        
        if (projectiles.Length > 0)
        {
            // Find the newest active projectile
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

    // ☆☆☆ PUBLIC METHOD - Called by PlayerLauncher when firing ☆☆☆
    public void NotifyProjectileFired(Transform projectileTransform)
    {
        // ★★★ UPDATE: Save current offset before switching targets ★★★
        UpdateCurrentOffset();
        
        // Only follow if toggle is enabled
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
        
        // Check if current projectile is still valid (only when following)
        if (followModeEnabled && currentTarget != null && currentTarget.GetComponent<Projectile>() != null)
        {
            if (!IsValidProjectile(currentTarget))
            {
                Debug.Log("Projectile stopped/destroyed - entering free roam mode");
                
                // ★★★ UPDATE: Save offset before losing target ★★★
                UpdateCurrentOffset();
                
                currentTarget = null;
                isInFreeMode = true;
                freeModePosition = transform.position;
                
                // Lock rotation when entering free mode
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }

    private void LateUpdate()
    {
        if (isInFreeMode)
        {
            // Stay locked at current position with fixed rotation
            transform.rotation = Quaternion.Euler(0, 0, 0); // Keep camera straight
            return;
        }
        else if (currentTarget != null && !isDragging)
        {
            // ★★★ UPDATE: Follow with remembered offset instead of startOffset ★★★
            Vector3 targetPosition = currentTarget.position + currentOffset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed);
            
            // For side-scroller: keep camera rotation fixed, don't look at target
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (currentTarget == null && !isDragging && !followModeEnabled)
        {
            // Default back to Player only if follow mode is off
            currentTarget = cameraTarget;
        }
    }

    private void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsClickingOnToggle())
            {
                dragStartPosition = Input.mousePosition;
                isDragging = true;
                isInFreeMode = true;
                freeModePosition = transform.position;
                
                // ★★★ UPDATE: Save offset before losing target ★★★
                UpdateCurrentOffset();
                currentTarget = null;
                
                // Lock rotation when entering free mode
                transform.rotation = Quaternion.Euler(0, 0, 0);
                Debug.Log("🔒 Entered free roam mode (dragging)");
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 currentPos = Input.mousePosition;
            Vector3 difference = Camera.main.ScreenToViewportPoint(dragStartPosition - currentPos);
            
            // Move in X (horizontal) and Y (vertical) only - NOT Z!
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
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 zoomDir = transform.forward * scroll * scrollSpeed;
            transform.position += zoomDir;

            // ★★★ UPDATE: Update currentOffset when zooming ★★★
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
            
            // Enter free mode when scrolling - allows zooming while following Player
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
        
        // ★★★ UPDATE: Use current offset instead of resetting to startOffset ★★★
        transform.position = cameraTarget.position + currentOffset;
        
        // Keep camera rotation fixed for side-scroller
        transform.rotation = Quaternion.Euler(0, 0, 0);
        
        Debug.Log("🏠 Camera returned to Player");
    }
    
    // ★★★ NEW METHOD: Update the current offset based on camera position ★★★
    private void UpdateCurrentOffset()
    {
        if (currentTarget != null)
        {
            currentOffset = transform.position - currentTarget.position;
            Debug.Log($"📏 Offset updated: {currentOffset}");
        }
    }

    private bool IsValidProjectile(Transform potentialTarget)
    {
        if (potentialTarget == null) return false;
        
        Projectile projectile = potentialTarget.GetComponent<Projectile>();
        if (projectile == null) return false;
        
        // Check if projectile is frozen (hit something)
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
}
using UnityEngine;
using UnityEngine.UIElements; // ★★★ ADDED: For VisualElement

public class DragLaunchController : MonoBehaviour
{
    [Header("Drag Settings")]
    public float maxDragDistance = 5f; // ★★★ INCREASED from 3f
    public float velocityMultiplier = 10f;
    public Transform firePoint; // StaffTip - where projectile fires from
    public LineRenderer trajectoryLine;
    public int trajectoryPoints = 30;
    public float trajectoryTimeStep = 0.1f;
    
    [Header("Visual Feedback")]
    public Color dragLineColor = Color.yellow;
    public float dragLineWidth = 0.1f;
    
    [Header("UI Reference")]
    public UIDocument uiDocument;
    
    [Header("Camera Reference")]
    public CameraHandler cameraHandler;
    
    private bool isDragging = false;
    private Vector3 dragStartPos;
    private Vector3 dragCurrentPos;
    private Camera mainCamera;
    private LineRenderer dragLine;
    // Predicted visuals removed
    private VisualElement settingsPanel;
    private VisualElement formulaPanel;
    private SettingsManager settingsManager;
    private GameObject predictedImpactMarker;
    
    // UI field references
    private TextField angleField;
    private TextField velocityField;

    private void Start()
    {
        mainCamera = Camera.main;
        
        // Get UI panels and fields
        if (uiDocument == null)
        {
            uiDocument = FindFirstObjectByType<UIDocument>();
        }
        
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            settingsPanel = root.Q<VisualElement>("settingsPanel");
            formulaPanel = root.Q<VisualElement>("formulaPanel");
            angleField = root.Q<TextField>("angleField");
            velocityField = root.Q<TextField>("velocityField");
        }
        
        // Auto-find camera handler
        if (cameraHandler == null)
        {
            cameraHandler = FindFirstObjectByType<CameraHandler>();
        }
        if (settingsManager == null)
        {
            settingsManager = FindFirstObjectByType<SettingsManager>();
        }
        
        // Create drag line
        GameObject dragLineObj = new GameObject("DragLine");
        dragLineObj.transform.SetParent(transform);
        dragLine = dragLineObj.AddComponent<LineRenderer>();
        dragLine.startWidth = dragLineWidth;
        dragLine.endWidth = dragLineWidth;
        dragLine.material = new Material(Shader.Find("Sprites/Default"));
        dragLine.startColor = dragLineColor;
        dragLine.endColor = dragLineColor;
        dragLine.positionCount = 0;
        
        // Predicted line removed per request
        
        // Setup trajectory line if assigned
        if (trajectoryLine != null)
        {
            trajectoryLine.startWidth = 0.05f;
            trajectoryLine.endWidth = 0.05f;
            trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
            trajectoryLine.startColor = Color.green;
            trajectoryLine.endColor = Color.red;
            trajectoryLine.positionCount = 0;
        }
    }
    
    private void OnEnable()
    {
        Debug.Log($"🟢 DragLaunchController ENABLED for {gameObject.name}");
        
        // Immediately disable input fields when drag mode is enabled
        SetInputFieldsInteractable(false);
        
        // Reset dragging state
        isDragging = false;
    }
    
    private void OnDisable()
    {
        Debug.Log($"🔴 DragLaunchController DISABLED for {gameObject.name}");
        
        // When drag-launch mode is disabled, enable input fields
        SetInputFieldsInteractable(true);
        
        // Clear any active drag visuals
        isDragging = false;
        if (dragLine != null)
            dragLine.positionCount = 0;
        if (trajectoryLine != null)
            trajectoryLine.positionCount = 0;
        // Predicted line removed per request
    }
    
    private void SetInputFieldsInteractable(bool interactable)
    {
        if (angleField != null)
        {
            // Force disable the field
            angleField.SetEnabled(interactable);
            angleField.pickingMode = interactable ? PickingMode.Position : PickingMode.Ignore;
            
            if (!interactable)
            {
                angleField.value = "-- Drag to Aim --";
            }
            else
            {
                angleField.value = "";
            }
        }
        if (velocityField != null)
        {
            // Force disable the field
            velocityField.SetEnabled(interactable);
            velocityField.pickingMode = interactable ? PickingMode.Position : PickingMode.Ignore;
            
            if (!interactable)
            {
                velocityField.value = "-- Drag to Fire --";
            }
            else
            {
                velocityField.value = "";
            }
        }
        
        Debug.Log($"📝 Input fields set to interactable: {interactable}");
    }

    private void Update()
    {
        // Only handle drag input if this component is enabled
        if (!enabled) return;
        
        // ★★★ CRITICAL: Process input in LateUpdate to ensure we get it AFTER camera
        HandleDragInput();
    }
    
    // ★★★ Use LateUpdate to guarantee we process AFTER CameraHandler's Update
    private void LateUpdate()
    {
        // Secondary check in LateUpdate for reliability
        if (!enabled || isDragging) return;
        
        // Quick check for starting drag if Update missed it
        if (Input.GetMouseButtonDown(0) && !IsClickingOnUI() && !IsAnyPanelOpen())
        {
            if (IsMouseOverObject())
            {
                isDragging = true;
                dragStartPos = transform.position;
                SetInputFieldsInteractable(false);
                Debug.Log($"✅ [LateUpdate] Started dragging {gameObject.name}");
            }
        }
    }
    
    private void HandleDragInput()
    {
        // Don't allow dragging if UI panels are open
        if (IsAnyPanelOpen())
        {
            if (isDragging)
            {
                isDragging = false;
                dragLine.positionCount = 0;
                if (trajectoryLine != null)
                    trajectoryLine.positionCount = 0;
            }
            return;
        }
        
        // Start drag - Simple and reliable method
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"🔵 DragLaunchController: Mouse clicked on {gameObject.name}");
            
            // Check if clicking on UI first
            if (IsClickingOnUI())
            {
                Debug.Log($"🚫 DragLaunchController: Blocked by UI");
                return;
            }
            
            Debug.Log($"🔍 DragLaunchController: Checking if mouse over {gameObject.name}...");
            
            // Check if mouse is over this object using screen bounds
            if (IsMouseOverObject())
            {
                isDragging = true;
                dragStartPos = firePoint != null ? firePoint.position : transform.position;
                SetInputFieldsInteractable(false);
                Debug.Log($"✅ Started dragging {gameObject.name}");
                return;
            }
            else
            {
                Debug.Log($"❌ DragLaunchController: Mouse not over {gameObject.name}");
            }
        }
        
        // During drag
        if (Input.GetMouseButton(0) && isDragging)
        {
            dragCurrentPos = GetMouseWorldPosition();
            
            // Clamp drag distance
            Vector3 dragVector = dragStartPos - dragCurrentPos;
            if (dragVector.magnitude > maxDragDistance)
            {
                dragVector = dragVector.normalized * maxDragDistance;
                dragCurrentPos = dragStartPos - dragVector;
            }
            
            // Calculate current values
            Vector3 launchVelocity = dragVector * velocityMultiplier;
            float angle = Mathf.Atan2(launchVelocity.y, Mathf.Abs(launchVelocity.x)) * Mathf.Rad2Deg;
            float velocity = launchVelocity.magnitude;
            
            // Update input fields with current drag values
            UpdateInputFieldsWithDragValues(angle, velocity);
            
            // Update drag line
            UpdateDragLine();
            
            // Update trajectory prediction
            UpdateTrajectoryPrediction(dragVector);
        }
        
        // Release drag
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            dragLine.positionCount = 0;
            if (trajectoryLine != null)
                trajectoryLine.positionCount = 0;
            
            Vector3 releaseDragVector = dragStartPos - dragCurrentPos;
            float distance = releaseDragVector.magnitude;
            
            if (distance > 0.1f)
            {
                // Calculate launch parameters
                Vector3 launchVelocity = releaseDragVector * velocityMultiplier;
                
                // ★★★ FIXED: Calculate angle correctly - preserve sign for up/down aiming
                PlayerLauncher launcher = GetComponent<PlayerLauncher>();
                bool isFacingLeft = launcher != null && launcher.isFacingLeft;
                
                float angle;
                
                if (isFacingLeft)
                {
                    // Player B: Reverse X direction so dragging right = firing left
                    float angleRad = Mathf.Atan2(launchVelocity.y, -launchVelocity.x);
                    angle = angleRad * Mathf.Rad2Deg; // ★★★ NO Abs() - preserve sign!
                }
                else
                {
                    // Player A: Normal direction
                    float angleRad = Mathf.Atan2(launchVelocity.y, launchVelocity.x);
                    angle = angleRad * Mathf.Rad2Deg; // ★★★ NO Abs() - preserve sign!
                }
                
                float velocity = launchVelocity.magnitude;
                
                Debug.Log($"🚀 Launched: Angle={angle:F1}°, Velocity={velocity:F1} m/s");
                
                // Trigger launch immediately (don't display in fields)
                SendLaunchCommand(angle, velocity);
                
                // ★★★ FIXED: Don't re-enable fields - they should stay disabled in drag mode
                // Fields will be re-enabled when control mode switches back to Input Based
            }
            // No else needed - short drags just cancel, fields stay disabled
        }
    }
    
    private void UpdateInputFieldsWithDragValues(float angle, float velocity)
    {
        if (angleField != null)
        {
            angleField.value = $"Angle: {angle:F1}°";
        }
        if (velocityField != null)
        {
            velocityField.value = $"Vel: {velocity:F1} m/s";
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector3.zero;
        
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }
    
    private void UpdateDragLine()
    {
        dragLine.positionCount = 2;
        dragLine.SetPosition(0, dragStartPos);
        dragLine.SetPosition(1, dragCurrentPos);
    }
    
    private void UpdateTrajectoryPrediction(Vector3 dragVector)
    {
        if (trajectoryLine == null) return;
        
        Vector3 velocity = dragVector * velocityMultiplier;
        Vector3 startPos = firePoint != null ? firePoint.position : dragStartPos;
        Vector3[] points = new Vector3[trajectoryPoints];
        bool predictedPlaced = false;
        bool drawPredicted = false; // disabled per request
        
        for (int i = 0; i < trajectoryPoints; i++)
        {
            float time = i * trajectoryTimeStep;
            points[i] = CalculatePositionAtTime(startPos, velocity, time);
            
            // Stop if trajectory goes below ground
            if (points[i].y < 0)
            {
                trajectoryLine.positionCount = i + 1;
                trajectoryLine.SetPositions(points);
                // Place predicted impact marker and draw predicted line if toggle is ON
                if (drawPredicted)
                {
                    Vector3 pPrev = i > 0 ? points[i - 1] : dragStartPos;
                    Vector3 pCurr = points[i];
                    // Linear interpolate to y=0
                    float t = Mathf.InverseLerp(pPrev.y, pCurr.y, 0f);
                    Vector3 impact = Vector3.Lerp(pPrev, pCurr, t);
                    PlacePredictedImpactMarker(impact);
                    predictedPlaced = true;
                    // Predicted line disabled
                }
                return;
            }
        }
        
        trajectoryLine.positionCount = trajectoryPoints;
        trajectoryLine.SetPositions(points);
        // If never went below ground or toggle off, clear predicted visuals
        if (!predictedPlaced || !drawPredicted)
        {
            RemovePredictedImpactMarker();
            // Predicted line disabled
        }
    }
    
    private Vector3 CalculatePositionAtTime(Vector3 start, Vector3 velocity, float time)
    {
        // Projectile motion equation: p = p0 + v*t + 0.5*g*t^2
        Vector3 gravity = Physics.gravity;
        return start + velocity * time + 0.5f * gravity * time * time;
    }

    private void PlacePredictedImpactMarker(Vector3 position)
    {
        if (predictedImpactMarker == null)
        {
            predictedImpactMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            predictedImpactMarker.name = "PredictedImpactMarker";
            predictedImpactMarker.transform.localScale = new Vector3(0.4f, 0.05f, 0.4f);
            var renderer = predictedImpactMarker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 1f, 0.6f, 0.9f);
            }
            // Disable collider to ensure no accidental projectile collisions
            var markerCol = predictedImpactMarker.GetComponent<Collider>();
            if (markerCol != null)
            {
                markerCol.enabled = false;
            }
        }
        // Place on ground (y=0)
        predictedImpactMarker.transform.position = new Vector3(position.x, 0f, position.z);
        predictedImpactMarker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        predictedImpactMarker.SetActive(true);
    }

    private void RemovePredictedImpactMarker()
    {
        if (predictedImpactMarker != null)
        {
            predictedImpactMarker.SetActive(false);
        }
    }
    
    private void SendLaunchCommand(float angle, float velocity)
    {
        PlayerLauncher launcher = GetComponent<PlayerLauncher>();
        if (launcher != null)
        {
            launcher.LaunchWithParameters(angle, velocity);
        }
    }
    
    private void OnDrawGizmos()
    {
        // Always show drag radius when this controller is enabled
        if (enabled && Application.isPlaying)
        {
            Gizmos.color = isDragging ? Color.yellow : Color.green;
            
            // Show screen-space detection radius
            if (Camera.main != null)
            {
                Gizmos.DrawWireSphere(transform.position, 1.5f);
            }
        }
    }
    
    // ★★★ NEW: Check if any UI panel is open ★★★
    private bool IsAnyPanelOpen()
    {
        bool settingsOpen = settingsPanel != null && settingsPanel.style.display == DisplayStyle.Flex;
        bool formulaOpen = formulaPanel != null && formulaPanel.style.display == DisplayStyle.Flex;
        return settingsOpen || formulaOpen;
    }
    
    // ★★★ FIXED: Only block if clicking on INTERACTIVE UI elements (not the root canvas)
    private bool IsClickingOnUI()
    {
        if (uiDocument == null) return false;
        
        var root = uiDocument.rootVisualElement;
        if (root == null) return false;
        
        Vector2 mousePosition = Input.mousePosition;
        
        // Clamp to screen bounds to avoid null panel errors
        if (mousePosition.x < 0 || mousePosition.x > Screen.width || 
            mousePosition.y < 0 || mousePosition.y > Screen.height)
        {
            return false;
        }
        
        var pickedElement = root.panel?.Pick(mousePosition);
        
        // ★★★ CRITICAL FIX: Ignore the root element itself (it covers the whole screen)
        // Only block if we picked an actual UI element (button, panel, field, etc.)
        if (pickedElement == null || pickedElement == root)
        {
            Debug.Log("✅ No blocking UI element at mouse position");
            return false;
        }
        
        // Check if the picked element is an interactive control or panel
        bool isInteractiveUI = pickedElement is Button || 
                               pickedElement is TextField || 
                               pickedElement is Toggle ||
                               pickedElement.name == "settingsPanel" ||
                               pickedElement.name == "formulaPanel" ||
                               pickedElement.parent?.name == "settingsPanel" ||
                               pickedElement.parent?.name == "formulaPanel";
        
        if (isInteractiveUI)
        {
            Debug.Log($"🚫 Blocked by UI element: {pickedElement.name} ({pickedElement.GetType().Name})");
        }
        else
        {
            Debug.Log($"✅ Ignoring non-blocking UI element: {pickedElement.name}");
        }
        
        return isInteractiveUI;
    }
    
    // ★★★ SIMPLE & RELIABLE: Physics raycast from mouse to detect click
    // This is called a "RAYCAST" - it shoots an invisible ray from the camera through the mouse position
    // and checks what it hits. This is the standard Unity method for 3D object selection.
    private bool IsMouseOverObject()
    {
        // Create a ray from the camera through the mouse position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        // ★★★ CRITICAL FIX: Use RaycastAll to check ALL hits, not just the first
        // This prevents other objects from blocking detection
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
        
        if (hits.Length == 0)
        {
            Debug.Log("❌ Raycast didn't hit anything");
            return false;
        }
        
        // Log all hits for debugging
        Debug.Log($"🎯 Raycast hit {hits.Length} objects:");
        foreach (RaycastHit hit in hits)
        {
            Debug.Log($"   - {hit.collider.gameObject.name} at distance {hit.distance:F2}");
            
            // Check if THIS hit is our player (or a child of our player)
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                Debug.Log($"✅ Found {gameObject.name} in raycast hits at point: {hit.point}");
                return true;
            }
        }
        
        // Our player wasn't in any of the hits
        Debug.Log($"❌ {gameObject.name} not found in raycast hits");
        return false;
    }
}
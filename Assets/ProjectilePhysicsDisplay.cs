using UnityEngine;

public class ProjectilePhysicsDisplay : MonoBehaviour
{
    [Header("3D Text Settings")]
    public int fontSize = 24; // Normal size for readability
    public Color maxHeightColor = Color.magenta;
    public Color initialVelocityColor = Color.green;
    public Color timeColor = Color.yellow;
    public Color rangeColor = Color.cyan;
    public Color velocityComponentsColor = new Color(1f, 0.7f, 0f); // orange
    public Color velocityArrowColor = new Color(0.2f, 1f, 0.8f);
    public Color apexMarkerColor = Color.magenta;
    
    // Physics tracking
    private Vector3 startPosition;
    private float maxHeight = 0f;
    private Vector3 maxHeightPosition;
    private bool hasReachedMaxHeight = false;
    private float initialVelocity = 0f;
    private float launchAngle = 0f;
    private float flightTime = 0f;
    private bool isActive = true;
    private bool hasLanded = false;
    
    // Quarter-flight markers
    private GameObject[] quarterLabels = new GameObject[3]; // 25%, 50%, 75%
    private TextMesh[] quarterTexts = new TextMesh[3];
    private float[] quarterTimes = new float[3];
    private bool[] quarterPlaced = new bool[3];
    private float estimatedFlightTime = -1f;
    private Vector3 initialVelocityVector;
    private float finalFlightTime = -1f; // actual flight time until impact/collision
    
    // 3D Text labels
    private GameObject maxHeightLabel;
    private GameObject initialVelocityLabel;
    private GameObject flightTimeLabel;
    private GameObject rangeLabel;
    private GameObject velocityComponentsLabel;
    private LineRenderer velocityArrow;
    private GameObject apexMarker;
    
    // TextMesh components
    private TextMesh maxHeightText;
    private TextMesh initialVelocityText;
    private TextMesh flightTimeText;
    private TextMesh rangeText;
    private TextMesh velocityComponentsText;
    
    private Rigidbody rb;
    private SettingsManager settingsManager;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        settingsManager = FindFirstObjectByType<SettingsManager>();
        startPosition = transform.position;
        
        if (rb != null)
        {
            initialVelocity = rb.linearVelocity.magnitude;
            
            // Calculate launch angle
            Vector3 vel = rb.linearVelocity;
            launchAngle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
            initialVelocityVector = vel; // Store full initial velocity vector
        }
        
        Create3DLabels();
        CreateVelocityArrow();
    }
    
    private void Create3DLabels()
    {
        // Max Height label (appears at peak) - NOT parented to projectile
        maxHeightLabel = new GameObject("MaxHeightLabel");
        maxHeightText = maxHeightLabel.AddComponent<TextMesh>();
        ConfigureTextMesh(maxHeightText, maxHeightColor);
        maxHeightText.text = "";
        
        // Initial Velocity label (at start position)
        initialVelocityLabel = new GameObject("InitialVelocityLabel");
        initialVelocityText = initialVelocityLabel.AddComponent<TextMesh>();
        ConfigureTextMesh(initialVelocityText, initialVelocityColor);
        
        // Flight Time label (appears at landing)
        flightTimeLabel = new GameObject("FlightTimeLabel");
        flightTimeLabel.transform.SetParent(transform);
        flightTimeText = flightTimeLabel.AddComponent<TextMesh>();
        ConfigureTextMesh(flightTimeText, timeColor);
        flightTimeText.text = "";
        
        // Range label (appears at landing)
        rangeLabel = new GameObject("RangeLabel");
        rangeLabel.transform.SetParent(transform);
        rangeText = rangeLabel.AddComponent<TextMesh>();
        ConfigureTextMesh(rangeText, rangeColor);
        rangeText.text = "";

        // Velocity components (Vx, Vy) - appears near projectile during flight
        velocityComponentsLabel = new GameObject("VelocityComponentsLabel");
        velocityComponentsLabel.transform.SetParent(transform);
        velocityComponentsText = velocityComponentsLabel.AddComponent<TextMesh>();
        ConfigureTextMesh(velocityComponentsText, velocityComponentsColor);
        velocityComponentsText.text = "";

        // Impact stats removed per request

        // Quarter-flight labels
        for (int i = 0; i < 3; i++)
        {
            quarterLabels[i] = new GameObject($"FlightLabel_t{(i+1)*25}");
            quarterTexts[i] = quarterLabels[i].AddComponent<TextMesh>();
            ConfigureTextMesh(quarterTexts[i], new Color(1f, 1f, 1f));
            quarterTexts[i].text = "";
            quarterPlaced[i] = false;
        }
    }
    
    private void ConfigureTextMesh(TextMesh textMesh, Color color)
    {
        textMesh.fontSize = fontSize;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.1f; // Normal character size
        textMesh.fontStyle = FontStyle.Bold;
        
        // Set to UI layer (layer 5) to exclude from post-processing
        textMesh.gameObject.layer = 5; // UI layer is typically not affected by post-processing
    }

    private void CreateVelocityArrow()
    {
        velocityArrow = gameObject.AddComponent<LineRenderer>();
        velocityArrow.positionCount = 2;
        velocityArrow.startWidth = 0.05f;
        velocityArrow.endWidth = 0.02f;
        velocityArrow.material = new Material(Shader.Find("Sprites/Default"));
        velocityArrow.startColor = velocityArrowColor;
        velocityArrow.endColor = velocityArrowColor;
    }

    private void Update()
    {
        if (!isActive) return;
        
        flightTime += Time.deltaTime;

        // Estimate total flight time once using ballistic equation to y=0
        if (estimatedFlightTime < 0f && rb != null)
        {
            float y0 = startPosition.y;
            float v0y = rb.linearVelocity.y;
            float g = Physics.gravity.y;
            // Solve y0 + v0y t + 0.5 g t^2 = 0 for t > 0
            float a = 0.5f * g;
            float b = v0y;
            float c = y0;
            float disc = b*b - 4*a*c;
            if (disc >= 0f)
            {
                float sqrtDisc = Mathf.Sqrt(disc);
                float t1 = (-b + sqrtDisc) / (2*a);
                float t2 = (-b - sqrtDisc) / (2*a);
                float t = Mathf.Max(t1, t2);
                if (t > 0f && float.IsFinite(t))
                {
                    estimatedFlightTime = t;
                    quarterTimes[0] = 0.25f * t;
                    quarterTimes[1] = 0.5f * t;
                    quarterTimes[2] = 0.75f * t;
                }
            }
        }
        
        // Track max height
        if (transform.position.y > maxHeight)
        {
            maxHeight = transform.position.y;
            maxHeightPosition = transform.position;
            hasReachedMaxHeight = false;
        }
        else if (!hasReachedMaxHeight && rb != null && rb.linearVelocity.y < 0)
        {
            // Reached peak and now descending
            hasReachedMaxHeight = true;
            ShowMaxHeightLabel();
        }
        
        UpdateInitialVelocityLabel();
        UpdateVelocityComponents();
        UpdateVelocityArrow();

        UpdateQuarterFlightLabels();
    }

    // Ideal ballistic position along the projectile path at time t
    private Vector3 PositionAtTime(float t)
    {
        Vector3 g = Physics.gravity;
        return startPosition + initialVelocityVector * t + 0.5f * g * t * t;
    }
    
    private void ShowMaxHeightLabel()
    {
        float heightAboveStart = maxHeight - startPosition.y;
        string text = "Max Height\nH = " + heightAboveStart.ToString("F2") + "m";
        
        // ★★★ FIXED: Position label AT max height position, not above projectile
        maxHeightLabel.transform.SetParent(null); // Detach from projectile
        maxHeightLabel.transform.position = maxHeightPosition + Vector3.up * 0.5f; // Small offset above max point
        maxHeightLabel.transform.rotation = Quaternion.identity;
        
        if (maxHeightText != null)
            maxHeightText.text = text;

        // Apex marker sphere
        if (apexMarker == null)
        {
            apexMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            apexMarker.name = "ApexMarker";
            apexMarker.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            var renderer = apexMarker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = apexMarkerColor;
            }
            // Disable collider to prevent projectile collisions at apex
            var apexCol = apexMarker.GetComponent<Collider>();
            if (apexCol != null)
            {
                apexCol.enabled = false;
            }
            // Optional: place on an ignored layer if project uses collision layers
            // apexMarker.layer = LayerMask.NameToLayer("IgnoreProjectile");
        }
        apexMarker.transform.position = maxHeightPosition;
        
        Debug.Log($"📊 Max Height: {heightAboveStart:F2}m at position {maxHeightPosition}");
    }
    
    private void UpdateInitialVelocityLabel()
    {
        // Show initial velocity at start position
        string text = "V0 = " + initialVelocity.ToString("F1") + " m/s\nAngle = " + launchAngle.ToString("F1") + "deg";
        
        Vector3 labelPos = startPosition + Vector3.up * 0.5f + Vector3.left * 1f;
        initialVelocityLabel.transform.position = labelPos;
        initialVelocityLabel.transform.rotation = Quaternion.identity;
        
        if (initialVelocityText != null)
            initialVelocityText.text = text;
    }

    private void UpdateVelocityComponents()
    {
        if (rb == null || hasLanded) return;
        Vector3 v = rb.linearVelocity;
        string text = $"Vx = {v.x:F2} m/s\nVy = {v.y:F2} m/s";
        Vector3 labelPos = transform.position + Vector3.up * 0.6f + Vector3.right * 0.8f;
        velocityComponentsLabel.transform.position = labelPos;
        velocityComponentsLabel.transform.rotation = Quaternion.identity;
        velocityComponentsText.text = text;
    }

    private void UpdateQuarterFlightLabels()
    {
        if (rb == null) return;
        // Use finalFlightTime when landed; otherwise use estimatedFlightTime
        float referenceTime = hasLanded && finalFlightTime > 0f ? finalFlightTime : estimatedFlightTime;
        if (referenceTime <= 0f) return;
        // Prepare quarter times if not set from reference
        if (quarterTimes[0] <= 0f || quarterTimes[1] <= 0f || quarterTimes[2] <= 0f)
        {
            quarterTimes[0] = 0.25f * referenceTime;
            quarterTimes[1] = 0.5f * referenceTime;
            quarterTimes[2] = 0.75f * referenceTime;
        }
        for (int i = 0; i < 3; i++)
        {
            if (!quarterPlaced[i] && flightTime >= quarterTimes[i])
            {
                Vector3 v = rb.linearVelocity;
                float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                float height = transform.position.y - startPosition.y;
                float timeAtPoint = quarterTimes[i];
                
                // Display time and physics data without percentage
                quarterTexts[i].text = $"t={timeAtPoint:F2}s\nVx={v.x:F2} Vy={v.y:F2}\nAngle={angle:F1}°\nHeight={height:F2}m";
                
                // Place directly on the ideal trajectory point for that time
                Vector3 p = PositionAtTime(quarterTimes[i]);
                // Small visual offset upward to avoid z-fighting with the line
                Vector3 offset = Vector3.up * 0.2f;
                quarterLabels[i].transform.position = p + offset;
                quarterLabels[i].transform.rotation = Quaternion.identity;
                quarterPlaced[i] = true;
            }
        }
    }

    private void UpdateVelocityArrow()
    {
        if (rb == null || velocityArrow == null || hasLanded) return;
        Vector3 v = rb.linearVelocity;
        float scale = 0.2f + Mathf.Clamp(v.magnitude * 0.03f, 0f, 1f);
        Vector3 start = transform.position;
        Vector3 end = start + v.normalized * scale;
        velocityArrow.SetPosition(0, start);
        velocityArrow.SetPosition(1, end);
    }
    
    public void OnProjectileLanded()
    {
        isActive = false;
        hasLanded = true;
        // Capture actual flight time on impact
        finalFlightTime = flightTime;
        // Reset quarter times from actual flight time so 75% doesn't coincide with impact
        quarterTimes[0] = 0.25f * finalFlightTime;
        quarterTimes[1] = 0.5f * finalFlightTime;
        quarterTimes[2] = 0.75f * finalFlightTime;
        
        // Show final statistics
        float range = Mathf.Abs(transform.position.x - startPosition.x);
        
        // Flight time label
        string timeText = "Time\nt = " + flightTime.ToString("F2") + "s";
        Vector3 timePos = transform.position + Vector3.up * 1.5f;
        flightTimeLabel.transform.position = timePos;
        flightTimeLabel.transform.rotation = Quaternion.identity;
        
        if (flightTimeText != null)
            flightTimeText.text = timeText;
        
        // Range label
        string rangeTextStr = "Range\nR = " + range.ToString("F2") + "m";
        Vector3 rangePos = transform.position + Vector3.down * 0.8f;
        rangeLabel.transform.position = rangePos;
        rangeLabel.transform.rotation = Quaternion.identity;
        
        if (rangeText != null)
            rangeText.text = rangeTextStr;

        // Impact speed/angle display removed
        
        Debug.Log($"📊 Flight Time: {flightTime:F2}s, Range: {range:F2}m");
        // Remove velocity arrow on landing
        if (velocityArrow != null) velocityArrow.enabled = false;
        // Hide in-flight components
        velocityComponentsText.text = "";
        // Optionally remove apex marker shortly after landing
        if (apexMarker != null)
        {
            Destroy(apexMarker, 3f);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up labels
        if (maxHeightLabel != null) Destroy(maxHeightLabel);
        if (initialVelocityLabel != null) Destroy(initialVelocityLabel);
        if (flightTimeLabel != null) Destroy(flightTimeLabel);
        if (rangeLabel != null) Destroy(rangeLabel);
        if (velocityComponentsLabel != null) Destroy(velocityComponentsLabel);
        // Impact label removed
        if (velocityArrow != null) Destroy(velocityArrow);
        if (apexMarker != null) Destroy(apexMarker);
        for (int i = 0; i < 3; i++)
        {
            if (quarterLabels[i] != null) Destroy(quarterLabels[i]);
        }
    }
}   
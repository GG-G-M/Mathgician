using UnityEngine;

public class ProjectilePhysicsDisplay : MonoBehaviour
{
    [Header("3D Text Settings")]
    public int fontSize = 24;
    public Color maxHeightColor = Color.magenta;
    public Color initialVelocityColor = Color.green;
    public Color timeColor = Color.yellow;
    public Color rangeColor = Color.cyan;
    
    // Physics tracking
    private Vector3 startPosition;
    private float maxHeight = 0f;
    private Vector3 maxHeightPosition;
    private bool hasReachedMaxHeight = false;
    private float initialVelocity = 0f;
    private float launchAngle = 0f;
    private float flightTime = 0f;
    private bool isActive = true;
    
    // 3D Text labels
    private GameObject maxHeightLabel;
    private GameObject initialVelocityLabel;
    private GameObject flightTimeLabel;
    private GameObject rangeLabel;
    
    // TextMesh components
    private TextMesh maxHeightText;
    private TextMesh initialVelocityText;
    private TextMesh flightTimeText;
    private TextMesh rangeText;
    
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        
        if (rb != null)
        {
            initialVelocity = rb.linearVelocity.magnitude;
            
            // Calculate launch angle
            Vector3 vel = rb.linearVelocity;
            launchAngle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
        }
        
        Create3DLabels();
    }
    
    private void Create3DLabels()
    {
        // Max Height label (appears at peak)
        maxHeightLabel = new GameObject("MaxHeightLabel");
        maxHeightLabel.transform.SetParent(transform);
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
    }
    
    private void ConfigureTextMesh(TextMesh textMesh, Color color)
    {
        textMesh.fontSize = fontSize;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.08f;
        textMesh.fontStyle = FontStyle.Bold;
    }

    private void Update()
    {
        if (!isActive) return;
        
        flightTime += Time.deltaTime;
        
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
    }
    
    private void ShowMaxHeightLabel()
    {
        float heightAboveStart = maxHeight - startPosition.y;
        string text = "Max Height\nH = " + heightAboveStart.ToString("F2") + "m";
        
        maxHeightLabel.transform.position = maxHeightPosition + Vector3.up * 1f;
        maxHeightLabel.transform.rotation = Quaternion.identity;
        
        if (maxHeightText != null)
            maxHeightText.text = text;
        
        Debug.Log($"📊 Max Height: {heightAboveStart:F2}m");
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
    
    public void OnProjectileLanded()
    {
        isActive = false;
        
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
        
        Debug.Log($"📊 Flight Time: {flightTime:F2}s, Range: {range:F2}m");
    }
    
    private void OnDestroy()
    {
        // Clean up labels
        if (maxHeightLabel != null) Destroy(maxHeightLabel);
        if (initialVelocityLabel != null) Destroy(initialVelocityLabel);
        if (flightTimeLabel != null) Destroy(flightTimeLabel);
        if (rangeLabel != null) Destroy(rangeLabel);
    }
}   
using UnityEngine;
using UnityEngine.UIElements;
using TMPro; // For TextMeshPro

public class DistanceDisplay : MonoBehaviour
{
    [Header("Players")]
    public Transform playerA;
    public Transform playerB;
    
    [Header("UI (Optional - for UI labels)")]
    public UIDocument uiDocument;
    
    [Header("Line Renderers")]
    public LineRenderer straightLine;
    public LineRenderer horizontalLine;
    
    [Header("3D Text Settings")]
    public Font textFont; // Leave null to use default Arial
    public int fontSize = 40; // ★★★ INCREASED from 20
    public Color textColor = Color.black; // Changed to black for visibility on light backgrounds

    // UI Elements (optional)
    private Label straightDistanceText;
    private Label horizontalDistanceText;
    private Label verticalDistanceText;
    private Label projectileHorizontalText;
    private Label projectileTrajectoryText;
    
    // 3D Text Objects
    private GameObject straightDistanceLabel;
    private GameObject horizontalDistanceLabel;
    private GameObject verticalDistanceLabel;
    private TextMesh straightTextMesh;
    private TextMesh horizontalTextMesh;
    private TextMesh verticalTextMesh;

    private void Start()
    {
        // Get UI elements (optional)
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            straightDistanceText = root.Q<Label>("straightDistanceLabel");
            horizontalDistanceText = root.Q<Label>("horizontalDistanceLabel");
            verticalDistanceText = root.Q<Label>("verticalDistanceLabel");
            projectileHorizontalText = root.Q<Label>("projectileHorizontalLabel");
            projectileTrajectoryText = root.Q<Label>("projectileTrajectoryLabel");
        }
        
        // Setup line renderers with thicker lines
        SetupLineRenderer(straightLine, Color.cyan, 0.15f); // ★★★ INCREASED from 0.05f
        SetupLineRenderer(horizontalLine, Color.yellow, 0.10f); // ★★★ INCREASED from 0.03f
        
        // Create 3D text labels
        Create3DLabels();
    }

    private void SetupLineRenderer(LineRenderer line, Color color, float width)
    {
        if (line != null)
        {
            line.positionCount = 0;
            line.startWidth = width;
            line.endWidth = width;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.useWorldSpace = true;
        }
        else
        {
            Debug.LogWarning("LineRenderer not assigned!");
        }
    }
    
    private void Create3DLabels()
    {
        // Straight distance label - BRIGHT CYAN for high visibility
        straightDistanceLabel = new GameObject("StraightDistanceLabel");
        straightTextMesh = straightDistanceLabel.AddComponent<TextMesh>();
        ConfigureTextMesh(straightTextMesh, Color.black);
        
        // Horizontal distance label - BRIGHT YELLOW for high visibility
        horizontalDistanceLabel = new GameObject("HorizontalDistanceLabel");
        horizontalTextMesh = horizontalDistanceLabel.AddComponent<TextMesh>();
        ConfigureTextMesh(horizontalTextMesh, Color.black);
        
        // Vertical distance label - BRIGHT GREEN for high visibility
        verticalDistanceLabel = new GameObject("VerticalDistanceLabel");
        verticalTextMesh = verticalDistanceLabel.AddComponent<TextMesh>();
        ConfigureTextMesh(verticalTextMesh, Color.black);
        
        Debug.Log("✅ 3D distance labels created!");
    }
    
    private void ConfigureTextMesh(TextMesh textMesh, Color color)
    {
        textMesh.fontSize = fontSize;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.25f; // ★★★ INCREASED from 0.1f for better visibility
        
        // Set to UI layer (layer 5) to exclude from post-processing
        textMesh.gameObject.layer = 5;
        
        if (textFont != null)
        {
            textMesh.font = textFont;
            textMesh.GetComponent<MeshRenderer>().material = textFont.material;
        }
    }

    private void Update()
    {
        if (playerA != null && playerB != null)
        {
            UpdateDistanceUI();
            UpdateStraightLine();
            UpdateHorizontalLine();
            Update3DLabels();
        }
    }
    
    private void UpdateDistanceUI()
    {
        float straightDistance = Vector3.Distance(playerA.position, playerB.position);
        float horizontalPathDistance = CalculateHorizontalPathDistance();
        float verticalDistance = CalculateVerticalDistance();
        
        // Update UI labels if they exist
        if (straightDistanceText != null)
            straightDistanceText.text = $"Straight Distance: {straightDistance:F1}m";
        
        if (horizontalDistanceText != null)
            horizontalDistanceText.text = $"Horizontal Path: {horizontalPathDistance:F1}m";
            
        if (verticalDistanceText != null)
            verticalDistanceText.text = $"Vertical Distance: {verticalDistance:F1}m";
    }
    
    private float CalculateHorizontalPathDistance()
    {
        float horizontalLevel = playerB.position.y;
        float verticalA = Mathf.Abs(playerA.position.y - horizontalLevel);
        float horizontal = Mathf.Abs(playerA.position.x - playerB.position.x);
        float verticalB = Mathf.Abs(playerB.position.y - horizontalLevel);
        
        return verticalA + horizontal + verticalB;
    }
    
    private float CalculateVerticalDistance()
    {
        return playerA.position.y - playerB.position.y;
    }
    
    private void UpdateStraightLine()
    {
        if (straightLine == null) return;
        
        straightLine.positionCount = 2;
        straightLine.SetPosition(0, playerA.position);
        straightLine.SetPosition(1, playerB.position);
    }
    
    private void UpdateHorizontalLine()
    {
        if (horizontalLine == null) return;
        
        float horizontalLevel = playerB.position.y;
        
        Vector3 pointA = playerA.position;
        Vector3 pointAHorizontal = new Vector3(playerA.position.x, horizontalLevel, playerA.position.z);
        Vector3 pointBHorizontal = new Vector3(playerB.position.x, horizontalLevel, playerB.position.z);
        Vector3 pointB = playerB.position;
        
        horizontalLine.positionCount = 4;
        horizontalLine.SetPosition(0, pointA);
        horizontalLine.SetPosition(1, pointAHorizontal);
        horizontalLine.SetPosition(2, pointBHorizontal);
        horizontalLine.SetPosition(3, pointB);
    }
    
    private void Update3DLabels()
    {
        // Straight distance label - positioned at midpoint of straight line
        if (straightTextMesh != null && straightLine != null && straightLine.positionCount >= 2)
        {
            float straightDistance = Vector3.Distance(playerA.position, playerB.position);
            Vector3 midpoint = (playerA.position + playerB.position) / 2f;
            
            // Offset slightly above the line
            Vector3 labelPos = midpoint + Vector3.up * 0.5f;
            straightDistanceLabel.transform.position = labelPos;
            straightTextMesh.text = $"Distance: {straightDistance:F2}m";
            
            // Face the camera
            straightDistanceLabel.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        
        // Horizontal distance label - positioned at middle of horizontal segment
        if (horizontalTextMesh != null && horizontalLine != null && horizontalLine.positionCount >= 4)
        {
            float horizontalLevel = playerB.position.y;
            Vector3 pointAHorizontal = new Vector3(playerA.position.x, horizontalLevel, playerA.position.z);
            Vector3 pointBHorizontal = new Vector3(playerB.position.x, horizontalLevel, playerB.position.z);
            
            float horizontalDist = Mathf.Abs(playerA.position.x - playerB.position.x);
            Vector3 horizontalMidpoint = (pointAHorizontal + pointBHorizontal) / 2f;
            
            // Offset slightly below the line
            Vector3 labelPos = horizontalMidpoint + Vector3.down * 0.5f;
            horizontalDistanceLabel.transform.position = labelPos;
            horizontalTextMesh.text = $"Distance: {horizontalDist:F2}m";
            
            horizontalDistanceLabel.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        
        // Vertical distance label - positioned beside vertical segment (shows HEIGHT)
        if (verticalTextMesh != null && playerA != null && playerB != null)
        {
            float verticalDist = Mathf.Abs(playerA.position.y - playerB.position.y);
            float horizontalLevel = playerB.position.y;
            
            Vector3 topPoint = playerA.position;
            Vector3 bottomPoint = new Vector3(playerA.position.x, horizontalLevel, playerA.position.z);
            Vector3 verticalMidpoint = (topPoint + bottomPoint) / 2f;
            
            // Offset to the side of the vertical line
            Vector3 labelPos = verticalMidpoint + Vector3.left * 1f;
            verticalDistanceLabel.transform.position = labelPos;
            verticalTextMesh.text = $"Height: {verticalDist:F2}m";
            
            verticalDistanceLabel.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
    
    public void UpdateProjectileDistances(float horizontalDistance, float trajectoryDistance)
    {
        // Update UI labels if they exist
        if (projectileHorizontalText != null)
            projectileHorizontalText.text = $"Proj Horizontal: {horizontalDistance:F2}m";
        
        if (projectileTrajectoryText != null)
            projectileTrajectoryText.text = $"Proj Trajectory: {trajectoryDistance:F2}m";
        
        // Log to console
        Debug.Log($"📏 Projectile - Horizontal: {horizontalDistance:F2}m, Trajectory: {trajectoryDistance:F2}m");
    }
    
    public void ClearProjectileDistances()
    {
        if (projectileHorizontalText != null)
            projectileHorizontalText.text = "Proj Horizontal: 0.00m";
        if (projectileTrajectoryText != null)
            projectileTrajectoryText.text = "Proj Trajectory: 0.00m";
    }
}
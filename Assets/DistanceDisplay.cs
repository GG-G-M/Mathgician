using UnityEngine;
using UnityEngine.UIElements;

public class DistanceDisplay : MonoBehaviour
{
    [Header("Players")]
    public Transform playerA;
    public Transform playerB;
    
    [Header("UI")]
    public UIDocument uiDocument;
    
    [Header("Line Renderers")]
    public LineRenderer straightLine;  // The LineRenderer on StraightLine GameObject
    public LineRenderer horizontalLine; // The LineRenderer on HorizontalLine GameObject

    // UI Elements
    private Label straightDistanceText;
    private Label horizontalDistanceText;
    private Label projectileHorizontalText;
    private Label projectileTrajectoryText;

    private void Start()
    {
        // Get UI elements
        var root = uiDocument.rootVisualElement;
        straightDistanceText = root.Q<Label>("straightDistanceLabel");
        horizontalDistanceText = root.Q<Label>("horizontalDistanceLabel");
        projectileHorizontalText = root.Q<Label>("projectileHorizontalLabel");
        projectileTrajectoryText = root.Q<Label>("projectileTrajectoryLabel");
        
        // Setup line renderers
        SetupLineRenderer(straightLine, Color.cyan, 0.05f);       // Blue straight line
        SetupLineRenderer(horizontalLine, Color.yellow, 0.03f);   // Yellow horizontal path
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

    private void Update()
    {
        if (playerA != null && playerB != null)
        {
            UpdateDistanceUI();
            UpdateStraightLine();
            UpdateHorizontalLine();
        }
    }
    
    private void UpdateDistanceUI()
    {
        float straightDistance = Vector3.Distance(playerA.position, playerB.position);
        float horizontalPathDistance = CalculateHorizontalPathDistance();
        
        if (straightDistanceText != null)
            straightDistanceText.text = $"Straight Distance: {straightDistance:F1}m";
        
        if (horizontalDistanceText != null)
            horizontalDistanceText.text = $"Horizontal Path: {horizontalPathDistance:F1}m";
    }
    
    private float CalculateHorizontalPathDistance()
    {
        // Use Player B's height as the horizontal level
        float horizontalLevel = playerB.position.y;
        
        // Calculate horizontal path: A → horizontal level → move horizontally → B
        float verticalA = Mathf.Abs(playerA.position.y - horizontalLevel);
        float horizontal = Mathf.Abs(playerA.position.x - playerB.position.x);
        float verticalB = Mathf.Abs(playerB.position.y - horizontalLevel);
        
        return verticalA + horizontal + verticalB;
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
        
        // Use Player B's height as the horizontal level
        float horizontalLevel = playerB.position.y;
        
        // Create horizontal path based on who is higher
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
    
    public void UpdateProjectileDistances(float horizontalDistance, float trajectoryDistance)
    {
        if (projectileHorizontalText != null)
            projectileHorizontalText.text = $"Proj Horizontal: {horizontalDistance:F2}m";
        
        if (projectileTrajectoryText != null)
            projectileTrajectoryText.text = $"Proj Trajectory: {trajectoryDistance:F2}m";
    }
    
    public void ClearProjectileDistances()
    {
        if (projectileHorizontalText != null)
            projectileHorizontalText.text = "Proj Horizontal: 0.00m";
        if (projectileTrajectoryText != null)
            projectileTrajectoryText.text = "Proj Trajectory: 0.00m";
    }
}
using UnityEngine;
using UnityEngine.UIElements;

public class DistanceDisplay : MonoBehaviour
{
    [Header("Players")]
    public Transform playerA;
    public Transform playerB;
    
    [Header("UI")]
    public UIDocument uiDocument;
    
    [Header("Line Renderer")]
    public LineRenderer distanceLine;
    
    private Label distanceText;

    private void Start()
    {
        // Get UI elements
        var root = uiDocument.rootVisualElement;
        distanceText = root.Q<Label>("distanceLabel");
        
        // Setup line renderer
        if (distanceLine != null)
        {
            distanceLine.positionCount = 2;
            distanceLine.startWidth = 0.1f;
            distanceLine.endWidth = 0.1f;
            distanceLine.material = new Material(Shader.Find("Sprites/Default"));
            distanceLine.startColor = Color.white;
            distanceLine.endColor = Color.white;
        }
    }

    private void Update()
    {
        if (playerA == null || playerB == null) return;
        
        // Calculate distance
        float distance = Vector3.Distance(playerA.position, playerB.position);
        
        // Update UI text
        if (distanceText != null)
            distanceText.text = $"Distance: {distance:F1}m";
        
        // Update line between players
        UpdateDistanceLine();
    }
    
    private void UpdateDistanceLine()
    {
        if (distanceLine == null) return;
        
        Vector3 playerAPos = playerA.position + Vector3.up * 0.5f; // Slightly above players
        Vector3 playerBPos = playerB.position + Vector3.up * 0.5f;
        
        distanceLine.SetPosition(0, playerAPos);
        distanceLine.SetPosition(1, playerBPos);
    }
}
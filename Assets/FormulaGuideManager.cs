using UnityEngine;
using UnityEngine.UIElements;

public class FormulaGuideManager : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;
    
    private Button formulaGuideButton;
    private VisualElement formulaPanel;
    private Button closeFormulaButton;
    private ScrollView formulaScrollView;

    private void Start()
    {
        SetupUI();
    }
    
    private void SetupUI()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("⚠️ UIDocument not assigned to FormulaGuideManager!");
            return;
        }
        
        var root = uiDocument.rootVisualElement;
        
        // Query for formula guide button and panel
        formulaGuideButton = root.Q<Button>("formulaGuideButton");
        formulaPanel = root.Q<VisualElement>("formulaPanel");
        closeFormulaButton = root.Q<Button>("closeFormulaButton");
        formulaScrollView = root.Q<ScrollView>("formulaScrollView");
        
        if (formulaGuideButton != null)
        {
            formulaGuideButton.clicked += OpenFormulaPanel;
            Debug.Log("✅ Formula Guide button found!");
        }
        else
        {
            Debug.LogWarning("⚠️ 'formulaGuideButton' not found in UI!");
        }
        
        if (formulaPanel != null)
        {
            formulaPanel.style.display = DisplayStyle.None;
            PopulateFormulas();
        }
        else
        {
            Debug.LogWarning("⚠️ 'formulaPanel' not found in UI!");
        }
        
        if (closeFormulaButton != null)
        {
            closeFormulaButton.clicked += CloseFormulaPanel;
        }
    }
    
    private void OpenFormulaPanel()
    {
        if (formulaPanel != null)
        {
            formulaPanel.style.display = DisplayStyle.Flex;
            Debug.Log("📐 Formula guide opened");
        }
    }
    
    private void CloseFormulaPanel()
    {
        if (formulaPanel != null)
        {
            formulaPanel.style.display = DisplayStyle.None;
            Debug.Log("📐 Formula guide closed");
        }
    }
    
    private void PopulateFormulas()
    {
        if (formulaScrollView == null) return;
        
        // Clear existing content
        formulaScrollView.Clear();
        
        // Add formulas focused on calculating from distance
        AddFormulaSection("🎯 SOLVING FOR ANGLE & VELOCITY FROM DISTANCE", "");
        
        AddFormulaSeparator();
        
        AddFormulaSection("📏 Given Horizontal Distance (R), find Velocity & Angle", 
            "If you know the range R and want to find V₀ and θ:\n\n" +
            "1. Choose your angle θ (suggest 30-60°)\n" +
            "2. Calculate required velocity:\n" +
            "   V₀ = √(R × g / sin(2θ))\n\n" +
            "Example: R = 15m, θ = 45°, g = 9.81\n" +
            "   V₀ = √(15 × 9.81 / sin(90°))\n" +
            "   V₀ = √(147.15 / 1)\n" +
            "   V₀ = 12.13 m/s"
        );
        
        AddFormulaSeparator();
        
        AddFormulaSection("📏 Given Distance, find Angle (with fixed velocity)",
            "If you know R and V₀, solve for θ:\n\n" +
            "   sin(2θ) = (R × g) / V₀²\n" +
            "   2θ = arcsin((R × g) / V₀²)\n" +
            "   θ = arcsin((R × g) / V₀²) / 2\n\n" +
            "Example: R = 20m, V₀ = 15 m/s\n" +
            "   sin(2θ) = (20 × 9.81) / 15²\n" +
            "   sin(2θ) = 196.2 / 225 = 0.872\n" +
            "   2θ = 60.6°\n" +
            "   θ = 30.3°"
        );
        
        AddFormulaSeparator();
        
        AddFormulaSection("🎯 Quick Distance Estimation",
            "• At 45°: R = V₀² / g\n" +
            "  (45° gives maximum range)\n\n" +
            "• At 30°: R ≈ 0.866 × V₀² / g\n" +
            "• At 60°: R ≈ 0.866 × V₀² / g\n\n" +
            "Note: 30° and 60° give same range!\n" +
            "Lower angle = flatter arc\n" +
            "Higher angle = higher peak"
        );
        
        AddFormulaSeparator();
        
        AddFormulaSection("📐 Core Range Formula",
            "• Range Formula:\n  R = V₀² × sin(2θ) / g\n\n" +
            "Rearranged:\n" +
            "• Find Velocity:\n  V₀ = √(R × g / sin(2θ))\n\n" +
            "• Find Angle:\n  θ = arcsin(R × g / V₀²) / 2"
        );
        
        AddFormulaSeparator();
        
        AddFormulaSection("📊 Distance Breakdown",
            "Horizontal Distance:\n" +
            "  R = V₀ × cos(θ) × t\n\n" +
            "Where flight time:\n" +
            "  t = 2 × V₀ × sin(θ) / g\n\n" +
            "Combined:\n" +
            "  R = V₀² × sin(2θ) / g"
        );
        
        AddFormulaSeparator();
        
        AddFormulaSection("🔢 Step-by-Step Example",
            "Problem: Hit a target 25m away\n\n" +
            "Step 1: Choose angle (try 40°)\n" +
            "Step 2: Calculate velocity needed:\n" +
            "   V₀ = √(R × g / sin(2θ))\n" +
            "   V₀ = √(25 × 9.81 / sin(80°))\n" +
            "   V₀ = √(245.25 / 0.985)\n" +
            "   V₀ = √248.98\n" +
            "   V₀ = 15.78 m/s\n\n" +
            "Result: Fire at 40° with 15.78 m/s!"
        );
        
        AddFormulaSeparator();
        
        AddFormulaSection("⚡ Quick Reference",
            "• V₀ = Initial Velocity (m/s)\n" +
            "• θ = Launch Angle (degrees)\n" +
            "• g = 9.81 m/s² (gravity)\n" +
            "• R = Horizontal Range (meters)\n" +
            "• sin(2θ) = 2×sin(θ)×cos(θ)\n\n" +
            "Pro Tip: Use arcsin to convert\n" +
            "from sine value back to angle!"
        );
        
        AddFormulaSeparator();
        
        AddFormulaSection("🎯 Strategy Tips",
            "• Measure distance to opponent first\n" +
            "• Pick an angle (30-60° works well)\n" +
            "• Calculate velocity needed\n" +
            "• Adjust if projectile falls short/long\n" +
            "• Higher angle = safer over obstacles\n" +
            "• Lower angle = faster, flatter shot"
        );
    }
    
    private void AddFormulaSection(string title, string content)
    {
        var section = new VisualElement();
        section.style.marginBottom = 15;
        section.style.paddingLeft = 10;
        section.style.paddingRight = 10;
        
        var titleLabel = new Label(title);
        titleLabel.style.fontSize = 16;
        titleLabel.style.color = Color.cyan;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 8;
        
        section.Add(titleLabel);
        
        if (!string.IsNullOrEmpty(content))
        {
            var contentLabel = new Label(content);
            contentLabel.style.fontSize = 13;
            contentLabel.style.color = Color.white;
            contentLabel.style.whiteSpace = WhiteSpace.Normal;
            contentLabel.style.flexWrap = Wrap.Wrap;
            
            section.Add(contentLabel);
        }
        
        if (formulaScrollView != null)
        {
            formulaScrollView.Add(section);
        }
    }
    
    private void AddFormulaSeparator()
    {
        var separator = new VisualElement();
        separator.style.height = 2;
        separator.style.backgroundColor = new Color(1, 1, 1, 0.2f);
        separator.style.marginTop = 10;
        separator.style.marginBottom = 10;
        
        if (formulaScrollView != null)
        {
            formulaScrollView.Add(separator);
        }
    }
}
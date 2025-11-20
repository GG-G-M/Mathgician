using UnityEngine;
using UnityEngine.UIElements;

public class PlayerLauncher : MonoBehaviour
{
    public UIDocument uiDocument;
    public GameObject projectilePrefab;
    public Transform firePoint;

    private TextField angleField;
    private TextField velocityField;
    private Button fireButton;

    private void Start()
    {
        // Get root visual element
        var root = uiDocument.rootVisualElement;

        // Get the UI elements by their names
        angleField = root.Q<TextField>("angleField");
        velocityField = root.Q<TextField>("velocityField");
        fireButton = root.Q<Button>("fireButton");

        // Register button click event
        fireButton.clicked += FireProjectile;
    }

    private void FireProjectile()
    {
        // Read values
        float angle = float.Parse(angleField.value);
        float velocity = float.Parse(velocityField.value);

        // Convert angle to radians
        float rad = angle * Mathf.Deg2Rad;

        // Compute direction
        Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);

        // Spawn projectile
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Rigidbody rb = proj.GetComponent<Rigidbody>();

        // Apply velocity
        rb.linearVelocity = dir * velocity;

        // Prevent collision with player
        Physics.IgnoreCollision(GetComponent<Collider>(), proj.GetComponent<Collider>());
    }
}

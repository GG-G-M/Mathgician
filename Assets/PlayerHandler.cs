using UnityEngine;

public class PlayerHandler : MonoBehaviour
{
    [Header("Player Info")]
    public string playerName = "Player"; // Set to "Player A" or "Player B" in inspector
    public bool isAlive = true;

    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        Debug.Log($"{playerName} has been hit and defeated!");
        
        // Visual feedback - make player semi-transparent
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = rend.material;
            Color color = mat.color;
            color.a = 0.3f;
            mat.color = color;
        }

        // Optional: Disable collider so it can't be hit again
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
    }
    
    public string GetPlayerName()
    {
        return playerName;
    }
}
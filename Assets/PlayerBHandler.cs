using UnityEngine;

public class PlayerBHandler : MonoBehaviour
{
    public bool isAlive = true;

    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        Debug.Log("Player B has been hit! Player A wins!");
        
        // Visual feedback - make Player B semi-transparent
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = rend.material;
            Color color = mat.color;
            color.a = 0.3f;
            mat.color = color;
        }

        // Optional: Disable collider so it can't be hit again
        // Collider col = GetComponent<Collider>();
        // if (col != null)
        //     col.enabled = false;
    }
}
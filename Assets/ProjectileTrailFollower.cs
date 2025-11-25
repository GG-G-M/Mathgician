using UnityEngine;

public class ProjectileTrailFollower : MonoBehaviour
{
    public Transform target;

    private void Update()
    {
        if (target != null)
            transform.position = target.position;
        else
            enabled = false; // stop following after projectile destroyed
    }
}
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public GameObject layerObject;
        [Range(0f, 1f)]
        public float parallaxEffect = 0.5f; // 0 = no movement, 1 = moves with camera
        public float startZ = 0f;
        [HideInInspector]
        public float textureWidth; // Calculated from sprite
        [HideInInspector]
        public Transform[] tiles; // 3-tile ring for seamless wrap
    }
    
    [Header("Parallax Layers")]
    [Tooltip("Assign layers from back to front (layer01, layer02, layer03)")]
    public ParallaxLayer[] layers;
    
    [Header("Camera Reference")]
    public Camera mainCamera;
    
    private Vector3 previousCameraPosition;
    
    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera != null)
        {
            previousCameraPosition = mainCamera.transform.position;
        }
        
        // Initialize layer positions and calculate widths, create tiling ring
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layerObject != null)
            {
                Vector3 pos = layers[i].layerObject.transform.position;
                pos.z = layers[i].startZ;
                layers[i].layerObject.transform.position = pos;
                
                // Calculate texture width for tiling
                SpriteRenderer sr = layers[i].layerObject.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    layers[i].textureWidth = sr.sprite.bounds.size.x * layers[i].layerObject.transform.localScale.x;
                    // Create 3-tile ring if not present
                    layers[i].tiles = new Transform[3];
                    layers[i].tiles[0] = layers[i].layerObject.transform;
                    // Duplicate two clones for seamless wrap
                    for (int t = 1; t < 3; t++)
                    {
                        GameObject clone = Instantiate(layers[i].layerObject, layers[i].layerObject.transform.parent);
                        clone.name = layers[i].layerObject.name + "_tile" + t;
                        layers[i].tiles[t] = clone.transform;
                    }
                    // Position tiles left, center, right (contiguous, no gaps)
                    float w = layers[i].textureWidth;
                    Vector3 center = layers[i].tiles[0].position;
                    layers[i].tiles[0].position = new Vector3(center.x, center.y, layers[i].startZ);
                    layers[i].tiles[1].position = new Vector3(center.x - w, center.y, layers[i].startZ);
                    layers[i].tiles[2].position = new Vector3(center.x + w, center.y, layers[i].startZ);
                }
            }
        }
    }
    
    private void LateUpdate()
    {
        if (mainCamera == null) return;
        
        Vector3 currentCameraPosition = mainCamera.transform.position;
        Vector3 deltaMovement = currentCameraPosition - previousCameraPosition;
        
        // Apply parallax effect to each layer with seamless X tiling
        foreach (ParallaxLayer layer in layers)
        {
            if (layer.layerObject != null)
            {
                // Move layer based on parallax effect multiplier
                Vector3 parallaxMovement = new Vector3(
                    deltaMovement.x * layer.parallaxEffect,
                    deltaMovement.y * layer.parallaxEffect,
                    0f
                );
                
                if (layer.tiles != null && layer.tiles.Length == 3)
                {
                    // Move all tiles together
                    for (int t = 0; t < 3; t++)
                    {
                        layer.tiles[t].position += parallaxMovement;
                    }
                    // Wrap tiles: if a tile is more than half width away, shift by 2*width to opposite side
                    float w = layer.textureWidth;
                    for (int t = 0; t < 3; t++)
                    {
                        Transform tile = layer.tiles[t];
                        float cameraX = currentCameraPosition.x;
                        float dx = cameraX - tile.position.x;
                        if (dx > w * 0.5f)
                        {
                            // Tile is too far left; move right by 2*width to maintain contiguous spacing
                            tile.position += new Vector3(2f * w, 0f, 0f);
                        }
                        else if (dx < -w * 0.5f)
                        {
                            // Tile is too far right; move left by 2*width
                            tile.position += new Vector3(-2f * w, 0f, 0f);
                        }
                    }
                }
                else
                {
                    // Fallback: move single object and simple wrap
                    layer.layerObject.transform.position += parallaxMovement;
                    if (layer.textureWidth > 0f)
                    {
                        float cameraX = currentCameraPosition.x;
                        float dx = cameraX - layer.layerObject.transform.position.x;
                        if (dx > layer.textureWidth)
                        {
                            layer.layerObject.transform.position += new Vector3(2f * layer.textureWidth, 0f, 0f);
                        }
                        else if (dx < -layer.textureWidth)
                        {
                            layer.layerObject.transform.position += new Vector3(-2f * layer.textureWidth, 0f, 0f);
                        }
                    }
                }
            }
        }
        
        previousCameraPosition = currentCameraPosition;
    }
}

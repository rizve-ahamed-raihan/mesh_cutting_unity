using UnityEngine;
using UnityEngine.InputSystem;

public class SliceController : MonoBehaviour
{
    public GameObject cubePrefab;
    
    private Vector2 dragStartPosition;
    private bool isDragging = false;
    private const float minDragDistance = 5f; // Minimum pixels to register as a drag

    void Update()
    {
        if (Camera.main == null)
            return;

        // Handle left mouse button drag for slicing
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Store initial mouse position
                dragStartPosition = Mouse.current.position.ReadValue();
                isDragging = true;
                Debug.Log($"Left button pressed at position: {dragStartPosition}");
            }
            
            if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
            {
                // Get final mouse position
                Vector2 dragEndPosition = Mouse.current.position.ReadValue();
                isDragging = false;
                Debug.Log($"Left button released at position: {dragEndPosition}");
                
                // Check if drag distance is significant
                float dragDistance = Vector2.Distance(dragStartPosition, dragEndPosition);
                if (dragDistance >= minDragDistance)
                {
                    // Perform slice if we hit a cube
                    PerformSlice(dragStartPosition, dragEndPosition);
                }
            }

            // Handle right mouse button for reset
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                Debug.Log("Right button pressed - resetting scene");
                ResetScene();
            }
        }
    }

    void PerformSlice(Vector2 screenPosStart, Vector2 screenPosEnd)
    {
        Debug.Log($"PerformSlice called with start: {screenPosStart}, end: {screenPosEnd}");
        
        // First raycast from end position to find the cube
        Ray rayEnd = Camera.main.ScreenPointToRay(screenPosEnd);
        if (!Physics.Raycast(rayEnd, out RaycastHit hit))
        {
            Debug.LogWarning("No raycast hit at screen pos end");
            return;
        }
        
        Debug.Log($"Hit object: {hit.collider.gameObject.name} at point: {hit.point}");
        
        CubeSlicer slicer = hit.collider.GetComponent<CubeSlicer>();
        if (slicer == null)
        {
            Debug.LogWarning("No CubeSlicer component on hit object");
            return;
        }
        
        // Create a depth plane at the hit point, facing the camera
        Plane depthPlane = new Plane(-Camera.main.transform.forward, hit.point);
        
        // Convert screen start position to world position on the depth plane
        Ray rayStart = Camera.main.ScreenPointToRay(screenPosStart);
        Vector3 worldStart;
        // Allow drag start to be anywhere on screen - if it doesn't hit depth plane, use approximate position
        if (depthPlane.Raycast(rayStart, out float enterStart) && enterStart > 0)
        {
            worldStart = rayStart.origin + rayStart.direction * enterStart;
        }
        else
        {
            // Use a large distance estimate based on hit point
            float distToHit = Vector3.Distance(rayStart.origin, hit.point);
            worldStart = rayStart.origin + rayStart.direction * distToHit;
        }
        
        // Convert screen end position to world position on the depth plane
        Vector3 worldEnd;
        if (!depthPlane.Raycast(rayEnd, out float enterEnd))
        {
            Debug.LogWarning("Depth plane raycast failed for end position");
            return;
        }
        worldEnd = rayEnd.origin + rayEnd.direction * enterEnd;
        
        Debug.Log($"World start: {worldStart}, World end: {worldEnd}");
        
        // Compute world-space drag vector
        Vector3 dragWorld = worldEnd - worldStart;
        
        Debug.Log($"Drag world: {dragWorld}, Magnitude: {dragWorld.magnitude}");
        
        // Ignore very small drags
        if (dragWorld.magnitude < minDragDistance * 0.01f)
        {
            Debug.LogWarning($"Drag too small: {dragWorld.magnitude} < {minDragDistance * 0.01f}");
            return;
        }
        
        // Compute slice normal: perpendicular to drag direction and camera forward
        // This creates the Metal Gear Rising effect:
        // - Horizontal drag → vertical slice
        // - Vertical drag → horizontal slice
        Vector3 sliceNormal = Vector3.Cross(dragWorld.normalized, Camera.main.transform.forward).normalized;
        
        Debug.Log($"Slice normal: {sliceNormal}, Drag magnitude: {dragWorld.magnitude}");
        
        // Slice at the hit point with computed normal
        slicer.Slice(hit.point, sliceNormal);
        Debug.Log("Slice called successfully!");
    }

    void ResetScene()
    {
        // Always spawn at center position
        Vector3 cubePosition = new Vector3(0, 1, -7);
        Quaternion cubeRotation = Quaternion.identity;
        float originalForce = 3f;
        
        CubeSlicer[] cubes = FindObjectsByType<CubeSlicer>(FindObjectsSortMode.None);
        if (cubes.Length > 0)
        {
            originalForce = cubes[0].force;
        }

        // Destroy all slice pieces
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && obj != this.gameObject && obj.name.Contains("Slice"))
            {
                Destroy(obj);
            }
        }

        // Destroy any remaining cubes
        foreach (CubeSlicer cube in cubes)
        {
            if (cube != null && cube.gameObject != this.gameObject)
            {
                Destroy(cube.gameObject);
            }
        }

        // Spawn new cube at the center position
        if (cubePrefab != null)
        {
            GameObject newCube = Instantiate(cubePrefab, cubePosition, cubeRotation);
            newCube.transform.localScale = Vector3.one;
            
            CubeSlicer slicer = newCube.GetComponent<CubeSlicer>();
            if (slicer == null)
            {
                slicer = newCube.AddComponent<CubeSlicer>();
            }
            
            slicer.force = originalForce;
            
            if (newCube.GetComponent<Collider>() == null)
            {
                newCube.AddComponent<BoxCollider>();
            }
        }
    }
}
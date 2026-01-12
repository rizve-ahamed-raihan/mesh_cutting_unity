using UnityEngine;
using UnityEngine.InputSystem;

public class SliceController : MonoBehaviour
{
    public GameObject cubePrefab;
    private int frameCount = 0;

    void Update()
    {
        frameCount++;
        
        // Always log mouse state to verify Update is running
        if (frameCount % 120 == 0)
        {
            Debug.Log($"[Frame {frameCount}] Update IS RUNNING. Mouse.current = {(Mouse.current != null ? "NOT NULL" : "NULL")}");
        }
        
        // Click on a cube to slice it
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log($"[Frame {frameCount}] Left mouse button pressed");
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");
                CubeSlicer slicer = hit.collider.GetComponent<CubeSlicer>();
                if (slicer != null)
                {
                    Debug.Log("CubeSlicer found, slicing...");
                    // Slice along a random plane through the hit point
                    Vector3 sliceNormal = Random.onUnitSphere;
                    slicer.Slice(hit.point, sliceNormal);
                }
                else
                {
                    Debug.Log("No CubeSlicer component found on hit object");
                }
            }
            else
            {
                Debug.Log("Raycast didn't hit anything");
            }
        }

        // Press right mouse button to reset - destroy all slices and spawn new cube
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            Debug.Log("========== RIGHT BUTTON PRESSED ==========");
            try
            {
                Debug.Log("Starting reset sequence...");
                // Find the original cube position before destroying
                Vector3 cubePosition = new Vector3(0, 1, -7);
                Quaternion cubeRotation = Quaternion.identity;
                Material originalMaterial = null;
                float originalForce = 3f;
                
                Debug.Log("Finding existing cubes...");
                CubeSlicer[] cubes = FindObjectsByType<CubeSlicer>(FindObjectsSortMode.None);
                Debug.Log($"Found {cubes.Length} cubes to destroy");
                if (cubes.Length > 0)
                {
                    cubePosition = new Vector3(cubes[0].transform.position.x, 1, -7);
                    cubeRotation = cubes[0].transform.rotation;
                    // Save original properties
                    if (cubes[0].GetComponent<MeshRenderer>() != null)
                    {
                        originalMaterial = cubes[0].GetComponent<MeshRenderer>().material;
                    }
                    originalForce = cubes[0].force;
                    Debug.Log($"Saved position: {cubePosition}, rotation: {cubeRotation}, force: {originalForce}");
                }

                Debug.Log("Finding slice pieces to destroy...");
                // Destroy all slice pieces
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                Debug.Log($"Total GameObjects found: {allObjects.Length}");
                int sliceCount = 0;
                foreach (GameObject obj in allObjects)
                {
                    // CRITICAL: Don't destroy the GameObject with this script!
                    if (obj != null && obj != this.gameObject && obj.name.Contains("Slice"))
                    {
                        Debug.Log($"Destroying slice: {obj.name}");
                        Destroy(obj);
                        sliceCount++;
                    }
                }
                Debug.Log($"Destroyed {sliceCount} slice pieces");

                Debug.Log("Destroying remaining cubes...");
                // Destroy any remaining cubes
                foreach (CubeSlicer cube in cubes)
                {
                    // CRITICAL: Don't destroy the GameObject with this script!
                    if (cube != null && cube.gameObject != this.gameObject)
                    {
                        Debug.Log($"Destroying cube: {cube.gameObject.name}");
                        Destroy(cube.gameObject);
                    }
                }
                Debug.Log("All old objects destroyed");

                Debug.Log("Spawning new cube...");
                // Spawn new cube at the original position
                if (cubePrefab != null)
                {
                    GameObject newCube = Instantiate(cubePrefab, cubePosition, cubeRotation);
                    Debug.Log($"Spawned new cube at position: {cubePosition}");
                    
                    // Verify the cube has all necessary components
                    CubeSlicer slicer = newCube.GetComponent<CubeSlicer>();
                    if (slicer == null)
                    {
                        Debug.LogWarning("Spawned cube doesn't have CubeSlicer component! Adding it now.");
                        slicer = newCube.AddComponent<CubeSlicer>();
                    }
                    
                    // Set the force property
                    slicer.force = originalForce;
                    
                    // Ensure it has a collider for raycasting
                    if (newCube.GetComponent<Collider>() == null)
                    {
                        Debug.LogWarning("Spawned cube doesn't have a Collider! Adding BoxCollider.");
                        newCube.AddComponent<BoxCollider>();
                    }
                    
                    // Verify MeshFilter and MeshRenderer exist
                    if (newCube.GetComponent<MeshFilter>() == null)
                    {
                        Debug.LogError("Spawned cube is missing MeshFilter! Slicing will not work.");
                    }
                    if (newCube.GetComponent<MeshRenderer>() == null)
                    {
                        Debug.LogError("Spawned cube is missing MeshRenderer! Slicing will not work.");
                    }
                    
                    Debug.Log($"New cube components: CubeSlicer={newCube.GetComponent<CubeSlicer>() != null}, " +
                             $"Collider={newCube.GetComponent<Collider>() != null}, " +
                             $"MeshFilter={newCube.GetComponent<MeshFilter>() != null}, " +
                             $"MeshRenderer={newCube.GetComponent<MeshRenderer>() != null}");
                    Debug.Log("========== RESET COMPLETE ==========");
                }
                else
                {
                    Debug.LogWarning("Cube prefab is not assigned!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception during reset: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}

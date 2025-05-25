using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class WallGenerator : MonoBehaviour
{
    [Header("Assign the Cube here, set up x and y for thin!")]
    public GameObject linePrefab;
    [Header("Parent for organizing spawned lines")]
    public Transform linesParent;

    /// Call this after paper is detected and lines need to be visualized.
    public void VisualizeLines(
        Texture2D tex, 
        Matrix4x4 displayMatrix, 
        ARRaycastManager raycastManager,
        Vector2[] paperQuad = null)
    {
        if (tex == null)
        {
            Debug.LogError("Texture2D is null!");
            return;
        }

        var detectedLines = DetectLines(tex);

        CleanupOldLines();

        foreach (var line in detectedLines)
        {

            // 1. Pixel to viewport (normalized)
            Vector2 p1_viewport = Converter.FromRawCpuToViewport(displayMatrix, line.Item1, new Vector2(tex.width, tex.height));
            Vector2 p2_viewport = Converter.FromRawCpuToViewport(displayMatrix, line.Item2, new Vector2(tex.width, tex.height));

            // 2. Viewport to AR world
            Vector3 p1_world = ViewportToARWorld(p1_viewport, raycastManager);
            Vector3 p2_world = ViewportToARWorld(p2_viewport, raycastManager);

            Debug.Log($"Line viewport: {p1_viewport} -> {p2_viewport}, world: {p1_world} -> {p2_world}");

            // 3. Only instantiate if both endpoints could be placed
            if (p1_world == Vector3.zero || p2_world == Vector3.zero)
            {
                Debug.LogWarning($"MISSING: Line [{p1_viewport}] -> [{p2_viewport}], world [{p1_world}] -> [{p2_world}] could not be placed! ARPlane may be too small.");
                continue;
            }

            InstantiateLine(p1_world, p2_world);
        }
    }

    private List<(Vector2, Vector2)> DetectLines(Texture2D tex)
    {
        Vector2[][] detectedLinesArr;

        const int MaxLines = 256;

        int numLines = PaperPlugin.FindBlackLines(
            tex.GetRawTextureData(), tex.width, tex.height, out detectedLinesArr, MaxLines);

        var results = new List<(Vector2, Vector2)>(numLines);
        for (int i = 0; i < numLines; ++i)
            results.Add((detectedLinesArr[i][0], detectedLinesArr[i][1]));

        if (numLines == 0)
            Debug.LogWarning("No lines detected.");

        return results;
    }

    private void CleanupOldLines()
    {
        if (linesParent == null) return;
        foreach (Transform child in linesParent)
            Destroy(child.gameObject);
    }

    /// Instantiates one line prefab between two points (in world space).
    private void InstantiateLine(Vector3 p1, Vector3 p2)
    {
        Vector3 midPoint = (p1 + p2) * 0.5f;
        Vector3 dir = (p2 - p1).normalized;
        float len = Vector3.Distance(p1, p2);

        GameObject go = Instantiate(linePrefab, linesParent);
        go.transform.position = midPoint;

        // You may want to align on Y or the plane normal. Here, we align with the world up.
        go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        Vector3 scale = go.transform.localScale;
        scale.z = len;
        go.transform.localScale = scale;
    }

    /// Converts a viewport point to AR world position using a raycast.
    /// Returns Vector3.zero if no plane is hit.
    private static Vector3 ViewportToARWorld(Vector2 viewport, ARRaycastManager raycastManager)
    {
        var ray = Camera.main.ViewportPointToRay(viewport);
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon) && hits.Count > 0)
        {
            return hits[0].pose.position;
        }
        // Fallback: returns Vector3.zero, so you can handle it outside
        return Vector3.zero;
    }
}

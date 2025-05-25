using System.Collections.Generic;
using UnityEngine;

public class WallGenerator : MonoBehaviour
{
    [Header("Assign your Cube here")]
    public GameObject linePrefab;
    [Header("Parent for organizing spawned lines")]
    public Transform linesParent;

    /// Call this with your paper-cropped Texture2D (RGBA32)
    public void VisualizeLines(Texture2D tex)
    {
        if (tex == null)
        {
            Debug.LogError("Texture2D is null!");
            return;
        }

       
        var detectedLines = DetectLines(tex);

        
        CleanupOldLines();

       
        foreach (var line in detectedLines)
            InstantiateLine(line.Item1, line.Item2);
    }

    /// Runs native detection and parses results.
    private List<(Vector2, Vector2)> DetectLines(Texture2D tex)
    {
        Vector2[][] detectedLinesArr;
        int numLines = PaperPlugin.FindBlackLines(
            tex.GetRawTextureData(), tex.width, tex.height, out detectedLinesArr, 32);

        var results = new List<(Vector2, Vector2)>(numLines);
        for (int i = 0; i < numLines; ++i)
            results.Add((detectedLinesArr[i][0], detectedLinesArr[i][1]));

        if (numLines == 0)
            Debug.Log("No lines detected.");

        return results;
    }

    /// Removes previous line GameObjects under the parent.
    private void CleanupOldLines()
    {
        if (linesParent == null) return;
        foreach (Transform child in linesParent)
            Destroy(child.gameObject);
    }

    /// Instantiates one line prefab between two points (in pixel space for now).
    private void InstantiateLine(Vector2 p1, Vector2 p2)
    {
        Vector3 worldP1 = PixelToWorld(p1);
        Vector3 worldP2 = PixelToWorld(p2);
        Vector3 midPoint = (worldP1 + worldP2) * 0.5f;
        Vector3 dir = (worldP2 - worldP1).normalized;
        float len = Vector3.Distance(worldP1, worldP2);

        GameObject go = Instantiate(linePrefab, linesParent);
        go.transform.position = midPoint;
        go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        Vector3 scale = go.transform.localScale;
        scale.z = len;
        go.transform.localScale = scale;
    }

    /// Converts 2D pixel (texture) coordinates to 3D world space.
    /// For now, just map x=>x, y=>z, y=0. Adjust as needed for AR.
    private Vector3 PixelToWorld(Vector2 p)
    {
        return new Vector3(p.x, 0, p.y);
    }
}

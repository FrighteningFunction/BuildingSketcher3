using UnityEngine;

public class PaperDetectorTest : MonoBehaviour
{
    // Any RGBA32 image with a white rectangle on a dark bg works
    [SerializeField] Texture2D testImage;

    void Start()
    {
        if (testImage == null)
        {
            Debug.LogError("Assign a test RGBA texture in the inspector");
            return;
        }

        // Convert texture to raw RGBA32 bytes
        var rgba = testImage.GetPixels32();           // Color32[]
        byte[] bytes = new byte[rgba.Length * 4];
        for (int i = 0; i < rgba.Length; ++i)
        {
            Color32 c = rgba[i];
            int o = i * 4;
            bytes[o] = c.r;
            bytes[o + 1] = c.g;
            bytes[o + 2] = c.b;
            bytes[o + 3] = c.a;
        }

        // Call native plugin
        if (PaperPlugin.FindPaperCorners(
                bytes, testImage.width, testImage.height, out Vector2[] corners))
        {
            Debug.Log($"Paper found! Corners (px): " +
                      $"{string.Join(", ", corners)}");
        }
        else
        {
            Debug.LogWarning("Paper NOT found.");
        }
    }
}


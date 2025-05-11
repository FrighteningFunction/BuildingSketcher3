using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// High-level controller that captures the camera frame, runs the native paper-corner
/// detector, and spawns / moves four cubes at the detected corner positions.
/// </summary>
public class PaperDetector : MonoBehaviour
{
    /* ---------- 1. native-plugin import ---------- */
    [DllImport("PaperPlugin1")]
    private static extern bool FindPaperCorners(
        byte[] imageData,
        int width,
        int height,
        float[] outCorners);

    /* ---------- 2. scene references ---------- */
    [SerializeField] ARCameraManager cameraManager;
    [SerializeField] GameObject cornerPrefab;
    [SerializeField] Transform cornersParent;

    /* ---------- 3. runtime fields ---------- */
    Texture2D cameraTexture;
    GameObject[] cornerCubes = new GameObject[4];

    /* ---------- Unity lifecycle hooks ---------- */
    void Awake() => InitializeCornerCubes();
    void OnEnable() => cameraManager.frameReceived += OnCameraFrame;
    void OnDisable() => cameraManager.frameReceived -= OnCameraFrame;

    /* ============================================================= */
    /* =========== Main per-frame callback, now bite-sized ========== */
    /* ============================================================= */

    /// <summary>
    /// Master callback invoked every time ARFoundation provides a new camera frame.
    /// </summary>
    void OnCameraFrame(ARCameraFrameEventArgs args)
    {
        // 1) Acquire the CPU image
        if (!TryGetCpuImage(out var cpuImage)) return;

        // 2) Convert it to a Texture2D in RGBA32 format
        ConvertCpuImageToTexture(cpuImage);

        // 3) Extract managed byte[] for the plugin call
        byte[] rgbaBytes = GetTextureBytes(cameraTexture);

        // 4) Ask the native plugin for the paper corners
        float[] corners = new float[8];
        bool found = FindPaperCorners(
            rgbaBytes, cameraTexture.width, cameraTexture.height, corners);

        // 5) If detected, move / show the corner cubes
        if (found) UpdateCornerCubes(corners);
    }

    /* ============================================================= */
    /* ======================  Helper methods  ===================== */
    /* ============================================================= */

    // ───────────────────────────────────────────────────────────────
    // Initializes four cubes (one for each corner) when the scene loads.
    // ───────────────────────────────────────────────────────────────
    void InitializeCornerCubes()
    {
        for (int i = 0; i < 4; i++)
        {
            cornerCubes[i] = Instantiate(cornerPrefab, cornersParent);
            cornerCubes[i].name = $"Corner_{i}";
            cornerCubes[i].SetActive(false);         // hidden until first detection
        }
    }

    // ───────────────────────────────────────────────────────────────
    // Attempts to get the most recent XRCpuImage. Returns true if successful.
    // Automatically disposes the image if conversion fails later on.
    // ───────────────────────────────────────────────────────────────
    bool TryGetCpuImage(out XRCpuImage image) =>
        cameraManager.TryAcquireLatestCpuImage(out image);

    // ───────────────────────────────────────────────────────────────
    // Converts the acquired CPU image into an RGBA32 Texture2D,
    // re-using an existing Texture2D if the resolution hasn’t changed.
    // ───────────────────────────────────────────────────────────────
    void ConvertCpuImageToTexture(XRCpuImage image)
    {
        var conv = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };

        // Ensure we have a Texture2D of correct size/format
        if (cameraTexture == null ||
            cameraTexture.width != image.width ||
            cameraTexture.height != image.height)
        {
            cameraTexture = new Texture2D(image.width, image.height,
                                          TextureFormat.RGBA32, false);
        }

        // Allocate a temporary buffer and perform the conversion
        int byteCount = image.GetConvertedDataSize(conv);
        using (var buffer = new NativeArray<byte>(byteCount, Allocator.Temp))
        {
            image.Convert(conv, buffer);
            cameraTexture.LoadRawTextureData(buffer);
            cameraTexture.Apply();
        }

        image.Dispose();   // always dispose the XRCpuImage
    }

    // ───────────────────────────────────────────────────────────────
    // Returns the Texture2D pixel data as a managed byte[] so it can
    // be passed to the unmanaged plugin (P/Invoke requires managed memory).
    // ───────────────────────────────────────────────────────────────
    static byte[] GetTextureBytes(Texture2D tex) =>
        tex.GetRawTextureData<byte>().ToArray();

    // ───────────────────────────────────────────────────────────────
    // Converts the 4 screen-space corner coordinates returned by the plugin
    // into 3D world positions and places the cubes there.
    // ───────────────────────────────────────────────────────────────
    void UpdateCornerCubes(float[] corners)
    {
        for (int i = 0; i < 4; i++)
        {
            Vector2 screen = new(
                corners[i * 2 + 0],
                corners[i * 2 + 1]);

            // Ray-cast 0.5 m forward from the camera through that pixel
            var ray = Camera.main.ScreenPointToRay(screen);
            Vector3 pos = ray.GetPoint(0.5f);

            cornerCubes[i].transform.position = pos;
            cornerCubes[i].SetActive(true);
        }
    }
}

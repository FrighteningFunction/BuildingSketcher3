// PaperDetector.cs
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using static UnityEngine.Rendering.GPUSort;
using System.IO;

[RequireComponent(typeof(ARCameraManager))]
public class PaperDetector : MonoBehaviour
{
    /* ---------- Inspector refs ---------- */
    [SerializeField] ARCameraManager cameraManager;
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] ARPlaneManager arPlaneManager;
    [SerializeField] RawImage debugImage;
    [SerializeField] PipelineDebugger pipelineDebugger;
    [SerializeField] PaperEdgeLines paperEdgeLines;
    [SerializeField] WallGenerator wallGenerator;

    [SerializeField] bool debugMode = false;

    /* ---------- internals ---------- */
    private Texture2D camTex;
    private Vector2[] imgCorners;
    private Matrix4x4 D;


    void OnGUI()
    {
        if (debugMode)
        {
            paperEdgeLines.DrawWhiteDots(D,
                debugImage != null,
                imgCorners,
                camTex != null ? new Vector2(camTex.width, camTex.height) : Vector2.zero);
            Debug.Log("Drawing white dots on texture.");
        }
    }

    void OnEnable()
    {
        cameraManager.frameReceived += OnFrame;

        debugImage.enabled = debugMode;

        if (debugMode)
        {
            Debug.Log("DebugMode turned on.");
        }
    }
    void OnDisable() => cameraManager.frameReceived -= OnFrame;

    void OnFrame(ARCameraFrameEventArgs args)
    {

        Debug.Log("--------------------------------NEW FRAME---------------------------------\n" +
                  "--------------------------------------------------------------------------");

        if (!cameraManager.TryAcquireLatestCpuImage(out var cpu))
            return;

        UpdateTexture(cpu);

        if (!TryDetectPaperCorners()) return;

        FetchDisplayMatrix(args);


        Vector2[] viewportCorners = ConvertImageCornersToViewport();


        paperEdgeLines.PlaceLinesFromViewport(viewportCorners);

        if (imgCorners != null && imgCorners.Length == 4)
        {
            wallGenerator.VisualizeLines(camTex, D, raycastManager, imgCorners);
        }

        ExecuteDebug(viewportCorners);
    }

    private bool TryDetectPaperCorners()
    {
        byte[] rgba = camTex.GetRawTextureData<byte>().ToArray();
        if (!PaperPlugin.FindPaperCorners(rgba, camTex.width, camTex.height, out imgCorners))
        {
            paperEdgeLines.DisableLines();

            Debug.LogWarning(" No paper corners detected.");
            return false;
        }

        return true;
    }


    private Vector2[] ConvertImageCornersToViewport()
    {
        // 2) convert each raw image‐space corner → normalized UV → into the same UV space
        Vector2[] viewportCorners = new Vector2[imgCorners.Length];
        for (int i = 0; i < imgCorners.Length; i++)
        {

            viewportCorners[i] = Converter.FromRawCpuToViewport(D, imgCorners[i], new Vector2(camTex.width, camTex.height));
        }

        return viewportCorners;
    }

    private void FetchDisplayMatrix(ARCameraFrameEventArgs args)
    {
        if (!args.displayMatrix.HasValue)
        {
            Debug.LogError(" No display matrix available.");
            return;
        }
        else
        {
            D = args.displayMatrix.Value;
        }
    }

    private void ExecuteDebug(Vector2[] viewportCorners)
    {
        if (!debugMode) return;

        pipelineDebugger.printPaperCornerViewportCoords(viewportCorners);

        pipelineDebugger.printConverterCornersDebug(D, new Vector2(camTex.width, camTex.height), Converter.FromRawCpuToViewport);

        pipelineDebugger.logTransormedCorners(D, new Vector2(camTex.width, camTex.height), Converter.FromRawCpuToViewport);

        pipelineDebugger.printDisplayMatrix(D);

       MarkCpuCornersOnTexture(new Color32(255, 0, 0, 255));
    }

    private void UpdateTexture(XRCpuImage img)
    {
        var p = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, img.width, img.height),
            outputDimensions = new Vector2Int(img.width, img.height),
            outputFormat = TextureFormat.RGBA32
        };

        if (camTex == null || camTex.width != img.width || camTex.height != img.height)
        {
            camTex = new Texture2D(img.width, img.height, TextureFormat.RGBA32, false);
            Debug.Log($"Created camTex: {img.width}x{img.height}");
        }

        using var buf = new NativeArray<byte>(img.GetConvertedDataSize(p), Allocator.Temp);
        img.Convert(p, buf);
        camTex.LoadRawTextureData(buf);
        camTex.Apply();

        if(debugMode) debugImage.texture = camTex;

        img.Dispose();
    }


    private void MarkCpuCornersOnTexture(Color32 dotColor, int dotSize = 7)
    {
        if (imgCorners == null || camTex == null) return;

        int w = camTex.width;
        int h = camTex.height;
        int half = dotSize / 2;

        // pull pixel array once
        var pixels = camTex.GetPixels32();

        foreach (Vector2 c in imgCorners)
        {
            int cx = Mathf.RoundToInt(c.x);
            int cy = Mathf.RoundToInt(c.y);

            // Texture2D origin is bottom-left, CPU origin is top-left → flip Y:
            // For some reason, no. Maybe because opencv measured it from Texture2D?
            //cy = h - 1 - cy;

            for (int dy = -half; dy <= half; ++dy)
                for (int dx = -half; dx <= half; ++dx)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (x < 0 || x >= w || y < 0 || y >= h) continue;
                    pixels[y * w + x] = dotColor;
                }
        }

        camTex.SetPixels32(pixels);
        camTex.Apply(false);         // no mip update, fast
    }
}

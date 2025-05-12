// PaperDetector.cs
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

[RequireComponent(typeof(ARCameraManager))]
public class PaperDetector : MonoBehaviour
{
    /* ---------- Inspector refs ---------- */
    [SerializeField] ARCameraManager cameraManager;
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] ARPlaneManager arPlaneManager;
    [SerializeField] RawImage debugImage;

    [Header("Line Settings")]
    [SerializeField] Material lineMaterial;
    [SerializeField] float lineWidth = 0.005f;

    /* ---------- internals ---------- */
    Texture2D camTex;
    lineSegment[] segments = new lineSegment[4];
    static readonly List<ARRaycastHit> hits = new();
    Vector3[] prevPos = new Vector3[4];

    struct lineSegment
    {
        public LineRenderer lr;
        public bool end0Fallback;
        public bool end1Fallback;
    }

    void Awake()
    {
        InitLines();
        arPlaneManager.planePrefab.active = false;
        Debug.Log("PaperDetector initialized.");
    }

    void OnEnable() => cameraManager.frameReceived += OnFrame;
    void OnDisable() => cameraManager.frameReceived -= OnFrame;

    void OnFrame(ARCameraFrameEventArgs _)
    {
        if (!cameraManager.TryAcquireLatestCpuImage(out var cpu))
            return;

        UpdateTexture(cpu);

        byte[] rgba = camTex.GetRawTextureData<byte>().ToArray();
        if (!PaperPlugin.FindPaperCorners(rgba, camTex.width, camTex.height, out Vector2[] imgCorners))
        {
            foreach (var seg in segments)
                seg.lr.enabled = false;

            Debug.LogWarning(" No paper corners detected.");
            return;
        }

        Debug.Log($"Detected {imgCorners.Length} corners.");
        for (int i = 0; i < imgCorners.Length; i++)
            Debug.Log($"    Corner[{i}] = {imgCorners[i]}");

        Vector2[] ordered = OrderCorners(imgCorners);

        Vector2[] viewportCorners = new Vector2[ordered.Length];
        for (int i = 0; i < ordered.Length; i++)
        {
            viewportCorners[i] = ImagePointToViewport(ordered[i], camTex.width, camTex.height);
            Debug.Log($"    Ordered → Viewport[{i}] = {viewportCorners[i]:F3}");
        }

        PlaceLinesFromViewport(viewportCorners);
    }

    Vector2[] OrderCorners(Vector2[] c)
    {
        Vector2 center = c.Aggregate(Vector2.zero, (a, b) => a + b) / 4f;
        return c.OrderBy(p => Mathf.Atan2(p.y - center.y, p.x - center.x)).ToArray();
    }

    Vector2 ImagePointToViewport(Vector2 imgPt, int texWidth, int texHeight)
    {
        float x = imgPt.x / texWidth;
        float y = 1f - (imgPt.y / texHeight);

        float dx = x - 0.5f;
        float dy = y - 0.5f;

        switch (Screen.orientation)
        {
            case ScreenOrientation.Portrait:
                x = 0.5f - dy;
                y = 0.5f + dx;
                break;
            case ScreenOrientation.PortraitUpsideDown:
                x = 0.5f + dy;
                y = 0.5f - dx;
                break;
            case ScreenOrientation.LandscapeRight:
                x = 0.5f + dy;
                y = 0.5f + dx;
                break;
        }

        return new Vector2(x, y);
    }

    void InitLines()
    {
        for (int i = 0; i < 4; ++i)
        {
            var go = new GameObject($"PaperEdge_{i}");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = lineWidth;
            segments[i].lr = lr;
        }

        Debug.Log("LineRenderers initialized.");
    }

    void UpdateTexture(XRCpuImage img)
    {
        var p = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, img.width, img.height),
            outputDimensions = new Vector2Int(img.width, img.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY | XRCpuImage.Transformation.MirrorX
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

        debugImage.texture = camTex;
        img.Dispose();
    }

    void PlaceLinesFromViewport(Vector2[] vpCorners)
    {
        Vector3[] worldPos = new Vector3[vpCorners.Length];
        bool[] usedFallback = new bool[vpCorners.Length];

        for (int i = 0; i < vpCorners.Length; i++)
        {
            Vector2 vp = vpCorners[i];
            var ray = Camera.main.ViewportPointToRay(vp);
            Vector3 hitPt;
            bool hitPlane = false;

            if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
            {
                var chosen = hits[0];
                var plane = arPlaneManager.GetPlane(chosen.trackableId);
                hitPt = chosen.pose.position + plane.transform.up * 0.1f;
                hitPlane = true;
            }
            else
            {
                hitPt = ray.GetPoint(0.5f);
            }

            worldPos[i] = Vector3.Lerp(prevPos[i], hitPt, 0.25f);
            prevPos[i] = worldPos[i];
            usedFallback[i] = !hitPlane;

            Debug.Log($" Corner[{i}] → World: {worldPos[i]:F2} {(hitPlane ? "YES" : "NO")}");
        }

        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            var seg = segments[i];
            seg.lr.enabled = true;
            seg.lr.SetPosition(0, worldPos[i]);
            seg.lr.SetPosition(1, worldPos[j]);

            bool anyFallback = usedFallback[i] || usedFallback[j];
            Color col = anyFallback ? Color.yellow : Color.blue;
            seg.lr.startColor = seg.lr.endColor = col;

            Debug.Log($"Segment[{i}] From {worldPos[i]:F2} → {worldPos[j]:F2} Color: {(anyFallback ? "YELLOW" : "BLUE")}");
        }
    }
}

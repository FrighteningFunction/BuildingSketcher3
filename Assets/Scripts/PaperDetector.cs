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

    struct lineSegment
    {
        public LineRenderer lr;
        public bool end0Fallback;
        public bool end1Fallback;
    }

    void Awake() { 
        InitLines();

        arPlaneManager.planePrefab.active = false;
    }
    void OnEnable() => cameraManager.frameReceived += OnFrame;
    void OnDisable() => cameraManager.frameReceived -= OnFrame;

    /* ================================================================= */
    /*                               FRAME                               */
    /* ================================================================= */
    void OnFrame(ARCameraFrameEventArgs _)
    {
        // 1) Acquire camera image
        if (!cameraManager.TryAcquireLatestCpuImage(out var cpu))
            return;

        UpdateTexture(cpu);

        // 2) Find paper corners in image space
        byte[] rgba = camTex.GetRawTextureData<byte>().ToArray();
        if (!PaperPlugin.FindPaperCorners(
                rgba, camTex.width, camTex.height,
                out Vector2[] imgCorners))
        {
            // nothing found: hide lines
            foreach (var seg in segments)
                seg.lr.enabled = false;
            return;
        }

        // 3) Order the corners (CW)
        Vector2[] ordered = OrderCorners(imgCorners);

        //convert each to viewport coordinates 
        Vector2[] viewportCorners = new Vector2[ordered.Length];
        for (int i = 0; i < ordered.Length; i++)
        {
            viewportCorners[i] = ImagePointToViewport(
                ordered[i],
                camTex.width,
                camTex.height
            );
        }

        // 4) Project + raycast + draw
        PlaceLinesFromViewport(viewportCorners);
    }

    /* Quick heuristic ordering: Sort by angle around center */
    Vector2[] OrderCorners(Vector2[] c)
    {
        Vector2 center = c.Aggregate(Vector2.zero, (a, b) => a + b) / 4f;
        return c.OrderBy(p => Mathf.Atan2(p.y - center.y, p.x - center.x)).ToArray();
    }

    /* ================================================================= */
    /*                            HELPERS                                */
    /* ================================================================= */

    /// <summary>
    /// Converts a pixel-space point (as returned by your OpenCV plugin on camTex)
    /// into normalized Unity viewport coordinates, handling MirrorY and orientation.
    /// </summary>
    Vector2 ImagePointToViewport(Vector2 imgPt, int texWidth, int texHeight)
    {
        // 1) Normalize X to [0,1], invert Y to account for MirrorY
        float x = imgPt.x / texWidth;
        float y = 1f - (imgPt.y / texHeight);

        // 2) Rotate around screen center (0.5,0.5) based on orientation
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
                // LandscapeLeft: no change
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
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.enabled = false;

            lr.material.renderQueue = 3001;

            segments[i] = new lineSegment { lr = lr };
        }
    }

    void UpdateTexture(XRCpuImage img)
    {
        var p = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, img.width, img.height),
            outputDimensions = new Vector2Int(img.width, img.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };

        if (camTex == null || camTex.width != img.width || camTex.height != img.height)
            camTex = new Texture2D(img.width, img.height, TextureFormat.RGBA32, false);

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
        TrackableId? masterPlane = null;

        for (int i = 0; i < vpCorners.Length; i++)
        {
            // directly use viewport coords to cast ray
            var ray = Camera.main.ViewportPointToRay(new Vector3(vpCorners[i].x, vpCorners[i].y, 0));
            Vector3 fallback = ray.GetPoint(0.5f);
            bool hitPlane = false;
            Vector3 finalPos = fallback;

            if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
            {
                // pick your hit (same plane logic...)
                var chosen = hits[0];
                if (!masterPlane.HasValue) masterPlane = chosen.trackableId;
                if (chosen.trackableId == masterPlane)
                {
                    var arPlane = arPlaneManager.GetPlane(chosen.trackableId);
                    finalPos = chosen.pose.position + arPlane.transform.up * 0.1f;
                    hitPlane = true;
                }
            }

            worldPos[i] = finalPos;
            usedFallback[i] = !hitPlane;
        }

        // draw exactly as before, looping i → i+1...
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            var seg = segments[i];
            seg.lr.enabled = true;
            seg.lr.SetPosition(0, worldPos[i]);
            seg.lr.SetPosition(1, worldPos[j]);
            bool anyFallback = usedFallback[i] || usedFallback[j];
            seg.lr.startColor = seg.lr.endColor = anyFallback ? Color.yellow : Color.blue;
        }
    }
}

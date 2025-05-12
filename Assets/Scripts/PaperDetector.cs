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

    void Awake() => InitLines();
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

        // 4) Project + raycast + draw
        PlaceLines(ordered);
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

    void PlaceLines(Vector2[] imgCorners)
    {
        // Prepare arrays
        Vector3[] worldPos = new Vector3[4];
        bool[] usedFallback = new bool[4];
        TrackableId? masterPlane = null;

        // Raycast each corner
        for (int i = 0; i < 4; i++)
        {
            var uv = new Vector2(imgCorners[i].x / camTex.width,
                                 imgCorners[i].y / camTex.height);
            var ray = Camera.main.ViewportPointToRay(uv);
            Vector3 fallbackPos = ray.GetPoint(0.5f);
            bool hitPlane = false;
            Vector3 finalPos = fallbackPos;

            if (raycastManager.Raycast(ray, hits,
                TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
            {
                // prefer same plane after the first hit
                ARRaycastHit chosen;
                var planeHit = hits.FirstOrDefault(h => masterPlane.HasValue && h.trackableId == masterPlane);
                if (!planeHit.Equals(default(ARRaycastHit)))
                    chosen = planeHit;
                else
                    chosen = hits[0];
                if (!masterPlane.HasValue)
                    masterPlane = chosen.trackableId;

                if (chosen.trackableId == masterPlane)
                {
                    var arPlane = arPlaneManager.GetPlane(chosen.trackableId);
                    Vector3 normal = arPlane.transform.up;            // plane’s normal
                    finalPos = chosen.pose.position + normal * 0.1f;  
                    hitPlane = true;
                }
            }

            worldPos[i] = finalPos;
            usedFallback[i] = !hitPlane;
        }

        // Draw 4 border segments
        for (int i = 0; i < 4; i++)
        {
            var seg = segments[i];
            // determine next index (wrap around)
            int j = (i + 1) % 4;
            seg.lr.enabled = true;
            seg.lr.SetPosition(0, worldPos[i]);
            seg.lr.SetPosition(1, worldPos[j]);

            // color = blue if both ends hit plane; else yellow
            bool anyFallback = usedFallback[i] || usedFallback[j];
            Color c = anyFallback ? Color.yellow : Color.blue;
            seg.lr.startColor = seg.lr.endColor = c;
        }
    }
}

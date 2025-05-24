using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

[RequireComponent(typeof(ARCameraManager))]
public class PaperEdgeLines : MonoBehaviour
{


    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] ARPlaneManager arPlaneManager;
    [SerializeField] bool debugMode = true;



    [Header("Line Settings")]
    [SerializeField] Material lineMaterial;
    [SerializeField] float lineWidth = 0.005f;


    private lineSegment[] segments = new lineSegment[4];
    private static readonly List<ARRaycastHit> hits = new();



    private Texture2D whiteDot;



    struct lineSegment
    {
        public LineRenderer lr;
        public bool end0Fallback;
        public bool end1Fallback;
    }

    void Awake()
    {
        InitLines();

        lineMaterial.renderQueue = 3100;

        if (debugMode)
        {
            whiteDot = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whiteDot.SetPixel(0, 0, Color.white);
            whiteDot.Apply();
        }


        Debug.Log("PaperDetector initialized.");
    }

    public void DrawWhiteDots(Matrix4x4 D, bool firstFrame, Vector2[] imgCorners, Vector2 camTexSize)
    {
        if (!firstFrame || !debugMode) return;   // no frame yet or we are not debugging

        foreach (var cpu in imgCorners)
        {
            Vector2 vp = Converter.FromRawCpuToViewport(D, cpu, new Vector2(camTexSize.x, camTexSize.y)); // 0..1
            Vector2 gui = new Vector2(vp.x * Screen.width,
                                      (1 - vp.y) * Screen.height); // GUI y is top→down

            GUI.DrawTexture(new Rect(gui, new Vector2(18, 18)), whiteDot);
        }
    }

    public void InitLines()
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

    public void DisableLines()
    {
        foreach (var seg in segments)
        {
            seg.lr.enabled = false;
        }
        Debug.Log("LineRenderers disabled.");
    }



    public void PlaceLinesFromViewport(Vector2[] vpCorners)
    {
        Vector3[] worldPos = new Vector3[vpCorners.Length];
        bool[] usedFallback = new bool[vpCorners.Length];

        for (int i = 0; i < vpCorners.Length; i++)
        {
            Vector2 vp = vpCorners[i];
            var ray = Camera.main.ViewportPointToRay(vp);
            Vector3 hitPt;
            bool hitPlane = false;

            if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon))
            {
                var chosen = hits[0];
                var plane = arPlaneManager.GetPlane(chosen.trackableId);
                hitPt = chosen.pose.position;
                hitPlane = true;
            }
            else
            {
                hitPt = ray.GetPoint(1.0f);
            }

            worldPos[i] = hitPt;

            usedFallback[i] = !hitPlane;

            Debug.Log($" Corner[{i}] to World: {worldPos[i]:F2} {(hitPlane ? "YES" : "NO")}");
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

            Debug.Log($"Segment[{i}] From {worldPos[i]:F2} to {worldPos[j]:F2} Color: {(anyFallback ? "YELLOW" : "BLUE")}");
        }
    }
}
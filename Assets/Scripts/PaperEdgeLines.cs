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
        if (!firstFrame || !debugMode) return;

        if (imgCorners == null || imgCorners.Length == 0)
        {
            Debug.LogWarning("DrawWhiteDots: imgCorners is null or empty!");
            return;
        }
        if (whiteDot == null)
        {
            Debug.LogWarning("DrawWhiteDots: whiteDot texture is null! Did Awake() run?");
            return;
        }
        if (camTexSize.x <= 1 || camTexSize.y <= 1)
        {
            Debug.LogWarning("DrawWhiteDots: camTexSize is invalid: " + camTexSize);
            return;
        }

        foreach (var cpu in imgCorners)
        {
            Vector2 vp = Converter.FromRawCpuToViewport(D, cpu, camTexSize); // 0..1
            Vector2 gui = new Vector2(vp.x * Screen.width, (1 - vp.y) * Screen.height);

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
        // Step 1: Convert all viewport corners to world positions, also track fallback usage.
        Vector3[] worldPos = new Vector3[vpCorners.Length];
        bool[] usedFallback = new bool[vpCorners.Length];

        for (int i = 0; i < vpCorners.Length; i++)
        {
            worldPos[i] = Converter.ViewportToWorld(vpCorners[i], raycastManager, hits, out usedFallback[i]);
            Debug.Log($" Corner[{i}] to World: {worldPos[i]:F2} {(usedFallback[i] ? "NO" : "YES")}");
        }

        // Step 2: Draw segments (lines) between the world positions
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            DrawSegment(i, j, worldPos);
        }
    }

    // Helper: Draw a single segment (line) between two world points.
    private void DrawSegment(int i, int j, Vector3[] worldPos)
    {
        var seg = segments[i];
        seg.lr.enabled = true;
        seg.lr.SetPosition(0, worldPos[i]);
        seg.lr.SetPosition(1, worldPos[j]);
        
        Color col = Color.blue;
        seg.lr.startColor = seg.lr.endColor = col;

        Debug.Log($"Segment[{i}] From {worldPos[i]:F2} to {worldPos[j]:F2}");
    }

}
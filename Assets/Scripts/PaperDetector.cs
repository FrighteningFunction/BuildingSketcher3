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
    [SerializeField] PipelineDebugger pipelineDebugger;

    [Header("Line Settings")]
    [SerializeField] Material lineMaterial;
    [SerializeField] float lineWidth = 0.005f;

    /* ---------- internals ---------- */
    private Texture2D camTex;
    private lineSegment[] segments = new lineSegment[4];
    private static readonly List<ARRaycastHit> hits = new();

    //temp
    private Vector2[] imgCorners;
    private Matrix4x4 D;
    private Texture2D whiteDot;

    void OnGUI()
    {
        if (debugImage.texture == null) return;   // no frame yet

        foreach (var cpu in imgCorners)
        {
            Vector2 vp = fromRawCpuToViewport(D, cpu); // 0..1
            Vector2 gui = new Vector2(vp.x * Screen.width,
                                      (1 - vp.y) * Screen.height); // GUI y is top→down

            GUI.DrawTexture(new Rect(gui, new Vector2(18, 18)), whiteDot);
        }
    }



    struct lineSegment
    {
        public LineRenderer lr;
        public bool end0Fallback;
        public bool end1Fallback;
    }

    void Awake()
    {
        InitLines();
        arPlaneManager.planePrefab.active = true;
        lineMaterial.renderQueue = 3100;

        // create a 1×1 white texture
        whiteDot = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        whiteDot.SetPixel(0, 0, Color.white);
        whiteDot.Apply();


        Debug.Log("PaperDetector initialized.");
    }

    void OnEnable() => cameraManager.frameReceived += OnFrame;
    void OnDisable() => cameraManager.frameReceived -= OnFrame;

    void OnFrame(ARCameraFrameEventArgs args)
    {

        Debug.Log("--------------------------------NEW FRAME---------------------------------\n"+
                  "--------------------------------------------------------------------------");

        if (!cameraManager.TryAcquireLatestCpuImage(out var cpu))
            return;

        UpdateTexture(cpu);        

        byte[] rgba = camTex.GetRawTextureData<byte>().ToArray();
        if (!PaperPlugin.FindPaperCorners(rgba, camTex.width, camTex.height, out imgCorners))
        {
            foreach (var seg in segments)
                seg.lr.enabled = false;

            Debug.LogWarning(" No paper corners detected.");
            return;
        }

        Matrix4x4 displayMatrix;
        if (!args.displayMatrix.HasValue)
        {
            Debug.LogWarning(" No display matrix available.");
            return;
        }
        else
        {
            displayMatrix = args.displayMatrix.Value;
            D = displayMatrix;
        }

        // 2) convert each raw image‐space corner → normalized UV → into the same UV space
        Vector2[] viewportCorners = new Vector2[imgCorners.Length];
        for(int i = 0; i < imgCorners.Length; i++)
        { 

            viewportCorners[i] = fromRawCpuToViewport(displayMatrix, imgCorners[i]);
        }


        Vector2[] ordered = OrderCorners(viewportCorners);

        for (int i = 0; i < viewportCorners.Length; ++i)
            Debug.Log($"vp[{i}] = {viewportCorners[i]}");   // should stay between 0-1
        PlaceLinesFromViewport(ordered);

        //Debugging

        pipelineDebugger.printConverterCornersDebug(displayMatrix, new Vector2(camTex.width, camTex.height), fromRawCpuToViewport);

        pipelineDebugger.logTransormedCorners(displayMatrix, new Vector2(camTex.width, camTex.height), fromRawCpuToViewport);

        pipelineDebugger.printDisplayMatrix(displayMatrix);

        //pipelineDebugger.SetupPrinted();
    }

    public Vector2 fromRawCpuToViewport(Matrix4x4 D, Vector2 cord)
    {
        float u = cord.x / camTex.width;
        float v = cord.y / camTex.height;

        Vector4 uv = new Vector4(u, v, 1f, 0f);
        Vector4 mapped = D.transpose * uv;

        //if (mapped.w != 0f)                                   // <-- projective divide
        //{
        //    mapped.x /= mapped.w;
        //    mapped.y /= mapped.w;
        //}

        //remove cropping
        float topCrop = 1f - D[2, 1];
        Debug.Log($"topCrop = {topCrop}");
        float scaleY = 1f / (1f - 2f*topCrop);

        mapped.y = (mapped.y - topCrop) * scaleY;


        Debug.Log($"mapped = {mapped}");

        return new Vector2(mapped.x, mapped.y);
    }

    Vector2[] OrderCorners(Vector2[] c)
    {
        Vector2 center = c.Aggregate(Vector2.zero, (a, b) => a + b) / 4f;
        return c.OrderBy(p => Mathf.Atan2(p.y - center.y, p.x - center.x)).ToArray();
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

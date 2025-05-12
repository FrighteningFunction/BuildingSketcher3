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
    [SerializeField] GameObject cubePrefab;
    [SerializeField] Transform cubesParent;
    [SerializeField] ARPlaneManager arPlaneManager;


    [SerializeField] RawImage debugImage;

    /* ---------- internals ---------- */
    Texture2D camTex;
    readonly GameObject[] cubes = new GameObject[4];
    static readonly List<ARRaycastHit> hits = new();

    readonly Color lockedColor = Color.gray;
    readonly Color fallbackColor = Color.yellow;

    void Awake() => InitCubes();
    void OnEnable() => cameraManager.frameReceived += OnFrame;
    void OnDisable() => cameraManager.frameReceived -= OnFrame;

    /* ================================================================= */
    /*                               FRAME                               */
    /* ================================================================= */
    void OnFrame(ARCameraFrameEventArgs _)
    {
        Debug.Log($"[OnFrame] camTex before = {(camTex == null ? "null" : camTex.width + "x" + camTex.height)}");
        if (!cameraManager.TryAcquireLatestCpuImage(out var cpu))
        {
            Debug.Log("[OnFrame] No CPU image acquired");
            return;
        }

        UpdateTexture(cpu);
        Debug.Log($"[OnFrame] camTex after = {camTex.width} x {camTex.height}");
        byte[] rgba = camTex.GetRawTextureData<byte>().ToArray();
        Debug.Log($"[OnFrame] RGBA byte count = {rgba.Length}");

        if (!PaperPlugin.FindPaperCorners(
                 rgba, camTex.width, camTex.height,
                 out Vector2[] imgCorners))
        {
            Debug.Log("Plugin: paper NOT found.");
            return;
        }

        Debug.Log($"Plugin corners img px: {string.Join(" ", imgCorners.Select(v => $"[{v.x:F0},{v.y:F0}]"))}");

        Vector2[] ordered = OrderCorners(imgCorners);
        Debug.Log($"Ordered corners img px: {string.Join(" ", ordered.Select(v => $"[{v.x:F0},{v.y:F0}]"))}");

        PlaceCubes(ordered);
    }

    /* Quick heuristic ordering: Sort by angle around center */
    private Vector2[] OrderCorners(Vector2[] c)
    {
        Vector2 center = c.Aggregate(Vector2.zero, (a, b) => a + b) / 4f;
        var sorted = c.OrderBy(p => Mathf.Atan2(p.y - center.y, p.x - center.x)).ToArray();
        Debug.Log($"[OrderCorners] center = {center}, sorted = {string.Join(" ", sorted.Select(v => $"({v.x:F0},{v.y:F0})"))}");
        return sorted;
    }

    /* ================================================================= */
    /*                            HELPERS                                */
    /* ================================================================= */

    void InitCubes()
    {
        for (int i = 0; i < 4; ++i)
        {
            cubes[i] = Instantiate(cubePrefab, cubesParent);
            cubes[i].name = $"Cube_{i}";
            cubes[i].SetActive(false);
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

        Debug.Log($"[UpdateTexture] CPU image = {img.width}x{img.height}, conversion output = {p.outputDimensions.x}x{p.outputDimensions.y}");

        if (camTex == null || camTex.width != img.width || camTex.height != img.height)
        {
            camTex = new Texture2D(img.width, img.height, TextureFormat.RGBA32, false);
            Debug.Log("[UpdateTexture] Created new Texture2D");
        }

        using var buf = new NativeArray<byte>(img.GetConvertedDataSize(p), Allocator.Temp);
        Debug.Log($"[UpdateTexture] buffer size = {buf.Length}");
        img.Convert(p, buf);
        camTex.LoadRawTextureData(buf);
        camTex.Apply();

        debugImage.texture = camTex;
        img.Dispose();
    }

    /* -- project each corner, try AR raycast -- */
    void PlaceCubes(Vector2[] imgCorners)
    {
        Debug.Log("[PlaceCubes] Starting placement pipeline");
        var worldPos = new Vector3[4];
        var usedFallback = new bool[4];
        TrackableId? masterPlaneId = null;

        for (int i = 0; i < imgCorners.Length; i++)
        {
            Debug.Log($"[PlaceCubes] imgCorner[{i}] = {imgCorners[i]}");
            float vx = imgCorners[i].x / camTex.width;
            float vy = imgCorners[i].y / camTex.height;
            Debug.Log($"[PlaceCubes] normalized coords[{i}] = ({vx:F3},{vy:F3})");

            // switch to ViewportPointToRay
            var ray = Camera.main.ViewportPointToRay(new Vector3(vx, vy, 0));
            Debug.Log($"[PlaceCubes] ray origin = {ray.origin}, dir = {ray.direction}");

            bool didPlaneHit = false;
            Vector3 hitPos = ray.GetPoint(0.5f);
            Debug.Log($"[PlaceCubes] default fallback pos[{i}] = {hitPos}");

            if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
            {
                Debug.Log($"[PlaceCubes] raycast hits count = {hits.Count}");
                ARRaycastHit chosenHit;
                if (masterPlaneId.HasValue)
                {
                    chosenHit = hits.FirstOrDefault(h => h.trackableId == masterPlaneId.Value);
                    Debug.Log($"[PlaceCubes] chosenHit by masterPlaneId = {chosenHit.pose.position}");
                }
                else
                {
                    chosenHit = hits[0];
                    masterPlaneId = chosenHit.trackableId;
                    Debug.Log($"[PlaceCubes] initial masterPlaneId = {masterPlaneId}");
                }

                if (chosenHit.trackableId == masterPlaneId)
                {
                    hitPos = chosenHit.pose.position;
                    didPlaneHit = true;
                    Debug.Log($"[PlaceCubes] plane hitPos[{i}] = {hitPos}");
                }
            }

            if (!didPlaneHit)
            {
                hitPos = ray.GetPoint(0.5f);
                usedFallback[i] = true;
                Debug.Log($"[PlaceCubes] fallback for index {i}, pos = {hitPos}");
            }
            else
            {
                usedFallback[i] = false;
            }

            worldPos[i] = hitPos;
        }

        for (int i = 0; i < cubes.Length; i++)
        {
            var cube = cubes[i];
            cube.transform.position = worldPos[i];
            cube.SetActive(true);
            Debug.Log($"[PlaceCubes] placing cube[{i}] at {worldPos[i]}, fallback = {usedFallback[i]}");

            var rend = cube.GetComponentInChildren<Renderer>();
            rend.material.color = usedFallback[i] ? fallbackColor : lockedColor;
        }

        Debug.Log($"[PlaceCubes] Completed. MasterPlaneId = {masterPlaneId}, any fallback = {usedFallback.Any(b => b)}");
    }
}

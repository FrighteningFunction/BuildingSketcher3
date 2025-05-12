// PaperDetector.cs
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARCameraManager))]
public class PaperDetector : MonoBehaviour
{
    /* ---------- Inspector refs ---------- */
    [SerializeField] ARCameraManager cameraManager;
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] GameObject cubePrefab;
    [SerializeField] Transform cubesParent;
    [SerializeField] ARPlaneManager arPlaneManager;

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
        if (!cameraManager.TryAcquireLatestCpuImage(out var cpu)) return;

        UpdateTexture(cpu);
        byte[] rgba = camTex.GetRawTextureData<byte>().ToArray();

        if (!PaperPlugin.FindPaperCorners(
                 rgba, camTex.width, camTex.height,
                 out Vector2[] imgCorners))
        {
            Debug.Log("Plugin: paper NOT found.");
            return;
        }

        Debug.Log($"Plugin corners img px: {string.Join(" ", imgCorners.Select(v => $"[{v.x:F0},{v.y:F0}]"))}");

        // order them TL, TR, BR, BL for repeatable cube assignment
        Vector2[] ordered = OrderCorners(imgCorners);
        PlaceCubes(ordered);
    }

    /* Quick heuristic ordering: left-most two = top row, sort by Y */
    private Vector2[] OrderCorners(Vector2[] c)
    {
        Vector2 center = c.Aggregate(Vector2.zero, (a, b) => a + b) / 4f;
        var sorted = c.OrderBy(p => Mathf.Atan2(p.y - center.y, p.x - center.x)).ToArray();
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

            
            transformation =
                 XRCpuImage.Transformation.MirrorY
        };

        if (camTex == null
         || camTex.width != img.width
         || camTex.height != img.height)
        {
            camTex = new Texture2D(img.width, img.height, TextureFormat.RGBA32, false);
        }

        using var buf = new NativeArray<byte>(img.GetConvertedDataSize(p), Allocator.Temp);
        img.Convert(p, buf);
        camTex.LoadRawTextureData(buf);
        camTex.Apply();
        img.Dispose();
    }


    /* -- project each corner, try AR raycast -- */
    void PlaceCubes(Vector2[] imgCorners)
    {
        // temp storage per‐corner
        var worldPos = new Vector3[4];
        var usedFallback = new bool[4];

        // the one "master" plane we discovered on corner 0
        TrackableId? masterPlaneId = null;

        for(int i = 0; i < imgCorners.Length; i++)
{
            float vx = imgCorners[i].x / camTex.width;
            float vy = imgCorners[i].y / camTex.height;
            var ray = Camera.main.ViewportPointToRay(new Vector3(vx, vy, 0));

            bool didPlaneHit = false;
            Vector3 hitPos = ray.GetPoint(0.5f); // default fallback

            if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon))
            {
                ARRaycastHit chosenHit = default;
                if (masterPlaneId.HasValue)
                {
                    chosenHit = hits.FirstOrDefault(h => h.trackableId == masterPlaneId.Value);
                }
                else
                {
                    chosenHit = hits[0];
                    masterPlaneId = chosenHit.trackableId;
                }

                if (chosenHit.trackableId == masterPlaneId)
                {
                    hitPos = chosenHit.pose.position;
                    didPlaneHit = true;
                }
            }

            // **fixed** fallback
            if (!didPlaneHit)
            {
                hitPos = ray.GetPoint(0.5f);
                usedFallback[i] = true;
            }
            else
            {
                usedFallback[i] = false;
            }

            worldPos[i] = hitPos;
        }

        // finally, move & color the cubes
        for (int i = 0; i < cubes.Length; i++)
        {
            var cube = cubes[i];
            cube.transform.position = worldPos[i];
            cube.SetActive(true);

            var rend = cube.GetComponentInChildren<Renderer>();
            rend.material.color = usedFallback[i] ? fallbackColor : lockedColor;
        }
    }

    /* ---------- conversions ---------- */

    
}

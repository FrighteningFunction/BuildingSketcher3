using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class Converter
{

    public static Vector2 FromRawCpuToViewport(Matrix4x4 displayMatrix, Vector2 cpuPx, Vector2 XRCpuSize)
    {
        // 1. Normalise to 0-1 
        //    while GL / Unity UV origin is bottom-left.
        float u = cpuPx.x / XRCpuSize.x;
        float v = cpuPx.y / XRCpuSize.y;

        // 2. Build a **row** vector  (u , v , 1 , 0)  and multiply by the (row-
        //    major) display matrix.  Unity matrices are column-major, so we take
        //    the transpose once to compensate.
        Vector4 uvRow = new Vector4(u, v, 1f, 0f);
        Vector4 mapped = displayMatrix.transpose * uvRow;

        // 3. Perspective divide — needed only when ARCore’s image-stabilisation
        //    variant is active (then .z ≠ 1).  Harmless otherwise.
        if (Mathf.Abs(mapped.z) > 1e-6f)
        {
            mapped.x /= mapped.z;
            mapped.y /= mapped.z;
        }

        // 4. Return **viewport** coordinates (still 0-1).  Caller decides whether
        //    to turn them into GUI pixels or cast a ray, etc.
        return new Vector2(mapped.x, mapped.y);
    }

    // Helper: Try to get a world position from a viewport point, with fallback.
    public static Vector3 ViewportToWorld(Vector2 viewport, ARRaycastManager raycastManager, List<ARRaycastHit> hits, out bool usedFallback)
    {
        var ray = Camera.main.ViewportPointToRay(viewport);
        if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon))
        {
            var chosen = hits[0];
            var worldPos = chosen.pose.position;

            usedFallback = false;

            return worldPos;
        }

        usedFallback = true;
        return ray.GetPoint(1.0f);
    }
}
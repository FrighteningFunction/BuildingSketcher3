using UnityEngine;
using System.Collections.Generic;

public static class PaperPolygonClipper
{
    /// <summary>
    /// Clips the segment [a, b] to the convex quadrilateral (given in ccw/cw order as 4 corners).
    /// Returns true if any part is inside and outputs clipped endpoints.
    /// </summary>
    public static bool ClipLineToQuad(Vector2 a, Vector2 b, Vector2[] quad, out Vector2 clippedA, out Vector2 clippedB)
    {
        // Use Liang-Barsky or Cyrus-Beck, but here's a brute-force (good for convex quads)
        clippedA = a;
        clippedB = b;

        // Early out: both endpoints inside?
        if (IsPointInQuad(a, quad) && IsPointInQuad(b, quad))
            return true;

        // Otherwise, clip both ways
        var inside = new List<Vector2>();
        if (IsPointInQuad(a, quad)) inside.Add(a);
        if (IsPointInQuad(b, quad)) inside.Add(b);

        // Intersect segment with all quad edges
        for (int i = 0; i < 4; ++i)
        {
            Vector2 p1 = quad[i];
            Vector2 p2 = quad[(i + 1) % 4];

            if (LineSegmentsIntersect(a, b, p1, p2, out Vector2 intersection))
                inside.Add(intersection);
        }

        if (inside.Count >= 2)
        {
            // Pick the two points that are furthest apart as endpoints
            float maxDist = -1f;
            Vector2 ep1 = inside[0], ep2 = inside[1];
            for (int i = 0; i < inside.Count; ++i)
                for (int j = i + 1; j < inside.Count; ++j)
                {
                    float d = (inside[i] - inside[j]).sqrMagnitude;
                    if (d > maxDist) { maxDist = d; ep1 = inside[i]; ep2 = inside[j]; }
                }
            clippedA = ep1; clippedB = ep2;
            return true;
        }

        // No visible segment inside quad
        return false;
    }

    // Utility: Check if a 2D point is inside a convex quad
    private static bool IsPointInQuad(Vector2 p, Vector2[] quad)
    {
        // Use winding or cross-product; works for convex quad
        for (int i = 0; i < 4; ++i)
        {
            Vector2 a = quad[i], b = quad[(i + 1) % 4];
            Vector2 edge = b - a;
            Vector2 toPoint = p - a;
            float cross = edge.x * toPoint.y - edge.y * toPoint.x;
            if (cross < 0) return false;
        }
        return true;
    }

    // Utility: Segment-segment intersection
    private static bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        Vector2 r = p2 - p1;
        Vector2 s = q2 - q1;
        float denom = r.x * s.y - r.y * s.x;
        if (Mathf.Abs(denom) < 1e-6f) return false; // Parallel

        float t = ((q1 - p1).x * s.y - (q1 - p1).y * s.x) / denom;
        float u = ((q1 - p1).x * r.y - (q1 - p1).y * r.x) / denom;
        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            intersection = p1 + t * r;
            return true;
        }
        return false;
    }
}

using UnityEngine;

public static class QuadHelper
{

    /// Returns TRUE when p lies inside the convex quad Q (cw or ccw order)
    public static bool PointInQuad(Vector2 p, Vector2[] Q)
    {
        // cross-product sign should be identical for all four edges
        float last = 0f;
        for (int i = 0; i < 4; ++i)
        {
            Vector2 a = Q[i];
            Vector2 b = Q[(i + 1) % 4];
            Vector2 edge = b - a;
            Vector2 toP = p - a;
            float cross = edge.x * toP.y - edge.y * toP.x;

            if (i == 0) last = cross;
            else if (cross * last < 0f) return false;      // sign flipped → outside
        }
        return true;
    }

    public static Vector2[] InsetQuad(Vector2[] Q, float insetPx)
    {
        var R = new Vector2[4];
        for (int i = 0; i < 4; ++i)
        {
            Vector2 prev = Q[(i + 3) % 4];
            Vector2 curr = Q[i];
            Vector2 next = Q[(i + 1) % 4];

            Vector2 dirA = (curr - prev).normalized;
            Vector2 dirB = (next - curr).normalized;

            // outwards normals (left-hand rule for ccw)
            Vector2 nA = new Vector2(-dirA.y, dirA.x);
            Vector2 nB = new Vector2(-dirB.y, dirB.x);

            Vector2 bis = (nA + nB).normalized;
            // flip if bisector points outwards
            if (Vector2.Dot(bis, curr - (prev + next) * 0.5f) > 0) bis = -bis;

            R[i] = curr + bis * insetPx;
        }
        return R;
    }
}

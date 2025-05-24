using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class PaperPlugin
{
#if UNITY_ANDROID && !UNITY_EDITOR
    // Name *without* "lib" prefix or ".so" suffix
    private const string LIB_NAME = "PaperPlugin";

    [DllImport(LIB_NAME, EntryPoint = "FindPaperCorners")]
    private static extern bool FindPaperCornersNative(
        byte[] imageData, int width, int height,
        [Out] float[] outCorners);

    [DllImport(LIB_NAME, EntryPoint = "FindBlackLines")]
    private static extern int FindBlackLinesNative(
        byte[] imageData, int width, int height,
        [Out] float[] outLines, int maxLines);
#else
    // Editor stub so code still compiles & runs in Play-Mode
    private static bool FindPaperCornersNative(
        byte[] _, int __, int ___, float[] outCorners)
    { return false; }

    private static int FindBlackLinesNative(
        byte[] _, int __, int ___, float[] ____, int _____)
    { return 0; }
#endif

    /// Safe public wrapper
    public static bool FindPaperCorners(
        byte[] rgbaImage, int width, int height,
        out Vector2[] corners)
    {
        if (rgbaImage == null) throw new ArgumentNullException(nameof(rgbaImage));
        if (rgbaImage.Length != width * height * 4)
            throw new ArgumentException("RGBA32 byte length mismatch");

        float[] raw = new float[8];           // 4 corners × (x,y)
        bool ok = FindPaperCornersNative(rgbaImage, width, height, raw);
        corners = new Vector2[4];
        for (int i = 0; i < 4; ++i)
            corners[i] = new Vector2(raw[i * 2], raw[i * 2 + 1]);
        return ok;
    }

    public static int FindBlackLines(
        byte[] rgbaImage, int width, int height,
        out Vector2[][] lines, int maxLines = 32)
    {
        if (rgbaImage == null) throw new ArgumentNullException(nameof(rgbaImage));
        if (rgbaImage.Length != width * height * 4)
            throw new ArgumentException("RGBA32 byte length mismatch");

        float[] raw = new float[maxLines * 4];
        int numLines = FindBlackLinesNative(rgbaImage, width, height, raw, maxLines);

        lines = new Vector2[numLines][];
        for (int i = 0; i < numLines; ++i)
        {
            lines[i] = new Vector2[2];
            lines[i][0] = new Vector2(raw[i * 4 + 0], raw[i * 4 + 1]);
            lines[i][1] = new Vector2(raw[i * 4 + 2], raw[i * 4 + 3]);
        }
        return numLines;
    }
}


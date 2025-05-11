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
#else
    // Editor stub so code still compiles & runs in Play-Mode
    private static bool FindPaperCornersNative(
        byte[] _, int __, int ___, float[] outCorners)
    { return false; }
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
}


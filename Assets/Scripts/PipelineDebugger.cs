using System;
using UnityEngine;

public class PipelineDebugger : MonoBehaviour
{

    private bool printed = false;

    public void SetupPrinted()
    {
        printed = true;
    }

    public void printConverterCornersDebug(Matrix4x4 D, Vector2 xrCpuSize, Func<Matrix4x4, Vector2, Vector2> converter)
    {
        if(!printed)
        {

            var topLeft = converter(D, new Vector2(0, xrCpuSize.y));
            var topRight = converter(D, new Vector2(xrCpuSize.x, xrCpuSize.y));
            var bottomLeft = converter(D, new Vector2(0, 0));
            var bottomRight = converter(D, new Vector2(xrCpuSize.x, 0));

            float width = Vector2.Distance(bottomLeft, bottomRight);
            float height = Vector2.Distance(bottomLeft, topLeft);

            Debug.Log("&&&&&&&&&&& DEBUGGING &&&&&&&&&&&&&&&\n"+
                $"Viewport rectangle width: {width}, height: {height}");

            
        }

    }

    public void logTransormedCorners(Matrix4x4 D, Vector2 xrCpuSize, Func<Matrix4x4, Vector2, Vector2> converter)
    {
        if (!printed)
        {
            Vector2[] imgCorners = {
            new Vector2(0, 0),
            new Vector2(xrCpuSize.x, 0),
            new Vector2(xrCpuSize.x, xrCpuSize.y),
            new Vector2(0,  xrCpuSize.y)
            };

            for (int i = 0; i < imgCorners.Length; i++)
            {
                Vector2 viewportCorner = converter(D, imgCorners[i]);
                Debug.Log($"Corner {i}: cpu {imgCorners[i]} -> viewport {viewportCorner}");
            } 
        }

    }
}

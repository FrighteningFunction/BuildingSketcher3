using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class DEPRECATED_PaperDetector_DEPRECATED : MonoBehaviour
{
    public ARTrackedImageManager m_ImageManager;
    public TextMeshProUGUI detectionStatusText;
    public GameObject paperPlanePrefab;
    public GameObject testCube;

    private Dictionary<string, ARTrackedImage> trackedImages = new();

    private Dictionary<string, GameObject> spawnedCubes = new();


    private GameObject spawnedPlane;

    

    private readonly string[] requiredMarkers = { "TopLeft", "TopRight", "BottomLeft", "BottomRight" };

    void Awake() => Debug.Log("PaperDetector.Awake");

    void OnEnable()
    {
        Debug.Log("PaperDetector enabled");
        m_ImageManager.trackablesChanged.AddListener(OnChanged);
    }

    void OnDisable()
    {
        m_ImageManager.trackablesChanged.RemoveListener(OnChanged);
    }

    void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        // Add or update tracked markers
        foreach (var img in eventArgs.added)
            TryTrack(img);

        foreach (var img in eventArgs.updated)
            TryTrack(img);

        //remove
        foreach (var removed in eventArgs.removed)
        {
            string name = removed.Value.referenceImage.name;
            if (trackedImages.ContainsKey(name))
            {
                trackedImages.Remove(name);
                DeleteCube(name); // Delete the cube associated with the marker
                Debug.Log($"Marker {name} removed.");
            }
        }

        //UpdatePaperStatus();

        UpdatePlaceholdersAndUI();
    }

    private void DeleteCube(string name)
    {
        if (spawnedCubes.TryGetValue(name, out var cube))
        {
            Destroy(cube);
            spawnedCubes.Remove(name);
        }
    }

    private void AddCube(ARTrackedImage img)
    {
        string name = img.referenceImage.name;
        if (!spawnedCubes.ContainsKey(name))
        {
            GameObject cube = Instantiate(testCube, img.transform.position, img.transform.rotation);
            spawnedCubes[name] = cube;
            Debug.Log($"Spawned cube for {name}");
        }
    }

    private void TryTrack(ARTrackedImage img)
    {
        if (img.trackingState == TrackingState.Tracking)
        {
            string name = img.referenceImage.name;
            if (System.Array.Exists(requiredMarkers, m => m == name))
            {
                trackedImages[name] = img;
                AddCube(img);
                Debug.Log($"Marker {name} tracked.");
            }
        }
    }

    private void UpdatePaperStatus()
    {
        if (AllMarkersFound())
        {
            detectionStatusText.text = "Paper Detected!";

            Vector3 center = Vector3.zero;
            foreach (var img in trackedImages.Values)
            {
                center += img.transform.position;
            }
            center /= trackedImages.Count;

            Quaternion rotation = trackedImages["TopLeft"].transform.rotation;

            if (spawnedPlane == null)
            {
                spawnedPlane = Instantiate(paperPlanePrefab, center, rotation);
                Debug.Log("Spawned paper plane.");
            }
            else
            {
                spawnedPlane.transform.SetPositionAndRotation(center, rotation);
                Debug.Log("Updated paper plane.");
            }
        }
        else
        {
            detectionStatusText.text = $"Paper Not Detected ({trackedImages.Count}/4)";

            if (spawnedPlane != null)
            {
                Destroy(spawnedPlane);
                spawnedPlane = null;
                Debug.Log("Destroyed paper plane.");
            }
        }
    }


    private bool AllMarkersFound()
    {
        foreach (var marker in requiredMarkers)
        {
            if (!trackedImages.ContainsKey(marker))
            {
                Debug.Log($"Missing: {marker}");
                return false;
            }
        }
        return true;
    }

    void UpdatePlaceholdersAndUI()
    {
        // Update each cube's position/rotation
        foreach (var kv in spawnedCubes)
        {
            var name = kv.Key;
            var cube = kv.Value;
            var img = trackedImages[name];
            cube.transform.SetPositionAndRotation(img.transform.position, img.transform.rotation);
        }

        // Update status text
        if (trackedImages.Count == requiredMarkers.Length)
            detectionStatusText.text = "All Markers Tracked";
        else
            detectionStatusText.text = $"Tracking {trackedImages.Count}/4";
    }
}

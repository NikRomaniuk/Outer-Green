using System.Collections.Generic;
using UnityEngine;

public class PlantSegmentsManager : MonoBehaviour
{
    // Stores active input points gathered from PlantSegment instances
    public List<Vector2> activeInputPoints = new List<Vector2>();

    void Start()
    {
    }

    void Update()
    {
    }

    // Clears the active input points list
    public void ClearActiveInputPoints()
    {
        activeInputPoints.Clear();
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "NewSegment", menuName = "Scriptable Objects/Plant Segment")]
public class PlantSegment : ScriptableObject
{
    // Unique identifier for the plant segment type
    public string id;
    // Input connection points (local positions) where this segment can receive attachments from other segments
    public GameObject[] inputPoints;
    // Output connection points (local positions) where this segment connects to other segments
    public Vector2[] outputPoints;
}

using UnityEngine;

public enum PotSize
{
    Small,
    Medium,
    Large
}

public class PotManager : MonoBehaviour
{
    [SerializeField] private PlantSegmentsManager plantSegmentsManager;
    [SerializeField] private GameObject plantSlot;
    [SerializeField] private PotSize potSize = PotSize.Medium;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

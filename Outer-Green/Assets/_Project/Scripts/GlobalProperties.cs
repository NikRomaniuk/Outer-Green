using UnityEngine;

public class GlobalProperties : MonoBehaviour
{
    public static GlobalProperties Instance { get; private set; }

    [Header("Dynamic Scene Data")]
    public Quaternion BillboardingRotation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Optional: Don't destroy this object when loading new scenes
        // DontDestroyOnLoad(gameObject); 
    }

    private void Start()
    {
        //SetBillboardingRotation();
    }

    public void SetBillboardingRotation()
    {
        if (Camera.main == null) return;

        // Get camera direction
        Vector3 lookDir = Camera.main.transform.forward;
        lookDir.y = 0;

        if (lookDir.sqrMagnitude > 0.001f)
        {
            BillboardingRotation = Quaternion.LookRotation(lookDir);
        }
    }
}

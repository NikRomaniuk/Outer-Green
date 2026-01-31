using UnityEngine;
using System;

// ═════════════════════════════════════════════════════════════════════════════════════════════════
// TIME MANAGER - Event-Driven Cycle System
// ═════════════════════════════════════════════════════════════════════════════════════════════════
// Core time management system that tracks game cycles and triggers growth events
//
// EVENT FLOW:
// 1. Timer accumulates deltaTime until cycleDuration is reached
// 2. OnCyclesPassed event fires → PlantManager processes growth logic
// 3. OnCyclesCompleted event fires → PlantManager finalizes visual updates
// 4. Cycle repeats
//
// PERSISTENCE:
// - Saves total cycles and last cycle time to PlayerPrefs
// - On startup, calculates missed cycles during absence and simulates them
//
// EVENTS:
// - OnCyclesPassed(long count): Fired when cycles complete, carries cycle count for logic processing
// - OnCyclesCompleted(): Fired after OnCyclesPassed, used for visual updates and finalization
// ═════════════════════════════════════════════════════════════════════════════════════════════════

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    // -=-=- Configuration -=-=-
    [Header("Cycle Settings")]
    [SerializeField] private float cycleDuration = 5f;
    
    [Header("Debug Options")]
    [Tooltip("Reset all saved cycles and time data on start")]
    [SerializeField] private bool resetOnStart = false;
    
    [Tooltip("Manually add cycles on start (for testing)")]
    [SerializeField] private long addCyclesOnStart = 0;
    
    [Tooltip("Disable absence simulation")]
    [SerializeField] private bool disableAbsenceSimulation = false;

    // -=-=- State -=-=-
    private float _timer;
    public long CyclesTotal { get; private set; }
    private bool _firstCycle = true;
    
    // -=-=- Events -=-=-
    public static event Action<long> OnCyclesPassed;    // Logic processing (growth calculations)
    public static event Action OnCyclesCompleted;       // Visual finalization (spawn visuals)

    // -=-=- Persistence Keys -=-=-
    private const string LAST_CYCLE_TIME_KEY = "TimeManager_LastCycleTime";
    private const string CYCLES_TOTAL_KEY = "TimeManager_CyclesTotal";

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Reset data if requested
        if (resetOnStart)
        {
            Debug.LogWarning("[TimeManager] Resetting all saved cycle data!");
            PlayerPrefs.DeleteKey(LAST_CYCLE_TIME_KEY);
            PlayerPrefs.DeleteKey(CYCLES_TOTAL_KEY);
            PlayerPrefs.Save();
            CyclesTotal = 0;
        }

        // Load saved cycles total
        if (PlayerPrefs.HasKey(CYCLES_TOTAL_KEY))
        {
            string cyclesStr = PlayerPrefs.GetString(CYCLES_TOTAL_KEY, "0");
            if (long.TryParse(cyclesStr, out long savedCycles))
            {
                CyclesTotal = savedCycles;
                Debug.Log($"[TimeManager] Loaded saved CyclesTotal: {CyclesTotal}");
            }
        }
        else
        {
            Debug.Log("[TimeManager] No saved cycles found, starting fresh");
        }

        // Calculate absence simulation
        if (!disableAbsenceSimulation)
        {
            SimulateAbsence();
        }
        else
        {
            Debug.Log("[TimeManager] Absence simulation disabled");
            SaveTimeData();
        }
        
        // Add manual cycles if requested
        if (addCyclesOnStart > 0)
        {
            Debug.Log($"[TimeManager] Adding {addCyclesOnStart} manual cycles on start");
            CyclesTotal += addCyclesOnStart;
            SaveTimeData();
            OnCyclesPassed?.Invoke(addCyclesOnStart);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // CYCLE PROCESSING
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= cycleDuration)
        {
            long cyclesCount = (long)Mathf.FloorToInt(_timer / cycleDuration);
            _timer %= cycleDuration; // Carry over leftover time

            CyclesTotal += cyclesCount;
            
            Debug.Log($"[TimeManager] Cycle passed! Count: {cyclesCount}, Total: {CyclesTotal}");
            
            // Save current time and total cycles
            SaveTimeData();

            // Call after every cycle update to make sure all logic is processed
            if (!_firstCycle)
            {
                OnCyclesCompleted?.Invoke();
            }
            else
            {
                _firstCycle = false;
            }

            // Notify subscribers that cycles have passed
            OnCyclesPassed?.Invoke(cyclesCount);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // ABSENCE SIMULATION
    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // Calculates missed cycles since last session and triggers catch-up growth

    private void SimulateAbsence()
    {
        if (!PlayerPrefs.HasKey(LAST_CYCLE_TIME_KEY))
        {
            // First time running - save current time
            Debug.Log("[TimeManager] First run, no absence simulation needed");
            SaveTimeData();
            return;
        }

        // Get last saved time
        string lastTimeStr = PlayerPrefs.GetString(LAST_CYCLE_TIME_KEY);
        if (!long.TryParse(lastTimeStr, out long lastTimeTicks))
        {
            SaveTimeData();
            return;
        }

        // Calculate time difference
        long currentTicks = DateTime.Now.Ticks;
        long elapsedTicks = currentTicks - lastTimeTicks;
        double elapsedSeconds = TimeSpan.FromTicks(elapsedTicks).TotalSeconds;

        // Convert to cycles
        long missedCycles = (long)(elapsedSeconds / cycleDuration);

        if (missedCycles > 0)
        {
            CyclesTotal += missedCycles;
            Debug.Log($"[TimeManager] Absence simulation: {missedCycles} cycles missed (elapsed: {elapsedSeconds:F1}s)");
            SaveTimeData();

            // Notify subscribers about missed cycles
            OnCyclesPassed?.Invoke(missedCycles);
        }
        else
        {
            Debug.Log($"[TimeManager] No cycles missed (elapsed: {elapsedSeconds:F1}s)");
            // Update time even if no cycles passed
            SaveTimeData();
        }
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // PERSISTENCE
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    private void SaveTimeData()
    {
        PlayerPrefs.SetString(LAST_CYCLE_TIME_KEY, DateTime.Now.Ticks.ToString());
        PlayerPrefs.SetString(CYCLES_TOTAL_KEY, CyclesTotal.ToString());
        PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        SaveTimeData();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveTimeData();
        }
    }
}

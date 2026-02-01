using UnityEngine;
using System;
using UnityEngine.SceneManagement;

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
// - Saves total cycles to PlayerPrefs
//
// EVENTS:
// - OnCyclesPassed(): Fired when cycles complete, used for logic processing
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

    // -=-=- State -=-=-
    private float _timer;
    public long CyclesTotal { get; private set; }
    private bool _firstCycle = true;
    
    [Header("Startup")]
    [Tooltip("Pause cycle timer on scene load for this many seconds before starting")]
    [SerializeField] private bool pauseOnStart = true;
    [SerializeField, Min(0f)] private float startupPauseDuration = 3f;
    private float _startupPauseTimer = 0f;
    private bool _isStartupPaused = false;
    
    // -=-=- Events -=-=-
    public static event Action OnCyclesPassed;          // Logic processing (growth calculations)
    public static event Action OnCyclesCompleted;       // Visual finalization (spawn visuals)

    // -=-=- Persistence Keys -=-=-
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
        
        // Configure startup pause
        _isStartupPaused = pauseOnStart;
        _startupPauseTimer = startupPauseDuration;
        if (_isStartupPaused)
        {
            Debug.Log($"[TimeManager] Startup pause enabled: duration {_startupPauseTimer} seconds");
        }

        // Subscribe to scene loaded to reapply startup pause on scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Unsubscribe from sceneLoaded when destroyed
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Handle scene loaded and reapply startup pause if configured
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!pauseOnStart) return;
        _isStartupPaused = true;
        _startupPauseTimer = startupPauseDuration;
        Debug.Log($"[TimeManager] Scene loaded ({scene.name}), startup pause reapplied: {_startupPauseTimer} seconds");
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // CYCLE PROCESSING
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        // If we are paused at startup, handle the pause and optionally wait for simulation to finish
        if (_isStartupPaused)
        {
            if (_startupPauseTimer > 0f)
            {
                _startupPauseTimer -= Time.deltaTime;
                return; // keep timer paused until initial duration elapses
            }

            // initial pause elapsed; only resume if no simulation is running
            if (SimulationManager.IsSimulating)
            {
                // keep waiting until simulation finishes
                return;
            }

            _isStartupPaused = false;
            Debug.Log("[TimeManager] Startup pause ended; resuming cycle timer.");
        }

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
            OnCyclesPassed?.Invoke();
        }
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════════
    // PERSISTENCE
    // ═════════════════════════════════════════════════════════════════════════════════════════════════

    private void SaveTimeData()
    {
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

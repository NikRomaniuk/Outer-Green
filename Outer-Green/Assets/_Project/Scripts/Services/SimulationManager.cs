using UnityEngine;

public static class SimulationManager
{
    // -=-=- Simulation -=-=-
    // Semaphore (Synchronization Primitive) to track and sync processes
    private static int _activeProcesses = 0;

    public static void RegisterProcess() => _activeProcesses++; // Call when a process starts
    public static void UnregisterProcess() => _activeProcesses--; // Call when a process ends

    // Property that checks if simulation is active
    public static bool IsSimulating => _activeProcesses > 0;
}

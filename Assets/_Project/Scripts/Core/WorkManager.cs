using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages work assignments for the commune.
/// WHY: Automates assigning members to work buildings.
/// </summary>
public class WorkManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How often to check and assign work (seconds)")]
    [SerializeField] private float assignmentCheckInterval = 5f;

    [Tooltip("Automatically assign idle members to work?")]
    [SerializeField] private bool autoAssignWork = true;

    private float assignmentTimer = 0f;

    // Singleton
    public static WorkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!autoAssignWork) return;

        assignmentTimer += Time.deltaTime;

        if (assignmentTimer >= assignmentCheckInterval)
        {
            assignmentTimer = 0f;
            AssignIdleMembersToWork();
        }
    }

    /// <summary>
    /// Find idle members and assign them to work buildings that need workers.
    /// </summary>
    private void AssignIdleMembersToWork()
    {
        if (SimulationManager.Instance == null || BuildingManager.Instance == null)
            return;

        // Get all members without work assignments
        var members = SimulationManager.Instance.GetAllMembers();
        var idleMembers = new List<Member>();

        foreach (var member in members)
        {
            if (member.IsUsingBuilding() || member.GetTargetBuilding() != null)
            {
                Debug.Log($"Skipping {member.PersonName} - currently busy with a building");
                continue;
            }

            if (!member.HasWorkAssignment())
            {
                idleMembers.Add(member);
            }
        }

        if (idleMembers.Count == 0)
        {
            // Everyone already has work
            return;
        }

        // Find work buildings that need workers
        var allBuildings = BuildingManager.Instance.GetAllBuildings();

        foreach (var building in allBuildings)
        {
            if (!building.Definition.isWorkBuilding) continue;

            // Check if building needs more workers
            int currentWorkers = building.GetAssignedWorkers().Count;
            int capacity = building.Definition.workerCapacity;

            if (currentWorkers < capacity && idleMembers.Count > 0)
            {
                // Assign next idle member
                Member member = idleMembers[0];
                bool success = member.AssignToWork(building);

                if (success)
                {
                    idleMembers.RemoveAt(0);
                    Debug.Log($"WorkManager: Assigned {member.PersonName} to {building.Definition.buildingName}");
                }
            }
        }
    }

    /// <summary>
    /// Manually assign a specific member to a specific building.
    /// PUBLIC: Can be called by UI or other systems.
    /// </summary>
    public bool AssignMemberToWork(Member member, Building workBuilding)
    {
        if (member == null || workBuilding == null)
            return false;

        return member.AssignToWork(workBuilding);
    }

    /// <summary>
    /// Unassign a member from their current work.
    /// </summary>
    public void UnassignMember(Member member)
    {
        if (member != null)
        {
            member.UnassignFromWork();
        }
    }

    /// <summary>
    /// Get all members assigned to a specific building.
    /// </summary>
    public List<Member> GetWorkersAtBuilding(Building building)
    {
        if (building == null) return new List<Member>();
        return building.GetAssignedWorkers();
    }

    /// <summary>
    /// Get production summary for all work buildings.
    /// </summary>
    public string GetProductionSummary()
    {
        if (BuildingManager.Instance == null)
            return "No buildings";

        var allBuildings = BuildingManager.Instance.GetAllBuildings();
        string summary = "Production Summary:\n";

        foreach (var building in allBuildings)
        {
            if (building.Definition.isWorkBuilding)
            {
                int workers = building.GetAssignedWorkers().Count;
                int activeWorkers = building.GetActiveWorkerCount();
                string itemName = building.Definition.producedItem?.itemName ?? "Unknown";
                float rate = building.Definition.productionRate;

                summary += $"{building.Definition.buildingName}: {activeWorkers}/{workers} workers, producing {rate}/hour {itemName}\n";
            }
        }

        return summary;
    }
}
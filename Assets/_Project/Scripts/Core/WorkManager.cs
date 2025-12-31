using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Ideology.Structures;

public class WorkManager : MonoBehaviour
{
    public static WorkManager Instance { get; private set; }

    [Header("Work Assignment")]
    [Tooltip("How often to check for idle members and assign work (seconds)")]
    [SerializeField] private float assignmentCheckInterval = 5f;

    private float timeSinceLastCheck = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        timeSinceLastCheck += Time.deltaTime;

        if (timeSinceLastCheck >= assignmentCheckInterval)
        {
            timeSinceLastCheck = 0f;
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

        List<Member> allMembers = SimulationManager.Instance.GetAllMembers();

        foreach (var member in allMembers)
        {
            // Skip if member is already assigned to work
            if (member.HasWorkAssignment())
                continue;

            // Skip if member is currently using a building for needs
            if (member.GetTargetStructure() != null)
            {
                Debug.Log($"Skipping {member.PersonName} - currently busy with a building");
                continue;
            }

            // Find a work building that needs workers
            Building workBuilding = FindWorkBuildingNeedingWorkers();

            if (workBuilding != null)
            {
                // Assign member to work
                bool success = member.AssignToWork(workBuilding);

                if (success)
                {
                    Debug.Log($"WorkManager: Assigned {member.PersonName} to {workBuilding.Definition.structureName}");
                }
            }
        }
    }

    /// <summary>
    /// Find a work building that needs more workers.
    /// </summary>
    private Building FindWorkBuildingNeedingWorkers()
    {
        List<Building> allBuildings = BuildingManager.Instance.GetAllBuildings();

        foreach (var building in allBuildings)
        {
            if (building.Definition.isWorkBuilding)
            {
                int currentWorkers = building.GetAssignedWorkers().Count;
                int maxWorkers = building.Definition.workerCapacity;

                if (currentWorkers < maxWorkers)
                {
                    return building;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Manually assign a member to a specific work building.
    /// PUBLIC: Called by UI or player commands.
    /// </summary>
    public bool AssignMemberToWork(Member member, Building workBuilding)
    {
        if (member == null || workBuilding == null)
            return false;

        if (!workBuilding.Definition.isWorkBuilding)
        {
            Debug.LogWarning($"{workBuilding.Definition.structureName} is not a work building!");
            return false;
        }

        // Unassign from current work first
        if (member.HasWorkAssignment())
        {
            member.UnassignFromWork();
        }

        // Assign to new work
        return member.AssignToWork(workBuilding);
    }

    /// <summary>
    /// Unassign a member from their current work.
    /// PUBLIC: Called by UI or player commands.
    /// </summary>
    public void UnassignMemberFromWork(Member member)
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
        if (building == null)
            return new List<Member>();

        return building.GetAssignedWorkers();
    }

    /// <summary>
    /// Get all work buildings and their worker counts.
    /// </summary>
    public Dictionary<Building, int> GetWorkBuildingStatus()
    {
        Dictionary<Building, int> status = new Dictionary<Building, int>();

        List<Building> allBuildings = BuildingManager.Instance.GetAllBuildings();

        foreach (var building in allBuildings)
        {
            if (building.Definition.isWorkBuilding)
            {
                status[building] = building.GetAssignedWorkers().Count;
            }
        }

        return status;
    }
}
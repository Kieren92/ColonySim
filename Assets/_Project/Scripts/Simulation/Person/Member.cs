using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Ideology;
using Ideology.Structures;

/// <summary>
/// A member of the commune with building usage, inventory, and work behavior.
/// </summary>
public class Member : Person
{
    private Dictionary<Building, float> unreachableBuildingCooldowns = new Dictionary<Building, float>();
    private const float BUILDING_COOLDOWN_TIME = 5f; // Don't retry same building for 5 seconds (reduced for debugging)

    // Track when we last failed to find a structure for a specific need
    private Dictionary<string, float> unfulfillableNeedCooldowns = new Dictionary<string, float>();
    private const float UNFULFILLABLE_NEED_COOLDOWN = 10f; // Don't spam logs for 10 seconds

    // Member-specific data
    public float IdeologyAlignment { get; private set; } = 50f;
    public string Role { get; set; } = "None";

    // AI state
    private float stateTimer = 0f;
    private bool isWorking = false;
    private bool isResting = false;

    // Building usage
    private Structure targetStructure;
    private bool isUsingBuilding = false;
    private float buildingUseTimer = 0f;

    // Work assignment
    private Building assignedWorkBuilding;
    private float workTimer = 0f;
    private float workSessionDuration = 60f; // Work for 60 seconds before break

    // Role assignment
    private RoleDefinition assignedRole;

    public void InitializeAsMember(
        string name,
        int age,
        List<NeedDefinition> needs,
        List<SkillDefinition> skills,
        float startingIdeologyAlignment = 50f)
    {
        Initialize(name, age, needs, skills);
        IdeologyAlignment = startingIdeologyAlignment;

        // Start with low random needs for testing
        foreach (var need in Needs.GetAllNeeds())
        {
            need.currentValue = Random.Range(20f, 60f);
        }

        GameEvents.TriggerMemberJoined(this);
    }

    public override void UpdateSimulation(float deltaTime)
    {
        // Update needs
        Needs.UpdateNeeds(deltaTime);

        // Update state timer
        stateTimer += deltaTime;

        // Update cooldowns for unreachable buildings
        UpdateBuildingCooldowns(deltaTime);

        // Update cooldowns for unfulfillable needs
        UpdateUnfulfillableNeedCooldowns(deltaTime);

        // Update building usage
        if (isUsingBuilding)
        {
            UpdateBuildingUsage(deltaTime);
        }
        else
        {
            // Check needs every 2 seconds
            if (stateTimer >= 2f && targetStructure == null)
            {
                stateTimer = 0f;
                UpdateAI();
            }
        }
    }

    private void UpdateBuildingCooldowns(float deltaTime)
    {
        // Create a list of keys to avoid modifying dictionary during enumeration
        List<Building> toRemove = new List<Building>();
        List<Building> keys = new List<Building>(unreachableBuildingCooldowns.Keys);

        foreach (var building in keys)
        {
            unreachableBuildingCooldowns[building] -= deltaTime;

            if (unreachableBuildingCooldowns[building] <= 0)
            {
                toRemove.Add(building);
            }
        }

        foreach (var building in toRemove)
        {
            unreachableBuildingCooldowns.Remove(building);
            Debug.Log($"{PersonName}: {building.Definition.structureName} removed from cooldown, can try again");
        }
    }

    private void UpdateUnfulfillableNeedCooldowns(float deltaTime)
    {
        List<string> toRemove = new List<string>();
        List<string> keys = new List<string>(unfulfillableNeedCooldowns.Keys);

        foreach (var needName in keys)
        {
            unfulfillableNeedCooldowns[needName] -= deltaTime;

            if (unfulfillableNeedCooldowns[needName] <= 0)
            {
                toRemove.Add(needName);
            }
        }

        foreach (var needName in toRemove)
        {
            unfulfillableNeedCooldowns.Remove(needName);
        }
    }

    /// <summary>
    /// AI decision making - work, satisfy needs, or idle.
    /// </summary>
    private void UpdateAI()
    {
        // Priority 1: Critical needs (below 30)
        var (needDef, value) = Needs.GetMostUrgentNeed();

        if (needDef != null && value < 30f)
        {
            // Need is critical - go satisfy it
            HandleCriticalNeed(needDef);
            return;
        }

        // DEBUG: Show current state
        if (targetStructure != null)
        {
            Debug.Log($"{PersonName}: Has target {targetStructure.Definition.structureName}, isUsingBuilding={isUsingBuilding}");
        }
        else
        {
            Debug.Log($"{PersonName}: No target building, state is IDLE");
        }

        // Priority 2: Go to work if assigned
        if (assignedWorkBuilding != null && !isUsingBuilding)
        {
            // Check if needs are too low to work (need break)
            if (Needs.GetNeedValue("Energy") < 40f)
            {
                // Too tired to work - take a break
                ChangeState("TakingBreak");
                isWorking = false;
                isResting = true;
            }
            else
            {
                // Go to work
                if (targetStructure != assignedWorkBuilding)
                {
                    targetStructure = assignedWorkBuilding;
                    ChangeState($"GoingToWork_{assignedWorkBuilding.Definition.structureName}");
                }
            }

            return;
        }

        // Priority 3: Moderate needs (30-60)
        if (needDef != null && value < 60f)
        {
            HandleCriticalNeed(needDef);
            return;
        }

        // Priority 4: Idle / wander
        ChangeState("Idle");
        isWorking = false;
        isResting = false;

        // Update activity state for need decay modifiers
        Needs.SetActivityState(isWorking, isResting);
    }

    /// <summary>
    /// Handle a critical need by finding appropriate structure.
    /// </summary>
    private void HandleCriticalNeed(NeedDefinition needDef)
    {
        Structure structure = FindStructureForNeed(needDef.needName);

        // Check if structure is on cooldown
        if (structure != null && IsStructureOnCooldown(structure))
        {
            Debug.Log($"{PersonName}: {structure.Definition.structureName} is on cooldown, skipping");
            structure = null; // Treat as if no structure found
        }

        // Only log if we haven't recently logged about this unfulfillable need
        bool shouldLog = !unfulfillableNeedCooldowns.ContainsKey(needDef.needName);

        if (shouldLog)
        {
            Debug.Log($"{PersonName}: Need {needDef.needName} is critical ({Needs.GetNeedValue(needDef.needName):F1}), looking for structure... Found: {structure?.Definition.structureName ?? "NONE"}");
        }

        if (structure != null)
        {
            targetStructure = structure;
            ChangeState($"GoingTo_{structure.Definition.structureName}");

            if (shouldLog)
            {
                Debug.Log($"{PersonName}: Target set to {structure.Definition.structureName} at grid {structure.GridPosition}");
            }
        }
        else
        {
            // No structure available - add to cooldown to prevent spam
            if (shouldLog)
            {
                unfulfillableNeedCooldowns[needDef.needName] = UNFULFILLABLE_NEED_COOLDOWN;
            }

            // No structure available (or all structures on cooldown)
            switch (needDef.needName)
            {
                case "Hunger":
                case "Thirst":
                    ChangeState("SeekingFood");
                    break;
                case "Energy":
                    ChangeState("Resting");
                    break;
                case "Social":
                    ChangeState("Socializing");
                    break;
                default:
                    ChangeState("Idle");
                    break;
            }
            isWorking = false;
            isResting = (needDef.needName == "Energy");
        }
    }

    /// <summary>
    /// Find a structure that can satisfy a need.
    /// </summary>
    private Structure FindStructureForNeed(string needName)
    {
        if (StructureManager.Instance == null)
        {
            // Fallback to old BuildingManager if StructureManager not available
            if (BuildingManager.Instance != null)
            {
                return BuildingManager.Instance.FindNearestBuildingForNeed(needName, Position);
            }
            return null;
        }

        return StructureManager.Instance.FindStructureForNeed(needName);
    }

    /// <summary>
    /// Check if a structure (or its parent building) is on cooldown.
    /// </summary>
    private bool IsStructureOnCooldown(Structure structure)
    {
        if (structure is Building building)
        {
            return unreachableBuildingCooldowns.ContainsKey(building);
        }

        // For interior structures, check if parent building is on cooldown
        if (structure is InteriorStructure interiorStructure)
        {
            return unreachableBuildingCooldowns.ContainsKey(interiorStructure.ParentBuilding);
        }

        return false;
    }

    /// <summary>
    /// Notify the view (MemberView) to move to a building.
    /// WHY: Simulation tells presentation what to do, not the other way around.
    /// </summary>
    private void NotifyViewToMoveToBuilding(Building building)
    {
        // This is handled by MemberView checking the member's state
        // MemberView will detect the state change and move accordingly
    }

    /// <summary>
    /// Called by MemberView when member arrives at target structure.
    /// PUBLIC: Called from presentation layer.
    /// </summary>
    public void OnArrivedAtBuilding()
    {
        if (targetStructure != null)
        {
            // Try to start using the structure
            bool success = targetStructure.StartUsing(this);

            if (success)
            {
                isUsingBuilding = true;
                buildingUseTimer = 0f;
                ChangeState($"Using_{targetStructure.Definition.structureName}");
            }
            else
            {
                // Structure at capacity - give up and idle
                Debug.LogWarning($"{PersonName}: Structure at capacity");
                targetStructure = null;
                ChangeState("Idle");
            }
        }
    }

    /// <summary>
    /// Update while using a building or interior structure.
    /// </summary>
    private void UpdateBuildingUsage(float deltaTime)
    {
        if (targetStructure == null) return;

        buildingUseTimer += deltaTime;

        // Get what this structure does
        string satisfiesNeed = null;
        float needRestoreAmount = 0f;
        float useDuration = 0f;

        if (targetStructure is Building building)
        {
            satisfiesNeed = building.Definition.satisfiesNeed;
            needRestoreAmount = building.Definition.needRestoreAmount;
            useDuration = building.Definition.useDuration;
        }
        else if (targetStructure is InteriorStructure interiorStructure)
        {
            satisfiesNeed = interiorStructure.Definition.satisfiesNeed;
            needRestoreAmount = interiorStructure.Definition.needRestoreAmount;
            useDuration = interiorStructure.Definition.useDuration;
        }

        if (!string.IsNullOrEmpty(satisfiesNeed))
        {
            // Check if this is food/water (consumable resources)
            if (satisfiesNeed == "Hunger" || satisfiesNeed == "Thirst")
            {
                // Try to consume an item at regular intervals (every 2 seconds)
                if (buildingUseTimer % 2f < deltaTime)
                {
                    bool consumed = TryConsumeItem(satisfiesNeed);
                    if (consumed)
                    {
                        // Finished consuming
                        FinishUsingBuilding();
                        return;
                    }
                }

                // Done using structure?
                if (buildingUseTimer >= useDuration)
                {
                    FinishUsingBuilding();
                }
            }
            else
            {
                // Other structures restore need gradually (sleep, social)
                float restorePerSecond = needRestoreAmount / useDuration;
                Needs.SatisfyNeed(satisfiesNeed, restorePerSecond * deltaTime);

                // Done using structure?
                if (buildingUseTimer >= useDuration)
                {
                    FinishUsingBuilding();
                }
            }
        }

        // Update activity state for need decay
        Needs.SetActivityState(isWorking, isResting);
    }

    /// <summary>
    /// Try to consume an item to satisfy a need.
    /// WHY: Members eat food, drink water - actual consumption from inventory.
    /// </summary>
    private bool TryConsumeItem(string needName)
    {
        // Find consumable items that satisfy this need
        ItemStack consumable = FindConsumableForNeed(needName);

        if (consumable != null)
        {
            // Consume from personal inventory first
            if (PersonalInventory.HasItem(consumable.definition.itemName, 1))
            {
                PersonalInventory.RemoveItem(consumable.definition.itemName, 1);
                Needs.SatisfyNeed(needName, consumable.definition.needRestoreAmount);

                Debug.Log($"{PersonName} consumed {consumable.definition.itemName} from personal inventory");
                return true;
            }

            // Try to take from commune if personal inventory empty
            if (CommuneInventoryManager.Instance != null)
            {
                bool success = CommuneInventoryManager.Instance.MemberTakeFromCommune(
                    this,
                    consumable.definition.itemName,
                    1
                );

                if (success)
                {
                    // Consume immediately
                    PersonalInventory.RemoveItem(consumable.definition.itemName, 1);
                    Needs.SatisfyNeed(needName, consumable.definition.needRestoreAmount);

                    Debug.Log($"{PersonName} consumed {consumable.definition.itemName} from commune");
                    return true;
                }
            }
        }

        Debug.LogWarning($"{PersonName} has no items to satisfy {needName}");
        return false;
    }

    /// <summary>
    /// Find a consumable item that satisfies a need.
    /// </summary>
    private ItemStack FindConsumableForNeed(string needName)
    {
        // Check personal inventory
        var personalItems = PersonalInventory.GetAllItems();
        foreach (var stack in personalItems)
        {
            if (stack.definition.isConsumable && stack.definition.satisfiesNeed == needName)
            {
                return stack;
            }
        }

        // Check commune inventory
        if (CommuneInventoryManager.Instance != null)
        {
            var communeItems = CommuneInventoryManager.Instance.GetCommuneInventory();
            foreach (var stack in communeItems)
            {
                if (stack.definition.isConsumable && stack.definition.satisfiesNeed == needName)
                {
                    return stack;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Done using building - clean up and return to AI.
    /// </summary>
    private void FinishUsingBuilding()
    {
        if (targetStructure != null)
        {
            targetStructure.StopUsing(this);
            Debug.Log($"{PersonName}: Finished using {targetStructure.Definition.structureName}");
        }

        targetStructure = null;
        isUsingBuilding = false;
        buildingUseTimer = 0f;
        ChangeState("Idle");
    }

    // ===== WORK SYSTEM =====

    /// <summary>
    /// Assign this member to work at a building.
    /// PUBLIC: Called by work assignment system.
    /// </summary>
    public bool AssignToWork(Building workBuilding)
    {
        if (workBuilding == null || !workBuilding.Definition.isWorkBuilding)
            return false;

        // Unassign from previous work
        if (assignedWorkBuilding != null)
        {
            assignedWorkBuilding.UnassignWorker(this);
        }

        // Assign to new work
        bool success = workBuilding.AssignWorker(this);
        if (success)
        {
            assignedWorkBuilding = workBuilding;
            Debug.Log($"{PersonName} assigned to work at {workBuilding.Definition.structureName}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Unassign from current work.
    /// </summary>
    public void UnassignFromWork()
    {
        if (assignedWorkBuilding != null)
        {
            assignedWorkBuilding.UnassignWorker(this);
            assignedWorkBuilding = null;
        }
    }

    /// <summary>
    /// Get the currently assigned work building.
    /// </summary>
    public Building GetAssignedWorkBuilding() => assignedWorkBuilding;

    /// <summary>
    /// Is this member currently assigned to work?
    /// </summary>
    public bool HasWorkAssignment() => assignedWorkBuilding != null;

    /// <summary>
    /// Is this member currently working?
    /// </summary>
    public bool IsWorking() => isWorking;

    /// <summary>
    /// Get the member's current target structure.
    /// </summary>
    public Structure GetTargetStructure() => targetStructure;

    /// <summary>
    /// Get the current target building (alias for MemberView compatibility).
    /// </summary>
    public Structure GetTargetBuilding() => targetStructure;

    /// <summary>
    /// Is member currently using a building?
    /// </summary>
    public bool IsUsingBuilding() => isUsingBuilding;

    // ===== IDEOLOGY =====

    /// <summary>
    /// Adjust member's ideology alignment.
    /// Positive = more collective, Negative = more individual.
    /// </summary>
    public void AdjustIdeologyAlignment(float delta)
    {
        IdeologyAlignment = Mathf.Clamp(IdeologyAlignment + delta, 0f, 100f);
    }

    // ===== ROLE SYSTEM =====

    /// <summary>
    /// Assign a role to this member.
    /// </summary>
    public void AssignRole(RoleDefinition role)
    {
        assignedRole = role;
        Debug.Log($"{PersonName} assigned role: {role?.roleName ?? "None"}");
    }

    /// <summary>
    /// Get the member's currently assigned role.
    /// </summary>
    public RoleDefinition GetAssignedRole() => assignedRole;

    /// <summary>
    /// Check if member has an assigned role.
    /// </summary>
    public bool HasAssignedRole() => assignedRole != null;

    /// <summary>
    /// Clear the current structure target (used when can't reach structure).
    /// </summary>
    public void ClearTargetBuilding()
    {
        if (targetStructure != null)
        {
            // Add to cooldown list (only for buildings, not interior structures)
            Building buildingToBlacklist = null;

            if (targetStructure is Building building)
            {
                buildingToBlacklist = building;
            }
            else if (targetStructure is InteriorStructure interiorStructure)
            {
                // If unreachable interior structure, blacklist the parent building
                buildingToBlacklist = interiorStructure.ParentBuilding;
            }

            if (buildingToBlacklist != null)
            {
                unreachableBuildingCooldowns[buildingToBlacklist] = BUILDING_COOLDOWN_TIME;
                Debug.Log($"{PersonName}: Added {buildingToBlacklist.Definition.structureName} to unreachable list for {BUILDING_COOLDOWN_TIME}s");
            }

            // Only call StopUsing if we actually started using it
            if (isUsingBuilding)
            {
                targetStructure.StopUsing(this);
            }
        }

        targetStructure = null;
        isUsingBuilding = false;
        buildingUseTimer = 0f;

        Debug.Log($"{PersonName}: Cleared unreachable target structure");
    }
}
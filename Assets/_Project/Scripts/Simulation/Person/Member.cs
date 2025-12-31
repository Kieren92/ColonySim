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
    private const float BUILDING_COOLDOWN_TIME = 30f; // Don't retry same building for 30 seconds

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

        Debug.Log($"{PersonName}: Need {needDef.needName} is critical ({Needs.GetNeedValue(needDef.needName):F1}), looking for structure... Found: {structure?.Definition.structureName ?? "NONE"}");

        if (structure != null)
        {
            targetStructure = structure;
            ChangeState($"GoingTo_{structure.Definition.structureName}");
            Debug.Log($"{PersonName}: Target set to {structure.Definition.structureName} at grid {structure.GridPosition}");
        }
        else
        {
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
        if (Ideology.StructureManager.Instance == null)
        {
            // Fallback to old BuildingManager if StructureManager not available
            if (BuildingManager.Instance != null)
            {
                return BuildingManager.Instance.FindNearestBuildingForNeed(needName, Position);
            }
            return null;
        }

        return Ideology.StructureManager.Instance.FindStructureForNeed(needName);
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
            targetStructure.StartUsing(this);
            bool success = true;

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
        if (targetStructure == null)
        {
            isUsingBuilding = false;
            return;
        }

        buildingUseTimer += deltaTime;

        // Check if this is a work building or work station
        bool isWorkStructure = false;
        string satisfiesNeed = null;
        float needRestoreAmount = 0f;
        float useDuration = 0f;
        List<SkillContribution> productionSkills = null;
        if (targetStructure is Building building)
        {
            productionSkills = building.Definition.productionSkills != null
                ? new List<SkillContribution>(building.Definition.productionSkills)
                : null;
        }
        else if (targetStructure is InteriorStructure interiorStructure)
        {
            isWorkStructure = interiorStructure.Definition.isWorkStation;
            satisfiesNeed = interiorStructure.Definition.satisfiesNeed;
            needRestoreAmount = interiorStructure.Definition.needRestoreAmount;
            useDuration = interiorStructure.Definition.useDuration;
            // TODO: Add production skills to InteriorStructureDefinition
        }

        if (isWorkStructure)
        {
            // Working - set state
            isWorking = true;
            isResting = false;

            // Track work time
            workTimer += deltaTime;

            // Improve relevant skills while working
            if (productionSkills != null)
            {
                foreach (var skillContrib in productionSkills)
                {
                    if (skillContrib.skill != null)
                    {
                        // Gain experience based on weight
                        float xpGain = deltaTime * 0.1f * skillContrib.weight;
                        Skills.AddExperience(skillContrib.skill.skillName, xpGain);
                    }
                }
            }

            // Take a break after work session
            if (workTimer >= workSessionDuration)
            {
                workTimer = 0f;
                FinishUsingBuilding();
                ChangeState("TakingBreak");
                return;
            }
        }
        else
        {
            // For food/water buildings, consume actual items
            if (satisfiesNeed == "Hunger" || satisfiesNeed == "Thirst")
            {
                // Only consume once when entering
                if (buildingUseTimer < 0.1f)
                {
                    bool consumed = TryConsumeItem(satisfiesNeed);
                    if (!consumed)
                    {
                        Debug.LogWarning($"{PersonName} went to {targetStructure.Definition.structureName} but no items available!");
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
    /// Finish using the current structure.
    /// </summary>
    public void FinishUsingBuilding()
    {
        if (targetStructure != null)
        {
            targetStructure.StopUsing(this);
        }

        targetStructure = null;
        isUsingBuilding = false;
        buildingUseTimer = 0f;

        Debug.Log($"{PersonName} finished using building and is now idle");
    }

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
    /// Get assigned work building.
    /// </summary>
    public Building GetAssignedWork() => assignedWorkBuilding;

    /// <summary>
    /// Is this member currently assigned to work?
    /// </summary>
    public bool HasWorkAssignment() => assignedWorkBuilding != null;

    /// <summary>
    /// Get the current target structure (for MemberView to know where to go).
    /// </summary>
    public Structure GetTargetStructure() => targetStructure;

    /// <summary>
    /// Set the target structure for this member to use.
    /// </summary>
    public void SetTargetStructure(Structure structure)
    {
        targetStructure = structure;
    }

    /// <summary>
    /// Check if currently using a building.
    /// </summary>
    public bool IsUsingBuilding() => isUsingBuilding;

    public void AdjustIdeologyAlignment(float amount)
    {
        float oldAlignment = IdeologyAlignment;
        IdeologyAlignment = Mathf.Clamp(IdeologyAlignment + amount, 0f, 100f);

        if (Mathf.Abs(oldAlignment - IdeologyAlignment) > 0.1f)
        {
            GameEvents.TriggerBeliefChanged(this, "CommuneAlignment", IdeologyAlignment);
        }
    }

    public override string GetDescription()
    {
        return $"{base.GetDescription()}, Role: {Role}, Ideology: {IdeologyAlignment:F0}%";
    }

    /// <summary>
    /// Assign a role to this member.
    /// PUBLIC: Called by role assignment system (to be implemented).
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




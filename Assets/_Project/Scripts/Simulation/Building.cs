using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a building in the simulation (pure logic, no Unity dependencies).
/// WHY: Keeps simulation separate from presentation for save/load, testing.
/// </summary>
public class Building
{
    // Identity
    public string BuildingID { get; private set; }
    public BuildingDefinition Definition { get; private set; }

    // State
    public Vector3 WorldPosition { get; set; }
    public Vector2Int GridPosition { get; set; }
    public bool IsOperational { get; set; } = true;

    // Usage tracking
    private List<Member> currentUsers = new List<Member>();

    // Work assignment (for work buildings)
    private List<Member> assignedWorkers = new List<Member>();
    private float productionProgress = 0f;
    private float currentProductionQuality = 1f;

    /// <summary>
    /// Initialize building with definition.
    /// </summary>
    public Building(BuildingDefinition definition, Vector3 worldPos, Vector2Int gridPos)
    {
        BuildingID = System.Guid.NewGuid().ToString();
        Definition = definition;
        WorldPosition = worldPos;
        GridPosition = gridPos;
    }

    /// <summary>
    /// Can this building accept another user?
    /// </summary>
    public bool HasCapacity()
    {
        return currentUsers.Count < Definition.capacity;
    }

    /// <summary>
    /// Member begins using this building.
    /// RETURNS: True if successfully started using, false if at capacity.
    /// </summary>
    public bool StartUsing(Member member)
    {
        if (!HasCapacity())
        {
            Debug.LogWarning($"{Definition.buildingName}: At capacity, cannot accept {member.PersonName}");
            return false;
        }

        if (!currentUsers.Contains(member))
        {
            currentUsers.Add(member);
            Debug.Log($"{member.PersonName} started using {Definition.buildingName}. Current users: {currentUsers.Count}/{Definition.capacity}");
        }

        return true; // FIXED: was "return false"
    }


    /// <summary>
    /// Member stops using this building.
    /// </summary>
    public void StopUsing(Member member)
    {
        if (currentUsers.Contains(member))
        {
            currentUsers.Remove(member);
            Debug.Log($"{member.PersonName} stopped using {Definition.buildingName}");
        }
        else
        {
            Debug.LogWarning($"{member.PersonName} tried to stop using {Definition.buildingName} but wasn't in the user list!");
        }
    }

    /// <summary>
    /// Get list of current users (for UI/debugging).
    /// </summary>
    public List<Member> GetCurrentUsers() => new List<Member>(currentUsers);

    /// <summary>
    /// Get the "use position" where members should stand when using this building.
    /// WHY: Building center is unwalkable, so find adjacent walkable cell.
    /// </summary>
    public Vector3 GetUsePosition()
    {
        if (GridSystem.Instance == null)
            return WorldPosition;

        // Try to find a walkable cell adjacent to the building
        Vector2Int[] offsets = new Vector2Int[]
        {
            new Vector2Int(0, -1),  // South (front)
            new Vector2Int(-1, 0),  // West
            new Vector2Int(1, 0),   // East
            new Vector2Int(0, 1),   // North (back)
            new Vector2Int(-1, -1), // Southwest
            new Vector2Int(1, -1),  // Southeast
            new Vector2Int(-1, 1),  // Northwest
            new Vector2Int(1, 1)    // Northeast
        };

        // Check each offset for a walkable cell
        foreach (var offset in offsets)
        {
            Vector2Int checkPos = GridPosition + offset;
            GridCell cell = GridSystem.Instance.GetCell(checkPos);

            if (cell != null && cell.IsWalkable && !cell.IsOccupied)
            {
                // Found a good spot!
                Debug.Log($"{Definition.buildingName}: Found entry point at {checkPos}");
                return cell.WorldPosition;
            }
        }

        // Fallback: Try cells further away (2 cells out)
        for (int radius = 2; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if (x == 0 && z == 0) continue;

                    Vector2Int checkPos = GridPosition + new Vector2Int(x, z);
                    GridCell cell = GridSystem.Instance.GetCell(checkPos);

                    if (cell != null && cell.IsWalkable && !cell.IsOccupied)
                    {
                        Debug.Log($"{Definition.buildingName}: Found distant entry point at {checkPos} (radius {radius})");
                        return cell.WorldPosition;
                    }
                }
            }
        }

        // Last resort: return world position
        Debug.LogWarning($"{Definition.buildingName}: Could not find walkable entry point! Using center position.");
        return WorldPosition;
    }

    // ===== WORK BUILDING FEATURES =====

    /// <summary>
    /// Assign a member to work at this building.
    /// RETURNS: True if successfully assigned.
    /// </summary>
    public bool AssignWorker(Member member)
    {
        if (!Definition.isWorkBuilding)
        {
            Debug.LogWarning($"{Definition.buildingName} is not a work building!");
            return false;
        }

        if (assignedWorkers.Count >= Definition.workerCapacity)
        {
            Debug.LogWarning($"{Definition.buildingName} is at worker capacity!");
            return false;
        }

        if (!assignedWorkers.Contains(member))
        {
            assignedWorkers.Add(member);
            Debug.Log($"{member.PersonName} assigned to work at {Definition.buildingName}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Remove a worker from this building.
    /// </summary>
    public void UnassignWorker(Member member)
    {
        if (assignedWorkers.Contains(member))
        {
            assignedWorkers.Remove(member);
            Debug.Log($"{member.PersonName} unassigned from {Definition.buildingName}");
        }
    }

    /// <summary>
    /// Get all assigned workers.
    /// </summary>
    public List<Member> GetAssignedWorkers() => new List<Member>(assignedWorkers);

    /// <summary>
    /// Get current number of workers actually working (at the building).
    /// </summary>
    public int GetActiveWorkerCount()
    {
        int count = 0;
        foreach (var worker in assignedWorkers)
        {
            if (currentUsers.Contains(worker))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Update production (called by BuildingManager every frame).
    /// WHY: Production happens over time based on active workers and their skills.
    /// </summary>
    public void UpdateProduction(float deltaTime)
    {
        if (!Definition.isWorkBuilding || Definition.producedItem == null)
            return;

        // Only produce if workers are present
        int activeWorkers = GetActiveWorkerCount();
        if (activeWorkers == 0)
            return;

        // Calculate production rate
        float baseRate = Definition.productionRate / 3600f; // Per second
        float workerMultiplier = activeWorkers; // Each worker contributes

        // Skill bonus from all active workers
        float totalSpeedBonus = 0f;
        float totalQualityBonus = 0f;
        int workerCount = 0;

        foreach (var worker in assignedWorkers)
        {
            if (currentUsers.Contains(worker))
            {
                var (speed, quality) = CalculateWorkerEffectiveness(worker);
                totalSpeedBonus += speed;
                totalQualityBonus += quality;
                workerCount++;
            }
        }

        // Average the bonuses
        float avgSpeed = workerCount > 0 ? totalSpeedBonus / workerCount : 1f;
        float avgQuality = workerCount > 0 ? totalQualityBonus / workerCount : 1f;

        // Calculate production this frame
        float production = baseRate * workerMultiplier * avgSpeed * deltaTime;
        productionProgress += production;

        // Store quality for when item is produced
        currentProductionQuality = avgQuality;

        // Produce items when progress >= 1
        while (productionProgress >= 1f)
        {
            ProduceItem(currentProductionQuality);
            productionProgress -= 1f;
        }
    }

    /// <summary>
    /// Calculate how effective a worker is at this building.
    /// RETURNS: (speedMultiplier, qualityMultiplier)
    /// </summary>
    private (float speed, float quality) CalculateWorkerEffectiveness(Member worker)
    {
        if (Definition.productionSkills == null || Definition.productionSkills.Length == 0)
            return (1f, 1f);

        float speed = SkillCalculator.CalculateSpeedMultiplier(
            worker,
            Definition.productionSkills,
            Definition.skillCombineMode
        );

        float quality = SkillCalculator.CalculateQualityMultiplier(
            worker,
            Definition.productionSkills,
            Definition.skillCombineMode
        );

        return (speed, quality);
    }

    /// <summary>
    /// Produce one unit of the item with quality modifier.
    /// WHY: Quality affects how much is produced.
    /// </summary>
    private void ProduceItem(float quality)
    {
        if (CommuneInventoryManager.Instance != null && Definition.producedItem != null)
        {
            int baseAmount = 1;
            float bonusChance = quality - 1f;

            if (bonusChance > 0f)
            {
                if (Random.value < bonusChance)
                {
                    baseAmount++;
                }
            }

            CommuneInventoryManager.Instance.AddToCommuneInventory(Definition.producedItem, baseAmount);

            // Enhanced debug output
            string workerInfo = "";
            foreach (var worker in assignedWorkers)
            {
                if (currentUsers.Contains(worker))
                {
                    var (speed, qual) = CalculateWorkerEffectiveness(worker);
                    int farming = worker.Skills.GetSkillLevel("Farming");
                    int strength = worker.Skills.GetSkillLevel("Strength");
                    int construction = worker.Skills.GetSkillLevel("Construction");

                    workerInfo += $"\n  - {worker.PersonName}: Farm={farming}, Str={strength}, Const={construction} → Speed={speed:F2}x, Quality={qual:F2}x";
                }
            }

            if (baseAmount > 1)
            {
                Debug.Log($"🌟 {Definition.buildingName} produced {baseAmount}x {Definition.producedItem.itemName} (HIGH QUALITY: {quality:F2}){workerInfo}");
            }
            else
            {
                Debug.Log($"{Definition.buildingName} produced {baseAmount}x {Definition.producedItem.itemName} (quality: {quality:F2}){workerInfo}");
            }
        }
    }

    /// <summary>
    /// Get production progress (0-1) for UI display.
    /// </summary>
    public float GetProductionProgress() => productionProgress;

    /// <summary>
    /// Get current production quality for UI display.
    /// </summary>
    public float GetProductionQuality() => currentProductionQuality;

    /// <summary>
    /// Get estimated production per hour (for UI).
    /// </summary>
    public float GetEstimatedProductionRate()
    {
        if (!Definition.isWorkBuilding) return 0f;

        int activeWorkers = GetActiveWorkerCount();
        if (activeWorkers == 0) return 0f;

        // Calculate average speed bonus
        float totalSpeed = 0f;
        int count = 0;

        foreach (var worker in assignedWorkers)
        {
            if (currentUsers.Contains(worker))
            {
                var (speed, _) = CalculateWorkerEffectiveness(worker);
                totalSpeed += speed;
                count++;
            }
        }

        float avgSpeed = count > 0 ? totalSpeed / count : 1f;

        return Definition.productionRate * activeWorkers * avgSpeed;
    }
}
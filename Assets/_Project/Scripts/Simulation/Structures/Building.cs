using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Ideology.Structures
{
    public class Building : Structure
    {
        public new BuildingDefinition Definition => base.Definition as BuildingDefinition;

        private Dictionary<InteriorStructureSlot, InteriorStructure> placedInteriorStructures
            = new Dictionary<InteriorStructureSlot, InteriorStructure>();

        // Worker management
        private List<Member> assignedWorkers = new List<Member>();
        private float productionProgress = 0f;

        // Track occupied cells for this building
        private List<Vector2Int> occupiedCells;

        public Building(BuildingDefinition definition, Vector2Int gridPos, Vector3 worldPos, GameObject gameObject)
            : base(definition, gridPos, worldPos, gameObject)
        {
            // Calculate occupied cells based on building size
            occupiedCells = new List<Vector2Int>();
            for (int x = 0; x < definition.size.x; x++)
            {
                for (int y = 0; y < definition.size.y; y++)
                {
                    occupiedCells.Add(gridPos + new Vector2Int(x, y));
                }
            }
        }

        protected override List<Vector2Int> GetOccupiedCells()
        {
            return occupiedCells;
        }

        /// <summary>
        /// Place an interior structure in this building
        /// </summary>
        public bool TryPlaceInteriorStructure(InteriorStructureSlot slot, InteriorStructureDefinition structureDef)
        {
            if (!Definition.canContainInteriorStructures)
            {
                Debug.LogWarning($"{Definition.structureName} cannot contain interior structures!");
                return false;
            }

            if (!slot.playerCanModify && slot.fixedStructure != null)
            {
                Debug.LogWarning($"Slot {slot.slotName} is fixed and cannot be modified!");
                return false;
            }

            if (!slot.allowedCategories.Contains(structureDef.category))
            {
                Debug.LogWarning($"Slot {slot.slotName} doesn't allow {structureDef.category} structures!");
                return false;
            }

            // Calculate world position for interior structure
            Vector2Int interiorGridPos = GridPosition + slot.localPosition;
            Vector3 interiorWorldPos = GridSystem.Instance.GridToWorld(interiorGridPos);

            // Instantiate prefab
            GameObject interiorGO = GameObject.Instantiate(structureDef.prefab, interiorWorldPos, Quaternion.identity);
            interiorGO.transform.SetParent(GameObject.transform);

            // Create runtime instance
            InteriorStructure interiorStructure = new InteriorStructure(
                structureDef,
                interiorGridPos,
                interiorWorldPos,
                interiorGO,
                this
            );

            placedInteriorStructures[slot] = interiorStructure;

            Debug.Log($"Placed {structureDef.structureName} in {Definition.structureName} at slot {slot.slotName}");
            return true;
        }

        /// <summary>
        /// Get all interior structures in this building
        /// </summary>
        public List<InteriorStructure> GetInteriorStructures()
        {
            return new List<InteriorStructure>(placedInteriorStructures.Values);
        }

        /// <summary>
        /// Find an interior structure that satisfies a specific need
        /// </summary>
        public InteriorStructure FindInteriorStructureForNeed(string needName)
        {
            foreach (var structure in placedInteriorStructures.Values)
            {
                if (structure.Definition.satisfiesNeed == needName && structure.CanUse(null))
                {
                    return structure;
                }
            }
            return null;
        }

        /// <summary>
        /// Find an interior structure that supports a specific work type
        /// </summary>
        public InteriorStructure FindWorkStation(WorkType workType)
        {
            foreach (var structure in placedInteriorStructures.Values)
            {
                if (structure.Definition.isWorkStation &&
                    structure.Definition.supportedWorkTypes.Contains(workType) &&
                    structure.CanUse(null))
                {
                    return structure;
                }
            }
            return null;
        }

        // Worker management methods
        public bool AssignWorker(Member member)
        {
            if (!Definition.isWorkBuilding)
            {
                Debug.LogWarning($"{Definition.structureName} is not a work building!");
                return false;
            }

            if (assignedWorkers.Count >= Definition.workerCapacity)
            {
                Debug.LogWarning($"{Definition.structureName} is at worker capacity!");
                return false;
            }

            if (!assignedWorkers.Contains(member))
            {
                assignedWorkers.Add(member);
                Debug.Log($"{member.PersonName} assigned to work at {Definition.structureName}");
                return true;
            }

            return false;
        }

        public void UnassignWorker(Member member)
        {
            if (assignedWorkers.Contains(member))
            {
                assignedWorkers.Remove(member);
                Debug.Log($"{member.PersonName} unassigned from {Definition.structureName}");
            }
        }

        public List<Member> GetAssignedWorkers() => new List<Member>(assignedWorkers);

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

        public void UpdateProduction(float deltaTime)
        {
            if (!Definition.isWorkBuilding || Definition.producedItem == null)
                return;

            int activeWorkers = currentUsers.Count(user => assignedWorkers.Contains(user));
            if (activeWorkers > 0)
            {
                productionProgress += deltaTime * Definition.productionRate * activeWorkers;

                if (productionProgress >= 60f)
                {
                    productionProgress = 0f;
                    Debug.Log($"{Definition.structureName} produced {Definition.producedItem.itemName}");
                }
            }
        }

        public bool IsOperational => Definition.IsOperational;
        public bool HasCapacity() => currentUsers.Count < Definition.maxOccupants;
    }
}
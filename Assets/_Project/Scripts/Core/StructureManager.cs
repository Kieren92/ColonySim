using UnityEngine;
using System.Collections.Generic;
using ColonySim.Structures;

namespace ColonySim
{
    /// <summary>
    /// Manages all placed structures (buildings and interior structures)
    /// Replaces the old BuildingManager for structure-specific logic
    /// </summary>
    public class StructureManager : MonoBehaviour
    {
        public static StructureManager Instance { get; private set; }

        [Header("Placed Structures")]
        private List<Building> placedBuildings = new List<Building>();
        private List<InteriorStructure> placedInteriorStructures = new List<InteriorStructure>();

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

        /// <summary>
        /// Register a newly placed building
        /// </summary>
        public void RegisterBuilding(Building building)
        {
            if (!placedBuildings.Contains(building))
            {
                placedBuildings.Add(building);
                Debug.Log($"Registered building: {building.Definition.structureName}");
            }
        }

        /// <summary>
        /// Register a newly placed interior structure
        /// </summary>
        public void RegisterInteriorStructure(InteriorStructure structure)
        {
            if (!placedInteriorStructures.Contains(structure))
            {
                placedInteriorStructures.Add(structure);
                Debug.Log($"Registered interior structure: {structure.Definition.structureName}");
            }
        }

        /// <summary>
        /// Find a building or interior structure that satisfies a specific need
        /// </summary>
        public Structure FindStructureForNeed(string needName)
        {
            // First check interior structures
            foreach (var interiorStructure in placedInteriorStructures)
            {
                if (interiorStructure.Definition.satisfiesNeed == needName &&
                    interiorStructure.CanUse(null))
                {
                    return interiorStructure;
                }
            }

            // Then check standalone buildings
            foreach (var building in placedBuildings)
            {
                // Check if building has interior structures that satisfy the need
                var interiorStructure = building.FindInteriorStructureForNeed(needName);
                if (interiorStructure != null)
                {
                    return interiorStructure;
                }

                // Check if building itself satisfies the need
                if (building.Definition.satisfiesNeed == needName &&
                    building.CanUse(null))
                {
                    return building;
                }
            }

            return null;
        }

        /// <summary>
        /// Find a work station (building or interior structure) that supports a work type
        /// </summary>
        public Structure FindWorkStation(WorkType workType)
        {
            // Check interior structures first (more specific)
            foreach (var interiorStructure in placedInteriorStructures)
            {
                if (interiorStructure.Definition.isWorkStation &&
                    interiorStructure.Definition.supportedWorkTypes.Contains(workType) &&
                    interiorStructure.CanUse(null))
                {
                    return interiorStructure;
                }
            }

            // Check buildings
            foreach (var building in placedBuildings)
            {
                // Check building's interior structures
                var workStation = building.FindWorkStation(workType);
                if (workStation != null)
                {
                    return workStation;
                }

                // Check if building itself is a work station
                if (building.Definition.isWorkBuilding &&
                    building.Definition.supportedWorkTypes.Contains(workType) &&
                    building.CanUse(null))
                {
                    return building;
                }
            }

            return null;
        }

        /// <summary>
        /// Get all buildings
        /// </summary>
        public List<Building> GetAllBuildings() => new List<Building>(placedBuildings);

        /// <summary>
        /// Get all interior structures
        /// </summary>
        public List<InteriorStructure> GetAllInteriorStructures() => new List<InteriorStructure>(placedInteriorStructures);

        /// <summary>
        /// Remove a building and all its interior structures
        /// </summary>
        public void RemoveBuilding(Building building)
        {
            // Remove all interior structures
            foreach (var interiorStructure in building.GetInteriorStructures())
            {
                placedInteriorStructures.Remove(interiorStructure);
                if (interiorStructure.GameObject != null)
                {
                    Destroy(interiorStructure.GameObject);
                }
            }

            placedBuildings.Remove(building);

            if (building.GameObject != null)
            {
                Destroy(building.GameObject);
            }

            Debug.Log($"Removed building: {building.Definition.structureName}");
        }
    }
}
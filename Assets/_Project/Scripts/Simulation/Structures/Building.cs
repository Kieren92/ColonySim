using UnityEngine;
using System.Collections.Generic;

namespace ColonySim.Structures
{
    public class Building : Structure
    {
        public new BuildingDefinition Definition => base.Definition as BuildingDefinition;

        private Dictionary<InteriorStructureSlot, InteriorStructure> placedInteriorStructures
            = new Dictionary<InteriorStructureSlot, InteriorStructure>();

        public Building(BuildingDefinition definition, Vector2Int gridPos, Vector3 worldPos, GameObject gameObject)
            : base(definition, gridPos, worldPos, gameObject)
        {
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
    }
}
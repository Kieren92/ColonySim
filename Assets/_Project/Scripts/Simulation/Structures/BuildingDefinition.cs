using UnityEngine;
using System.Collections.Generic;

namespace Ideology.Structures
{
    [CreateAssetMenu(fileName = "New Building", menuName = "Ideology/Structures/Building")]
    public class BuildingDefinition : StructureDefinition
    {
        [Header("Building Properties")]
        public BuildingType buildingType;
        public bool canContainInteriorStructures = false;
        public int maxOccupants = 1;

        [Header("Interior Structure Slots")]
        public List<InteriorStructureSlot> interiorSlots = new List<InteriorStructureSlot>();

        [Header("Construction")]
        public float buildTime = 5f;
        public List<ItemCost> buildCosts = new List<ItemCost>();

        [Header("Functionality - Need Satisfaction")]
        public string satisfiesNeed; // For simple buildings without interior structures
        public float needRestoreAmount;
        public float useDuration;

        [Header("Functionality - Work")]
        public bool isWorkBuilding = false;
        public List<WorkType> supportedWorkTypes = new List<WorkType>();
        public List<SkillRequirement> requiredSkills = new List<SkillRequirement>();

        [Header("Production (Work Buildings)")]
        public ItemDefinition producedItem;
        public float productionRate = 5f;
        public int workerCapacity = 2;
        public SkillContribution[] productionSkills;
        public SkillCombineMode skillCombineMode = SkillCombineMode.Additive;

        [Header("Visual")]
        public Color buildingColor = Color.gray;

        [Header("Operational")]
        public bool IsOperational = true; // Can be set to false if building is damaged/disabled
    }

    [System.Serializable]
    public class InteriorStructureSlot
    {
        public string slotName; // "Bedroom 1", "Kitchen Area", "Workshop Space"
        public Vector2Int localPosition; // Position relative to building origin
        public Vector2Int slotSize = Vector2Int.one; // Area available for structure
        public List<StructureCategory> allowedCategories; // What types can go here
        public bool isRequired = false; // Building can't function without this filled

        [Header("Restrictions")]
        public InteriorStructureDefinition fixedStructure; // Pre-placed, can't be removed
        public bool playerCanModify = true;
    }

    public enum BuildingType
    {
        Housing,
        Storage,
        Production,
        Recreation,
        Infrastructure,
        Defense
    }

    public enum StructureCategory
    {
        Furniture,
        Equipment,
        Appliance,
        Storage,
        Decoration
    }

    [System.Serializable]
    public class ItemCost
    {
        public ItemDefinition item;
        public int amount;
    }

    [System.Serializable]
    public class SkillRequirement
    {
        public string skillName;
        public int minimumLevel;
    }
}
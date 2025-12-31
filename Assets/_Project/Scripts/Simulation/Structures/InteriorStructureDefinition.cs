using UnityEngine;
using System.Collections.Generic;

namespace Ideology.Structures
{
    [CreateAssetMenu(fileName = "New Interior Structure", menuName = "Ideology/Structures/Interior Structure")]
    public class InteriorStructureDefinition : StructureDefinition
    {
        [Header("Interior Properties")]
        public StructureCategory category;
        public bool requiresBuilding = true; // Can't be placed in the open
        public List<BuildingType> allowedBuildingTypes = new List<BuildingType>();

        [Header("Functionality - Need Satisfaction")]
        public string satisfiesNeed;
        public float needRestoreAmount;
        public float useDuration;

        [Header("Functionality - Work")]
        public bool isWorkStation = false;
        public List<WorkType> supportedWorkTypes = new List<WorkType>();
        public List<SkillRequirement> requiredSkills = new List<SkillRequirement>();

        [Header("Production (if work station)")]
        public List<CraftingRecipe> recipes = new List<CraftingRecipe>();

        [Header("Construction")]
        public float buildTime = 2f;
        public List<ItemCost> buildCosts = new List<ItemCost>();
    }

    [System.Serializable]
    public class CraftingRecipe
    {
        public string recipeName;
        public List<ItemCost> ingredients;
        public ItemDefinition output;
        public int outputAmount = 1;
        public float craftTime = 10f;
        public string requiredSkill;
        public int minimumSkillLevel = 0;

        [Header("Ideology Tags (for future)")]
        public List<string> ideologyTags; // "violent", "wasteful", "luxurious", etc.
    }

    public enum WorkType
    {
        Crafting,
        Cooking,
        Research,
        Medicine,
        Farming,
        Construction,
        Art,
        Engineering
    }
}
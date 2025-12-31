using UnityEngine;

/// <summary>
/// Defines a building type that can be placed in the commune.
/// WHY ScriptableObject: Designers can create new building types without code.
/// </summary>
[CreateAssetMenu(fileName = "New Building", menuName = "Ideology/Buildings/Building Definition")]
public class BuildingDefinition : ScriptableObject
{
    [Header("Identity")]
    public string buildingName = "Food Storage";

    [TextArea(2, 4)]
    public string description = "Stores food for the commune.";

    [Header("Functionality")]
    [Tooltip("What need does this building satisfy?")]
    public string satisfiesNeed = "Hunger"; // Hunger, Energy, Social, etc.

    [Tooltip("How much need value is restored when used")]
    [Range(0f, 100f)]
    public float needRestoreAmount = 50f;

    [Tooltip("How long does it take to use this building (seconds)")]
    public float useDuration = 5f;

    [Header("Capacity")]
    [Tooltip("How many members can use this building simultaneously")]
    [Range(1, 20)]
    public int capacity = 1;

    [Header("Production (Work Buildings)")]
    [Tooltip("Is this a work building that produces resources?")]
    public bool isWorkBuilding = false;

    [Tooltip("What item does this building produce?")]
    public ItemDefinition producedItem;

    [Tooltip("Base production rate (items per in-game hour)")]
    public float productionRate = 5f;

    [Tooltip("How many workers can work here simultaneously?")]
    [Range(1, 10)]
    public int workerCapacity = 2;

    [Tooltip("Which skills affect production in this building?")]
    public SkillContribution[] productionSkills;

    [Tooltip("How do multiple skills combine for production?")]
    public SkillCombineMode skillCombineMode = SkillCombineMode.Additive;

    [Header("Placement")]
    [Tooltip("How much grid space does this building occupy?")]
    public Vector2Int size = new Vector2Int(2, 2); // Width x Height in grid cells

    [Tooltip("Prefab to instantiate when building is placed")]
    public GameObject prefab;

    [Header("Cost (future use)")]
    public int woodCost = 10;
    public int laborCost = 5;

    [Header("Visual")]
    public Color buildingColor = Color.gray;
    public Sprite icon;
}
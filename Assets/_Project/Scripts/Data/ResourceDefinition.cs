using UnityEngine;

/// <summary>
/// Ownership model determines how items can be held.
/// WHY: Core to ideology mechanics - who owns what?
/// </summary>
public enum OwnershipModel
{
    Communal,      // Must be shared (default in communist commune)
    Personal,      // Can be personally owned
    Tool,          // Tools - may require commune permission
    Contraband     // Banned by ideology
}

/// <summary>
/// Item category for organization and rules.
/// </summary>
public enum ItemCategory
{
    Resource,      // Food, water, materials
    Tool,          // Implements for work
    Personal,      // Clothing, mementos
    Luxury,        // Non-essential comfort items
    Contraband     // Explicitly banned
}

/// <summary>
/// Defines an item type that can exist in inventories.
/// WHY: Flexible system that supports resources AND personal possessions.
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Ideology/Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "Food";

    [TextArea(2, 4)]
    public string description = "Essential for survival.";

    [Header("Ownership")]
    [Tooltip("How can this item be owned?")]
    public OwnershipModel ownershipModel = OwnershipModel.Communal;

    [Tooltip("Item category")]
    public ItemCategory category = ItemCategory.Resource;

    [Header("Properties")]
    [Tooltip("Can this item stack (multiple in one slot)?")]
    public bool isStackable = true;

    [Tooltip("Maximum stack size (0 = unlimited)")]
    public int maxStackSize = 100;

    [Tooltip("Does this item decay over time?")]
    public bool canDecay = false;

    [Tooltip("Decay rate (0-1 per in-game day)")]
    [Range(0f, 1f)]
    public float decayRate = 0.02f;

    [Tooltip("Weight per item (for carrying capacity, future use)")]
    public float weight = 1f;

    [Header("Consumption")]
    [Tooltip("Can this item be consumed/used?")]
    public bool isConsumable = false;

    [Tooltip("What need does consuming this satisfy?")]
    public string satisfiesNeed = "";

    [Tooltip("How much need is restored when consumed")]
    [Range(0f, 100f)]
    public float needRestoreAmount = 0f;

    [Header("Ideology Impact")]
    [Tooltip("Ideological significance of owning this item")]
    [Range(-10f, 10f)]
    public float ideologyImpact = 0f; // Positive = supports ideology, Negative = contradicts

    [Header("Visual")]
    public Color itemColor = Color.white;
    public Sprite icon;
}
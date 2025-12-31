using UnityEngine;

/// <summary>
/// Represents a quantity of an item type.
/// WHY: Items can stack (e.g., 50 food) or be unique (e.g., 1 guitar).
/// </summary>
[System.Serializable]
public class ItemStack
{
    public ItemDefinition definition;
    public int quantity;

    // Optional: individual item properties (for non-stackables)
    public float condition = 100f; // Durability, freshness, etc.
    public string customName = ""; // "Bob's Guitar"

    public ItemStack(ItemDefinition def, int qty = 1)
    {
        definition = def;
        quantity = qty;
        condition = 100f;
    }

    /// <summary>
    /// Can items be added to this stack?
    /// </summary>
    public bool CanStack(ItemDefinition otherDef)
    {
        if (!definition.isStackable) return false;
        if (definition != otherDef) return false;
        if (definition.maxStackSize > 0 && quantity >= definition.maxStackSize) return false;

        return true;
    }

    /// <summary>
    /// Try to add items to this stack. Returns amount actually added.
    /// </summary>
    public int Add(int amount)
    {
        if (!definition.isStackable && quantity > 0) return 0;

        int spaceAvailable = definition.maxStackSize > 0
            ? definition.maxStackSize - quantity
            : int.MaxValue;

        int actualAmount = Mathf.Min(amount, spaceAvailable);
        quantity += actualAmount;

        return actualAmount;
    }

    /// <summary>
    /// Try to remove items from this stack. Returns amount actually removed.
    /// </summary>
    public int Remove(int amount)
    {
        int actualAmount = Mathf.Min(amount, quantity);
        quantity -= actualAmount;

        return actualAmount;
    }

    /// <summary>
    /// Update item condition (decay, damage).
    /// </summary>
    public void UpdateCondition(float deltaTime)
    {
        if (definition.canDecay)
        {
            // Decay per in-game day
            float decayAmount = definition.decayRate * (deltaTime / 86400f) * 100f;
            condition = Mathf.Max(0, condition - decayAmount);

            // If completely decayed, mark for removal
            if (condition <= 0)
            {
                quantity = 0;
            }
        }
    }

    public bool IsEmpty() => quantity <= 0;
    public bool IsFull() => definition.maxStackSize > 0 && quantity >= definition.maxStackSize;
}
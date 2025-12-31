using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generic inventory that can hold items.
/// Used by both commune (shared) and individuals (personal).
/// WHY: Same structure for both, different rules applied at usage.
/// </summary>
public class Inventory
{
    public string InventoryName { get; private set; }
    public int Capacity { get; private set; } // Slot capacity (0 = unlimited)

    private List<ItemStack> items = new List<ItemStack>();

    public Inventory(string name, int capacity = 0)
    {
        InventoryName = name;
        Capacity = capacity;
    }

    /// <summary>
    /// Try to add items to inventory. Returns amount actually added.
    /// </summary>
    public int AddItem(ItemDefinition itemDef, int quantity)
    {
        if (itemDef == null || quantity <= 0) return 0;

        int remaining = quantity;

        // Try to stack with existing items first
        if (itemDef.isStackable)
        {
            foreach (var stack in items)
            {
                if (stack.CanStack(itemDef))
                {
                    int added = stack.Add(remaining);
                    remaining -= added;

                    if (remaining <= 0) break;
                }
            }
        }

        // Create new stacks if needed
        while (remaining > 0)
        {
            // Check capacity
            if (Capacity > 0 && items.Count >= Capacity)
            {
                Debug.LogWarning($"{InventoryName}: Inventory full, cannot add more items");
                break;
            }

            // Create new stack
            int stackAmount = itemDef.maxStackSize > 0
                ? Mathf.Min(remaining, itemDef.maxStackSize)
                : remaining;

            ItemStack newStack = new ItemStack(itemDef, stackAmount);
            items.Add(newStack);

            remaining -= stackAmount;
        }

        int actuallyAdded = quantity - remaining;

        if (actuallyAdded > 0)
        {
            Debug.Log($"{InventoryName}: Added {actuallyAdded}x {itemDef.itemName}");
        }

        return actuallyAdded;
    }

    /// <summary>
    /// Try to remove items from inventory. Returns amount actually removed.
    /// </summary>
    public int RemoveItem(string itemName, int quantity)
    {
        int remaining = quantity;

        // Remove from stacks (oldest first)
        for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            ItemStack stack = items[i];

            if (stack.definition.itemName == itemName)
            {
                int removed = stack.Remove(remaining);
                remaining -= removed;

                // Remove empty stacks
                if (stack.IsEmpty())
                {
                    items.RemoveAt(i);
                }
            }
        }

        int actuallyRemoved = quantity - remaining;

        if (actuallyRemoved > 0)
        {
            Debug.Log($"{InventoryName}: Removed {actuallyRemoved}x {itemName}");
        }

        return actuallyRemoved;
    }

    /// <summary>
    /// Check if inventory has at least this many items.
    /// </summary>
    public bool HasItem(string itemName, int quantity)
    {
        int total = GetItemCount(itemName);
        return total >= quantity;
    }

    /// <summary>
    /// Get total count of an item across all stacks.
    /// </summary>
    public int GetItemCount(string itemName)
    {
        int total = 0;

        foreach (var stack in items)
        {
            if (stack.definition.itemName == itemName)
            {
                total += stack.quantity;
            }
        }

        return total;
    }

    /// <summary>
    /// Get all items (for UI display).
    /// </summary>
    public List<ItemStack> GetAllItems() => new List<ItemStack>(items);

    /// <summary>
    /// Get all items of a specific category.
    /// </summary>
    public List<ItemStack> GetItemsByCategory(ItemCategory category)
    {
        return items.Where(stack => stack.definition.category == category).ToList();
    }

    /// <summary>
    /// Update all items (decay, condition).
    /// </summary>
    public void UpdateItems(float deltaTime)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            items[i].UpdateCondition(deltaTime);

            // Remove empty stacks
            if (items[i].IsEmpty())
            {
                Debug.Log($"{InventoryName}: {items[i].definition.itemName} decayed completely");
                items.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Transfer items to another inventory.
    /// RETURNS: Amount actually transferred.
    /// </summary>
    public int TransferTo(Inventory targetInventory, string itemName, int quantity)
    {
        // Check if we have the items
        if (!HasItem(itemName, quantity)) return 0;

        // Find the item definition
        ItemStack sourceStack = items.Find(s => s.definition.itemName == itemName);
        if (sourceStack == null) return 0;

        // Remove from this inventory
        int removed = RemoveItem(itemName, quantity);

        // Add to target inventory
        int added = targetInventory.AddItem(sourceStack.definition, removed);

        // If target couldn't take all, return the rest
        if (added < removed)
        {
            int returned = removed - added;
            AddItem(sourceStack.definition, returned);
        }

        return added;
    }

    /// <summary>
    /// Get total number of slots used.
    /// </summary>
    public int GetUsedSlots() => items.Count;

    /// <summary>
    /// Is inventory full?
    /// </summary>
    public bool IsFull() => Capacity > 0 && items.Count >= Capacity;

    /// <summary>
    /// Is inventory empty?
    /// </summary>
    public bool IsEmpty() => items.Count == 0;
}
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the commune's shared inventory and ownership rules.
/// WHY: Central authority for shared resources and ideology enforcement.
/// </summary>
public class CommuneInventoryManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("All item types available in the game")]
    [SerializeField] private List<ItemDefinition> availableItems;

    [Header("Starting Inventory")]
    [Tooltip("Items the commune starts with")]
    [SerializeField] private List<StartingItem> startingItems;

    [Header("Ownership Rules (Ideology)")]
    [Tooltip("Must members share all resources with commune?")]
    [SerializeField] private bool enforceResourceSharing = true;

    [Tooltip("Can members own tools personally?")]
    [SerializeField] private bool allowPersonalTools = false;

    [Tooltip("Can members own luxury items?")]
    [SerializeField] private bool allowLuxuryItems = true;

    [Tooltip("Are there banned/contraband items?")]
    [SerializeField] private bool enforceContraband = false;

    [Header("Time")]
    [SerializeField] private float secondsPerDay = 300f; // 5 minutes = 1 day

    // The commune's shared inventory
    private Inventory communeInventory;

    // Singleton
    public static CommuneInventoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeInventory();
    }

    private void Update()
    {
        // Update commune inventory (decay, etc)
        if (communeInventory != null)
        {
            float inGameDeltaTime = Time.deltaTime * (86400f / secondsPerDay);
            communeInventory.UpdateItems(inGameDeltaTime);
        }
    }

    /// <summary>
    /// Initialize commune inventory with starting items.
    /// </summary>
    private void InitializeInventory()
    {
        communeInventory = new Inventory("Commune Storage", capacity: 0); // Unlimited capacity

        // Add starting items
        if (startingItems != null)
        {
            foreach (var startingItem in startingItems)
            {
                if (startingItem.item != null)
                {
                    communeInventory.AddItem(startingItem.item, startingItem.quantity);
                }
            }
        }

        Debug.Log($"Commune inventory initialized with {startingItems?.Count ?? 0} item types");
    }

    // ===== PUBLIC API - Commune Inventory Access =====

    /// <summary>
    /// Add items to commune inventory.
    /// </summary>
    public int AddToCommuneInventory(ItemDefinition item, int quantity)
    {
        return communeInventory?.AddItem(item, quantity) ?? 0;
    }

    /// <summary>
    /// Remove items from commune inventory.
    /// </summary>
    public int RemoveFromCommuneInventory(string itemName, int quantity)
    {
        return communeInventory?.RemoveItem(itemName, quantity) ?? 0;
    }

    /// <summary>
    /// Check if commune has items.
    /// </summary>
    public bool CommuneHasItem(string itemName, int quantity)
    {
        return communeInventory?.HasItem(itemName, quantity) ?? false;
    }

    /// <summary>
    /// Get commune item count.
    /// </summary>
    public int GetCommuneItemCount(string itemName)
    {
        return communeInventory?.GetItemCount(itemName) ?? 0;
    }

    /// <summary>
    /// Get all commune items.
    /// </summary>
    public List<ItemStack> GetCommuneInventory()
    {
        return communeInventory?.GetAllItems() ?? new List<ItemStack>();
    }

    // ===== OWNERSHIP RULES (Ideology Enforcement) =====

    /// <summary>
    /// Check if a member is allowed to personally own an item type.
    /// WHY: Core ideology mechanic - what can individuals own?
    /// </summary>
    public bool CanMemberOwn(ItemDefinition item)
    {
        switch (item.ownershipModel)
        {
            case OwnershipModel.Communal:
                // Resources must be shared if enforcing
                if (item.category == ItemCategory.Resource && enforceResourceSharing)
                    return false;
                return true;

            case OwnershipModel.Personal:
                // Personal items always allowed
                return true;

            case OwnershipModel.Tool:
                // Tools only if ideology allows
                return allowPersonalTools;

            case OwnershipModel.Contraband:
                // Never allowed if enforcing
                return !enforceContraband;

            default:
                return true;
        }
    }

    /// <summary>
    /// Member tries to take item from commune.
    /// RETURNS: True if allowed and transferred.
    /// </summary>
    public bool MemberTakeFromCommune(Member member, string itemName, int quantity)
    {
        if (member == null)
        {
            Debug.LogWarning("Cannot take item: member is null");
            return false;
        }

        // TEMPORARILY DISABLE IDEOLOGY CHECK FOR TESTING
        /*
        if (currentIdeology != null && !currentIdeology.allowsPersonalOwnership)
        {
            Debug.LogWarning($"{member.PersonName} not allowed to take {itemName} (ideology restriction)");
            return false;
        }
        */

        // Get the item definition
        ItemDefinition itemDef = null;
        foreach (var startingItem in startingItems)
        {
            if (startingItem.item != null && startingItem.item.itemName == itemName)
            {
                itemDef = startingItem.item;
                break;
            }
        }

        if (itemDef == null)
        {
            Debug.LogWarning($"Item definition not found for {itemName}");
            return false;
        }

        // Check if item exists in commune storage
        if (!communeInventory.HasItem(itemName, quantity))
        {
            Debug.LogWarning($"Commune storage doesn't have {quantity}x {itemName}");
            return false;
        }

        // Remove from commune
        communeInventory.RemoveItem(itemName, quantity);

        // Add to member's personal inventory
        member.PersonalInventory.AddItem(itemDef, quantity);

        Debug.Log($"{member.PersonName} took {quantity}x {itemName} from commune storage");
        return true;
    }

    /// <summary>
    /// Member contributes item to commune.
    /// </summary>
    public bool MemberContributeToCommune(Member member, string itemName, int quantity)
    {
        // Check if member has the item
        if (!member.PersonalInventory.HasItem(itemName, quantity))
        {
            Debug.LogWarning($"{member.PersonName} doesn't have {quantity}x {itemName}");
            return false;
        }

        // Transfer from member to commune
        int transferred = member.PersonalInventory.TransferTo(communeInventory, itemName, quantity);

        if (transferred > 0)
        {
            Debug.Log($"{member.PersonName} contributed {transferred}x {itemName} to commune");

            // Positive ideology impact for sharing (if collectivist)
            if (enforceResourceSharing)
            {
                member.AdjustIdeologyAlignment(1f); // Small boost for sharing
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Enforce ideology - scan member inventories for violations.
    /// Called periodically (e.g., daily) to enforce rules.
    /// </summary>
    public void EnforceOwnershipRules()
    {
        if (SimulationManager.Instance == null) return;

        var members = SimulationManager.Instance.GetAllMembers();

        foreach (var member in members)
        {
            var personalItems = member.PersonalInventory.GetAllItems();

            foreach (var stack in personalItems.ToArray()) // ToArray to avoid modification during iteration
            {
                if (!CanMemberOwn(stack.definition))
                {
                    // Confiscate item
                    int confiscated = member.PersonalInventory.TransferTo(
                        communeInventory,
                        stack.definition.itemName,
                        stack.quantity
                    );

                    if (confiscated > 0)
                    {
                        Debug.Log($"Confiscated {confiscated}x {stack.definition.itemName} from {member.PersonName}");

                        // Ideology impact - member resents confiscation
                        member.AdjustIdeologyAlignment(-2f);

                        // Story event could be triggered here
                        // GameEvents.TriggerItemConfiscated(member, stack.definition, confiscated);
                    }
                }
            }
        }
    }

    // ===== IDEOLOGY SETTINGS (Can be changed at runtime) =====

    public void SetResourceSharing(bool enforce)
    {
        enforceResourceSharing = enforce;
        Debug.Log($"Resource sharing now {(enforce ? "enforced" : "optional")}");
    }

    public void SetPersonalTools(bool allow)
    {
        allowPersonalTools = allow;
        Debug.Log($"Personal tools now {(allow ? "allowed" : "forbidden")}");
    }

    public void SetLuxuryItems(bool allow)
    {
        allowLuxuryItems = allow;
        Debug.Log($"Luxury items now {(allow ? "allowed" : "forbidden")}");
    }
}

/// <summary>
/// Helper class for starting inventory configuration.
/// </summary>
[System.Serializable]
public class StartingItem
{
    public ItemDefinition item;
    public int quantity;
}
using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays commune and member inventories.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Transform communeInventoryContainer;
    [SerializeField] private Transform selectedMemberContainer;

    private Dictionary<string, TextMeshProUGUI> communeLabels = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, TextMeshProUGUI> memberLabels = new Dictionary<string, TextMeshProUGUI>();

    private Member selectedMember;

    private void Start()
    {
        // Will create labels dynamically as items appear
    }

    private void Update()
    {

        UpdateCommuneInventoryDisplay();
        UpdateSelectedMemberDisplay();
    }

    /// <summary>
    /// Display commune inventory.
    /// </summary>
    private void UpdateCommuneInventoryDisplay()
    {
        if (CommuneInventoryManager.Instance == null) return;

        var communeItems = CommuneInventoryManager.Instance.GetCommuneInventory();

        // Update existing labels or create new ones
        foreach (var stack in communeItems)
        {
            string itemName = stack.definition.itemName;

            if (!communeLabels.ContainsKey(itemName))
            {
                // Create label
                GameObject labelObj = new GameObject($"Commune_{itemName}");
                labelObj.transform.SetParent(communeInventoryContainer != null ? communeInventoryContainer : transform);

                TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
                label.fontSize = 20;
                label.color = stack.definition.itemColor;

                communeLabels[itemName] = label;

                LayoutCommuneLabels();
            }

            // Update text
            communeLabels[itemName].text = $"{itemName}: {stack.quantity}";
        }

        // Remove labels for items that no longer exist
        var keysToRemove = new List<string>();
        foreach (var kvp in communeLabels)
        {
            bool exists = communeItems.Exists(s => s.definition.itemName == kvp.Key);
            if (!exists)
            {
                keysToRemove.Add(kvp.Key);
                Destroy(kvp.Value.gameObject);
            }
        }
        foreach (var key in keysToRemove)
        {
            communeLabels.Remove(key);
        }
    }

    /// <summary>
    /// Display selected member's inventory.
    /// </summary>
    private void UpdateSelectedMemberDisplay()
    {

        // Auto-select first member for now
        if (selectedMember == null && SimulationManager.Instance != null)
        {
            Debug.Log("Trying to find a member...");
            var members = SimulationManager.Instance.GetAllMembers();
            Debug.Log($"SimulationManager has {members.Count} members");

            if (members.Count > 0)
            {
                selectedMember = members[0];
                Debug.Log($"Selected member: {selectedMember.PersonName}");
            }
            else
            {
                Debug.LogWarning("No members found!");
            }
        }

        if (selectedMember == null)
        {
            Debug.LogWarning("selectedMember is still null!");
            return;
        }



        // Create title label
        if (!memberLabels.ContainsKey("_TITLE_"))
        {
            Debug.Log("Creating title label...");

            GameObject titleObj = new GameObject("MemberInventoryTitle");
            titleObj.transform.SetParent(selectedMemberContainer != null ? selectedMemberContainer : transform);

            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.fontSize = 24;
            title.color = Color.white;
            title.fontStyle = FontStyles.Bold;
            title.text = $"{selectedMember.PersonName}'s Inventory:";

            RectTransform rect = title.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-10, -10);
            rect.sizeDelta = new Vector2(200, 30);

            memberLabels["_TITLE_"] = title;

            Debug.Log($"Title created at position {rect.anchoredPosition}");
        }

        var memberItems = selectedMember.PersonalInventory.GetAllItems();


        // Update labels
        foreach (var stack in memberItems)
        {
            string itemName = stack.definition.itemName;

            if (!memberLabels.ContainsKey(itemName))
            {
                GameObject labelObj = new GameObject($"Member_{itemName}");
                labelObj.transform.SetParent(selectedMemberContainer != null ? selectedMemberContainer : transform);

                TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
                label.fontSize = 20;
                label.color = stack.definition.itemColor;

                memberLabels[itemName] = label;

                LayoutMemberLabels();
            }

            memberLabels[itemName].text = $"{itemName}: {stack.quantity}";
        }

        // Remove labels for items that no longer exist
        var keysToRemove = new List<string>();
        foreach (var kvp in memberLabels)
        {
            if (kvp.Key == "_TITLE_") continue; // Don't remove title

            bool exists = memberItems.Exists(s => s.definition.itemName == kvp.Key);
            if (!exists)
            {
                keysToRemove.Add(kvp.Key);
                Destroy(kvp.Value.gameObject);
            }
        }
        foreach (var key in keysToRemove)
        {
            memberLabels.Remove(key);
        }
    }

    private void LayoutCommuneLabels()
    {
        int index = 0;
        foreach (var kvp in communeLabels)
        {
            RectTransform rect = kvp.Value.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -10 - (index * 25));
            rect.sizeDelta = new Vector2(200, 25);

            index++;
        }
    }

    private void LayoutMemberLabels()
    {
        int index = 0;
        foreach (var kvp in memberLabels)
        {
            RectTransform rect = kvp.Value.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-10, -10 - (index * 25));
            rect.sizeDelta = new Vector2(200, 25);

            index++;
        }
    }
}
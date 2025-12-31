using UnityEngine;
using TMPro;
using ColonySim.Structures;

/// <summary>
/// Visual representation of a Building in the game world.
/// </summary>
public class BuildingView : MonoBehaviour
{
    [Header("References")]
    private Building building;

    [Header("UI")]
    [SerializeField] private TextMeshPro nameLabel;
    [SerializeField] private TextMeshPro capacityLabel;

    /// <summary>
    /// Initialize this view with a Building to represent.
    /// </summary>
    public void Initialize(Building buildingData)
    {
        building = buildingData;
        transform.position = building.WorldPosition;

        // Set color based on building definition
        SetBuildingColor(building.Definition.buildingColor);

        // Create UI
        CreateUIElements();
        UpdateDisplay();
    }

    private void Update()
    {
        if (building != null)
        {
            UpdateDisplay();
            FaceUIToCamera();
        }
    }

    /// <summary>
    /// Set the building's visual color.
    /// </summary>
    private void SetBuildingColor(Color color)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    /// <summary>
    /// Update UI labels.
    /// </summary>
    private void UpdateDisplay()
    {
        if (nameLabel != null)
        {
            nameLabel.text = building.Definition.buildingName;
        }

        if (capacityLabel != null)
        {
            int current = building.GetCurrentUsers().Count;
            int max = building.Definition.capacity;
            capacityLabel.text = $"{current}/{max}";

            // Color based on capacity
            capacityLabel.color = current >= max ? Color.red : Color.green;
        }
    }

    /// <summary>
    /// Create name and capacity labels above building.
    /// </summary>
    private void CreateUIElements()
    {
        // Name label
        GameObject nameObj = new GameObject("NameLabel");
        nameObj.transform.SetParent(transform);
        nameObj.transform.localPosition = new Vector3(0, 3f, 0);
        nameLabel = nameObj.AddComponent<TextMeshPro>();
        nameLabel.fontSize = 4;
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.color = Color.white;

        // Capacity label
        GameObject capacityObj = new GameObject("CapacityLabel");
        capacityObj.transform.SetParent(transform);
        capacityObj.transform.localPosition = new Vector3(0, 2.5f, 0);
        capacityLabel = capacityObj.AddComponent<TextMeshPro>();
        capacityLabel.fontSize = 3;
        capacityLabel.alignment = TextAlignmentOptions.Center;
    }

    /// <summary>
    /// Make UI face camera.
    /// </summary>
    private void FaceUIToCamera()
    {
        if (Camera.main == null) return;

        if (nameLabel != null)
        {
            nameLabel.transform.LookAt(Camera.main.transform);
            nameLabel.transform.Rotate(0, 180, 0);
        }

        if (capacityLabel != null)
        {
            capacityLabel.transform.LookAt(Camera.main.transform);
            capacityLabel.transform.Rotate(0, 180, 0);
        }
    }

    /// <summary>
    /// Get the Building this view represents.
    /// </summary>
    public Building GetBuilding() => building;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (building != null && building.Definition.usePositions != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var usePos in building.Definition.usePositions)
            {
                Vector2Int worldGridPos = building.GridPosition + usePos.relativePosition;
                Vector3 worldPosition = GridSystem.Instance.GridToWorld(worldGridPos);
                Gizmos.DrawWireSphere(worldPosition, 0.3f);
                Gizmos.DrawLine(building.WorldPosition, worldPosition);
            }
        }
    }
#endif
}
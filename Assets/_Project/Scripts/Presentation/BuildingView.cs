using UnityEngine;
using TMPro;
using Ideology.Structures;

public class BuildingView : MonoBehaviour
{
    [Header("References")]
    private Building building;

    [Header("UI")]
    [SerializeField] private TextMeshPro nameLabel;
    [SerializeField] private TextMeshPro capacityLabel;

    public void Initialize(Building buildingData)
    {
        building = buildingData;
        transform.position = building.WorldPosition;

        SetBuildingColor(building.Definition.buildingColor);

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

    private void SetBuildingColor(Color color)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private void UpdateDisplay()
    {
        if (nameLabel != null)
        {
            nameLabel.text = building.Definition.structureName;
        }

        if (capacityLabel != null)
        {
            int current = building.GetCurrentUsers();
            int max = building.Definition.maxOccupants;
            capacityLabel.text = $"{current}/{max}";

            capacityLabel.color = current >= max ? Color.red : Color.green;
        }
    }

    private void CreateUIElements()
    {
        GameObject nameObj = new GameObject("NameLabel");
        nameObj.transform.SetParent(transform);
        nameObj.transform.localPosition = new Vector3(0, 3f, 0);
        nameLabel = nameObj.AddComponent<TextMeshPro>();
        nameLabel.fontSize = 4;
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.color = Color.white;

        GameObject capacityObj = new GameObject("CapacityLabel");
        capacityObj.transform.SetParent(transform);
        capacityObj.transform.localPosition = new Vector3(0, 2.5f, 0);
        capacityLabel = capacityObj.AddComponent<TextMeshPro>();
        capacityLabel.fontSize = 3;
        capacityLabel.alignment = TextAlignmentOptions.Center;
    }

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
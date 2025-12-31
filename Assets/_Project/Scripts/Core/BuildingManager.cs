using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages all buildings in the commune.
/// Handles placement, tracking, and finding buildings by need type.
/// </summary>
public class BuildingManager : MonoBehaviour
{
    [Header("Available Buildings")]
    [Tooltip("Building types that can be placed")]
    [SerializeField] private List<BuildingDefinition> availableBuildings;

    [Header("Placement")]
    [SerializeField] private Material placementPreviewMaterial;
    [SerializeField] private Color validPlacementColor = Color.green;
    [SerializeField] private Color invalidPlacementColor = Color.red;

    // Track all buildings
    private List<Building> buildings = new List<Building>();
    private List<BuildingView> buildingViews = new List<BuildingView>();

    // Placement state
    private bool isPlacingBuilding = false;
    private BuildingDefinition currentBuildingToPlace;
    private GameObject placementPreview;

    // Singleton
    public static BuildingManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        Debug.Log($"Update: isPlacingBuilding = {isPlacingBuilding}");
        HandleKeyboardShortcuts();
        
        if (isPlacingBuilding)
        {
            UpdatePlacementPreview();
            HandlePlacementInput();
        }

        UpdateProduction();
    }

    /// <summary>
    /// Update production for all work buildings.
    /// </summary>
    private void UpdateProduction()
    {
        float deltaTime = Time.deltaTime;

        foreach (var building in buildings)
        {
            building.UpdateProduction(deltaTime);
        }
    }

    /// <summary>
    /// Start placing a building of the given type.
    /// PUBLIC: Called by UI or keybinds.
    /// </summary>
    public void StartPlacingBuilding(BuildingDefinition definition)
    {
        if (definition == null || definition.prefab == null)
        {
            Debug.LogError("Invalid building definition!");
            return;
        }

        // Clean up old preview if switching building types
        if (placementPreview != null)
        {
            Destroy(placementPreview);
            placementPreview = null;
        }

        isPlacingBuilding = true;
        currentBuildingToPlace = definition;

        // Create preview
        placementPreview = Instantiate(definition.prefab);

        // Make it semi-transparent
        Renderer renderer = placementPreview.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material previewMat = new Material(renderer.material);
            previewMat.color = new Color(previewMat.color.r, previewMat.color.g, previewMat.color.b, 0.5f);
            renderer.material = previewMat;
        }

        Debug.Log($"Started placing: {definition.buildingName}");
    }

    /// <summary>
    /// Update placement preview position.
    /// </summary>
    private void UpdatePlacementPreview()
    {
        if (placementPreview == null)
        {
            Debug.LogWarning("Placement preview is null!");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogWarning("Camera.main is null!");
            return;
        }

        // Raycast from mouse to ground
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        float distance;
        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance); 

            // Snap to grid
            Vector2Int gridPos = GridSystem.Instance.WorldToGrid(worldPoint);
            Vector3 snappedPos = GridSystem.Instance.GridToWorld(gridPos);

            placementPreview.transform.position = snappedPos;

            // Check if placement is valid
            bool isValid = IsValidPlacement(gridPos);

            // Color preview based on validity
            Renderer renderer = placementPreview.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = isValid ? validPlacementColor : invalidPlacementColor;
                color.a = 0.5f;
                renderer.material.color = color;
            }
        }
        else
        {
            Debug.LogWarning("Raycast did not hit ground plane!");
        }
    }

    /// <summary>
    /// Handle input during placement.
    /// </summary>
    private void HandlePlacementInput()
    {
        // Left click: Place building
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuilding();
        }

        // Right click or Escape: Cancel
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    /// <summary>
    /// Try to place the building at current preview position.
    /// </summary>
    private void TryPlaceBuilding()
    {
        if (placementPreview == null) return;

        Vector3 worldPos = placementPreview.transform.position;
        Vector2Int gridPos = GridSystem.Instance.WorldToGrid(worldPos);

        if (IsValidPlacement(gridPos))
        {
            PlaceBuilding(currentBuildingToPlace, worldPos, gridPos);

            // Stay in placement mode (can place multiple)
            // To exit: right-click or ESC
        }
        else
        {
            Debug.LogWarning("Cannot place building here!");
        }
    }

    /// <summary>
    /// Check if a building can be placed at this grid position.
    /// </summary>
    private bool IsValidPlacement(Vector2Int gridPos)
    {
        if (currentBuildingToPlace == null) return false;

        // Check all cells the building would occupy
        for (int x = 0; x < currentBuildingToPlace.size.x; x++)
        {
            for (int y = 0; y < currentBuildingToPlace.size.y; y++)
            {
                Vector2Int checkPos = new Vector2Int(gridPos.x + x, gridPos.y + y);
                GridCell cell = GridSystem.Instance.GetCell(checkPos);

                if (cell == null || !cell.IsWalkable || cell.IsOccupied)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Actually place the building (create simulation and view).
    /// </summary>
    private void PlaceBuilding(BuildingDefinition definition, Vector3 worldPosition, Vector2Int gridPosition)
    {
        // Instantiate the building prefab
        GameObject buildingObject = Instantiate(definition.prefab, worldPosition, Quaternion.identity);
        buildingObject.name = definition.structureName;

        // Create the runtime Building instance (NEW STRUCTURE SYSTEM)
        Building building = new Building(definition, gridPosition, worldPosition, buildingObject);

        // Mark grid cells as occupied
        List<Vector2Int> occupiedCells = definition.GetOccupiedCells(gridPosition);
        foreach (Vector2Int cell in occupiedCells)
        {
            GridSystem.Instance.SetCellOccupied(cell, true);
        }

        // Register with StructureManager
        StructureManager.Instance.RegisterBuilding(building);

        // Add BuildingView component for visualization
        BuildingView buildingView = buildingObject.AddComponent<BuildingView>();
        buildingView.Initialize(building);

        Debug.Log($"Placed {definition.structureName} at {gridPosition}");

        // Trigger event
        GameEvents.TriggerBuildingPlaced(building);

        // Clear placement mode
        CancelPlacement();
    }

    /// <summary>
    /// Cancel building placement.
    /// </summary>
    private void CancelPlacement()
    {
        if (placementPreview != null)
        {
            Destroy(placementPreview);
        }

        isPlacingBuilding = false;
        currentBuildingToPlace = null;

        Debug.Log("Placement cancelled");
    }

    /// <summary>
    /// Find the nearest building that satisfies a given need.
    /// RETURNS: Building if found, null if none exists.
    /// </summary>
    public Building FindNearestBuildingForNeed(string needName, Vector3 fromPosition)
    {
        Building nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var building in buildings)
        {
            // Check if building satisfies this need
            if (building.Definition.satisfiesNeed == needName && building.IsOperational)
            {
                // Check if it has capacity
                if (building.HasCapacity())
                {
                    float distance = Vector3.Distance(fromPosition, building.WorldPosition);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = building;
                    }
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// Get all buildings.
    /// </summary>
    public List<Building> GetAllBuildings() => new List<Building>(buildings);

    /// <summary>
    /// Get all building views.
    /// </summary>
    public List<BuildingView> GetAllBuildingViews() => new List<BuildingView>(buildingViews);

    /// <summary>
    /// Handle keyboard shortcuts for building placement (temporary dev feature).
    /// </summary>
    private void HandleKeyboardShortcuts()
    {
        if (availableBuildings == null || availableBuildings.Count == 0) return;

        // Press 1, 2, 3, 4, 5 to start placing buildings
        if (Input.GetKeyDown(KeyCode.Alpha1) && availableBuildings.Count > 0)
        {
            StartPlacingBuilding(availableBuildings[0]); // Food Storage
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) && availableBuildings.Count > 1)
        {
            StartPlacingBuilding(availableBuildings[1]); // Sleeping Area
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) && availableBuildings.Count > 2)
        {
            StartPlacingBuilding(availableBuildings[2]); // Social Space
        }

        if (Input.GetKeyDown(KeyCode.Alpha4) && availableBuildings.Count > 3)
        {
            StartPlacingBuilding(availableBuildings[3]); // Farm
        }

        if (Input.GetKeyDown(KeyCode.Alpha5) && availableBuildings.Count > 4)
        {
            StartPlacingBuilding(availableBuildings[4]); // Workshop
        }
    }

    /// <summary>
    /// Find all work buildings of a specific type.
    /// </summary>
    public List<Building> FindWorkBuildings(string producedItemName)
    {
        List<Building> workBuildings = new List<Building>();

        foreach (var building in buildings)
        {
            if (building.Definition.isWorkBuilding &&
                building.Definition.producedItem != null &&
                building.Definition.producedItem.itemName == producedItemName)
            {
                workBuildings.Add(building);
            }
        }

        return workBuildings;
    }

    /// <summary>
    /// Find a work building that needs workers.
    /// </summary>
    public Building FindWorkBuildingNeedingWorkers()
    {
        foreach (var building in buildings)
        {
            if (building.Definition.isWorkBuilding)
            {
                if (building.GetAssignedWorkers().Count < building.Definition.workerCapacity)
                {
                    return building;
                }
            }
        }

        return null;
    }

}
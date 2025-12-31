using UnityEngine;
using System.Collections.Generic;

namespace ColonySim.Structures
{
    /// <summary>
    /// Base class for all placeable structures (buildings, furniture, equipment)
    /// </summary>
    public abstract class StructureDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string structureName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;

        [Header("Grid Configuration")]
        public Vector2Int size = Vector2Int.one;
        public List<Vector2Int> occupiedCells = new List<Vector2Int>(); // Relative positions

        [Header("Interaction Points")]
        public List<UsePosition> usePositions = new List<UsePosition>();

        [Header("Visual")]
        public GameObject prefab;

        /// <summary>
        /// Get all grid cells this structure occupies
        /// </summary>
        public virtual List<Vector2Int> GetOccupiedCells(Vector2Int origin)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            if (occupiedCells.Count > 0)
            {
                foreach (var cell in occupiedCells)
                {
                    cells.Add(origin + cell);
                }
            }
            else
            {
                // Default: fill size rectangle
                for (int x = 0; x < size.x; x++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        cells.Add(origin + new Vector2Int(x, y));
                    }
                }
            }

            return cells;
        }
    }

    [System.Serializable]
    public class UsePosition
    {
        public Vector2Int relativePosition; // Relative to structure origin
        public InteractionType interactionType;
        public string interactionLabel; // "Work here", "Sleep here", "Store items"
        public bool isRequired = true; // Must be accessible for structure to function

        [Header("Capacity")]
        public int maxSimultaneousUsers = 1;
    }

    public enum InteractionType
    {
        StandAdjacent,  // Work at a table, use storage
        Occupy,         // Sleep in bed, sit in chair
        Enter,          // Walk into a room/building
        PassThrough     // Doorway, corridor
    }
}
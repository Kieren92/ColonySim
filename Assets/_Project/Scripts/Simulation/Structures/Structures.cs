using UnityEngine;
using System.Collections.Generic;

namespace Ideology.Structures
{
    /// <summary>
    /// Runtime instance of a placed structure
    /// </summary>
    public abstract class Structure
    {
        public StructureDefinition Definition { get; protected set; }
        public Vector2Int GridPosition { get; protected set; }
        public Vector3 WorldPosition { get; protected set; }
        public GameObject GameObject { get; protected set; }
        protected List<Member> currentUsers = new List<Member>();

        public Structure(StructureDefinition definition, Vector2Int gridPos, Vector3 worldPos, GameObject gameObject)
        {
            Definition = definition;
            GridPosition = gridPos;
            WorldPosition = worldPos;
            GameObject = gameObject;
        }

        public virtual bool CanUse(Member member)
        {
            // Check capacity
            int totalCapacity = 0;
            foreach (var usePos in Definition.usePositions)
            {
                totalCapacity += usePos.maxSimultaneousUsers;
            }

            return currentUsers.Count < totalCapacity;
        }

        public virtual bool StartUsing(Member member)
        {
            if (!CanUse(member))
                return false;

            if (!currentUsers.Contains(member))
            {
                currentUsers.Add(member);
            }

            return true;
        }

        public virtual void StopUsing(Member member)
        {
            currentUsers.Remove(member);
        }

        public Vector3 GetUsePosition(Member member)
        {
            List<UsePosition> validPositions = new List<UsePosition>();

            // Get occupied cells - only Buildings have this concept
            List<Vector2Int> occupiedCells = GetOccupiedCells();

            foreach (var usePos in Definition.usePositions)
            {
                Vector2Int worldPos = GridPosition + usePos.relativePosition;

                // For interior interaction types (Occupy, Enter), allow positions inside the building
                bool isInteriorInteraction = usePos.interactionType == InteractionType.Occupy ||
                                            usePos.interactionType == InteractionType.Enter;

                if (!isInteriorInteraction)
                {
                    // For exterior interactions (StandAdjacent), skip if position is occupied by the building
                    if (occupiedCells != null && occupiedCells.Contains(worldPos))
                        continue;

                    // Check if the position is walkable
                    if (!GridSystem.Instance.IsCellWalkable(worldPos))
                        continue;
                }
                else
                {
                    // For interior interactions, the position MUST be inside the building
                    if (occupiedCells == null || !occupiedCells.Contains(worldPos))
                        continue;
                }

                // Check if position is already occupied by another member
                // (optional: add occupancy tracking later)

                validPositions.Add(usePos);
            }

            if (validPositions.Count == 0)
            {
                Debug.LogWarning($"{Definition.structureName}: No valid use positions!");
                return Vector3.zero;
            }

            // For now, return the first valid position
            // TODO: Implement smart selection based on member distance, availability, etc.
            UsePosition selectedPosition = validPositions[0];
            Vector2Int selectedGridPos = GridPosition + selectedPosition.relativePosition;
            return GridSystem.Instance.GridToWorld(selectedGridPos);
        }

        /// <summary>
        /// Get the cells occupied by this structure. Override in Building class.
        /// </summary>
        protected virtual List<Vector2Int> GetOccupiedCells()
        {
            // Default: structures don't occupy cells (only Buildings do)
            return null;
        }

        public int GetCurrentUsers() => currentUsers.Count;
    }
}
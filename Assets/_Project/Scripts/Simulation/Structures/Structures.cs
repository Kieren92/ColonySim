using UnityEngine;
using System.Collections.Generic;

namespace ColonySim.Structures
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

        public virtual Vector3 GetUsePosition(Member member = null)
        {
            // Find first available use position
            foreach (var usePos in Definition.usePositions)
            {
                Vector2Int worldGridPos = GridPosition + usePos.relativePosition;

                if (GridSystem.Instance.IsCellWalkable(worldGridPos))
                {
                    return GridSystem.Instance.GridToWorld(worldGridPos);
                }
            }

            Debug.LogWarning($"{Definition.structureName}: No valid use positions!");
            return WorldPosition;
        }

        public int GetCurrentUsers() => currentUsers.Count;
    }
}
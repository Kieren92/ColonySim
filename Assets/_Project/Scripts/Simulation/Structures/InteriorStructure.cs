using UnityEngine;

namespace ColonySim.Structures
{
    public class InteriorStructure : Structure
    {
        public new InteriorStructureDefinition Definition => base.Definition as InteriorStructureDefinition;
        public Building ParentBuilding { get; private set; }

        public InteriorStructure(
            InteriorStructureDefinition definition,
            Vector2Int gridPos,
            Vector3 worldPos,
            GameObject gameObject,
            Building parentBuilding)
            : base(definition, gridPos, worldPos, gameObject)
        {
            ParentBuilding = parentBuilding;
        }

        public override bool CanUse(Member member)
        {
            // Check parent building is accessible
            if (ParentBuilding == null)
                return false;

            // Check base capacity
            if (!base.CanUse(member))
                return false;

            // Check skill requirements
            if (member != null && Definition.requiredSkills.Count > 0)
            {
                foreach (var skillReq in Definition.requiredSkills)
                {
                    if (member.Skills.GetSkillLevel(skillReq.skillName) < skillReq.minimumLevel)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    public enum GoalResolutionKind
    {
        Completed,
        BaseOffTarget,
        CustomRequired,
        SealOnly,
        OverlayStackOnly
    }

    public sealed class GoalResolution
    {
        public GoalResolutionKind kind;
        public WorldStateGoal goal = null!;
        public WorldStateGoal targetGoal = null!;
        public string worldEffect = "";
        public float distance;
        public float radius;
    }

    public sealed class FloorGoalSystem
    {
        public GoalResolution ResolveBase(IReadOnlyList<WorldStateGoal> activeGoals, SpellFamily family, Vector2 center, bool isCustomShape = false)
        {
            foreach (var goal in activeGoals.Where(goal => !goal.completed))
            {
                if (goal.MatchesBase(family, center))
                {
                    if (goal.requiresCustomShape && !isCustomShape)
                    {
                        return new GoalResolution
                        {
                            kind = GoalResolutionKind.CustomRequired,
                            targetGoal = goal,
                            worldEffect = "custom_required",
                            distance = Vector2.Distance(center, goal.position),
                            radius = goal.radius
                        };
                    }

                    return new GoalResolution
                    {
                        kind = GoalResolutionKind.Completed,
                        goal = goal,
                        worldEffect = SpellLabels.English(family)
                    };
                }
            }

            var target = activeGoals
                .Where(goal => !goal.completed && goal.requiredBase == family)
                .OrderBy(goal => Vector2.Distance(center, goal.position))
                .FirstOrDefault();
            if (target != null)
            {
                return new GoalResolution
                {
                    kind = GoalResolutionKind.BaseOffTarget,
                    targetGoal = target,
                    worldEffect = "base_off_target",
                    distance = Vector2.Distance(center, target.position),
                    radius = target.radius
                };
            }

            return new GoalResolution
            {
                kind = GoalResolutionKind.SealOnly,
                worldEffect = "seal_only"
            };
        }

        public GoalResolution ResolveOverlay(
            IReadOnlyList<WorldStateGoal> activeGoals,
            CompiledSeal seal,
            OverlayOperator op,
            Vector2 center)
        {
            foreach (var goal in activeGoals.Where(goal => !goal.completed))
            {
                if (goal.MatchesOverlay(seal, op, center))
                {
                    return new GoalResolution
                    {
                        kind = GoalResolutionKind.Completed,
                        goal = goal,
                        worldEffect = SpellLabels.English(op)
                    };
                }
            }

            return new GoalResolution
            {
                kind = GoalResolutionKind.OverlayStackOnly,
                worldEffect = "overlay_stack"
            };
        }
    }
}

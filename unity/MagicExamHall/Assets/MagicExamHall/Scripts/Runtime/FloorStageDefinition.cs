using System;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    [CreateAssetMenu(menuName = "Magic Exam Hall/Floor Stage Definition", fileName = "FloorStageDefinition")]
    public sealed class FloorStageDefinition : ScriptableObject
    {
        public int floorNumber = 3;
        public Vector2 playerStart = new(-7.2f, -2.58f);
        public Vector2 stageMin = new(-8.5f, -4.4f);
        public Vector2 stageMax = new(21.0f, 4.4f);
        public Vector2 cameraXRange = new(-2.8f, 15.4f);
        public float cameraY = -0.2f;
        public float killY = -5.2f;
        public Vector2 customReferencePosition = new(-7.25f, 1.1f);
        public StagePropDefinition[] props = Array.Empty<StagePropDefinition>();
        public StageObstacleDefinition[] obstacles = Array.Empty<StageObstacleDefinition>();
        public StageEnvironmentEffect[] environmentEffects = Array.Empty<StageEnvironmentEffect>();

        public StageObstacleDefinition FindObstacle(string goalId)
        {
            return obstacles?.FirstOrDefault(item => string.Equals(item.requiredGoalId, goalId, StringComparison.OrdinalIgnoreCase));
        }

        public StageObstacleDefinition FindObstacleForEffect(CustomSpellEffectKind effect)
        {
            return obstacles?.FirstOrDefault(item => item.requiredEffect == effect);
        }

        public bool TryResolveEffect(SpellFamily family, SpellResult spell, out StageEnvironmentEffect effect)
        {
            effect = environmentEffects?.FirstOrDefault(item => item != null && item.Matches(family, spell));
            return effect != null;
        }

        public StageEnvironmentEffect FindEffect(CustomSpellEffectKind effectKind, SpellFamily? family = null)
        {
            return environmentEffects?.FirstOrDefault(item =>
                item != null &&
                item.customEffect == effectKind &&
                (!family.HasValue || item.baseFamily == family.Value));
        }

        public static FloorStageDefinition CreateFallbackFloorThree()
        {
            var definition = CreateInstance<FloorStageDefinition>();
            definition.name = "Floor3CrossingFallback";
            definition.props = new[]
            {
                StagePropDefinition.Solid("Start Stone Walkway", new Vector2(-6.4f, -3.35f), new Vector2(4.4f, 0.5f)),
                StagePropDefinition.Solid("River Bank", new Vector2(0.2f, -3.35f), new Vector2(2.7f, 0.5f)),
                StagePropDefinition.Solid("Gap Far Stone Walkway", new Vector2(5.5f, -3.35f), new Vector2(4.2f, 0.5f)),
                StagePropDefinition.Solid("Vine Landing", new Vector2(12.2f, -3.35f), new Vector2(3.1f, 0.5f)),
                StagePropDefinition.Solid("Exit Ledge", new Vector2(18.8f, -3.35f), new Vector2(4.0f, 0.5f))
            };
            definition.obstacles = new[]
            {
                new StageObstacleDefinition
                {
                    requiredGoalId = "frozen_river",
                    requiredEffect = CustomSpellEffectKind.Ice,
                    title = "강물",
                    center = new Vector2(-2.55f, -3.15f),
                    size = new Vector2(2.85f, 1.05f),
                    resetPosition = new Vector2(-5.0f, -2.58f),
                    safePositionAfterSolved = new Vector2(0.2f, -2.58f),
                    goalPosition = new Vector2(-2.55f, -1.88f),
                    solutionPosition = new Vector2(-2.55f, -3.16f),
                    solutionSize = new Vector2(2.85f, 0.34f),
                    lockedNote = "강물은 얼린 뒤 지나갈 수 있습니다. 물 문양 위에 육각형 도형을 얹으세요."
                },
                new StageObstacleDefinition
                {
                    requiredGoalId = "earth_stairs",
                    requiredEffect = CustomSpellEffectKind.Stability,
                    title = "깨진 구멍",
                    center = new Vector2(3.25f, -3.15f),
                    size = new Vector2(2.35f, 1.05f),
                    resetPosition = new Vector2(0.8f, -2.58f),
                    safePositionAfterSolved = new Vector2(5.2f, -2.58f),
                    goalPosition = new Vector2(3.25f, -1.88f),
                    solutionPosition = new Vector2(3.25f, -3.16f),
                    solutionSize = new Vector2(2.35f, 0.34f),
                    lockedSpriteKind = PixelSpriteKind.Rubble,
                    lockedNote = "바닥이 깨져 구멍이 뚫려 있습니다. 땅 문양 위에 사각 메움판을 얹어 빈 공간을 메우세요."
                },
                new StageObstacleDefinition
                {
                    requiredGoalId = "living_bridge",
                    requiredEffect = CustomSpellEffectKind.LivingBridge,
                    title = "낭떠러지",
                    center = new Vector2(8.9f, -3.15f),
                    size = new Vector2(3.25f, 1.2f),
                    resetPosition = new Vector2(6.2f, -2.58f),
                    safePositionAfterSolved = new Vector2(11.0f, -2.58f),
                    goalPosition = new Vector2(8.85f, -1.88f),
                    solutionPosition = new Vector2(8.9f, -3.16f),
                    solutionSize = new Vector2(3.25f, 0.34f),
                    lockedNote = "앞의 낭떠러지는 발판 없이는 건널 수 없습니다. 생명 문양 위에 화살표와 사각 발판 도형을 얹으세요."
                },
                new StageObstacleDefinition
                {
                    requiredGoalId = "wind_platform",
                    requiredEffect = CustomSpellEffectKind.WindPlatform,
                    title = "먼 발판",
                    center = new Vector2(15.35f, -3.15f),
                    size = new Vector2(2.65f, 1.15f),
                    resetPosition = new Vector2(13.1f, -2.58f),
                    safePositionAfterSolved = new Vector2(17.3f, -2.58f),
                    goalPosition = new Vector2(15.35f, -1.88f),
                    solutionPosition = new Vector2(15.35f, -3.16f),
                    solutionSize = new Vector2(2.65f, 0.34f),
                    lockedNote = "마지막 빈 공간은 떠 있는 발판으로 건넙니다. 바람 문양 위에 사각형 도형을 얹으세요."
                }
            };
            return definition;
        }
    }

    [Serializable]
    public sealed class StagePropDefinition
    {
        public string title = "";
        public Vector2 position;
        public Vector2 size = Vector2.one;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.white;
        public PixelSpriteKind spriteKind = PixelSpriteKind.FloorTile;
        public Sprite spriteOverride = null!;
        public int sortingOrder = -4;
        public bool tiled = true;
        public bool hasCollider;

        public static StagePropDefinition Solid(string title, Vector2 position, Vector2 size)
        {
            return new StagePropDefinition
            {
                title = title,
                position = position,
                size = size,
                primaryColor = new Color(0.22f, 0.20f, 0.19f),
                secondaryColor = new Color(0.44f, 0.38f, 0.30f),
                spriteKind = PixelSpriteKind.FloorTile,
                sortingOrder = -4,
                tiled = true,
                hasCollider = true
            };
        }
    }

    [Serializable]
    public sealed class StageObstacleDefinition
    {
        public string requiredGoalId = "";
        public CustomSpellEffectKind requiredEffect = CustomSpellEffectKind.None;
        public string title = "";
        public Vector2 center;
        public Vector2 size = Vector2.one;
        public Vector2 resetPosition;
        public Vector2 safePositionAfterSolved;
        public Vector2 goalPosition;
        public float goalRadius = 2.15f;
        public Vector2 solutionPosition;
        public Vector2 solutionSize = Vector2.one;
        public Color lockedColor = new(0.025f, 0.030f, 0.045f, 1f);
        public PixelSpriteKind lockedSpriteKind = PixelSpriteKind.WaterHazard;
        [TextArea]
        public string lockedNote = "";
    }
}

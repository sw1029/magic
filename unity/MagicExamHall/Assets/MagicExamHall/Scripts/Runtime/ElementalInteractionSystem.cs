using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    [Flags]
    public enum ElementalMaterial
    {
        None = 0,
        Stone = 1 << 0,
        Soil = 1 << 1,
        Wood = 1 << 2,
        Plant = 1 << 3,
        Water = 1 << 4,
        Ice = 1 << 5,
        Metal = 1 << 6,
        Cloth = 1 << 7,
        Air = 1 << 8,
        Fire = 1 << 9,
        Creature = 1 << 10
    }

    [Flags]
    public enum ElementalState
    {
        None = 0,
        Wet = 1 << 0,
        Burning = 1 << 1,
        Frozen = 1 << 2,
        WindPushed = 1 << 3,
        Charged = 1 << 4,
        Grown = 1 << 5,
        Stabilized = 1 << 6,
        Steaming = 1 << 7
    }

    public enum ElementalReactionKind
    {
        None,
        Wet,
        Ignite,
        Extinguish,
        Freeze,
        Melt,
        Steam,
        Push,
        Conduct,
        Grow,
        Stabilize
    }

    public readonly struct ElementalInteractionContext
    {
        public ElementalInteractionContext(
            SpellFamily family,
            CustomSpellEffectKind customEffect,
            CustomShapeEventKind eventKind,
            Vector2 center,
            Vector2 direction,
            float radius,
            string sourceLabel)
        {
            this.family = family;
            this.customEffect = customEffect;
            this.eventKind = eventKind;
            this.center = center;
            this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            this.radius = Mathf.Max(0.2f, radius);
            this.sourceLabel = string.IsNullOrWhiteSpace(sourceLabel) ? family.ToString() : sourceLabel;
        }

        public readonly SpellFamily family;
        public readonly CustomSpellEffectKind customEffect;
        public readonly CustomShapeEventKind eventKind;
        public readonly Vector2 center;
        public readonly Vector2 direction;
        public readonly float radius;
        public readonly string sourceLabel;

        public bool IsCold => customEffect == CustomSpellEffectKind.Ice ||
                              (family == SpellFamily.Water && eventKind == CustomShapeEventKind.Stun);

        public bool IsElectric => customEffect == CustomSpellEffectKind.Electric ||
                                  eventKind == CustomShapeEventKind.SlashDamage && family == SpellFamily.Fire;

        public bool IsSteel => customEffect == CustomSpellEffectKind.Steel;

        public bool IsWindForce => family == SpellFamily.Wind ||
                                   customEffect == CustomSpellEffectKind.Flow ||
                                   eventKind == CustomShapeEventKind.MoveSpeedBuff ||
                                   eventKind == CustomShapeEventKind.DirectionalProjectile ||
                                   eventKind == CustomShapeEventKind.AttributeLaser;
    }

    public readonly struct ElementalReactionReport
    {
        public ElementalReactionReport(
            ElementalEntity entity,
            ElementalReactionKind reactionKind,
            ElementalState resultingState,
            Vector2 position,
            string note)
        {
            this.entity = entity;
            this.reactionKind = reactionKind;
            this.resultingState = resultingState;
            this.position = position;
            this.note = note ?? "";
        }

        public readonly ElementalEntity entity;
        public readonly ElementalReactionKind reactionKind;
        public readonly ElementalState resultingState;
        public readonly Vector2 position;
        public readonly string note;
    }

    public sealed class ElementalEntity : MonoBehaviour
    {
        [SerializeField] private string entityId = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private ElementalMaterial materials = ElementalMaterial.None;
        [SerializeField] private ElementalState states = ElementalState.None;
        [SerializeField] private float responseRadius = 0.75f;
        [SerializeField] private bool movableByWind;
        [SerializeField] private float windMoveDistance = 0.34f;

        private SpriteRenderer spriteRenderer = null!;
        private Color baseTint = Color.white;

        public string EntityId => entityId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public ElementalMaterial Materials => materials;
        public ElementalState States => states;
        public float ResponseRadius => responseRadius;
        public bool MovableByWind => movableByWind;
        public int ReactionCount { get; private set; }
        public ElementalReactionKind LastReactionKind { get; private set; } = ElementalReactionKind.None;

        public bool HasMaterial(ElementalMaterial material)
        {
            return (materials & material) != 0;
        }

        public bool HasState(ElementalState state)
        {
            return (states & state) != 0;
        }

        public void Configure(
            string id,
            ElementalMaterial materialFlags,
            float radius,
            bool windMovable,
            SpriteRenderer renderer)
        {
            entityId = string.IsNullOrWhiteSpace(id) ? name : id;
            displayName = entityId;
            materials = materialFlags;
            responseRadius = Mathf.Max(0.15f, radius);
            movableByWind = windMovable;
            spriteRenderer = renderer == null ? GetComponent<SpriteRenderer>() : renderer;
            baseTint = spriteRenderer == null ? Color.white : spriteRenderer.color;
            RefreshVisual();
        }

        public bool IsInRange(Vector2 center, float radius)
        {
            var totalRadius = Mathf.Max(0.1f, radius) + Mathf.Max(0.05f, responseRadius);
            return Vector2.Distance(transform.position, center) <= totalRadius;
        }

        public bool TryApply(ElementalInteractionContext context, out ElementalReactionReport report)
        {
            var reaction = ResolveReaction(context);
            report = default;
            if (reaction == ElementalReactionKind.None)
            {
                return false;
            }

            ApplyReactionState(reaction, context);
            ReactionCount++;
            LastReactionKind = reaction;
            RefreshVisual();
            report = new ElementalReactionReport(
                this,
                reaction,
                states,
                transform.position,
                BuildNote(reaction, context));
            return true;
        }

        private ElementalReactionKind ResolveReaction(ElementalInteractionContext context)
        {
            if (context.IsCold)
            {
                if (HasMaterial(ElementalMaterial.Water) || HasState(ElementalState.Wet))
                {
                    return ElementalReactionKind.Freeze;
                }

                if (HasMaterial(ElementalMaterial.Fire) || HasState(ElementalState.Burning))
                {
                    return ElementalReactionKind.Extinguish;
                }
            }

            if (context.family == SpellFamily.Water)
            {
                if (HasState(ElementalState.Burning) || HasMaterial(ElementalMaterial.Fire))
                {
                    return ElementalReactionKind.Extinguish;
                }

                if (HasMaterial(ElementalMaterial.Water) || HasMaterial(ElementalMaterial.Wood) || HasMaterial(ElementalMaterial.Plant) || HasMaterial(ElementalMaterial.Soil))
                {
                    return ElementalReactionKind.Wet;
                }
            }

            if (context.family == SpellFamily.Fire)
            {
                if (HasMaterial(ElementalMaterial.Ice) || HasState(ElementalState.Frozen))
                {
                    return ElementalReactionKind.Melt;
                }

                if (HasMaterial(ElementalMaterial.Water) || HasState(ElementalState.Wet))
                {
                    return ElementalReactionKind.Steam;
                }

                if (HasMaterial(ElementalMaterial.Wood) || HasMaterial(ElementalMaterial.Plant) || HasMaterial(ElementalMaterial.Cloth))
                {
                    return ElementalReactionKind.Ignite;
                }
            }

            if (context.IsElectric &&
                (HasMaterial(ElementalMaterial.Metal) || HasMaterial(ElementalMaterial.Water) || HasState(ElementalState.Wet) || HasMaterial(ElementalMaterial.Creature)))
            {
                return ElementalReactionKind.Conduct;
            }

            if (context.IsWindForce && (movableByWind || HasMaterial(ElementalMaterial.Air) || HasMaterial(ElementalMaterial.Plant)))
            {
                return ElementalReactionKind.Push;
            }

            if (context.family == SpellFamily.Life && (HasMaterial(ElementalMaterial.Plant) || HasMaterial(ElementalMaterial.Wood) || HasMaterial(ElementalMaterial.Soil)))
            {
                return ElementalReactionKind.Grow;
            }

            if (context.family == SpellFamily.Earth && (HasMaterial(ElementalMaterial.Stone) || HasMaterial(ElementalMaterial.Soil)))
            {
                return ElementalReactionKind.Stabilize;
            }

            if (context.IsSteel && (HasMaterial(ElementalMaterial.Stone) || HasMaterial(ElementalMaterial.Soil) || HasMaterial(ElementalMaterial.Metal)))
            {
                return ElementalReactionKind.Stabilize;
            }

            return ElementalReactionKind.None;
        }

        private void ApplyReactionState(ElementalReactionKind reaction, ElementalInteractionContext context)
        {
            switch (reaction)
            {
                case ElementalReactionKind.Wet:
                    states |= ElementalState.Wet;
                    states &= ~ElementalState.Burning;
                    break;
                case ElementalReactionKind.Ignite:
                    states |= ElementalState.Burning;
                    states &= ~(ElementalState.Wet | ElementalState.Frozen);
                    break;
                case ElementalReactionKind.Extinguish:
                    states |= ElementalState.Wet;
                    states &= ~ElementalState.Burning;
                    break;
                case ElementalReactionKind.Freeze:
                    states |= ElementalState.Frozen;
                    states &= ~(ElementalState.Burning | ElementalState.Steaming);
                    materials |= ElementalMaterial.Ice;
                    break;
                case ElementalReactionKind.Melt:
                    states |= ElementalState.Wet;
                    states &= ~ElementalState.Frozen;
                    materials &= ~ElementalMaterial.Ice;
                    break;
                case ElementalReactionKind.Steam:
                    states |= ElementalState.Steaming;
                    states &= ~ElementalState.Frozen;
                    break;
                case ElementalReactionKind.Push:
                    states |= ElementalState.WindPushed;
                    if (movableByWind)
                    {
                        transform.position += (Vector3)(context.direction * windMoveDistance);
                    }
                    break;
                case ElementalReactionKind.Conduct:
                    states |= ElementalState.Charged;
                    break;
                case ElementalReactionKind.Grow:
                    states |= ElementalState.Grown;
                    transform.localScale = Vector3.Lerp(transform.localScale, transform.localScale * 1.08f, 0.85f);
                    break;
                case ElementalReactionKind.Stabilize:
                    states |= ElementalState.Stabilized;
                    break;
            }
        }

        private string BuildNote(ElementalReactionKind reaction, ElementalInteractionContext context)
        {
            return $"{DisplayName}: {Korean(reaction)} ({context.sourceLabel})";
        }

        private void RefreshVisual()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                return;
            }

            var tint = baseTint;
            if (HasState(ElementalState.Burning))
            {
                tint = Color.Lerp(tint, new Color(1f, 0.32f, 0.08f, tint.a), 0.62f);
            }
            else if (HasState(ElementalState.Frozen))
            {
                tint = Color.Lerp(tint, new Color(0.58f, 0.90f, 1f, tint.a), 0.62f);
            }
            else if (HasState(ElementalState.Wet))
            {
                tint = Color.Lerp(tint, new Color(0.24f, 0.52f, 1f, tint.a), 0.34f);
            }

            if (HasState(ElementalState.Charged))
            {
                tint = Color.Lerp(tint, new Color(1f, 0.92f, 0.20f, tint.a), 0.48f);
            }
            if (HasState(ElementalState.Grown))
            {
                tint = Color.Lerp(tint, new Color(0.34f, 0.92f, 0.42f, tint.a), 0.38f);
            }
            if (HasState(ElementalState.Stabilized))
            {
                tint = Color.Lerp(tint, new Color(0.82f, 0.66f, 0.38f, tint.a), 0.28f);
            }

            spriteRenderer.color = tint;
        }

        public static string Korean(ElementalReactionKind reaction)
        {
            return reaction switch
            {
                ElementalReactionKind.Wet => "젖음",
                ElementalReactionKind.Ignite => "점화",
                ElementalReactionKind.Extinguish => "진화",
                ElementalReactionKind.Freeze => "빙결",
                ElementalReactionKind.Melt => "융해",
                ElementalReactionKind.Steam => "증기",
                ElementalReactionKind.Push => "밀림",
                ElementalReactionKind.Conduct => "전도",
                ElementalReactionKind.Grow => "성장",
                ElementalReactionKind.Stabilize => "안정화",
                _ => "반응 없음"
            };
        }
    }

    public static class ElementalInteractionSystem
    {
        public static IReadOnlyList<ElementalReactionReport> Apply(
            IEnumerable<ElementalEntity> entities,
            ElementalInteractionContext context)
        {
            if (entities == null)
            {
                return Array.Empty<ElementalReactionReport>();
            }

            var reports = new List<ElementalReactionReport>();
            foreach (var entity in entities.Where(entity => entity != null && entity.isActiveAndEnabled))
            {
                if (!entity.IsInRange(context.center, context.radius))
                {
                    continue;
                }

                if (entity.TryApply(context, out var report))
                {
                    reports.Add(report);
                }
            }

            return reports;
        }

        public static string BuildSummary(IReadOnlyList<ElementalReactionReport> reports)
        {
            if (reports == null || reports.Count == 0)
            {
                return "";
            }

            return string.Join(
                " / ",
                reports
                    .GroupBy(report => report.reactionKind)
                    .OrderByDescending(group => group.Count())
                    .ThenBy(group => group.Key.ToString())
                    .Select(group => $"{ElementalEntity.Korean(group.Key)} {group.Count()}"));
        }

        public static ElementalMaterial InferMaterial(string name, PixelSpriteKind kind)
        {
            var normalized = (name ?? "").ToLowerInvariant();
            var material = kind switch
            {
                PixelSpriteKind.Player => ElementalMaterial.Creature | ElementalMaterial.Cloth,
                PixelSpriteKind.Target => ElementalMaterial.Creature | ElementalMaterial.Wood | ElementalMaterial.Metal,
                PixelSpriteKind.Scarecrow => ElementalMaterial.Creature | ElementalMaterial.Wood | ElementalMaterial.Plant,
                PixelSpriteKind.FloorTile => ElementalMaterial.Stone | ElementalMaterial.Soil,
                PixelSpriteKind.WallTrim => ElementalMaterial.Stone,
                PixelSpriteKind.Rug => ElementalMaterial.Cloth,
                PixelSpriteKind.Bookshelf => ElementalMaterial.Wood,
                PixelSpriteKind.Candle => ElementalMaterial.Fire | ElementalMaterial.Wood | ElementalMaterial.Metal,
                PixelSpriteKind.FireRune => ElementalMaterial.Fire,
                PixelSpriteKind.WaterRune => ElementalMaterial.Water,
                PixelSpriteKind.WindRune => ElementalMaterial.Air,
                PixelSpriteKind.EarthRune => ElementalMaterial.Stone | ElementalMaterial.Soil,
                PixelSpriteKind.LifeRune => ElementalMaterial.Plant,
                PixelSpriteKind.WaterHazard => ElementalMaterial.Water,
                PixelSpriteKind.IceBridge => ElementalMaterial.Ice | ElementalMaterial.Water,
                PixelSpriteKind.VineBridge => ElementalMaterial.Plant | ElementalMaterial.Wood,
                PixelSpriteKind.EarthStep => ElementalMaterial.Stone | ElementalMaterial.Soil,
                PixelSpriteKind.WindPlatformTile => ElementalMaterial.Air,
                PixelSpriteKind.CliffFace => ElementalMaterial.Stone,
                PixelSpriteKind.Rubble => ElementalMaterial.Stone | ElementalMaterial.Soil,
                _ => ElementalMaterial.None
            };

            if (normalized.Contains("bookcase") || normalized.Contains("bookshelf"))
            {
                material |= ElementalMaterial.Wood;
            }
            if (normalized.Contains("river") || normalized.Contains("water"))
            {
                material |= ElementalMaterial.Water;
            }
            if (normalized.Contains("ice") || normalized.Contains("frozen"))
            {
                material |= ElementalMaterial.Ice | ElementalMaterial.Water;
            }
            if (normalized.Contains("vine") || normalized.Contains("plant") || normalized.Contains("runner"))
            {
                material |= ElementalMaterial.Plant;
            }
            if (normalized.Contains("torch") || normalized.Contains("candle") || normalized.Contains("ember"))
            {
                material |= ElementalMaterial.Fire | ElementalMaterial.Wood;
            }
            if (normalized.Contains("metal") || normalized.Contains("target"))
            {
                material |= ElementalMaterial.Metal;
            }
            if (normalized.Contains("chasm") || normalized.Contains("gap") || normalized.Contains("wind"))
            {
                material |= ElementalMaterial.Air;
            }

            return material;
        }

        public static bool IsPhysicalElementalSprite(string name, PixelSpriteKind kind)
        {
            if (kind is PixelSpriteKind.Pulse or PixelSpriteKind.RuneCircle or PixelSpriteKind.Portal or PixelSpriteKind.GuideArrow or
                PixelSpriteKind.ShapeLine or PixelSpriteKind.ShapeArrow or PixelSpriteKind.ShapeRect or PixelSpriteKind.ShapeEllipse or
                PixelSpriteKind.ShapeHexagon or PixelSpriteKind.ShapeBrace or PixelSpriteKind.ShapeCross)
            {
                return false;
            }

            var normalized = name ?? "";
            return !normalized.StartsWith("Stage Effect ", StringComparison.Ordinal) &&
                   !normalized.StartsWith("Custom Shape ", StringComparison.Ordinal) &&
                   !normalized.StartsWith("Spell Pulse", StringComparison.Ordinal) &&
                   !normalized.StartsWith("Seal ", StringComparison.Ordinal);
        }

        public static bool InferWindMovable(string name, PixelSpriteKind kind, bool tiled)
        {
            if (tiled)
            {
                return false;
            }

            var normalized = (name ?? "").ToLowerInvariant();
            return kind == PixelSpriteKind.Target ||
                   kind == PixelSpriteKind.Scarecrow ||
                   kind == PixelSpriteKind.Rubble ||
                   kind == PixelSpriteKind.Candle ||
                   normalized.Contains("loose") ||
                   normalized.Contains("rubble") ||
                   normalized.Contains("target");
        }

        public static float InferResponseRadius(Vector3 scale, bool tiled, Vector2 tiledSize)
        {
            if (tiled)
            {
                return Mathf.Clamp(Mathf.Max(tiledSize.x, tiledSize.y) * 0.5f, 0.35f, 4.2f);
            }

            return Mathf.Clamp(Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) * 0.85f, 0.35f, 1.75f);
        }

        public static float SpellRadiusFor(SpellFamily family, CustomSpellEffectKind customEffect, CustomShapeEventKind eventKind)
        {
            if (eventKind is CustomShapeEventKind.AttributeLaser or CustomShapeEventKind.DirectionalProjectile or CustomShapeEventKind.CurveProjectile)
            {
                return 2.65f;
            }

            return customEffect switch
            {
                CustomSpellEffectKind.Ice => 2.55f,
                CustomSpellEffectKind.Electric => 2.35f,
                CustomSpellEffectKind.LivingBridge => 2.75f,
                CustomSpellEffectKind.WindPlatform => 2.80f,
                CustomSpellEffectKind.Steel => 2.45f,
                CustomSpellEffectKind.Stability => 2.40f,
                CustomSpellEffectKind.Connection => 2.50f,
                _ => family == SpellFamily.Wind ? 2.70f : 2.20f
            };
        }
    }
}

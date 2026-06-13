using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    public enum CustomShapeEventRole
    {
        Effect,
        Operator
    }

    public enum CustomShapeEventKind
    {
        None,
        SlashDamage,
        DirectionalProjectile,
        WallEntity,
        Barrier,
        Trap,
        Stun,
        MagicAmplify,
        AttackBuff,
        MoveSpeedBuff,
        SpecialAttackBoost,
        BuffDispel,
        EventBlock,
        AttributeLaser,
        RandomBuffDispel,
        PiercingMark,
        GuardBuff,
        CurveProjectile
    }

    public enum CustomShapeEventPersistence
    {
        Timed,
        Permanent
    }

    public enum CustomSpellEffectKind
    {
        None,
        Ice,
        Electric,
        Cleanse,
        Focus,
        Flow,
        Connection,
        Stability,
        LivingBridge,
        WindPlatform,
        Steel
    }

    public readonly struct CustomSpellEffectDefinition
    {
        public CustomSpellEffectDefinition(CustomSpellEffectKind kind, string displayName, string requirementLabel, string note, int impact)
        {
            this.kind = kind;
            this.displayName = displayName;
            this.requirementLabel = requirementLabel;
            this.note = note;
            this.impact = impact;
        }

        public readonly CustomSpellEffectKind kind;
        public readonly string displayName;
        public readonly string requirementLabel;
        public readonly string note;
        public readonly int impact;
        public bool IsValid => kind != CustomSpellEffectKind.None;
    }

    public static class CustomSpellEffectCatalog
    {
        public static CustomSpellEffectDefinition Resolve(SpellFamily baseFamily, SpellResult spell)
        {
            if (spell == null || !spell.isCustomShape)
            {
                return For(CustomSpellEffectKind.None);
            }

            return baseFamily switch
            {
                SpellFamily.Water when HasToken(spell, "hexagon") || IsEvent(spell, CustomShapeEventKind.Stun) => For(CustomSpellEffectKind.Ice),
                SpellFamily.Fire when HasToken(spell, "line") || IsEvent(spell, CustomShapeEventKind.SlashDamage) => For(CustomSpellEffectKind.Electric),
                SpellFamily.Water when HasToken(spell, "ellipse") || IsEvent(spell, CustomShapeEventKind.Barrier) => For(CustomSpellEffectKind.Cleanse),
                SpellFamily.Fire when HasToken(spell, "star") || IsEvent(spell, CustomShapeEventKind.MagicAmplify) => For(CustomSpellEffectKind.Focus),
                SpellFamily.Wind when HasToken(spell, "wave") || IsEvent(spell, CustomShapeEventKind.MoveSpeedBuff) => For(CustomSpellEffectKind.Flow),
                SpellFamily.Life when IsEvent(spell, CustomShapeEventKind.DirectionalProjectile) || HasToken(spell, "arrow") => For(CustomSpellEffectKind.LivingBridge),
                SpellFamily.Life when HasToken(spell, "brace") || IsEvent(spell, CustomShapeEventKind.AttackBuff) => For(CustomSpellEffectKind.Connection),
                SpellFamily.Earth when HasToken(spell, "pentagon") || IsEvent(spell, CustomShapeEventKind.GuardBuff) => For(CustomSpellEffectKind.Steel),
                SpellFamily.Wind when HasToken(spell, "rect") || IsEvent(spell, CustomShapeEventKind.WallEntity) => For(CustomSpellEffectKind.WindPlatform),
                SpellFamily.Earth when HasToken(spell, "rect") || IsEvent(spell, CustomShapeEventKind.WallEntity) => For(CustomSpellEffectKind.Stability),
                _ => For(CustomSpellEffectKind.None)
            };
        }

        public static bool IsSingleStepTransform(SpellFamily baseFamily, SpellResult spell, CustomSpellEffectKind kind)
        {
            if (spell == null || !spell.isCustomShape)
            {
                return false;
            }

            return baseFamily switch
            {
                SpellFamily.Fire => kind == CustomSpellEffectKind.Electric && HasToken(spell, "line"),
                SpellFamily.Water => kind == CustomSpellEffectKind.Ice && HasToken(spell, "hexagon"),
                SpellFamily.Earth => kind == CustomSpellEffectKind.Steel && HasToken(spell, "pentagon"),
                SpellFamily.Wind => kind == CustomSpellEffectKind.Flow && HasToken(spell, "wave"),
                SpellFamily.Life => kind == CustomSpellEffectKind.Connection && HasToken(spell, "brace"),
                _ => false
            };
        }

        public static CustomSpellEffectDefinition For(CustomSpellEffectKind kind)
        {
            return kind switch
            {
                CustomSpellEffectKind.Ice => new(kind, "얼음 결정", "물 + 육각형", "물이 얼음 결정으로 굳어 발판과 제압 효과를 만듭니다.", 28),
                CustomSpellEffectKind.Electric => new(kind, "번개 직선", "불꽃 + 직선", "불꽃이 직선 경로를 타고 번개처럼 뻗습니다.", 34),
                CustomSpellEffectKind.Cleanse => new(kind, "정화 물막", "물 + 둥근 물막", "둥근 물막이 오염을 씻어 내고 상태를 안정시킵니다.", 18),
                CustomSpellEffectKind.Focus => new(kind, "불꽃 초점", "불꽃 + 별 초점", "별 모양 초점이 다음 타격이 모일 지점을 밝혀 줍니다.", 24),
                CustomSpellEffectKind.Flow => new(kind, "바람 물결", "바람 + 물결", "바람이 물결 경로를 따라 이동 흐름을 만듭니다.", 20),
                CustomSpellEffectKind.Connection => new(kind, "생명 연결", "생명 + 연결선", "생명력이 떨어진 대상을 묶어 연결합니다.", 22),
                CustomSpellEffectKind.Steel => new(kind, "강철 오각형", "땅 + 오각형", "땅 문양이 오각형 구조로 압축되어 강철 문양으로 굳습니다.", 26),
                CustomSpellEffectKind.Stability => new(kind, "구멍 메우기", "땅 + 메움판", "사각 암반판이 깨진 바닥 구멍을 메워 길을 안정시킵니다.", 16),
                CustomSpellEffectKind.LivingBridge => new(kind, "덩굴 다리", "생명 + 화살표", "덩굴이 화살 방향으로 뻗어 낭떠러지를 잇습니다.", 0),
                CustomSpellEffectKind.WindPlatform => new(kind, "바람 발판", "바람 + 발판", "바람이 사각 발판을 띄워 건너갈 길을 만듭니다.", 0),
                _ => new(CustomSpellEffectKind.None, "", "", "", 0)
            };
        }

        public static string RequirementLabel(CustomSpellEffectKind kind)
        {
            var definition = For(kind);
            return definition.IsValid ? definition.requirementLabel : "";
        }

        public static string Korean(CustomSpellEffectKind kind)
        {
            var definition = For(kind);
            return definition.IsValid ? definition.displayName : "";
        }

        private static bool IsEvent(SpellResult spell, CustomShapeEventKind eventKind)
        {
            return Enum.TryParse<CustomShapeEventKind>(spell.customEventKind, out var parsed) && parsed == eventKind;
        }

        private static bool HasToken(SpellResult spell, string token)
        {
            var needle = token.ToLowerInvariant();
            return Contains(spell.customShapeToken, needle) ||
                   Contains(spell.customEventId, needle) ||
                   Contains(spell.customEventLabel, needle) ||
                   Contains(spell.customEventKind, needle);
        }

        private static bool Contains(string value, string needle)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.ToLowerInvariant().Contains(needle);
        }
    }

    public sealed class CustomShapeEventDefinition
    {
        public CustomShapeEventDefinition(
            string token,
            string eventId,
            string displayName,
            CustomShapeEventRole role,
            CustomShapeEventKind eventKind,
            string uiSummary,
            bool usesDirection = false,
            bool emitsFromEndPoint = false,
            bool blocksOverlappedEvent = false,
            CustomShapeEventKind operatorOnlyKind = CustomShapeEventKind.None,
            CustomShapeEventKind operatorTargetKind = CustomShapeEventKind.None,
            CustomShapeEventPersistence visualPersistence = CustomShapeEventPersistence.Timed,
            float visualLifetimeSeconds = 0f)
        {
            this.token = token;
            this.eventId = eventId;
            this.displayName = displayName;
            this.role = role;
            this.eventKind = eventKind;
            this.uiSummary = uiSummary;
            this.usesDirection = usesDirection;
            this.emitsFromEndPoint = emitsFromEndPoint;
            this.blocksOverlappedEvent = blocksOverlappedEvent;
            this.operatorOnlyKind = operatorOnlyKind == CustomShapeEventKind.None ? eventKind : operatorOnlyKind;
            this.operatorTargetKind = operatorTargetKind == CustomShapeEventKind.None ? eventKind : operatorTargetKind;
            this.visualPersistence = visualPersistence;
            this.visualLifetimeSeconds = visualLifetimeSeconds > 0f
                ? visualLifetimeSeconds
                : CustomShapeEventCatalog.VisualLifetimeFor(eventKind);
        }

        public readonly string token;
        public readonly string eventId;
        public readonly string displayName;
        public readonly CustomShapeEventRole role;
        public readonly CustomShapeEventKind eventKind;
        public readonly CustomShapeEventKind operatorOnlyKind;
        public readonly CustomShapeEventKind operatorTargetKind;
        public readonly string uiSummary;
        public readonly bool usesDirection;
        public readonly bool emitsFromEndPoint;
        public readonly bool blocksOverlappedEvent;
        public readonly CustomShapeEventPersistence visualPersistence;
        public readonly float visualLifetimeSeconds;
        public bool IsOperator => role == CustomShapeEventRole.Operator;
    }

    public sealed class CustomShapeEventPayload
    {
        public static readonly CustomShapeEventPayload Empty = new();

        public string shapeToken = "";
        public string eventId = "";
        public string displayName = "";
        public CustomShapeEventRole role = CustomShapeEventRole.Effect;
        public CustomShapeEventKind eventKind = CustomShapeEventKind.None;
        public string uiSummary = "";
        public bool usesDirection;
        public bool emitsFromEndPoint;
        public bool operatorOnly;
        public bool blocksEvent;
        public bool eventBlocked;
        public string blockedByToken = "";
        public CustomShapeEventPersistence visualPersistence = CustomShapeEventPersistence.Timed;
        public float visualLifetimeSeconds;
        public Vector2 origin;
        public Vector2 startPoint;
        public Vector2 endPoint;
        public Vector2 direction = Vector2.right;

        public bool HasEvent => !string.IsNullOrWhiteSpace(eventId) && eventKind != CustomShapeEventKind.None;

        public CustomShapeEventPayload Clone()
        {
            return new CustomShapeEventPayload
            {
                shapeToken = shapeToken,
                eventId = eventId,
                displayName = displayName,
                role = role,
                eventKind = eventKind,
                uiSummary = uiSummary,
                usesDirection = usesDirection,
                emitsFromEndPoint = emitsFromEndPoint,
                operatorOnly = operatorOnly,
                blocksEvent = blocksEvent,
                eventBlocked = eventBlocked,
                blockedByToken = blockedByToken,
                visualPersistence = visualPersistence,
                visualLifetimeSeconds = visualLifetimeSeconds,
                origin = origin,
                startPoint = startPoint,
                endPoint = endPoint,
                direction = direction
            };
        }
    }

    public static class CustomShapeEventCatalog
    {
        private static readonly CustomShapeEventDefinition[] Definitions =
        {
            new("line", "line_slash_damage", "절단 피해", CustomShapeEventRole.Effect, CustomShapeEventKind.SlashDamage, "반응: 절단 피해", usesDirection: true),
            new("arrow", "arrow_operator", "방향 사출", CustomShapeEventRole.Operator, CustomShapeEventKind.DirectionalProjectile, "연산자: 끝점 방향 사출", usesDirection: true, emitsFromEndPoint: true, operatorOnlyKind: CustomShapeEventKind.AttributeLaser, operatorTargetKind: CustomShapeEventKind.DirectionalProjectile),
            new("beamArrow", "beam_arrow_laser", "속성 레이저", CustomShapeEventRole.Effect, CustomShapeEventKind.AttributeLaser, "반응: 화살표 방향 속성 빛줄기", usesDirection: true, emitsFromEndPoint: true),
            new("rect", "rect_wall_entity", "벽 생성", CustomShapeEventRole.Effect, CustomShapeEventKind.WallEntity, "반응: 벽 구조물", visualPersistence: CustomShapeEventPersistence.Permanent),
            new("roundRect", "round_rect_guard_buff", "방어 버프", CustomShapeEventRole.Effect, CustomShapeEventKind.GuardBuff, "반응: 방어 버프"),
            new("ellipse", "ellipse_barrier", "배리어", CustomShapeEventRole.Effect, CustomShapeEventKind.Barrier, "반응: 배리어"),
            new("triangle", "triangle_trap", "함정", CustomShapeEventRole.Effect, CustomShapeEventKind.Trap, "반응: 함정 설치"),
            new("diamond", "diamond_piercing_mark", "관통 표식", CustomShapeEventRole.Effect, CustomShapeEventKind.PiercingMark, "반응: 관통 표식"),
            new("pentagon", "pentagon_guard_buff", "수호 버프", CustomShapeEventRole.Effect, CustomShapeEventKind.GuardBuff, "반응: 수호 버프"),
            new("hexagon", "hexagon_stun", "스턴", CustomShapeEventRole.Effect, CustomShapeEventKind.Stun, "반응: 스턴"),
            new("star", "star_magic_amplify", "마법 강화", CustomShapeEventRole.Effect, CustomShapeEventKind.MagicAmplify, "반응: 다음 마법 강화"),
            new("arc", "arc_special_attack", "특공 상승", CustomShapeEventRole.Effect, CustomShapeEventKind.SpecialAttackBoost, "반응: 특공 상승"),
            new("curve", "curve_projectile", "곡선 사출", CustomShapeEventRole.Effect, CustomShapeEventKind.CurveProjectile, "반응: 곡선 사출", usesDirection: true),
            new("wave", "wave_move_speed", "이동속도", CustomShapeEventRole.Effect, CustomShapeEventKind.MoveSpeedBuff, "반응: 이동속도"),
            new("brace", "brace_attack_buff", "공격력 버프", CustomShapeEventRole.Effect, CustomShapeEventKind.AttackBuff, "반응: 공격력 버프"),
            new("cross", "cross_operator", "버프 삭제", CustomShapeEventRole.Operator, CustomShapeEventKind.BuffDispel, "연산자: 겹친 반응 차단", blocksOverlappedEvent: true, operatorOnlyKind: CustomShapeEventKind.RandomBuffDispel, operatorTargetKind: CustomShapeEventKind.EventBlock)
        };

        public static IReadOnlyList<CustomShapeEventDefinition> All => Definitions;

        public static CustomShapeEventDefinition ForToken(string token)
        {
            token = NormalizeToken(token);
            return Definitions.FirstOrDefault(item => string.Equals(item.token, token, StringComparison.OrdinalIgnoreCase)) ?? Definitions[0];
        }

        public static bool TryGetDefinition(string token, out CustomShapeEventDefinition definition)
        {
            definition = Definitions.FirstOrDefault(item => string.Equals(item.token, token, StringComparison.OrdinalIgnoreCase));
            return definition != null;
        }

        public static string UiSummary(string token)
        {
            return ForToken(token).uiSummary;
        }

        public static CustomShapeEventPayload BuildPayload(
            string token,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            bool operatorOnly = true)
        {
            var definition = ForToken(token);
            var geometry = GestureGeometry.From(strokes);
            var eventKind = definition.IsOperator && operatorOnly
                ? definition.operatorOnlyKind
                : definition.eventKind;
            var origin = definition.emitsFromEndPoint ? geometry.endPoint : geometry.center;
            return new CustomShapeEventPayload
            {
                shapeToken = definition.token,
                eventId = EventIdFor(definition, eventKind),
                displayName = DisplayNameFor(definition, eventKind),
                role = definition.role,
                eventKind = eventKind,
                uiSummary = definition.uiSummary,
                usesDirection = definition.usesDirection,
                emitsFromEndPoint = definition.emitsFromEndPoint,
                operatorOnly = definition.IsOperator && operatorOnly,
                blocksEvent = definition.blocksOverlappedEvent,
                visualPersistence = VisualPersistenceFor(definition, eventKind),
                visualLifetimeSeconds = VisualLifetimeFor(definition, eventKind),
                origin = origin,
                startPoint = geometry.startPoint,
                endPoint = geometry.endPoint,
                direction = geometry.direction
            };
        }

        public static CustomShapeEventPayload BuildPayload(
            IReadOnlyList<string> tokens,
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            var normalized = (tokens ?? Array.Empty<string>())
                .Select(ForToken)
                .GroupBy(definition => definition.token, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            if (normalized.Count == 0)
            {
                return BuildPayload("line", strokes);
            }

            var operatorDefinition = normalized.FirstOrDefault(definition => definition.IsOperator);
            var targetDefinition = normalized.FirstOrDefault(definition => !definition.IsOperator);
            if (operatorDefinition == null || targetDefinition == null)
            {
                var single = normalized[0];
                var operatorOnly = !string.Equals(single.token, "arrow", StringComparison.OrdinalIgnoreCase);
                return BuildPayload(single.token, strokes, operatorOnly);
            }

            return ComposeWithOperator(
                operatorDefinition.token,
                targetDefinition.token,
                strokes,
                strokes,
                overlaps: true);
        }

        public static CustomShapeEventPayload ComposeWithOperator(
            string operatorToken,
            string targetToken,
            IReadOnlyList<IReadOnlyList<StrokeSample>> operatorStrokes,
            IReadOnlyList<IReadOnlyList<StrokeSample>> targetStrokes,
            bool overlaps)
        {
            var operatorDefinition = ForToken(operatorToken);
            var target = BuildPayload(targetToken, targetStrokes, operatorOnly: false);
            var op = BuildPayload(operatorToken, operatorStrokes, operatorOnly: true);
            if (!operatorDefinition.IsOperator || !overlaps)
            {
                return target;
            }

            if (operatorDefinition.blocksOverlappedEvent)
            {
                target.eventId = $"{operatorDefinition.token}_blocks_{target.shapeToken}";
                target.displayName = "반응 차단";
                target.eventKind = CustomShapeEventKind.EventBlock;
                target.eventBlocked = true;
                target.blockedByToken = operatorDefinition.token;
                target.blocksEvent = true;
                target.uiSummary = $"{operatorDefinition.displayName}: {target.displayName}";
                ApplyVisualPolicy(target, CustomShapeEventKind.EventBlock);
                return target;
            }

            target.eventId = $"{operatorDefinition.token}_casts_{target.shapeToken}";
            target.displayName = $"{target.displayName} 사출";
            target.eventKind = operatorDefinition.operatorTargetKind;
            target.usesDirection = true;
            target.emitsFromEndPoint = operatorDefinition.emitsFromEndPoint;
            target.origin = op.endPoint;
            target.startPoint = op.startPoint;
            target.endPoint = op.endPoint;
            target.direction = op.direction;
            target.uiSummary = $"{operatorDefinition.displayName}: {target.displayName}";
            ApplyVisualPolicy(target, operatorDefinition.operatorTargetKind);
            return target;
        }

        public static CustomShapeEventPersistence VisualPersistenceFor(CustomShapeEventKind eventKind)
        {
            return eventKind == CustomShapeEventKind.WallEntity
                ? CustomShapeEventPersistence.Permanent
                : CustomShapeEventPersistence.Timed;
        }

        public static float VisualLifetimeFor(CustomShapeEventKind eventKind)
        {
            return eventKind switch
            {
                CustomShapeEventKind.WallEntity => 0f,
                CustomShapeEventKind.SlashDamage => 1.0f,
                CustomShapeEventKind.DirectionalProjectile => 1.15f,
                CustomShapeEventKind.AttributeLaser => 1.25f,
                CustomShapeEventKind.CurveProjectile => 1.25f,
                CustomShapeEventKind.BuffDispel => 1.1f,
                CustomShapeEventKind.RandomBuffDispel => 1.1f,
                CustomShapeEventKind.EventBlock => 1.2f,
                CustomShapeEventKind.Stun => 2.4f,
                CustomShapeEventKind.PiercingMark => 2.8f,
                CustomShapeEventKind.Trap => 4.0f,
                CustomShapeEventKind.Barrier => 1.4f,
                CustomShapeEventKind.AttackBuff => 1.6f,
                CustomShapeEventKind.MoveSpeedBuff => 1.6f,
                CustomShapeEventKind.SpecialAttackBoost => 1.6f,
                CustomShapeEventKind.MagicAmplify => 1.6f,
                CustomShapeEventKind.GuardBuff => 1.6f,
                _ => 1.5f
            };
        }

        private static CustomShapeEventPersistence VisualPersistenceFor(CustomShapeEventDefinition definition, CustomShapeEventKind eventKind)
        {
            return eventKind == definition.eventKind ? definition.visualPersistence : VisualPersistenceFor(eventKind);
        }

        private static float VisualLifetimeFor(CustomShapeEventDefinition definition, CustomShapeEventKind eventKind)
        {
            return eventKind == definition.eventKind ? definition.visualLifetimeSeconds : VisualLifetimeFor(eventKind);
        }

        private static void ApplyVisualPolicy(CustomShapeEventPayload payload, CustomShapeEventKind eventKind)
        {
            payload.visualPersistence = VisualPersistenceFor(eventKind);
            payload.visualLifetimeSeconds = VisualLifetimeFor(eventKind);
        }

        private static string NormalizeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return "line";
            }

            return Definitions.FirstOrDefault(item => string.Equals(item.token, token, StringComparison.OrdinalIgnoreCase))?.token ?? "line";
        }

        private static string EventIdFor(CustomShapeEventDefinition definition, CustomShapeEventKind eventKind)
        {
            if (!definition.IsOperator || eventKind == definition.eventKind)
            {
                return definition.eventId;
            }

            return $"{definition.token}_{eventKind.ToString().ToLowerInvariant()}";
        }

        private static string DisplayNameFor(CustomShapeEventDefinition definition, CustomShapeEventKind eventKind)
        {
            if (!definition.IsOperator)
            {
                return definition.displayName;
            }

            return eventKind switch
            {
                CustomShapeEventKind.AttributeLaser => "속성 레이저",
                CustomShapeEventKind.RandomBuffDispel => "무작위 버프 삭제",
                CustomShapeEventKind.EventBlock => "반응 차단",
                CustomShapeEventKind.DirectionalProjectile => "방향 사출",
                _ => definition.displayName
            };
        }

        private readonly struct GestureGeometry
        {
            private GestureGeometry(Vector2 center, Vector2 startPoint, Vector2 endPoint, Vector2 direction)
            {
                this.center = center;
                this.startPoint = startPoint;
                this.endPoint = endPoint;
                this.direction = direction;
            }

            public readonly Vector2 center;
            public readonly Vector2 startPoint;
            public readonly Vector2 endPoint;
            public readonly Vector2 direction;

            public static GestureGeometry From(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
            {
                var clean = strokes?
                    .Select(stroke => stroke?.Where(sample => IsFinite(sample.position)).ToList() ?? new List<StrokeSample>())
                    .Where(stroke => stroke.Count > 0)
                    .ToList() ?? new List<List<StrokeSample>>();
                if (clean.Count == 0)
                {
                    return new GestureGeometry(Vector2.zero, Vector2.zero, Vector2.zero, Vector2.right);
                }

                var points = clean.SelectMany(stroke => stroke.Select(sample => sample.position)).ToList();
                var center = new Vector2(points.Average(point => point.x), points.Average(point => point.y));
                var start = clean[0][0].position;
                var endStroke = clean[^1];
                var end = endStroke[^1].position;
                var direction = end - start;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    var min = new Vector2(points.Min(point => point.x), points.Min(point => point.y));
                    var max = new Vector2(points.Max(point => point.x), points.Max(point => point.y));
                    direction = max - min;
                }

                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Vector2.right;
                }

                return new GestureGeometry(center, start, end, direction.normalized);
            }

            private static bool IsFinite(Vector2 value)
            {
                return float.IsFinite(value.x) && float.IsFinite(value.y);
            }
        }
    }
}

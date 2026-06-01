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
        WindPlatform
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
                SpellFamily.Life when IsEvent(spell, CustomShapeEventKind.DirectionalProjectile) && HasToken(spell, "rect") => For(CustomSpellEffectKind.LivingBridge),
                SpellFamily.Life when HasToken(spell, "brace") || IsEvent(spell, CustomShapeEventKind.AttackBuff) => For(CustomSpellEffectKind.Connection),
                SpellFamily.Wind when HasToken(spell, "rect") || IsEvent(spell, CustomShapeEventKind.WallEntity) => For(CustomSpellEffectKind.WindPlatform),
                SpellFamily.Earth when HasToken(spell, "rect") || IsEvent(spell, CustomShapeEventKind.WallEntity) => For(CustomSpellEffectKind.Stability),
                _ => For(CustomSpellEffectKind.None)
            };
        }

        public static CustomSpellEffectDefinition For(CustomSpellEffectKind kind)
        {
            return kind switch
            {
                CustomSpellEffectKind.Ice => new(kind, "얼음", "물 + 육각형", "물이 육각 결정으로 굳어 발판과 제압 효과를 만듭니다.", 28),
                CustomSpellEffectKind.Electric => new(kind, "전기", "불꽃 + 직선", "불꽃이 직선 경로를 타고 전류처럼 뻗습니다.", 34),
                CustomSpellEffectKind.Cleanse => new(kind, "정화", "물 + 원", "둥근 물막이 오염을 씻어 내고 상태를 안정시킵니다.", 18),
                CustomSpellEffectKind.Focus => new(kind, "집중", "불꽃 + 별", "별 모양 초점이 다음 타격이 모일 지점을 밝혀 줍니다.", 24),
                CustomSpellEffectKind.Flow => new(kind, "흐름", "바람 + 물결", "바람이 물결 경로를 따라 이동 흐름을 만듭니다.", 20),
                CustomSpellEffectKind.Connection => new(kind, "연결", "생명 + 중괄호", "생명 마법이 떨어진 대상을 묶는 연결감을 만듭니다.", 22),
                CustomSpellEffectKind.Stability => new(kind, "안정", "땅 + 사각형", "사각 구조물이 흔들리는 바닥을 받쳐 안정시킵니다.", 16),
                CustomSpellEffectKind.LivingBridge => new(kind, "생명 다리", "생명 + 화살표 + 사각형", "생명 마법이 사각 발판을 뻗어 낭떠러지를 잇습니다.", 0),
                CustomSpellEffectKind.WindPlatform => new(kind, "바람 발판", "바람 + 사각형", "바람이 사각 발판을 띄워 건너갈 길을 만듭니다.", 0),
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
            CustomShapeEventKind operatorTargetKind = CustomShapeEventKind.None)
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
            new("line", "line_slash_damage", "절단 피해", CustomShapeEventRole.Effect, CustomShapeEventKind.SlashDamage, "이벤트: 절단 피해", usesDirection: true),
            new("arrow", "arrow_operator", "방향 사출", CustomShapeEventRole.Operator, CustomShapeEventKind.DirectionalProjectile, "연산자: 끝점 방향 사출", usesDirection: true, emitsFromEndPoint: true, operatorOnlyKind: CustomShapeEventKind.AttributeLaser, operatorTargetKind: CustomShapeEventKind.DirectionalProjectile),
            new("rect", "rect_wall_entity", "벽 생성", CustomShapeEventRole.Effect, CustomShapeEventKind.WallEntity, "이벤트: 벽 구조물"),
            new("roundRect", "round_rect_guard_buff", "방어 버프", CustomShapeEventRole.Effect, CustomShapeEventKind.GuardBuff, "이벤트: 방어 버프"),
            new("ellipse", "ellipse_barrier", "배리어", CustomShapeEventRole.Effect, CustomShapeEventKind.Barrier, "이벤트: 배리어"),
            new("triangle", "triangle_trap", "함정", CustomShapeEventRole.Effect, CustomShapeEventKind.Trap, "이벤트: 함정 설치"),
            new("diamond", "diamond_piercing_mark", "관통 표식", CustomShapeEventRole.Effect, CustomShapeEventKind.PiercingMark, "이벤트: 관통 표식"),
            new("pentagon", "pentagon_guard_buff", "수호 버프", CustomShapeEventRole.Effect, CustomShapeEventKind.GuardBuff, "이벤트: 수호 버프"),
            new("hexagon", "hexagon_stun", "스턴", CustomShapeEventRole.Effect, CustomShapeEventKind.Stun, "이벤트: 스턴"),
            new("star", "star_magic_amplify", "마법 강화", CustomShapeEventRole.Effect, CustomShapeEventKind.MagicAmplify, "이벤트: 다음 마법 강화"),
            new("arc", "arc_special_attack", "특공 상승", CustomShapeEventRole.Effect, CustomShapeEventKind.SpecialAttackBoost, "이벤트: 특공 상승"),
            new("curve", "curve_projectile", "곡선 사출", CustomShapeEventRole.Effect, CustomShapeEventKind.CurveProjectile, "이벤트: 곡선 사출", usesDirection: true),
            new("wave", "wave_move_speed", "이동속도", CustomShapeEventRole.Effect, CustomShapeEventKind.MoveSpeedBuff, "이벤트: 이동속도"),
            new("brace", "brace_attack_buff", "공격력 버프", CustomShapeEventRole.Effect, CustomShapeEventKind.AttackBuff, "이벤트: 공격력 버프"),
            new("cross", "cross_operator", "버프 삭제", CustomShapeEventRole.Operator, CustomShapeEventKind.BuffDispel, "연산자: 겹친 이벤트 차단", blocksOverlappedEvent: true, operatorOnlyKind: CustomShapeEventKind.RandomBuffDispel, operatorTargetKind: CustomShapeEventKind.EventBlock)
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
                return BuildPayload(normalized[0].token, strokes, operatorOnly: true);
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
                target.displayName = "이벤트 차단";
                target.eventKind = CustomShapeEventKind.EventBlock;
                target.eventBlocked = true;
                target.blockedByToken = operatorDefinition.token;
                target.blocksEvent = true;
                target.uiSummary = $"{operatorDefinition.displayName}: {target.displayName}";
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
            return target;
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
                CustomShapeEventKind.EventBlock => "이벤트 차단",
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

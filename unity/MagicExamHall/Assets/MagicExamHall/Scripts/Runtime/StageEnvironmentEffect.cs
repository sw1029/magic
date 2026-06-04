using System;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    [CreateAssetMenu(menuName = "Magic Exam Hall/Stage Environment Effect", fileName = "StageEnvironmentEffect")]
    public sealed class StageEnvironmentEffect : ScriptableObject
    {
        public string effectId = "";
        public SpellFamily baseFamily;
        public CustomSpellEffectKind customEffect = CustomSpellEffectKind.None;
        public string displayName = "";
        public string requirementLabel = "";
        [TextArea]
        public string note = "";
        public string[] requiredShapeTokens = Array.Empty<string>();
        public CustomShapeEventKind[] acceptedEventKinds = Array.Empty<CustomShapeEventKind>();
        public StageEntityDefinition entity = new();
        public StageEffectVisualDefinition visual = new();

        public CustomSpellEffectDefinition ToRuntimeDefinition()
        {
            return new CustomSpellEffectDefinition(
                customEffect,
                string.IsNullOrWhiteSpace(displayName) ? CustomSpellEffectCatalog.Korean(customEffect) : displayName,
                string.IsNullOrWhiteSpace(requirementLabel) ? CustomSpellEffectCatalog.RequirementLabel(customEffect) : requirementLabel,
                string.IsNullOrWhiteSpace(note) ? CustomSpellEffectCatalog.For(customEffect).note : note,
                CustomSpellEffectCatalog.For(customEffect).impact);
        }

        public bool Matches(SpellFamily family, SpellResult spell)
        {
            if (spell == null || !spell.isCustomShape || family != baseFamily || customEffect == CustomSpellEffectKind.None)
            {
                return false;
            }

            if (acceptedEventKinds != null &&
                Enum.TryParse<CustomShapeEventKind>(spell.customEventKind, out var eventKind) &&
                acceptedEventKinds.Contains(eventKind))
            {
                return true;
            }

            return requiredShapeTokens != null &&
                   requiredShapeTokens.Length > 0 &&
                   requiredShapeTokens.All(token => ContainsToken(spell, token));
        }

        private static bool ContainsToken(SpellResult spell, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return true;
            }

            var needle = token.ToLowerInvariant();
            return Contains(spell.customShapeToken, needle) ||
                   Contains(spell.customShapeLabel, needle) ||
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

    [Serializable]
    public sealed class StageEntityDefinition
    {
        public string entityName = "";
        public PixelSpriteKind spriteKind = PixelSpriteKind.FloorTile;
        public Sprite spriteOverride = null!;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.white;
        public Vector2 offset;
        public Vector2 size = Vector2.one;
        public bool tiled = true;
        public int sortingOrder = -1;
        public bool hasCollider = true;
        public bool createsSteps;
        public int stepCount = 1;
        public Vector2 stepStartOffset;
        public Vector2 stepSpacing = Vector2.right;
        public Vector2 stepSize = Vector2.one;
    }

    [Serializable]
    public sealed class StageEffectVisualDefinition
    {
        public bool enabled = true;
        public bool showGroundGlow = true;
        public bool showEntityWake = true;
        public bool showAnchorGlyphs = true;
        public bool showEventSignature = true;
        public Color primaryColor = default;
        public Color secondaryColor = default;
        public Vector2 glowPadding = new(0.55f, 0.30f);
        public float glyphScale = 0.46f;
        public int sortingOrder = 7;
    }
}

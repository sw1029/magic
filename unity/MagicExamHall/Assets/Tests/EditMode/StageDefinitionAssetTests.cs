using System.Linq;
using MagicExamHall;
using NUnit.Framework;
using UnityEngine;

namespace MagicExamHall.Tests
{
    public sealed class StageDefinitionAssetTests
    {
        [Test]
        public void FloorThreeCrossingStageDefinitionLoadsWithFourInteractions()
        {
            var definition = Resources.Load<FloorStageDefinition>("StageDefinitions/Floor3Crossing");

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.floorNumber, Is.EqualTo(3));
            Assert.That(definition.obstacles.Select(item => item.requiredGoalId), Is.EquivalentTo(new[]
            {
                "frozen_river",
                "earth_stairs",
                "living_bridge",
                "wind_platform"
            }));
            Assert.That(definition.environmentEffects.Length, Is.EqualTo(4));
            Assert.That(definition.props.Count(item => item.hasCollider), Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void FloorThreeEnvironmentEffectsMapCustomShapesToPlatformSprites()
        {
            var definition = Resources.Load<FloorStageDefinition>("StageDefinitions/Floor3Crossing");

            Assert.That(definition.FindEffect(CustomSpellEffectKind.Ice, SpellFamily.Water).entity.spriteKind, Is.EqualTo(PixelSpriteKind.IceBridge));
            Assert.That(definition.FindEffect(CustomSpellEffectKind.Stability, SpellFamily.Earth).entity.createsSteps, Is.False);
            Assert.That(definition.FindEffect(CustomSpellEffectKind.Stability, SpellFamily.Earth).entity.spriteKind, Is.EqualTo(PixelSpriteKind.EarthStep));
            Assert.That(definition.FindEffect(CustomSpellEffectKind.LivingBridge, SpellFamily.Life).entity.spriteKind, Is.EqualTo(PixelSpriteKind.VineBridge));
            Assert.That(definition.FindEffect(CustomSpellEffectKind.WindPlatform, SpellFamily.Wind).entity.spriteKind, Is.EqualTo(PixelSpriteKind.WindPlatformTile));
        }
    }
}

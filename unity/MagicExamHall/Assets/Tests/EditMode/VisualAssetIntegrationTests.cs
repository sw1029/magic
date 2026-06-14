using System;
using System.Linq;
using MagicExamHall;
using NUnit.Framework;
using UnityEngine;

namespace MagicExamHall.Tests
{
    public sealed class VisualAssetIntegrationTests
    {
        [TestCase("Fonts/Galmuri11")]
        [TestCase("Fonts/Galmuri11-Bold")]
        [TestCase("Fonts/Galmuri14")]
        public void BundledKoreanFontsLoadFromResources(string resourcePath)
        {
            Assert.That(Resources.Load<Font>(resourcePath), Is.Not.Null, resourcePath);
        }

        [Test]
        public void EveryMentorMoodHasExternalPixelArt()
        {
            try
            {
                PixelArtFactory.ResetExternalSpriteCache();
                var mentorKinds = Enum.GetValues(typeof(PixelSpriteKind))
                    .Cast<PixelSpriteKind>()
                    .Where(kind => kind.ToString().StartsWith("Mentor", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(mentorKinds, Has.Length.EqualTo(15));
                foreach (var kind in mentorKinds)
                {
                    var sprite = PixelArtFactory.CreateSprite($"External {kind}", Color.magenta, Color.green, kind);
                    Assert.That(sprite, Is.Not.Null, kind.ToString());
                    Assert.That(sprite.texture.width, Is.EqualTo(16), kind.ToString());
                    Assert.That(sprite.texture.height, Is.EqualTo(32), kind.ToString());
                    Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Point), kind.ToString());
                }
            }
            finally
            {
                PixelArtFactory.ResetExternalSpriteCache();
            }
        }

        [Test]
        public void PlayerAnimationLibraryUsesCompleteExternalFrameSet()
        {
            try
            {
                PlayerSpriteLibrary.ResetCache();
                var sprites = PlayerSpriteLibrary.Load(Color.white, Color.blue);

                Assert.That(sprites.HasExternalFrames, Is.True);
                foreach (var facing in Enum.GetValues(typeof(PlayerFacing)).Cast<PlayerFacing>())
                {
                    Assert.That(sprites.GetFrameCount(PlayerAnimationState.Idle, facing), Is.EqualTo(2), facing.ToString());
                    Assert.That(sprites.GetFrameCount(PlayerAnimationState.Walk, facing), Is.EqualTo(4), facing.ToString());
                    AssertFrameSize(sprites.GetFrame(PlayerAnimationState.Idle, facing, 0));
                    AssertFrameSize(sprites.GetFrame(PlayerAnimationState.Walk, facing, 0));
                }

                Assert.That(sprites.GetFrameCount(PlayerAnimationState.CastCharge, PlayerFacing.Down), Is.EqualTo(3));
                Assert.That(sprites.GetFrameCount(PlayerAnimationState.CastRelease, PlayerFacing.Down), Is.EqualTo(2));
                AssertFrameSize(sprites.GetFrame(PlayerAnimationState.CastCharge, PlayerFacing.Down, 0));
                AssertFrameSize(sprites.GetFrame(PlayerAnimationState.CastRelease, PlayerFacing.Down, 0));
            }
            finally
            {
                PlayerSpriteLibrary.ResetCache();
            }
        }

        private static void AssertFrameSize(Sprite sprite)
        {
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.rect.width, Is.EqualTo(PlayerSpriteLibrary.FrameWidth));
            Assert.That(sprite.rect.height, Is.EqualTo(PlayerSpriteLibrary.FrameHeight));
        }
    }
}

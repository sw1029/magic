using MagicExamHall;
using NUnit.Framework;
using UnityEngine;

namespace MagicExamHall.Tests
{
    public sealed class PlayerMovementInputTests
    {
        [Test]
        public void ArrowKeysDriveMovementWhenAxisIsNeutral()
        {
            Assert.That(ExamGameController.BuildMovementInputForTests(0f, 0f, leftHeld: true, rightHeld: false, downHeld: false, upHeld: false), Is.EqualTo(Vector2.left));
            Assert.That(ExamGameController.BuildMovementInputForTests(0f, 0f, leftHeld: false, rightHeld: true, downHeld: false, upHeld: false), Is.EqualTo(Vector2.right));
            Assert.That(ExamGameController.BuildMovementInputForTests(0f, 0f, leftHeld: false, rightHeld: false, downHeld: true, upHeld: false), Is.EqualTo(Vector2.down));
            Assert.That(ExamGameController.BuildMovementInputForTests(0f, 0f, leftHeld: false, rightHeld: false, downHeld: false, upHeld: true), Is.EqualTo(Vector2.up));
        }

        [Test]
        public void DigitalMovementNormalizesDiagonalsAndCancelsOppositeKeys()
        {
            var diagonal = ExamGameController.BuildMovementInputForTests(0f, 0f, leftHeld: false, rightHeld: true, downHeld: false, upHeld: true);

            Assert.That(diagonal.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(diagonal.x, Is.GreaterThan(0.70f));
            Assert.That(diagonal.y, Is.GreaterThan(0.70f));
            Assert.That(ExamGameController.BuildMovementInputForTests(0f, 0f, leftHeld: true, rightHeld: true, downHeld: false, upHeld: false), Is.EqualTo(Vector2.zero));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using MagicExamHall;
using NUnit.Framework;
using UnityEngine;

namespace MagicExamHall.Tests
{
    public sealed class GestureRecognizerTests
    {
        [TestCase(SpellFamily.Wind)]
        [TestCase(SpellFamily.Earth)]
        [TestCase(SpellFamily.Fire)]
        [TestCase(SpellFamily.Water)]
        [TestCase(SpellFamily.Life)]
        public void CanonicalSamplesRecognizeTheirFamilies(SpellFamily family)
        {
            var strokes = GestureRecognizer.CreateCanonicalSamples(family);
            var result = GestureRecognizer.Recognize(strokes, family);

            Assert.That(result.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.recognizedFamily, Is.EqualTo(family));
            Assert.That(result.success, Is.True);
            Assert.That(result.confidence, Is.GreaterThan(0.7f));
        }

        [TestCase(OverlayOperator.SteelBrace)]
        [TestCase(OverlayOperator.ElectricFork)]
        [TestCase(OverlayOperator.IceBar)]
        [TestCase(OverlayOperator.SoulDot)]
        [TestCase(OverlayOperator.VoidCut)]
        [TestCase(OverlayOperator.MartialAxis)]
        public void CanonicalOverlaySamplesRecognizeTheirOperators(OverlayOperator op)
        {
            var seal = CreateWorldSeal(op == OverlayOperator.MartialAxis ? new[] { OverlayOperator.VoidCut } : new OverlayOperator[0]);
            var strokes = OverlayRecognizer.CreateCanonicalSamples(op, seal.worldCenter, seal.worldScale * 0.24f);
            var result = OverlayRecognizer.Recognize(strokes, seal);

            Assert.That(
                result.status,
                Is.EqualTo(RecognitionStatus.Recognized),
                $"score={result.score:0.000}, shape={result.shapeConfidence:0.000}, scale={result.scaleRatio:0.000}, anchor={result.anchorZone}, reason={result.feedbackReason}");
            Assert.That(result.recognizedOperator, Is.EqualTo(op));
            Assert.That(result.success, Is.True);
            Assert.That(result.shapeConfidence, Is.GreaterThan(0.48f));
        }

        [Test]
        public void MartialAxisRequiresVoidCutInSealStack()
        {
            var seal = CreateWorldSeal();
            var strokes = OverlayRecognizer.CreateCanonicalSamples(OverlayOperator.MartialAxis, seal.worldCenter, seal.worldScale * 0.24f);
            var result = OverlayRecognizer.Recognize(strokes, seal);

            Assert.That(result.status, Is.Not.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.success, Is.False);
            Assert.That(result.recognizedOperator, Is.EqualTo(OverlayOperator.MartialAxis));
            Assert.That(result.feedbackReason, Does.Contain("절단 장식").And.Contain("축"));
            Assert.That(result.feedbackReason, Does.Not.Contain("void_cut"));
        }

        [Test]
        public void TinyOverlayExplainsScaleMismatch()
        {
            var seal = CreateWorldSeal();
            var strokes = OverlayRecognizer.CreateCanonicalSamples(OverlayOperator.IceBar, seal.worldCenter, seal.worldScale * 0.03f);
            var result = OverlayRecognizer.Recognize(strokes, seal);

            Assert.That(result.status, Is.Not.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.success, Is.False);
            Assert.That(result.recognizedOperator, Is.EqualTo(OverlayOperator.IceBar));
            Assert.That(result.scaleHint, Is.EqualTo(OverlayScaleHint.TooSmall));
            Assert.That(result.feedbackReason, Does.Contain("너무 작"));
        }

        [Test]
        public void DefaultSealLifetimeLeavesOverlaySetupTime()
        {
            var seal = CreateWorldSeal();

            Assert.That(seal.expiresAt - seal.createdAt, Is.EqualTo(SpellRuntime.DefaultSealDurationSeconds).Within(0.001f));
            Assert.That(SpellRuntime.DefaultSealDurationSeconds, Is.GreaterThanOrEqualTo(10f));
        }

        [Test]
        public void PixelArtFactoryCreatesProceduralMentorSprites()
        {
            PixelArtFactory.ResetExternalSpriteCache();

            foreach (var kind in System.Enum.GetValues(typeof(PixelSpriteKind)).Cast<PixelSpriteKind>().Where(kind => kind.ToString().StartsWith("Mentor")))
            {
                var sprite = PixelArtFactory.CreateSprite($"procedural-sentinel-{kind}", Color.magenta, Color.green, kind);

                Assert.That(sprite, Is.Not.Null, kind.ToString());
                Assert.That(sprite.texture.width, Is.EqualTo(32), kind.ToString());
                Assert.That(sprite.texture.height, Is.EqualTo(32), kind.ToString());
                Assert.That(sprite.texture.name, Does.StartWith($"procedural-sentinel-{kind}"), kind.ToString());
            }
        }

        [Test]
        public void PlayerSpriteLibraryReloadsDestroyedCachedFrames()
        {
            try
            {
                PlayerSpriteLibrary.ResetCache();
                var firstSet = PlayerSpriteLibrary.Load(new Color(0.95f, 0.92f, 0.78f), new Color(0.28f, 0.62f, 0.96f));
                var firstFrame = firstSet.GetFrame(PlayerAnimationState.Idle, PlayerFacing.Down, 0);
                UnityEngine.Object.DestroyImmediate(firstFrame);

                var reloaded = PlayerSpriteLibrary.Load(new Color(0.95f, 0.92f, 0.78f), new Color(0.28f, 0.62f, 0.96f));
                var reloadedFrame = reloaded.GetFrame(PlayerAnimationState.Idle, PlayerFacing.Down, 0);

                Assert.That(reloadedFrame, Is.Not.Null);
                Assert.That(reloadedFrame.rect.width, Is.EqualTo(PlayerSpriteLibrary.FrameWidth));
                Assert.That(reloadedFrame.rect.height, Is.EqualTo(PlayerSpriteLibrary.FrameHeight));
            }
            finally
            {
                PlayerSpriteLibrary.ResetCache();
            }
        }

        [Test]
        public void PixelSpriteViewAppliesRuntimeTint()
        {
            var body = new GameObject("Tinted Pulse Test");
            try
            {
                body.AddComponent<SpriteRenderer>();
                var view = body.AddComponent<PixelSpriteView>();
                var tint = new Color(0.25f, 0.75f, 1f, 0.6f);
                view.kind = PixelSpriteKind.Pulse;
                view.rendererTint = tint;

                view.Apply();

                Assert.That(body.GetComponent<SpriteRenderer>().color, Is.EqualTo(tint));
            }
            finally
            {
                Object.DestroyImmediate(body);
            }
        }

        [Test]
        public void AudioDirectorCreatesRequiredProceduralCues()
        {
            var body = new GameObject("Audio Director Test");
            try
            {
                var director = body.AddComponent<AudioDirector>();

                director.Initialize();

                Assert.That(director.SfxClipCountForTests, Is.EqualTo(System.Enum.GetValues(typeof(AudioCue)).Length));
                Assert.That(director.BgmClipCountForTests, Is.EqualTo(2));
                director.PlayBgm(BgmCue.AmbientTower);
                Assert.That(director.CurrentBgmForTests, Is.EqualTo(BgmCue.AmbientTower));
                director.PlayBgm(BgmCue.None);
                Assert.That(director.CurrentBgmForTests, Is.EqualTo(BgmCue.None));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(body);
            }
        }

        [Test]
        public void AccessibilitySettingsClampAndExposeInputChoices()
        {
            var originalSensitivity = MagicExamSettings.MouseSensitivity;
            var originalTextScale = MagicExamSettings.TextScale;
            var originalMovement = MagicExamSettings.MovementPreset;
            var originalSwap = MagicExamSettings.SwapMouseButtons;
            try
            {
                MagicExamSettings.MouseSensitivity = 9f;
                MagicExamSettings.TextScale = 9f;
                MagicExamSettings.MovementPreset = 99;
                MagicExamSettings.SwapMouseButtons = true;

                Assert.That(MagicExamSettings.MouseSensitivity, Is.EqualTo(1.75f).Within(0.001f));
                Assert.That(MagicExamSettings.TextScale, Is.EqualTo(1.5f).Within(0.001f));
                Assert.That(MagicExamSettings.MovementPreset, Is.EqualTo(2));
                Assert.That(MagicExamSettings.DrawMouseButton, Is.EqualTo(0));
                Assert.That(MagicExamSettings.MovementPresetLabel, Is.EqualTo("IJKL"));
            }
            finally
            {
                MagicExamSettings.MouseSensitivity = originalSensitivity;
                MagicExamSettings.TextScale = originalTextScale;
                MagicExamSettings.MovementPreset = originalMovement;
                MagicExamSettings.SwapMouseButtons = originalSwap;
            }
        }

        [Test]
        public void OpenTriangleIsIncompleteInsteadOfFalsePositive()
        {
            var stroke = new List<StrokeSample>
            {
                new(new Vector2(220, 70), 0f),
                new(new Vector2(400, 390), 0.12f),
                new(new Vector2(80, 390), 0.24f)
            };

            var result = GestureRecognizer.Recognize(new List<List<StrokeSample>> { stroke }, SpellFamily.Fire);

            Assert.That(result.status, Is.Not.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("불꽃").And.Contain("틈"));
            Assert.That(result.nextHint, Does.Contain("마지막 선").And.Contain("삼각형"));
        }

        [Test]
        public void SlightlyGappedWaterCircleCountsAsClosed()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeEllipseArc(25f, 360f, 64, new Vector2(220f, 220f), 170f, 150f, 0f)
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Water);

            Assert.That(result.quality.closure, Is.LessThan(0.62f).And.GreaterThanOrEqualTo(0.50f));
            Assert.That(
                result.status,
                Is.EqualTo(RecognitionStatus.Recognized),
                $"closure={result.quality.closure:0.000}, confidence={result.confidence:0.000}, reason={result.feedbackReason}");
            Assert.That(result.recognizedFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(result.success, Is.True);
        }

        [Test]
        public void ColdStartWaterIntentAcceptsSlightlyGappedCircle()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeEllipseArc(25f, 360f, 64, new Vector2(220f, 220f), 170f, 150f, 0f)
            };
            var intent = new BaseRecognitionIntent
            {
                family = SpellFamily.Water,
                goalId = "puddle",
                source = "near_goal_symbol",
                radius = 2f,
                strength = 1f,
                tutorialCaptureCount = 0,
                strongConsiderationEnabled = true
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Water, intent);

            Assert.That(
                result.status,
                Is.EqualTo(RecognitionStatus.Recognized),
                $"closure={result.quality.closure:0.000}, confidence={result.confidence:0.000}, reason={result.feedbackReason}");
            Assert.That(result.recognizedFamily, Is.EqualTo(SpellFamily.Water));
        }

        [Test]
        public void ColdStartWaterIntentKeepsWideGapIncomplete()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeEllipseArc(70f, 360f, 56, new Vector2(220f, 220f), 170f, 150f, 0f)
            };
            var intent = new BaseRecognitionIntent
            {
                family = SpellFamily.Water,
                goalId = "puddle",
                source = "near_goal_symbol",
                radius = 2f,
                strength = 1f,
                tutorialCaptureCount = 0,
                strongConsiderationEnabled = true
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Water, intent);

            Assert.That(result.status, Is.Not.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.recognizedFamily, Is.Null);
        }

        [Test]
        public void WaterLikeOvalLoopDoesNotResolveAsEarth()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeEllipseArc(-8f, 355f, 56, new Vector2(220f, 220f), 180f, 105f, 0f)
            };

            var result = SpellRuntime.RecognizeBase(strokes);

            Assert.That(
                result.spell.recognizedFamily,
                Is.Not.EqualTo(SpellFamily.Earth),
                $"target={result.spell.targetFamily}, status={result.spell.status}, confidence={result.spell.confidence:0.000}, reason={result.spell.feedbackReason}");
        }

        [Test]
        public void ColdStartWaterIntentKeepsOvalWaterInputOnWater()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeEllipseArc(-8f, 355f, 56, new Vector2(220f, 220f), 180f, 105f, 0f)
            };
            var intent = new BaseRecognitionIntent
            {
                family = SpellFamily.Water,
                goalId = "puddle",
                source = "near_goal_symbol",
                radius = 2f,
                strength = 1f,
                tutorialCaptureCount = 0,
                strongConsiderationEnabled = true
            };

            var result = SpellRuntime.RecognizeBase(strokes, intent);

            Assert.That(
                result.spell.recognizedFamily,
                Is.EqualTo(SpellFamily.Water),
                $"target={result.spell.targetFamily}, status={result.spell.status}, confidence={result.spell.confidence:0.000}, preIntent={result.spell.preIntentFamily}");
        }

        [Test]
        public void ColdStartWaterIntentStillAcceptsClosedCanonicalCircle()
        {
            var strokes = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Water, timeStep: 0.03f);
            var intent = new BaseRecognitionIntent
            {
                family = SpellFamily.Water,
                goalId = "puddle",
                source = "near_goal_symbol",
                radius = 2f,
                strength = 1f,
                tutorialCaptureCount = 0,
                strongConsiderationEnabled = true
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Water, intent);

            Assert.That(result.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.recognizedFamily, Is.EqualTo(SpellFamily.Water));
        }

        [Test]
        public void TwoLineWindIsIncomplete()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeLine(70, 150, 390, 145, 0f),
                MakeLine(70, 240, 390, 235, 0.2f)
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Wind);

            Assert.That(result.status, Is.EqualTo(RecognitionStatus.Incomplete).Or.EqualTo(RecognitionStatus.Ambiguous));
            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("3획").And.Contain("위, 가운데, 아래"));
            Assert.That(result.nextHint, Does.Contain("세 번").And.Contain("평행선"));
        }

        [Test]
        public void UnevenWindLinesExplainSpacing()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeLine(70, 120, 390, 118, 0f),
                MakeLine(70, 150, 390, 148, 0.2f),
                MakeLine(70, 330, 390, 328, 0.4f)
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Wind);

            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("간격"));
            Assert.That(result.nextHint, Does.Contain("간격").And.Contain("비슷"));
        }

        [Test]
        public void ExtraWindStrokeRemainsIncompleteWithActionHint()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeLine(70, 120, 390, 118, 0f),
                MakeLine(70, 190, 390, 188, 0.2f),
                MakeLine(70, 260, 390, 258, 0.4f),
                MakeLine(70, 330, 390, 328, 0.6f)
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Wind);

            Assert.That(result.status, Is.EqualTo(RecognitionStatus.Incomplete));
            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("세 줄").And.Contain("획이 많"));
            Assert.That(result.nextHint, Does.Contain("추가 선").And.Contain("3획"));
        }

        [Test]
        public void LifeFailureDistinguishesStemAndBranches()
        {
            var strokes = new List<List<StrokeSample>>
            {
                MakeLine(220, 80, 220, 360, 0f)
            };

            var result = GestureRecognizer.Recognize(strokes, SpellFamily.Life);

            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("줄기").And.Contain("가지"));
            Assert.That(result.nextHint, Does.Contain("가운데 줄기").And.Contain("왼쪽 가지").And.Contain("오른쪽 가지"));
        }

        [Test]
        public void SuccessfulLifeKeepsPositiveNextHint()
        {
            var result = GestureRecognizer.Recognize(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Life), SpellFamily.Life);

            Assert.That(result.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(result.success, Is.True);
            Assert.That(result.nextHint, Does.Contain("좋습니다"));
            Assert.That(result.nextHint, Does.Not.Contain("가지가 갈라지게"));
        }

        [Test]
        public void EmptyBaseFailureUsesPlayerFacingCopy()
        {
            var result = GestureRecognizer.Recognize(new List<List<StrokeSample>>(), SpellFamily.Water);

            Assert.That(result.success, Is.False);
            Assert.That(result.feedbackReason, Does.Contain("바닥").And.Contain("선"));
            Assert.That(result.nextHint, Does.Contain("오른쪽 마우스"));
            Assert.That(result.feedbackReason, Does.Not.Contain("No stroke"));
        }

        [Test]
        public void FastAndSlowFireKeepFamilyButChangeTempo()
        {
            var fast = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, timeStep: 0.01f);
            var slow = GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, timeStep: 0.08f);

            var fastResult = GestureRecognizer.Recognize(fast, SpellFamily.Fire);
            var slowResult = GestureRecognizer.Recognize(slow, SpellFamily.Fire);

            Assert.That(fastResult.recognizedFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(slowResult.recognizedFamily, Is.EqualTo(SpellFamily.Fire));
            Assert.That(fastResult.quality.tempo, Is.GreaterThan(slowResult.quality.tempo + 0.12f));
        }

        [Test]
        public void LoggerWritesAttemptAndSurveyFiles()
        {
            var sessionId = "logger-schema-" + System.Guid.NewGuid().ToString("N");
            var logger = CreateEnabledLoggerForTest(sessionId);
            logger.LogAttempt(new AttemptLog
            {
                sessionId = sessionId,
                trialId = "1-1",
                targetFamily = "fire",
                recognizedFamily = "fire",
                status = RecognitionStatus.Recognized.ToString(),
                confidence = 0.9f,
                closure = 1f,
                smoothness = 0.8f,
                tempo = 0.7f,
                stability = 0.9f,
                rotationBias = 0.1f,
                attemptIndex = 1,
                elapsedMs = 1200,
                feedbackViewed = true,
                success = true,
                hintShown = true,
                assistLevel = 2,
                assisted = true
            });
            logger.LogSurvey(new SurveyLog
            {
                sessionId = sessionId,
                clarity = 4,
                fairness = 4,
                feedbackHelpfulness = 5,
                controlFeeling = 4,
                immersion = 5,
                comment = "clear",
                completedTrials = 5,
                totalAttempts = 6
            });

            Assert.That(System.IO.File.Exists(System.IO.Path.Combine(logger.OutputDirectory, "attempts.csv")), Is.True);
            Assert.That(System.IO.File.Exists(System.IO.Path.Combine(logger.OutputDirectory, "survey.csv")), Is.True);
            var attemptsCsv = System.IO.File.ReadAllText(System.IO.Path.Combine(logger.OutputDirectory, "attempts.csv"));
            Assert.That(attemptsCsv, Does.Contain("phase,baseFamily,overlayStack,sealId,floorId,targetObject,worldEffect"));
            Assert.That(attemptsCsv, Does.Contain("hintShown,assistLevel,assisted"));
            AssertLastAttemptCsvFields(logger, success: true, hintShown: true, assistLevel: 2, assisted: true);
        }

        [Test]
        public void LoggerWritesCumulativeGqmHciResultFiles()
        {
            var sessionId = "logger-result-" + System.Guid.NewGuid().ToString("N");
            var logger = CreateEnabledLoggerForTest(sessionId);

            logger.LogAttempt(new AttemptLog
            {
                sessionId = sessionId,
                trialId = "1",
                phase = SpellPhase.Base.ToString(),
                floorId = "1",
                targetObject = "puddle",
                recognizedFamily = "earth",
                status = RecognitionStatus.Ambiguous.ToString(),
                confidence = 0.56f,
                closure = 0.42f,
                smoothness = 0.68f,
                tempo = 0.72f,
                stability = 0.58f,
                rotationBias = 0.16f,
                elapsedMs = 9000,
                attemptIndex = 1,
                hintShown = true,
                assistLevel = 1,
                success = false
            });
            logger.LogAttempt(new AttemptLog
            {
                sessionId = sessionId,
                trialId = "2",
                phase = SpellPhase.Base.ToString(),
                floorId = "1",
                targetObject = "puddle",
                recognizedFamily = "water",
                status = RecognitionStatus.Recognized.ToString(),
                confidence = 0.88f,
                closure = 0.92f,
                smoothness = 0.84f,
                tempo = 0.80f,
                stability = 0.86f,
                rotationBias = 0.08f,
                elapsedMs = 18000,
                attemptIndex = 2,
                success = true
            });
            logger.LogQuestChecklist(new QuestChecklistLog
            {
                sessionId = sessionId,
                floorId = "1",
                floorTitle = "First floor",
                reason = "floor_change",
                completed = 5,
                total = 5,
                globalCompleted = 5,
                globalTotal = 5,
                elapsedMs = 22000,
                items = "puddle:done"
            });
            logger.LogAttempt(new AttemptLog
            {
                sessionId = sessionId,
                trialId = "3",
                phase = SpellPhase.Overlay.ToString(),
                floorId = "2",
                targetObject = "custom_water",
                recognizedFamily = "ice",
                status = RecognitionStatus.Recognized.ToString(),
                confidence = 0.78f,
                smoothness = 0.74f,
                stability = 0.82f,
                elapsedMs = 11000,
                attemptIndex = 3,
                success = true
            });
            logger.LogQuestChecklist(new QuestChecklistLog
            {
                sessionId = sessionId,
                floorId = "2",
                floorTitle = "Second floor",
                reason = "ending",
                completed = 3,
                total = 5,
                globalCompleted = 8,
                globalTotal = 10,
                elapsedMs = 19000,
                items = "custom_water:done"
            });

            var result = logger.WriteSessionResult(new SessionResultContextLog
            {
                sessionId = sessionId,
                buildVersion = ExamGameController.BuildVersion,
                generatedAtUtc = "2026-06-12T00:00:00.0000000Z",
                floorCount = 2,
                floorTitles = new[] { "First floor", "Second floor" },
                totalElapsedMs = 41000,
                trueEnding = true,
                completedFinalGoals = 6,
                totalFinalGoals = 6,
                discoveryCount = 2
            });

            Assert.That(result, Is.Not.Null);
            Assert.That(result.floorCount, Is.EqualTo(2));
            Assert.That(result.totalAttempts, Is.EqualTo(3));
            Assert.That(result.floors.Length, Is.EqualTo(2));
            Assert.That(result.floors[0].floorId, Is.EqualTo(1));
            Assert.That(result.floors[0].failures, Is.EqualTo(1));
            Assert.That(result.floors[1].floorId, Is.EqualTo(2));
            Assert.That(System.IO.File.Exists(logger.SessionResultJsonPath), Is.True);
            Assert.That(System.IO.File.Exists(logger.SessionResultCsvPath), Is.True);
            Assert.That(System.IO.File.Exists(logger.FloorResultsCsvPath), Is.True);

            var resultJson = System.IO.File.ReadAllText(logger.SessionResultJsonPath);
            var floorCsv = System.IO.File.ReadAllText(logger.FloorResultsCsvPath);
            var globalCsv = System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Directory.GetParent(logger.OutputDirectory)!.FullName, "session-results.csv"));
            Assert.That(resultJson, Does.Contain("gqm-hci-v1"));
            Assert.That(resultJson, Does.Contain("gqmA1ShapeDifficultyScore"));
            Assert.That(floorCsv, Does.Contain("floorId,floorTitle"));
            Assert.That(floorCsv, Does.Contain("\"First floor\""));
            Assert.That(floorCsv, Does.Contain("\"Second floor\""));
            Assert.That(globalCsv, Does.Contain(sessionId));
        }

        [Test]
        public void LoggerSuppressesDefaultTestSessionCollection()
        {
            var prefixedSessionId = "test-session-" + System.Guid.NewGuid().ToString("N");
            var prefixedLogger = new ExamLogger(prefixedSessionId);

            prefixedLogger.LogAttempt(new AttemptLog
            {
                sessionId = prefixedSessionId,
                trialId = "1-1",
                recognizedFamily = "fire",
                status = RecognitionStatus.Recognized.ToString(),
                success = true
            });
            prefixedLogger.LogSurvey(new SurveyLog
            {
                sessionId = prefixedSessionId,
                clarity = 5,
                fairness = 5,
                feedbackHelpfulness = 5,
                controlFeeling = 5,
                immersion = 5
            });

            Assert.That(prefixedLogger.IsCollectionEnabled, Is.False);
            Assert.That(prefixedLogger.OutputDirectory, Is.EqualTo(ExamLogger.DisabledOutputDirectory));
            Assert.That(System.IO.Directory.Exists(System.IO.Path.Combine(Application.persistentDataPath, "MagicExamHallLogs", prefixedSessionId)), Is.False);

            var runnerSessionId = "logger-runner-" + System.Guid.NewGuid().ToString("N");
            var runnerLogger = new ExamLogger(runnerSessionId);

            runnerLogger.LogAttempt(new AttemptLog
            {
                sessionId = runnerSessionId,
                trialId = "1-2",
                recognizedFamily = "water",
                status = RecognitionStatus.Recognized.ToString(),
                success = true
            });

            Assert.That(runnerLogger.IsCollectionEnabled, Is.False);
            Assert.That(runnerLogger.OutputDirectory, Is.EqualTo(ExamLogger.DisabledOutputDirectory));
            Assert.That(System.IO.Directory.Exists(System.IO.Path.Combine(Application.persistentDataPath, "MagicExamHallLogs", runnerSessionId)), Is.False);
        }

        [Test]
        public void RepeatedFailuresEscalateAssistLevel()
        {
            var failedResult = GestureRecognizer.Recognize(new List<List<StrokeSample>>(), SpellFamily.Fire);

            var firstFailure = HintAssistance.ForAttempt(SpellFamily.Fire, 0, false, failedResult);
            var secondFailure = HintAssistance.ForAttempt(SpellFamily.Fire, 1, false, failedResult);
            var thirdFailure = HintAssistance.ForAttempt(SpellFamily.Fire, 2, false, failedResult);
            var laterFailure = HintAssistance.ForAttempt(SpellFamily.Fire, 5, false, failedResult);

            Assert.That(firstFailure.currentLevel, Is.EqualTo(AssistLevel.ReasonHint));
            Assert.That(secondFailure.currentLevel, Is.EqualTo(AssistLevel.Checklist));
            Assert.That(thirdFailure.currentLevel, Is.EqualTo(AssistLevel.GhostTrace));
            Assert.That(laterFailure.currentLevel, Is.EqualTo(AssistLevel.GhostTrace));
        }

        [TestCase(0, AssistLevel.ReasonHint, false)]
        [TestCase(1, AssistLevel.Checklist, true)]
        [TestCase(2, AssistLevel.GhostTrace, true)]
        [TestCase(7, AssistLevel.GhostTrace, true)]
        public void FailureEscalationStateCarriesStableMetadata(int priorFailures, AssistLevel expectedLevel, bool expectedAssisted)
        {
            var failedResult = GestureRecognizer.Recognize(new List<List<StrokeSample>>(), SpellFamily.Wind);
            var state = HintAssistance.ForAttempt(SpellFamily.Wind, priorFailures, false, failedResult);

            Assert.That(state.family, Is.EqualTo(SpellFamily.Wind));
            Assert.That(state.failureCount, Is.EqualTo(priorFailures));
            Assert.That(state.currentLevel, Is.EqualTo(expectedLevel));
            Assert.That(state.AssistLevelNumber, Is.EqualTo((int)expectedLevel));
            Assert.That(state.hintShown, Is.True);
            Assert.That(state.assisted, Is.EqualTo(expectedAssisted));
            Assert.That(state.body, Is.Not.Empty);

            if (expectedLevel == AssistLevel.ReasonHint)
            {
                Assert.That(state.body, Is.EqualTo(failedResult.nextHint));
            }
            else if (expectedLevel == AssistLevel.Checklist)
            {
                foreach (var checklistItem in HintAssistance.ChecklistFor(SpellFamily.Wind))
                {
                    Assert.That(state.body, Does.Contain(checklistItem));
                }
            }
            else
            {
                Assert.That(state.body, Is.Not.EqualTo(failedResult.nextHint));
                Assert.That(state.body, Does.Not.Contain(" · "));
            }
        }

        [TestCase(0, AssistLevel.None, false, false)]
        [TestCase(1, AssistLevel.ReasonHint, true, true)]
        [TestCase(2, AssistLevel.Checklist, true, true)]
        [TestCase(5, AssistLevel.GhostTrace, true, true)]
        public void SuccessAssistStateReflectsPriorFailures(int priorFailures, AssistLevel expectedLevel, bool expectedHintShown, bool expectedAssisted)
        {
            var successfulResult = GestureRecognizer.Recognize(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Life), SpellFamily.Life);
            var state = HintAssistance.ForAttempt(SpellFamily.Life, priorFailures, true, successfulResult);

            Assert.That(state.currentLevel, Is.EqualTo(expectedLevel));
            Assert.That(state.hintShown, Is.EqualTo(expectedHintShown));
            Assert.That(state.assisted, Is.EqualTo(expectedAssisted));
            Assert.That(state.failureCount, Is.EqualTo(priorFailures));
            Assert.That(state.body, Is.Not.Empty);
        }

        [Test]
        public void ReasonHintFallsBackWhenRecognitionHintIsMissing()
        {
            var resultWithoutHint = new SpellResult
            {
                status = RecognitionStatus.Invalid,
                targetFamily = SpellFamily.Earth,
                nextHint = ""
            };

            var state = HintAssistance.ForAttempt(SpellFamily.Earth, 0, false, resultWithoutHint);

            Assert.That(state.currentLevel, Is.EqualTo(AssistLevel.ReasonHint));
            Assert.That(state.body, Does.Contain("사다리꼴"));
        }

        [TestCase(SpellFamily.Fire, "삼각형")]
        [TestCase(SpellFamily.Water, "원")]
        [TestCase(SpellFamily.Wind, "3획")]
        [TestCase(SpellFamily.Earth, "사다리꼴")]
        [TestCase(SpellFamily.Life, "가지")]
        public void RepeatedFailureCopyEscalatesWithFamilySpecificActions(SpellFamily family, string expectedWord)
        {
            var failedResult = GestureRecognizer.Recognize(new List<List<StrokeSample>>(), family);

            var checklist = HintAssistance.ForAttempt(family, 1, false, failedResult);
            var strong = HintAssistance.ForAttempt(family, 2, false, failedResult);

            Assert.That(checklist.body, Does.Contain(expectedWord));
            Assert.That(strong.body, Does.Contain(expectedWord));
            Assert.That(checklist.body, Is.Not.EqualTo(strong.body));
            Assert.That(checklist.body, Does.Not.Contain("closure"));
            Assert.That(checklist.body, Does.Not.Contain("Incomplete"));
            Assert.That(checklist.body, Does.Not.Contain("Invalid"));
            Assert.That(strong.body, Does.Not.Contain("closure"));
            Assert.That(strong.body, Does.Not.Contain("Incomplete"));
            Assert.That(strong.body, Does.Not.Contain("Invalid"));
        }

        [Test]
        public void SuccessAfterAssistIsLoggedAsAssisted()
        {
            var successfulResult = GestureRecognizer.Recognize(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire), SpellFamily.Fire);
            var hintState = HintAssistance.ForAttempt(SpellFamily.Fire, 2, true, successfulResult);

            Assert.That(hintState.assisted, Is.True);
            Assert.That(hintState.hintShown, Is.True);
            Assert.That(hintState.currentLevel, Is.EqualTo(AssistLevel.Checklist));

            var sessionId = "assist-success-" + System.Guid.NewGuid().ToString("N");
            var logger = CreateEnabledLoggerForTest(sessionId);
            logger.LogAttempt(new AttemptLog
            {
                sessionId = sessionId,
                trialId = "1-3",
                targetFamily = "fire",
                recognizedFamily = "fire",
                status = successfulResult.status.ToString(),
                confidence = successfulResult.confidence,
                closure = successfulResult.quality.closure,
                smoothness = successfulResult.quality.smoothness,
                tempo = successfulResult.quality.tempo,
                stability = successfulResult.quality.stability,
                rotationBias = successfulResult.quality.rotationBias,
                attemptIndex = 3,
                elapsedMs = 3000,
                feedbackViewed = true,
                success = true,
                hintShown = hintState.hintShown,
                assistLevel = hintState.AssistLevelNumber,
                assisted = hintState.assisted
            });

            AssertLastAttemptCsvFields(logger, success: true, hintShown: true, assistLevel: 2, assisted: true);
        }

        [Test]
        public void EndingReportSeparatesShownHintsFromAssistedSuccess()
        {
            var report = new EndingReport();
            var failedResult = GestureRecognizer.Recognize(new List<List<StrokeSample>>(), SpellFamily.Fire);
            report.RecordAssist(HintAssistance.ForAttempt(SpellFamily.Fire, 0, false, failedResult));
            report.RecordAssist(HintAssistance.ForAttempt(SpellFamily.Fire, 1, false, failedResult));

            var successfulResult = GestureRecognizer.Recognize(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire), SpellFamily.Fire);
            var assistedSuccess = HintAssistance.ForAttempt(SpellFamily.Fire, 1, true, successfulResult);
            report.RecordBase(SpellFamily.Fire, successfulResult.quality, success: true, assistedSuccess);

            var text = report.BuildText(
                totalAttempts: 3,
                outputDirectory: "MagicExamHallLogs/test",
                trueEnding: true,
                completedFinalGoals: 6,
                totalFinalGoals: 6,
                noteExcerpts: System.Array.Empty<string>());

            Assert.That(text, Does.Contain("힌트 표시: 2회"));
            Assert.That(text, Does.Contain("최대 체크리스트"));
            Assert.That(text, Does.Contain("힌트 후 성공 1회"));
            Assert.That(text, Does.Contain("문양 습관"));
        }

        [Test]
        public void PreviewBeforeFailureDoesNotShowHint()
        {
            var preview = HintAssistance.PreviewFor(SpellFamily.Water, 0);

            Assert.That(preview.currentLevel, Is.EqualTo(AssistLevel.None));
            Assert.That(preview.hintShown, Is.False);
            Assert.That(preview.assisted, Is.False);
        }

        [Test]
        public void SpellCastingServiceRoutesBaseThenOverlay()
        {
            var service = new SpellCastingService();
            var baseStrokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, 1.6f, 0.03f), Vector2.zero, 0.8f);

            var baseOutcome = service.Process(baseStrokes, Vector2.zero, baseStrokes.Count, new List<CompiledSeal>(), 0f);

            Assert.That(baseOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.BaseSucceeded));
            Assert.That(baseOutcome.createdSeal.baseFamily, Is.EqualTo(SpellFamily.Fire));

            var overlayStrokes = OverlayRecognizer.CreateCanonicalSamples(
                OverlayOperator.VoidCut,
                baseOutcome.createdSeal.worldCenter,
                baseOutcome.createdSeal.worldScale * 0.24f);
            var overlayOutcome = service.Process(
                overlayStrokes,
                baseOutcome.createdSeal.worldCenter,
                overlayStrokes.Count,
                new List<CompiledSeal> { baseOutcome.createdSeal },
                0.2f);

            Assert.That(overlayOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.OverlaySucceeded));
            Assert.That(overlayOutcome.targetSeal, Is.SameAs(baseOutcome.createdSeal));
            Assert.That(baseOutcome.createdSeal.overlayStack, Does.Contain(OverlayOperator.VoidCut));
        }

        [Test]
        public void SpellCastingServiceCanConsumeRecognitionResultsFromAnotherLayer()
        {
            var service = new SpellCastingService();
            var baseStrokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Earth, 1.6f, 0.03f), Vector2.zero, 0.8f);
            var baseResult = SpellRuntime.RecognizeBase(baseStrokes);

            var baseOutcome = service.ProcessBaseResult(baseResult, Vector2.zero, baseStrokes.Count, 0f);

            Assert.That(baseOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.BaseSucceeded));
            Assert.That(baseOutcome.createdSeal.baseFamily, Is.EqualTo(SpellFamily.Earth));

            var overlayResult = new OverlayRecognitionResult
            {
                status = RecognitionStatus.Recognized,
                recognizedOperator = OverlayOperator.IceBar,
                score = 0.95f,
                shapeConfidence = 0.95f
            };
            var overlayOutcome = service.ProcessOverlayResult(
                overlayResult,
                baseOutcome.createdSeal,
                baseOutcome.createdSeal.worldCenter,
                1);

            Assert.That(overlayOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.OverlaySucceeded));
            Assert.That(baseOutcome.createdSeal.overlayStack, Does.Contain(OverlayOperator.IceBar));
        }

        [Test]
        public void SpellCastingServiceKeepsRecognizedBaseRetryNearExistingSeal()
        {
            var service = new SpellCastingService();
            var seal = CreateWorldSeal();
            var retryStrokes = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Wind, 1.6f, 0.03f), seal.worldCenter, 0.8f);
            var basePreview = SpellRuntime.RecognizeBase(retryStrokes);
            var overlayPreview = OverlayRecognizer.Recognize(retryStrokes, seal);

            var outcome = service.Process(retryStrokes, seal.worldCenter, retryStrokes.Count, new List<CompiledSeal> { seal }, 0.2f);

            Assert.That(basePreview.spell.status, Is.EqualTo(RecognitionStatus.Recognized));
            Assert.That(outcome.kind, Is.EqualTo(SpellCastOutcomeKind.BaseSucceeded), $"overlay status={overlayPreview.status}, op={overlayPreview.OperatorText}, score={overlayPreview.score:0.000}, shape={overlayPreview.shapeConfidence:0.000}, scale={overlayPreview.scaleRatio:0.000}, hint={overlayPreview.scaleHint}");
            Assert.That(outcome.createdSeal.baseFamily, Is.EqualTo(SpellFamily.Wind));
            Assert.That(seal.overlayStack, Is.Empty);
        }

        [Test]
        public void SpellCastingServiceUsesInjectedRecognitionBoundary()
        {
            var service = new SpellCastingService(
                new StubBaseRecognizer(SpellFamily.Water),
                new StubOverlayRecognizer(OverlayOperator.ElectricFork));
            var strokes = new List<List<StrokeSample>>
            {
                MakeLine(-0.5f, -0.5f, 0.5f, 0.5f, 0f)
            };

            var baseOutcome = service.Process(strokes, new Vector2(2f, -1f), strokes.Count, new List<CompiledSeal>(), 10f);

            Assert.That(baseOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.BaseSucceeded));
            Assert.That(baseOutcome.createdSeal.baseFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(baseOutcome.createdSeal.worldCenter, Is.EqualTo(new Vector2(2f, -1f)));

            var overlayOutcome = service.Process(
                strokes,
                baseOutcome.createdSeal.worldCenter,
                strokes.Count,
                new List<CompiledSeal> { baseOutcome.createdSeal },
                10.2f);

            Assert.That(overlayOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.OverlaySucceeded));
            Assert.That(overlayOutcome.targetSeal, Is.SameAs(baseOutcome.createdSeal));
            Assert.That(baseOutcome.createdSeal.overlayStack, Does.Contain(OverlayOperator.ElectricFork));
        }

        [Test]
        public void SpellCastingServiceProcessesExplicitRecognitionHandoffs()
        {
            var service = new SpellCastingService();
            var baseHandoff = SpellRecognitionHandoff.Base(
                RecognitionStatus.Recognized,
                SpellFamily.Water,
                SpellFamily.Water,
                new Vector2(1.5f, -0.5f),
                0.96f,
                PerfectQuality(),
                worldScale: 1.4f,
                strokeCount: 2,
                sourceId: "external-base");

            var baseOutcome = service.ProcessHandoff(baseHandoff, new List<CompiledSeal>(), 0f);

            Assert.That(baseOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.BaseSucceeded));
            Assert.That(baseOutcome.createdSeal.baseFamily, Is.EqualTo(SpellFamily.Water));
            Assert.That(baseOutcome.createdSeal.worldCenter, Is.EqualTo(new Vector2(1.5f, -0.5f)));

            var overlayHandoff = SpellRecognitionHandoff.Overlay(
                RecognitionStatus.Recognized,
                OverlayOperator.IceBar,
                baseOutcome.createdSeal.worldCenter,
                0.94f,
                0.91f,
                targetSealId: baseOutcome.createdSeal.sealId,
                sourceId: "external-overlay");

            var overlayOutcome = service.ProcessHandoff(
                overlayHandoff,
                new List<CompiledSeal> { baseOutcome.createdSeal },
                0.2f);

            Assert.That(overlayOutcome.kind, Is.EqualTo(SpellCastOutcomeKind.OverlaySucceeded));
            Assert.That(overlayOutcome.targetSeal, Is.SameAs(baseOutcome.createdSeal));
            Assert.That(baseOutcome.createdSeal.overlayStack, Does.Contain(OverlayOperator.IceBar));
        }

        [Test]
        public void OverlayHandoffRequiresActiveAttachableSeal()
        {
            var service = new SpellCastingService();
            var noSealHandoff = SpellRecognitionHandoff.Overlay(
                RecognitionStatus.Recognized,
                OverlayOperator.SoulDot,
                Vector2.zero,
                0.95f,
                0.95f);

            var noSeal = service.ProcessHandoff(noSealHandoff, new List<CompiledSeal>(), 0f);

            Assert.That(noSeal.kind, Is.EqualTo(SpellCastOutcomeKind.OverlayNoActiveSeal));

            var seal = CreateWorldSeal();
            var farHandoff = SpellRecognitionHandoff.Overlay(
                RecognitionStatus.Recognized,
                OverlayOperator.SoulDot,
                seal.worldCenter + Vector2.right * (SpellCastingService.AttachRadiusFor(seal) + 0.5f),
                0.95f,
                0.95f,
                targetSealId: seal.sealId);

            var detached = service.ProcessHandoff(farHandoff, new List<CompiledSeal> { seal }, 0.2f);

            Assert.That(detached.kind, Is.EqualTo(SpellCastOutcomeKind.DetachedOverlay));
            Assert.That(detached.targetSeal, Is.SameAs(seal));
            Assert.That(seal.overlayStack, Is.Empty);
        }

        [Test]
        public void SpellCastingServiceExposesAttachLookupForInputAdapters()
        {
            var seal = CreateWorldSeal();
            var seals = new List<CompiledSeal> { seal };
            var attachRadius = SpellCastingService.AttachRadiusFor(seal);

            var near = SpellCastingService.FindAttachableSeal(seals, seal.worldCenter + Vector2.right * (attachRadius * 0.95f), 0.2f);
            var far = SpellCastingService.FindAttachableSeal(seals, seal.worldCenter + Vector2.right * (attachRadius + 0.2f), 0.2f);
            var expired = SpellCastingService.FindAttachableSeal(seals, seal.worldCenter, seal.expiresAt + 0.1f);

            Assert.That(near, Is.SameAs(seal));
            Assert.That(far, Is.Null);
            Assert.That(expired, Is.Null);
        }

        [Test]
        public void SpellCastingServiceRejectsNullHandoffInputs()
        {
            var service = new SpellCastingService();
            var seal = CreateWorldSeal();
            var overlayResult = new OverlayRecognitionResult();

            Assert.Throws<System.ArgumentNullException>(() => SpellCastingService.AttachRadiusFor(null));
            Assert.Throws<System.ArgumentNullException>(() => SpellCastingService.FindAttachableSeal(null, Vector2.zero, 0f));
            Assert.Throws<System.ArgumentNullException>(() => service.ProcessBaseResult(null, Vector2.zero, 0, 0f));
            Assert.Throws<System.ArgumentNullException>(() => service.ProcessOverlayResult(null, seal, Vector2.zero, 0));
            Assert.Throws<System.ArgumentNullException>(() => service.ProcessOverlayResult(overlayResult, null, Vector2.zero, 0));
        }

        [Test]
        public void SpellCastingServiceSeparatesDuplicateAndFullOverlayStacks()
        {
            var service = new SpellCastingService();
            var seal = CreateWorldSeal(OverlayOperator.VoidCut);
            var duplicateStrokes = OverlayRecognizer.CreateCanonicalSamples(OverlayOperator.VoidCut, seal.worldCenter, seal.worldScale * 0.24f);

            var duplicate = service.Process(duplicateStrokes, seal.worldCenter, duplicateStrokes.Count, new List<CompiledSeal> { seal }, 0.2f);

            Assert.That(duplicate.kind, Is.EqualTo(SpellCastOutcomeKind.OverlayDuplicate));
            Assert.That(seal.overlayStack.Count(op => op == OverlayOperator.VoidCut), Is.EqualTo(1));

            var fullSeal = CreateWorldSeal(OverlayOperator.VoidCut, OverlayOperator.IceBar, OverlayOperator.SoulDot);
            var extraStrokes = OverlayRecognizer.CreateCanonicalSamples(OverlayOperator.SteelBrace, fullSeal.worldCenter, fullSeal.worldScale * 0.24f);

            var full = service.Process(extraStrokes, fullSeal.worldCenter, extraStrokes.Count, new List<CompiledSeal> { fullSeal }, 0.2f);

            Assert.That(full.kind, Is.EqualTo(SpellCastOutcomeKind.OverlayStackFull));
            Assert.That(fullSeal.overlayStack.Count, Is.EqualTo(SpellCastingService.MaxOverlayStack));
            Assert.That(fullSeal.overlayStack, Has.None.EqualTo(OverlayOperator.SteelBrace));
        }

        [Test]
        public void FloorGoalSystemDistinguishesCompletedAndOffTargetBaseCasts()
        {
            var goals = new List<WorldStateGoal>
            {
                WorldStateGoal.Base("puddle", "물웅덩이", SpellFamily.Water, new Vector2(3f, 0f), Color.blue, "물길이 맑아집니다.")
            };
            var system = new FloorGoalSystem();

            var offTarget = system.ResolveBase(goals, SpellFamily.Water, Vector2.zero);
            var completed = system.ResolveBase(goals, SpellFamily.Water, new Vector2(3f, 0f));

            Assert.That(offTarget.kind, Is.EqualTo(GoalResolutionKind.BaseOffTarget));
            Assert.That(offTarget.targetGoal, Is.SameAs(goals[0]));
            Assert.That(offTarget.distance, Is.GreaterThan(offTarget.radius));
            Assert.That(completed.kind, Is.EqualTo(GoalResolutionKind.Completed));
            Assert.That(completed.goal, Is.SameAs(goals[0]));
        }

        private static List<StrokeSample> MakeLine(float x1, float y1, float x2, float y2, float start)
        {
            return Enumerable.Range(0, 12)
                .Select(index =>
                {
                    var t = index / 11f;
                    return new StrokeSample(new Vector2(Mathf.Lerp(x1, x2, t), Mathf.Lerp(y1, y2, t)), start + index * 0.02f);
                })
                .ToList();
        }

        private static List<StrokeSample> MakeEllipseArc(float startDegrees, float endDegrees, int count, Vector2 center, float radiusX, float radiusY, float startTime)
        {
            return Enumerable.Range(0, count)
                .Select(index =>
                {
                    var t = index / Mathf.Max(count - 1f, 1f);
                    var angle = Mathf.Lerp(startDegrees, endDegrees, t) * Mathf.Deg2Rad;
                    var point = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
                    return new StrokeSample(point, startTime + index * 0.025f);
                })
                .ToList();
        }

        private static CompiledSeal CreateWorldSeal(params OverlayOperator[] overlays)
        {
            var baseSamples = Offset(GestureRecognizer.CreateCanonicalSamples(SpellFamily.Fire, 1.6f, 0.03f), Vector2.zero, 0.8f);
            var baseResult = SpellRuntime.RecognizeBase(baseSamples);
            var seal = SpellRuntime.CreateSeal(baseResult, 0f);
            foreach (var op in overlays)
            {
                seal.overlayStack.Add(op);
            }

            return seal;
        }

        private static List<List<StrokeSample>> Offset(List<List<StrokeSample>> strokes, Vector2 center, float canonicalCenter)
        {
            return strokes
                .Select(stroke => stroke.Select(sample => new StrokeSample(sample.position - Vector2.one * canonicalCenter + center, sample.time)).ToList())
                .ToList();
        }

        private static QualityVector PerfectQuality()
        {
            return new QualityVector
            {
                closure = 1f,
                smoothness = 1f,
                tempo = 1f,
                stability = 1f,
                rotationBias = 0f
            };
        }

        private static void AssertLastAttemptCsvFields(ExamLogger logger, bool success, bool hintShown, int assistLevel, bool assisted)
        {
            var csvPath = System.IO.Path.Combine(logger.OutputDirectory, "attempts.csv");
            var lastRow = System.IO.File.ReadAllLines(csvPath).Last();
            var fields = lastRow.Split(',');

            Assert.That(fields[^4], Is.EqualTo(success ? "true" : "false"));
            Assert.That(fields[^3], Is.EqualTo(hintShown ? "true" : "false"));
            Assert.That(fields[^2], Is.EqualTo(assistLevel.ToString()));
            Assert.That(fields[^1], Is.EqualTo(assisted ? "true" : "false"));
        }

        private static ExamLogger CreateEnabledLoggerForTest(string sessionId)
        {
            var outputRoot = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MagicExamHallLoggerTests",
                System.Guid.NewGuid().ToString("N"));
            return new ExamLogger(sessionId, outputRoot, enableCollection: true);
        }

        private sealed class StubBaseRecognizer : IBaseGestureRecognizer
        {
            private readonly SpellFamily family;

            public StubBaseRecognizer(SpellFamily family)
            {
                this.family = family;
            }

            public BaseRecognitionResult RecognizeBase(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
            {
                return new BaseRecognitionResult
                {
                    spell = new SpellResult
                    {
                        status = RecognitionStatus.Recognized,
                        recognizedFamily = family,
                        targetFamily = family,
                        confidence = 0.99f,
                        quality = PerfectQuality(),
                        feedbackReason = "stub base",
                        nextHint = "stub next",
                        success = true
                    },
                    worldScale = 1.25f,
                    bufferStrokeCount = strokes.Count
                };
            }
        }

        private sealed class StubOverlayRecognizer : IOverlayGestureRecognizer
        {
            private readonly OverlayOperator op;

            public StubOverlayRecognizer(OverlayOperator op)
            {
                this.op = op;
            }

            public OverlayRecognitionResult RecognizeOverlay(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes, CompiledSeal seal)
            {
                return new OverlayRecognitionResult
                {
                    status = RecognitionStatus.Recognized,
                    recognizedOperator = op,
                    score = 0.98f,
                    shapeConfidence = 0.98f,
                    scaleRatio = 0.24f,
                    feedbackReason = "stub overlay"
                };
            }
        }
    }
}

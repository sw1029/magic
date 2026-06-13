using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    public sealed class TutorialWalkthroughRecorder : MonoBehaviour
    {
        private const int DefaultFrameRate = 12;
        private const float BaseDrawScale = 1.6f;
        private const float CanonicalCenter = 0.8f;

        private static readonly Dictionary<SpellFamily, Color> FamilyColors = new()
        {
            { SpellFamily.Fire, new Color(1f, 0.32f, 0.18f, 0.96f) },
            { SpellFamily.Water, new Color(0.36f, 0.68f, 1f, 0.96f) },
            { SpellFamily.Wind, new Color(0.80f, 0.94f, 1f, 0.96f) },
            { SpellFamily.Earth, new Color(0.88f, 0.66f, 0.36f, 0.96f) },
            { SpellFamily.Life, new Color(0.36f, 0.92f, 0.48f, 0.96f) }
        };

        private readonly List<GameObject> transientObjects = new();
        private readonly List<string> segmentNotes = new();
        private ExamGameController controller = null!;
        private string outputDirectory = "";
        private int frameRate = DefaultFrameRate;
        private int frameIndex;
        private bool completed;
        private Material strokeMaterial = null!;
        private Camera mainCamera = null!;
        private GameObject pointerObject = null!;
        private LineRenderer pointerRing = null!;
        private LineRenderer pointerCross = null!;

        public static TutorialWalkthroughRecorder Begin(
            ExamGameController controller,
            string outputDirectory,
            int frameRate = DefaultFrameRate)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            var recorderObject = new GameObject("Tutorial Walkthrough Recorder");
            var recorder = recorderObject.AddComponent<TutorialWalkthroughRecorder>();
            recorder.controller = controller;
            recorder.outputDirectory = outputDirectory;
            recorder.frameRate = Mathf.Max(6, frameRate);
            recorder.StartCoroutine(recorder.Run());
            return recorder;
        }

        public bool IsCompleted => completed;
        public int FrameCount => frameIndex;
        public string OutputDirectory => outputDirectory;

        private IEnumerator Run()
        {
            Application.targetFrameRate = frameRate;
            Time.captureFramerate = frameRate;
            mainCamera = controller.mainCamera != null ? controller.mainCamera : Camera.main;
            strokeMaterial = new Material(Shader.Find("Sprites/Default"));
            CreatePointer();
            Directory.CreateDirectory(outputDirectory);

            yield return RecordSeconds(0.5f);
            yield return RunFloorOne();
            yield return RunFloorTwo();
            yield return RunFloorThree();
            yield return RecordSeconds(1.2f);

            ClearTransientObjects();
            if (pointerObject != null)
            {
                Destroy(pointerObject);
            }

            Time.captureFramerate = 0;
            Application.targetFrameRate = -1;
            completed = true;
            WriteManifest();
            Debug.Log($"TutorialWalkthroughRecorder complete: frames={frameIndex}, dir={outputDirectory}");
        }

        private IEnumerator RunFloorOne()
        {
            segmentNotes.Add("floor1: base spell tutorial, including offset drawing with player-position cast origin");
            controller.LoadFloorForTests(0);
            yield return RecordSeconds(1.2f);

            yield return MoveTo(controller.StageGoalPositionForTests("ember"), 0.75f);
            yield return DrawBaseAndSubmit(SpellFamily.Fire, controller.PlayerPosition + new Vector2(0.78f, 0.15f), 0.85f, 0.7f);

            yield return MoveTo(controller.StageGoalPositionForTests("puddle"), 0.9f);
            yield return DrawBaseAndSubmit(SpellFamily.Water, controller.PlayerPosition, 0.75f, 0.65f);

            yield return MoveTo(controller.StageGoalPositionForTests("vane"), 0.95f);
            yield return DrawBaseAndSubmit(SpellFamily.Wind, controller.PlayerPosition, 0.75f, 0.65f);

            yield return MoveTo(controller.StageGoalPositionForTests("pillar"), 1.0f);
            yield return DrawBaseAndSubmit(SpellFamily.Earth, controller.PlayerPosition, 0.75f, 0.65f);

            yield return MoveTo(controller.StageGoalPositionForTests("vine"), 0.95f);
            yield return DrawBaseAndSubmit(SpellFamily.Life, controller.PlayerPosition, 0.75f, 1.0f);
        }

        private IEnumerator RunFloorTwo()
        {
            segmentNotes.Add("floor2: examiner grants the current slot shape, then each target is solved in sequence");
            controller.LoadFloorForTests(1);
            yield return RecordSeconds(1.2f);

            yield return DrawCustomBaseGoal("custom_water", SpellFamily.Water, 0.72f);
            yield return DrawCustomBaseGoal("custom_fire", SpellFamily.Fire, 0.72f);
            yield return DrawCustomBaseGoal("custom_wind", SpellFamily.Wind, 0.72f);
            yield return DrawCustomBaseGoal("custom_earth", SpellFamily.Earth, 0.72f);
            yield return DrawCustomBaseGoal("custom_life", SpellFamily.Life, 1.0f);
        }

        private IEnumerator RunFloorThree()
        {
            segmentNotes.Add("floor3: examiner grants traversal shapes, then river, hole, chasm, and wind gap are solved");
            controller.LoadFloorForTests(2);
            yield return RecordSeconds(1.35f);

            yield return SolveFloorThreeObstacle("frozen_river", SpellFamily.Water, 0.2f);
            yield return SolveFloorThreeObstacle("earth_stairs", SpellFamily.Earth, 5.2f);
            yield return SolveFloorThreeObstacle("living_bridge", SpellFamily.Life, 11.0f);
            yield return SolveFloorThreeObstacle("wind_platform", SpellFamily.Wind, 17.3f);
        }

        private IEnumerator SolveFloorThreeObstacle(string goalId, SpellFamily family, float landingX)
        {
            var goalPosition = controller.StageGoalPositionForTests(goalId);
            var completedBefore = controller.CompletedGoalCountForTests;
            yield return MoveTo(goalPosition, 1.1f);
            yield return DrawBaseAndSubmit(family, goalPosition, 0.55f, 0.22f, pinPlayerToDrawCenterBeforeSubmit: true);
            yield return DrawCustomFollowup(
                family,
                goalPosition,
                0.6f,
                1.05f,
                pinPlayerToDrawCenterBeforeSubmit: true,
                refreshBaseFamilyBeforeSubmit: family);

            if (controller.CompletedGoalCountForTests <= completedBefore)
            {
                yield return RecordSeconds(0.25f);
                controller.MovePlayerForTests(goalPosition);
                yield return DrawBaseAndSubmit(family, goalPosition, 0.38f, 0.15f, pinPlayerToDrawCenterBeforeSubmit: true);
                yield return DrawCustomFollowup(
                    family,
                    goalPosition,
                    0.42f,
                    1.0f,
                    pinPlayerToDrawCenterBeforeSubmit: true,
                    refreshBaseFamilyBeforeSubmit: family);
            }

            if (controller.CompletedGoalCountForTests <= completedBefore)
            {
                controller.MovePlayerForTests(goalPosition);
                controller.CastSyntheticBaseForTests(family, goalPosition, movePlayerToReference: false);
                var fallbackStrokes = controller.CustomReferenceStrokesForTests(family, goalPosition);
                controller.CastRawBaseForTests(fallbackStrokes, goalPosition, movePlayerToReference: false);
                yield return RecordSeconds(0.75f);
            }

            yield return MoveTo(new Vector2(landingX, -2.58f), 1.05f);
            yield return RecordSeconds(0.25f);
        }

        private IEnumerator DrawCustomBaseGoal(string goalId, SpellFamily family, float holdAfter)
        {
            var goalPosition = controller.StageGoalPositionForTests(goalId);
            yield return MoveTo(goalPosition, 0.8f);
            var strokes = controller.CustomReferenceStrokesForTests(family, controller.PlayerPosition);
            yield return DrawStrokes(strokes, FamilyColors[family], 0.62f);
            controller.CastRawBaseForTests(strokes, controller.PlayerPosition, movePlayerToReference: false);
            yield return RecordSeconds(holdAfter);
            ClearTransientObjects();
        }

        private IEnumerator DrawBaseAndSubmit(
            SpellFamily family,
            Vector2 drawCenter,
            float drawSeconds,
            float holdAfter,
            bool pinPlayerToDrawCenterBeforeSubmit = false)
        {
            var strokes = Offset(GestureRecognizer.CreateCanonicalSamples(family, BaseDrawScale, 0.03f), drawCenter, CanonicalCenter);
            yield return DrawStrokes(strokes, FamilyColors[family], drawSeconds);
            if (pinPlayerToDrawCenterBeforeSubmit)
            {
                controller.MovePlayerForTests(drawCenter);
            }

            controller.CastRawBaseForTests(strokes, drawCenter, movePlayerToReference: false);
            yield return RecordSeconds(holdAfter);
            ClearTransientObjects();
        }

        private IEnumerator DrawCustomFollowup(
            SpellFamily family,
            Vector2 drawCenter,
            float drawSeconds,
            float holdAfter,
            bool pinPlayerToDrawCenterBeforeSubmit = false,
            SpellFamily? refreshBaseFamilyBeforeSubmit = null)
        {
            var strokes = controller.CustomReferenceStrokesForTests(family, drawCenter);
            yield return DrawStrokes(strokes, FamilyColors[family], drawSeconds);
            if (pinPlayerToDrawCenterBeforeSubmit)
            {
                controller.MovePlayerForTests(drawCenter);
            }

            if (refreshBaseFamilyBeforeSubmit.HasValue)
            {
                controller.CastSyntheticBaseForTests(refreshBaseFamilyBeforeSubmit.Value, drawCenter, movePlayerToReference: false);
            }

            controller.CastRawBaseForTests(strokes, drawCenter, movePlayerToReference: false);
            yield return RecordSeconds(holdAfter);
            ClearTransientObjects();
        }

        private IEnumerator DrawStrokes(List<List<StrokeSample>> strokes, Color color, float targetSeconds)
        {
            var pointCount = Mathf.Max(1, strokes.Sum(stroke => stroke?.Count ?? 0));
            var targetFrames = Mathf.Max(1, Mathf.CeilToInt(targetSeconds * frameRate));
            var pointsPerFrame = Mathf.Max(1, Mathf.CeilToInt(pointCount / (float)targetFrames));
            foreach (var stroke in strokes)
            {
                if (stroke == null || stroke.Count == 0)
                {
                    continue;
                }

                var line = CreateStrokeLine(color);
                for (var index = 0; index < stroke.Count; index += pointsPerFrame)
                {
                    var visiblePointCount = Mathf.Min(stroke.Count, index + pointsPerFrame);
                    line.positionCount = visiblePointCount;
                    for (var pointIndex = 0; pointIndex < visiblePointCount; pointIndex++)
                    {
                        var position = stroke[pointIndex].position;
                        line.SetPosition(pointIndex, new Vector3(position.x, position.y, 0f));
                    }

                    SetPointer(stroke[visiblePointCount - 1].position, true);
                    yield return CaptureTick();
                }

                SetPointer(stroke[^1].position, false);
                yield return RecordSeconds(0.08f);
            }
        }

        private LineRenderer CreateStrokeLine(Color color)
        {
            var lineObject = new GameObject("Recorded User Stroke");
            transientObjects.Add(lineObject);
            var line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = strokeMaterial;
            line.startColor = color;
            line.endColor = Color.Lerp(color, Color.white, 0.18f);
            line.startWidth = 0.075f;
            line.endWidth = 0.055f;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.useWorldSpace = true;
            line.sortingOrder = 80;
            return line;
        }

        private void CreatePointer()
        {
            pointerObject = new GameObject("Recorded User Pointer");
            pointerRing = CreatePointerLine("Recorded User Pointer Ring", 34, 0.038f);
            pointerCross = CreatePointerLine("Recorded User Pointer Cross", 5, 0.022f);
            SetPointer(Vector2.zero, false, visible: false);
        }

        private LineRenderer CreatePointerLine(string name, int positionCount, float width)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(pointerObject.transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = strokeMaterial;
            line.positionCount = positionCount;
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 3;
            line.numCornerVertices = 3;
            line.useWorldSpace = false;
            line.sortingOrder = 120;
            return line;
        }

        private void SetPointer(Vector2 position, bool pressed, bool visible = true)
        {
            if (pointerObject == null)
            {
                return;
            }

            pointerObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            pointerObject.transform.position = new Vector3(position.x, position.y, -0.12f);
            var radius = pressed ? 0.18f : 0.13f;
            var color = pressed
                ? new Color(1f, 0.96f, 0.42f, 0.98f)
                : new Color(1f, 1f, 1f, 0.86f);
            pointerRing.startColor = color;
            pointerRing.endColor = color;
            pointerCross.startColor = color;
            pointerCross.endColor = color;

            for (var index = 0; index < pointerRing.positionCount; index++)
            {
                var angle = Mathf.PI * 2f * index / (pointerRing.positionCount - 1);
                pointerRing.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }

            var crossRadius = radius * 0.46f;
            pointerCross.SetPosition(0, new Vector3(-crossRadius, 0f, 0f));
            pointerCross.SetPosition(1, new Vector3(crossRadius, 0f, 0f));
            pointerCross.SetPosition(2, Vector3.zero);
            pointerCross.SetPosition(3, new Vector3(0f, -crossRadius, 0f));
            pointerCross.SetPosition(4, new Vector3(0f, crossRadius, 0f));
        }

        private IEnumerator MoveTo(Vector2 destination, float seconds)
        {
            var origin = controller.PlayerPosition;
            var frames = Mathf.Max(1, Mathf.CeilToInt(seconds * frameRate));
            for (var frame = 1; frame <= frames; frame++)
            {
                var t = frame / (float)frames;
                var eased = t * t * (3f - 2f * t);
                controller.MovePlayerForTests(Vector2.Lerp(origin, destination, eased));
                yield return CaptureTick();
            }
        }

        private IEnumerator RecordSeconds(float seconds)
        {
            var frames = Mathf.Max(1, Mathf.CeilToInt(seconds * frameRate));
            for (var frame = 0; frame < frames; frame++)
            {
                yield return CaptureTick();
            }
        }

        private IEnumerator CaptureTick()
        {
            yield return new WaitForEndOfFrame();
            CaptureFrame();
            yield return null;
        }

        private void CaptureFrame()
        {
            var width = Mathf.Max(320, Screen.width);
            var height = Mathf.Max(180, Screen.height);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply(false);
            var path = Path.Combine(outputDirectory, $"frame_{frameIndex:D05}.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Destroy(texture);
            frameIndex++;
        }

        private void ClearTransientObjects()
        {
            foreach (var item in transientObjects)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }

            transientObjects.Clear();
        }

        private void WriteManifest()
        {
            var manifestPath = Path.Combine(outputDirectory, "manifest.txt");
            var lines = new List<string>
            {
                "Magic Exam Hall tutorial walkthrough recording",
                $"frames={frameIndex}",
                $"fps={frameRate}",
                $"screen={Screen.width}x{Screen.height}",
                $"completed={completed}",
                "segments:"
            };
            lines.AddRange(segmentNotes.Select(note => $"- {note}"));
            File.WriteAllLines(manifestPath, lines);
        }

        private static List<List<StrokeSample>> Offset(
            List<List<StrokeSample>> strokes,
            Vector2 center,
            float canonicalCenter)
        {
            return strokes
                .Select(stroke => stroke
                    .Select(sample => new StrokeSample(sample.position + center - Vector2.one * canonicalCenter, sample.time))
                    .ToList())
                .ToList();
        }
    }
}

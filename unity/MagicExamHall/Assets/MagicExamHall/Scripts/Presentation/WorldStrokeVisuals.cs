using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    public sealed class WorldStrokeVisuals : MonoBehaviour
    {
        public const float DefaultStrokeVisualLifetimeSeconds = 4.2f;
        public const float RecognizedStrokeFadeSeconds = 0.32f;
        public const float InvalidStrokeFadeSeconds = 1.0f;

        public Color strokeColor = new(0.22f, 0.95f, 1f, 0.92f);
        public float lineWidth = 0.075f;
        public float lifetimeSeconds = DefaultStrokeVisualLifetimeSeconds;

        private readonly Dictionary<string, StrokeVisual> active = new();
        private readonly List<StrokeVisual> visuals = new();
        private readonly List<StrokeVisual> pendingRecognition = new();

        public int VisualCountForTests => visuals.Count(visual => visual.body != null);

        public void HandleStrokeStarted(StrokeInputStroke stroke)
        {
            UpsertVisual(stroke, completed: false);
        }

        public void HandleStrokeUpdated(StrokeInputStroke stroke)
        {
            UpsertVisual(stroke, completed: false);
        }

        public void HandleStrokeCompleted(StrokeInputStroke stroke)
        {
            UpsertVisual(stroke, completed: true);
        }

        public void HandleStrokeCanceled()
        {
            foreach (var item in active.Values)
            {
                item.completed = true;
            }

            active.Clear();
        }

        public void ShowCompletedSession(StrokeInputSession session)
        {
            foreach (var stroke in session.Strokes)
            {
                HandleStrokeCompleted(stroke);
            }
        }

        public void ClearAll()
        {
            foreach (var visual in visuals)
            {
                if (visual.body != null)
                {
                    Destroy(visual.body);
                }
            }

            visuals.Clear();
            active.Clear();
            pendingRecognition.Clear();
        }

        public void MarkLastCompletedSessionRecognized(Color color)
        {
            MarkPendingRecognition(StrokeVisualState.Recognized, new Color(color.r, color.g, color.b, 0.38f));
        }

        public void MarkLastCompletedSessionInvalid()
        {
            MarkPendingRecognition(StrokeVisualState.Invalid, new Color(0.76f, 0.78f, 0.86f, 0.72f));
        }

        public void Tick(float deltaTime)
        {
            for (var index = visuals.Count - 1; index >= 0; index--)
            {
                var visual = visuals[index];
                if (!visual.completed)
                {
                    continue;
                }

                visual.age += deltaTime;
                visual.stateAge += deltaTime;
                var color = visual.ColorFor(strokeColor, lifetimeSeconds);
                if (visual.line != null)
                {
                    visual.line.startColor = color;
                    visual.line.endColor = color;
                }

                if (visual.ShouldRemove(lifetimeSeconds))
                {
                    if (visual.body != null)
                    {
                        Destroy(visual.body);
                    }

                    visuals.RemoveAt(index);
                }
            }
        }

        private void UpsertVisual(StrokeInputStroke stroke, bool completed)
        {
            if (stroke == null || stroke.Points.Count < 1)
            {
                return;
            }

            if (!active.TryGetValue(stroke.Id, out var visual))
            {
                visual = CreateVisual(stroke.Id);
                active[stroke.Id] = visual;
                visuals.Add(visual);
            }

            visual.completed = completed;
            if (completed)
            {
                active.Remove(stroke.Id);
                visual.age = 0f;
                if (!pendingRecognition.Contains(visual))
                {
                    pendingRecognition.Add(visual);
                }
            }

            visual.line.positionCount = stroke.Points.Count;
            visual.line.startWidth = lineWidth;
            visual.line.endWidth = lineWidth;
            visual.line.startColor = strokeColor;
            visual.line.endColor = strokeColor;

            for (var index = 0; index < stroke.Points.Count; index++)
            {
                var point = stroke.Points[index].Position;
                visual.line.SetPosition(index, new Vector3(point.x, point.y, -0.2f));
            }
        }

        private StrokeVisual CreateVisual(string strokeId)
        {
            var body = new GameObject($"World Spell Stroke {strokeId}");
            body.transform.SetParent(transform, true);
            var line = body.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.numCornerVertices = 0;
            line.numCapVertices = 0;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.sortingOrder = 20;
            return new StrokeVisual(body, line);
        }

        private void MarkPendingRecognition(StrokeVisualState state, Color targetColor)
        {
            foreach (var visual in pendingRecognition.Where(visual => visual.line != null).ToList())
            {
                visual.state = state;
                visual.stateAge = 0f;
                visual.targetColor = targetColor;
                visual.completed = true;
            }

            pendingRecognition.Clear();
        }

        private sealed class StrokeVisual
        {
            public readonly GameObject body;
            public readonly LineRenderer line;
            public bool completed;
            public float age;
            public float stateAge;
            public Color targetColor;
            public StrokeVisualState state;

            public StrokeVisual(GameObject body, LineRenderer line)
            {
                this.body = body;
                this.line = line;
                targetColor = Color.white;
            }

            public bool ShouldRemove(float lifetimeSeconds)
            {
                return state switch
                {
                    StrokeVisualState.Recognized => stateAge >= RecognizedStrokeFadeSeconds,
                    StrokeVisualState.Invalid => stateAge >= InvalidStrokeFadeSeconds,
                    _ => age >= Mathf.Max(lifetimeSeconds, 0.001f)
                };
            }

            public Color ColorFor(Color baseColor, float lifetimeSeconds)
            {
                return state switch
                {
                    StrokeVisualState.Recognized => RecognizedColor(baseColor),
                    StrokeVisualState.Invalid => InvalidColor(),
                    _ => new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(0.92f, 0f, age / Mathf.Max(lifetimeSeconds, 0.001f)))
                };
            }

            private Color RecognizedColor(Color baseColor)
            {
                var tintT = Mathf.Clamp01(stateAge / 0.08f);
                var fadeT = Mathf.Clamp01((stateAge - 0.08f) / Mathf.Max(WorldStrokeVisuals.RecognizedStrokeFadeSeconds - 0.08f, 0.001f));
                var color = Color.Lerp(baseColor, targetColor, tintT);
                color.a = Mathf.Lerp(0.38f, 0f, fadeT);
                return color;
            }

            private Color InvalidColor()
            {
                var fadeT = Mathf.Clamp01(stateAge / InvalidStrokeFadeSeconds);
                var color = targetColor;
                color.a = Mathf.Lerp(0.72f, 0f, fadeT);
                return color;
            }
        }

        private enum StrokeVisualState
        {
            Drawing,
            Recognized,
            Invalid
        }
    }
}

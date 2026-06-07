using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MagicExamHall
{
    public sealed class WorldDrawingController : MonoBehaviour
    {
        public const float DefaultBufferSeconds = 1.05f;
        public const float DefaultMinPointDistance = 0.05f;
        public const float StrokeVisualLifetimeSeconds = 2.3f;

        public Camera mainCamera = null!;
        public float bufferSeconds = DefaultBufferSeconds;
        public float minPointDistance = DefaultMinPointDistance;
        public Color strokeColor = new(0.22f, 0.95f, 1f, 0.92f);

        private readonly List<List<StrokeSample>> bufferedStrokes = new();
        private readonly List<StrokeSample> activeStroke = new();
        private readonly List<StrokeVisual> visuals = new();
        private AudioSource drawingAudio;
        private AudioClip[] penTickClips = Array.Empty<AudioClip>();
        private AudioClip[] penCompleteClips = Array.Empty<AudioClip>();
        private float lastPenTickAt = -10f;
        private bool drawing;
        private bool waitingForBuffer;
        private float lastReleaseTime;

        public event Action<List<List<StrokeSample>>, Vector2, int> SpellBuffered = delegate { };

        public bool HasBufferedInput => bufferedStrokes.Count > 0 || activeStroke.Count > 0;

        public void ApplyPlayableDefaults()
        {
            bufferSeconds = DefaultBufferSeconds;
            minPointDistance = DefaultMinPointDistance;
        }

        private void Awake()
        {
            mainCamera ??= Camera.main;
            ConfigureDrawingAudio();
        }

        private void Update()
        {
            TickInput();
            TickBuffer();
            TickVisuals();
        }

        public void SubmitSyntheticSpell(List<List<StrokeSample>> strokes)
        {
            if (strokes == null || strokes.Count == 0)
            {
                SpellBuffered(new List<List<StrokeSample>>(), Vector2.zero, 0);
                return;
            }

            var copy = strokes.Select(stroke => stroke.Select(sample => new StrokeSample(sample.position, sample.time)).ToList()).ToList();
            SpellBuffered(copy, CenterOf(copy), copy.Count);
        }

        private void TickInput()
        {
            if (mainCamera == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(1) && !PointerIsOverUi())
            {
                drawing = true;
                waitingForBuffer = false;
                activeStroke.Clear();
                PlayRandomDrawingClip(penTickClips, 0.26f);
                AddPoint(Input.mousePosition);
            }

            if (drawing && Input.GetMouseButton(1))
            {
                if (Time.time - lastPenTickAt >= 0.18f)
                {
                    PlayRandomDrawingClip(penTickClips, 0.16f);
                    lastPenTickAt = Time.time;
                }

                AddPoint(Input.mousePosition);
            }

            if (!drawing || !Input.GetMouseButtonUp(1))
            {
                return;
            }

            AddPoint(Input.mousePosition);
            if (activeStroke.Count >= 2)
            {
                var stroke = new List<StrokeSample>(activeStroke);
                bufferedStrokes.Add(stroke);
                CreateStrokeVisual(stroke);
                PlayRandomDrawingClip(penCompleteClips, 0.22f);
            }

            activeStroke.Clear();
            drawing = false;
            waitingForBuffer = true;
            lastReleaseTime = Time.time;
        }

        private void TickBuffer()
        {
            if (!waitingForBuffer || Time.time - lastReleaseTime < bufferSeconds)
            {
                return;
            }

            Flush();
        }

        private void Flush()
        {
            waitingForBuffer = false;
            if (bufferedStrokes.Count == 0)
            {
                return;
            }

            var copy = bufferedStrokes.Select(stroke => stroke.Select(sample => new StrokeSample(sample.position, sample.time)).ToList()).ToList();
            bufferedStrokes.Clear();
            SpellBuffered(copy, CenterOf(copy), copy.Count);
        }

        private void AddPoint(Vector2 screenPoint)
        {
            var world = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, -mainCamera.transform.position.z));
            var point = new Vector2(world.x, world.y);
            if (activeStroke.Count > 0 && Vector2.Distance(activeStroke[^1].position, point) < minPointDistance)
            {
                return;
            }

            activeStroke.Add(new StrokeSample(point, Time.time));
        }

        private bool PointerIsOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void CreateStrokeVisual(IReadOnlyList<StrokeSample> stroke)
        {
            if (stroke.Count < 2)
            {
                return;
            }

            var body = new GameObject("World Spell Stroke");
            body.transform.SetParent(transform, true);
            var line = body.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = stroke.Count;
            line.startWidth = 0.075f;
            line.endWidth = 0.075f;
            line.numCornerVertices = 0;
            line.numCapVertices = 0;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = strokeColor;
            line.endColor = strokeColor;
            line.sortingOrder = 20;
            for (var index = 0; index < stroke.Count; index++)
            {
                line.SetPosition(index, new Vector3(stroke[index].position.x, stroke[index].position.y, -0.2f));
            }

            visuals.Add(new StrokeVisual(body, line));
        }

        private void ConfigureDrawingAudio()
        {
            drawingAudio = GetComponent<AudioSource>();
            if (drawingAudio == null)
            {
                drawingAudio = gameObject.AddComponent<AudioSource>();
            }

            drawingAudio.playOnAwake = false;
            drawingAudio.spatialBlend = 0f;
            drawingAudio.volume = 1f;
            drawingAudio.ignoreListenerPause = true;
            penTickClips = Resources.LoadAll<AudioClip>("Sfx/PenAndPaper01")
                .Where(clip => clip.name.IndexOf("SFX_Penv", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            penCompleteClips = Resources.LoadAll<AudioClip>("Sfx/HydrographicPen01");
        }

        private void PlayRandomDrawingClip(AudioClip[] clips, float volume)
        {
            if (drawingAudio == null || clips == null || clips.Length == 0)
            {
                return;
            }

            drawingAudio.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            drawingAudio.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)], volume);
            drawingAudio.pitch = 1f;
        }

        private void TickVisuals()
        {
            for (var index = visuals.Count - 1; index >= 0; index--)
            {
                var visual = visuals[index];
                visual.age += Time.deltaTime;
                var alpha = Mathf.Lerp(0.92f, 0f, visual.age / StrokeVisualLifetimeSeconds);
                var color = new Color(strokeColor.r, strokeColor.g, strokeColor.b, alpha);
                if (visual.line != null)
                {
                    visual.line.startColor = color;
                    visual.line.endColor = color;
                }

                if (visual.age >= StrokeVisualLifetimeSeconds)
                {
                    if (visual.body != null)
                    {
                        Destroy(visual.body);
                    }
                    visuals.RemoveAt(index);
                }
            }
        }

        private static Vector2 CenterOf(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            var points = strokes.SelectMany(stroke => stroke).Select(sample => sample.position).ToList();
            return points.Count == 0 ? Vector2.zero : new Vector2(points.Average(point => point.x), points.Average(point => point.y));
        }

        private sealed class StrokeVisual
        {
            public readonly GameObject body;
            public readonly LineRenderer line;
            public float age;

            public StrokeVisual(GameObject body, LineRenderer line)
            {
                this.body = body;
                this.line = line;
            }
        }
    }
}

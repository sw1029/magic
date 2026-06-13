using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    public sealed class WorldDrawingController : MonoBehaviour
    {
        public const float DefaultBufferSeconds = 1.75f;
        public const float DefaultMinPointDistance = 0.035f;
        public const float StrokeVisualLifetimeSeconds = WorldStrokeVisuals.DefaultStrokeVisualLifetimeSeconds;

        public Camera mainCamera = null!;
        public float bufferSeconds = DefaultBufferSeconds;
        public float minPointDistance = DefaultMinPointDistance;
        public Color strokeColor = new(0.22f, 0.95f, 1f, 0.92f);

        private StrokeSessionBuffer sessionBuffer = null!;
        private WorldPointerInputSource inputSource = null!;
        private WorldStrokeVisuals strokeVisuals = null!;
        private AudioSource drawingAudio = null!;
        private AudioClip[] penTickClips = Array.Empty<AudioClip>();
        private AudioClip[] penCompleteClips = Array.Empty<AudioClip>();
        private float lastPenTickAt = -10f;
        private bool wired;

        public event Action<StrokeInputSession> StrokeSessionCompleted = delegate { };
        public event Action<string, StrokeInputStroke> RawStrokeEvent = delegate { };
        public event Action<string> RawStrokeStateEvent = delegate { };

        // Legacy event kept for tests/tools that still hand off StrokeSample groups directly.
        public event Action<List<List<StrokeSample>>, Vector2, int> SpellBuffered = delegate { };
        public event Action InputCancelled = delegate { };

        public bool HasBufferedInput => (sessionBuffer?.HasPendingStrokes ?? false) || (inputSource?.IsDrawing ?? false);
        public bool HasActiveStrokeCenter => inputSource?.HasActiveWorldCenter == true;
        public Vector2 ActiveStrokeCenter => inputSource?.ActiveWorldCenter ?? Vector2.zero;
        public int BufferedStrokeCountForTests => sessionBuffer?.PendingStrokeCount ?? 0;
        public int StrokeVisualCountForTests => strokeVisuals?.VisualCountForTests ?? 0;

        public void ApplyPlayableDefaults()
        {
            bufferSeconds = DefaultBufferSeconds;
            minPointDistance = DefaultMinPointDistance;
            EnsureComponents();
            SyncOptions();
        }

        private void Awake()
        {
            mainCamera ??= Camera.main;
            EnsureComponents();
            SyncOptions();
            ConfigureDrawingAudio();
        }

        private void OnDestroy()
        {
            if (!wired || inputSource == null || sessionBuffer == null || strokeVisuals == null)
            {
                return;
            }

            inputSource.StrokeStarted -= strokeVisuals.HandleStrokeStarted;
            inputSource.StrokeStarted -= OnStrokeAudioStarted;
            inputSource.StrokeStarted -= OnRawStrokeStarted;
            inputSource.StrokeUpdated -= strokeVisuals.HandleStrokeUpdated;
            inputSource.StrokeUpdated -= OnStrokeAudioUpdated;
            inputSource.StrokeUpdated -= OnRawStrokeUpdated;
            inputSource.StrokeCompleted -= strokeVisuals.HandleStrokeCompleted;
            inputSource.StrokeCompleted -= OnStrokeAudioCompleted;
            inputSource.StrokeCompleted -= OnStrokeCompleted;
            inputSource.StrokeCompleted -= OnRawStrokeCompleted;
            inputSource.StrokeCanceled -= strokeVisuals.HandleStrokeCanceled;
            inputSource.StrokeCanceled -= OnRawStrokeCanceled;
            sessionBuffer.SessionCompleted -= OnSessionCompleted;
            wired = false;
        }

        private void Update()
        {
            EnsureComponents();
            SyncOptions();
            inputSource.Tick(Time.deltaTime);
            sessionBuffer.Tick(Time.time, inputSource.IsDrawing);
            strokeVisuals.Tick(Time.deltaTime);
        }

        public void SubmitSyntheticSpell(List<List<StrokeSample>> strokes)
        {
            var session = StrokeInputSessionExtensions.FromStrokeSamples(
                strokes ?? new List<List<StrokeSample>>(),
                $"synthetic-{Guid.NewGuid():N}",
                Time.time,
                InputCoordinateSpace.World);
            SubmitSyntheticSession(session);
        }

        public void SubmitSyntheticSession(StrokeInputSession session)
        {
            EnsureComponents();
            strokeVisuals.ShowCompletedSession(session);
            sessionBuffer.SubmitSession(session);
        }

        public bool CancelBufferedInput()
        {
            EnsureComponents();
            var hadInput = HasBufferedInput;
            var canceledActive = inputSource.CancelActiveStroke();
            sessionBuffer.Cancel();
            strokeVisuals.ClearAll();

            if (hadInput || canceledActive)
            {
                InputCancelled();
            }

            return hadInput || canceledActive;
        }

        public void SetStrokeColor(Color color)
        {
            strokeColor = color;
            if (strokeVisuals != null)
            {
                strokeVisuals.strokeColor = color;
            }
        }

        public void MarkLastBufferedStrokesRecognized(Color color)
        {
            EnsureComponents();
            strokeVisuals.MarkLastCompletedSessionRecognized(color);
        }

        public void MarkLastBufferedStrokesInvalid()
        {
            EnsureComponents();
            strokeVisuals.MarkLastCompletedSessionInvalid();
        }

        public void BufferStrokeForTests(List<StrokeSample> stroke)
        {
            if (stroke == null || stroke.Count < 2)
            {
                return;
            }

            EnsureComponents();
            var points = stroke.Select(sample => new StrokeInputPoint(sample.position, sample.time, 1f, 0, InputCoordinateSpace.World)).ToList();
            var inputStroke = new StrokeInputStroke($"buffered-test-{Guid.NewGuid():N}", points);
            strokeVisuals.HandleStrokeCompleted(inputStroke);
            sessionBuffer.PushCompletedStroke(inputStroke, Time.time);
        }

        private void EnsureComponents()
        {
            if (sessionBuffer == null)
            {
                sessionBuffer = new StrokeSessionBuffer(bufferSeconds);
            }

            inputSource = inputSource != null
                ? inputSource
                : gameObject.GetComponent<WorldPointerInputSource>() ?? gameObject.AddComponent<WorldPointerInputSource>();
            strokeVisuals = strokeVisuals != null
                ? strokeVisuals
                : gameObject.GetComponent<WorldStrokeVisuals>() ?? gameObject.AddComponent<WorldStrokeVisuals>();

            if (wired)
            {
                return;
            }

            inputSource.StrokeStarted += strokeVisuals.HandleStrokeStarted;
            inputSource.StrokeStarted += OnStrokeAudioStarted;
            inputSource.StrokeStarted += OnRawStrokeStarted;
            inputSource.StrokeUpdated += strokeVisuals.HandleStrokeUpdated;
            inputSource.StrokeUpdated += OnStrokeAudioUpdated;
            inputSource.StrokeUpdated += OnRawStrokeUpdated;
            inputSource.StrokeCompleted += strokeVisuals.HandleStrokeCompleted;
            inputSource.StrokeCompleted += OnStrokeAudioCompleted;
            inputSource.StrokeCompleted += OnStrokeCompleted;
            inputSource.StrokeCompleted += OnRawStrokeCompleted;
            inputSource.StrokeCanceled += strokeVisuals.HandleStrokeCanceled;
            inputSource.StrokeCanceled += OnRawStrokeCanceled;
            sessionBuffer.SessionCompleted += OnSessionCompleted;
            wired = true;
        }

        private void SyncOptions()
        {
            if (inputSource != null)
            {
                inputSource.mainCamera = mainCamera;
                inputSource.minPointDistance = minPointDistance;
            }

            if (sessionBuffer != null)
            {
                sessionBuffer.BufferSeconds = bufferSeconds;
            }

            if (strokeVisuals != null)
            {
                strokeVisuals.strokeColor = strokeColor;
                strokeVisuals.lifetimeSeconds = StrokeVisualLifetimeSeconds;
            }
        }

        private void OnStrokeCompleted(StrokeInputStroke stroke)
        {
            sessionBuffer.PushCompletedStroke(stroke, Time.time);
        }

        private void OnRawStrokeStarted(StrokeInputStroke stroke)
        {
            RawStrokeEvent("stroke_started", stroke);
        }

        private void OnRawStrokeUpdated(StrokeInputStroke stroke)
        {
            RawStrokeEvent("stroke_updated", stroke);
        }

        private void OnRawStrokeCompleted(StrokeInputStroke stroke)
        {
            RawStrokeEvent("stroke_completed", stroke);
        }

        private void OnRawStrokeCanceled()
        {
            RawStrokeStateEvent("stroke_canceled");
        }

        private void OnStrokeAudioStarted(StrokeInputStroke stroke)
        {
            PlayRandomDrawingClip(penTickClips, 0.26f);
            lastPenTickAt = Time.time;
        }

        private void OnStrokeAudioUpdated(StrokeInputStroke stroke)
        {
            if (Time.time - lastPenTickAt < 0.18f)
            {
                return;
            }

            PlayRandomDrawingClip(penTickClips, 0.16f);
            lastPenTickAt = Time.time;
        }

        private void OnStrokeAudioCompleted(StrokeInputStroke stroke)
        {
            PlayRandomDrawingClip(penCompleteClips, 0.22f);
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
            drawingAudio.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)], volume * MagicExamSettings.SfxVolume);
            drawingAudio.pitch = 1f;
        }

        private void OnSessionCompleted(StrokeInputSession session)
        {
            StrokeSessionCompleted(session);
            var samples = session.ToStrokeSamples();
            SpellBuffered(samples, session.GetWorldCenter(), samples.Count);
        }
    }
}

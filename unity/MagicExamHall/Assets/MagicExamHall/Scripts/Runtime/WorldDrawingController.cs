using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicExamHall
{
    public sealed class WorldDrawingController : MonoBehaviour
    {
        public const float DefaultBufferSeconds = 1.05f;
        public const float DefaultMinPointDistance = 0.05f;
        public const float StrokeVisualLifetimeSeconds = WorldStrokeVisuals.DefaultStrokeVisualLifetimeSeconds;

        public Camera mainCamera = null!;
        public float bufferSeconds = DefaultBufferSeconds;
        public float minPointDistance = DefaultMinPointDistance;
        public Color strokeColor = new(0.22f, 0.95f, 1f, 0.92f);

        private StrokeSessionBuffer sessionBuffer = null!;
        private WorldPointerInputSource inputSource = null!;
        private WorldStrokeVisuals strokeVisuals = null!;
        private bool wired;

        public event Action<StrokeInputSession> StrokeSessionCompleted = delegate { };

        // Legacy event kept for tests/tools that still hand off StrokeSample groups directly.
        public event Action<List<List<StrokeSample>>, Vector2, int> SpellBuffered = delegate { };
        public event Action InputCancelled = delegate { };

        public bool HasBufferedInput => (sessionBuffer?.HasPendingStrokes ?? false) || (inputSource?.IsDrawing ?? false);
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
        }

        private void OnDestroy()
        {
            if (!wired || inputSource == null || sessionBuffer == null || strokeVisuals == null)
            {
                return;
            }

            inputSource.StrokeStarted -= strokeVisuals.HandleStrokeStarted;
            inputSource.StrokeUpdated -= strokeVisuals.HandleStrokeUpdated;
            inputSource.StrokeCompleted -= strokeVisuals.HandleStrokeCompleted;
            inputSource.StrokeCompleted -= OnStrokeCompleted;
            inputSource.StrokeCanceled -= strokeVisuals.HandleStrokeCanceled;
            sessionBuffer.SessionCompleted -= OnSessionCompleted;
            wired = false;
        }

        private void Update()
        {
            EnsureComponents();
            SyncOptions();
            inputSource.Tick(Time.deltaTime);
            sessionBuffer.Tick(Time.time);
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
            inputSource.StrokeUpdated += strokeVisuals.HandleStrokeUpdated;
            inputSource.StrokeCompleted += strokeVisuals.HandleStrokeCompleted;
            inputSource.StrokeCompleted += OnStrokeCompleted;
            inputSource.StrokeCanceled += strokeVisuals.HandleStrokeCanceled;
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

        private void OnSessionCompleted(StrokeInputSession session)
        {
            StrokeSessionCompleted(session);
            var samples = session.ToStrokeSamples();
            SpellBuffered(samples, session.GetWorldCenter(), samples.Count);
        }
    }
}

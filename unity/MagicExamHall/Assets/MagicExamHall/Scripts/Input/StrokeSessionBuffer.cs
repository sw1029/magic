using System;
using System.Collections.Generic;
using System.Linq;

namespace MagicExamHall
{
    public sealed class StrokeSessionBuffer
    {
        private readonly List<StrokeInputStroke> pendingStrokes = new();
        private bool waitingForBuffer;
        private float lastStrokeCompletedAt;
        private double startedAtSeconds;

        public StrokeSessionBuffer(float bufferSeconds)
        {
            BufferSeconds = bufferSeconds;
        }

        public event Action<StrokeInputSession> SessionCompleted = delegate { };

        public float BufferSeconds { get; set; }
        public bool HasPendingStrokes => pendingStrokes.Count > 0;
        public int PendingStrokeCount => pendingStrokes.Count;

        public void PushCompletedStroke(StrokeInputStroke stroke, float now)
        {
            if (stroke == null || stroke.Points.Count < 2)
            {
                return;
            }

            if (pendingStrokes.Count == 0)
            {
                startedAtSeconds = stroke.Points[0].TimeSeconds;
            }

            pendingStrokes.Add(stroke);
            waitingForBuffer = true;
            lastStrokeCompletedAt = now;
        }

        public void Tick(float now)
        {
            if (!waitingForBuffer || now - lastStrokeCompletedAt < BufferSeconds)
            {
                return;
            }

            Flush(now);
        }

        public void Flush(float now)
        {
            waitingForBuffer = false;
            if (pendingStrokes.Count == 0)
            {
                return;
            }

            var session = new StrokeInputSession(
                $"session-{Guid.NewGuid():N}",
                pendingStrokes.ToList(),
                startedAtSeconds,
                now,
                InputCoordinateSpace.World);
            pendingStrokes.Clear();
            SessionCompleted(session);
        }

        public void SubmitSession(StrokeInputSession session)
        {
            pendingStrokes.Clear();
            waitingForBuffer = false;
            SessionCompleted(session);
        }

        public void Cancel()
        {
            pendingStrokes.Clear();
            waitingForBuffer = false;
            startedAtSeconds = 0d;
        }
    }
}

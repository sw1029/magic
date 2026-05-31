using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicExamHall
{
    public enum InputCoordinateSpace
    {
        Screen,
        World,
        Normalized
    }

    public readonly struct StrokeInputPoint
    {
        public readonly Vector2 Position;
        public readonly float TimeSeconds;
        public readonly float Pressure;
        public readonly int PointerId;
        public readonly InputCoordinateSpace CoordinateSpace;

        public StrokeInputPoint(
            Vector2 position,
            float timeSeconds,
            float pressure = 1f,
            int pointerId = 0,
            InputCoordinateSpace coordinateSpace = InputCoordinateSpace.World)
        {
            Position = position;
            TimeSeconds = timeSeconds;
            Pressure = pressure;
            PointerId = pointerId;
            CoordinateSpace = coordinateSpace;
        }
    }

    public sealed class StrokeInputStroke
    {
        public StrokeInputStroke(string id, IEnumerable<StrokeInputPoint> points)
        {
            Id = string.IsNullOrWhiteSpace(id) ? $"stroke-{Guid.NewGuid():N}" : id;
            Points = points == null ? Array.Empty<StrokeInputPoint>() : points.ToList().AsReadOnly();
        }

        public string Id { get; }
        public IReadOnlyList<StrokeInputPoint> Points { get; }

        public StrokeInputStroke WithPoint(StrokeInputPoint point)
        {
            var next = Points.ToList();
            next.Add(point);
            return new StrokeInputStroke(Id, next);
        }

        public List<StrokeSample> ToStrokeSamples()
        {
            return Points
                .Select(point => new StrokeSample(point.Position, point.TimeSeconds))
                .ToList();
        }
    }

    public sealed class StrokeInputSession
    {
        public StrokeInputSession(
            string id,
            IEnumerable<StrokeInputStroke> strokes,
            double startedAtSeconds,
            double endedAtSeconds,
            InputCoordinateSpace coordinateSpace = InputCoordinateSpace.World)
        {
            Id = string.IsNullOrWhiteSpace(id) ? $"session-{Guid.NewGuid():N}" : id;
            Strokes = strokes == null ? Array.Empty<StrokeInputStroke>() : strokes.ToList().AsReadOnly();
            StartedAtSeconds = startedAtSeconds;
            EndedAtSeconds = endedAtSeconds;
            CoordinateSpace = coordinateSpace;
        }

        public string Id { get; }
        public IReadOnlyList<StrokeInputStroke> Strokes { get; }
        public double StartedAtSeconds { get; }
        public double EndedAtSeconds { get; }
        public InputCoordinateSpace CoordinateSpace { get; }
    }

    public static class StrokeInputSessionExtensions
    {
        public static StrokeInputSession FromStrokeSamples(
            IReadOnlyList<IReadOnlyList<StrokeSample>> strokes,
            string id,
            double now,
            InputCoordinateSpace coordinateSpace = InputCoordinateSpace.World)
        {
            var inputStrokes = new List<StrokeInputStroke>();
            var startedAt = now;
            var endedAt = now;

            for (var strokeIndex = 0; strokeIndex < (strokes?.Count ?? 0); strokeIndex++)
            {
                var points = new List<StrokeInputPoint>();
                var stroke = strokes![strokeIndex];

                for (var pointIndex = 0; pointIndex < stroke.Count; pointIndex++)
                {
                    var sample = stroke[pointIndex];
                    if (float.IsFinite(sample.position.x) && float.IsFinite(sample.position.y))
                    {
                        points.Add(new StrokeInputPoint(sample.position, sample.time, 1f, 0, coordinateSpace));
                        startedAt = Math.Min(startedAt, sample.time);
                        endedAt = Math.Max(endedAt, sample.time);
                    }
                }

                inputStrokes.Add(new StrokeInputStroke($"stroke-{strokeIndex + 1}", points));
            }

            if (inputStrokes.Count == 0)
            {
                startedAt = now;
                endedAt = now;
            }

            return new StrokeInputSession(id, inputStrokes, startedAt, endedAt, coordinateSpace);
        }

        public static List<List<StrokeSample>> ToStrokeSamples(this StrokeInputSession session)
        {
            return session.Strokes
                .Select(stroke => stroke.ToStrokeSamples())
                .Where(stroke => stroke.Count >= 2)
                .ToList();
        }

        public static Vector2 GetWorldCenter(this StrokeInputSession session)
        {
            var points = session.Strokes.SelectMany(stroke => stroke.Points).Select(point => point.Position).ToList();
            return points.Count == 0
                ? Vector2.zero
                : new Vector2(points.Average(point => point.x), points.Average(point => point.y));
        }

        public static float EstimateWorldScale(this StrokeInputSession session)
        {
            var points = session.Strokes.SelectMany(stroke => stroke.Points).Select(point => point.Position).ToList();
            if (points.Count == 0)
            {
                return 1f;
            }

            var min = new Vector2(points.Min(point => point.x), points.Min(point => point.y));
            var max = new Vector2(points.Max(point => point.x), points.Max(point => point.y));
            return Mathf.Max(Vector2.Distance(min, max), 0.5f);
        }
    }
}

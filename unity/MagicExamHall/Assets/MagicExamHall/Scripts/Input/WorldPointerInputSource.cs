using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MagicExamHall
{
    public sealed class WorldPointerInputSource : MonoBehaviour
    {
        public Camera mainCamera = null!;
        public int mouseButton = 1;
        public float minPointDistance = WorldDrawingController.DefaultMinPointDistance;
        public bool ignorePointerOverUi = true;

        private readonly List<StrokeInputPoint> activePoints = new();
        private bool drawing;
        private string activeStrokeId = "";

        public event Action<StrokeInputStroke> StrokeStarted = delegate { };
        public event Action<StrokeInputStroke> StrokeUpdated = delegate { };
        public event Action<StrokeInputStroke> StrokeCompleted = delegate { };
        public event Action StrokeCanceled = delegate { };

        public bool IsDrawing => drawing;

        private void Awake()
        {
            mainCamera ??= Camera.main;
        }

        public void Tick(float deltaTime)
        {
            if (mainCamera == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(mouseButton) && !PointerIsOverUi())
            {
                BeginStroke(Input.mousePosition);
            }

            if (drawing && Input.GetMouseButton(mouseButton))
            {
                AddPoint(Input.mousePosition);
            }

            if (drawing && Input.GetMouseButtonUp(mouseButton))
            {
                CompleteStroke(Input.mousePosition);
            }
        }

        public bool CancelActiveStroke()
        {
            if (!drawing && activePoints.Count == 0)
            {
                return false;
            }

            drawing = false;
            activePoints.Clear();
            activeStrokeId = "";
            StrokeCanceled();
            return true;
        }

        private void BeginStroke(Vector2 screenPoint)
        {
            drawing = true;
            activeStrokeId = $"stroke-{Guid.NewGuid():N}";
            activePoints.Clear();
            AddPoint(screenPoint, force: true);
            StrokeStarted(CurrentStroke());
        }

        private void CompleteStroke(Vector2 screenPoint)
        {
            AddPoint(screenPoint);
            drawing = false;

            if (activePoints.Count >= 2)
            {
                StrokeCompleted(CurrentStroke());
            }
            else
            {
                StrokeCanceled();
            }

            activePoints.Clear();
            activeStrokeId = "";
        }

        private void AddPoint(Vector2 screenPoint, bool force = false)
        {
            var world = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, -mainCamera.transform.position.z));
            var point = new StrokeInputPoint(new Vector2(world.x, world.y), Time.time, 1f, 0, InputCoordinateSpace.World);
            if (!force && activePoints.Count > 0 && Vector2.Distance(activePoints[^1].Position, point.Position) < minPointDistance)
            {
                return;
            }

            activePoints.Add(point);
            if (drawing)
            {
                StrokeUpdated(CurrentStroke());
            }
        }

        private StrokeInputStroke CurrentStroke()
        {
            return new StrokeInputStroke(activeStrokeId, activePoints);
        }

        private bool PointerIsOverUi()
        {
            return ignorePointerOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}

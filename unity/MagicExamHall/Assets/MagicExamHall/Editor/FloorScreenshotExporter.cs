using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MagicExamHall.Editor
{
    public static class FloorScreenshotExporter
    {
        private const string ScenePath = "Assets/Scenes/MagicExamHall.unity";
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;

        [MenuItem("Magic Exam Hall/Export Floor Screenshots")]
        public static void Export()
        {
            var output = GetArgument("-floorScreenshotOutput");
            if (string.IsNullOrWhiteSpace(output))
            {
                output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "outputs", "unity-floor-screens"));
            }

            Export(output);
        }

        [MenuItem("Magic Exam Hall/Export All UI Screens")]
        public static void ExportAllScreens()
        {
            var output = GetArgument("-floorScreenshotOutput");
            if (string.IsNullOrWhiteSpace(output))
            {
                output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "outputs", "unity-ui-screens"));
            }

            ExportAllScreens(output);
        }

        public static void ExportAllScreens(string output)
        {
            Directory.CreateDirectory(output);
            EditorSceneManager.OpenScene(ScenePath);

            var controller = UnityEngine.Object.FindFirstObjectByType<ExamGameController>();
            if (controller == null)
            {
                throw new InvalidOperationException("ExamGameController was not found in the scene.");
            }

            PrepareController(controller);
            var boot = UnityEngine.Object.FindFirstObjectByType<GameBootController>();
            if (boot == null)
            {
                throw new InvalidOperationException("GameBootController was not found in the scene.");
            }

            var camera = controller.mainCamera != null ? controller.mainCamera : Camera.main;
            var canvas = controller.canvas;
            if (camera == null || canvas == null)
            {
                throw new InvalidOperationException("A camera and canvas are required for screenshots.");
            }

            WithCaptureCanvas(camera, canvas, () =>
            {
                Canvas.ForceUpdateCanvases();
                Capture(camera, output, "ui_01_title.png");

                boot.ShowMainMenuForTests();
                Canvas.ForceUpdateCanvases();
                Capture(camera, output, "ui_02_menu.png");

                InvokePrivate(boot, "ShowOptions", GameBootState.MainMenu);
                Canvas.ForceUpdateCanvases();
                Capture(camera, output, "ui_03_options.png");

                boot.StartNewGameForTests();
                controller.CloseFirstFloorLetterForTests();
                InvokePrivate(controller, "UpdateHud");
                Canvas.ForceUpdateCanvases();
                Capture(camera, output, "ui_04_gameplay.png");

                boot.ShowPauseForTests();
                Canvas.ForceUpdateCanvases();
                Capture(camera, output, "ui_05_pause.png");

                boot.ResumeGameplayForTests();
                boot.ShowCodexForTests();
                Canvas.ForceUpdateCanvases();
                Capture(camera, output, "ui_06_codex.png");

                boot.ResumeGameplayForTests();
                boot.ShowDiscoveryCodexForTests();
                Canvas.ForceUpdateCanvases();
                Capture(camera, output, "ui_07_codex_discovery.png");

                boot.ResumeGameplayForTests();
                for (var floor = 0; floor < controller.FloorCount; floor++)
                {
                    controller.LoadFloorForTests(floor);
                    InvokePrivate(controller, "UpdateHud");
                    Canvas.ForceUpdateCanvases();
                    Capture(camera, output, $"unity_floor_{floor + 1:00}.png");
                }

                controller.CompleteCurrentFloorForTests();
                controller.AdvanceFloorForTests();
                Canvas.ForceUpdateCanvases();
                Capture(camera, output, "ui_08_ending_report.png");
            });

            Debug.Log($"All UI screenshots exported to {output}");
        }

        public static void Export(string output)
        {
            Directory.CreateDirectory(output);
            EditorSceneManager.OpenScene(ScenePath);

            var controller = UnityEngine.Object.FindFirstObjectByType<ExamGameController>();
            if (controller == null)
            {
                throw new InvalidOperationException("ExamGameController was not found in the scene.");
            }

            PrepareController(controller);
            var camera = controller.mainCamera != null ? controller.mainCamera : Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("No camera was available for screenshots.");
            }

            var canvas = controller.canvas;
            if (canvas == null)
            {
                throw new InvalidOperationException("No canvas was available for screenshots.");
            }

            var originalCameraPosition = camera.transform.position;
            var originalOrthographic = camera.orthographic;
            var originalOrthographicSize = camera.orthographicSize;
            var originalMode = canvas.renderMode;
            var originalCamera = canvas.worldCamera;
            var originalPlane = canvas.planeDistance;

            camera.transform.position = new Vector3(0f, 0f, -10f);
            PixelRenderSetup.ConfigureCamera(camera, ExamGameController.GameplayCameraOrthographicSize, camera.backgroundColor);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            try
            {
                for (var floor = 0; floor < controller.FloorCount; floor++)
                {
                    controller.LoadFloorForTests(floor);
                    InvokePrivate(controller, "UpdateHud");
                    Canvas.ForceUpdateCanvases();
                    Capture(camera, output, $"unity_floor_{floor + 1:00}.png");
                }
            }
            finally
            {
                camera.transform.position = originalCameraPosition;
                camera.orthographic = originalOrthographic;
                camera.orthographicSize = originalOrthographicSize;
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCamera;
                canvas.planeDistance = originalPlane;
            }

            Debug.Log($"Floor screenshots exported to {output}");
        }

        private static void WithCaptureCanvas(Camera camera, Canvas canvas, Action capture)
        {
            var originalCameraPosition = camera.transform.position;
            var originalOrthographic = camera.orthographic;
            var originalOrthographicSize = camera.orthographicSize;
            var originalMode = canvas.renderMode;
            var originalCamera = canvas.worldCamera;
            var originalPlane = canvas.planeDistance;
            var originalOverrideSorting = canvas.overrideSorting;
            var originalSortingOrder = canvas.sortingOrder;

            camera.transform.position = new Vector3(0f, 0f, -10f);
            PixelRenderSetup.ConfigureCamera(camera, ExamGameController.GameplayCameraOrthographicSize, camera.backgroundColor);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10000;

            try
            {
                capture();
            }
            finally
            {
                camera.transform.position = originalCameraPosition;
                camera.orthographic = originalOrthographic;
                camera.orthographicSize = originalOrthographicSize;
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCamera;
                canvas.planeDistance = originalPlane;
                canvas.overrideSorting = originalOverrideSorting;
                canvas.sortingOrder = originalSortingOrder;
            }
        }

        private static void PrepareController(ExamGameController controller)
        {
            if (controller.ActiveGoalCount == 0)
            {
                InvokePrivate(controller, "Awake");
            }
        }

        private static void Capture(Camera camera, string output, string fileName)
        {
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24)
            {
                antiAliasing = 1
            };
            var texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                texture.Apply();

                var path = Path.Combine(output, fileName);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Debug.Log($"Saved screenshot: {path}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().Name, methodName);
            }

            method.Invoke(target, args);
        }

        private static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : "";
        }
    }
}

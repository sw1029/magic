using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MagicExamHall.Editor
{
    public static class MagicExamHallBuildPipeline
    {
        private const string ScenePath = "Assets/Scenes/MagicExamHall.unity";
        private const string DefaultBuildPath = "Builds/MagicExamHall.exe";
        private const string BuildPathArgument = "-magicExamHallBuildPath";
        private const int DefaultWindowWidth = 1280;
        private const int DefaultWindowHeight = 800;

        [MenuItem("Magic Exam Hall/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            BuildWindowsPlayer(ResolveBuildPath(Environment.GetCommandLineArgs()));
        }

        public static BuildReport BuildWindowsPlayer(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("A Windows player output path is required.", nameof(outputPath));
            }

            EnsureSceneRegistered();
            ConfigureWindowedPlayerSettings();

            var absoluteOutputPath = Path.GetFullPath(outputPath);
            var outputDirectory = Path.GetDirectoryName(absoluteOutputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            Debug.Log($"Building Magic Exam Hall Windows player at {absoluteOutputPath}");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = absoluteOutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            var summary = report.summary;
            Debug.Log(
                $"Magic Exam Hall Windows player build finished with {summary.result}: {summary.totalSize} bytes, " +
                $"{summary.totalErrors} errors, {summary.totalWarnings} warnings.");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Magic Exam Hall Windows player build failed with {summary.result}: " +
                    $"{summary.totalErrors} errors, {summary.totalWarnings} warnings.");
            }

            return report;
        }

        internal static string ResolveBuildPath(string[] args)
        {
            if (args == null)
            {
                return DefaultBuildPath;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, BuildPathArgument, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
                    {
                        throw new ArgumentException($"{BuildPathArgument} requires a non-empty output path.");
                    }

                    return args[i + 1];
                }

                var inlinePrefix = BuildPathArgument + "=";
                if (arg.StartsWith(inlinePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var inlinePath = arg.Substring(inlinePrefix.Length);
                    if (string.IsNullOrWhiteSpace(inlinePath))
                    {
                        throw new ArgumentException($"{BuildPathArgument} requires a non-empty output path.");
                    }

                    return inlinePath;
                }
            }

            return DefaultBuildPath;
        }

        private static void EnsureSceneRegistered()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene == null)
            {
                throw new FileNotFoundException($"Could not find Magic Exam Hall scene at {ScenePath}.", ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureWindowedPlayerSettings()
        {
            PlayerSettings.defaultScreenWidth = DefaultWindowWidth;
            PlayerSettings.defaultScreenHeight = DefaultWindowHeight;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.allowFullscreenSwitch = true;
        }
    }
}

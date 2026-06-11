using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MagicExamHall
{
    public static class PixelRenderSetup
    {
        public const int ReferenceResolutionX = 632;
        public const int ReferenceResolutionY = 356;
        public const int AssetsPixelsPerUnit = 32;
        public const float DefaultGlobalLightIntensity = 0.42f;
        public const float DefaultPlayerLightIntensity = 0.24f;
        public const string GlobalLightName = "Global 2D Light";
        public const string PlayerCastingLightName = "Player Casting Light 2D";

        public static PixelPerfectCamera ConfigureCamera(Camera camera, float orthographicSize, Color backgroundColor)
        {
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;

            var pixelPerfect = camera.GetComponent<PixelPerfectCamera>() ?? camera.gameObject.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = AssetsPixelsPerUnit;
            pixelPerfect.refResolutionX = ReferenceResolutionX;
            pixelPerfect.refResolutionY = ReferenceResolutionY;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;

            // Preserve the existing room framing while the component still snaps camera movement to its pixel grid.
            pixelPerfect.CorrectCinemachineOrthoSize(orthographicSize);
            camera.orthographicSize = orthographicSize;
            return pixelPerfect;
        }

        public static Light2D EnsureGlobalLight(Transform parent = null)
        {
            var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.lightType == Light2D.LightType.Global)
                {
                    return light;
                }
            }

            var lightObject = new GameObject(GlobalLightName);
            if (parent != null)
            {
                lightObject.transform.SetParent(parent, false);
            }

            var globalLight = lightObject.AddComponent<Light2D>();
            ConfigureGlobalLight(globalLight, Color.white, DefaultGlobalLightIntensity);
            return globalLight;
        }

        public static void ConfigureGlobalLight(Light2D light, Color color, float intensity = DefaultGlobalLightIntensity)
        {
            light.name = GlobalLightName;
            light.lightType = Light2D.LightType.Global;
            light.color = color;
            light.intensity = intensity;
            light.blendStyleIndex = 0;
            light.volumetricEnabled = false;
            light.shadowsEnabled = false;
        }

        public static Light2D EnsurePlayerCastingLight(Transform player)
        {
            var light = player.Find(PlayerCastingLightName)?.GetComponent<Light2D>();
            if (light == null)
            {
                var lightObject = new GameObject(PlayerCastingLightName);
                lightObject.transform.SetParent(player, false);
                lightObject.transform.localPosition = Vector3.zero;
                light = lightObject.AddComponent<Light2D>();
            }

            ConfigurePointLight(light, new Color(0.48f, 0.84f, 1f), DefaultPlayerLightIntensity, 0.08f, 1.15f);
            return light;
        }

        public static Light2D ConfigureSpriteLight(GameObject target, PixelSpriteKind kind, Color primary, Color secondary, string spriteName)
        {
            return kind switch
            {
                PixelSpriteKind.Candle => EnsurePointLight(target, $"{spriteName} Flame Light 2D", secondary, 0.88f, 0.12f, 1.95f),
                PixelSpriteKind.RuneCircle => EnsurePointLight(target, $"{spriteName} Rune Light 2D", primary, 0.52f, 0.16f, 2.35f),
                PixelSpriteKind.Pulse => EnsurePointLight(target, $"{spriteName} Pulse Light 2D", primary, 0.46f, 0.02f, 1.45f),
                PixelSpriteKind.Station => EnsurePointLight(target, $"{spriteName} Core Light 2D", primary, 0.32f, 0.10f, 1.55f),
                PixelSpriteKind.Target => EnsurePointLight(target, $"{spriteName} Target Light 2D", primary, 0.30f, 0.08f, 1.30f),
                _ => null
            };
        }

        private static Light2D EnsurePointLight(GameObject target, string lightName, Color color, float intensity, float innerRadius, float outerRadius)
        {
            var light = target.transform.Find(lightName)?.GetComponent<Light2D>();
            if (light == null)
            {
                var lightObject = new GameObject(lightName);
                lightObject.transform.SetParent(target.transform, false);
                lightObject.transform.localPosition = Vector3.zero;
                light = lightObject.AddComponent<Light2D>();
            }

            ConfigurePointLight(light, color, intensity, innerRadius, outerRadius);
            return light;
        }

        private static void ConfigurePointLight(Light2D light, Color color, float intensity, float innerRadius, float outerRadius)
        {
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = innerRadius;
            light.pointLightOuterRadius = outerRadius;
            light.pointLightInnerAngle = 360f;
            light.pointLightOuterAngle = 360f;
            light.falloffIntensity = 0.72f;
            light.blendStyleIndex = 0;
            light.volumetricEnabled = false;
            light.shadowsEnabled = false;
        }
    }
}

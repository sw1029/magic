using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MagicExamHall.Editor
{
    public static class PropVariantSpriteGenerator
    {
        private const string OutputFolder = "Assets/MagicExamHall/Resources/Sprites";

        private static readonly Dictionary<PixelSpriteKind, (Color primary, Color secondary)> Palettes = new()
        {
            [PixelSpriteKind.Bookshelf] = (new Color(0.42f, 0.23f, 0.12f, 1f), new Color(0.42f, 0.80f, 0.88f, 1f)),
            [PixelSpriteKind.Candle] = (new Color(0.63f, 0.57f, 0.44f, 1f), new Color(1f, 0.56f, 0.15f, 1f)),
            [PixelSpriteKind.FloorGuard] = (new Color(0.28f, 0.30f, 0.34f, 1f), new Color(0.95f, 0.72f, 0.34f, 1f)),
            [PixelSpriteKind.WallCorner] = (new Color(0.24f, 0.22f, 0.30f, 1f), new Color(0.48f, 0.84f, 1f, 1f)),
            [PixelSpriteKind.Pillar] = (new Color(0.27f, 0.25f, 0.31f, 1f), new Color(0.95f, 0.72f, 0.34f, 1f))
        };

        [MenuItem("Magic Exam Hall/Generate Prop Variant Sprites")]
        public static void Generate()
        {
            Directory.CreateDirectory(OutputFolder);
            var writtenAssetPaths = new List<string>();

            foreach (var entry in Palettes)
            {
                var kind = entry.Key;
                var palette = entry.Value;
                for (var variant = 0; variant < PixelArtFactory.GetVariantCount(kind); variant++)
                {
                    writtenAssetPaths.Add(Write(kind, variant, palette.primary, palette.secondary, variantName: true));
                }

                var basePath = Path.Combine(OutputFolder, $"{kind}.png");
                if (!File.Exists(basePath))
                {
                    writtenAssetPaths.Add(Write(kind, 0, palette.primary, palette.secondary, variantName: false));
                }
            }

            AssetDatabase.Refresh();
            foreach (var assetPath in writtenAssetPaths)
            {
                ConfigureImporter(assetPath);
            }

            PixelArtFactory.ResetExternalSpriteCache();
            AssetDatabase.SaveAssets();
            Debug.Log($"Generated prop variant sprites in {OutputFolder}");
        }

        private static string Write(PixelSpriteKind kind, int variant, Color primary, Color secondary, bool variantName)
        {
            var fileName = variantName ? $"{kind}_{variant}.png" : $"{kind}.png";
            var texture = PixelArtFactory.CreateProceduralTexture($"{kind}_{variant}", primary, secondary, kind, variant);
            var fullPath = Path.Combine(OutputFolder, fileName);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            return fullPath.Replace("\\", "/");
        }

        private static void ConfigureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelArtFactory.SpritePixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
    }
}

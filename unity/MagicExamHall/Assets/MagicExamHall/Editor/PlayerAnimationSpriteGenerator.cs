using System.IO;
using UnityEditor;
using UnityEngine;

namespace MagicExamHall.Editor
{
    public static class PlayerAnimationSpriteGenerator
    {
        private const int Width = PlayerSpriteLibrary.FrameWidth;
        private const int Height = PlayerSpriteLibrary.FrameHeight;
        private const string OutputFolder = "Assets/MagicExamHall/Resources/Sprites/Player";

        private static readonly Color Transparent = new(0f, 0f, 0f, 0f);
        private static readonly Color Outline = new(0.035f, 0.032f, 0.045f, 1f);
        private static readonly Color Skin = new(0.95f, 0.78f, 0.58f, 1f);
        private static readonly Color SkinShade = new(0.74f, 0.49f, 0.34f, 1f);
        private static readonly Color RobeDark = new(0.08f, 0.18f, 0.33f, 1f);
        private static readonly Color Robe = new(0.20f, 0.48f, 0.85f, 1f);
        private static readonly Color RobeLight = new(0.52f, 0.82f, 1f, 1f);
        private static readonly Color Hair = new(0.18f, 0.10f, 0.08f, 1f);
        private static readonly Color Gold = new(0.96f, 0.72f, 0.25f, 1f);
        private static readonly Color GlowBlue = new(0.42f, 0.88f, 1f, 1f);
        private static readonly Color GlowWarm = new(1f, 0.82f, 0.38f, 1f);
        private static readonly Color Shadow = new(0f, 0f, 0f, 0.32f);

        [MenuItem("Magic Exam Hall/Generate Player Animation Sprites")]
        public static void Generate()
        {
            Directory.CreateDirectory(OutputFolder);

            foreach (var facing in new[] { PlayerFacing.Down, PlayerFacing.Up, PlayerFacing.Left, PlayerFacing.Right })
            {
                for (var frame = 0; frame < 2; frame++)
                {
                    WriteDirectional("idle", facing, frame, frame);
                }

                for (var frame = 0; frame < 4; frame++)
                {
                    WriteDirectional("walk", facing, frame, frame);
                }
            }

            for (var frame = 0; frame < 3; frame++)
            {
                WriteCast("cast_charge", frame, false);
            }

            for (var frame = 0; frame < 2; frame++)
            {
                WriteCast("cast_release", frame, true);
            }

            AssetDatabase.Refresh();
            foreach (var assetPath in Directory.GetFiles(OutputFolder, "*.png"))
            {
                ConfigureImporter(assetPath.Replace("\\", "/"));
            }

            PlayerSpriteLibrary.ResetCache();
            PixelArtFactory.ResetExternalSpriteCache();
            AssetDatabase.SaveAssets();
            Debug.Log($"Generated player animation sprites in {OutputFolder}");
        }

        private static void WriteDirectional(string state, PlayerFacing facing, int frame, int motionFrame)
        {
            var texture = CreateTexture();
            var breathe = state == "idle" && frame == 1 ? 1 : 0;
            var step = state == "walk" ? WalkOffset(motionFrame) : 0;
            DrawPlayer(texture, facing, breathe, step, casting: false, release: false, frame);
            Save(texture, $"{state}_{PlayerSpriteLibrary.Key(facing)}_{frame}.png");
        }

        private static void WriteCast(string state, int frame, bool release)
        {
            var texture = CreateTexture();
            DrawPlayer(texture, PlayerFacing.Down, breathe: 0, step: 0, casting: true, release, frame);
            Save(texture, $"{state}_{frame}.png");
        }

        private static Texture2D CreateTexture()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    texture.SetPixel(x, y, Transparent);
                }
            }

            return texture;
        }

        private static void DrawPlayer(Texture2D texture, PlayerFacing facing, int breathe, int step, bool casting, bool release, int frame)
        {
            Ellipse(texture, 24, 10, 14, 4, Shadow);

            var bodyY = 15 + breathe;
            var headY = 39 + breathe;
            var leftFoot = Mathf.Max(0, -step);
            var rightFoot = Mathf.Max(0, step);

            DrawRobe(texture, bodyY, step);
            DrawFeet(texture, leftFoot, rightFoot);

            switch (facing)
            {
                case PlayerFacing.Up:
                    DrawBackHead(texture, headY);
                    DrawBackArms(texture, bodyY, step);
                    break;
                case PlayerFacing.Left:
                    DrawProfileHead(texture, headY, left: true);
                    DrawSideArms(texture, bodyY, left: true, step);
                    break;
                case PlayerFacing.Right:
                    DrawProfileHead(texture, headY, left: false);
                    DrawSideArms(texture, bodyY, left: false, step);
                    break;
                default:
                    DrawFrontHead(texture, headY);
                    DrawFrontArms(texture, bodyY, step);
                    break;
            }

            if (casting)
            {
                DrawCastingPose(texture, release, frame);
            }

            texture.Apply();
        }

        private static void DrawRobe(Texture2D texture, int bodyY, int step)
        {
            Fill(texture, 17, bodyY, 18, 28, Outline);
            Fill(texture, 18, bodyY + 1, 16, 26, RobeDark);
            Fill(texture, 20, bodyY + 4, 12, 22, Robe);
            Fill(texture, 25, bodyY + 5, 3, 21, RobeLight);
            Fill(texture, 22, bodyY + 7, 2, 18, Gold);
            Fill(texture, 29, bodyY + 7, 1, 18, Gold);
            Fill(texture, 16, bodyY + 2 + Mathf.Max(0, step), 6, 4, Outline);
            Fill(texture, 30, bodyY + 2 + Mathf.Max(0, -step), 6, 4, Outline);
        }

        private static void DrawFeet(Texture2D texture, int leftFoot, int rightFoot)
        {
            Fill(texture, 17, 9 + leftFoot, 8, 4, Outline);
            Fill(texture, 29, 9 + rightFoot, 8, 4, Outline);
            Fill(texture, 18, 10 + leftFoot, 6, 2, RobeDark);
            Fill(texture, 30, 10 + rightFoot, 6, 2, RobeDark);
        }

        private static void DrawFrontHead(Texture2D texture, int headY)
        {
            Fill(texture, 16, headY, 20, 16, Outline);
            Fill(texture, 18, headY + 1, 16, 13, Skin);
            Fill(texture, 17, headY + 12, 18, 5, Hair);
            Fill(texture, 16, headY + 15, 20, 3, Outline);
            Fill(texture, 19, headY + 16, 14, 2, RobeDark);
            Set(texture, 21, headY + 7, Outline);
            Set(texture, 31, headY + 7, Outline);
            Fill(texture, 25, headY + 4, 2, 2, SkinShade);
            Fill(texture, 23, headY + 2, 6, 1, Outline);
        }

        private static void DrawBackHead(Texture2D texture, int headY)
        {
            Fill(texture, 16, headY, 20, 16, Outline);
            Fill(texture, 18, headY + 1, 16, 13, Hair);
            Fill(texture, 17, headY + 10, 18, 7, RobeDark);
            Fill(texture, 20, headY + 12, 12, 3, Robe);
            Fill(texture, 18, headY + 15, 16, 3, Outline);
            Fill(texture, 24, headY + 2, 3, 10, new Color(0.29f, 0.17f, 0.12f, 1f));
        }

        private static void DrawProfileHead(Texture2D texture, int headY, bool left)
        {
            var x = left ? 14 : 18;
            Fill(texture, x, headY, 18, 16, Outline);
            Fill(texture, x + 2, headY + 1, 14, 13, Skin);
            Fill(texture, x + (left ? 0 : 3), headY + 11, 15, 5, Hair);
            Fill(texture, x + (left ? -1 : 1), headY + 15, 18, 3, Outline);
            Set(texture, x + (left ? 4 : 13), headY + 7, Outline);
            Fill(texture, x + (left ? 1 : 13), headY + 5, 3, 2, SkinShade);
        }

        private static void DrawFrontArms(Texture2D texture, int bodyY, int step)
        {
            Fill(texture, 13, bodyY + 12 + Mathf.Max(0, step), 6, 14, Outline);
            Fill(texture, 34, bodyY + 12 + Mathf.Max(0, -step), 6, 14, Outline);
            Fill(texture, 14, bodyY + 13 + Mathf.Max(0, step), 4, 11, RobeDark);
            Fill(texture, 35, bodyY + 13 + Mathf.Max(0, -step), 4, 11, RobeDark);
            Fill(texture, 14, bodyY + 8 + Mathf.Max(0, step), 4, 4, Skin);
            Fill(texture, 35, bodyY + 8 + Mathf.Max(0, -step), 4, 4, Skin);
        }

        private static void DrawBackArms(Texture2D texture, int bodyY, int step)
        {
            Fill(texture, 12, bodyY + 13 + Mathf.Max(0, step), 7, 15, Outline);
            Fill(texture, 34, bodyY + 13 + Mathf.Max(0, -step), 7, 15, Outline);
            Fill(texture, 14, bodyY + 14 + Mathf.Max(0, step), 4, 13, RobeDark);
            Fill(texture, 35, bodyY + 14 + Mathf.Max(0, -step), 4, 13, RobeDark);
        }

        private static void DrawSideArms(Texture2D texture, int bodyY, bool left, int step)
        {
            var armX = left ? 12 : 35;
            Fill(texture, armX, bodyY + 13 + Mathf.Abs(step), 6, 15, Outline);
            Fill(texture, armX + 1, bodyY + 14 + Mathf.Abs(step), 4, 12, RobeDark);
            Fill(texture, armX + 1, bodyY + 10 + Mathf.Abs(step), 4, 4, Skin);
        }

        private static void DrawCastingPose(Texture2D texture, bool release, int frame)
        {
            var lift = release ? 5 : frame * 2;
            Fill(texture, 10, 31 + lift, 9, 6, Outline);
            Fill(texture, 34, 31 + lift, 9, 6, Outline);
            Fill(texture, 12, 32 + lift, 6, 4, Skin);
            Fill(texture, 35, 32 + lift, 6, 4, Skin);

            var radius = release ? 12 + frame * 2 : 5 + frame * 3;
            Ellipse(texture, 24, 33 + lift, radius, Mathf.Max(3, radius / 2), release ? GlowWarm : GlowBlue);
            Ellipse(texture, 24, 33 + lift, Mathf.Max(2, radius - 4), Mathf.Max(2, radius / 2 - 2), Color.white);
            for (var i = 0; i < 6 + frame * 2; i++)
            {
                var x = 8 + i * 6;
                var y = 49 + ((i + frame) % 3) * 3;
                Set(texture, x, y, release ? GlowWarm : GlowBlue);
                Set(texture, x + 1, y, release ? GlowWarm : GlowBlue);
            }
        }

        private static int WalkOffset(int frame)
        {
            return frame switch
            {
                1 => 2,
                3 => -2,
                _ => 0
            };
        }

        private static void Save(Texture2D texture, string fileName)
        {
            File.WriteAllBytes(Path.Combine(OutputFolder, fileName), texture.EncodeToPNG());
        }

        private static void ConfigureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelRenderSetup.AssetsPixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        private static void Fill(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (var yy = y; yy < y + height; yy++)
            {
                for (var xx = x; xx < x + width; xx++)
                {
                    Set(texture, xx, yy, color);
                }
            }
        }

        private static void Ellipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (var y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                for (var x = centerX - radiusX; x <= centerX + radiusX; x++)
                {
                    var dx = (x - centerX) / Mathf.Max(1f, radiusX);
                    var dy = (y - centerY) / Mathf.Max(1f, radiusY);
                    if (dx * dx + dy * dy <= 1f)
                    {
                        Set(texture, x, y, color);
                    }
                }
            }
        }

        private static void Set(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }
    }
}

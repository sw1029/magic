using UnityEngine;

namespace MagicExamHall
{
    public static class PixelArtFactory
    {
        private const int Size = 32;
        private const float PixelsPerUnit = 16f;

        public static Sprite CreateSprite(string name, Color primary, Color secondary, PixelSpriteKind kind)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = $"{name} Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Clear(texture);
            switch (kind)
            {
                case PixelSpriteKind.Player:
                    DrawPlayer(texture, primary, secondary);
                    break;
                case PixelSpriteKind.Mentor:
                    DrawMentor(texture, primary, secondary);
                    break;
                case PixelSpriteKind.Station:
                    DrawStation(texture, primary, secondary);
                    break;
                case PixelSpriteKind.Target:
                    DrawTarget(texture, primary, secondary);
                    break;
                case PixelSpriteKind.Pulse:
                    DrawPulse(texture, primary);
                    break;
                case PixelSpriteKind.FloorTile:
                    DrawFloorTile(texture, primary, secondary);
                    break;
                case PixelSpriteKind.WallTrim:
                    DrawWallTrim(texture, primary, secondary);
                    break;
                case PixelSpriteKind.Rug:
                    DrawRug(texture, primary, secondary);
                    break;
                case PixelSpriteKind.Bookshelf:
                    DrawBookshelf(texture, primary, secondary);
                    break;
                case PixelSpriteKind.Candle:
                    DrawCandle(texture, primary, secondary);
                    break;
                case PixelSpriteKind.RuneCircle:
                    DrawRuneCircle(texture, primary, secondary);
                    break;
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static void DrawPlayer(Texture2D texture, Color skin, Color robe)
        {
            var outline = new Color(0.035f, 0.032f, 0.045f, 1f);
            var robeDark = Shade(robe, 0.38f);
            var robeMid = Shade(robe, 0.72f);
            var robeLight = Mix(robe, Color.white, 0.36f);
            var robeGlow = Mix(robe, new Color(0.42f, 0.85f, 1f, 1f), 0.25f);
            var skinShade = Shade(skin, 0.72f);
            var skinLight = Mix(skin, Color.white, 0.24f);
            var hair = new Color(0.18f, 0.10f, 0.06f, 1f);
            var hairLight = new Color(0.37f, 0.22f, 0.11f, 1f);
            var gold = new Color(1f, 0.78f, 0.26f, 1f);
            var boot = new Color(0.08f, 0.06f, 0.08f, 1f);

            Ellipse(texture, 16, 4, 11, 3, new Color(0f, 0f, 0f, 0.34f));
            Fill(texture, 10, 5, 5, 2, outline);
            Fill(texture, 17, 5, 5, 2, outline);
            Fill(texture, 11, 6, 4, 1, boot);
            Fill(texture, 17, 6, 4, 1, boot);
            Fill(texture, 8, 7, 16, 3, outline);
            Fill(texture, 9, 8, 14, 2, robeDark);
            Fill(texture, 7, 10, 18, 12, outline);
            Fill(texture, 8, 11, 16, 11, robeDark);
            Fill(texture, 10, 12, 12, 10, robeMid);
            Fill(texture, 12, 12, 8, 10, robe);
            Fill(texture, 15, 11, 2, 11, robeLight);
            Line(texture, 12, 11, 10, 21, Shade(robe, 0.5f));
            Line(texture, 20, 11, 22, 21, Shade(robe, 0.5f));
            Line(texture, 16, 12, 16, 21, robeGlow);
            Set(texture, 14, 16, gold);
            Set(texture, 17, 16, gold);
            Fill(texture, 8, 13, 3, 8, outline);
            Fill(texture, 21, 13, 3, 8, outline);
            Fill(texture, 9, 14, 2, 6, robeMid);
            Fill(texture, 21, 14, 2, 6, robeLight);
            Set(texture, 9, 13, robeGlow);
            Set(texture, 22, 20, robeGlow);
            Fill(texture, 11, 21, 10, 6, outline);
            Fill(texture, 12, 22, 8, 5, skinShade);
            Fill(texture, 13, 23, 6, 4, skin);
            Fill(texture, 14, 25, 4, 2, skinLight);
            Fill(texture, 11, 26, 10, 3, hair);
            Fill(texture, 12, 28, 8, 1, hairLight);
            Fill(texture, 10, 29, 12, 2, outline);
            Fill(texture, 11, 30, 10, 1, robeDark);
            Set(texture, 13, 24, outline);
            Set(texture, 18, 24, outline);
            Set(texture, 16, 22, Shade(skin, 0.62f));
            Set(texture, 19, 26, skinLight);
            Set(texture, 14, 30, gold);
            Set(texture, 17, 30, gold);
        }

        private static void DrawMentor(Texture2D texture, Color skin, Color robe)
        {
            var outline = new Color(0.035f, 0.032f, 0.045f, 1f);
            var robeDark = Shade(robe, 0.34f);
            var robeMid = Shade(robe, 0.66f);
            var robeLight = Mix(robe, Color.white, 0.42f);
            var skinShade = Shade(skin, 0.72f);
            var skinLight = Mix(skin, Color.white, 0.22f);
            var silver = new Color(0.82f, 0.88f, 0.95f, 1f);
            var silverDark = new Color(0.46f, 0.52f, 0.62f, 1f);
            var gold = new Color(1f, 0.78f, 0.24f, 1f);
            var violetGlow = Mix(robe, new Color(0.75f, 0.56f, 1f, 1f), 0.36f);

            Ellipse(texture, 16, 4, 12, 3, new Color(0f, 0f, 0f, 0.34f));
            Fill(texture, 8, 6, 16, 4, outline);
            Fill(texture, 9, 7, 14, 3, robeDark);
            Fill(texture, 7, 10, 18, 13, outline);
            Fill(texture, 8, 11, 16, 12, robeDark);
            Fill(texture, 10, 12, 12, 11, robeMid);
            Fill(texture, 12, 12, 8, 11, robe);
            Fill(texture, 15, 11, 2, 12, robeLight);
            Line(texture, 9, 13, 22, 22, violetGlow);
            Line(texture, 22, 13, 10, 22, Shade(robe, 0.48f));
            Line(texture, 8, 22, 23, 22, gold);
            Set(texture, 11, 16, gold);
            Set(texture, 20, 16, gold);
            Fill(texture, 12, 22, 8, 5, outline);
            Fill(texture, 13, 23, 6, 4, skinShade);
            Fill(texture, 14, 24, 4, 3, skin);
            Fill(texture, 14, 25, 4, 1, skinLight);
            Fill(texture, 10, 27, 12, 2, outline);
            Fill(texture, 11, 28, 10, 1, silver);
            Fill(texture, 8, 29, 16, 2, outline);
            Fill(texture, 9, 30, 14, 1, robeDark);
            Fill(texture, 13, 29, 6, 3, robe);
            Fill(texture, 11, 26, 10, 1, silverDark);
            Set(texture, 13, 24, outline);
            Set(texture, 18, 24, outline);
            Set(texture, 16, 23, Shade(skin, 0.6f));
            Set(texture, 19, 25, skinLight);
            Line(texture, 24, 7, 24, 26, outline);
            Line(texture, 25, 7, 25, 26, gold);
            Set(texture, 24, 18, silver);
            Set(texture, 25, 14, silver);
            Diamond(texture, 25, 28, 4, 4, outline);
            Diamond(texture, 25, 28, 3, 3, violetGlow);
            Diamond(texture, 25, 29, 1, 2, Mix(violetGlow, Color.white, 0.45f));
            Set(texture, 25, 31, Color.white);
        }

        private static void DrawStation(Texture2D texture, Color element, Color accent)
        {
            var outline = new Color(0.035f, 0.032f, 0.045f, 1f);
            var stone = new Color(0.46f, 0.48f, 0.54f, 1f);
            var stoneDark = new Color(0.24f, 0.26f, 0.31f, 1f);
            var stoneLight = new Color(0.66f, 0.68f, 0.74f, 1f);
            var glow = Mix(element, Color.white, 0.22f);

            Ellipse(texture, 16, 5, 12, 4, new Color(0f, 0f, 0f, 0.36f));
            Diamond(texture, 16, 9, 13, 5, outline);
            Diamond(texture, 16, 10, 11, 4, stoneDark);
            Diamond(texture, 16, 12, 10, 4, stone);
            Fill(texture, 8, 12, 16, 8, outline);
            Fill(texture, 9, 13, 14, 7, stoneDark);
            Fill(texture, 10, 16, 12, 3, stone);
            Fill(texture, 11, 18, 10, 2, stoneLight);
            Fill(texture, 11, 19, 10, 2, outline);
            Fill(texture, 12, 20, 8, 2, stone);
            Diamond(texture, 16, 24, 6, 6, outline);
            Diamond(texture, 16, 24, 4, 5, element);
            Diamond(texture, 16, 25, 2, 3, glow);
            Set(texture, 15, 27, Color.white);
            Set(texture, 20, 13, glow);
            Set(texture, 11, 13, glow);
            Line(texture, 10, 14, 22, 14, Shade(element, 0.75f));
            Set(texture, 7, 17, accent);
            Set(texture, 24, 17, accent);
        }

        private static void DrawTarget(Texture2D texture, Color primary, Color secondary)
        {
            var outline = new Color(0.035f, 0.032f, 0.045f, 1f);
            var stone = new Color(0.42f, 0.43f, 0.48f, 1f);
            var stoneLight = new Color(0.64f, 0.66f, 0.72f, 1f);
            var core = Mix(primary, secondary, 0.35f);

            Ellipse(texture, 16, 5, 8, 3, new Color(0f, 0f, 0f, 0.34f));
            Fill(texture, 10, 6, 12, 3, outline);
            Fill(texture, 11, 7, 10, 2, stone);
            Fill(texture, 12, 9, 8, 8, outline);
            Fill(texture, 13, 10, 6, 7, stoneLight);
            Fill(texture, 12, 17, 8, 2, outline);
            Fill(texture, 13, 18, 6, 2, stone);
            Diamond(texture, 16, 23, 7, 7, outline);
            Diamond(texture, 16, 23, 5, 5, core);
            Diamond(texture, 16, 24, 2, 3, Mix(core, Color.white, 0.35f));
            Set(texture, 18, 26, Color.white);
            Set(texture, 11, 14, Shade(core, 0.8f));
            Set(texture, 21, 14, Shade(core, 0.8f));
        }

        private static void DrawPulse(Texture2D texture, Color primary)
        {
            var glow = Mix(primary, Color.white, 0.35f);
            Ring(texture, 16, 16, 11, 10, new Color(primary.r, primary.g, primary.b, 0.75f));
            Ring(texture, 16, 16, 7, 6, new Color(glow.r, glow.g, glow.b, 0.65f));
            Line(texture, 16, 3, 16, 29, new Color(glow.r, glow.g, glow.b, 0.42f));
            Line(texture, 3, 16, 29, 16, new Color(glow.r, glow.g, glow.b, 0.42f));
            Set(texture, 16, 16, Color.white);
        }

        private static void DrawFloorTile(Texture2D texture, Color primary, Color secondary)
        {
            var grout = Shade(secondary, 0.72f);
            var edgeDark = Shade(primary, 0.54f);
            var edgeLight = Mix(primary, Color.white, 0.12f);
            Fill(texture, 0, 0, Size, Size, primary);
            Fill(texture, 1, 1, 14, 14, Shade(primary, 0.90f));
            Fill(texture, 17, 1, 14, 14, Mix(primary, secondary, 0.10f));
            Fill(texture, 1, 17, 14, 14, Mix(primary, secondary, 0.06f));
            Fill(texture, 17, 17, 14, 14, Shade(primary, 0.84f));
            Fill(texture, 0, 15, Size, 2, grout);
            Fill(texture, 15, 0, 2, Size, grout);
            Fill(texture, 0, 0, Size, 1, edgeDark);
            Fill(texture, 0, 0, 1, Size, edgeDark);
            Fill(texture, 0, 31, Size, 1, edgeLight);
            Fill(texture, 31, 0, 1, Size, edgeLight);
            Line(texture, 2, 14, 14, 14, edgeLight);
            Line(texture, 18, 14, 30, 14, edgeLight);
            Line(texture, 14, 2, 14, 14, edgeLight);
            Line(texture, 14, 18, 14, 30, edgeLight);
            Line(texture, 5, 6, 9, 5, Shade(primary, 0.62f));
            Line(texture, 21, 4, 27, 7, Shade(primary, 0.58f));
            Line(texture, 22, 21, 28, 19, Shade(primary, 0.60f));
            Line(texture, 6, 24, 11, 27, Shade(primary, 0.64f));
            Set(texture, 4, 9, Mix(primary, Color.white, 0.16f));
            Set(texture, 8, 3, Shade(primary, 0.72f));
            Set(texture, 24, 11, Mix(primary, Color.white, 0.11f));
            Set(texture, 28, 24, Shade(primary, 0.68f));
            Set(texture, 3, 21, Mix(primary, secondary, 0.25f));
            Set(texture, 11, 29, edgeLight);
            Set(texture, 19, 27, Shade(primary, 0.70f));
        }

        private static void DrawWallTrim(Texture2D texture, Color primary, Color secondary)
        {
            var dark = Shade(primary, 0.46f);
            var mid = Shade(primary, 0.74f);
            var light = Mix(primary, Color.white, 0.16f);
            Fill(texture, 0, 0, Size, Size, dark);
            Fill(texture, 0, 18, Size, 14, primary);
            Fill(texture, 0, 15, Size, 3, Shade(secondary, 0.78f));
            Fill(texture, 0, 13, Size, 2, Shade(primary, 0.36f));
            Fill(texture, 0, 30, Size, 2, light);
            for (var x = 0; x < Size; x += 8)
            {
                Fill(texture, x + 1, 19, 6, 10, mid);
                Fill(texture, x + 1, 28, 6, 1, light);
                Line(texture, x, 18, x + 3, 31, Shade(primary, 0.52f));
                Set(texture, x + 3, 23, secondary);
                Set(texture, x + 4, 24, Mix(secondary, Color.white, 0.28f));
                Set(texture, x + 5, 21, Shade(secondary, 0.72f));
            }

            Line(texture, 1, 12, 8, 9, Shade(primary, 0.38f));
            Line(texture, 20, 10, 28, 12, Shade(primary, 0.40f));
            Set(texture, 13, 6, light);
            Set(texture, 18, 8, Shade(primary, 0.62f));
            Set(texture, 3, 3, Mix(primary, secondary, 0.24f));
        }

        private static void DrawRug(Texture2D texture, Color primary, Color secondary)
        {
            var dark = Shade(primary, 0.45f);
            var mid = Shade(primary, 0.78f);
            var light = Mix(primary, Color.white, 0.14f);
            Fill(texture, 0, 0, Size, Size, dark);
            Fill(texture, 3, 0, 26, Size, primary);
            Fill(texture, 4, 0, 2, Size, secondary);
            Fill(texture, 26, 0, 2, Size, secondary);
            Fill(texture, 7, 0, 2, Size, Shade(secondary, 0.68f));
            Fill(texture, 23, 0, 2, Size, Shade(secondary, 0.68f));
            Fill(texture, 10, 0, 12, Size, mid);
            for (var y = 1; y < Size; y += 6)
            {
                Set(texture, 5, y, light);
                Set(texture, 26, y + 2, light);
                Set(texture, 8, y + 3, secondary);
                Set(texture, 23, y, secondary);
            }

            for (var y = 3; y < Size; y += 8)
            {
                Diamond(texture, 16, y + 2, 6, 4, Shade(secondary, 0.76f));
                Diamond(texture, 16, y + 2, 3, 2, secondary);
                Set(texture, 16, y + 2, Color.white);
            }

            Fill(texture, 0, 0, 3, Size, new Color(0f, 0f, 0f, 0.25f));
            Fill(texture, 29, 0, 3, Size, new Color(0f, 0f, 0f, 0.25f));
            for (var y = 1; y < Size; y += 4)
            {
                Line(texture, 1, y, 3, y + 1, light);
                Line(texture, 28, y + 1, 30, y, light);
            }
        }

        private static void DrawBookshelf(Texture2D texture, Color wood, Color accent)
        {
            var outline = new Color(0.04f, 0.025f, 0.018f, 1f);
            var woodDark = Shade(wood, 0.48f);
            var woodMid = Shade(wood, 0.82f);
            var woodLight = Mix(wood, Color.white, 0.13f);
            Fill(texture, 4, 3, 24, 26, outline);
            Fill(texture, 5, 4, 22, 24, woodDark);
            Fill(texture, 6, 5, 20, 22, wood);
            Fill(texture, 7, 8, 18, 2, woodDark);
            Fill(texture, 7, 16, 18, 2, woodDark);
            Fill(texture, 7, 24, 18, 2, woodDark);
            Fill(texture, 6, 26, 20, 1, woodLight);
            Fill(texture, 6, 5, 1, 21, woodLight);
            Fill(texture, 25, 5, 1, 21, outline);
            var colors = new[]
            {
                accent,
                new Color(0.85f, 0.18f, 0.22f, 1f),
                new Color(0.18f, 0.48f, 0.9f, 1f),
                new Color(0.92f, 0.72f, 0.22f, 1f),
                new Color(0.45f, 0.78f, 0.38f, 1f)
            };
            for (var shelf = 0; shelf < 3; shelf++)
            {
                for (var i = 0; i < 7; i++)
                {
                    var color = colors[(shelf + i) % colors.Length];
                    var x = 8 + i * 2;
                    var y = 10 + shelf * 8;
                    var height = 4 + ((i + shelf) % 2);
                    Fill(texture, x, y, 1, height, color);
                    Set(texture, x, y + height - 1, Mix(color, Color.white, 0.18f));
                    Set(texture, x, y, Shade(color, 0.55f));
                }
            }

            Diamond(texture, 21, 13, 2, 2, Mix(accent, Color.white, 0.30f));
            Fill(texture, 10, 21, 4, 2, new Color(0.93f, 0.84f, 0.60f, 1f));
            Set(texture, 12, 22, Shade(wood, 0.45f));
            Line(texture, 7, 7, 24, 7, woodMid);
            Line(texture, 7, 15, 24, 15, woodMid);
            Line(texture, 7, 23, 24, 23, woodMid);
        }

        private static void DrawCandle(Texture2D texture, Color metal, Color flame)
        {
            var outline = new Color(0.04f, 0.035f, 0.03f, 1f);
            var metalDark = Shade(metal, 0.58f);
            var metalLight = Mix(metal, Color.white, 0.24f);
            Ellipse(texture, 16, 4, 8, 2, new Color(0f, 0f, 0f, 0.32f));
            Fill(texture, 14, 5, 4, 13, outline);
            Fill(texture, 15, 6, 2, 11, metal);
            Set(texture, 16, 12, metalLight);
            Fill(texture, 9, 12, 14, 2, outline);
            Fill(texture, 10, 13, 12, 1, metalLight);
            Line(texture, 10, 13, 8, 17, outline);
            Line(texture, 22, 13, 24, 17, outline);
            Line(texture, 11, 13, 9, 17, metal);
            Line(texture, 21, 13, 23, 17, metal);
            Fill(texture, 8, 17, 3, 5, outline);
            Fill(texture, 21, 17, 3, 5, outline);
            Fill(texture, 9, 18, 1, 3, metalDark);
            Fill(texture, 22, 18, 1, 3, metalDark);
            Diamond(texture, 16, 22, 4, 6, Shade(flame, 0.85f));
            Diamond(texture, 9, 25, 3, 5, Shade(flame, 0.78f));
            Diamond(texture, 23, 25, 3, 5, Shade(flame, 0.78f));
            Diamond(texture, 16, 23, 2, 4, Mix(flame, Color.white, 0.45f));
            Diamond(texture, 9, 26, 1, 3, Mix(flame, Color.white, 0.45f));
            Diamond(texture, 23, 26, 1, 3, Mix(flame, Color.white, 0.45f));
            Set(texture, 16, 25, Color.white);
            Set(texture, 9, 28, Color.white);
            Set(texture, 23, 28, Color.white);
            Set(texture, 15, 21, new Color(1f, 0.32f, 0.12f, 1f));
            Set(texture, 8, 24, new Color(1f, 0.32f, 0.12f, 1f));
            Set(texture, 22, 24, new Color(1f, 0.32f, 0.12f, 1f));
        }

        private static void DrawRuneCircle(Texture2D texture, Color primary, Color secondary)
        {
            Ring(texture, 16, 16, 13, 12, new Color(primary.r, primary.g, primary.b, 0.62f));
            Ring(texture, 16, 16, 9, 8, new Color(primary.r, primary.g, primary.b, 0.35f));
            Line(texture, 16, 4, 16, 28, new Color(secondary.r, secondary.g, secondary.b, 0.35f));
            Line(texture, 4, 16, 28, 16, new Color(secondary.r, secondary.g, secondary.b, 0.35f));
            Line(texture, 8, 8, 24, 24, new Color(primary.r, primary.g, primary.b, 0.25f));
            Line(texture, 8, 24, 24, 8, new Color(primary.r, primary.g, primary.b, 0.25f));
            Set(texture, 16, 29, secondary);
            Set(texture, 16, 3, secondary);
            Set(texture, 29, 16, secondary);
            Set(texture, 3, 16, secondary);
        }

        private static void Clear(Texture2D texture)
        {
            var clear = new Color(0f, 0f, 0f, 0f);
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }
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

        private static void Diamond(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (var y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                for (var x = centerX - radiusX; x <= centerX + radiusX; x++)
                {
                    var dx = Mathf.Abs(x - centerX) / (float)Mathf.Max(radiusX, 1);
                    var dy = Mathf.Abs(y - centerY) / (float)Mathf.Max(radiusY, 1);
                    if (dx + dy <= 1f)
                    {
                        Set(texture, x, y, color);
                    }
                }
            }
        }

        private static void Ellipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (var y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                for (var x = centerX - radiusX; x <= centerX + radiusX; x++)
                {
                    var dx = (x - centerX) / (float)Mathf.Max(radiusX, 1);
                    var dy = (y - centerY) / (float)Mathf.Max(radiusY, 1);
                    if (dx * dx + dy * dy <= 1f)
                    {
                        Set(texture, x, y, color);
                    }
                }
            }
        }

        private static void Ring(Texture2D texture, int centerX, int centerY, int outerRadius, int innerRadius, Color color)
        {
            var outer = outerRadius * outerRadius;
            var inner = innerRadius * innerRadius;
            for (var y = centerY - outerRadius; y <= centerY + outerRadius; y++)
            {
                for (var x = centerX - outerRadius; x <= centerX + outerRadius; x++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    var d = dx * dx + dy * dy;
                    if (d <= outer && d >= inner)
                    {
                        Set(texture, x, y, color);
                    }
                }
            }
        }

        private static void Line(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            var dx = Mathf.Abs(x1 - x0);
            var dy = -Mathf.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var error = dx + dy;
            while (true)
            {
                Set(texture, x0, y0, color);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                var e2 = 2 * error;
                if (e2 >= dy)
                {
                    error += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private static void Set(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static Color Shade(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r * amount),
                Mathf.Clamp01(color.g * amount),
                Mathf.Clamp01(color.b * amount),
                color.a);
        }

        private static Color Mix(Color a, Color b, float t)
        {
            return new Color(
                Mathf.Lerp(a.r, b.r, t),
                Mathf.Lerp(a.g, b.g, t),
                Mathf.Lerp(a.b, b.b, t),
                Mathf.Lerp(a.a, b.a, t));
        }
    }

    public enum PixelSpriteKind
    {
        Player,
        Mentor,
        Station,
        Target,
        Pulse,
        FloorTile,
        WallTrim,
        Rug,
        Bookshelf,
        Candle,
        RuneCircle
    }
}

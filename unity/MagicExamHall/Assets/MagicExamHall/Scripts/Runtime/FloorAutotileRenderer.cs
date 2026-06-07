using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MagicExamHall
{
    public enum FloorAutotileLayerKind
    {
        Backdrop,
        Floor,
        Wall,
        Rug
    }

    public sealed class FloorAutotileLayer : MonoBehaviour
    {
        public int floorNumber;
        public FloorAutotileLayerKind kind;
        public int placedTileCount;
        public int uniqueTileCount;
    }

    public readonly struct FloorAutotileTheme
    {
        public readonly Color fill;
        public readonly Color line;
        public readonly Color accent;
        public readonly FloorAutotileLayerKind kind;
        public readonly int seed;

        public FloorAutotileTheme(Color fill, Color line, Color accent, FloorAutotileLayerKind kind, int seed)
        {
            this.fill = fill;
            this.line = line;
            this.accent = accent;
            this.kind = kind;
            this.seed = seed;
        }
    }

    public static class FloorAutotileRenderer
    {
        public const int TilePixels = 32;
        public const int VariantCount = 4;
        public const float PixelsPerUnit = 32f;

        private const int North = 1;
        private const int East = 2;
        private const int South = 4;
        private const int West = 8;

        public static FloorAutotileLayer CreateRectLayer(
            string name,
            Transform parent,
            int floorNumber,
            Vector2 center,
            int width,
            int height,
            FloorAutotileTheme theme,
            int sortingOrder,
            Func<int, int, int, int, bool> fill = null)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(center.x - width * 0.5f, center.y - height * 0.5f, 0f);

            var grid = root.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            var tilemapObject = new GameObject($"{name} Tilemap");
            tilemapObject.transform.SetParent(root.transform, false);
            var tilemap = tilemapObject.AddComponent<Tilemap>();
            var renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.mode = TilemapRenderer.Mode.Chunk;
            renderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = PixelMaterialProvider.SpriteMaterial;

            var marker = root.AddComponent<FloorAutotileLayer>();
            marker.floorNumber = floorNumber;
            marker.kind = theme.kind;

            var tiles = CreateTiles(name, theme);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (!Filled(fill, x, y, width, height))
                    {
                        continue;
                    }

                    var mask = BuildMask(fill, x, y, width, height);
                    var variant = PositiveModulo(Hash(x, y, theme.seed), VariantCount);
                    tilemap.SetTile(new Vector3Int(x, y, 0), tiles[mask, variant]);
                    marker.placedTileCount++;
                }
            }

            marker.uniqueTileCount = tilemap.GetUsedTilesCount();
            tilemap.CompressBounds();
            return marker;
        }

        public static int BuildMask(Func<int, int, int, int, bool> fill, int x, int y, int width, int height)
        {
            var mask = 0;
            if (Filled(fill, x, y + 1, width, height))
            {
                mask |= North;
            }
            if (Filled(fill, x + 1, y, width, height))
            {
                mask |= East;
            }
            if (Filled(fill, x, y - 1, width, height))
            {
                mask |= South;
            }
            if (Filled(fill, x - 1, y, width, height))
            {
                mask |= West;
            }

            return mask;
        }

        private static bool Filled(Func<int, int, int, int, bool> fill, int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return false;
            }

            return fill == null || fill(x, y, width, height);
        }

        private static Tile[,] CreateTiles(string layerName, FloorAutotileTheme theme)
        {
            var tiles = new Tile[16, VariantCount];
            for (var mask = 0; mask < 16; mask++)
            {
                for (var variant = 0; variant < VariantCount; variant++)
                {
                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.name = $"{layerName} {mask:X1}-{variant}";
                    tile.sprite = CreateSprite(tile.name, theme, mask, variant);
                    tile.colliderType = Tile.ColliderType.None;
                    tiles[mask, variant] = tile;
                }
            }

            return tiles;
        }

        private static Sprite CreateSprite(string name, FloorAutotileTheme theme, int mask, int variant)
        {
            var texture = new Texture2D(TilePixels, TilePixels, TextureFormat.RGBA32, false)
            {
                name = $"{name} Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var fill = Shade(theme.fill, 0.92f + variant * 0.035f);
            Clear(texture, fill);
            DrawAutotileDetails(texture, theme, mask, variant);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, TilePixels, TilePixels), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static void DrawAutotileDetails(Texture2D texture, FloorAutotileTheme theme, int mask, int variant)
        {
            var edge = Shade(theme.line, 0.72f);
            var softEdge = Color.Lerp(theme.line, theme.fill, 0.28f);
            var highlight = Color.Lerp(theme.fill, Color.white, 0.18f);
            var accent = Color.Lerp(theme.accent, Color.white, 0.12f);

            if ((mask & North) == 0)
            {
                Fill(texture, 0, TilePixels - 2, TilePixels, 2, edge);
                Fill(texture, 2, TilePixels - 4, TilePixels - 4, 1, softEdge);
            }
            else
            {
                Fill(texture, 2, TilePixels - 1, TilePixels - 4, 1, Color.Lerp(theme.line, theme.fill, 0.68f));
            }

            if ((mask & East) == 0)
            {
                Fill(texture, TilePixels - 2, 0, 2, TilePixels, edge);
                Fill(texture, TilePixels - 4, 2, 1, TilePixels - 4, softEdge);
            }
            else
            {
                Fill(texture, TilePixels - 1, 2, 1, TilePixels - 4, Color.Lerp(theme.line, theme.fill, 0.72f));
            }

            if ((mask & South) == 0)
            {
                Fill(texture, 0, 0, TilePixels, 2, Shade(edge, 0.78f));
                Fill(texture, 2, 3, TilePixels - 4, 1, softEdge);
            }
            else
            {
                Fill(texture, 2, 0, TilePixels - 4, 1, Color.Lerp(theme.line, theme.fill, 0.70f));
            }

            if ((mask & West) == 0)
            {
                Fill(texture, 0, 0, 2, TilePixels, edge);
                Fill(texture, 3, 2, 1, TilePixels - 4, softEdge);
            }
            else
            {
                Fill(texture, 0, 2, 1, TilePixels - 4, Color.Lerp(theme.line, theme.fill, 0.72f));
            }

            Fill(texture, 4, TilePixels - 5, 7, 1, highlight);
            Fill(texture, TilePixels - 9, 4, 5, 1, Shade(theme.line, 0.82f));

            switch (theme.kind)
            {
                case FloorAutotileLayerKind.Backdrop:
                    DrawSpeckles(texture, theme.line, variant, sparse: true);
                    break;
                case FloorAutotileLayerKind.Floor:
                    DrawStone(texture, theme.line, accent, variant);
                    break;
                case FloorAutotileLayerKind.Wall:
                    DrawWallCarving(texture, theme.line, accent, variant);
                    break;
                case FloorAutotileLayerKind.Rug:
                    DrawRunnerStitch(texture, theme.line, accent, variant);
                    break;
            }
        }

        private static void DrawStone(Texture2D texture, Color line, Color accent, int variant)
        {
            var groove = Color.Lerp(line, Color.black, 0.10f);
            var offset = 5 + variant * 3;
            Fill(texture, offset, 14, 11, 1, Color.Lerp(groove, Color.white, 0.08f));
            Fill(texture, 18 - variant, 19, 8, 1, Color.Lerp(groove, Color.white, 0.06f));
            Set(texture, 8 + variant * 3, 7 + variant, accent);
            Set(texture, 9 + variant * 3, 7 + variant, accent);
            DrawSpeckles(texture, line, variant, sparse: false);
        }

        private static void DrawWallCarving(Texture2D texture, Color line, Color accent, int variant)
        {
            Fill(texture, 4, 10, 24, 1, Color.Lerp(line, Color.white, 0.08f));
            Fill(texture, 4, 21, 24, 1, Color.Lerp(line, Color.white, 0.12f));
            Fill(texture, 6 + variant, 15, 6, 2, accent);
            Fill(texture, 20 - variant, 15, 6, 2, Color.Lerp(accent, line, 0.34f));
        }

        private static void DrawRunnerStitch(Texture2D texture, Color line, Color accent, int variant)
        {
            var border = Color.Lerp(line, accent, 0.45f);
            Fill(texture, 5, 0, 2, TilePixels, border);
            Fill(texture, TilePixels - 7, 0, 2, TilePixels, border);
            for (var y = 4 + variant; y < TilePixels; y += 8)
            {
                Fill(texture, 11, y, 4, 2, accent);
                Fill(texture, 18, y + 3, 4, 2, Color.Lerp(accent, Color.white, 0.16f));
            }
        }

        private static void DrawSpeckles(Texture2D texture, Color color, int variant, bool sparse)
        {
            var count = sparse ? 3 : 6;
            for (var index = 0; index < count; index++)
            {
                var x = PositiveModulo(index * 11 + variant * 7, TilePixels - 6) + 3;
                var y = PositiveModulo(index * 17 + variant * 5, TilePixels - 6) + 3;
                Set(texture, x, y, Color.Lerp(color, Color.white, sparse ? 0.10f : 0.18f));
            }
        }

        private static int Hash(int x, int y, int seed)
        {
            unchecked
            {
                var value = seed;
                value = value * 397 ^ x;
                value = value * 397 ^ y;
                return value;
            }
        }

        private static int PositiveModulo(int value, int modulo)
        {
            var result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        private static void Clear(Texture2D texture, Color color)
        {
            for (var y = 0; y < TilePixels; y++)
            {
                for (var x = 0; x < TilePixels; x++)
                {
                    texture.SetPixel(x, y, color);
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

        private static void Set(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= TilePixels || y >= TilePixels)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static Color Shade(Color color, float multiplier)
        {
            return new Color(
                Mathf.Clamp01(color.r * multiplier),
                Mathf.Clamp01(color.g * multiplier),
                Mathf.Clamp01(color.b * multiplier),
                color.a);
        }
    }
}

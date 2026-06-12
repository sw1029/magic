using System.Collections.Generic;
using UnityEngine;

namespace MagicExamHall
{
    public static class PixelArtFactory
    {
        private const int Size = 32;
        private const string ExternalSpriteRoot = "Sprites/";

        public const int NoVariantIndex = -1;
        public const float SpritePixelsPerUnit = 16f;

        private static readonly Dictionary<(PixelSpriteKind kind, int variantIndex), Sprite> ExternalCache = new();
        private static readonly HashSet<(PixelSpriteKind kind, int variantIndex)> ExternalMissCache = new();
        private static readonly Dictionary<(PlayerAnimationState state, PlayerFacing facing, int frame), Sprite> ApprenticeFrameCache = new();

        /// <summary>
        /// Creates or loads a sprite for the given kind. If a PNG exists at
        /// <c>Assets/MagicExamHall/Resources/Sprites/&lt;Kind&gt;.png</c>, it is
        /// loaded as-is. Otherwise the legacy procedural drawer is used.
        ///
        /// External art is rendered with its own colors as authored, so the
        /// <paramref name="primary"/> and <paramref name="secondary"/> values
        /// only apply to the procedural fallback. Per-floor tinting can still
        /// be applied via <c>SpriteRenderer.color</c> on the call site.
        /// </summary>
        public static Sprite CreateSprite(string name, Color primary, Color secondary, PixelSpriteKind kind)
        {
            return CreateSprite(name, primary, secondary, kind, NoVariantIndex);
        }

        /// <summary>
        /// Creates or loads a sprite variant. Variant PNGs are searched first
        /// as <c>Resources/Sprites/&lt;Kind&gt;_&lt;Variant&gt;.png</c>, then
        /// <c>Resources/Sprites/&lt;Kind&gt;/&lt;Variant&gt;.png</c>. If the
        /// specific variant is missing, the base <c>&lt;Kind&gt;.png</c> is
        /// used before falling back to the procedural drawer.
        /// </summary>
        public static Sprite CreateSprite(string name, Color primary, Color secondary, PixelSpriteKind kind, int variantIndex)
        {
            var external = LoadExternalSprite(kind, variantIndex);
            if (external != null)
            {
                return external;
            }

            var texture = CreateProceduralTexture(name, primary, secondary, kind, variantIndex);
            return CreateFullRectSprite(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), SpritePixelsPerUnit);
        }

        public static Texture2D CreateProceduralTexture(string name, Color primary, Color secondary, PixelSpriteKind kind, int variantIndex = NoVariantIndex)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = $"{name} Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Clear(texture);
            var normalizedVariant = NormalizeVariantIndex(kind, variantIndex);
            switch (kind)
            {
                case PixelSpriteKind.Player:
                    DrawPlayer(texture, primary, secondary);
                    break;
                case PixelSpriteKind.MentorNeutral:
                    DrawMentor(texture, primary, secondary, MentorExpression.Neutral, 0);
                    break;
                case PixelSpriteKind.MentorHappy:
                    DrawMentor(texture, primary, secondary, MentorExpression.Happy, 0);
                    break;
                case PixelSpriteKind.MentorFrown:
                    DrawMentor(texture, primary, secondary, MentorExpression.Frown, 0);
                    break;
                case PixelSpriteKind.MentorScholarNeutral:
                    DrawMentor(texture, primary, secondary, MentorExpression.Neutral, 1);
                    break;
                case PixelSpriteKind.MentorScholarHappy:
                    DrawMentor(texture, primary, secondary, MentorExpression.Happy, 1);
                    break;
                case PixelSpriteKind.MentorScholarFrown:
                    DrawMentor(texture, primary, secondary, MentorExpression.Frown, 1);
                    break;
                case PixelSpriteKind.MentorGuideNeutral:
                    DrawMentor(texture, primary, secondary, MentorExpression.Neutral, 2);
                    break;
                case PixelSpriteKind.MentorGuideHappy:
                    DrawMentor(texture, primary, secondary, MentorExpression.Happy, 2);
                    break;
                case PixelSpriteKind.MentorGuideFrown:
                    DrawMentor(texture, primary, secondary, MentorExpression.Frown, 2);
                    break;
                case PixelSpriteKind.MentorWatcherNeutral:
                    DrawMentor(texture, primary, secondary, MentorExpression.Neutral, 3);
                    break;
                case PixelSpriteKind.MentorWatcherHappy:
                    DrawMentor(texture, primary, secondary, MentorExpression.Happy, 3);
                    break;
                case PixelSpriteKind.MentorWatcherFrown:
                    DrawMentor(texture, primary, secondary, MentorExpression.Frown, 3);
                    break;
                case PixelSpriteKind.MentorArchivistNeutral:
                    DrawMentor(texture, primary, secondary, MentorExpression.Neutral, 4);
                    break;
                case PixelSpriteKind.MentorArchivistHappy:
                    DrawMentor(texture, primary, secondary, MentorExpression.Happy, 4);
                    break;
                case PixelSpriteKind.MentorArchivistFrown:
                    DrawMentor(texture, primary, secondary, MentorExpression.Frown, 4);
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
                    DrawBookshelf(texture, primary, secondary, normalizedVariant);
                    break;
                case PixelSpriteKind.Candle:
                    DrawCandle(texture, primary, secondary, normalizedVariant);
                    break;
                case PixelSpriteKind.FloorGuard:
                    DrawFloorGuard(texture, primary, secondary, normalizedVariant);
                    break;
                case PixelSpriteKind.WallCorner:
                    DrawWallCorner(texture, primary, secondary, normalizedVariant);
                    break;
                case PixelSpriteKind.Pillar:
                    DrawPillar(texture, primary, secondary, normalizedVariant);
                    break;
                case PixelSpriteKind.GuideArrow:
                    DrawGuideArrow(texture, primary, secondary);
                    break;
                case PixelSpriteKind.RuneCircle:
                    DrawRuneCircle(texture, primary, secondary);
                    break;
                case PixelSpriteKind.FireRune:
                    DrawFireRune(texture, primary, secondary);
                    break;
                case PixelSpriteKind.WaterRune:
                    DrawWaterRune(texture, primary, secondary);
                    break;
                case PixelSpriteKind.WindRune:
                    DrawWindRune(texture, primary, secondary);
                    break;
                case PixelSpriteKind.EarthRune:
                    DrawEarthRune(texture, primary, secondary);
                    break;
                case PixelSpriteKind.LifeRune:
                    DrawLifeRune(texture, primary, secondary);
                    break;
                case PixelSpriteKind.WaterHazard:
                    DrawWaterHazard(texture, primary, secondary);
                    break;
                case PixelSpriteKind.IceBridge:
                    DrawIceBridge(texture, primary, secondary);
                    break;
                case PixelSpriteKind.VineBridge:
                    DrawVineBridge(texture, primary, secondary);
                    break;
                case PixelSpriteKind.EarthStep:
                    DrawEarthStep(texture, primary, secondary);
                    break;
                case PixelSpriteKind.WindPlatformTile:
                    DrawWindPlatformTile(texture, primary, secondary);
                    break;
                case PixelSpriteKind.CliffFace:
                    DrawCliffFace(texture, primary, secondary);
                    break;
                case PixelSpriteKind.Portal:
                    DrawPortal(texture, primary, secondary);
                    break;
                case PixelSpriteKind.Rubble:
                    DrawRubble(texture, primary, secondary);
                    break;
                case PixelSpriteKind.ShapeLine:
                    DrawShapeLine(texture, primary, secondary);
                    break;
                case PixelSpriteKind.ShapeArrow:
                    DrawShapeArrow(texture, primary, secondary);
                    break;
                case PixelSpriteKind.ShapeRect:
                    DrawShapeRect(texture, primary, secondary);
                    break;
                case PixelSpriteKind.ShapeEllipse:
                    DrawShapeEllipse(texture, primary, secondary);
                    break;
                case PixelSpriteKind.ShapeHexagon:
                    DrawShapeHexagon(texture, primary, secondary);
                    break;
                case PixelSpriteKind.ShapeBrace:
                    DrawShapeBrace(texture, primary, secondary);
                    break;
                case PixelSpriteKind.ShapeCross:
                    DrawShapeCross(texture, primary, secondary);
                    break;
            }

            texture.Apply();
            return texture;
        }

        public static int GetVariantCount(PixelSpriteKind kind)
        {
            return kind switch
            {
                PixelSpriteKind.Bookshelf => 3,
                PixelSpriteKind.Candle => 3,
                PixelSpriteKind.FloorGuard => 4,
                PixelSpriteKind.WallCorner => 4,
                PixelSpriteKind.Pillar => 2,
                _ => 1
            };
        }

        public static int SelectDeterministicVariant(PixelSpriteKind kind, int floorNumber, Vector2 position, string salt = "")
        {
            var count = GetVariantCount(kind);
            if (count <= 1)
            {
                return NoVariantIndex;
            }

            unchecked
            {
                var hash = 2166136261u;
                hash = (hash ^ (uint)(int)kind) * 16777619u;
                hash = (hash ^ (uint)Mathf.RoundToInt(position.x * 100f)) * 16777619u;
                hash = (hash ^ (uint)Mathf.RoundToInt(position.y * 100f)) * 16777619u;
                hash = (hash ^ (uint)floorNumber) * 16777619u;
                for (var index = 0; index < salt.Length; index++)
                {
                    hash = (hash ^ (uint)salt[index]) * 16777619u;
                }

                return (int)(hash % (uint)count);
            }
        }

        public static int SelectDeterministicVariantAvoiding(PixelSpriteKind kind, int floorNumber, Vector2 position, string salt, int avoidVariant)
        {
            var count = GetVariantCount(kind);
            var variant = SelectDeterministicVariant(kind, floorNumber, position, salt);
            if (count > 1 && avoidVariant >= 0 && variant == NormalizeVariantIndex(kind, avoidVariant))
            {
                variant = (variant + 1) % count;
            }

            return variant;
        }

        /// <summary>
        /// Looks up a sprite under <c>Resources/Sprites/&lt;Kind&gt;</c>.
        /// Results are cached so a missing PNG only hits disk once per session.
        ///
        /// Call <see cref="ResetExternalSpriteCache"/> from editor reload hooks
        /// or tests when the art needs to be re-discovered.
        /// </summary>
        private static Sprite LoadExternalSprite(PixelSpriteKind kind, int variantIndex)
        {
            var key = (kind, variantIndex);
            if (ExternalCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            if (ExternalMissCache.Contains(key))
            {
                return null;
            }

            foreach (var path in ExternalSpritePaths(kind, variantIndex))
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite == null)
                {
                    var texture = Resources.Load<Texture2D>(path);
                    if (texture == null)
                    {
                        continue;
                    }

                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    sprite = CreateFullRectSprite(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        SpritePixelsPerUnit);
                    sprite.name = ResourceNameFrom(path);
                }

                ExternalCache[key] = sprite;
                return sprite;
            }

            ExternalMissCache.Add(key);
            return null;
        }

        /// <summary>
        /// Drops the external sprite caches. Useful for editor reload hooks
        /// or PlayMode tests that want a clean lookup.
        /// </summary>
        public static void ResetExternalSpriteCache()
        {
            ExternalCache.Clear();
            ExternalMissCache.Clear();
            ApprenticeFrameCache.Clear();
        }

        private static IEnumerable<string> ExternalSpritePaths(PixelSpriteKind kind, int variantIndex)
        {
            if (variantIndex >= 0)
            {
                yield return $"{ExternalSpriteRoot}{kind}_{variantIndex}";
                yield return $"{ExternalSpriteRoot}{kind}/{variantIndex}";
            }

            yield return ExternalSpriteRoot + kind;
        }

        private static string ResourceNameFrom(string path)
        {
            var slash = path.LastIndexOf('/');
            return slash >= 0 ? path[(slash + 1)..] : path;
        }

        private static int NormalizeVariantIndex(PixelSpriteKind kind, int variantIndex)
        {
            var count = GetVariantCount(kind);
            if (count <= 1)
            {
                return NoVariantIndex;
            }

            return PositiveModulo(variantIndex < 0 ? 0 : variantIndex, count);
        }

        private static int PositiveModulo(int value, int divisor)
        {
            return ((value % divisor) + divisor) % divisor;
        }

        private static void DrawPlayer(Texture2D texture, Color skin, Color robe)
        {
            DrawApprentice(texture, skin, robe, PlayerFacing.Down, 0, 0, false, 0, 0);
        }

        /// <summary>
        /// Builds one procedural animation frame for the apprentice using the
        /// same silhouette as <see cref="PixelSpriteKind.Player"/>. Used by
        /// <see cref="PlayerSpriteLibrary"/> when no external frame PNGs exist,
        /// so the player still walks, breathes, and casts without assets.
        /// Frames are cached per (state, facing, frame); the player colors are
        /// constant so they are not part of the key.
        /// </summary>
        public static Sprite CreateApprenticeFrame(Color skin, Color robe, PlayerAnimationState state, PlayerFacing facing, int frame)
        {
            var key = (state, facing, frame);
            if (ApprenticeFrameCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var leftBootLift = 0;
            var rightBootLift = 0;
            var armsRaised = false;
            var sparkLevel = 0;
            var headBob = 0;
            switch (state)
            {
                case PlayerAnimationState.Idle:
                    headBob = frame % 2;
                    break;
                case PlayerAnimationState.Walk:
                    leftBootLift = frame % 4 == 1 ? 1 : 0;
                    rightBootLift = frame % 4 == 3 ? 1 : 0;
                    break;
                case PlayerAnimationState.CastCharge:
                    armsRaised = true;
                    sparkLevel = Mathf.Clamp(frame, 0, 2);
                    break;
                case PlayerAnimationState.CastRelease:
                    armsRaised = true;
                    sparkLevel = frame == 0 ? 3 : 2;
                    break;
            }

            var drawFacing = facing == PlayerFacing.Left ? PlayerFacing.Right : facing;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = $"Apprentice {state} {facing} {frame} Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Clear(texture);
            DrawApprentice(texture, skin, robe, drawFacing, leftBootLift, rightBootLift, armsRaised, sparkLevel, headBob);
            if (facing == PlayerFacing.Left)
            {
                MirrorHorizontally(texture);
            }

            texture.Apply();
            var sprite = CreateFullRectSprite(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), SpritePixelsPerUnit);
            ApprenticeFrameCache[key] = sprite;
            return sprite;
        }

        private static Sprite CreateFullRectSprite(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit)
        {
            return Sprite.Create(texture, rect, pivot, pixelsPerUnit, 0u, SpriteMeshType.FullRect);
        }

        private static void DrawApprentice(
            Texture2D texture,
            Color skin,
            Color robe,
            PlayerFacing facing,
            int leftBootLift,
            int rightBootLift,
            bool armsRaised,
            int sparkLevel,
            int headBob)
        {
            var outline = new Color(0.035f, 0.032f, 0.045f, 1f);
            var robeDark = Shade(robe, 0.48f);
            var robeMid = Shade(robe, 0.78f);
            var robeLight = Mix(robe, Color.white, 0.28f);
            var hair = new Color(0.22f, 0.13f, 0.08f, 1f);
            var gold = new Color(1f, 0.78f, 0.26f, 1f);

            Ellipse(texture, 16, 5, 9, 3, new Color(0f, 0f, 0f, 0.35f));
            Fill(texture, 11, 6, 10, 3, outline);
            Fill(texture, 12, 7, 8, 2, robeDark);
            Fill(texture, 9, 9, 14, 13, outline);
            Fill(texture, 10, 10, 12, 12, robeMid);
            Fill(texture, 12, 11, 8, 10, robe);
            Fill(texture, 15, 10, 2, 11, robeLight);

            if (armsRaised)
            {
                // Both arms lifted toward the sky while casting.
                Fill(texture, 7, 16, 3, 7, outline);
                Fill(texture, 22, 16, 3, 7, outline);
                Fill(texture, 8, 17, 1, 5, robeDark);
                Fill(texture, 23, 17, 1, 5, robeDark);
                Fill(texture, 7, 23, 2, 2, skin);
                Fill(texture, 23, 23, 2, 2, skin);
                if (sparkLevel > 0)
                {
                    var spark = sparkLevel >= 2 ? Color.white : gold;
                    Set(texture, 8, 26, spark);
                    Set(texture, 23, 26, spark);
                    Set(texture, 7, 25, Mix(spark, robe, 0.4f));
                    Set(texture, 24, 25, Mix(spark, robe, 0.4f));
                    if (sparkLevel >= 3)
                    {
                        Set(texture, 9, 27, spark);
                        Set(texture, 22, 27, spark);
                        Set(texture, 16, 31, spark);
                    }
                }
            }
            else
            {
                Fill(texture, 8, 12, 3, 8, outline);
                Fill(texture, 21, 12, 3, 8, outline);
                Fill(texture, 9, 13, 2, 6, robeDark);
                Fill(texture, 21, 13, 2, 6, robeDark);
            }

            Set(texture, 14, 16, gold);
            Set(texture, 17, 16, gold);

            // Head block by facing; headBob lowers it 1px for an idle breath.
            var bob = -Mathf.Clamp(headBob, 0, 1);
            Fill(texture, 12, 21 + bob, 8, 5, outline);
            switch (facing)
            {
                case PlayerFacing.Up:
                    // Back of the hood: no face, a center seam instead.
                    Fill(texture, 13, 22 + bob, 6, 4, robeMid);
                    Fill(texture, 15, 22 + bob, 2, 4, robeDark);
                    Fill(texture, 12, 25 + bob, 8, 3, robeDark);
                    break;
                case PlayerFacing.Right:
                    // Profile facing right: hair in the back, one eye.
                    Fill(texture, 13, 22 + bob, 6, 4, skin);
                    Fill(texture, 12, 22 + bob, 3, 4, hair);
                    Fill(texture, 12, 25 + bob, 8, 3, hair);
                    Set(texture, 18, 23 + bob, outline);
                    Set(texture, 17, 21 + bob, Shade(skin, 0.75f));
                    break;
                default:
                    Fill(texture, 13, 22 + bob, 6, 4, skin);
                    Fill(texture, 12, 25 + bob, 8, 3, hair);
                    Set(texture, 13, 23 + bob, outline);
                    Set(texture, 18, 23 + bob, outline);
                    Set(texture, 16, 21 + bob, Shade(skin, 0.75f));
                    break;
            }

            Fill(texture, 10, 27 + bob, 12, 2, outline);
            Fill(texture, 11, 28 + bob, 10, 1, robeDark);
            Fill(texture, 13, 29 + bob, 6, 2, robe);

            // Boots; a lifted boot reads as a stride frame.
            Fill(texture, 11, 5 + leftBootLift, 4, 2, outline);
            Fill(texture, 17, 5 + rightBootLift, 4, 2, outline);
            Fill(texture, 12, 6 + leftBootLift, 3, 1, robeDark);
            Fill(texture, 17, 6 + rightBootLift, 3, 1, robeDark);
        }

        private static void MirrorHorizontally(Texture2D texture)
        {
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width / 2; x++)
                {
                    var mirroredX = texture.width - 1 - x;
                    var left = texture.GetPixel(x, y);
                    var right = texture.GetPixel(mirroredX, y);
                    texture.SetPixel(x, y, right);
                    texture.SetPixel(mirroredX, y, left);
                }
            }
        }

        private static void DrawMentor(Texture2D texture, Color skin, Color robe, MentorExpression expression, int variant)
        {
            DrawPlayer(texture, skin, robe);
            var outline = new Color(0.035f, 0.032f, 0.045f, 1f);
            var robeDark = Shade(robe, 0.5f);
            var robeLight = Mix(robe, Color.white, 0.32f);
            Fill(texture, 13, 22, 6, 3, skin);
            Fill(texture, 12, 25, 8, 3, new Color(0.18f, 0.10f, 0.22f, 1f));
            DrawMentorVariant(texture, skin, robe, variant, outline, robeDark, robeLight);

            switch (expression)
            {
                case MentorExpression.Happy:
                    Set(texture, 13, 23, outline);
                    Set(texture, 14, 24, outline);
                    Set(texture, 18, 23, outline);
                    Set(texture, 17, 24, outline);
                    Set(texture, 15, 22, Shade(skin, 0.75f));
                    Set(texture, 16, 22, Shade(skin, 0.75f));
                    break;
                case MentorExpression.Frown:
                    Line(texture, 12, 24, 14, 23, outline);
                    Line(texture, 17, 23, 19, 24, outline);
                    Line(texture, 14, 22, 17, 22, outline);
                    Set(texture, 16, 21, Shade(skin, 0.75f));
                    break;
                default:
                    Set(texture, 13, 23, outline);
                    Set(texture, 18, 23, outline);
                    Set(texture, 16, 21, Shade(skin, 0.75f));
                    break;
            }
        }

        private static void DrawMentorVariant(Texture2D texture, Color skin, Color robe, int variant, Color outline, Color robeDark, Color robeLight)
        {
            switch (variant)
            {
                case 1:
                    Fill(texture, 11, 25, 10, 2, outline);
                    Fill(texture, 12, 26, 8, 1, robeLight);
                    Fill(texture, 12, 23, 3, 2, new Color(0.78f, 0.95f, 1f, 1f));
                    Fill(texture, 17, 23, 3, 2, new Color(0.78f, 0.95f, 1f, 1f));
                    Line(texture, 15, 24, 17, 24, outline);
                    Fill(texture, 6, 13, 4, 5, outline);
                    Fill(texture, 7, 14, 3, 4, new Color(0.22f, 0.16f, 0.10f, 1f));
                    Line(texture, 8, 18, 10, 14, robeLight);
                    break;
                case 2:
                    Fill(texture, 10, 24, 12, 4, outline);
                    Fill(texture, 11, 24, 10, 3, robeDark);
                    Fill(texture, 9, 18, 4, 7, outline);
                    Fill(texture, 19, 18, 4, 7, outline);
                    Line(texture, 9, 17, 6, 14, robeLight);
                    Line(texture, 23, 17, 26, 14, robeLight);
                    Fill(texture, 13, 27, 6, 2, robe);
                    break;
                case 3:
                    Fill(texture, 10, 26, 12, 2, outline);
                    Fill(texture, 11, 27, 10, 1, robeDark);
                    Fill(texture, 13, 28, 6, 2, robe);
                    Fill(texture, 8, 12, 4, 10, outline);
                    Fill(texture, 20, 12, 4, 10, outline);
                    Fill(texture, 9, 13, 2, 8, robeDark);
                    Fill(texture, 21, 13, 2, 8, robeDark);
                    Line(texture, 12, 25, 8, 29, robeLight);
                    Line(texture, 19, 25, 23, 29, robeLight);
                    Fill(texture, 13, 24, 6, 1, new Color(1f, 0.78f, 0.36f, 1f));
                    break;
                case 4:
                    Fill(texture, 10, 25, 12, 2, outline);
                    Fill(texture, 12, 27, 8, 2, outline);
                    Fill(texture, 13, 28, 6, 3, robeDark);
                    Set(texture, 16, 30, Color.white);
                    Set(texture, 15, 29, Color.white);
                    Set(texture, 17, 29, Color.white);
                    Fill(texture, 7, 12, 5, 4, outline);
                    Fill(texture, 8, 13, 4, 3, new Color(0.78f, 0.70f, 0.42f, 1f));
                    Fill(texture, 22, 12, 3, 7, outline);
                    Fill(texture, 23, 13, 2, 6, robeLight);
                    break;
                default:
                    Fill(texture, 10, 27, 12, 2, outline);
                    Fill(texture, 11, 28, 10, 1, robeDark);
                    Fill(texture, 13, 29, 6, 2, robe);
                    break;
            }
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
            Fill(texture, 0, 0, Size, Size, primary);
            Fill(texture, 0, 0, Size, 1, Shade(primary, 0.55f));
            Fill(texture, 0, 0, 1, Size, Shade(primary, 0.55f));
            Fill(texture, 0, 31, Size, 1, Mix(primary, Color.white, 0.08f));
            Fill(texture, 31, 0, 1, Size, Mix(primary, Color.white, 0.08f));
            Line(texture, 2, 15, 30, 15, secondary);
            Line(texture, 15, 2, 15, 30, secondary);
            Set(texture, 6, 8, Mix(primary, Color.white, 0.1f));
            Set(texture, 22, 5, Shade(primary, 0.68f));
            Set(texture, 25, 22, Mix(primary, Color.white, 0.08f));
            Set(texture, 8, 25, Shade(primary, 0.7f));
            Line(texture, 20, 18, 24, 20, Shade(primary, 0.55f));
            Line(texture, 7, 12, 10, 10, Mix(primary, Color.white, 0.08f));
        }

        private static void DrawWallTrim(Texture2D texture, Color primary, Color secondary)
        {
            Fill(texture, 0, 0, Size, Size, Shade(primary, 0.68f));
            Fill(texture, 0, 20, Size, 12, primary);
            Fill(texture, 0, 17, Size, 3, secondary);
            Fill(texture, 0, 15, Size, 2, Shade(primary, 0.42f));
            for (var x = 0; x < Size; x += 8)
            {
                Line(texture, x, 20, x + 3, 31, Shade(primary, 0.5f));
                Fill(texture, x + 1, 22, 2, 2, Mix(primary, Color.white, 0.12f));
            }
        }

        private static void DrawRug(Texture2D texture, Color primary, Color secondary)
        {
            Fill(texture, 0, 0, Size, Size, Shade(primary, 0.55f));
            Fill(texture, 3, 0, 26, Size, primary);
            Fill(texture, 5, 0, 2, Size, secondary);
            Fill(texture, 25, 0, 2, Size, secondary);
            Fill(texture, 9, 0, 14, Size, Shade(primary, 0.82f));
            for (var y = 3; y < Size; y += 8)
            {
                Diamond(texture, 16, y + 2, 5, 3, secondary);
                Set(texture, 16, y + 2, Color.white);
            }
            Fill(texture, 0, 0, 3, Size, new Color(0f, 0f, 0f, 0.25f));
            Fill(texture, 29, 0, 3, Size, new Color(0f, 0f, 0f, 0.25f));
        }

        private static void DrawBookshelf(Texture2D texture, Color wood, Color accent, int variant)
        {
            var outline = new Color(0.04f, 0.025f, 0.018f, 1f);
            var shelfDark = Shade(wood, 0.55f);
            Fill(texture, 5, 4, 22, 24, outline);
            Fill(texture, 6, 5, 20, 22, wood);
            Fill(texture, 7, 8, 18, 2, shelfDark);
            Fill(texture, 7, 16, 18, 2, shelfDark);
            Fill(texture, 7, 24, 18, 2, shelfDark);
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
                    if (variant == 1 && (shelf + i) % 3 == 0)
                    {
                        continue;
                    }

                    if (variant == 2 && shelf == 1 && i is >= 2 and <= 4)
                    {
                        continue;
                    }

                    var color = colors[(shelf + i) % colors.Length];
                    Fill(texture, 8 + i * 2, 10 + shelf * 8, 1, 5, color);
                    Set(texture, 8 + i * 2, 14 + shelf * 8, Shade(color, 0.6f));
                }
            }

            if (variant == 1)
            {
                Fill(texture, 8, 12, 6, 2, new Color(0.82f, 0.72f, 0.48f, 1f));
                Line(texture, 16, 20, 23, 22, Shade(wood, 0.32f));
                Fill(texture, 18, 25, 5, 1, new Color(0.77f, 0.68f, 0.46f, 1f));
            }
            else if (variant == 2)
            {
                Diamond(texture, 16, 19, 4, 4, outline);
                Diamond(texture, 16, 20, 3, 3, accent);
                Set(texture, 16, 21, Color.white);
                Fill(texture, 10, 25, 4, 2, new Color(0.42f, 0.20f, 0.72f, 1f));
                Fill(texture, 18, 25, 4, 2, new Color(0.22f, 0.58f, 0.72f, 1f));
            }
        }

        private static void DrawCandle(Texture2D texture, Color metal, Color flame, int variant)
        {
            var outline = new Color(0.04f, 0.035f, 0.03f, 1f);
            Ellipse(texture, 16, 4, 7, 2, new Color(0f, 0f, 0f, 0.3f));
            if (variant == 1)
            {
                DrawCandleStem(texture, 11, 5, 4, 10, metal, flame, outline);
                DrawCandleStem(texture, 16, 5, 4, 16, metal, flame, outline);
                DrawCandleStem(texture, 21, 5, 4, 12, metal, flame, outline);
                Fill(texture, 9, 10, 15, 2, outline);
                Fill(texture, 10, 11, 13, 1, Shade(metal, 1.12f));
            }
            else if (variant == 2)
            {
                DrawCandleStem(texture, 12, 5, 5, 8, metal, flame, outline);
                DrawCandleStem(texture, 18, 5, 4, 11, Shade(metal, 1.08f), flame, outline);
                Fill(texture, 11, 5, 11, 2, outline);
                Fill(texture, 12, 6, 9, 1, Shade(metal, 0.78f));
                Set(texture, 20, 10, Shade(metal, 1.3f));
                Set(texture, 13, 8, Shade(metal, 1.3f));
            }
            else
            {
                DrawCandleStem(texture, 16, 5, 4, 14, metal, flame, outline);
                Fill(texture, 10, 12, 12, 2, outline);
                Fill(texture, 11, 13, 10, 1, Shade(metal, 1.15f));
                DrawCandleStem(texture, 10, 9, 3, 6, metal, flame, outline);
                DrawCandleStem(texture, 21, 9, 3, 6, metal, flame, outline);
            }
        }

        private static void DrawCandleStem(Texture2D texture, int centerX, int baseY, int width, int height, Color metal, Color flame, Color outline)
        {
            Fill(texture, centerX - width / 2, baseY, width, height, outline);
            Fill(texture, centerX - width / 2 + 1, baseY + 1, Mathf.Max(1, width - 2), Mathf.Max(1, height - 2), metal);
            var flameY = baseY + height + 3;
            Diamond(texture, centerX, flameY, 4, 6, Shade(flame, 0.85f));
            Diamond(texture, centerX, flameY + 1, 2, 4, Mix(flame, Color.white, 0.45f));
            Set(texture, centerX, flameY + 3, Color.white);
            Set(texture, centerX - 1, flameY - 1, new Color(1f, 0.32f, 0.12f, 1f));
        }

        private static void DrawFloorGuard(Texture2D texture, Color primary, Color accent, int variant)
        {
            var outline = new Color(0.035f, 0.035f, 0.045f, 1f);
            var metal = Shade(primary, 0.78f);
            var light = Mix(primary, Color.white, 0.25f);
            Fill(texture, 4, 6, 24, 20, outline);
            Fill(texture, 5, 7, 22, 18, metal);
            Fill(texture, 6, 22, 20, 2, light);
            Fill(texture, 6, 8, 20, 2, Shade(primary, 0.52f));
            Line(texture, 7, 12, 24, 12, Shade(primary, 0.58f));
            Line(texture, 7, 18, 24, 18, Shade(primary, 0.58f));

            switch (variant)
            {
                case 1:
                    Line(texture, 10, 22, 15, 16, outline);
                    Line(texture, 15, 16, 13, 10, outline);
                    Line(texture, 20, 20, 24, 15, Shade(primary, 0.42f));
                    break;
                case 2:
                    Ring(texture, 16, 16, 6, 5, new Color(accent.r, accent.g, accent.b, 0.82f));
                    Line(texture, 16, 10, 16, 22, accent);
                    Line(texture, 10, 16, 22, 16, accent);
                    Set(texture, 16, 16, Color.white);
                    break;
                case 3:
                    Ellipse(texture, 15, 15, 8, 6, new Color(0.02f, 0.018f, 0.014f, 0.62f));
                    Line(texture, 9, 12, 22, 19, new Color(0.86f, 0.36f, 0.16f, 1f));
                    Set(texture, 20, 19, Color.white);
                    break;
            }
        }

        private static void DrawWallCorner(Texture2D texture, Color primary, Color accent, int variant)
        {
            var outline = new Color(0.035f, 0.032f, 0.045f, 1f);
            var stone = Shade(primary, 0.82f);
            Fill(texture, 4, 4, 9, 24, outline);
            Fill(texture, 4, 19, 24, 9, outline);
            Fill(texture, 6, 6, 5, 20, stone);
            Fill(texture, 6, 21, 20, 5, stone);
            Fill(texture, 11, 21, 4, 5, Shade(primary, 0.55f));
            Fill(texture, 6, 15, 5, 4, Shade(primary, 0.55f));
            Set(texture, 9, 24, Mix(primary, Color.white, 0.22f));
            Set(texture, 20, 24, Mix(primary, Color.white, 0.16f));

            switch (variant)
            {
                case 1:
                    Line(texture, 8, 24, 14, 19, outline);
                    Line(texture, 8, 17, 11, 12, outline);
                    break;
                case 2:
                    Diamond(texture, 9, 23, 3, 2, accent);
                    Diamond(texture, 9, 12, 2, 3, accent);
                    Set(texture, 9, 23, Color.white);
                    break;
                case 3:
                    Fill(texture, 7, 8, 3, 2, Shade(primary, 0.48f));
                    Fill(texture, 17, 22, 5, 2, Shade(primary, 0.48f));
                    Line(texture, 5, 27, 27, 5, new Color(0f, 0f, 0f, 0.22f));
                    break;
            }
        }

        private static void DrawPillar(Texture2D texture, Color primary, Color accent, int variant)
        {
            var outline = new Color(0.035f, 0.032f, 0.045f, 1f);
            var stone = Shade(primary, 0.86f);
            Ellipse(texture, 16, 5, 8, 2, new Color(0f, 0f, 0f, 0.28f));
            Fill(texture, 8, 5, 16, 4, outline);
            Fill(texture, 10, 6, 12, 2, Shade(primary, 0.62f));
            Fill(texture, 10, 8, 12, 18, outline);
            Fill(texture, 12, 9, 8, 16, stone);
            Fill(texture, 15, 9, 2, 16, Mix(primary, Color.white, 0.2f));
            Fill(texture, 7, 25, 18, 4, outline);
            Fill(texture, 9, 26, 14, 2, Shade(primary, 0.62f));

            if (variant == 1)
            {
                Line(texture, 12, 12, 20, 20, accent);
                Line(texture, 12, 20, 20, 12, accent);
                Ring(texture, 16, 16, 5, 4, new Color(accent.r, accent.g, accent.b, 0.52f));
                Set(texture, 16, 16, Color.white);
            }
            else
            {
                Fill(texture, 11, 13, 10, 2, Shade(primary, 0.58f));
                Fill(texture, 11, 19, 10, 2, Shade(primary, 0.58f));
                Set(texture, 13, 16, accent);
                Set(texture, 19, 16, accent);
            }
        }

        private static void DrawGuideArrow(Texture2D texture, Color primary, Color secondary)
        {
            var outline = new Color(0.05f, 0.035f, 0.012f, 1f);
            var glow = Mix(primary, Color.white, 0.38f);

            for (var offset = -2; offset <= 2; offset++)
            {
                Line(texture, 6 + offset, 25, 23 + offset, 8, outline);
                Line(texture, 6, 25 + offset, 23, 8 + offset, outline);
            }

            for (var offset = -1; offset <= 1; offset++)
            {
                Line(texture, 7 + offset, 24, 22 + offset, 9, primary);
                Line(texture, 7, 24 + offset, 22, 9 + offset, primary);
            }

            Line(texture, 22, 9, 22, 18, outline);
            Line(texture, 22, 9, 13, 9, outline);
            Line(texture, 21, 10, 21, 17, secondary);
            Line(texture, 21, 10, 14, 10, secondary);
            Fill(texture, 18, 11, 4, 4, glow);
            Set(texture, 22, 9, Color.white);
            Set(texture, 21, 10, Color.white);
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

        private static readonly Color RuneOutline = new Color(0.04f, 0.035f, 0.05f, 1f);

        private static void DrawFireRune(Texture2D texture, Color primary, Color secondary)
        {
            var glow = Mix(primary, Color.white, 0.4f);
            var deep = Shade(primary, 0.6f);

            // Upward triangle, base at y=5 (bottom), apex at y=28 (top)
            for (var y = 6; y <= 27; y++)
            {
                var progress = (y - 6) / 21f;
                var halfW = Mathf.RoundToInt(Mathf.Lerp(11f, 0.5f, progress));
                Line(texture, 16 - halfW, y, 16 + halfW, y, primary);
            }
            // Outline edges
            Line(texture, 5, 5, 27, 5, RuneOutline);
            Line(texture, 5, 5, 16, 28, RuneOutline);
            Line(texture, 27, 5, 16, 28, RuneOutline);
            // Bottom shadow band
            Line(texture, 6, 6, 26, 6, deep);
            // Inner flame core
            Diamond(texture, 16, 13, 3, 5, glow);
            Set(texture, 16, 17, Color.white);
            // Spark dot near apex
            Set(texture, 16, 26, secondary);
        }

        private static void DrawWaterRune(Texture2D texture, Color primary, Color secondary)
        {
            var glow = Mix(primary, Color.white, 0.45f);
            var deep = Shade(primary, 0.55f);

            // Closed circular loop
            Ellipse(texture, 16, 16, 12, 12, RuneOutline);
            Ellipse(texture, 16, 16, 11, 11, primary);
            Ellipse(texture, 16, 16, 7, 7, deep);
            Ellipse(texture, 16, 16, 5, 5, primary);
            Ellipse(texture, 17, 18, 3, 3, glow);
            Set(texture, 14, 20, Color.white);
            // Drop highlight upper right
            Set(texture, 21, 21, glow);
            Set(texture, 22, 20, glow);
            // Inner core dot
            Set(texture, 16, 16, secondary);
        }

        private static void DrawWindRune(Texture2D texture, Color primary, Color secondary)
        {
            var glow = Mix(primary, Color.white, 0.35f);
            var deep = Shade(primary, 0.7f);

            // Three open parallel horizontal lines
            for (var i = 0; i < 3; i++)
            {
                var y = 9 + i * 7; // y=9, 16, 23
                Fill(texture, 7, y, 18, 2, primary);
                // Top and bottom outlines
                Line(texture, 7, y - 1, 24, y - 1, RuneOutline);
                Line(texture, 7, y + 2, 24, y + 2, RuneOutline);
                // Tapered ends
                Set(texture, 6, y, deep);
                Set(texture, 6, y + 1, deep);
                Set(texture, 25, y, deep);
                Set(texture, 25, y + 1, deep);
            }
            // Middle line accent
            Fill(texture, 13, 16, 6, 2, glow);
            Set(texture, 16, 16, Color.white);
            // Tip dots showing motion
            Set(texture, 27, 9, secondary);
            Set(texture, 27, 23, secondary);
        }

        private static void DrawEarthRune(Texture2D texture, Color primary, Color secondary)
        {
            var glow = Mix(primary, Color.white, 0.3f);
            var deep = Shade(primary, 0.6f);

            // Trapezoid: wide bottom, narrow top
            for (var y = 6; y <= 26; y++)
            {
                var progress = (y - 6) / 20f;
                var halfW = Mathf.RoundToInt(Mathf.Lerp(11f, 5f, progress));
                Line(texture, 16 - halfW, y, 16 + halfW, y, primary);
            }
            // Outline edges
            Line(texture, 5, 5, 27, 5, RuneOutline);    // bottom
            Line(texture, 11, 27, 21, 27, RuneOutline); // top
            Line(texture, 5, 5, 11, 27, RuneOutline);   // left
            Line(texture, 27, 5, 21, 27, RuneOutline);  // right
            // Bottom heavy band
            Line(texture, 6, 6, 26, 6, deep);
            Line(texture, 6, 7, 26, 7, deep);
            // Top highlight
            Fill(texture, 13, 24, 6, 2, glow);
            // Center stone groove
            Line(texture, 12, 14, 20, 14, deep);
            Set(texture, 16, 14, secondary);
        }

        private static void DrawLifeRune(Texture2D texture, Color primary, Color secondary)
        {
            var glow = Mix(primary, Color.white, 0.4f);
            var deep = Shade(primary, 0.6f);

            // Vertical stem (lower half)
            Fill(texture, 15, 4, 3, 14, primary);
            Line(texture, 14, 4, 14, 17, RuneOutline);
            Line(texture, 18, 4, 18, 17, RuneOutline);
            // Branches splitting upward from y=17
            for (var step = 0; step <= 10; step++)
            {
                var t = step / 10f;
                var leftX = Mathf.RoundToInt(Mathf.Lerp(16f, 7f, t));
                var rightX = Mathf.RoundToInt(Mathf.Lerp(16f, 25f, t));
                var y = 17 + step;
                Set(texture, leftX, y, primary);
                Set(texture, leftX - 1, y, primary);
                Set(texture, rightX, y, primary);
                Set(texture, rightX + 1, y, primary);
                // Outline strokes
                Set(texture, leftX - 2, y, RuneOutline);
                Set(texture, rightX + 2, y, RuneOutline);
            }
            // Stem highlight
            Fill(texture, 16, 6, 1, 11, glow);
            // Bud tips
            Diamond(texture, 7, 27, 2, 2, glow);
            Diamond(texture, 25, 27, 2, 2, glow);
            Set(texture, 7, 27, Color.white);
            Set(texture, 25, 27, Color.white);
            // Root shadow at stem base
            Line(texture, 13, 5, 19, 5, deep);
            Set(texture, 16, 17, secondary);
        }

        private static void DrawWaterHazard(Texture2D texture, Color primary, Color secondary)
        {
            Fill(texture, 2, 7, 28, 18, Shade(primary, 0.72f));
            for (var y = 8; y <= 24; y += 4)
            {
                Line(texture, 4, y, 10, y + 1, Mix(primary, Color.white, 0.28f));
                Line(texture, 14, y + 1, 22, y, secondary);
            }
            Diamond(texture, 8, 14, 3, 2, Shade(secondary, 0.62f));
            Diamond(texture, 23, 18, 2, 2, Shade(secondary, 0.58f));
        }

        private static void DrawIceBridge(Texture2D texture, Color primary, Color secondary)
        {
            Fill(texture, 2, 10, 28, 12, Mix(primary, Color.white, 0.22f));
            Fill(texture, 2, 9, 28, 2, Color.white);
            for (var x = 5; x < 29; x += 7)
            {
                Line(texture, x, 11, x + 4, 21, secondary);
            }
            Line(texture, 2, 22, 29, 22, Shade(primary, 0.64f));
        }

        private static void DrawVineBridge(Texture2D texture, Color primary, Color secondary)
        {
            Line(texture, 2, 16, 29, 15, Shade(primary, 0.72f));
            Line(texture, 2, 18, 29, 17, primary);
            for (var x = 5; x < 29; x += 5)
            {
                Line(texture, x, 12, x + 2, 20, secondary);
                Diamond(texture, x + 1, 12, 2, 2, Mix(primary, Color.white, 0.2f));
            }
        }

        private static void DrawEarthStep(Texture2D texture, Color primary, Color secondary)
        {
            Fill(texture, 3, 7, 26, 8, Shade(primary, 0.78f));
            Fill(texture, 3, 15, 26, 7, primary);
            Fill(texture, 3, 22, 26, 3, secondary);
            for (var x = 5; x < 27; x += 7)
            {
                Line(texture, x, 9, x + 4, 13, Shade(secondary, 0.72f));
            }
        }

        private static void DrawWindPlatformTile(Texture2D texture, Color primary, Color secondary)
        {
            Fill(texture, 4, 14, 24, 6, Mix(primary, Color.white, 0.12f));
            Line(texture, 4, 13, 27, 13, Color.white);
            Line(texture, 4, 20, 27, 20, Shade(primary, 0.65f));
            Line(texture, 6, 8, 20, 8, secondary);
            Line(texture, 12, 24, 26, 24, secondary);
        }

        private static void DrawShapeLine(Texture2D texture, Color primary, Color secondary)
        {
            Line(texture, 5, 22, 27, 9, Shade(primary, 0.42f));
            Line(texture, 5, 23, 27, 10, Shade(primary, 0.62f));
            Line(texture, 6, 21, 26, 9, secondary);
            Line(texture, 6, 22, 26, 10, Color.white);
            Line(texture, 7, 23, 27, 11, primary);
            Line(texture, 8, 24, 28, 12, secondary);
        }

        private static void DrawShapeArrow(Texture2D texture, Color primary, Color secondary)
        {
            Line(texture, 6, 23, 24, 9, primary);
            Line(texture, 7, 24, 25, 10, Shade(primary, 0.72f));
            Line(texture, 24, 9, 24, 17, secondary);
            Line(texture, 24, 9, 16, 9, secondary);
            Line(texture, 23, 10, 19, 18, Color.white);
        }

        private static void DrawShapeRect(Texture2D texture, Color primary, Color secondary)
        {
            Fill(texture, 7, 9, 19, 15, Shade(primary, 0.42f));
            Line(texture, 7, 9, 25, 9, primary);
            Line(texture, 25, 9, 25, 23, primary);
            Line(texture, 25, 23, 7, 23, secondary);
            Line(texture, 7, 23, 7, 9, secondary);
            Line(texture, 10, 12, 22, 20, Mix(primary, Color.white, 0.30f));
        }

        private static void DrawShapeEllipse(Texture2D texture, Color primary, Color secondary)
        {
            Ellipse(texture, 16, 16, 12, 9, Shade(primary, 0.35f));
            Ellipse(texture, 16, 16, 10, 7, secondary);
            Ellipse(texture, 16, 16, 7, 4, new Color(0f, 0f, 0f, 0f));
            Line(texture, 8, 11, 24, 11, Mix(primary, Color.white, 0.24f));
            Line(texture, 8, 21, 24, 21, Shade(primary, 0.72f));
        }

        private static void DrawShapeHexagon(Texture2D texture, Color primary, Color secondary)
        {
            Line(texture, 12, 6, 20, 6, primary);
            Line(texture, 20, 6, 27, 16, primary);
            Line(texture, 27, 16, 20, 26, secondary);
            Line(texture, 20, 26, 12, 26, secondary);
            Line(texture, 12, 26, 5, 16, primary);
            Line(texture, 5, 16, 12, 6, primary);
            Line(texture, 11, 14, 21, 18, Mix(primary, Color.white, 0.32f));
            Diamond(texture, 16, 16, 2, 2, Color.white);
        }

        private static void DrawShapeBrace(Texture2D texture, Color primary, Color secondary)
        {
            Line(texture, 11, 5, 8, 9, primary);
            Line(texture, 8, 9, 8, 14, primary);
            Line(texture, 8, 14, 5, 16, secondary);
            Line(texture, 5, 16, 8, 18, secondary);
            Line(texture, 8, 18, 8, 23, primary);
            Line(texture, 8, 23, 11, 27, primary);
            Line(texture, 21, 5, 24, 9, primary);
            Line(texture, 24, 9, 24, 14, primary);
            Line(texture, 24, 14, 27, 16, secondary);
            Line(texture, 27, 16, 24, 18, secondary);
            Line(texture, 24, 18, 24, 23, primary);
            Line(texture, 24, 23, 21, 27, primary);
            Line(texture, 12, 16, 20, 16, Mix(primary, Color.white, 0.35f));
        }

        private static void DrawShapeCross(Texture2D texture, Color primary, Color secondary)
        {
            Line(texture, 8, 8, 24, 24, primary);
            Line(texture, 9, 8, 25, 24, secondary);
            Line(texture, 24, 8, 8, 24, primary);
            Line(texture, 25, 8, 9, 24, secondary);
        }

        private static void DrawCliffFace(Texture2D texture, Color primary, Color secondary)
        {
            Fill(texture, 2, 2, 28, 28, Shade(primary, 0.72f));
            Line(texture, 7, 2, 12, 28, secondary);
            Line(texture, 18, 3, 13, 29, Shade(secondary, 0.78f));
            Diamond(texture, 22, 19, 4, 3, Mix(primary, Color.white, 0.16f));
            Diamond(texture, 9, 9, 3, 2, Mix(primary, Color.white, 0.12f));
        }

        private static void DrawPortal(Texture2D texture, Color primary, Color secondary)
        {
            Ring(texture, 16, 16, 13, 8, Mix(primary, Color.white, 0.22f));
            Ring(texture, 16, 16, 8, 5, secondary);
            Fill(texture, 14, 6, 4, 20, Color.white);
            Fill(texture, 7, 14, 18, 4, Mix(secondary, Color.white, 0.3f));
        }

        private static void DrawRubble(Texture2D texture, Color primary, Color secondary)
        {
            Diamond(texture, 8, 10, 4, 3, primary);
            Diamond(texture, 18, 13, 6, 4, secondary);
            Diamond(texture, 24, 8, 3, 2, Shade(primary, 0.72f));
            Diamond(texture, 12, 22, 5, 3, Mix(primary, secondary, 0.5f));
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

    internal enum MentorExpression
    {
        Neutral,
        Happy,
        Frown
    }

    public enum PixelSpriteKind
    {
        Player = 0,
        Station = 1,
        Target = 2,
        Pulse = 3,
        FloorTile = 4,
        WallTrim = 5,
        Rug = 6,
        Bookshelf = 7,
        Candle = 8,
        RuneCircle = 9,
        FireRune = 10,
        WaterRune = 11,
        WindRune = 12,
        EarthRune = 13,
        LifeRune = 14,
        WaterHazard = 15,
        IceBridge = 16,
        VineBridge = 17,
        EarthStep = 18,
        WindPlatformTile = 19,
        CliffFace = 20,
        Portal = 21,
        Rubble = 22,
        GuideArrow = 23,
        ShapeLine = 24,
        ShapeArrow = 25,
        ShapeRect = 26,
        ShapeEllipse = 27,
        ShapeHexagon = 28,
        ShapeBrace = 29,
        ShapeCross = 30,
        FloorGuard = 31,
        WallCorner = 32,
        Pillar = 33,
        MentorNeutral = 34,
        MentorHappy = 35,
        MentorFrown = 36,
        MentorScholarNeutral = 37,
        MentorScholarHappy = 38,
        MentorScholarFrown = 39,
        MentorGuideNeutral = 40,
        MentorGuideHappy = 41,
        MentorGuideFrown = 42,
        MentorWatcherNeutral = 43,
        MentorWatcherHappy = 44,
        MentorWatcherFrown = 45,
        MentorArchivistNeutral = 46,
        MentorArchivistHappy = 47,
        MentorArchivistFrown = 48
    }
}

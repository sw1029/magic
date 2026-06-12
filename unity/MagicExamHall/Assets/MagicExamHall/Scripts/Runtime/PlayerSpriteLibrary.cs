using System.Collections.Generic;
using UnityEngine;

namespace MagicExamHall
{
    public enum PlayerFacing
    {
        Down,
        Up,
        Left,
        Right
    }

    public enum PlayerAnimationState
    {
        Idle,
        Walk,
        CastCharge,
        CastRelease
    }

    public sealed class PlayerSpriteSet
    {
        private readonly Dictionary<string, Sprite[]> frames;
        private readonly Sprite fallback;

        internal PlayerSpriteSet(Dictionary<string, Sprite[]> frames, Sprite fallback, bool hasExternalFrames)
        {
            this.frames = frames;
            this.fallback = fallback;
            HasExternalFrames = hasExternalFrames;
        }

        public bool HasExternalFrames { get; }

        public Sprite GetFrame(PlayerAnimationState state, PlayerFacing facing, int frameIndex)
        {
            var resolvedFrames = GetFrames(state, facing);
            return resolvedFrames[Mathf.Abs(frameIndex) % resolvedFrames.Length];
        }

        public int GetFrameCount(PlayerAnimationState state, PlayerFacing facing)
        {
            return GetFrames(state, facing).Length;
        }

        private Sprite[] GetFrames(PlayerAnimationState state, PlayerFacing facing)
        {
            if (frames.TryGetValue(PlayerSpriteLibrary.Key(state, facing), out var directionalFrames) && directionalFrames.Length > 0)
            {
                return directionalFrames;
            }

            if (frames.TryGetValue(PlayerSpriteLibrary.Key(state), out var sharedFrames) && sharedFrames.Length > 0)
            {
                return sharedFrames;
            }

            return new[] { fallback };
        }
    }

    public static class PlayerSpriteLibrary
    {
        public const int FrameWidth = 48;
        public const int FrameHeight = 64;
        public const string ResourceRoot = "Sprites/Player/";

        private static PlayerSpriteSet cachedSet;

        public static PlayerSpriteSet Load(Color fallbackSkin, Color fallbackRobe)
        {
            if (cachedSet != null)
            {
                return cachedSet;
            }

            var frames = new Dictionary<string, Sprite[]>();
            var loadedAllFrames = true;

            foreach (var facing in DirectionalFacings())
            {
                loadedAllFrames &= LoadDirectional(frames, PlayerAnimationState.Idle, facing, 2, fallbackSkin, fallbackRobe);
                loadedAllFrames &= LoadDirectional(frames, PlayerAnimationState.Walk, facing, 4, fallbackSkin, fallbackRobe);
            }

            loadedAllFrames &= LoadShared(frames, PlayerAnimationState.CastCharge, 3, fallbackSkin, fallbackRobe);
            loadedAllFrames &= LoadShared(frames, PlayerAnimationState.CastRelease, 2, fallbackSkin, fallbackRobe);

            var fallback = PixelArtFactory.CreateSprite("Player Animation Fallback", fallbackSkin, fallbackRobe, PixelSpriteKind.Player);
            var spriteSet = new PlayerSpriteSet(frames, fallback, loadedAllFrames);
            if (loadedAllFrames)
            {
                cachedSet = spriteSet;
            }

            return spriteSet;
        }

        public static PlayerSpriteSet CreateFallbackSet(Color fallbackSkin, Color fallbackRobe)
        {
            var fallback = PixelArtFactory.CreateSprite("Player Animation Fallback", fallbackSkin, fallbackRobe, PixelSpriteKind.Player);
            return new PlayerSpriteSet(new Dictionary<string, Sprite[]>(), fallback, hasExternalFrames: false);
        }

        public static string Key(PlayerAnimationState state, PlayerFacing facing)
        {
            return $"{Key(state)}_{Key(facing)}";
        }

        public static string Key(PlayerAnimationState state)
        {
            return state switch
            {
                PlayerAnimationState.Idle => "idle",
                PlayerAnimationState.Walk => "walk",
                PlayerAnimationState.CastCharge => "cast_charge",
                PlayerAnimationState.CastRelease => "cast_release",
                _ => state.ToString().ToLowerInvariant()
            };
        }

        public static string Key(PlayerFacing facing)
        {
            return facing switch
            {
                PlayerFacing.Down => "down",
                PlayerFacing.Up => "up",
                PlayerFacing.Left => "left",
                PlayerFacing.Right => "right",
                _ => facing.ToString().ToLowerInvariant()
            };
        }

        public static void ResetCache()
        {
            cachedSet = null;
        }

        private static IEnumerable<PlayerFacing> DirectionalFacings()
        {
            yield return PlayerFacing.Down;
            yield return PlayerFacing.Up;
            yield return PlayerFacing.Left;
            yield return PlayerFacing.Right;
        }

        private static bool LoadDirectional(Dictionary<string, Sprite[]> frames, PlayerAnimationState state, PlayerFacing facing, int frameCount, Color fallbackSkin, Color fallbackRobe)
        {
            var loaded = LoadFrames($"{Key(state)}_{Key(facing)}", frameCount);
            var external = loaded.Length == frameCount;
            if (!external)
            {
                loaded = BuildProceduralFrames(state, facing, frameCount, fallbackSkin, fallbackRobe);
            }

            frames[Key(state, facing)] = loaded;
            return external;
        }

        private static bool LoadShared(Dictionary<string, Sprite[]> frames, PlayerAnimationState state, int frameCount, Color fallbackSkin, Color fallbackRobe)
        {
            var loaded = LoadFrames(Key(state), frameCount);
            var external = loaded.Length == frameCount;
            if (!external)
            {
                loaded = BuildProceduralFrames(state, PlayerFacing.Down, frameCount, fallbackSkin, fallbackRobe);
            }

            frames[Key(state)] = loaded;
            return external;
        }

        private static Sprite[] BuildProceduralFrames(PlayerAnimationState state, PlayerFacing facing, int frameCount, Color skin, Color robe)
        {
            var generated = new Sprite[frameCount];
            for (var frame = 0; frame < frameCount; frame++)
            {
                generated[frame] = PixelArtFactory.CreateApprenticeFrame(skin, robe, state, facing, frame);
            }

            return generated;
        }

        private static Sprite[] LoadFrames(string prefix, int frameCount)
        {
            var loaded = new List<Sprite>(frameCount);
            for (var frame = 0; frame < frameCount; frame++)
            {
                var sprite = LoadFrame($"{prefix}_{frame}");
                if (sprite == null)
                {
                    break;
                }

                loaded.Add(sprite);
            }

            return loaded.ToArray();
        }

        private static Sprite LoadFrame(string resourceName)
        {
            var path = ResourceRoot + resourceName;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                PixelRenderSetup.AssetsPixelsPerUnit);
            sprite.name = resourceName;
            return sprite;
        }
    }
}

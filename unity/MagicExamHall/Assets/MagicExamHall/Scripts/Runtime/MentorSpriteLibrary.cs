using System.Collections.Generic;
using UnityEngine;

namespace MagicExamHall
{
    public static class MentorSpriteLibrary
    {
        private const string ResourceRoot = "MentorSprites/";
        private const float TargetWorldHeight = 1.85f;
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static bool TryGetSprite(string spriteSetKey, MentorMood mood, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(spriteSetKey))
            {
                sprite = null;
                return false;
            }

            foreach (var resourceName in CandidateNames(spriteSetKey, mood))
            {
                sprite = LoadSprite(resourceName);
                if (sprite != null)
                {
                    return true;
                }
            }

            sprite = null;
            return false;
        }

        private static IEnumerable<string> CandidateNames(string spriteSetKey, MentorMood mood)
        {
            switch (mood)
            {
                case MentorMood.Happy:
                    yield return spriteSetKey + "_happy";
                    break;
                case MentorMood.Frown:
                    yield return spriteSetKey + "_frown";
                    break;
                default:
                    yield return spriteSetKey + "_neutral";
                    break;
            }

            yield return spriteSetKey + "_neutral";
            yield return spriteSetKey;
        }

        private static Sprite LoadSprite(string resourceName)
        {
            if (Cache.TryGetValue(resourceName, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(ResourceRoot + resourceName);
            if (sprite != null)
            {
                Cache[resourceName] = sprite;
                return sprite;
            }

            var texture = Resources.Load<Texture2D>(ResourceRoot + resourceName);
            if (texture == null)
            {
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixelsPerUnit = Mathf.Max(16f, texture.height / TargetWorldHeight);
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = resourceName;
            Cache[resourceName] = sprite;
            return sprite;
        }
    }
}

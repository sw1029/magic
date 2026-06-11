using UnityEngine;

namespace MagicExamHall
{
    /// <summary>
    /// Soft radial halo sprite used for one-shot glow bursts. Steady glow is
    /// handled by URP 2D point lights (<see cref="PixelRenderSetup"/>); this
    /// covers the moments lights cannot: a flash that swells and fades when a
    /// seal forms or a goal is satisfied.
    /// </summary>
    public static class GlowSpriteFactory
    {
        private const int HaloSize = 64;
        private const float PixelsPerUnit = 16f;

        private static Sprite halo;

        /// <summary>World-space diameter of the halo sprite at scale 1.</summary>
        public static float HaloWorldDiameter => HaloSize / PixelsPerUnit;

        public static Sprite Halo
        {
            get
            {
                if (halo == null)
                {
                    halo = BuildHalo();
                }

                return halo;
            }
        }

        private static Sprite BuildHalo()
        {
            var texture = new Texture2D(HaloSize, HaloSize, TextureFormat.RGBA32, false)
            {
                name = "Glow Halo Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var center = (HaloSize - 1) * 0.5f;
            for (var y = 0; y < HaloSize; y++)
            {
                for (var x = 0; x < HaloSize; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var falloff = Mathf.Clamp01(1f - distance);
                    // Quadratic falloff keeps a bright core with a soft skirt.
                    var alpha = falloff * falloff;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, HaloSize, HaloSize), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }
    }

    public static class GlowPulse
    {
        /// <summary>One-shot glow burst that fades out and destroys itself.</summary>
        public static void Flash(Vector2 position, Color color, float worldRadius, int sortingOrder, float durationSeconds = 0.55f)
        {
            var flashObject = new GameObject("Glow Flash");
            flashObject.transform.position = new Vector3(position.x, position.y, 0f);
            var fade = flashObject.AddComponent<GlowFlashFade>();
            fade.Initialize(color, worldRadius, sortingOrder, durationSeconds);
        }
    }

    internal sealed class GlowFlashFade : MonoBehaviour
    {
        private SpriteRenderer flashRenderer;
        private Color tint;
        private float duration = 0.55f;
        private float elapsed;
        private float scale;

        public void Initialize(Color color, float worldRadius, int sortingOrder, float durationSeconds)
        {
            tint = color;
            duration = Mathf.Max(0.05f, durationSeconds);
            scale = worldRadius * 2f / GlowSpriteFactory.HaloWorldDiameter;
            flashRenderer = gameObject.AddComponent<SpriteRenderer>();
            flashRenderer.sprite = GlowSpriteFactory.Halo;
            flashRenderer.sharedMaterial = PixelMaterialProvider.AdditiveMaterial;
            flashRenderer.sortingOrder = sortingOrder;
            transform.localScale = Vector3.one * scale * 0.6f;
            flashRenderer.color = new Color(tint.r, tint.g, tint.b, 0.85f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.one * scale * Mathf.Lerp(0.6f, 1.25f, progress);
            if (flashRenderer != null)
            {
                flashRenderer.color = new Color(tint.r, tint.g, tint.b, Mathf.Lerp(0.85f, 0f, progress * progress));
            }

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}

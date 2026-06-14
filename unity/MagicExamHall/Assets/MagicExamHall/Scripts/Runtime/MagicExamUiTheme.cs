using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MagicExamHall
{
    public enum MagicExamUiAnchor
    {
        Stretch,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }

    public enum MagicExamUiSpriteId
    {
        TitleLogo,
        BookPanel,
        ScrollPanel,
        DarkPanel,
        ButtonPanel,
        RuneCursor,
        NoteIcon,
        Checkbox,
        SliderTrack
    }

    public enum MagicExamButtonStyle
    {
        Primary,
        Secondary,
        Tab,
        Danger,
        Parchment
    }

    public static class MagicExamUiTheme
    {
        public static readonly Color DeepTower = new(0.018f, 0.022f, 0.032f, 0.92f);
        public static readonly Color DeepTowerSolid = new(0.018f, 0.022f, 0.032f, 1f);
        public static readonly Color TowerGlass = new(0.035f, 0.052f, 0.070f, 0.88f);
        public static readonly Color TowerGlassStrong = new(0.045f, 0.064f, 0.088f, 0.96f);
        public static readonly Color Parchment = new(0.86f, 0.68f, 0.43f, 0.98f);
        public static readonly Color ParchmentLight = new(0.96f, 0.78f, 0.48f, 0.98f);
        public static readonly Color ParchmentSoft = new(0.90f, 0.72f, 0.46f, 0.92f);
        public static readonly Color ParchmentInk = new(0.16f, 0.070f, 0.026f, 1f);
        public static readonly Color ParchmentMutedInk = new(0.27f, 0.145f, 0.060f, 0.92f);
        public static readonly Color Gold = new(1f, 0.82f, 0.38f, 1f);
        public static readonly Color GoldSoft = new(1f, 0.74f, 0.32f, 0.82f);
        public static readonly Color RuneBlue = new(0.48f, 0.84f, 1f, 1f);
        public static readonly Color RuneBlueDim = new(0.26f, 0.56f, 0.74f, 0.76f);
        public static readonly Color Wax = new(0.56f, 0.045f, 0.035f, 0.98f);
        public static readonly Color TextOnDark = new(0.93f, 0.97f, 1f, 1f);
        public static readonly Color TextOnDarkMuted = new(0.68f, 0.77f, 0.88f, 0.92f);
        public static readonly Color BorderBrown = new(0.33f, 0.17f, 0.060f, 0.96f);
        public static readonly Color BorderGold = new(0.96f, 0.68f, 0.28f, 0.86f);
        public static readonly Color DimOverlay = new(0.004f, 0.006f, 0.011f, 0.74f);
    }

    public static class MagicExamUiFactory
    {
        public static Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, MagicExamUiAnchor anchor, Color color)
        {
            var body = new GameObject(name);
            body.transform.SetParent(parent, false);
            var rect = body.AddComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            if (anchor != MagicExamUiAnchor.Stretch)
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }

            var image = body.AddComponent<Image>();
            image.color = color;
            image.material = PixelMaterialProvider.UiMaterial;
            return image;
        }

        public static RectTransform CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, MagicExamUiAnchor anchor, Color color)
        {
            return CreateImage(name, parent, anchoredPosition, size, anchor, color).rectTransform;
        }

        public static RectTransform CreateFramedPanel(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            MagicExamUiAnchor anchor,
            MagicExamUiSpriteId spriteId,
            Color tint,
            Color borderColor,
            float borderThickness = 2f)
        {
            var panel = CreatePanel(name, parent, anchoredPosition, size, anchor, tint);
            var image = panel.GetComponent<Image>();
            ApplySprite(image, spriteId, sliced: true);
            AddPixelBorder(panel, borderColor, borderThickness);
            return panel;
        }

        public static void ApplySprite(Image image, MagicExamUiSpriteId spriteId, bool sliced)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = MagicExamUiSprites.Get(spriteId);
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.material = PixelMaterialProvider.UiMaterial;
        }

        public static void AddPixelBorder(RectTransform target, Color color, float thickness)
        {
            if (target == null)
            {
                return;
            }

            var body = new GameObject($"{target.name} Border");
            body.transform.SetParent(target, false);
            var rect = body.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            body.AddComponent<CanvasRenderer>();
            var border = body.AddComponent<CustomShapeRectBorder>();
            border.color = color;
            border.thickness = thickness;
            border.raycastTarget = false;
        }

        public static void AddAccentRail(RectTransform target, Color color, float width = 5f)
        {
            if (target == null)
            {
                return;
            }

            var rail = CreateImage($"{target.name} Accent Rail", target, Vector2.zero, new Vector2(width, target.rect.height), MagicExamUiAnchor.TopLeft, color);
            var rect = rail.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, 0f);
            rail.raycastTarget = false;
        }

        public static void AddCornerCaps(RectTransform target, Color color, Vector2 size)
        {
            if (target == null)
            {
                return;
            }

            CreateImage($"{target.name} Left Cap", target, new Vector2(size.x * 0.5f, -size.y * 0.5f), size, MagicExamUiAnchor.TopLeft, color).raycastTarget = false;
            CreateImage($"{target.name} Right Cap", target, new Vector2(-size.x * 0.5f, -size.y * 0.5f), size, MagicExamUiAnchor.TopRight, color).raycastTarget = false;
        }

        public static void StyleButton(Button button, MagicExamButtonStyle style = MagicExamButtonStyle.Secondary)
        {
            if (button == null)
            {
                return;
            }

            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image != null)
            {
                var spriteId = style is MagicExamButtonStyle.Tab or MagicExamButtonStyle.Parchment
                    ? MagicExamUiSpriteId.ScrollPanel
                    : MagicExamUiSpriteId.DarkPanel;
                ApplySprite(image, spriteId, sliced: true);
                image.color = ButtonNormal(style);
            }

            var colors = button.colors;
            colors.normalColor = ButtonNormal(style);
            colors.highlightedColor = ButtonHighlight(style);
            colors.pressedColor = ButtonPressed(style);
            colors.selectedColor = ButtonHighlight(style);
            colors.disabledColor = new Color(0.16f, 0.14f, 0.12f, 0.58f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            foreach (var text in button.GetComponentsInChildren<Text>(true))
            {
                text.color = style is MagicExamButtonStyle.Parchment or MagicExamButtonStyle.Tab
                    ? MagicExamUiTheme.ParchmentInk
                    : MagicExamUiTheme.TextOnDark;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.raycastTarget = false;
                AddTextShadow(text, style is MagicExamButtonStyle.Parchment or MagicExamButtonStyle.Tab
                    ? new Color(1f, 0.82f, 0.48f, 0.20f)
                    : new Color(0f, 0f, 0f, 0.55f));
            }
        }

        public static void StyleDarkText(Text text, bool emphasized = false)
        {
            if (text == null)
            {
                return;
            }

            text.color = emphasized ? MagicExamUiTheme.Gold : MagicExamUiTheme.TextOnDark;
            text.lineSpacing = emphasized ? 1.03f : 1.08f;
            AddTextShadow(text, new Color(0f, 0f, 0f, emphasized ? 0.68f : 0.48f));
        }

        public static void StyleParchmentText(Text text, bool emphasized = false)
        {
            if (text == null)
            {
                return;
            }

            text.color = emphasized ? new Color(0.18f, 0.070f, 0.020f, 1f) : MagicExamUiTheme.ParchmentInk;
            text.lineSpacing = emphasized ? 1.03f : 1.08f;
            AddTextShadow(text, emphasized
                ? new Color(1f, 0.82f, 0.48f, 0.24f)
                : new Color(0.98f, 0.78f, 0.42f, 0.18f));
        }

        public static void AddTextShadow(Text text, Color color)
        {
            if (text == null)
            {
                return;
            }

            var shadow = text.gameObject.GetComponent<Shadow>() ?? text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;
        }

        public static void ApplyAnchor(RectTransform rect, MagicExamUiAnchor anchor)
        {
            switch (anchor)
            {
                case MagicExamUiAnchor.Stretch:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    break;
                case MagicExamUiAnchor.TopLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    break;
                case MagicExamUiAnchor.TopRight:
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 1f);
                    break;
                case MagicExamUiAnchor.BottomLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                    rect.pivot = new Vector2(0f, 0f);
                    break;
                case MagicExamUiAnchor.BottomRight:
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(1f, 0f);
                    break;
                case MagicExamUiAnchor.Center:
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
            }
        }

        private static Color ButtonNormal(MagicExamButtonStyle style)
        {
            return style switch
            {
                MagicExamButtonStyle.Primary => new Color(0.47f, 0.25f, 0.095f, 0.98f),
                MagicExamButtonStyle.Tab => new Color(0.76f, 0.56f, 0.30f, 0.98f),
                MagicExamButtonStyle.Danger => new Color(0.82f, 0.055f, 0.040f, 0.98f),
                MagicExamButtonStyle.Parchment => new Color(0.84f, 0.63f, 0.36f, 0.98f),
                _ => new Color(0.060f, 0.082f, 0.110f, 0.96f)
            };
        }

        private static Color ButtonHighlight(MagicExamButtonStyle style)
        {
            return style switch
            {
                MagicExamButtonStyle.Primary => new Color(0.62f, 0.34f, 0.13f, 1f),
                MagicExamButtonStyle.Tab => new Color(0.92f, 0.70f, 0.38f, 1f),
                MagicExamButtonStyle.Danger => new Color(0.96f, 0.11f, 0.075f, 1f),
                MagicExamButtonStyle.Parchment => new Color(0.94f, 0.72f, 0.42f, 1f),
                _ => new Color(0.090f, 0.128f, 0.172f, 1f)
            };
        }

        private static Color ButtonPressed(MagicExamButtonStyle style)
        {
            return style switch
            {
                MagicExamButtonStyle.Primary => new Color(0.32f, 0.16f, 0.055f, 1f),
                MagicExamButtonStyle.Tab => new Color(0.58f, 0.39f, 0.19f, 1f),
                MagicExamButtonStyle.Danger => new Color(0.66f, 0.030f, 0.025f, 1f),
                MagicExamButtonStyle.Parchment => new Color(0.68f, 0.48f, 0.25f, 1f),
                _ => new Color(0.030f, 0.045f, 0.065f, 1f)
            };
        }
    }

    public static class MagicExamUiSprites
    {
        private const string ResourceRoot = "Sprites/UI/";
        private static readonly Dictionary<MagicExamUiSpriteId, Sprite> Cache = new();

        public static Sprite Get(MagicExamUiSpriteId spriteId)
        {
            if (Cache.TryGetValue(spriteId, out var cached) && cached != null)
            {
                return cached;
            }

            var loaded = Resources.Load<Sprite>(ResourceRoot + spriteId);
            if (loaded == null)
            {
                var texture = Resources.Load<Texture2D>(ResourceRoot + spriteId);
                loaded = texture == null ? CreateFallback(spriteId) : CreateSprite(texture, spriteId.ToString(), BorderFor(spriteId));
            }

            Cache[spriteId] = loaded;
            return loaded;
        }

        public static void Reset()
        {
            Cache.Clear();
        }

        private static Sprite CreateFallback(MagicExamUiSpriteId spriteId)
        {
            var size = spriteId == MagicExamUiSpriteId.TitleLogo ? new Vector2Int(256, 96) : new Vector2Int(64, 64);
            var texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false)
            {
                name = $"Generated {spriteId}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Clear(texture);
            switch (spriteId)
            {
                case MagicExamUiSpriteId.TitleLogo:
                    DrawTitleLogo(texture);
                    break;
                case MagicExamUiSpriteId.BookPanel:
                    DrawPanel(texture, new Color32(185, 124, 62, 255), new Color32(246, 193, 104, 255), new Color32(68, 32, 12, 255), book: true);
                    break;
                case MagicExamUiSpriteId.ScrollPanel:
                    DrawPanel(texture, new Color32(214, 155, 82, 255), new Color32(248, 202, 124, 255), new Color32(82, 38, 12, 255), book: false);
                    break;
                case MagicExamUiSpriteId.ButtonPanel:
                    DrawPanel(texture, new Color32(116, 67, 30, 255), new Color32(202, 139, 62, 255), new Color32(38, 24, 18, 255), book: false);
                    break;
                case MagicExamUiSpriteId.RuneCursor:
                    DrawRuneCursor(texture);
                    break;
                case MagicExamUiSpriteId.NoteIcon:
                    DrawNoteIcon(texture);
                    break;
                case MagicExamUiSpriteId.Checkbox:
                    DrawCheckbox(texture);
                    break;
                case MagicExamUiSpriteId.SliderTrack:
                    DrawSliderTrack(texture);
                    break;
                default:
                    DrawPanel(texture, new Color32(12, 20, 30, 255), new Color32(34, 54, 72, 255), new Color32(1, 2, 4, 255), book: false);
                    break;
            }

            texture.Apply(false, true);
            return CreateSprite(texture, spriteId.ToString(), BorderFor(spriteId));
        }

        private static Sprite CreateSprite(Texture2D texture, string name, Vector4 border)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                border);
            sprite.name = name;
            return sprite;
        }

        private static Vector4 BorderFor(MagicExamUiSpriteId spriteId)
        {
            return spriteId switch
            {
                MagicExamUiSpriteId.TitleLogo or MagicExamUiSpriteId.NoteIcon or MagicExamUiSpriteId.RuneCursor => Vector4.zero,
                MagicExamUiSpriteId.SliderTrack => new Vector4(8f, 8f, 8f, 8f),
                _ => new Vector4(12f, 12f, 12f, 12f)
            };
        }

        private static void DrawPanel(Texture2D texture, Color32 dark, Color32 light, Color32 outline, bool book)
        {
            Fill(texture, 0, 0, texture.width, texture.height, dark);
            Fill(texture, 4, 4, texture.width - 8, texture.height - 8, light);
            Fill(texture, 8, 8, texture.width - 16, texture.height - 16, new Color32((byte)Mathf.Min(255, light.r + 18), (byte)Mathf.Min(255, light.g + 16), (byte)Mathf.Min(255, light.b + 10), light.a));
            Border(texture, 0, 0, texture.width, texture.height, outline, 3);
            Border(texture, 5, 5, texture.width - 10, texture.height - 10, new Color32(255, 219, 136, 180), 1);
            if (book)
            {
                Fill(texture, 9, 9, 8, 3, new Color32(115, 61, 24, 115));
                Fill(texture, texture.width - 17, texture.height - 12, 8, 3, new Color32(115, 61, 24, 115));
            }
        }

        private static void DrawTitleLogo(Texture2D texture)
        {
            Fill(texture, 0, 0, texture.width, texture.height, new Color32(0, 0, 0, 0));
            Fill(texture, 8, 46, 84, 3, new Color32(244, 190, 82, 175));
            Fill(texture, 164, 46, 84, 3, new Color32(244, 190, 82, 175));
            Fill(texture, 20, 40, 58, 2, new Color32(105, 201, 245, 110));
            Fill(texture, 178, 40, 58, 2, new Color32(105, 201, 245, 110));
            Fill(texture, 113, 28, 30, 38, new Color32(28, 43, 64, 220));
            Fill(texture, 108, 63, 40, 6, new Color32(116, 65, 26, 230));
            Fill(texture, 119, 18, 18, 10, new Color32(54, 35, 68, 230));
            Fill(texture, 123, 35, 10, 5, new Color32(105, 201, 245, 200));
            Fill(texture, 123, 48, 10, 5, new Color32(105, 201, 245, 200));
            Border(texture, 113, 28, 30, 38, new Color32(84, 45, 14, 220), 2);
        }

        private static void DrawRuneCursor(Texture2D texture)
        {
            var center = new Vector2(texture.width * 0.5f, texture.height * 0.5f);
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    if (Mathf.Abs(distance - 22f) < 1.8f || Mathf.Abs(distance - 12f) < 1.4f)
                    {
                        texture.SetPixel(x, y, new Color32(92, 202, 255, 220));
                    }
                }
            }
            Fill(texture, 31, 9, 2, 46, new Color32(255, 218, 116, 160));
            Fill(texture, 9, 31, 46, 2, new Color32(255, 218, 116, 160));
        }

        private static void DrawNoteIcon(Texture2D texture)
        {
            Fill(texture, 15, 10, 31, 42, new Color32(236, 190, 112, 255));
            Fill(texture, 18, 13, 25, 36, new Color32(255, 224, 150, 255));
            Fill(texture, 22, 18, 16, 2, new Color32(82, 38, 12, 210));
            Fill(texture, 22, 25, 18, 2, new Color32(82, 38, 12, 160));
            Fill(texture, 22, 32, 13, 2, new Color32(82, 38, 12, 130));
            Fill(texture, 12, 12, 6, 38, new Color32(91, 45, 20, 255));
            Border(texture, 15, 10, 31, 42, new Color32(52, 26, 11, 255), 2);
        }

        private static void DrawCheckbox(Texture2D texture)
        {
            Fill(texture, 8, 8, 48, 48, new Color32(250, 210, 132, 230));
            Border(texture, 8, 8, 48, 48, new Color32(70, 30, 8, 255), 4);
            DrawLine(texture, 20, 33, 29, 43, new Color32(180, 24, 18, 255), 4);
            DrawLine(texture, 29, 43, 46, 19, new Color32(180, 24, 18, 255), 4);
        }

        private static void DrawSliderTrack(Texture2D texture)
        {
            Fill(texture, 0, 20, 64, 24, new Color32(35, 27, 20, 230));
            Fill(texture, 5, 25, 54, 14, new Color32(117, 170, 198, 255));
            Border(texture, 0, 20, 64, 24, new Color32(74, 36, 12, 255), 2);
        }

        private static void Clear(Texture2D texture)
        {
            Fill(texture, 0, 0, texture.width, texture.height, new Color32(0, 0, 0, 0));
        }

        private static void Fill(Texture2D texture, int x, int y, int width, int height, Color32 color)
        {
            var xMin = Mathf.Clamp(x, 0, texture.width);
            var yMin = Mathf.Clamp(y, 0, texture.height);
            var xMax = Mathf.Clamp(x + width, 0, texture.width);
            var yMax = Mathf.Clamp(y + height, 0, texture.height);
            for (var py = yMin; py < yMax; py++)
            {
                for (var px = xMin; px < xMax; px++)
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }

        private static void Border(Texture2D texture, int x, int y, int width, int height, Color32 color, int thickness)
        {
            Fill(texture, x, y, width, thickness, color);
            Fill(texture, x, y + height - thickness, width, thickness, color);
            Fill(texture, x, y, thickness, height, color);
            Fill(texture, x + width - thickness, y, thickness, height, color);
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color32 color, int radius)
        {
            var dx = Mathf.Abs(x1 - x0);
            var sx = x0 < x1 ? 1 : -1;
            var dy = -Mathf.Abs(y1 - y0);
            var sy = y0 < y1 ? 1 : -1;
            var error = dx + dy;
            while (true)
            {
                Fill(texture, x0 - radius / 2, y0 - radius / 2, radius, radius, color);
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
    }
}

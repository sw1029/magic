using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MagicExamHall
{
    public enum MentorMood
    {
        Neutral,
        Happy,
        Frown
    }

    public sealed class MentorPresentationController : MonoBehaviour
    {
        private const float ReactionDurationSeconds = 0.55f;
        private const float SpeechPanelWidth = 340f;
        private const float SpeechBodyWidth = 332f;
        private const float SpeechMinPanelHeight = 96f;
        private const float SpeechMaxPanelHeight = 126f;
        private const float SpeechBodyBaseHeight = 84f;
        private const float SpeechBodyVerticalInset = 12f;
        private const float SpeechTextWidth = 298f;
        private const float SpeechTextMinHeight = 44f;
        private const float SpeechTextMaxHeight = 70f;
        private const float SpeechTextTop = 30f;
        private const float SpeechTextBottomPadding = 14f;
        private const int MaxSpeechLineLength = 20;

        private PixelSpriteView spriteView = null!;
        private SpriteRenderer spriteRenderer = null!;
        private Canvas speechCanvas = null!;
        private RectTransform canvasRect = null!;
        private RectTransform speechPanel = null!;
        private RectTransform speechBody = null!;
        private Text speakerText = null!;
        private Text bodyText = null!;
        private MentorProfile profile;
        private Vector3 homePosition;
        private float reactionStartedAt = -1f;

        public string CurrentMentorName => profile.name;
        public MentorMood CurrentMood { get; private set; }
        public string SpeechText => bodyText == null ? "" : bodyText.text;
        public bool IsVisible => spriteView != null && spriteView.gameObject.activeSelf && speechPanel != null && speechPanel.gameObject.activeSelf;

        public void Initialize(Canvas canvas, Font font)
        {
            profile = MentorProfile.ForFloor(1);
            speechCanvas = canvas;
            canvasRect = canvas.transform as RectTransform;
            EnsureWorldSprite();
            EnsureSpeechPanel(canvas, font);
            ConfigureFloor(1);
        }

        public void ConfigureFloor(int floorNumber)
        {
            profile = MentorProfile.ForFloor(floorNumber);
            if (speakerText != null)
            {
                speakerText.text = profile.name;
                speakerText.color = new Color(0.15f, 0.22f, 0.24f);
            }

            if (spriteView != null)
            {
                spriteView.primary = profile.skin;
                spriteView.secondary = profile.robe;
                spriteView.transform.position = profile.worldPosition;
                spriteView.transform.localScale = Vector3.one * profile.worldScale;
                homePosition = spriteView.transform.position;
                SetMood(MentorMood.Neutral);
                UpdateSpeechPanelPosition();
            }
        }

        public void Say(MentorMood mood, string text)
        {
            if (speechPanel == null || bodyText == null)
            {
                return;
            }

            speechPanel.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
            bodyText.text = CompactSpeechText(text);
            SetMood(mood);
            UpdateSpeechTextLayout();
            UpdateSpeechPanelPosition();
        }

        public void HideSpeech()
        {
            if (speechPanel == null)
            {
                return;
            }

            speechPanel.gameObject.SetActive(false);
        }

        public void Tick(float time)
        {
            if (spriteView == null || reactionStartedAt < 0f)
            {
                return;
            }

            var progress = Mathf.Clamp01((time - reactionStartedAt) / ReactionDurationSeconds);
            var wave = Mathf.Sin(progress * Mathf.PI);
            var offset = CurrentMood switch
            {
                MentorMood.Happy => new Vector3(0f, wave * 0.10f, 0f),
                MentorMood.Frown => new Vector3(-wave * 0.08f, 0f, 0f),
                _ => Vector3.zero
            };
            spriteView.transform.position = homePosition + offset;
            UpdateSpeechPanelPosition();

            if (progress >= 1f)
            {
                reactionStartedAt = -1f;
                spriteView.transform.position = homePosition;
                if (CurrentMood != MentorMood.Neutral)
                {
                    SetMood(MentorMood.Neutral, animate: false);
                }
            }
        }

        private void EnsureWorldSprite()
        {
            if (spriteView != null)
            {
                return;
            }

            var mentorObject = new GameObject("Floor Mentor");
            mentorObject.transform.SetParent(transform, false);
            spriteRenderer = mentorObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 31;
            spriteView = mentorObject.AddComponent<PixelSpriteView>();
            spriteView.enabled = false;
            spriteView.sortingOrder = 31;
        }

        private void EnsureSpeechPanel(Canvas canvas, Font font)
        {
            if (speechPanel != null)
            {
                return;
            }

            var bubbleColor = new Color(0.97f, 0.99f, 0.98f, 0.98f);
            var borderColor = new Color(0.45f, 0.62f, 0.63f, 0.86f);
            speechPanel = CreateRect("Mentor Speech", canvas.transform, new Vector2(92f, 52f), new Vector2(SpeechPanelWidth, SpeechMinPanelHeight), Anchor.BottomLeft);
            var tail = CreatePanel("Mentor Speech Tail", speechPanel, new Vector2(SpeechBodyWidth - 42f, -2f), new Vector2(24f, 24f), Anchor.BottomLeft, bubbleColor);
            tail.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddSimpleBorder(tail, borderColor, 1.4f);
            tail.SetAsFirstSibling();
            speechBody = CreatePanel("Mentor Speech Body", speechPanel, new Vector2(0f, 8f), new Vector2(SpeechBodyWidth, SpeechBodyBaseHeight), Anchor.BottomLeft, bubbleColor);
            var mask = speechBody.gameObject.AddComponent<RectMask2D>();
            mask.padding = new Vector4(6f, 14f, 6f, 6f);
            ApplySpeechBubbleBody(speechBody, borderColor);
            speakerText = CreateText("Mentor Speaker", speechBody, profile.name, 12, FontStyle.Bold, new Vector2(14, -9), new Vector2(SpeechTextWidth, 18), Anchor.TopLeft, font);
            bodyText = CreateText("Mentor Speech Text", speechBody, "", 15, FontStyle.Normal, new Vector2(14, -SpeechTextTop), new Vector2(SpeechTextWidth, SpeechTextMinHeight), Anchor.TopLeft, font);
            speakerText.color = new Color(0.15f, 0.22f, 0.24f);
            bodyText.color = new Color(0.10f, 0.12f, 0.13f);
            bodyText.lineSpacing = 1.0f;
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 13;
            bodyText.resizeTextMaxSize = 15;
            speechPanel.gameObject.SetActive(false);
        }

        private void SetMood(MentorMood mood, bool animate = true)
        {
            CurrentMood = mood;
            if (MentorSpriteLibrary.TryGetSprite(profile.spriteSetKey, mood, out var mentorSprite))
            {
                spriteView.enabled = false;
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = mentorSprite;
                spriteRenderer.sharedMaterial = PixelMaterialProvider.SpriteMaterial;
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingOrder = 31;
                spriteRenderer.drawMode = SpriteDrawMode.Simple;
            }
            else
            {
                spriteRenderer.enabled = true;
                spriteView.enabled = true;
                spriteView.kind = profile.KindFor(mood);
                spriteView.Apply();
            }

            if (animate && mood != MentorMood.Neutral)
            {
                reactionStartedAt = Time.time;
            }

            UpdateSpeechPanelPosition();
        }

        private void UpdateSpeechPanelPosition()
        {
            if (speechPanel == null || canvasRect == null || spriteView == null)
            {
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            var worldAnchor = GetSpeechAnchorWorld();
            var screenPoint = RectTransformUtility.WorldToScreenPoint(mainCamera, worldAnchor);
            var canvasCamera = speechCanvas != null && speechCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? speechCanvas.worldCamera ?? mainCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvasCamera, out var localPoint))
            {
                return;
            }

            var canvasSize = canvasRect.rect.size;
            var anchored = localPoint + Vector2.Scale(canvasSize, canvasRect.pivot);
            var desired = anchored + new Vector2(-SpeechPanelWidth + 36f, 24f);
            var size = speechPanel.sizeDelta;
            desired.x = Mathf.Clamp(desired.x, 16f, Mathf.Max(16f, canvasSize.x - size.x - 16f));
            desired.y = Mathf.Clamp(desired.y, 16f, Mathf.Max(16f, canvasSize.y - size.y - 16f));
            speechPanel.anchoredPosition = desired;
        }

        private static string CompactSpeechText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            var contextual = BuildContextualSpeech(text);
            if (!string.IsNullOrWhiteSpace(contextual))
            {
                return contextual;
            }

            var rawLines = text
                .Replace("\r", "\n")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (rawLines.Length == 0)
            {
                return "";
            }

            var first = ShortenSpeechLine(CleanSpeechLine(rawLines[0]));
            if (rawLines.Length == 1)
            {
                return first;
            }

            var secondIndex = 1;
            for (var index = 1; index < rawLines.Length; index++)
            {
                var candidate = CleanSpeechLine(rawLines[index]);
                if (ContainsActionHint(candidate))
                {
                    secondIndex = index;
                    break;
                }
            }

            var second = ShortenSpeechLine(CleanSpeechLine(rawLines[secondIndex]));
            return string.IsNullOrWhiteSpace(second) ? first : $"{first}\n{second}";
        }

        private static string BuildContextualSpeech(string text)
        {
            var normalized = text.Replace("\r", "\n").Replace("\n", " ").Trim();
            if (ContainsAny(normalized, "gold capture", "레퍼런스", "책장"))
            {
                return "책장에 도형 샘플이 있어.\n보기 눌러 슬롯에 담아 봐.";
            }

            if (ContainsAny(normalized, "처음에는 물", "물 도형을 먼저", "바닥에 직접 그린 선"))
            {
                return "바닥에 그린 선이 문양이 돼.\n먼저 물만 따라 그려 봐.";
            }

            if (ContainsAny(normalized, "빛나는 원", "기본 문양 위"))
            {
                return "기본 문양을 먼저 만들고,\n그 안에 도형을 얹어 봐.";
            }

            if (ContainsAny(normalized, "보호막", "안정화"))
            {
                var baseFamilyLabel = ExtractBaseFamilyLabel(normalized);
                return string.IsNullOrWhiteSpace(baseFamilyLabel)
                    ? "기초 속성 마법진을 그렸어.\n보호막으로 굳었어."
                    : $"{baseFamilyLabel} 기초 속성 마법진을 그렸어.\n보호막으로 굳었어.";
            }

            return "";
        }

        private static bool ContainsAny(string text, params string[] candidates)
        {
            return candidates.Any(candidate => text.Contains(candidate, StringComparison.Ordinal));
        }

        private static string ExtractBaseFamilyLabel(string text)
        {
            var sealIndex = text.IndexOf("seal", StringComparison.Ordinal);
            var markerIndex = sealIndex >= 0
                ? sealIndex
                : text.IndexOf("기초 속성 마법진", StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return "";
            }

            var prefix = text[..markerIndex].Trim().TrimEnd(',');
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return "";
            }

            var parts = prefix.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? "" : parts[^1].Trim().TrimEnd(',');
        }

        private static string CleanSpeechLine(string line)
        {
            var trimmed = line.Trim();
            var prefixes = new[] { "노트:", "다음:", "1층 목표:", "2층 목표:", "3층 목표:", "4층 목표:", "5층 목표:" };
            foreach (var prefix in prefixes)
            {
                if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return trimmed[prefix.Length..].Trim();
                }
            }

            return trimmed;
        }

        private static bool ContainsActionHint(string line)
        {
            return line.Contains("그려") ||
                line.Contains("눌") ||
                line.Contains("시작") ||
                line.Contains("다시") ||
                line.Contains("다음") ||
                line.Contains("표식") ||
                line.Contains("seal") ||
                line.Contains("base");
        }

        private static string ShortenSpeechLine(string line)
        {
            var trimmed = line.Trim();
            return trimmed.Length <= MaxSpeechLineLength
                ? trimmed
                : trimmed[..(MaxSpeechLineLength - 3)] + "...";
        }

        private void UpdateSpeechTextLayout()
        {
            if (speechPanel == null || speechBody == null || bodyText == null)
            {
                return;
            }

            var bodyRect = bodyText.rectTransform;
            bodyRect.sizeDelta = new Vector2(SpeechTextWidth, SpeechTextMinHeight);
            Canvas.ForceUpdateCanvases();

            var preferredTextHeight = Mathf.Ceil(bodyText.preferredHeight) + 4f;
            var textHeight = Mathf.Clamp(preferredTextHeight, SpeechTextMinHeight, SpeechTextMaxHeight);
            var bodyHeight = Mathf.Clamp(
                SpeechTextTop + textHeight + SpeechTextBottomPadding,
                SpeechBodyBaseHeight,
                SpeechMaxPanelHeight - SpeechBodyVerticalInset);
            var panelHeight = Mathf.Clamp(bodyHeight + SpeechBodyVerticalInset, SpeechMinPanelHeight, SpeechMaxPanelHeight);

            speechPanel.sizeDelta = new Vector2(SpeechPanelWidth, panelHeight);
            speechBody.sizeDelta = new Vector2(SpeechBodyWidth, bodyHeight);
            bodyRect.sizeDelta = new Vector2(SpeechTextWidth, Mathf.Max(SpeechTextMinHeight, bodyHeight - SpeechTextTop - SpeechTextBottomPadding));
        }

        private Vector3 GetSpeechAnchorWorld()
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                var bounds = spriteRenderer.bounds;
                return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            }

            return spriteView.transform.position + new Vector3(0f, 0.95f, 0f);
        }

        private static void ApplySpeechBubbleBody(RectTransform body, Color fallbackBorderColor)
        {
            var image = body.GetComponent<Image>();
            var sprite = MentorSpeechBubbleSkin.BodySprite;
            if (sprite == null)
            {
                AddSimpleBorder(body, fallbackBorderColor, 2f);
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.material = PixelMaterialProvider.UiMaterial;
            image.raycastTarget = false;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Anchor anchor, Color color)
        {
            var rect = CreateRect(name, parent, anchoredPosition, size, anchor);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Anchor anchor)
        {
            var panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            var rect = panelObject.AddComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static void AddSimpleBorder(RectTransform target, Color color, float thickness)
        {
            var borderObject = new GameObject($"{target.name} Border");
            borderObject.transform.SetParent(target, false);
            var rect = borderObject.AddComponent<RectTransform>();
            ApplyAnchor(rect, Anchor.Stretch);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var border = borderObject.AddComponent<CustomShapeRectBorder>();
            border.color = color;
            border.thickness = thickness;
            border.material = PixelMaterialProvider.UiMaterial;
            border.raycastTarget = false;
            borderObject.transform.SetAsLastSibling();
        }

        private static Text CreateText(string name, Transform parent, string content, int size, FontStyle style, Vector2 anchoredPosition, Vector2 rectSize, Anchor anchor, Font font)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = rectSize;
            var text = textObject.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = new Color(0.10f, 0.12f, 0.13f);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void ApplyAnchor(RectTransform rect, Anchor anchor)
        {
            switch (anchor)
            {
                case Anchor.BottomRight:
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(1f, 0f);
                    break;
                case Anchor.BottomLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                    rect.pivot = new Vector2(0f, 0f);
                    break;
                case Anchor.TopLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    break;
                case Anchor.Stretch:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
            }
        }

        private enum Anchor
        {
            TopLeft,
            BottomLeft,
            BottomRight,
            Stretch
        }
    }

    public readonly struct MentorProfile
    {
        public readonly string name;
        public readonly string spriteSetKey;
        public readonly Color skin;
        public readonly Color robe;
        public readonly PixelSpriteKind neutralKind;
        public readonly PixelSpriteKind happyKind;
        public readonly PixelSpriteKind frownKind;
        public readonly Vector3 worldPosition;
        public readonly float worldScale;

        private MentorProfile(
            string name,
            string spriteSetKey,
            Color skin,
            Color robe,
            PixelSpriteKind neutralKind,
            PixelSpriteKind happyKind,
            PixelSpriteKind frownKind,
            Vector3 worldPosition,
            float worldScale)
        {
            this.name = name;
            this.spriteSetKey = spriteSetKey;
            this.skin = skin;
            this.robe = robe;
            this.neutralKind = neutralKind;
            this.happyKind = happyKind;
            this.frownKind = frownKind;
            this.worldPosition = worldPosition;
            this.worldScale = worldScale;
        }

        public PixelSpriteKind KindFor(MentorMood mood)
        {
            return mood switch
            {
                MentorMood.Happy => happyKind,
                MentorMood.Frown => frownKind,
                _ => neutralKind
            };
        }

        public static MentorProfile ForFloor(int floor)
        {
            return floor switch
            {
                1 => new MentorProfile("입문 조교", "Floor1_TutorialMentor", new Color(0.95f, 0.84f, 0.70f), new Color(0.52f, 0.32f, 0.86f), PixelSpriteKind.MentorNeutral, PixelSpriteKind.MentorHappy, PixelSpriteKind.MentorFrown, new Vector3(-7.05f, -3.72f, 0f), 0.88f),
                2 => new MentorProfile("벽화 연구원", "Floor2_MuralResearcher", new Color(0.92f, 0.82f, 0.66f), new Color(0.18f, 0.62f, 0.72f), PixelSpriteKind.MentorScholarNeutral, PixelSpriteKind.MentorScholarHappy, PixelSpriteKind.MentorScholarFrown, new Vector3(-7.05f, -3.72f, 0f), 0.90f),
                3 => new MentorProfile("다리 안내원", "Floor3_BridgeGuide", new Color(0.96f, 0.86f, 0.68f), new Color(0.42f, 0.70f, 0.36f), PixelSpriteKind.MentorGuideNeutral, PixelSpriteKind.MentorGuideHappy, PixelSpriteKind.MentorGuideFrown, new Vector3(-7.05f, -3.72f, 0f), 0.90f),
                4 => new MentorProfile("균열 감시자", "Floor4_RiftWatcher", new Color(0.94f, 0.78f, 0.66f), new Color(0.70f, 0.24f, 0.28f), PixelSpriteKind.MentorWatcherNeutral, PixelSpriteKind.MentorWatcherHappy, PixelSpriteKind.MentorWatcherFrown, new Vector3(-7.05f, -3.72f, 0f), 0.88f),
                5 => new MentorProfile("성좌 기록관", "Floor5_StarArchivist", new Color(0.92f, 0.86f, 0.76f), new Color(0.25f, 0.36f, 0.78f), PixelSpriteKind.MentorArchivistNeutral, PixelSpriteKind.MentorArchivistHappy, PixelSpriteKind.MentorArchivistFrown, new Vector3(-7.05f, -3.72f, 0f), 0.90f),
                _ => ForFloor(1)
            };
        }
    }

    internal static class MentorSpeechBubbleSkin
    {
        private const string BodyPath = "Ui/MentorSpeechBubble";
        private static Sprite bodySprite;

        public static Sprite BodySprite
        {
            get
            {
                if (bodySprite != null)
                {
                    return bodySprite;
                }

                var texture = Resources.Load<Texture2D>(BodyPath);
                if (texture == null)
                {
                    return null;
                }

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                bodySprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(62f, 74f, 62f, 62f));
                bodySprite.name = "MentorSpeechBubble";
                return bodySprite;
            }
        }
    }
}

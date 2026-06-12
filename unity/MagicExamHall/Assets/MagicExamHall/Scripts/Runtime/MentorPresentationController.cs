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

        private PixelSpriteView spriteView = null!;
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
                speakerText.color = Color.Lerp(profile.robe, Color.white, 0.18f);
            }

            if (spriteView != null)
            {
                spriteView.primary = profile.skin;
                spriteView.secondary = profile.robe;
                spriteView.transform.position = profile.worldPosition;
                spriteView.transform.localScale = Vector3.one * profile.worldScale;
                homePosition = spriteView.transform.position;
                SetMood(MentorMood.Neutral);
            }
        }

        public void Say(MentorMood mood, string text)
        {
            if (speechPanel == null || bodyText == null)
            {
                return;
            }

            speechPanel.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
            bodyText.text = text;
            SetMood(mood);
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
            mentorObject.AddComponent<SpriteRenderer>();
            spriteView = mentorObject.AddComponent<PixelSpriteView>();
            spriteView.sortingOrder = 31;
        }

        private void EnsureSpeechPanel(Canvas canvas, Font font)
        {
            if (speechPanel != null)
            {
                return;
            }

            var bubbleColor = new Color(0.035f, 0.045f, 0.062f, 0.92f);
            var borderColor = new Color(0.30f, 0.50f, 0.72f, 0.78f);
            speechPanel = CreateRect("Mentor Speech", canvas.transform, new Vector2(92f, 52f), new Vector2(390f, 104f), Anchor.BottomLeft);
            var tail = CreatePanel("Mentor Speech Tail", speechPanel, new Vector2(22f, -4f), new Vector2(26f, 26f), Anchor.BottomLeft, bubbleColor);
            tail.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.SetAsFirstSibling();
            speechBody = CreatePanel("Mentor Speech Body", speechPanel, new Vector2(0f, 8f), new Vector2(382f, 92f), Anchor.BottomLeft, bubbleColor);
            AddSimpleBorder(speechBody, borderColor, 2f);
            speakerText = CreateText("Mentor Speaker", speechBody, profile.name, 12, FontStyle.Bold, new Vector2(14, -9), new Vector2(348, 18), Anchor.TopLeft, font);
            bodyText = CreateText("Mentor Speech Text", speechBody, "", 13, FontStyle.Normal, new Vector2(14, -30), new Vector2(348, 54), Anchor.TopLeft, font);
            speechPanel.gameObject.SetActive(false);
        }

        private void SetMood(MentorMood mood, bool animate = true)
        {
            CurrentMood = mood;
            spriteView.kind = profile.KindFor(mood);
            spriteView.Apply();
            if (animate && mood != MentorMood.Neutral)
            {
                reactionStartedAt = Time.time;
            }
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
            text.color = new Color(0.92f, 0.95f, 1f);
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
        public readonly Color skin;
        public readonly Color robe;
        public readonly PixelSpriteKind neutralKind;
        public readonly PixelSpriteKind happyKind;
        public readonly PixelSpriteKind frownKind;
        public readonly Vector3 worldPosition;
        public readonly float worldScale;

        private MentorProfile(
            string name,
            Color skin,
            Color robe,
            PixelSpriteKind neutralKind,
            PixelSpriteKind happyKind,
            PixelSpriteKind frownKind,
            Vector3 worldPosition,
            float worldScale)
        {
            this.name = name;
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
                1 => new MentorProfile("입문 조교", new Color(0.95f, 0.84f, 0.70f), new Color(0.52f, 0.32f, 0.86f), PixelSpriteKind.MentorNeutral, PixelSpriteKind.MentorHappy, PixelSpriteKind.MentorFrown, new Vector3(-7.05f, -3.72f, 0f), 0.88f),
                2 => new MentorProfile("벽화 연구원", new Color(0.92f, 0.82f, 0.66f), new Color(0.18f, 0.62f, 0.72f), PixelSpriteKind.MentorScholarNeutral, PixelSpriteKind.MentorScholarHappy, PixelSpriteKind.MentorScholarFrown, new Vector3(-7.05f, -3.72f, 0f), 0.90f),
                3 => new MentorProfile("다리 안내원", new Color(0.96f, 0.86f, 0.68f), new Color(0.42f, 0.70f, 0.36f), PixelSpriteKind.MentorGuideNeutral, PixelSpriteKind.MentorGuideHappy, PixelSpriteKind.MentorGuideFrown, new Vector3(-7.05f, -3.72f, 0f), 0.90f),
                4 => new MentorProfile("균열 감시자", new Color(0.94f, 0.78f, 0.66f), new Color(0.70f, 0.24f, 0.28f), PixelSpriteKind.MentorWatcherNeutral, PixelSpriteKind.MentorWatcherHappy, PixelSpriteKind.MentorWatcherFrown, new Vector3(-7.05f, -3.72f, 0f), 0.88f),
                5 => new MentorProfile("성좌 기록관", new Color(0.92f, 0.86f, 0.76f), new Color(0.25f, 0.36f, 0.78f), PixelSpriteKind.MentorArchivistNeutral, PixelSpriteKind.MentorArchivistHappy, PixelSpriteKind.MentorArchivistFrown, new Vector3(-7.05f, -3.72f, 0f), 0.90f),
                _ => ForFloor(1)
            };
        }
    }
}

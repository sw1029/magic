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

            speechPanel = CreatePanel("Mentor Speech", canvas.transform, new Vector2(-20, 20), new Vector2(440, 112), Anchor.BottomRight, new Color(0.035f, 0.045f, 0.062f, 0.90f));
            speakerText = CreateText("Mentor Speaker", speechPanel, profile.name, 13, FontStyle.Bold, new Vector2(14, -10), new Vector2(404, 20), Anchor.TopLeft, font);
            bodyText = CreateText("Mentor Speech Text", speechPanel, "", 13, FontStyle.Normal, new Vector2(14, -34), new Vector2(404, 66), Anchor.TopLeft, font);
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
            var panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            var rect = panelObject.AddComponent<RectTransform>();
            ApplyAnchor(rect, anchor);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = panelObject.AddComponent<Image>();
            image.color = color;
            return rect;
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
                case Anchor.TopLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    break;
            }
        }

        private enum Anchor
        {
            TopLeft,
            BottomRight
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
                1 => new MentorProfile("발착층 조교", new Color(0.95f, 0.84f, 0.70f), new Color(0.52f, 0.32f, 0.86f), PixelSpriteKind.MentorNeutral, PixelSpriteKind.MentorHappy, PixelSpriteKind.MentorFrown, new Vector3(-7.05f, -3.72f, 0f), 0.88f),
                2 => new MentorProfile("벽화 연구원", new Color(0.92f, 0.82f, 0.66f), new Color(0.18f, 0.62f, 0.72f), PixelSpriteKind.MentorScholarNeutral, PixelSpriteKind.MentorScholarHappy, PixelSpriteKind.MentorScholarFrown, new Vector3(-7.05f, -3.72f, 0f), 0.90f),
                3 => new MentorProfile("다리 안내원", new Color(0.96f, 0.86f, 0.68f), new Color(0.42f, 0.70f, 0.36f), PixelSpriteKind.MentorGuideNeutral, PixelSpriteKind.MentorGuideHappy, PixelSpriteKind.MentorGuideFrown, new Vector3(-7.05f, -3.72f, 0f), 0.90f),
                4 => new MentorProfile("균열 감시자", new Color(0.94f, 0.78f, 0.66f), new Color(0.70f, 0.24f, 0.28f), PixelSpriteKind.MentorWatcherNeutral, PixelSpriteKind.MentorWatcherHappy, PixelSpriteKind.MentorWatcherFrown, new Vector3(-7.05f, -3.72f, 0f), 0.88f),
                5 => new MentorProfile("성좌 기록관", new Color(0.92f, 0.86f, 0.76f), new Color(0.25f, 0.36f, 0.78f), PixelSpriteKind.MentorArchivistNeutral, PixelSpriteKind.MentorArchivistHappy, PixelSpriteKind.MentorArchivistFrown, new Vector3(-7.05f, -3.72f, 0f), 0.90f),
                _ => ForFloor(1)
            };
        }
    }
}

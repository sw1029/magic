using System;
using System.Collections.Generic;

namespace MagicExamHall
{
    public enum AssistLevel
    {
        None = 0,
        ReasonHint = 1,
        Checklist = 2,
        GhostTrace = 3
    }

    [Serializable]
    public sealed class HintState
    {
        public SpellFamily family;
        public int failureCount;
        public AssistLevel currentLevel;
        public bool hintShown;
        public bool assisted;
        public string title = "";
        public string body = "";

        public int AssistLevelNumber => (int)currentLevel;
    }

    public static class HintAssistance
    {
        public static HintState PreviewFor(SpellFamily family, int priorFailures, SpellResult result = null)
        {
            var level = (AssistLevel)Math.Min(Math.Max(priorFailures, 0), 3);
            return CreateState(family, priorFailures, level, priorFailures > 0, result);
        }

        public static HintState ForAttempt(SpellFamily family, int priorFailures, bool success, SpellResult result)
        {
            var level = ResolveLevel(priorFailures, success);
            return CreateState(family, priorFailures, level, priorFailures > 0, result);
        }

        public static AssistLevel ResolveLevel(int priorFailures, bool success)
        {
            if (success)
            {
                if (priorFailures <= 0)
                {
                    return AssistLevel.None;
                }

                return (AssistLevel)Math.Min(priorFailures, 3);
            }

            return (AssistLevel)Math.Min(Math.Max(priorFailures + 1, 1), 3);
        }

        public static IReadOnlyList<string> ChecklistFor(SpellFamily family)
        {
            return family switch
            {
                SpellFamily.Fire => new[] { "세 꼭짓점을 크게 찍기", "마지막 선을 처음 아래 꼭짓점으로 돌려 닫기", "삼각형을 기울이지 말고 세우기" },
                SpellFamily.Water => new[] { "한 획으로 둥글게 돌기", "끝점을 시작점 바로 옆에 놓기", "찌그러진 타원보다 고른 원 유지" },
                SpellFamily.Wind => new[] { "위, 가운데, 아래 3획만 남기기", "세 선을 같은 기울기로 맞추기", "세 줄 사이 간격을 비슷하게 벌리기" },
                SpellFamily.Earth => new[] { "윗변은 좁고 아랫변은 넓게 잡기", "네 모서리를 분명하게 꺾기", "마지막 선으로 사다리꼴을 닫기" },
                SpellFamily.Life => new[] { "가운데 줄기를 먼저 세우기", "같은 분기점에서 좌우 가지 뻗기", "원처럼 닫지 말고 끝을 열어 두기" },
                _ => Array.Empty<string>()
            };
        }

        private static string TitleFor(AssistLevel level)
        {
            return level switch
            {
                AssistLevel.None => "자율 입력",
                AssistLevel.ReasonHint => "짧은 힌트",
                AssistLevel.Checklist => "체크리스트",
                AssistLevel.GhostTrace => "강한 보조선",
                _ => "힌트"
            };
        }

        private static string BodyFor(SpellFamily family, AssistLevel level, SpellResult result)
        {
            if (level == AssistLevel.None)
            {
                return $"{SpellLabels.Korean(family)} 문양을 먼저 스스로 읽히게 해 보세요.";
            }

            if (level == AssistLevel.ReasonHint)
            {
                return string.IsNullOrWhiteSpace(result?.nextHint) ? ActionHintFor(family) : result.nextHint;
            }

            if (level == AssistLevel.Checklist)
            {
                return string.Join(" · ", ChecklistFor(family));
            }

            return StrongHintFor(family);
        }

        private static HintState CreateState(SpellFamily family, int priorFailures, AssistLevel level, bool assisted, SpellResult result)
        {
            return new HintState
            {
                family = family,
                failureCount = Math.Max(priorFailures, 0),
                currentLevel = level,
                hintShown = level != AssistLevel.None,
                assisted = assisted,
                title = TitleFor(level),
                body = BodyFor(family, level, result)
            };
        }

        private static string ActionHintFor(SpellFamily family)
        {
            return family switch
            {
                SpellFamily.Fire => "삼각형 꼭짓점 3개를 크게 잡고 마지막 점을 시작점 근처로 닫아 보세요.",
                SpellFamily.Water => "한 획으로 둥글게 돌린 뒤 끝점을 시작점 가까이에 놓아 보세요.",
                SpellFamily.Wind => "위, 가운데, 아래에 짧은 평행선 3개를 같은 간격으로 따로 그려 보세요.",
                SpellFamily.Earth => "윗변이 좁고 아랫변이 넓은 사다리꼴을 닫힌 모양으로 그려 보세요.",
                SpellFamily.Life => "가운데 줄기에서 좌우 가지가 갈라지는 열린 Y 형태를 만들어 보세요.",
                _ => "큰 실루엣을 먼저 맞추고 세부 속도는 나중에 조정하세요."
            };
        }

        private static string StrongHintFor(SpellFamily family)
        {
            return family switch
            {
                SpellFamily.Fire => "불꽃은 닫힌 삼각형입니다. 아래 꼭짓점에서 시작해 위 양쪽 꼭짓점을 찍고 처음으로 돌아오세요.",
                SpellFamily.Water => "물은 닫힌 원입니다. 한 번에 둥글게 돌리고 끝점을 시작점 바로 옆에 놓으세요.",
                SpellFamily.Wind => "바람은 도형이 아니라 3획입니다. 위, 가운데, 아래에 같은 기울기의 짧은 선 3개만 남기세요.",
                SpellFamily.Earth => "땅은 닫힌 사다리꼴입니다. 네 모서리를 만들고 마지막 선으로 틈을 막으세요.",
                SpellFamily.Life => "생명은 열린 Y입니다. 줄기 하나를 세운 뒤 같은 분기점에서 왼쪽 가지와 오른쪽 가지를 뻗고 끝을 닫지 마세요.",
                _ => "문양을 더 크게, 더 단순하게 그린 뒤 다시 시전하세요."
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MagicExamHall
{
    [Serializable]
    public sealed class CustomShapePoint
    {
        public float x;
        public float y;
        public float time;
        public float pressure = 1f;

        public CustomShapePoint()
        {
        }

        public CustomShapePoint(StrokeSample sample)
        {
            x = sample.position.x;
            y = sample.position.y;
            time = sample.time;
            pressure = 1f;
        }

        public StrokeSample ToStrokeSample()
        {
            return new StrokeSample(new Vector2(x, y), time);
        }
    }

    [Serializable]
    public sealed class CustomShapeStroke
    {
        public List<CustomShapePoint> points = new();

        public static CustomShapeStroke FromStroke(IReadOnlyList<StrokeSample> stroke)
        {
            return new CustomShapeStroke
            {
                points = stroke.Select(sample => new CustomShapePoint(sample)).ToList()
            };
        }

        public List<StrokeSample> ToStrokeSamples()
        {
            return points
                .Select(point => point.ToStrokeSample())
                .Where(sample => float.IsFinite(sample.position.x) && float.IsFinite(sample.position.y))
                .ToList();
        }
    }

    [Serializable]
    public sealed class CustomShapeCaptureRecord
    {
        public List<CustomShapeStroke> strokes = new();
        public float score;
        public string capturedAtIso = "";

        public static CustomShapeCaptureRecord FromStrokes(
            IReadOnlyList<IReadOnlyList<StrokeSample>> source,
            float score = 1f,
            DateTime? now = null)
        {
            return new CustomShapeCaptureRecord
            {
                strokes = Sanitize(source).Select(CustomShapeStroke.FromStroke).ToList(),
                score = Mathf.Clamp01(score),
                capturedAtIso = (now ?? DateTime.UtcNow).ToString("O", CultureInfo.InvariantCulture)
            };
        }

        public List<List<StrokeSample>> ToStrokeSamples()
        {
            return strokes
                .Select(stroke => stroke.ToStrokeSamples())
                .Where(stroke => stroke.Count >= 2)
                .ToList();
        }

        private static List<List<StrokeSample>> Sanitize(IReadOnlyList<IReadOnlyList<StrokeSample>> source)
        {
            if (source == null)
            {
                return new List<List<StrokeSample>>();
            }

            return source
                .Select(stroke => stroke
                    .Where(sample => float.IsFinite(sample.position.x) && float.IsFinite(sample.position.y))
                    .Select(sample => new StrokeSample(sample.position, sample.time))
                    .ToList())
                .Where(stroke => stroke.Count >= 2)
                .ToList();
        }
    }

    [Serializable]
    public sealed class CustomShapeSlot
    {
        public int slotIndex;
        public string shapeId = "";
        public string label = "";
        public string regexPattern = "";
        public string shapeToken = "";
        public List<string> eventShapeTokens = new();
        public SpellFamily mappedFamily = SpellFamily.Wind;
        public List<CustomShapeCaptureRecord> goldCaptures = new();
        public List<CustomShapeCaptureRecord> autoCaptures = new();
        public string createdAtIso = "";
        public string updatedAtIso = "";

        public bool IsOccupied => !string.IsNullOrWhiteSpace(shapeId) &&
                                  !string.IsNullOrWhiteSpace(label) &&
                                  !string.IsNullOrWhiteSpace(regexPattern) &&
                                  goldCaptures.Any(capture => capture.ToStrokeSamples().Count > 0);

        public IEnumerable<CustomShapeCaptureRecord> AllCaptures()
        {
            foreach (var capture in goldCaptures)
            {
                yield return capture;
            }

            foreach (var capture in autoCaptures)
            {
                yield return capture;
            }
        }
    }

    [Serializable]
    public sealed class CustomShapeProfileDocument
    {
        public int version = 1;
        public List<CustomShapeSlot> slots = new();
    }

    public sealed class CustomShapeValidationResult
    {
        public bool valid;
        public string message = "";
        public Regex regex = null!;
    }

    public sealed class CustomShapeProfileStore
    {
        public const int SlotCount = 12;
        public const int MaxAutoCapturesPerSlot = 18;
        public const int MaxLabelLength = 32;
        public const int MaxRegexLength = 140;

        public static readonly string[] HelperTokens =
        {
            "line",
            "arrow",
            "rect",
            "roundRect",
            "ellipse",
            "triangle",
            "diamond",
            "pentagon",
            "hexagon",
            "star",
            "arc",
            "curve",
            "wave",
            "brace",
            "cross"
        };

        private readonly List<CustomShapeSlot> slots;

        public CustomShapeProfileStore(string storagePath = "")
        {
            StoragePath = string.IsNullOrWhiteSpace(storagePath) ? DefaultStoragePath() : storagePath;
            slots = CreateEmptySlots();
        }

        private CustomShapeProfileStore(string storagePath, IEnumerable<CustomShapeSlot> sourceSlots)
        {
            StoragePath = string.IsNullOrWhiteSpace(storagePath) ? DefaultStoragePath() : storagePath;
            slots = NormalizeSlots(sourceSlots);
        }

        public string StoragePath { get; }
        public IReadOnlyList<CustomShapeSlot> Slots => slots;
        public int OccupiedCount => slots.Count(slot => slot.IsOccupied);

        public static string DefaultStoragePath()
        {
            return Path.Combine(Application.persistentDataPath, "MagicExamHallLogs", "custom-shapes.json");
        }

        public static CustomShapeProfileStore LoadDefault()
        {
            return LoadFromPath(DefaultStoragePath());
        }

        public static CustomShapeProfileStore LoadFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return new CustomShapeProfileStore(path);
            }

            try
            {
                var document = JsonUtility.FromJson<CustomShapeProfileDocument>(File.ReadAllText(path));
                return new CustomShapeProfileStore(path, document?.slots ?? new List<CustomShapeSlot>());
            }
            catch
            {
                return new CustomShapeProfileStore(path);
            }
        }

        public CustomShapeSlot GetSlot(int index)
        {
            return slots[Mathf.Clamp(index, 0, SlotCount - 1)];
        }

        public bool IsSlotOccupied(int index)
        {
            return IsValidSlotIndex(index) && slots[index].IsOccupied;
        }

        public bool TrySaveSlot(
            int slotIndex,
            string label,
            string regexPattern,
            SpellFamily mappedFamily,
            IReadOnlyList<IReadOnlyList<StrokeSample>> goldStrokes,
            out string message)
        {
            return TrySaveSlot(slotIndex, label, regexPattern, InferShapeToken(regexPattern), mappedFamily, goldStrokes, out message);
        }

        public bool TrySaveSlot(
            int slotIndex,
            string label,
            string regexPattern,
            string shapeToken,
            SpellFamily mappedFamily,
            IReadOnlyList<IReadOnlyList<StrokeSample>> goldStrokes,
            out string message)
        {
            return TrySaveSlot(slotIndex, label, regexPattern, shapeToken, new[] { shapeToken }, mappedFamily, goldStrokes, out message);
        }

        public bool TrySaveSlot(
            int slotIndex,
            string label,
            string regexPattern,
            string shapeToken,
            IReadOnlyList<string> eventShapeTokens,
            SpellFamily mappedFamily,
            IReadOnlyList<IReadOnlyList<StrokeSample>> goldStrokes,
            out string message)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                message = "슬롯 번호가 올바르지 않습니다.";
                return false;
            }

            var safeLabel = (label ?? "").Trim();
            var safePattern = (regexPattern ?? "").Trim();
            var safeShapeToken = NormalizeShapeToken(shapeToken);
            var safeEventShapeTokens = NormalizeShapeTokens(eventShapeTokens, safeShapeToken);
            var validation = ValidateDefinition(safeLabel, safePattern);
            if (!validation.valid)
            {
                message = validation.message;
                return false;
            }

            var gold = CustomShapeCaptureRecord.FromStrokes(goldStrokes);
            if (gold.ToStrokeSamples().Count == 0)
            {
                message = "gold capture가 필요합니다.";
                return false;
            }

            var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var slot = slots[slotIndex];
            var createdAt = slot.IsOccupied ? slot.createdAtIso : now;
            slots[slotIndex] = new CustomShapeSlot
            {
                slotIndex = slotIndex,
                shapeId = string.IsNullOrWhiteSpace(slot.shapeId) ? $"custom-slot-{slotIndex + 1:00}" : slot.shapeId,
                label = safeLabel[..Math.Min(safeLabel.Length, MaxLabelLength)],
                regexPattern = safePattern[..Math.Min(safePattern.Length, MaxRegexLength)],
                shapeToken = safeShapeToken,
                eventShapeTokens = safeEventShapeTokens,
                mappedFamily = mappedFamily,
                goldCaptures = new List<CustomShapeCaptureRecord> { gold },
                autoCaptures = LastItems(slot.autoCaptures, MaxAutoCapturesPerSlot).ToList(),
                createdAtIso = createdAt,
                updatedAtIso = now
            };

            Save();
            message = "저장되었습니다.";
            return true;
        }

        public static string BuildGeneratedRegex(string label, string shapeToken)
        {
            var safeShapeToken = NormalizeShapeToken(shapeToken);
            var tokens = new List<string>();
            tokens.AddRange(Tokenize(label ?? ""));
            tokens.Add(safeShapeToken);
            tokens.AddRange(ShapeAliases(safeShapeToken));
            tokens = tokens
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();

            return tokens.Count == 0 ? "line" : string.Join("|", tokens.Select(Regex.Escape));
        }

        public bool DeleteSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return false;
            }

            slots[slotIndex] = EmptySlot(slotIndex);
            Save();
            return true;
        }

        public bool RecordAutoCapture(string shapeId, IReadOnlyList<IReadOnlyList<StrokeSample>> strokes, float score)
        {
            if (string.IsNullOrWhiteSpace(shapeId))
            {
                return false;
            }

            var slot = slots.FirstOrDefault(item => item.shapeId == shapeId && item.IsOccupied);
            if (slot == null)
            {
                return false;
            }

            var capture = CustomShapeCaptureRecord.FromStrokes(strokes, score);
            if (capture.ToStrokeSamples().Count == 0)
            {
                return false;
            }

            slot.autoCaptures.Add(capture);
            while (slot.autoCaptures.Count > MaxAutoCapturesPerSlot)
            {
                slot.autoCaptures.RemoveAt(0);
            }

            slot.updatedAtIso = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            Save();
            return true;
        }

        public CustomShapeValidationResult ValidateDefinition(string label, string regexPattern)
        {
            label = (label ?? "").Trim();
            regexPattern = (regexPattern ?? "").Trim();

            if (string.IsNullOrWhiteSpace(label))
            {
                return Invalid("도형 이름이 필요합니다.");
            }

            if (label.Length > MaxLabelLength)
            {
                return Invalid($"도형 이름은 {MaxLabelLength}자 이하로 입력하세요.");
            }

            if (string.IsNullOrWhiteSpace(regexPattern))
            {
                return Invalid("정규식 정의가 필요합니다.");
            }

            if (regexPattern.Length > MaxRegexLength)
            {
                return Invalid($"정규식은 {MaxRegexLength}자 이하로 입력하세요.");
            }

            Regex compiled;
            try
            {
                compiled = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            catch
            {
                return Invalid("정규식을 읽을 수 없습니다.");
            }

            var keywordSource = $"{label} {string.Join(" ", Tokenize(label))} {string.Join(" ", HelperTokens)}";
            if (!compiled.IsMatch(keywordSource))
            {
                return Invalid("정규식이 도형 이름 또는 지원 토큰과 매칭되지 않습니다.");
            }

            return new CustomShapeValidationResult
            {
                valid = true,
                message = "valid",
                regex = compiled
            };
        }

        public void Save()
        {
            var directory = Path.GetDirectoryName(StoragePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                StoragePath,
                JsonUtility.ToJson(new CustomShapeProfileDocument { slots = slots }, prettyPrint: true));
        }

        private static List<CustomShapeSlot> NormalizeSlots(IEnumerable<CustomShapeSlot> sourceSlots)
        {
            var normalized = CreateEmptySlots();
            foreach (var slot in sourceSlots ?? Array.Empty<CustomShapeSlot>())
            {
                if (!IsValidSlotIndex(slot.slotIndex))
                {
                    continue;
                }

                slot.goldCaptures ??= new List<CustomShapeCaptureRecord>();
                slot.autoCaptures ??= new List<CustomShapeCaptureRecord>();
                slot.eventShapeTokens = NormalizeShapeTokens(slot.eventShapeTokens, slot.shapeToken);
                slot.autoCaptures = LastItems(slot.autoCaptures, MaxAutoCapturesPerSlot).ToList();
                slot.shapeToken = NormalizeShapeToken(string.IsNullOrWhiteSpace(slot.shapeToken)
                    ? InferShapeToken(slot.regexPattern)
                    : slot.shapeToken);
                slot.eventShapeTokens = NormalizeShapeTokens(slot.eventShapeTokens, slot.shapeToken);
                normalized[slot.slotIndex] = slot;
            }

            return normalized;
        }

        private static List<CustomShapeSlot> CreateEmptySlots()
        {
            return Enumerable.Range(0, SlotCount).Select(EmptySlot).ToList();
        }

        private static CustomShapeSlot EmptySlot(int index)
        {
            return new CustomShapeSlot
            {
                slotIndex = index,
                shapeId = "",
                label = "",
                regexPattern = "",
                shapeToken = "",
                eventShapeTokens = new List<string>(),
                mappedFamily = SpellFamily.Wind,
                goldCaptures = new List<CustomShapeCaptureRecord>(),
                autoCaptures = new List<CustomShapeCaptureRecord>(),
                createdAtIso = "",
                updatedAtIso = ""
            };
        }

        private static bool IsValidSlotIndex(int index)
        {
            return index >= 0 && index < SlotCount;
        }

        private static IEnumerable<T> LastItems<T>(IReadOnlyList<T> source, int count)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<T>();
            }

            return source.Skip(Math.Max(0, source.Count - count));
        }

        private static IEnumerable<string> Tokenize(string value)
        {
            return Regex.Split(value.ToLowerInvariant(), @"[^a-z0-9가-힣_]+")
                .Where(item => !string.IsNullOrWhiteSpace(item));
        }

        private static string NormalizeShapeToken(string shapeToken)
        {
            return HelperTokens.Contains(shapeToken ?? "", StringComparer.OrdinalIgnoreCase)
                ? HelperTokens.First(token => string.Equals(token, shapeToken, StringComparison.OrdinalIgnoreCase))
                : "line";
        }

        private static List<string> NormalizeShapeTokens(IEnumerable<string> shapeTokens, string fallback)
        {
            var normalized = (shapeTokens ?? Array.Empty<string>())
                .Select(NormalizeShapeToken)
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (normalized.Count == 0)
            {
                normalized.Add(NormalizeShapeToken(fallback));
            }

            return normalized;
        }

        private static string InferShapeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "line";
            }

            return HelperTokens.FirstOrDefault(token => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) ?? "line";
        }

        private static IEnumerable<string> ShapeAliases(string shapeToken)
        {
            return shapeToken switch
            {
                "line" => new[] { "선", "직선" },
                "arrow" => new[] { "화살표", "화살" },
                "rect" => new[] { "사각형", "네모" },
                "roundRect" => new[] { "둥근사각형", "둥근네모" },
                "ellipse" => new[] { "원", "타원" },
                "triangle" => new[] { "삼각형" },
                "diamond" => new[] { "마름모" },
                "pentagon" => new[] { "오각형" },
                "hexagon" => new[] { "육각형" },
                "star" => new[] { "별", "별표" },
                "arc" => new[] { "호", "반원" },
                "curve" => new[] { "곡선" },
                "wave" => new[] { "물결" },
                "brace" => new[] { "중괄호", "괄호" },
                "cross" => new[] { "십자", "교차" },
                _ => new[] { "자유형", "프리폼" }
            };
        }

        private static CustomShapeValidationResult Invalid(string message)
        {
            return new CustomShapeValidationResult
            {
                valid = false,
                message = message
            };
        }
    }
}

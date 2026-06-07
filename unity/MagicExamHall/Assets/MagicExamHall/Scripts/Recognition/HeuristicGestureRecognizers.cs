using System;
using System.Collections.Generic;

namespace MagicExamHall
{
    public interface IBaseGestureRecognizer
    {
        BaseRecognitionResult RecognizeBase(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes);
    }

    public interface IOverlayGestureRecognizer
    {
        OverlayRecognitionResult RecognizeOverlay(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes, CompiledSeal seal);
    }

    public sealed class HeuristicBaseGestureRecognizer : IBaseGestureRecognizer
    {
        public BaseRecognitionResult RecognizeBase(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes)
        {
            if (strokes == null)
            {
                throw new ArgumentNullException(nameof(strokes));
            }

            return SpellRuntime.RecognizeBase(strokes);
        }
    }

    public sealed class HeuristicOverlayGestureRecognizer : IOverlayGestureRecognizer
    {
        public OverlayRecognitionResult RecognizeOverlay(IReadOnlyList<IReadOnlyList<StrokeSample>> strokes, CompiledSeal seal)
        {
            if (strokes == null)
            {
                throw new ArgumentNullException(nameof(strokes));
            }

            if (seal == null)
            {
                throw new ArgumentNullException(nameof(seal));
            }

            return OverlayRecognizer.Recognize(strokes, seal);
        }
    }
}

# Legacy Survey And Magic-Circle Logic

This folder is the preservation marker for pre-engine survey and magic-circle prototypes.
The active two-track personalization formulas now live in:

- `src/recognizer/two-track-personalization-engine.ts`

The legacy UI and gameplay research surfaces are intentionally kept out of that engine:

- `survey/magic-symbol-tutorial/`
- `survey/tinyml-noisy-eval/`
- `survey/tutorial-threshold-eval/`
- `src/survey/`
- `src/recognizer/seal.ts`
- `src/recognizer/overlay.ts`
- `src/recognizer/operators.ts`

Use these paths as historical references for capture UX, survey submission, and magic-circle
presentation rules. Do not add new personalization policy formulas here; keep final two-track
policy changes inside the engine file above.

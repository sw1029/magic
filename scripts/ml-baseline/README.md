# ML Baseline Scripts

Base family tiny baseline:

```bash
python3 scripts/ml-baseline/train-base-family-baseline.py
```

What it does:

* generates synthetic family train/eval/hard-negative datasets with `scripts/tutorial-dataset/generate-synthetic.mjs`
* exports `base_candidate_row_v1` rows from the current recognizer via `scripts/ml-baseline/export-base-family-rows.mjs`
* trains a `GradientBoostingClassifier` main rerank baseline and `LogisticRegression` parity baseline
* trains a top-1 confidence calibrator and writes JSON artifacts

Outputs:

* `artifacts/ml/base-rerank-v1.json`
* `artifacts/ml/base-confidence-v1.json`

Current acceptance note:

* the offline selector keeps `familyFlipIncrease == 0` on both `eval` and `hard_negative_eval`
* under the current synthetic data sweep, the accepted rerank gate is conservative and keeps `delta_scale=0.0`
* confidence calibration still improves sharply over raw heuristic/confidence baselines and is exported in `base-confidence-v1.json`

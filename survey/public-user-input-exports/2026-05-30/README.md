# Public user-input survey exports, 2026-05-30

This directory contains sanitized public exports derived from local survey result files.
The retained data focuses on user-authored drawing inputs and the non-identifying target/recognition context needed to analyze those inputs.

Privacy cleanup applied:

- Removed submission/session identifiers, browser metadata, locale/timezone, free-form notes, and consent/session fields.
- Removed saved/completed timestamps, elapsed-time fields, pointer type, save state, and threshold transition logs.
- Removed stroke ids, point timestamps, and point pressure values.
- Normalized drawing coordinates to 0..1 within the source canvas instead of preserving screen pixel coordinates.

Files:

- `tinyml-noisy-eval/run-a.user-input.json` and `run-a.summary.csv`: sanitized 78-record TinyML noisy-eval export.
- `tinyml-noisy-eval/run-b.user-input.json` and `run-b.summary.csv`: sanitized 79-record TinyML noisy-eval export.
- `tutorial-threshold-eval/run-a.user-input.json` and `run-a.summary.csv`: sanitized 74-record tutorial threshold capture export.
- `manifest.json`: source-to-public file mapping and record counts.

# Contributing

This repository is public and shared by multiple contributors. Keep changes reviewable, scoped, and easy to validate.

## Required Workflow

- Do not push directly to `main`.
- Work on a short-lived branch for each change.
- Open a pull request for every code, docs, CI, or artifact change.
- Include the validation commands that were run in the PR body.
- Leave a clear note in the PR body when validation was skipped or could not be run.
- Do not bypass branch protection, required reviews, or required status checks.

## Safety Rules

- Do not commit secrets, API keys, tokens, private credentials, personal data, or local `.env` files.
- Do not add generated datasets, raw capture data, or large outputs unless the task explicitly asks for it.
- Do not change files under `artifacts/ml/` unless the task explicitly asks for ML artifact updates.
- Do not rewrite docs task state, queues, or frontmatter unless the task explicitly includes project-state maintenance.
- Do not revert unrelated user changes.
- Keep changes scoped to the requested behavior and the surrounding code that is necessary to support it.

## Expected Validation

Run the relevant checks before opening a PR:

```bash
npm run validate:docs
npm test
npm run build
```

For UI changes, also run the app locally and capture screenshots or describe the checked flow in the PR.

## Repository Context

- `src/` contains the web demo and recognizer core.
- `tests/` contains Vitest coverage.
- `scripts/` contains docs, survey, dataset, and baseline utility scripts.
- `docs/` contains project direction and work-tracking documents.
- `artifacts/ml/` contains versioned ML artifacts and should be treated as intentional project data.

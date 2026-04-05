# 소형 ML baseline + 유저 개인화 프롬프트 pack

이 문서는 [`tiny-ml-baseline-plan.md`](tiny-ml-baseline-plan.md)를 실제 작업으로 옮길 때 사용할 프롬프트 묶음이다.

이번 pack의 초점:

* public auxiliary / synthetic / tutorial의 역할 분리
* base family + overlay operator 둘 다를 다루는 tiny ML baseline
* 규칙계 권위를 유지하는 shadow-mode integration
* tutorial vector capture가 들어왔을 때의 user personalization adapter

주의:

* `synthetic_tutorial_like`는 onboarding 분포 근사용 synthetic preset이며, 실제 tutorial `adaptation` lane을 대신하지 않는다.
* `adaptation` / `acceptance_eval` split은 tutorial vector capture export가 들어온 뒤에만 쓴다.

권장 방식은 **통합 프롬프트 1개 + 병렬 agent 프롬프트 5개 + 최종 검수 프롬프트**다.

---

## 0. 현재 구현 반영 상태

현재 작업 트리 기준:

* base family 5종, overlay operator 6종의 heuristic recognizer가 있다.
* `rerank.ts`를 통해 heuristic-only personalization이 이미 들어가 있다.
* `tutorial-profile.ts`에는 family/operator prototype과 placement-aware operator summary가 있다.
* `scripts/tutorial-dataset/*`에는 public auxiliary raw cache/downloader와 NDJSON converter, ML split/feature manifest generator가 있다.
* `scripts/ml-baseline/*`와 `artifacts/ml/*`에 base/operator offline baseline과 artifact가 있다.
* runtime artifact loader와 shadow-mode summary가 이미 연결돼 있다.
* tutorial capture 실제 수집 UI는 아직 없다.
* 최종 decision gate-open은 아직 열지 않았다.

현재 확인된 실제 데이터 상태:

* Quick Draw auxiliary: `514,739`
* `$-family`: `14,878`
* CROHME loose InkML: `488`
* public auxiliary는 모두 `label: null`, `priority: public_auxiliary`, `usage.forbidden` 계약을 가진다.

즉, 이 pack은 **현재 shadow baseline의 acceptance/test/doc sync와 다음 personalization wave 준비**를 위한 것이다.

---

## 1. 공통 원칙

모든 프롬프트는 아래 전제를 공유한다.

* 반드시 [`tiny-ml-baseline-plan.md`](tiny-ml-baseline-plan.md)를 먼저 읽는다.
* `same shape = same family`를 깨면 안 된다.
* `same operator shape = same operator meaning`을 깨면 안 된다.
* `martial_axis requires void_cut`는 계속 규칙계가 처리한다.
* 모델은 canonical family/operator를 직접 결정하지 않는다.
* 모델은 `primitive/normalization 보조`, `top-k rerank`, `confidence calibration`, `tutorial-aware adaptation`까지만 허용한다.
* tutorial capture는 vector stroke 기준만 다루고, raster image mixing은 v1 baseline에서 제외한다.
* 초기 runtime 적용은 shadow mode로 시작한다.

구현 전에 반드시 읽을 파일:

* [`tiny-ml-baseline-plan.md`](tiny-ml-baseline-plan.md)
* [`tutorial-personalization-plan.md`](tutorial-personalization-plan.md)
* [`src/recognizer/recognize.ts`](/home/ysw/magic/src/recognizer/recognize.ts)
* [`src/recognizer/operators.ts`](/home/ysw/magic/src/recognizer/operators.ts)
* [`src/recognizer/rerank.ts`](/home/ysw/magic/src/recognizer/rerank.ts)
* [`src/recognizer/tutorial-profile.ts`](/home/ysw/magic/src/recognizer/tutorial-profile.ts)
* [`scripts/tutorial-dataset/README.md`](/home/ysw/magic/scripts/tutorial-dataset/README.md)

---

## 2. 통합 프롬프트

```text
현재 레포에 tiny ML baseline을 추가하라.

핵심 목표:
1. public auxiliary / synthetic / tutorial의 역할을 분리한 offline baseline을 만든다.
2. base family와 overlay operator 둘 다에 대해 top-k rerank + confidence calibration baseline을 설계/구현한다.
3. runtime은 shadow mode로 시작하고, rule gate와 canonical semantics는 그대로 유지한다.
4. tutorial vector capture가 들어왔을 때의 user personalization adapter를 결합 가능하게 만든다.

필수 제약:
* canonical family/operator semantics는 규칙계가 가진다.
* same shape = same family, same operator shape = same operator meaning을 깨면 안 된다.
* `martial_axis requires void_cut`는 여전히 규칙계가 처리한다.
* public auxiliary는 direct family/operator classifier 정답셋으로 쓰면 안 된다.
* tutorial capture는 vector stroke 기준만 다루고, raster image는 v1 baseline에서 제외한다.

작업 기준:
* 반드시 docs/20_queue/tiny-ml-baseline-plan.md 를 구현 기준으로 따른다.
* 가능하면 병렬로 진행하고, 마지막에 테스트/문서 동기화까지 수행한다.
```

---

## 3. 병렬 작업 구조

### Agent A. dataset split / feature manifest

소유 범위:

* `scripts/tutorial-dataset/*`
* `artifacts/ml/feature-spec-v1.json` 또는 동등한 schema 파일
* 필요 시 `docs/20_queue/tiny-ml-baseline-plan.md`의 data appendix

목표:

* public/synthetic/tutorial split manifest
* base/operator feature row contract
* Quick Draw dominance reweight 정책

프롬프트:

```text
`docs/20_queue/tiny-ml-baseline-plan.md`를 기준으로 tiny ML baseline용 dataset split과 feature manifest를 구현하라.

너의 소유 범위:
* scripts/tutorial-dataset/*
* artifacts/ml/feature-spec-v1.json 또는 동등 schema

필수 요구:
* public_auxiliary / synthetic_primary / tutorial_primary의 역할을 코드와 문서에 함께 남긴다.
* base family와 operator baseline용 feature row를 명시한다.
* Quick Draw dominance를 막기 위한 reweight/cap 규칙을 포함한다.
* public data는 direct family/operator supervision으로 못 쓰게 유지한다.
```

### Agent B. base family tiny baseline

소유 범위:

* `scripts/ml-baseline/*` 또는 동등한 Python sidecar 디렉토리
* `artifacts/ml/base-rerank-v1.json`
* `artifacts/ml/base-confidence-v1.json`

목표:

* base family GBDT reranker
* base confidence calibrator

프롬프트:

```text
`docs/20_queue/tiny-ml-baseline-plan.md`를 기준으로 base family tiny baseline을 구현하라.

너의 소유 범위:
* scripts/ml-baseline/*
* artifacts/ml/base-rerank-v1.json
* artifacts/ml/base-confidence-v1.json

필수 요구:
* heuristic candidate와 RecognitionFeatures를 입력으로 하는 base rerank baseline을 만든다.
* canonical family semantics를 직접 예측하지 말고, top-k rerank delta와 confidence만 출력한다.
* baseline은 gradient boosting을 기본으로 하고, logistic regression parity baseline도 비교 가능하게 둔다.
* family flip 증가가 0인지 확인하는 offline eval을 포함한다.
```

### Agent C. operator tiny baseline

소유 범위:

* `scripts/ml-baseline/*`
* `artifacts/ml/operator-rerank-v1.json`
* `artifacts/ml/operator-confidence-v1.json`

목표:

* operator pairwise GBDT
* confidence calibrator

프롬프트:

```text
`docs/20_queue/tiny-ml-baseline-plan.md`를 기준으로 overlay operator tiny baseline을 구현하라.

너의 소유 범위:
* scripts/ml-baseline/*
* artifacts/ml/operator-rerank-v1.json
* artifacts/ml/operator-confidence-v1.json

필수 요구:
* `void_cut/electric_fork` 등 hard-negative pair를 보정하는 pairwise baseline을 만든다.
* `anchorScore`, `scaleScore`, `placement`를 입력에 포함한다.
* `blockedBy`, off-anchor, wrong-scale 규칙을 우회하지 못하게 한다.
* canonical operator semantics를 직접 예측하지 말고 rerank delta와 confidence만 출력한다.
```

### Agent D. runtime shadow integration + personalization adapter

소유 범위:

* `src/recognizer/rerank.ts`
* `src/recognizer/recognize.ts`
* `src/recognizer/operators.ts`
* `src/app.ts`
* 필요 시 `src/recognizer/tutorial-profile.ts`

목표:

* artifact loader
* shadow mode scoring
* tutorial-aware user feature injection

프롬프트:

```text
`docs/20_queue/tiny-ml-baseline-plan.md`를 기준으로 runtime shadow integration과 tutorial-aware personalization adapter를 구현하라.

너의 소유 범위:
* src/recognizer/rerank.ts
* src/recognizer/recognize.ts
* src/recognizer/operators.ts
* src/app.ts
* 필요 시 src/recognizer/tutorial-profile.ts

필수 요구:
* artifact가 없으면 heuristic-only fallback으로 동작한다.
* artifact가 있어도 초기에는 shadow mode로만 점수를 계산한다.
* tutorial sample 수에 따라 user feature injection과 threshold bias를 다르게 적용한다.
* dependency와 same-shape invariants는 항상 규칙계가 먼저 보장한다.
```

### Agent E. 테스트 / 문서 / queue 통합

소유 범위:

* `tests/*`
* `docs/20_queue/*`
* `docs/30_tasks/epic-02-symbols-and-input/*`
* `docs/30_tasks/epic-07-future-expansion-backlog/*`
* `docs/20_queue/work-queue.md`

목표:

* offline eval 기준 정리
* shadow mode 수용 기준 정리
* queue/task 문서 동기화

프롬프트:

```text
`docs/20_queue/tiny-ml-baseline-plan.md`를 기준으로 tiny ML baseline의 테스트/문서/queue를 통합하라.

너의 소유 범위:
* tests/*
* docs/20_queue/*
* docs/30_tasks/epic-02-symbols-and-input/*
* docs/30_tasks/epic-07-future-expansion-backlog/*
* docs/20_queue/work-queue.md

필수 요구:
* ambiguity 감소, hard-negative error 감소, family flip 증가 0, dependency violation 0을 수용 기준으로 정리한다.
* T07-08, T07-09, 필요 시 T02-08을 queue와 README에 연결하고 현재 구현 상태와 남은 계획 상태를 분리한다.
* 문서는 실제 구현보다 앞서 나가지 않게 현재 상태와 계획 상태를 구분해 쓴다.
```

---

## 4. 순차 실행 프롬프트

### A. dataset split / feature schema

```text
tiny ML baseline용 dataset split manifest와 feature schema를 먼저 정리하라.

작업 대상:
* scripts/tutorial-dataset/*
* artifacts/ml/feature-spec-v1.json
```

### B. base family baseline

```text
base family top-k rerank와 confidence calibration baseline을 offline 실험 기준으로 구현하라.

작업 대상:
* scripts/ml-baseline/*
* artifacts/ml/base-rerank-v1.json
* artifacts/ml/base-confidence-v1.json
```

### C. operator baseline

```text
overlay operator hard-negative pair를 다루는 tiny baseline을 offline 실험 기준으로 구현하라.

작업 대상:
* scripts/ml-baseline/*
* artifacts/ml/operator-rerank-v1.json
* artifacts/ml/operator-confidence-v1.json
```

### D. runtime shadow integration

```text
runtime이 ML artifact를 optional shadow mode로 읽도록 연결하라.

작업 대상:
* src/recognizer/rerank.ts
* src/recognizer/recognize.ts
* src/recognizer/operators.ts
* src/app.ts
```

### E. personalization adapter

```text
tutorial vector capture가 들어왔을 때 user feature injection과 threshold bias를 결합하는 adapter를 추가하라.

작업 대상:
* src/recognizer/tutorial-profile.ts
* src/recognizer/rerank.ts
* 필요 시 src/app.ts
```

---

## 5. 최종 검수 프롬프트

```text
현재 작업 트리를 기준으로 tiny ML baseline과 user personalization 결합 목표가 실제로 충족됐는지 점검하라.

체크 항목:
* public/synthetic/tutorial 역할이 데이터와 문서에서 분리되어 있는가
* base family와 overlay operator 둘 다에 tiny baseline artifact가 존재하는가
* runtime이 artifact를 optional로 읽고, heuristic fallback이 가능한가
* 초기 적용이 shadow mode인가
* same shape invariance와 dependency rule이 그대로 유지되는가
* tutorial sample 수에 따른 personalization activation policy가 정리되어 있는가
* npm test, npm run build, npm run validate:docs 가 통과하는가

출력 형식:
* 충족된 항목
* 미충족 항목
* 남은 리스크
* 다음 권장 작업 3개 이내
```

---

## 6. 권장 실행 순서

가장 권장하는 순서는 아래다.

1. Agent A로 데이터/feature 계약 고정
2. Agent B, C를 병렬로 돌려 base/operator offline baseline 생성
3. Agent D가 runtime shadow integration과 personalization adapter를 연결
4. Agent E가 테스트/문서/queue를 정리
5. 최종 검수 프롬프트 수행

이 순서를 따르면,
현재 규칙 기반 recognizer 구조를 유지하면서도 tiny ML baseline과 future user personalization을 가장 안전하게 붙일 수 있다.

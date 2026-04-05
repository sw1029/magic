# 튜토리얼 기반 개인화 인식 확장 프롬프트 pack

이 문서는 `docs/20_queue/tutorial-personalization-plan.md`를 실제 작업으로 옮길 때 사용할 프롬프트 묶음입니다.

이번 pack의 초점은 아래입니다.

* 튜토리얼에서 얻는 소량의 사용자별 입력을 활용
* `same shape = same family`를 유지
* base family와 overlay operator 모두에 개인화 보조층 적용
* 규칙계는 유지하고, 작은 보조층만 추가

권장 방식은 **통합 프롬프트 1개 + 시스템 분할 병렬 프롬프트 5개 + 통합 검수 프롬프트**입니다.

---

## 0. 현재 구현 반영 상태

현재 작업 트리 기준으로 아래 항목은 이미 들어와 있습니다.

* `src/recognizer/tutorial-profile.ts`와 `src/recognizer/types.ts`에 tutorial capture/store, shape/operator prototype, calibration 구조가 있다.
* `src/recognizer/rerank.ts`, `src/recognizer/recognize.ts`, `src/recognizer/operators.ts`에 base/overlay personalization rerank가 들어가 있다.
* `src/app.ts`에는 localStorage 기반 tutorial onboarding hook과 live recognizer 주입 경로가 있다.
* operator tutorial capture는 `baseSnapshot`과 `operatorContext`를 함께 저장하고, overlay personalization은 placement-aware rerank를 사용한다.
* `scripts/tutorial-dataset/*`에는 hybrid dataset helper와 공통 NDJSON contract가 있다.
* `tests/recognizer-v15.test.ts`, `tests/overlay-operators.test.ts`에 불변 조건과 hard negative 검증이 추가됐다.

현재 작업 트리 검증 결과:

* `npm test`: 5개 test file, 39개 test 통과
* `npm run build`: 통과
* `npm run validate:docs`: 통과

아직 남아 있는 핵심 통합은 아래다.

* full tutorial UI와 dataset training pipeline orchestration은 아직 구현 범위 밖이다.
* placement-aware personalization을 넘는 learned placement prior / tiny model은 아직 backlog다.

즉, 아래 프롬프트는 현재 구현을 이어서 hardening하거나, 후속 placement/tiny-model wave를 분리 실행할 때 사용한다.

---

## 1. 사용 원칙

모든 프롬프트는 아래 전제를 공유합니다.

* 반드시 `docs/20_queue/tutorial-personalization-plan.md`를 먼저 읽는다.
* 현재 `src/recognizer/recognize.ts`, `src/recognizer/operators.ts`, `src/recognizer/user-profile.ts` 구조를 출발점으로 사용한다.
* `same shape = same family` 원칙을 깨면 안 된다.
* operator dependency rule, 특히 `martial_axis requires void_cut`를 깨면 안 된다.
* 최종 canonicalization 권한은 규칙계가 가진다.
* AI/ML은 입력 보정, 후보 재정렬, 확신도 추정, 개인화에만 쓴다.
* 문서와 코드 naming은 한국어 문서 / 영어 코드 기준을 유지한다.

구현 전에 반드시 읽을 파일:

* `docs/20_queue/tutorial-personalization-plan.md`
* `docs/20_queue/tiny-ml-baseline-plan.md`
* `docs/20_queue/prototype-implementation-plan.md`
* `docs/10_direction/final-direction.md`
* `docs/10_direction/prototype-target.md`
* `src/recognizer/templates.ts`
* `src/recognizer/operator-templates.ts`
* `src/recognizer/recognize.ts`
* `src/recognizer/operators.ts`
* `src/recognizer/user-profile.ts`
* `src/recognizer/types.ts`

---

## 2. 통합 프롬프트

```text
현재 레포에 튜토리얼 기반 개인화 인식 보조층을 추가하라.

핵심 목표:
1. 직접 마법진 입력 전 튜토리얼에서 base family 5종과 overlay operator 6종에 대한 사용자별 입력 샘플을 받을 수 있게 준비한다.
2. 현재 규칙 기반 recognizer를 유지한 채, 사용자별 shape/operator prototype과 rerank/confidence 보조층을 추가한다.
3. base family와 overlay operator 모두에서 오인식과 ambiguous를 줄이되, same shape = same family 원칙을 깨지 않는다.
4. synthetic/public/tutorial hybrid 데이터 전략을 구현 준비 수준으로 정리한다.

필수 제약:
* 최종 family/operator 의미 결정권은 규칙계가 가진다.
* user profile이 invalid를 억지로 recognized로 뒤집는 방향이면 안 된다.
* `martial_axis requires void_cut` 같은 dependency는 personalization 이후에도 규칙으로 유지한다.
* tutorial 데이터는 trace만이 아니라 recall/variation까지 고려한다.

작업 기준:
* 반드시 `docs/20_queue/tutorial-personalization-plan.md`를 구현 기준으로 따른다.
* 구현이 끝나면 테스트, 빌드, 문서 반영까지 수행한다.
```

---

## 3. 시스템 분할 병렬 작업 구조

병렬 작업은 아래 5개로 나눈다.

### Agent A. 튜토리얼 입력 캡처와 저장 형식

소유 범위:

* `src/recognizer/types.ts`
* `src/recognizer/tutorial-profile.ts`
* `src/app.ts`의 tutorial hook

목표:

* `TutorialCapture`, `UserShapeProfile`, `FamilyPrototype`, `OperatorPrototype` 타입 추가
* 튜토리얼 입력 저장 형식과 local storage / session 연결 자리 정의
* trace / recall / variation 3종 샘플 구분 구조 추가

프롬프트:

```text
`docs/20_queue/tutorial-personalization-plan.md`를 기준으로 튜토리얼 입력 캡처와 저장 구조를 구현하라.

너의 소유 범위:
* src/recognizer/types.ts
* src/recognizer/tutorial-profile.ts
* src/app.ts 중 tutorial/onboarding hook 구간만

필수 요구:
* TutorialCapture, UserShapeProfile, FamilyPrototype, OperatorPrototype 타입을 추가한다.
* base family 5종과 overlay operator 6종을 모두 수용한다.
* trace / recall / variation 구분을 저장한다.
* 현재 recognizer semantics는 건드리지 않는다.

중요:
* 다른 작업자가 recognize.ts, operators.ts, scripts 쪽을 동시에 수정할 수 있다.
* 그쪽 변경을 되돌리지 말고, tutorial capture와 profile 저장 구조에만 집중하라.
```

### Agent B. base family personalization core

소유 범위:

* `src/recognizer/recognize.ts`
* `src/recognizer/rerank.ts`
* `tests/recognizer-v15.test.ts`

목표:

* base family top-k rerank
* family confusion pair calibration
* user prototype similarity blending

프롬프트:

```text
`docs/20_queue/tutorial-personalization-plan.md`를 기준으로 base family personalization rerank를 구현하라.

너의 소유 범위:
* src/recognizer/recognize.ts
* src/recognizer/rerank.ts
* tests/recognizer-v15.test.ts

필수 요구:
* global heuristic candidate를 유지한 채 top-k rerank와 confidence calibration만 추가한다.
* same shape = same family 원칙을 깨지 않는다.
* user family prototype과 confusion pair를 사용할 수 있게 한다.
* ambiguous 감소 또는 confidence calibration 개선을 검증하는 테스트를 추가한다.

중요:
* types/tutorial profile은 다른 작업자가 제공하는 구조를 따른다.
* canonical family 결정권을 모델/보조층에 넘기지 마라.
```

### Agent C. overlay operator personalization core

소유 범위:

* `src/recognizer/operators.ts`
* `src/recognizer/rerank.ts`
* `tests/overlay-operators.test.ts`

목표:

* operator shape confidence personalization
* `void_cut`/`electric_fork` 같은 hard pair rerank
* dependency rule 유지

프롬프트:

```text
`docs/20_queue/tutorial-personalization-plan.md`를 기준으로 overlay operator personalization 보조층을 구현하라.

너의 소유 범위:
* src/recognizer/operators.ts
* src/recognizer/rerank.ts
* tests/overlay-operators.test.ts

필수 요구:
* operator candidate 생성은 유지하고, shape confidence와 top-k rerank만 보조적으로 추가한다.
* `martial_axis requires void_cut`는 규칙으로 유지한다.
* anchor zone과 scale 규칙을 personalization이 무시하면 안 된다.
* hard negative 중심 테스트를 추가한다.

중요:
* Agent B도 rerank.ts를 건드릴 수 있으므로 공통 helper는 충돌 없이 합칠 수 있게 작성하라.
* dependency를 모델이 암묵적으로 대신 판단하게 만들지 마라.
```

### Agent D. hybrid 데이터 전략과 offline helper

소유 범위:

* `scripts/tutorial-dataset/*`
* 필요 시 `docs/20_queue/tutorial-personalization-plan.md`의 dataset appendix 보강

목표:

* synthetic/public/tutorial 데이터 전략을 실행 준비 수준으로 정리
* 합성 데이터 생성 helper 자리 마련
* 공개 데이터 변환 helper 자리 마련

프롬프트:

```text
`docs/20_queue/tutorial-personalization-plan.md`를 기준으로 hybrid 데이터 전략을 구현 준비 수준으로 정리하라.

너의 소유 범위:
* scripts/tutorial-dataset/*
* 필요 시 docs/20_queue/tutorial-personalization-plan.md 의 데이터 appendix만 보강

필수 요구:
* synthetic 데이터 생성 규칙을 코드 또는 README 형태로 남긴다.
* Quick, Draw / $-family / CROHME 같은 공개 데이터는 직접 family classifier 학습이 아니라 전처리/표현학습 보조 용도로만 다룬다.
* tutorial 데이터가 최우선이라는 원칙을 명확히 남긴다.

중요:
* recognizer core를 직접 수정하지 마라.
* 지금 단계에서는 실제 대형 파이프라인보다 실행 가능한 helper와 README를 우선한다.
```

### Agent E. 테스트/평가/문서 통합

소유 범위:

* `tests/*` 중 통합 평가
* `README.md`
* `docs/20_queue/tutorial-personalization-plan.md`
* `docs/20_queue/tutorial-personalization-prompt-pack.md`

목표:

* 테스트 추가
* 평가 기준과 남은 리스크 정리
* 구현 후 문서 동기화

프롬프트:

```text
`docs/20_queue/tutorial-personalization-plan.md`를 기준으로 tutorial personalization 확장에 대한 테스트/평가/문서 통합을 담당하라.

너의 소유 범위:
* tests/*
* README.md
* docs/20_queue/tutorial-personalization-plan.md
* docs/20_queue/tutorial-personalization-prompt-pack.md

필수 요구:
* family flip 증가 여부, ambiguous 감소 또는 calibration 개선 여부를 검증하는 테스트를 추가한다.
* 구현 후 README와 설계 문서를 현재 상태에 맞춰 반영한다.
* 남은 리스크와 stop condition을 짧게 정리한다.

중요:
* 다른 작업자가 recognizer core를 수정 중일 수 있으므로, 통합 관점에서만 수정하라.
* 규칙 불변 조건을 테스트로 다시 고정하라.
```

---

## 4. 단계별 순차 프롬프트

병렬이 아닌 순차 실행이 필요할 때는 아래 순서를 권장한다.

### A. 튜토리얼 캡처와 profile 타입

```text
`docs/20_queue/tutorial-personalization-plan.md`를 기준으로 튜토리얼 입력 캡처 타입과 사용자 shape/operator profile 구조를 먼저 추가하라.

작업 대상:
* src/recognizer/types.ts
* src/recognizer/tutorial-profile.ts
* 필요 시 src/app.ts 의 tutorial entry hook
```

### B. base family rerank / confidence

```text
`docs/20_queue/tutorial-personalization-plan.md`를 기준으로 base family personalization rerank와 confidence calibration을 추가하라.

작업 대상:
* src/recognizer/recognize.ts
* src/recognizer/rerank.ts
* tests/recognizer-v15.test.ts
```

### C. overlay operator rerank / confidence

```text
`docs/20_queue/tutorial-personalization-plan.md`를 기준으로 overlay operator personalization 보조층을 추가하라.

작업 대상:
* src/recognizer/operators.ts
* src/recognizer/rerank.ts
* tests/overlay-operators.test.ts
```

### D. hybrid 데이터 helper

```text
`docs/20_queue/tutorial-personalization-plan.md`를 기준으로 synthetic/public/tutorial hybrid 데이터 전략을 실행 준비 수준으로 정리하라.

작업 대상:
* scripts/tutorial-dataset/*
* 필요 시 docs 보강
```

### E. 검증과 문서 동기화

```text
tutorial personalization 확장 결과를 검증하고 문서를 현재 구현 상태에 맞게 갱신하라.

실행:
* npm test
* npm run build
* npm run validate:docs

보고 형식:
* 구현된 항목
* 검증 결과
* 남은 리스크
```

---

## 5. 통합 검수 프롬프트

```text
현재 작업 트리를 기준으로 tutorial-driven personalization 목표가 실제로 충족됐는지 점검하라.

체크 항목:
* 튜토리얼 입력을 저장할 타입과 profile 구조가 존재하는가
* 실제 앱 루프가 tutorial profile store를 base/overlay recognizer 호출에 전달하는가
* base family에서 same shape = same family 원칙이 personalization 후에도 유지되는가
* operator personalization이 dependency rule을 깨지 않는가
* global heuristic 위에 rerank/confidence 보조만 얹혀 있는가
* synthetic/public/tutorial 데이터 전략이 문서화됐는가
* npm test, npm run build, npm run validate:docs 가 통과하는가

출력 형식:
* 충족된 항목
* 미충족 항목
* 남은 리스크
* 다음 권장 작업 3개 이내
```

---

## 6. 권장 실행 순서

가장 권장하는 실제 사용 순서는 아래다.

1. Agent A, B, C를 병렬로 실행
2. Agent D를 병렬로 실행하되 recognizer core와 독립 유지
3. Agent E가 통합 테스트와 문서 정리
4. 통합 검수 프롬프트 수행

이 순서를 따르면,
현재 V1.5 recognizer 구조를 깨지 않고 `튜토리얼 기반 개인화 보조층`을 가장 안전하게 확장할 수 있다.

---

## 7. operator tutorial context v2 병렬 prompt pack

이번 pack은 기존 tutorial personalization의 후속 wave로,
operator placement-aware personalization만 별도로 진행할 때 사용한다.

공통 불변 조건:

* `same shape = same family`
* `same operator shape = same operator meaning`
* `martial_axis requires void_cut`
* placement personalization은 `top-k rerank`와 `confidence 보조`까지만 허용
* canonical family/operator 결정권은 계속 규칙계가 가진다.

### 7-1. 통합 프롬프트

```text
`docs/20_queue/tutorial-personalization-plan.md`를 먼저 읽고, operator tutorial context v2를 구현하라.

목표:
1. operator tutorial capture에 base reference frame 기준 placement metadata를 저장한다.
2. operator prototype을 shape-only에서 shape+placement summary로 확장한다.
3. overlay recognizer에서 placement-aware personalization을 추가하되, dependency와 same-shape invariants는 유지한다.
4. off-anchor / wrong-scale 입력을 personalization이 rescue하지 못하게 한다.
5. 테스트와 문서를 이번 wave 기준으로 갱신한다.

불변 조건:
* canonical family/operator semantics는 규칙계가 가진다.
* same shape = same family, same operator shape = same operator meaning을 깨면 안 된다.
* `martial_axis requires void_cut`는 personalization 이후에도 규칙으로 강제한다.
* placement personalization은 shape confidence보다 앞서면 안 된다.
```

### 7-2. Agent P1. schema/store

소유 범위:

* `src/recognizer/types.ts`
* `src/recognizer/tutorial-profile.ts`

프롬프트:

```text
`docs/20_queue/tutorial-personalization-plan.md`의 operator tutorial context v2 섹션을 기준으로 schema/store를 구현하라.

너의 소유 범위:
* src/recognizer/types.ts
* src/recognizer/tutorial-profile.ts

필수 요구:
* operator tutorial capture에 `baseSnapshot`과 `operatorContext`를 추가한다.
* hydrate는 기존 shape-only tutorial data와 backward compatible 해야 한다.
* store rebuild 시 `averageAnchorZoneId`, `averageScaleRatio`, `averageStackIndex`, `existingOperatorBiases`를 계산한다.
* shape-only fallback 경로를 유지한다.

중요:
* recognizer semantics를 바꾸지 마라.
* app.ts, operators.ts, rerank.ts, tests는 다른 작업자가 동시에 수정할 수 있다.
```

### 7-3. Agent P2. app capture plumbing

소유 범위:

* `src/app.ts`

프롬프트:

```text
`docs/20_queue/tutorial-personalization-plan.md`의 operator tutorial context v2 섹션을 기준으로 app tutorial capture plumbing을 구현하라.

너의 소유 범위:
* src/app.ts

필수 요구:
* operator tutorial capture 시 current base reference frame과 existingOperators를 snapshot으로 저장한다.
* `anchorZoneId`, `anchorScore`, `scaleRatio`, `angleRadians`, `stackIndex`를 capture metadata로 넘긴다.
* base seal 이전에는 operator tutorial capture를 저장하지 않는다.
* 기존 tutorial hook API는 가능한 한 유지한다.

중요:
* recognizer 계산 로직 자체는 다른 작업자가 수정할 수 있다.
* full tutorial UI를 새로 만들지 말고 현재 hook/bridge 구조를 확장하라.
```

### 7-4. Agent P3. overlay placement-aware rerank

소유 범위:

* `src/recognizer/rerank.ts`
* `src/recognizer/operators.ts`

프롬프트:

```text
`docs/20_queue/tutorial-personalization-plan.md`의 operator tutorial context v2 섹션을 기준으로 overlay placement-aware personalization을 구현하라.

너의 소유 범위:
* src/recognizer/rerank.ts
* src/recognizer/operators.ts

필수 요구:
* 기존 heuristic candidate 생성은 유지한다.
* shape similarity 위에 anchor/scale/stack/context placement similarity를 gated bonus로 추가한다.
* personalization은 blockedBy, anchorScore, scaleScore를 우회할 수 없게 한다.
* off-anchor 또는 wrong-scale candidate는 personalization으로 recognized가 되면 안 된다.
* `void_cut/electric_fork` hard-negative는 shape+placement가 함께 맞을 때만 더 강한 rerank를 허용한다.

중요:
* dependency는 계속 규칙계가 처리해야 한다.
* score shift cap은 기존보다 보수적으로 유지하라.
```

### 7-5. Agent P4. tests/docs integration

소유 범위:

* `tests/overlay-operators.test.ts`
* `tests/recognizer-v15.test.ts`
* `docs/20_queue/tutorial-personalization-plan.md`
* `docs/20_queue/tutorial-personalization-prompt-pack.md`
* `docs/20_queue/work-queue.md`
* `docs/30_tasks/epic-02-symbols-and-input/*`
* `docs/30_tasks/epic-07-future-expansion-backlog/*`

프롬프트:

```text
`docs/20_queue/tutorial-personalization-plan.md`를 기준으로 operator tutorial context v2의 테스트와 문서를 통합하라.

너의 소유 범위:
* tests/overlay-operators.test.ts
* tests/recognizer-v15.test.ts
* docs/20_queue/tutorial-personalization-plan.md
* docs/20_queue/tutorial-personalization-prompt-pack.md
* docs/20_queue/work-queue.md
* docs/30_tasks/epic-02-symbols-and-input/*
* docs/30_tasks/epic-07-future-expansion-backlog/*

필수 요구:
* placement-aware personalization이 hard-negative를 개선하는 테스트를 추가한다.
* off-anchor rescue 금지, dependency 유지, clear template no-flip 회귀를 포함한다.
* 새 task `T02-07`, backlog `T07-07`을 추가한다.
* 문서는 실제 구현보다 앞서 나가지 않게 쓴다.
```

### 7-6. 최종 검수 프롬프트

```text
현재 작업 트리를 기준으로 operator tutorial context v2가 원본 방향을 유지한 채 들어갔는지 검수하라.

확인 항목:
* operator tutorial capture에 `baseSnapshot/operatorContext`가 저장되는가
* old tutorial data와 backward compatible한가
* placement-aware personalization이 overlay top-k rerank에만 작동하는가
* off-anchor / wrong-scale 입력을 personalization이 rescue하지 않는가
* `martial_axis requires void_cut`가 그대로 유지되는가
* clear operator template가 bias profile 때문에 뒤집히지 않는가
* 문서와 queue/task가 실제 구현 상태와 맞는가

실행:
* 가능한 테스트 실행
* npm run build
* npm run validate:docs

보고 형식:
* 충족된 점
* 부족한 점
* 남은 리스크
```

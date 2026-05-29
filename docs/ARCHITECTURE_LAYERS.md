# Architecture Layers

이 문서는 `Magic Recognizer V1.5`와 `Magic Exam Hall`을 같은 저장소 안에서 개발할 때 지켜야 할 계층 경계입니다. 목표는 Web 연구 프로토타입, Unity 게임, 인식 알고리즘, ML/파인튜닝 실험이 서로 발목을 잡지 않게 만드는 것입니다.

## 1. 결론

프로젝트는 "인식 기능과 게임"만 둘로 나누기보다, 아래처럼 전체를 계층화한다.

```text
L6 Release and QA
L5 Research, Dataset, ML Training
L4 Platform Apps
L3 Game Runtime
L2 Recognition Runtime
L1 Shared Contract
L0 Product Design
```

`Input Capture`는 별도 최상위 제품 계층이라기보다 Platform Apps와 Recognition Runtime 사이의 handoff boundary다. Unity/Web의 device API를 읽는 구현은 platform-facing code에 가깝지만, 그 산출물은 Shared Contract의 `StrokeSession` 모양으로 고정한다.

의존성 방향은 항상 아래에서 위로만 간다.

```text
Product Design
  -> Shared Contract
  -> Recognition Runtime
  -> Game Runtime
  -> Platform Apps
  -> Release and QA

Research/Dataset/ML은 Shared Contract와 Recognition Runtime을 사용하지만,
Unity 게임 플레이 코드가 Research/Dataset/ML 코드에 직접 의존하지 않는다.
```

## 2. 현재 상태 진단

### Web line

현재 Web 쪽은 연구실에 가깝다.

| 역할 | 현재 위치 |
| --- | --- |
| 제스처 입력 데모 | `src/app.ts`, `src/main.ts`, `src/demo-layer.ts` |
| family/operator 인식 | `src/recognizer/*` |
| quality/profile/personalization | `src/recognizer/quality.ts`, `src/recognizer/user-profile.ts`, `src/recognizer/tutorial-profile.ts` |
| dynamic policy | `src/recognizer/dynamic-gating.ts` |
| tiny ML artifact runtime | `src/recognizer/rerank.ts` |
| dashboard/what-if/tutorial lab | `src/demo/*` |
| survey/data conversion/ML scripts | `src/survey/*`, `scripts/*`, `artifacts/ml/*` |

문제는 `src/recognizer/rerank.ts`가 artifact loading, feature row 구성, model evaluation, personalization, shadow summary를 모두 들고 있다는 점이다. 팀원이 AI나 fine-tuning 모델을 연결하려면 이 파일 내부 구현을 직접 알아야 한다.

### Unity line

현재 Unity 쪽은 플레이어블 게임에 가깝다.

| 역할 | 현재 위치 |
| --- | --- |
| base 제스처 인식 | `SpellRecognition.cs` |
| overlay 인식과 seal runtime | `SpellRuntime.cs` |
| world drawing, floor, goal, HUD | `ExamGameController.cs`, `WorldDrawingController.cs` |
| pixel art runtime | `PixelArtFactory.cs`, `PixelSpriteView.cs` |
| hint/logging | `HintAssistance.cs`, `ExamLogging.cs` |

문제는 `SpellRecognition.cs`와 `SpellRuntime.cs` 안에 contract, heuristic recognizer, result model, label helper가 같이 있다. Unity에는 Web의 tiny ML, dynamic policy, shadow 판독을 끼울 명시적 adapter가 없다.

## 3. 목표 계층

### L0. Product Design

게임이 어떤 경험이어야 하는지 정한다.

| 포함 | 제외 |
| --- | --- |
| 게임 판타지, 층별 목표, family/operator 의미, UX 원칙 | TypeScript/C# 구현 세부, ML 모델 구조 |

현재 기준 파일:

- `docs/GAME_DESIGN.md`
- `docs/PROJECT_ROADMAP.md`

이 계층은 모든 구현의 이유를 제공하지만 코드에 import되지 않는다.

### L1. Shared Contract

Web과 Unity가 반드시 같은 뜻으로 써야 하는 약속을 정의한다.

포함 대상:

- `GlyphFamily` / `SpellFamily`
- `OverlayOperator`
- `RecognitionStatus`
- `StrokeSession` / stroke sample schema
- `QualityVector`
- base recognition result schema
- overlay recognition result schema
- final seal schema
- attempt/survey log schema
- ML feature row spec

현재 후보 파일:

- TypeScript: `src/recognizer/types.ts`
- Unity: `SpellRecognition.cs`, `SpellRuntime.cs`
- ML: `artifacts/ml/feature-spec-v1.json`
- 로그: `ExamLogging.cs`, `src/survey/survey-contract.ts`
- 문서: `docs/RECOGNITION_CONTRACT.md`, `docs/INPUT_LAYER_HANDOFF.md`

목표:

```text
contracts/
  recognition-contract.md
  recognition-schema.json
  log-schema.md

src/recognizer/contracts.ts
unity/MagicExamHall/.../Contracts/*.cs
```

중요 규칙:

1. contract는 판정을 하지 않는다.
2. contract는 Unity scene, Web DOM, Vite, ML training script를 모른다.
3. enum/string 이름은 Web과 Unity가 1:1 대응해야 한다.
4. 새 family/operator/status를 추가하면 이 계층부터 갱신한다.

### L2. Recognition Runtime

stroke를 해석해 recognition result를 만든다.

포함 대상:

- input capture가 넘긴 `StrokeSession` 수신
- geometry normalize
- feature extraction
- heuristic candidate scoring
- quality calculation
- overlay anchor/scale/dependency 검사
- dynamic recognition policy
- user profile / personalization 보정
- optional ML/shadow adapter

현재 Web 후보:

- `src/recognizer/geometry.ts`
- `src/recognizer/feature-v2.ts`
- `src/recognizer/gesture-matcher.ts`
- `src/recognizer/quality.ts`
- `src/recognizer/recognize.ts`
- `src/recognizer/operators.ts`
- `src/recognizer/rerank.ts`
- `src/recognizer/dynamic-gating.ts`

현재 Unity 후보:

- `SpellRecognition.cs`
- `SpellRuntime.cs` 안의 `OverlayRecognizer`

중요 규칙:

1. Recognition Runtime은 raw mouse/touch/stylus event를 직접 읽지 않는다. input capture가 만든 `StrokeSession`만 받는다.
2. Recognition Runtime은 world goal을 완료시키지 않는다.
3. Recognition Runtime은 HUD 문구, 카메라, 사운드, 픽셀 스프라이트를 모른다.
4. ML은 최종 게임 효과를 직접 결정하지 않고 후보 score, confidence, shadow summary만 제공한다.
5. AI/fine-tuning 담당자는 이 계층의 adapter만 구현하면 된다.

권장 adapter 모양:

```text
RecognitionModelAdapter
  scoreBaseCandidates(input) -> candidate scores / confidence / shadow
  scoreOverlayCandidates(input) -> candidate scores / confidence / shadow
  getRuntimeStatus() -> artifact/model availability
```

기본 구현은 현재 tiny ML artifact를 감싸고, 외부 구현은 같은 interface를 구현한다.

### L3. Game Runtime

인식 결과를 게임 세계의 변화로 바꾼다.

포함 대상:

- base seal 생성/수명
- overlay stack 누적
- final seal compile
- floor goal 판정
- world state effect mapping
- hint escalation state
- attempt logging event 생성

현재 후보:

- Unity `ExamGameController.cs`
- Unity `HintAssistance.cs`
- Unity `ExamLogging.cs`
- Web `src/recognizer/compile.ts`
- Web demo의 outcome compare 일부

중요 규칙:

1. Game Runtime은 raw stroke를 직접 분석하지 않는다. 반드시 Recognition Runtime을 호출한다.
2. Game Runtime은 ML 모델 파일을 직접 읽지 않는다.
3. Game Runtime은 "인식 결과가 fire recognized" 같은 의미를 받아 "불꽃 시험 목표 반응"으로 바꾼다.
4. Game Runtime의 테스트는 "given recognition result, world changes correctly"를 검증한다.

### L4. Platform Apps

사용자가 실제로 만지는 앱이다.

| 앱 | 역할 |
| --- | --- |
| Unity Playable | 최종 플레이어블 게임 |
| Web Research Lab | recognizer 연구, dashboard, what-if, survey, tutorial 실험 |

Unity App 포함:

- scene
- MonoBehaviour controller
- camera
- player movement
- device input capture
- HUD
- generated pixel sprites
- sound/visual effect

Web App 포함:

- DOM/canvas
- demo presets
- dashboard
- survey UI
- datacard authoring/preview

중요 규칙:

1. Unity와 Web은 서로 import하지 않는다.
2. 두 앱은 Shared Contract와 각자의 adapter를 통해 같은 개념을 사용한다.
3. Web은 연구용 기준선이지만 최종 게임의 UX를 강제하지 않는다.
4. Unity는 최종 플레이어블이지만 연구/검증 도구를 직접 들고 있지 않는다.

### L5. Research, Dataset, ML Training

인식기를 개선하기 위한 실험 계층이다.

포함 대상:

- survey API
- tutorial capture export
- public dataset conversion
- synthetic data generation
- ML baseline train/export
- dashboard plot/report
- dynamic policy calibration

현재 위치:

- `scripts/tutorial-dataset/*`
- `scripts/ml-baseline/*`
- `scripts/survey-guardrail-ml-experiment.ts`
- `scripts/*plots.py`
- `artifacts/ml/*`
- `src/demo/dashboard-*`
- `src/survey/*`

중요 규칙:

1. 이 계층은 게임 런타임에 직접 들어가지 않는다.
2. 결과물은 versioned artifact나 문서화된 contract로만 내려보낸다.
3. raw/private data와 contact data는 분리한다.
4. 모델이 family/operator 의미를 임의로 바꾸면 안 된다.

### L6. Release and QA

배포 후보가 실행 가능한지 검증한다.

포함 대상:

- Web tests/build
- Unity EditMode/PlayMode tests
- Unity player build
- player smoke
- manual 5-floor run
- release checklist
- privacy/log review

현재 기준:

- `docs/RELEASE_CHECKLIST.md`
- `README.md`
- Unity test asmdefs
- Vitest tests

## 4. 인식 기능과 게임 기능의 경계

가장 중요한 경계는 아래와 같다.

```text
Device input
  -> Input Capture
  -> StrokeSession
  -> Recognition Runtime
  -> RecognitionResult
  -> Game Runtime
  -> WorldEffect / Progress / Feedback Event
  -> Platform UI
```

Recognition Runtime이 반환해야 하는 것:

- status
- recognized family/operator
- confidence
- quality vector
- failure reason id 또는 reason summary
- next hint basis
- shadow/dynamic policy summary
- normalized/debug geometry, 필요한 경우에만

Game Runtime이 결정해야 하는 것:

- 주문이 어떤 목표 오브젝트에 닿았는지
- floor goal이 완료됐는지
- 어떤 world effect를 발생시킬지
- 어떤 hint level을 보여줄지
- 로그에 어떤 gameplay context를 붙일지

Platform UI가 결정해야 하는 것:

- HUD 배치
- 텍스트 표시 방식
- 색상/애니메이션/사운드
- 입력 장치별 조작감

player input 경계는 `docs/INPUT_LAYER_HANDOFF.md`를 기준으로 분리한다. 좁은 입력 캡처 계층은 raw device event를 `StrokeSession`까지 만들고, recognition 계층은 session을 family/operator/status/confidence가 담긴 `RecognitionResult`로 바꾼다. 사용자의 선 습관을 반영하는 personalization/model policy도 이 recognition 경계에 붙는다. 게임 계층은 완성된 recognition result 이후만 처리한다.

## 5. AI/fine-tuning 연결 경계

팀원이 AI나 fine-tuning 기능을 연결한다면 목표 지점은 Recognition Runtime 안의 model adapter다.

팀원이 받는 입력:

- normalized stroke/session
- heuristic candidates
- feature vector
- quality vector
- base/overlay context
- optional user profile summary

팀원이 돌려주는 출력:

- 후보별 score delta 또는 probability
- calibrated confidence
- ambiguity probability
- shadow top label
- model/runtime status
- debug metadata, 개발 모드에서만

팀원이 결정하면 안 되는 것:

- floor goal 완료 여부
- world effect 종류
- player assist penalty
- narrative/codex 내용
- release/privacy 정책

첫 구현 목표는 "ML이 판정을 뒤집는 구조"가 아니라 "ML이 shadow summary와 confidence를 제공하고, dynamic policy가 보수적으로 반영하는 구조"다.

## 6. 권장 디렉터리 목표

장기 목표는 아래 구조다.

```text
docs/
  ARCHITECTURE_LAYERS.md
  GAME_DESIGN.md
  INPUT_LAYER_HANDOFF.md
  PROJECT_ROADMAP.md
  RECOGNITION_CONTRACT.md
  RELEASE_CHECKLIST.md

contracts/
  recognition-contract.md
  recognition-schema.json
  log-schema.md

src/
  recognizer/
    contracts.ts
    recognition-service.ts
    model-adapter.ts
    heuristic/
    ml/
    policy/
  demo/
  survey/

scripts/
  tutorial-dataset/
  ml-baseline/
  reports/

artifacts/
  ml/

unity/MagicExamHall/Assets/MagicExamHall/Scripts/
  Contracts/
  Input/
  Recognition/
  Personalization/
  Game/
  Presentation/
  Runtime/
```

단번에 파일을 모두 옮기지 않는다. 테스트가 있는 단위부터 작게 이동한다.

## 7. 1차 리팩터 순서

### Step 1. Contract를 먼저 고정

완료 조건:

- family/operator/status 대응표 작성
- quality vector 대응표 작성
- stroke/result/log schema 작성
- Web과 Unity의 명칭 차이 목록 작성

현재 1차 반영 상태:

- `docs/RECOGNITION_CONTRACT.md`가 GitHub issue #15 / #41 기준의 초안으로 추가됐다.
- base family, overlay operator, status, stroke, quality, result, attempt log 대응표를 포함한다.
- Unity model/shadow adapter와 adjusted quality는 후속 미구현 항목으로 남겼다.

### Step 2. Unity player input and recognition handoff를 고정한다

완료 조건:

- Web의 `PointSample` / `Stroke` / `StrokeSession`과 Unity 입력 DTO 대응표를 작성한다.
- `WorldDrawingController`가 가진 입력, buffer, visual, game handoff 책임을 분리 대상으로 표시한다.
- 입력 캡처 계층이 구현할 interface와 recognition 계층이 받을 event를 정한다.
- 선/형태 판정, quality, confidence, personalization은 recognition 계층으로 분리한다.
- 게임 계층이 받을 recognition result 경계를 정한다.
- 첫 Unity 구현 PR 순서를 작게 나눈다.

현재 1차 반영 상태:

- `docs/INPUT_LAYER_HANDOFF.md`가 추가됐다.
- device input부터 stroke session까지의 입력 캡처 경계와, stroke session 이후 recognition/personalization 경계를 문서화했다.
- Unity 쪽 목표 파일 구조, DTO, source interface, session buffer, recognition service, presentation 분리 기준을 포함한다.
- 이번 PR은 C# 구현을 바꾸지 않고 다음 리팩터 PR의 기준선을 만든다.

### Step 3. Unity Recognition service 경계 생성

완료 조건:

- `IBaseGestureRecognizer`
- `IOverlayGestureRecognizer`
- `HeuristicBaseGestureRecognizer`
- `HeuristicOverlayRecognizer`
- recognition service/coordinator가 static recognizer 호출을 감싼다
- `ExamGameController`는 raw stroke나 recognizer 내부 구현 대신 recognition result를 받는다

### Step 4. Unity assembly 계층 분리

완료 조건:

- `MagicExamHall.Contracts`
- `MagicExamHall.Input`
- `MagicExamHall.Recognition`
- `MagicExamHall.Personalization`
- `MagicExamHall.Game`
- `MagicExamHall.Presentation`
- `MagicExamHall.Runtime`

초기에는 asmdef만 무리하게 쪼개지 말고, C# namespace와 폴더부터 이동한다.

### Step 5. Cross-platform regression fixture

완료 조건:

- 같은 stroke fixture를 Web/Unity에서 읽을 수 있는 JSON으로 저장
- canonical success, incomplete, ambiguous, invalid 케이스를 양쪽에서 비교
- 차이가 있으면 허용 차이 또는 platform-specific reason을 기록

## 8. 금지할 결합

아래 결합은 만들지 않는다.

| 금지 | 이유 |
| --- | --- |
| Unity `ExamGameController`가 `artifacts/ml/*.json`을 직접 읽음 | 게임 런타임과 ML 실험 계층이 붙는다 |
| ML script가 Unity scene/prefab을 수정 | 연구 계층이 앱 계층을 침범한다 |
| Web demo copy를 Unity HUD가 그대로 import | 플랫폼 UI가 서로 결합된다 |
| recognition result가 floor goal 완료를 포함 | 인식과 게임 규칙이 섞인다 |
| profile/personalization이 invalid 입력을 성공으로 강제 | 공정성 원칙이 깨진다 |
| log schema가 플랫폼마다 임의로 늘어남 | 분석과 제출 검증이 어려워진다 |

## 9. 작업 판단 기준

새 기능을 만들 때 먼저 아래 질문을 한다.

1. 이것은 raw device event를 stroke session으로 만드는가? 그러면 Platform Apps 안의 Input Capture boundary.
2. 이것은 stroke session의 의미를 해석하는가? 그러면 Recognition Runtime.
3. 이것은 사용자 입력 습관, threshold, model/shadow 보조 판독을 다루는가? 그러면 Recognition Runtime 안의 Personalization/model policy.
4. 이것은 세계 상태를 바꾸는가? 그러면 Game Runtime.
5. 이것은 화면에 보여주는가? 그러면 Platform App 또는 Presentation.
6. 이것은 데이터를 모으거나 모델을 학습하는가? 그러면 Research/Dataset/ML.
7. 이것은 Web과 Unity가 같은 뜻으로 써야 하는가? 그러면 Shared Contract부터 갱신.

이 기준으로 기능 위치를 정한 뒤 구현한다.

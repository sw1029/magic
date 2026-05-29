# Project Roadmap

이 문서는 `Magic Recognizer V1.5`와 Unity 프로토타입 `Magic Exam Hall`을 하나의 게임 프로젝트로 이어가기 위한 기준 로드맵입니다. 이 문서 하나만 읽어도 최종 게임이 어떤 모습인지, 어떤 기능이 있어야 하는지, 현재 어디까지 구현됐는지, 다음에 무엇을 해야 하는지 파악할 수 있도록 정리합니다.

## 1. 기준 정보

| 항목 | 내용 |
| --- | --- |
| 프로젝트명 | Magic Recognizer V1.5 / Magic Exam Hall |
| 최종 결과물 | 맵 위에 직접 마법 문양을 그려 주문을 시전하는 2D top-down HCI 퍼즐 어드벤처 |
| 핵심 플레이 | 이동, 맵 위 직접 드로잉, 세계 반응 관찰, 조합 실험, 목표 상태 해결, 노트 기록 |
| 핵심 연구 질문 | 기호 기반 입력이 재미와 몰입을 만들면서도 실패 이유를 납득 가능하게 설명할 수 있는가 |
| 주요 플랫폼 | Web prototype, Unity 6.3 LTS prototype |
| 작성 기준일 | 2026-05-14 |

### 소스 기준

현재 저장소에는 두 구현 흐름이 함께 존재합니다.

| 구현 흐름 | 기준 | 의미 |
| --- | --- | --- |
| Web prototype | `origin/main` 최신 참조 | recognizer, overlay, tutorial, dashboard, datacard, survey 기능이 가장 많이 반영된 기준선 |
| Unity game prototype | `unity/MagicExamHall` | 실제 게임 플레이 형태인 2D 시험장 vertical slice |

주의: 로드맵은 Web 연구 프로토타입과 Unity 플레이어블을 함께 다루며, 구현 상태 표에서는 Web과 Unity를 분리합니다.

## 2. 최종 게임 한 문장

플레이어는 떠 있는 마법탑의 입학생이 되어 맵 바닥에 직접 문양을 그리고, 문양의 형태와 품질에 따라 세계 상태를 바꾸며, 정해진 레시피가 아니라 관찰과 조합으로 시험을 통과하는 실시간 마법 드로잉 퍼즐 어드벤처입니다.

## 3. 완성된 게임의 모습

### 화면과 분위기

- 시점: 2D top-down.
- 아트 방향: JRPG풍 픽셀 아트 시험장.
- 첫 화면: 마케팅 랜딩이 아니라 바로 플레이 가능한 시험장.
- 공간: 떠 있는 마법탑, 돌바닥, 벽화, 룬 장치, 움직이는 발판, 불안정한 마법진.
- 플레이어: 입학생 마법사.
- UI: HUD, 짧은 룬 라벨, 마법 노트 기록, 진행도, 사후 성찰 리포트.

### 기본 플레이 흐름

1. 플레이어가 떠 있는 마법탑에 입장한다.
2. 현재 층의 목표 상태가 HUD와 환경 연출로 표시된다.
3. WASD 또는 방향키로 이동하며 방을 관찰한다.
4. 우클릭을 누른 채 마우스가 지나가는 맵 바닥에 문양을 직접 그린다.
5. 짧은 입력 버퍼가 끝나면 stroke 묶음이 주문으로 해석된다.
6. 주문 효과는 문양 중심에서 발생하고 주변 세계 상태를 바꾼다.
7. 플레이어는 여러 base/overlay 조합을 실험해 목표 상태를 만족시킨다.
8. 실패하거나 약한 주문은 흐릿한 파문, 약한 효과, 마법 노트 관찰문으로 피드백된다.
9. 마지막 층에서 대형 마법진을 복구하면 성찰형 리포트가 표시된다.

### 최종 게임 루프

```text
관찰 -> 맵 위 직접 드로잉 -> 인식/품질 분석 -> 세계 반응 -> 조합 실험 -> 목표 상태 해결 -> 노트 기록
```

### 플레이어가 느껴야 하는 것

- 버튼을 누른 것이 아니라 직접 주문을 외운 느낌.
- 실패해도 억울하지 않고, 무엇을 고치면 되는지 알 수 있는 느낌.
- 같은 문양이라도 더 안정적으로 그리면 주문 결과가 좋아지는 느낌.
- 새로운 장식 문양을 얹어 주문을 확장하는 발견감.

## 4. 핵심 설계 원칙

| 원칙 | 설명 |
| --- | --- |
| Family와 quality 분리 | 문양 종류 판정은 형태가 결정하고, 품질은 실행 결과와 피드백에 영향을 준다. |
| 실패는 학습 데이터 | 실패 화면은 단순 실패가 아니라 닫힘, 각도, 속도, 안정감 등 다음 행동을 알려준다. |
| 보조는 단계적으로 | 첫 실패는 짧은 힌트, 반복 실패는 체크리스트, 이후에는 강한 보조선을 제공한다. |
| 개인화는 신중하게 | 사용자 습관은 품질 보정과 후보 설명에 반영하되, 명백히 틀린 입력을 성공으로 뒤집지 않는다. |
| 조합은 base 이후 | 맵 위에 base seal을 먼저 만들고, 같은 위치 근처에 overlay operator를 후속으로 얹는다. |
| 세계 상태가 목표 | 시험은 특정 레시피 입력이 아니라 방의 목표 상태 달성으로 통과한다. |
| 연구 가능성 유지 | 시도, 품질, 힌트, 성공 여부, 설문을 로그로 남겨 HCI 검증이 가능해야 한다. |

## 5. 주문 체계

### Base family

| Family | 문양 | 게임 내 의미 | 현재 Web | 현재 Unity | 최종 역할 |
| --- | --- | --- | --- | --- | --- |
| `fire` | 위로 솟은 닫힌 삼각형 | 공격, 점화, 에너지 | 구현됨 | 구현됨 | 불꽃 시험, 공격 주문, 열 반응 |
| `water` | 둥근 닫힌 루프 | 순환, 정화, 회복 | 구현됨 | 구현됨 | 정화 시험, 불 진압, 보호막 |
| `wind` | 평행한 열린 선 3개 | 흐름, 이동, 밀기 | 구현됨 | 구현됨 | 흐름 시험, 장애물 밀기 |
| `earth` | 아래가 넓은 닫힌 사다리꼴 | 안정, 방어, 축조 | 구현됨 | 구현됨 | 축조 시험, 벽 생성 |
| `life` | 줄기와 상단 분기 Y | 성장, 연결, 생명력 | 구현됨 | 구현됨 | 생장 시험, 식물/씨앗 성장 |

### Overlay operator

| Operator | 문양/조건 | 의미 | 현재 Web | 현재 Unity | 최종 역할 |
| --- | --- | --- | --- | --- | --- |
| `steel_brace` | 오른쪽 근처 열린 브레이스 | 강화, 방어 | 구현됨 | 구현됨 | 방어력/지속시간 증가 |
| `electric_fork` | 꺾이고 갈라지는 번개형 | 분기, 전기 | 구현됨 | 구현됨 | 연쇄/전도 효과 |
| `ice_bar` | 수평 막대 | 냉기, 고정 | 구현됨 | 구현됨 | 빙결/정지 효과 |
| `soul_dot` | 작고 닫힌 점 | 집중, 핵심점 | 구현됨 | 구현됨 | 집중도/정밀도 증가 |
| `void_cut` | 짧은 대각선 절단 | 무효화, 절단 | 구현됨 | 구현됨 | 차단/해제 효과 |
| `martial_axis` | `void_cut` 이후 축선 | 전투 축, 후속 결합 | 구현됨 | 구현됨 | `void_cut` 이후 고급 공격 조합 |

### Final seal 규칙

1. base family를 먼저 확정한다.
2. base 확정 뒤 같은 기준 프레임 위에서 overlay를 그린다.
3. overlay는 위치, 크기, 의존성, 기존 operator stack을 함께 본다.
4. `martial_axis`는 `void_cut`이 먼저 있을 때만 활성화된다.
5. final seal은 `base + overlay stack + quality/profile delta`로 compile된다.

## 6. 현재 구현 현황 요약

상태 표기:

| 표기 | 의미 |
| --- | --- |
| 구현됨 | 코드와 테스트 또는 수동 검증 흐름이 존재한다. |
| 부분 구현 | 핵심 일부는 있으나 최종 게임 기준으로 연결이 부족하다. |
| 미구현 | 설계 대상이나 아직 코드가 없다. |
| 정리 필요 | 기능은 있으나 문서, 브랜치, 상태 표기가 어긋나 있다. |

### Web prototype 상태

| 영역 | 상태 | 근거/파일 |
| --- | --- | --- |
| 웹 캔버스 입력 | 구현됨 | `src/app.ts`, `src/main.ts` |
| 5개 base family 인식 | 구현됨 | `src/recognizer/recognize.ts`, `tests/recognizer.test.ts` |
| raw/adjusted quality | 구현됨 | `src/recognizer/quality.ts`, `src/recognizer/user-profile.ts` |
| user input profile | 구현됨 | `src/recognizer/user-profile.ts`, `src/recognizer/tutorial-profile.ts` |
| overlay operator 인식 | 구현됨 | `src/recognizer/overlay.ts`, `tests/overlay-operators.test.ts` |
| final seal compile | 구현됨 | `src/recognizer/compile.ts`, `tests/recognizer-v15.test.ts` |
| enclosing seal ring 감지 | 구현됨 | `src/recognizer/seal.ts`, `tests/seal.test.ts` |
| quality off/on compare | 구현됨 | `src/demo-layer.ts`, `src/demo/outcome-summary.ts` |
| why/explain panel | 구현됨 | `src/demo/explain.ts`, `tests/explain.test.ts` |
| guided tutorial flow | 구현됨 | `src/demo/tutorial-flow.ts`, `tests/tutorial-flow.test.ts` |
| recent seals/quick compare | 구현됨 | `src/demo/outcome-summary.ts`, `src/app.ts` |
| dashboard synthetic tests | 구현됨 | `src/demo/dashboard-*`, `tests/dashboard-*.test.ts` |
| datacard authoring/shape lab | 구현됨 | `src/recognizer/datacards.ts`, `src/recognizer/datacard-*` |
| what-if preview | 구현됨 | `src/demo/what-if.ts`, `src/demo/what-if-preview.ts` |
| survey contract/server | 구현됨 | `src/survey/*`, `scripts/survey-api-server.mjs` |
| ML baseline artifacts/scripts | 구현됨 | `artifacts/ml/*`, `scripts/ml-baseline/*` |

### Unity prototype 상태

| 영역 | 상태 | 근거/파일 |
| --- | --- | --- |
| Unity 프로젝트 골격 | 구현됨 | `unity/MagicExamHall` |
| 2D top-down 시험장 | 구현됨 | `Assets/Scenes/MagicExamHall.unity`, scene builder |
| 플레이어 이동 | 구현됨 | `ExamGameController.TickPlayer` |
| 5층 진행 루프 | 구현됨 | `FloorDefinition.BuildAll`, `ExamGameController` |
| 패널형 드로잉 | 제외됨 | 기본 플레이는 별도 입력 패널 없이 월드 드로잉을 사용 |
| 맵 위 직접 드로잉 | 구현됨 | `WorldDrawingController` |
| 5개 base family 인식 | 구현됨 | `SpellRecognition.cs`, `GestureRecognizerTests.cs` |
| 품질 벡터 | 구현됨 | `QualityAnalyzer` |
| 실패 이유/다음 힌트 | 구현됨 | `GestureRecognizer.BuildReason`, `BuildHint` |
| 보조 단계 escalator | 구현됨 | `HintAssistance.cs` |
| 성공 이펙트/진행도 | 구현됨 | `ExamGameController.cs`, `PixelArtFactory.cs` |
| 시도 로그 CSV/JSONL | 구현됨 | `ExamLogging.cs` |
| 엔딩 리포트/자동 설문 로그 | 구현됨 | `EndingReport`, `ExamLogging.cs` |
| EditMode/PlayMode 테스트 | 구현됨 | `Assets/Tests/*` |
| overlay operator | 구현됨 | `OverlayRecognizer`, `GestureRecognizerTests.cs` |
| final seal compile | 부분 구현 | base seal에 overlay stack을 붙이고 world goal 효과에 반영 |
| user profile 보정 | 미구현 | Unity 품질 분석은 있으나 web profile 정책 미연결 |
| datacard/what-if/dashboard | 미구현 | Web 검증 도구로만 존재 |

### 문서/운영 상태

| 영역 | 상태 | 메모 |
| --- | --- | --- |
| `docs/PROJECT_OVERVIEW.md` | 구현됨 | 프로젝트 개요와 설계 질문 정리 |
| `README.md` | 정리 필요 | 실제 docs 구조와 일부 안내가 다름 |
| `docs/20_queue/work-queue.md` | 정리 필요 | 큐 파일은 있으나 task 문서가 없음 |
| `docs/30_tasks/README.md` | 정리 필요 | task 문서 규칙만 있음 |
| GitHub workflow/PR template | 구현됨 | CI, CodeQL, issue/PR template 존재 |
| 검증 스크립트 | 구현됨 | 현재는 `validated 0 task documents against work queue` |

## 7. 최종 기능 명세

### 플레이 기능

| 기능 | 최종 요구 | 현재 상태 |
| --- | --- | --- |
| 마법탑 탐색 | 플레이어가 맵을 이동하고 층별 목표를 관찰 | Unity 구현됨 |
| 문양 입력 | 맵 바닥에 우클릭으로 직접 stroke 입력 | Unity 구현됨 |
| 기본 주문 | 5개 base family 인식과 성공/실패 판정 | Web/Unity 구현됨 |
| 고급 주문 | base 이후 overlay operator 누적 | Web/Unity 구현됨 |
| final seal | base와 overlay stack을 최종 주문으로 compile | Web 구현됨, Unity 부분 구현 |
| 주문 효과 | family/operator에 맞는 세계 변화와 피드백 | Unity 부분 구현 |
| 진행도 | 5개 시험 완료 상태 표시 | Unity 구현됨 |
| 재시도 | 실패 후 다시 그리기 | Unity 구현됨 |
| 힌트 단계 | 실패 횟수에 따라 힌트 강도 상승 | Unity 구현됨 |
| 결과 비교 | 현재/이전 시도, quality off/on 비교 | Web 구현됨, Unity 미구현 |

### 인식 기능

| 기능 | 최종 요구 | 현재 상태 |
| --- | --- | --- |
| 형태 특징 추출 | stroke count, closure, corner, circularity, fill ratio, parallelism | Web/Unity 구현됨 |
| 품질 벡터 | closure, smoothness, tempo, stability, rotationBias 등 | Web/Unity 구현됨 |
| 개인화 보정 | 반복 입력 profile 기반 adjusted quality와 후보 보조 | Web 구현됨, Unity 미구현 |
| shadow 판독 | 실제 판정과 별도 보조 판독/비교 | Web 구현됨, Unity 미구현 |
| 잘못된 입력 차단 | incomplete/invalid가 personalization으로 성공 처리되지 않음 | Web/Unity 일부 구현 |
| operator dependency | `martial_axis`는 `void_cut` 이후만 | Web/Unity 구현됨 |
| anchor zone | overlay 위치/크기/기준 프레임 검사 | Web/Unity 구현됨 |

### 튜토리얼/피드백 기능

| 기능 | 최종 요구 | 현재 상태 |
| --- | --- | --- |
| 모범 문양 보기 | family/operator별 exemplar chip | Web 구현됨 |
| 입력 중 feedforward | 안내선, ghost trace, anchor guide | Web/Unity 일부 구현 |
| 입력 후 feedback | 상태, 인식 family, confidence, 품질 지표, 이유 | Web/Unity 구현됨 |
| why panel | 왜 이 결과가 나왔는지 자연어 설명 | Web 구현됨 |
| checklist | family별 개선 체크리스트 | Unity 구현됨 |
| what-if | 구조/위치/순서를 바꾸면 어떤 위험이 생기는지 미리보기 | Web 구현됨 |

### 연구/검증 기능

| 기능 | 최종 요구 | 현재 상태 |
| --- | --- | --- |
| 시도 로그 | 세션, trial, target, recognized, status, quality, hint, assist 기록 | Unity 구현됨 |
| 설문 | 명확성, 공정성, 피드백 도움, 조작감, 몰입감, 자유 의견 | Unity 구현됨 |
| Web survey | 실험 그룹, prompt word, guess, engine comparison, interaction metrics | Web 구현됨 |
| dashboard | synthetic input batch, confusion/overlap/quality plot | Web 구현됨 |
| dataset conversion | tutorial/public dataset 변환과 ML manifest | Web scripts 구현됨 |
| privacy guardrail | raw/private data와 contact payload 분리 | Web survey contract 구현됨 |

## 8. 아키텍처 로드맵

상세 계층 경계와 리팩터 순서는 `docs/ARCHITECTURE_LAYERS.md`를 기준으로 한다. Web/Unity 인식 값과 로그 대응은 `docs/RECOGNITION_CONTRACT.md`를 기준으로 한다. 이 로드맵은 제품/마일스톤 관점의 요약이고, 계층 의존성 규칙은 architecture 문서가 우선한다.

### 현재 구조

```text
Web line
  src/recognizer/       TypeScript recognizer core
  src/demo/             HCI demo, tutorial, dashboard, what-if
  src/survey/           survey contract and small engines
  scripts/              docs, dataset, survey API, ML baseline
  artifacts/ml/         versioned baseline artifacts

Unity line
  unity/MagicExamHall/Assets/MagicExamHall/Scripts/Core
                        C# gesture recognition, quality, hints, logging
  unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime
                        scene controller, floor goals, world drawing, pixel sprites
  unity/MagicExamHall/Assets/Tests
                        EditMode and PlayMode tests
```

### 목표 구조

```text
Shared design contract
  Base family spec
  Overlay operator spec
  Quality vector spec
  Log schema
  Tutorial/feedback copy

Web research lab
  fast recognizer iteration
  dashboard and survey
  datacard authoring
  what-if exploration

Unity playable game
  player movement and scene
  floor goal and spell interaction
  final seal gameplay
  polished feedback and logging
```

### 핵심 통합 방향

1. Web recognizer를 설계 기준으로 유지한다.
2. Unity recognizer는 플레이 가능한 게임용 구현으로 유지하되, family/operator/quality/log schema를 Web과 맞춘다.
3. operator, final seal, profile 정책은 Web에서 검증한 뒤 Unity에 이식한다.
4. datacard는 최종적으로 튜토리얼 문구와 문양 규칙의 단일 소스가 되도록 한다.

## 9. 마일스톤

### M0. 기준 문서와 브랜치 정리

목표: 이 문서를 기준으로 프로젝트 상태를 재정렬한다.

| 작업 | 상태 | 완료 조건 |
| --- | --- | --- |
| 통합 로드맵 작성 | 구현됨 | `docs/PROJECT_ROADMAP.md` 존재 |
| README의 잘못된 docs 안내 수정 | 구현됨 | 실제 docs 구조와 검증 결과가 일치 |
| work queue/task 문서 정책 결정 | 미구현 | task 문서를 만들지, 큐를 단순화할지 결정 |
| Unity 브랜치와 최신 `origin/main` 통합 계획 수립 | 구현됨 | Unity 플레이어블이 `main`에 통합됨 |

### M1. Playable Unity vertical slice

목표: Unity 시험장 프로토타입을 맵 위 직접 드로잉 중심의 짧은 플레이어블로 안정화한다.

| 작업 | 상태 | 완료 조건 |
| --- | --- | --- |
| 시험장 scene 구성 | 구현됨 | 카메라, 플레이어, 5층 월드, UGUI, EventSystem 존재 |
| 이동/상호작용 | 구현됨 | WASD/방향키 이동 |
| 패널형 문양 입력 UI | 제외됨 | 기본 플레이에서는 입력 패널을 사용하지 않음 |
| 맵 위 직접 드로잉 | 구현됨 | 우클릭 hold로 바닥에 stroke를 그리고 버퍼 종료 시 주문 판정 |
| 5개 family 인식 | 구현됨 | canonical sample 테스트 통과 |
| 실패 피드백 | 구현됨 | reason, hint, quality 지표 표시 |
| 힌트 escalator | 구현됨 | 1회 실패 reason hint, 2회 checklist, 3회 ghost trace |
| 세션 로그 | 구현됨 | attempts/survey CSV/JSONL 저장 |
| PlayMode smoke test | 구현됨 | scene load, world casting, overlay, hazard, ending report 검증 |
| 빌드 산출물 | 미구현 | Windows/WebGL 등 실행 파일 생성과 smoke test |

### M2. World casting and overlay/final seal Unity 이식

목표: Unity에서도 맵 위 직접 드로잉으로 base seal을 만들고, 같은 위치 근처에 overlay를 후속으로 얹어 final spell을 만든다.

| 작업 | 상태 | 완료 조건 |
| --- | --- | --- |
| 월드 드로잉 입력 | 구현됨 | 우클릭 hold/release와 stroke buffer로 주문군 생성 |
| 월드 stroke 시각화 | 구현됨 | 맵 바닥에 빛나는 선이 잠깐 남고 fade-out |
| base seal instance | 구현됨 | base 성공 시 문양 중심에 임시 seal 생성 |
| Unity overlay 입력 단계 | 구현됨 | base seal 근처 후속 stroke가 overlay로 붙음 |
| operator recognizer C# 이식 | 구현됨 | 6개 operator 판정과 테스트 |
| anchor zone 시각화 | 부분 구현 | anchor/scale 판정과 실패 힌트는 있으나 전용 guide visual은 없음 |
| dependency 처리 | 구현됨 | `martial_axis`는 `void_cut` 없으면 차단 |
| final spell feedback | 부분 구현 | 문양 위 룬 라벨과 overlay stack feedback 표시 |
| world state effect mapping | 구현됨 | 주문이 주변 오브젝트 상태를 바꾸고 방 목표에 기여 |

### M3. 튜토리얼과 피드백 고도화

목표: 처음 보는 사용자가 설명 없이도 시험을 완료하고 실패 이유를 이해한다.

| 작업 | 상태 | 완료 조건 |
| --- | --- | --- |
| family별 tutorial card copy 통합 | 부분 구현 | Web datacard 문구를 Unity hint/checklist에 반영 |
| exemplar/ghost trace 개선 | 부분 구현 | 각 family/operator의 모범선이 명확함 |
| why panel Unity 버전 | 미구현 | 결과 이유가 2-3문장으로 표시 |
| quality off/on compare Unity 버전 | 미구현 | 품질 반영 전후 효과 차이 표시 |
| 접근성/조작 옵션 | 미구현 | 마우스 민감도, 다시 그리기, 키 안내, 색 대비 확인 |

### M4. 게임 콘텐츠 확장

목표: 기술 데모를 넘어 짧은 게임 경험으로 느껴지게 한다.

| 작업 | 상태 | 완료 조건 |
| --- | --- | --- |
| 층별 세계 반응 | 구현됨 | base/overlay/combo 목표가 층별 진행에 기여 |
| overlay tutorial floor | 구현됨 | 2층에서 6개 overlay를 모두 실험 |
| final seal challenge | 부분 구현 | 5층 성좌심에서 base/combo 요구치를 채움 |
| 실패/성공 연출 | 부분 구현 | 사운드, 화면 shake, particle, goal animation |
| 엔딩 요약 | 미구현 | 완료 시간, 시도 수, assist 사용량, quality 평균 표시 |

### M5. 연구 검증 패키지

목표: HCI 프로젝트 산출물로 제출 가능한 검증 흐름을 완성한다.

| 작업 | 상태 | 완료 조건 |
| --- | --- | --- |
| 실험 프로토콜 문서 | 미구현 | 참가자 과제, 측정 항목, 진행 순서 정의 |
| 로그 스키마 고정 | 부분 구현 | Web/Unity 로그 필드 대응표 작성 |
| 설문 결과 export | 부분 구현 | CSV/JSONL 저장 확인 |
| dashboard 분석 리포트 | Web 구현됨 | synthetic stress와 실제 로그를 비교할 수 있음 |
| privacy/security 점검 | 부분 구현 | raw data/contact data 분리 원칙 문서화 |

### M6. Beta polish와 릴리스

목표: 외부 사람이 실행해도 망가지지 않는 제출/배포 버전.

| 작업 | 상태 | 완료 조건 |
| --- | --- | --- |
| Unity 빌드 자동화 | 미구현 | batchmode build script 또는 문서화 |
| Web demo 배포 | 미구현 | GitHub Pages 또는 정적 빌드 배포 |
| 최종 README 정리 | 미구현 | 실행법, 데모 흐름, 로드맵 링크 최신화 |
| 릴리스 체크리스트 | 미구현 | 테스트, 빌드, 로그, 개인정보, 스크린샷 확인 |
| 발표 자료 | 미구현 | 문제, 설계, 구현, 검증, 결과를 5-8분 흐름으로 정리 |

## 10. Epic backlog

### Epic A. Project state and documentation

| ID | 작업 | 우선순위 | 상태 |
| --- | --- | --- | --- |
| A1 | README의 docs 읽기 순서와 실제 파일 구조 동기화 | P0 | 미구현 |
| A2 | 작업 큐를 실제 task 문서와 연결하거나 단순 status 문서로 전환 | P0 | 미구현 |
| A3 | Web main 기능과 Unity 브랜치 기능의 merge/rebase 전략 결정 | P0 | 미구현 |
| A4 | 최종 게임명 결정: `Magic Exam Hall` 유지 또는 별도 게임명 | P1 | 미구현 |
| A5 | Web 연구, Unity 게임, 인식, ML 실험 계층 경계 문서화 | P0 | 구현됨 |

### Epic B. Core recognition contract

| ID | 작업 | 우선순위 | 상태 |
| --- | --- | --- | --- |
| B1 | family/operator/quality/log schema 대응표 작성 | P0 | 부분 구현 |
| B2 | Web과 Unity의 recognition status 명칭 통일 | P0 | 부분 구현 |
| B3 | Unity에 overlay operator recognizer 추가 | P0 | 미구현 |
| B4 | Unity에 profile/shadow policy 중 최소 설명용 summary 추가 | P1 | 미구현 |
| B5 | C#과 TypeScript recognizer의 regression fixture 공유 방식 결정 | P1 | 미구현 |
| B6 | Web recognition model adapter 경계 생성 | P1 | 미구현 |
| B7 | RECOGNITION_CONTRACT.md 초안 작성 | P0 | 구현됨 |
| B8 | INPUT_LAYER_HANDOFF.md로 Unity/Web 입력 계층 경계 문서화 | P0 | 구현됨 |

### Epic C. Unity gameplay

| ID | 작업 | 우선순위 | 상태 |
| --- | --- | --- | --- |
| C1 | 5층 vertical slice 안정화 | P0 | 구현됨 |
| C2 | final seal challenge 1개 추가 | P0 | 부분 구현 |
| C3 | family별 world effect 추가 | P1 | 구현됨 |
| C4 | overlay별 effect modifier 추가 | P1 | 부분 구현 |
| C5 | 엔딩 결과 요약 화면 추가 | P1 | 구현됨 |
| C6 | 실행 빌드 생성과 버전 표기 | P1 | 미구현 |

### Epic D. Tutorial and feedback

| ID | 작업 | 우선순위 | 상태 |
| --- | --- | --- | --- |
| D1 | 실패 이유 문구를 user-facing 용어로 정리 | P0 | 부분 구현 |
| D2 | 각 family/operator별 모범선과 체크리스트 통합 | P0 | 부분 구현 |
| D3 | Unity 결과 패널에 why summary 추가 | P1 | 미구현 |
| D4 | quality 지표를 플레이어 친화적 이름으로 표시 | P1 | 부분 구현 |
| D5 | 반복 실패 시 보조선 강도와 성공 후 assisted 기록 검증 | P1 | 구현됨 |

### Epic E. Research and data

| ID | 작업 | 우선순위 | 상태 |
| --- | --- | --- | --- |
| E1 | Unity attempt/survey log schema를 Web survey contract와 대응 | P0 | 부분 구현 |
| E2 | 테스트 참가자용 진행 시나리오 문서 작성 | P0 | 미구현 |
| E3 | 결과 분석 notebook 또는 dashboard export 작성 | P1 | 미구현 |
| E4 | raw capture 저장 정책 확정 | P1 | 미구현 |
| E5 | 제출용 anonymized sample log 포함 여부 결정 | P2 | 미구현 |

### Epic F. Web research lab

| ID | 작업 | 우선순위 | 상태 |
| --- | --- | --- | --- |
| F1 | dashboard 시나리오를 발표/검증용으로 정리 | P1 | 구현됨 |
| F2 | datacard preview lane을 최종 튜토리얼 source로 승격할지 결정 | P1 | 부분 구현 |
| F3 | Web UI와 Unity UI 용어 통일 | P1 | 미구현 |
| F4 | Web demo 배포 경로 결정 | P2 | 미구현 |

## 11. MVP 범위

### 반드시 포함

- Unity에서 바로 플레이 가능한 5층 시험장.
- Fire, Water, Wind, Earth, Life 5개 문양 입력과 성공/실패.
- 실패 이유, 품질 지표, 재시도, 단계적 힌트.
- 세션 로그와 설문 저장.
- 문서: 프로젝트 개요, 로드맵, 실행법, 검증법.

### 가능하면 포함

- Unity overlay/final seal 최소 1개 시나리오.
- Web recognizer dashboard로 안정성 시연.
- 완료 화면에서 시도 수, 도움 받은 횟수, 평균 품질 표시.
- 사운드와 간단한 파티클/주문 효과.

### 이번 버전에서 제외 가능

- 온라인 멀티플레이.
- 복잡한 전투 밸런싱.
- 대규모 맵/스테이지.
- 상용 수준 아트/사운드.
- ML 모델을 실제 최종 판정에 강하게 반영하는 구조.

## 12. Definition of Done

### 기능 단위 완료 조건

| 기능 종류 | 완료 조건 |
| --- | --- |
| recognizer | canonical 성공, incomplete/invalid 방어, 품질 변화 테스트가 있다. |
| UI | 플레이어가 설명 없이 주요 버튼을 찾고, 텍스트가 겹치지 않는다. |
| gameplay | 시작부터 완료/설문 저장까지 끊기지 않는다. |
| feedback | 실패 이유와 다음 행동이 한 화면에 보인다. |
| logging | 세션 ID, trial ID, target, recognized, status, quality, attempt, assist가 저장된다. |
| docs | 실행법, 검증법, 현재 상태, 남은 작업이 문서와 일치한다. |

### 릴리스 후보 완료 조건

1. Unity scene이 새 clone 환경에서 열린다.
2. Web prototype은 `npm test`, `npm run build`, `npm run validate:docs`가 통과한다.
3. Unity EditMode/PlayMode 테스트가 통과한다.
4. 5개 층을 사람이 직접 플레이해 완료할 수 있다.
5. 최소 1개 실패, 1개 성공, 1개 assisted success가 로그에 남는다.
6. 설문 CSV/JSONL이 저장된다.
7. README와 로드맵의 상태 표기가 실제 코드와 맞다.

## 13. 검증 계획

### 자동 검증

| 명령/도구 | 목적 |
| --- | --- |
| `npm run validate:docs` | docs queue/task 상태 확인 |
| `npm test` | Web recognizer/demo/survey 테스트 |
| `npm run build` | TypeScript 타입과 Vite build 확인 |
| Unity EditMode tests | GestureRecognizer, HintAssistance, ExamLogger 검증 |
| Unity PlayMode tests | scene load, world casting, overlay stack, hazard reset, ending report 검증 |

### 수동 플레이 테스트

1. Unity scene을 연다.
2. 플레이어가 첫 층의 목표 오브젝트 근처로 이동한다.
3. 일부러 틀린 입력을 넣어 첫 힌트를 확인한다.
4. 같은 목표에서 두 번 더 실패해 checklist/ghost trace를 확인한다.
5. 성공 입력을 넣어 다음 목표와 다음 층으로 진행한다.
6. 5개 층을 모두 완료한다.
7. 설문을 저장하고 로그 파일을 확인한다.

### HCI 측정 지표

| 지표 | 의미 |
| --- | --- |
| 첫 시도 성공률 | 모범 문양을 보고 바로 성공하는 비율 |
| 층 완료 시간 | 각 시험을 완료하는 데 걸린 시간 |
| 재시도 수 | 문양별 난이도와 피드백 효과 |
| assist level 사용률 | 힌트 escalator가 얼마나 필요한지 |
| 실패 피드백 이해도 | 사용자가 다음에 무엇을 고칠지 설명할 수 있는지 |
| 공정성 평가 | 실패/성공이 납득 가능했는지 |
| 몰입감 | 직접 마법을 시전한다고 느꼈는지 |

## 14. 위험과 대응

| 위험 | 영향 | 대응 |
| --- | --- | --- |
| Web과 Unity recognizer가 갈라짐 | 같은 입력인데 플랫폼별 결과가 달라질 수 있음 | family/operator/quality contract 문서화와 fixture 공유 |
| 브랜치 divergence | 최신 Web 기능과 Unity 기능이 충돌할 수 있음 | Unity 변경을 최신 main에 재적용하는 통합 브랜치 생성 |
| 문서와 실제 파일 구조 불일치 | 새 작업자가 혼란스러움 | README/docs queue 정리 |
| 맵 위 직접 드로잉 조작감이 불안정함 | 최종 게임 감각이 흐려질 수 있음 | 입력 버퍼와 seal 유지 시간을 플레이 테스트로 조정한다 |
| overlay 목표 판정이 느슨함 | 조합 실험이 위치 없는 체크리스트처럼 느껴질 수 있음 | 목표 오브젝트 근처 시전과 anchor/scale 피드백을 강화한다 |
| 피드백 문구가 기술적임 | 사용자가 원인을 이해하지 못함 | user-facing 용어 사전과 why summary 작성 |
| 로그가 개인정보/원시데이터를 과하게 담음 | 공개/제출 위험 | raw capture와 contact data 분리, anonymized export 유지 |
| 범위가 커짐 | 제출 가능한 게임 완성도가 떨어짐 | MVP는 맵 위 직접 드로잉 + 1개 완성 층 + 로그/리포트로 고정 |

## 15. 열린 결정 사항

| 질문 | 선택지 | 권장 |
| --- | --- | --- |
| 최종 이름 | `Magic Recognizer`, `Magic Exam Hall`, 새 게임명 | 게임은 `Magic Exam Hall`, 기술은 `Magic Recognizer`로 병기 |
| 최종 제출물 중심 | Web prototype, Unity game, 둘 다 | Unity를 플레이 결과물로, Web을 연구/검증 도구로 제출 |
| overlay 포함 범위 | Web만 유지, Unity 1개만, Unity 전체 이식 | 최소 Unity 1개 final seal challenge 포함 |
| 개인화 실제 반영 | 품질만 반영, 후보/threshold까지 반영, shadow only | MVP는 품질/설명 중심, 판정 변경은 신중하게 제한 |
| 로그 저장 범위 | attempts/survey만, raw strokes 포함 | 기본은 attempts/survey, raw strokes는 명시 동의/별도 모드 |

## 16. 다음 10개 작업

1. README의 존재하지 않는 docs/task 안내를 현재 구조에 맞게 수정한다.
2. Unity 브랜치를 최신 `origin/main` 위에 통합할 전략을 정한다.
3. `docs/GAME_DESIGN.md`를 기준으로 직접 드로잉 조작감과 층 구조를 더 구체화한다.
4. Web의 operator spec을 Unity C# contract로 옮길 표를 만든다.
5. Unity에 우클릭 월드 stroke 입력과 0.8초 입력 버퍼를 설계/구현한다.
6. base seal instance와 seal 근처 overlay 후속 입력 규칙을 플레이 테스트로 조정한다.
7. 세계 상태 목표 판정 모델을 세분화한다.
8. 1층 발착층을 맵 위 직접 드로잉 튜토리얼로 다듬는다.
9. 마법 노트 관찰문과 반복 실패 힌트 문구를 작성한다.
10. 사용자 테스트 시나리오와 성찰형 리포트 항목을 작성한다.

## 17. 유지보수 규칙

- 기능을 구현하면 이 문서의 해당 상태를 같은 PR에서 갱신한다.
- Web과 Unity 중 한쪽에만 구현된 기능은 반드시 구현 채널을 표기한다.
- 새 operator나 family를 추가할 때는 datacard, recognizer, tutorial, Unity UI, 로그 schema를 함께 확인한다.
- 실험/설문 항목을 바꾸면 schema version과 분석 문서를 함께 갱신한다.
- `README.md`는 빠른 시작용, `docs/GAME_DESIGN.md`는 게임 기획 기준 문서, 이 문서는 제품/로드맵 기준 문서로 유지한다.

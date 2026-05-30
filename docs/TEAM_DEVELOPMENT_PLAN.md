# Team Development Plan

이 문서는 `Magic Exam Hall`을 3명이 병렬로 개발하기 위한 역할 경계와 다음 구현 순서를 정리한다. 목적은 누가 더 많이 했는지를 따지는 것이 아니라, 서로의 코드를 덜 막으면서 같은 게임을 끝까지 완성하는 것이다.

관련 맥락:

- [PR #94 플레이어 입력/인식/게임 경계 문서화](https://github.com/sw1029/magic/pull/94)는 입력, 인식, 게임 런타임의 기술적 경계를 잡는 문서 PR이다.
- 이 문서는 그 경계를 실제 3인 개발 분담과 첫 스프린트 순서로 풀어쓴다.

## 1. 현재 판단

프로젝트는 아직 "완성된 게임"이 아니라, 다음 세 축이 함께 있는 상태다.

| 축 | 현재 성격 | 개발상 의미 |
| --- | --- | --- |
| Web recognizer lab | 제스처 인식, quality, profile, dashboard, survey를 빠르게 실험하는 연구/검증 라인 | 입력 규칙과 인식 정책을 검증하는 기준선 |
| Unity playable | 2D top-down 픽셀 아트 시험장, 월드 드로잉, 층별 목표, 로그가 연결된 게임 라인 | 최종 제출/시연에서 플레이어가 직접 만질 결과물 |
| Docs/design | 게임 정체성, 로드맵, release checklist, 작업 큐 | 팀이 같은 기준으로 구현하게 만드는 계약서 |

따라서 앞으로의 구조는 "Web 대 Unity"가 아니라 다음처럼 나누는 것이 맞다.

```text
Player Device Input
  -> Input Capture Layer
  -> Stroke Session Layer
  -> Recognition / Personalization Layer
  -> Game Runtime Layer
  -> Feedback / Tutorial / Logs Layer
```

핵심 원칙은 간단하다.

- 입력 담당은 플레이어의 stroke를 안정적으로 수집하고 인식 결과를 만든다.
- 게임 담당은 인식 결과를 받아 세계 상태, seal, floor goal, ending을 처리한다.
- 튜토리얼/콘텐츠 담당은 플레이어가 지금 무엇을 해야 하고 왜 실패했는지 이해하게 만든다.
- 서로의 내부 구현에 직접 의존하지 않고 작은 데이터 계약으로만 연결한다.

## 2. 레이어별 책임

### 2.1 Input Capture Layer

역할: 마우스/터치/태블릿/테스트 입력을 stroke sample로 수집한다.

담당 범위:

- pointer down/move/up 이벤트 처리.
- 화면 좌표를 월드 좌표 또는 normalized 좌표로 변환.
- stroke 시작/종료/취소 상태 관리.
- UI 위에서는 그리기 입력을 막는 처리.
- 디버그와 테스트를 위한 synthetic input source 제공.

하면 안 되는 것:

- base family를 직접 판정하지 않는다.
- seal을 생성하거나 floor goal을 완료하지 않는다.
- HUD 문구, 노트, 엔딩 리포트를 직접 변경하지 않는다.

산출물 예시:

```text
IStrokeInputSource
WorldPointerInputSource
SyntheticStrokeInputSource
StrokeSample
StrokePath
```

### 2.2 Stroke Session Layer

역할: 여러 stroke를 하나의 주문 입력 세션으로 묶는다.

담당 범위:

- 우클릭 release 이후 약 0.8초 동안 다음 stroke를 기다리는 입력 버퍼.
- wind처럼 여러 획이 필요한 문양을 같은 세션으로 묶기.
- 너무 짧거나 노이즈가 큰 stroke를 session metadata에 표시.
- stroke 중심, bounding box, duration, stroke count 같은 기본 메타데이터 계산.

하면 안 되는 것:

- 문양을 성공/실패로 최종 판정하지 않는다.
- "이 stroke는 불꽃이다" 같은 게임 의미를 확정하지 않는다.

산출물 예시:

```text
StrokeInputSession
StrokeSessionBuffer
StrokeSessionMetadata
```

### 2.3 Recognition / Personalization Layer

역할: stroke session을 base/overlay/invalid 결과로 해석하고, 품질과 실패 이유를 함께 반환한다.

담당 범위:

- base family 5종 인식: fire, water, wind, earth, life.
- overlay operator 6종 인식: steel_brace, electric_fork, ice_bar, soul_dot, void_cut, martial_axis.
- quality vector 계산: closure, smoothness, tempo, stability, rotation bias 등.
- confidence, reason, hint, reject reason 반환.
- user profile 또는 personalization 정책이 들어갈 경우 family 판정과 quality 보정을 분리.
- `martial_axis`처럼 의존성이 있는 overlay는 dependency 상태를 결과에 명시.

하면 안 되는 것:

- 인식 성공만으로 Unity 오브젝트를 생성하지 않는다.
- floor progress를 올리지 않는다.
- "1층 목표 완료" 같은 게임 규칙을 직접 알지 않는다.

게임으로 넘기는 결과는 다음 수준이면 충분하다.

```text
RecognitionResult
  status: recognized | invalid | incomplete | dependency_missing
  kind: base | overlay
  family?: fire | water | wind | earth | life
  operator?: steel_brace | electric_fork | ice_bar | soul_dot | void_cut | martial_axis
  confidence: 0..1
  quality: QualityVector
  center: world position
  bounds: world bounds
  reason: short technical reason
  playerHint: short user-facing hint
```

### 2.4 Game Runtime Layer

역할: 인식 결과를 게임 세계의 변화로 바꾼다.

담당 범위:

- base recognition result를 받아 seal instance 생성.
- seal lifetime, 위치, 주변 목표 탐색, target radius 처리.
- overlay recognition result를 기존 seal의 operator stack에 부착.
- floor goal progress 계산.
- 1층 base 목표, 2층 overlay 목표, 3층 조합 목표, 4층 hazard, 5층 final seal challenge 처리.
- 성공/실패에 따른 world effect와 game state 갱신.
- ending report와 survey/log 저장 트리거.

하면 안 되는 것:

- pointer event를 직접 해석하지 않는다.
- recognizer 내부 threshold를 직접 조정하지 않는다.
- profile 학습 데이터를 직접 수정하지 않는다.

산출물 예시:

```text
SpellCastingService
SealInstance
SealOverlayStack
FloorGoalSystem
WorldEffectResolver
EndingReportController
```

### 2.5 Feedback / Tutorial / Logs Layer

역할: 플레이어가 현재 상황을 이해하도록 화면, 문구, 노트, 로그를 정리한다.

담당 범위:

- 1층에서 물/바람/불/땅/생명의 목표 표식이 헷갈리지 않게 보이게 하기.
- family/operator별 모범 문양과 짧은 체크리스트 제공.
- 실패 이유를 기술 용어에서 플레이어 언어로 바꾸기.
- 같은 목표 반복 실패 시 hint escalator와 ghost trace 표시.
- Magic Note, NPC 짧은 대사, 룬 라벨, HUD 우선순위 관리.
- 시도 로그와 설문 항목이 연구 질문에 답하게 정리.

하면 안 되는 것:

- 피드백 UI가 인식 판정 자체를 바꾸지 않는다.
- 튜토리얼 문구가 game runtime의 목표 완료를 대신하지 않는다.

산출물 예시:

```text
RuneGuidePanel
GoalMarkerLabel
MagicNoteController
HintMessageCatalog
RecognitionFeedbackPresenter
```

## 3. 3인 역할 분담

### 역할 A. 입력/인식 담당

이 담당자는 "플레이어가 그린 것을 게임이 이해할 수 있는 결과로 바꾸는 일"을 맡는다.

우선순위:

1. Unity에서 사용할 `StrokeInputSession` 계약을 정리한다.
2. 현재 `WorldDrawingController`에 섞여 있는 입력 수집과 인식 호출을 분리한다.
3. `IStrokeRecognitionService`를 만들고, 현재 C# recognizer를 그 뒤에 숨긴다.
4. Web recognizer에서 검증한 quality/profile 개념을 Unity 결과 타입에 맞춘다.
5. 테스트용 synthetic stroke source를 만들어 게임 담당자가 recognizer 없이도 floor logic을 테스트할 수 있게 한다.

완료 조건:

- 게임 런타임은 raw pointer event를 몰라도 된다.
- 게임 런타임은 `RecognitionResult`만 받아도 성공/실패/overlay/game effect를 처리할 수 있다.
- recognition layer 단위 테스트에서 canonical sample과 reject case가 통과한다.

### 역할 B. 게임 런타임 담당

이 담당자는 "인식 결과를 실제 게임 진행으로 바꾸는 일"을 맡는다.

우선순위:

1. `ExamGameController`에 몰려 있는 floor, seal, HUD, logging 처리를 작은 서비스로 분리한다.
2. `SpellCastingService`를 만들어 base/overlay/final spell 처리를 한 곳으로 모은다.
3. 1층에서 목표 근처에 그려야 한다는 규칙을 더 명확하게 만들고, 목표 거리 실패를 별도 상태로 처리한다.
4. 2층 이후 overlay/final seal loop가 "base를 고정하고 후속 overlay를 붙인다"는 느낌이 나도록 개선한다.
5. 5층 ending report까지 한 번에 끊기지 않는 15분 vertical slice를 안정화한다.

완료 조건:

- Unity를 켜면 1층부터 엔딩 리포트까지 수동으로 완주할 수 있다.
- floor goal은 recognizer 내부 구현과 독립적으로 테스트 가능하다.
- base seal lifetime, overlay attach radius, target radius 같은 게임 파라미터가 한곳에서 조정된다.

### 역할 C. 튜토리얼/콘텐츠/피드백 담당

이 담당자는 "처음 보는 사람이 무엇을 해야 하는지 이해하게 만드는 일"을 맡는다.

우선순위:

1. Rune Guide UI를 만들어 5개 base와 6개 overlay의 모범 문양을 게임 안에서 볼 수 있게 한다.
2. 1층의 물/바람/불/땅/생명 목표 표식을 색만이 아니라 라벨, 아이콘, 배치로 구분한다.
3. Magic Note 문구를 작성해 첫 성공, 첫 실패, 층 완료, 반복 실패 상황을 짧게 안내한다.
4. 실패 문구를 "closure score가 낮음"이 아니라 "끝점이 닿지 않아 힘이 새고 있다"처럼 플레이어 언어로 바꾼다.
5. 발표/시연용 5분, 10분, 15분 플레이 시나리오를 문서화한다.

완료 조건:

- 새 사용자가 1층에서 "어디에 어떤 문양을 그려야 하는지"를 30초 안에 이해한다.
- 물과 바람처럼 헷갈릴 수 있는 문양은 화면 안에서 명확히 구분된다.
- 반복 실패 시 다음에 고칠 행동이 한 문장으로 보인다.

## 4. 첫 번째 병렬 작업 순서

### Sprint 1. 경계 만들기

| 담당 | 작업 | 결과물 |
| --- | --- | --- |
| 입력/인식 | `StrokeInputSession`, `IStrokeRecognitionService` 초안 | 게임이 raw stroke 대신 인식 결과를 받는 구조 |
| 게임 런타임 | `SpellCastingService` 초안 | base/overlay/floor goal 처리 위치 정리 |
| 튜토리얼/콘텐츠 | Rune Guide 정보 구조와 1층 목표 표식안 | 1층에서 문양과 목표가 덜 헷갈리는 화면 기준 |

### Sprint 2. 1층 완주성

| 담당 | 작업 | 결과물 |
| --- | --- | --- |
| 입력/인식 | wind/water/fire reject case 보강 | 초보자 입력 실패 이유가 안정적으로 나온다 |
| 게임 런타임 | 목표 거리 실패, seal 고정, 다음 목표 전환 개선 | 1층에서 계속 멈추는 문제를 줄인다 |
| 튜토리얼/콘텐츠 | 1층 HUD/Magic Note/ghost trace 정리 | 사용자가 튜토리얼 설명 없이 다음 행동을 알 수 있다 |

### Sprint 3. 15분 vertical slice

| 담당 | 작업 | 결과물 |
| --- | --- | --- |
| 입력/인식 | overlay attach/dependency 결과 타입 고정 | final seal loop가 안정적으로 연결된다 |
| 게임 런타임 | 1층 축약 + 5층 축약 + ending report 연결 | 외부 1명이 15분 안에 완주 가능 |
| 튜토리얼/콘텐츠 | SFX 목록, 룬 라벨, 발표 시나리오 | 시연에서 게임의 의도가 바로 보인다 |

## 5. PR 운영 방식

큰 PR 하나로 모든 것을 바꾸면 서로 리뷰하기 어렵다. 앞으로는 아래처럼 나눈다.

| PR 종류 | 포함할 것 | 포함하지 않을 것 |
| --- | --- | --- |
| Contract PR | 타입, 인터페이스, 문서, 테스트용 fake/source | 실제 게임 밸런스 대규모 변경 |
| Runtime PR | seal, floor goal, world effect, ending | recognizer threshold 대규모 변경 |
| Feedback PR | HUD, Magic Note, guide UI, 문구 | recognizer 판정 로직 |
| Content PR | 층별 목표, 시나리오, 발표 흐름 | 입력 계층 구조 변경 |

PR 본문에는 최소한 다음을 적는다.

```text
## 요약
## 왜 필요한가
## 변경한 경계
## 담당자가 이어서 구현할 위치
## 검증
```

## 6. 지금 바로 다음에 만들면 좋은 코드 경계

현재 Unity 쪽에서 가장 먼저 분리하면 효과가 큰 경계는 다음 순서다.

1. `WorldDrawingController`는 stroke 수집까지만 담당한다.
2. `StrokeSessionBuffer`가 여러 stroke를 하나의 session으로 묶는다.
3. `IStrokeRecognitionService`가 session을 `RecognitionResult`로 바꾼다.
4. `SpellCastingService`가 `RecognitionResult`를 받아 base seal, overlay attach, floor goal을 처리한다.
5. `RecognitionFeedbackPresenter`가 결과를 HUD/Magic Note/룬 라벨로 보여준다.

목표 호출 흐름:

```text
WorldDrawingController
  -> StrokeSessionBuffer
  -> IStrokeRecognitionService.Recognize(session, context)
  -> SpellCastingService.Apply(result)
  -> RecognitionFeedbackPresenter.Show(result, gameOutcome)
```

이렇게 되면 입력/인식 담당자는 1~3번을, 게임 런타임 담당자는 4번을, 튜토리얼/콘텐츠 담당자는 5번을 거의 동시에 작업할 수 있다.

## 7. 완료 기준

이 팀 분담이 성공했다고 볼 기준은 다음과 같다.

- 새 팀원이 README와 이 문서만 읽어도 자기 작업 위치를 고를 수 있다.
- 입력/인식 PR과 게임 런타임 PR이 같은 파일을 과하게 충돌시키지 않는다.
- Unity에서 1층을 처음 플레이하는 사람이 목표와 문양을 구분할 수 있다.
- recognizer가 바뀌어도 floor goal 코드를 다시 쓰지 않는다.
- tutorial 문구가 바뀌어도 recognizer threshold가 바뀌지 않는다.
- 최종 시연은 Web 연구 도구가 아니라 Unity 플레이어블을 중심으로 진행하고, Web은 인식/품질/검증 근거로 보조한다.

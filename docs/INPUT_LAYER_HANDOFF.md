# Player Input and Recognition Handoff

이 문서는 player input에서 시작해 stroke recognition result가 나오기까지의 handoff 문서입니다. 목표는 입력 캡처, 선/형태 인식, 사용자별 보정, 게임 진행 처리를 서로 다른 사람이 병렬로 개발할 수 있게 만드는 것입니다.

## 0. 용어와 담당 범위

이 문서에서 "player input"은 두 단계로 나눈다.

```text
Narrow input capture
  raw mouse/touch/stylus event
  -> sampled points
  -> stroke
  -> stroke session

Stroke recognition
  stroke session
  -> family/operator candidates
  -> quality/confidence
  -> personalization/model policy
  -> recognition result
```

따라서 팀원이 맡으려는 일이 "입력받은 선이 어떤 종류인지", "무슨 형태인지", "사용자별로 개인화할지"까지 포함한다면 그 담당 범위는 단순 input capture가 아니라 `Input Capture + Stroke Recognition / Personalization`이다.

게임 계층은 `RecognitionResult` 이후의 seal 생성, overlay stack, floor goal, world effect, HUD/logging을 맡는다.

## 1. 한 줄 결론

Unity에서는 현재 `WorldDrawingController`와 `ExamGameController`에 섞여 있는 아래 책임을 분리한다.

```text
Device input
  -> Input capture source
  -> Stroke session buffer
  -> Stroke recognition runtime
  -> Personalization / model policy
  -> RecognitionResult
  -> Spell casting game runtime
  -> HUD / logging / world feedback
```

좁은 입력 캡처 경계는 `Device input`부터 `StrokeInputSession`을 만들어 넘기는 지점까지다. 선의 종류, 형태, family/operator, confidence, 개인화 보정은 recognition 경계다. 게임 계층은 완성된 recognition result 이후의 seal 생성, floor goal, world effect를 맡는다.

## 2. 왜 지금 분리해야 하는가

현재 Web 쪽은 입력 연구와 데이터 수집 흐름이 이미 비교적 명확하다.

| 입력 관련 개념 | Web 기준 위치 | 의미 |
| --- | --- | --- |
| point sample | `src/recognizer/types.ts:36` | x, y, t, pressure optional |
| stroke | `src/recognizer/types.ts:43` | point sample 배열 |
| stroke session | `src/recognizer/types.ts:48` | 여러 stroke와 시작/종료 시간 |
| user input profile | `src/recognizer/user-profile.ts:62` | 사용자의 입력 comfort band |
| profile update | `src/recognizer/user-profile.ts:103` | sealed result 기반 profile 누적 |
| survey trace | `src/survey/survey-contract.ts:23` | `[x, y, tMs]` shape trace |
| survey capture | `survey/magic-symbol-tutorial/main.ts:1545` | survey stroke를 shape trace로 변환 |

반면 Unity 쪽은 플레이어블을 빠르게 만들기 위해 입력, 버퍼, 시각화, 게임 처리 일부가 붙어 있다.

| 현재 위치 | 같이 들어 있는 책임 |
| --- | --- |
| `WorldDrawingController.cs` | 마우스 입력 감지, screen to world 변환, point 간격 필터, buffer timeout, LineRenderer 표시, `SpellBuffered` 이벤트 |
| `ExamGameController.cs` | `WorldDrawingController` 생성/구독, base/overlay 판정 호출, seal 생성, floor goal 처리, HUD/logging, 플레이어 이동 입력 |

이 상태에서는 입력 캡처나 recognition 경계를 바꾸는 사람이 마우스 대신 터치, 스타일러스, synthetic replay, 튜토리얼 입력 보정, 로깅 동의 흐름을 붙이려면 게임 컨트롤러까지 알아야 한다. 이 PR의 문서 목표는 그 결합을 끊고 각 계층이 병렬로 개발될 수 있는 기준을 먼저 고정하는 것이다.

## 3. 병렬 개발 경계

| 계층 | 책임 | 독립적으로 바꿀 수 있는 것 | 경계 밖으로 넘기지 않을 것 |
| --- | --- | --- | --- |
| Input capture layer | raw pointer/key/touch/stylus 입력을 stroke session으로 변환 | 입력 샘플링, min distance, buffer timeout, multi stroke grouping, synthetic replay, raw stroke export 정책 초안 | family/operator 판정, floor goal 완료, spell effect |
| Stroke recognition layer | stroke session을 base/overlay recognition result로 변환 | 선/형태 판정, recognizer, quality, confidence, threshold, dynamic policy, fixture | raw device event 처리, world object spawn, HUD 문구 |
| Personalization/model policy | 사용자 입력 습관과 보조 모델을 recognition에 반영 | adjusted quality, profile summary, model/shadow summary, confidence calibration | invalid/incomplete를 무조건 success로 승격, floor goal 완료 결정 |
| Game layer | recognition result를 게임 상태 변화로 변환 | seal 생성, overlay 적용, world effect, floor progression, challenge rule | pointer sampling 방식, raw stroke 저장 형식 |
| Presentation layer | 화면 피드백을 표시 | LineRenderer, ghost guide, HUD, failure feedback animation | recognition status 의미 변경 |
| Research/data layer | 입력과 결과를 분석 가능한 로그로 변환 | opt-in trace export, survey schema, dataset conversion | 기본 플레이 로그에 raw stroke 무단 저장 |

## 4. 현재 Unity 결합 지점

### `WorldDrawingController.cs`

현재 이 클래스는 이름보다 많은 일을 한다.

```text
WorldDrawingController
  - Input.GetMouseButtonDown / GetMouseButton / GetMouseButtonUp
  - Camera.ScreenToWorldPoint
  - currentStroke / bufferedStrokes 관리
  - DefaultBufferSeconds
  - DefaultMinPointDistance
  - LineRenderer 생성과 색상/폭 적용
  - SpellBuffered(List<List<StrokeSample>>, Vector2 center, int strokeCount)
```

분리해야 할 책임:

| 책임 | 새 위치 후보 |
| --- | --- |
| 마우스/터치 raw event 읽기 | `WorldPointerInputSource` |
| screen/world 좌표 변환 | `WorldPointerInputSource` 또는 `IInputCoordinateMapper` |
| stroke 샘플 최소 간격 | `StrokeInputSampler` 또는 source 내부 policy |
| 여러 stroke를 한 session으로 묶기 | `StrokeSessionBuffer` |
| 그리는 선 표시 | `Presentation/WorldStrokeVisuals` |
| recognition으로 session 완료 알림 | `ISpellInputPort` 또는 `StrokeSessionCompleted` event |

### `ExamGameController.cs`

현재 게임 컨트롤러는 아래 흐름을 직접 안다.

```text
ConfigureWorldDrawing()
  -> worldDrawing.SpellBuffered += OnSpellBuffered

OnSpellBuffered(strokes, center, strokeCount)
  -> ProcessSpellGroup(...)
  -> ProcessOverlay(...) or ProcessBase(...)
  -> seal/world/floor/logging
```

목표는 `ExamGameController`가 raw stroke buffer 구조와 recognition 내부 구현을 직접 알지 않는 것이다.

```text
OnStrokeSessionCompleted(StrokeInputSession session)
  -> recognitionService.Recognize(session)
  -> spellCastingService.Process(recognitionResult, currentGameContext)
  -> game result event
```

## 5. 목표 파일 구조

처음부터 asmdef까지 강제로 쪼개지 말고, 폴더와 namespace부터 분리한다.

```text
unity/MagicExamHall/Assets/MagicExamHall/Scripts/
  Input/
    StrokeInputTypes.cs
    IStrokeInputSource.cs
    StrokeSessionBuffer.cs
    WorldPointerInputSource.cs
    SyntheticStrokeInputSource.cs
  Recognition/
    IBaseGestureRecognizer.cs
    IOverlayGestureRecognizer.cs
    HeuristicBaseGestureRecognizer.cs
    HeuristicOverlayGestureRecognizer.cs
  Personalization/
    IUserInputProfileService.cs
    IRecognitionModelPolicy.cs
  Presentation/
    WorldStrokeVisuals.cs
  Game/
    SpellCastingService.cs
    SpellCastingContext.cs
    SpellCastingResult.cs
  Runtime/
    ExamGameController.cs
```

초기 PR에서는 `Input/`만 먼저 만들어도 된다. 팀원이 선/형태 판정과 개인화까지 맡는다면 `Recognition/`과 `Personalization/` 경계도 같은 작업 흐름 안에서 잡는다. `Game/`은 recognition result 이후만 받도록 다음 리팩터에서 옮긴다.

## 6. Input DTO 초안

Unity 런타임 내부 DTO는 Web의 `PointSample`, `Stroke`, `StrokeSession`과 의미가 맞아야 한다.

```csharp
namespace MagicExamHall.Input
{
    public enum InputCoordinateSpace
    {
        Screen,
        World,
        Normalized
    }

    public readonly struct StrokeInputPoint
    {
        public readonly Vector2 Position;
        public readonly float TimeSeconds;
        public readonly float Pressure;
        public readonly int PointerId;
        public readonly InputCoordinateSpace CoordinateSpace;
    }

    public sealed class StrokeInputStroke
    {
        public string Id { get; }
        public IReadOnlyList<StrokeInputPoint> Points { get; }
    }

    public sealed class StrokeInputSession
    {
        public string Id { get; }
        public IReadOnlyList<StrokeInputStroke> Strokes { get; }
        public double StartedAtSeconds { get; }
        public double EndedAtSeconds { get; }
        public InputCoordinateSpace CoordinateSpace { get; }
    }
}
```

주의:

1. `Pressure`는 지금 마우스 입력에서는 기본값 1로 둔다.
2. `PointerId`는 마우스에서는 0으로 둔다.
3. Recognition Runtime은 우선 world 좌표 stroke만 받아도 된다.
4. fixture/log 저장 시에는 Web 계약처럼 `{ x, y, t }` 또는 `[x, y, tMs]`로 변환한다.

## 7. Input source interface 초안

입력 캡처 구현은 이 interface 뒤쪽에서 독립적으로 진행한다.

```csharp
namespace MagicExamHall.Input
{
    public interface IStrokeInputSource
    {
        event Action<StrokeInputStroke> StrokeStarted;
        event Action<StrokeInputStroke> StrokeUpdated;
        event Action<StrokeInputStroke> StrokeCompleted;
        event Action StrokeCanceled;

        bool IsDrawing { get; }
        void Tick(float deltaTime);
    }
}
```

구현 후보:

| 구현 | 역할 |
| --- | --- |
| `WorldPointerInputSource` | 현재 우클릭 hold/release world drawing 유지 |
| `TouchStrokeInputSource` | 모바일/터치 입력 대응 |
| `StylusStrokeInputSource` | pressure, tilt 같은 optional 입력 대응 |
| `SyntheticStrokeInputSource` | 테스트와 replay용 session 주입 |
| `SurveyReplayInputSource` | Web/survey trace를 Unity에서 재생할 때 사용 |

## 8. Session buffer 초안

buffer는 "언제 주문 입력이 끝났다고 볼 것인가"만 결정한다.

```csharp
namespace MagicExamHall.Input
{
    public interface IStrokeSessionBuffer
    {
        event Action<StrokeInputSession> SessionCompleted;
        bool HasPendingStrokes { get; }
        void PushCompletedStroke(StrokeInputStroke stroke);
        void Tick(float deltaTime);
        void Flush();
        void Cancel();
    }
}
```

기본 policy:

| 항목 | 현재값 | 새 위치 |
| --- | --- | --- |
| buffer timeout | `1.05f` | `StrokeSessionBufferOptions.BufferSeconds` |
| min point distance | `0.05f` | source 또는 sampler option |
| empty submit | 현재 `SpellBuffered(empty)` | 명시적 `Cancel` 또는 invalid session policy로 분리 |
| center 계산 | `CenterOf(copy)` | `StrokeInputSessionExtensions.GetWorldCenter()` |

## 9. Presentation 분리

입력 시각화는 input source가 아니라 presentation이다.

현재 `WorldDrawingController`가 LineRenderer를 직접 만든다. 분리 후에는 아래처럼 바꾼다.

```text
WorldPointerInputSource
  -> StrokeStarted / StrokeUpdated / StrokeCompleted

WorldStrokeVisuals
  -> event를 구독해 LineRenderer를 그림
  -> session 완료 후 fade 또는 clear
```

이렇게 해야 pointer sampling을 바꿔도 선 표시 로직과 게임 처리 로직이 같이 흔들리지 않는다.

## 10. Recognition and game handoff

입력 캡처에서 recognition으로 넘어가는 이벤트는 하나로 좁힌다.

```csharp
public interface ISpellInputPort
{
    event Action<StrokeInputSession> StrokeSessionCompleted;
}
```

recognition 계층은 이 session을 받아 result를 만든다. 아래 `StrokeRecognitionResult`와 `RecognitionContext`는 다음 구현 PR에서 확정할 제안 타입이며, 현재 Unity의 `BaseRecognitionResult` / `OverlayRecognitionResult`를 감싸거나 대체하는 service boundary 역할을 한다.

```csharp
public interface IStrokeRecognitionService
{
    StrokeRecognitionResult Recognize(StrokeInputSession session, RecognitionContext context);
}
```

`RecognitionContext`에는 overlay 판정에 필요한 게임 상태를 read-only snapshot으로만 넣는다.

| context 값 | 용도 | 금지 |
| --- | --- | --- |
| current seal id/base family | overlay 기준 seal 선택 | seal 생성/삭제 직접 수행 |
| overlay stack | `martial_axis` 같은 dependency 검사 | overlay stack 직접 수정 |
| reference frame / world center / scale | anchor, scale, proximity 판정 | floor goal 완료 처리 |
| active phase hint | base/overlay/final 후보 선택 | HUD/logging 직접 실행 |

`ExamGameController`는 장기적으로 raw stroke나 recognizer 구현이 아니라 recognition result만 받는다.

```csharp
private void OnRecognitionCompleted(StrokeRecognitionResult result)
{
    spellCastingService.Process(result, BuildCastingContext());
}
```

기존 `SpellBuffered(List<List<StrokeSample>>, Vector2 center, int strokeCount)`는 임시 호환 adapter로 유지할 수 있다. 다만 새 코드는 `StrokeInputSession`을 기준으로 작성한다.

## 11. Web과 Unity 매핑

| Web | Unity 목표 | 비고 |
| --- | --- | --- |
| `PointSample.x` / `PointSample.y` | `StrokeInputPoint.Position` | 저장 시 `{x,y}`로 변환 |
| `PointSample.t` | `StrokeInputPoint.TimeSeconds` | Web은 ms 성격, Unity는 seconds일 수 있으므로 export에서 단위 명시 |
| `PointSample.pressure?` | `StrokeInputPoint.Pressure` | optional |
| `Stroke.id` | `StrokeInputStroke.Id` | Unity는 GUID 또는 session-local id |
| `Stroke.points` | `StrokeInputStroke.Points` | 최소 2 point 필요 |
| `StrokeSession.strokes` | `StrokeInputSession.Strokes` | recognition input |
| `StrokeSession.startedAt` | `StartedAtSeconds` | optional log field |
| `StrokeSession.endedAt` | `EndedAtSeconds` | optional log field |
| `ShapeTrace` | export/import DTO | survey/replay bridge |

## 12. 병렬 개발 시작점

입력 캡처 구현은 게임 규칙 변경 없이 아래 순서로 시작할 수 있다.

1. `Scripts/Input/StrokeInputTypes.cs`를 만든다.
2. `WorldDrawingController`의 `StrokeSample` 리스트를 `StrokeInputSession`으로 변환하는 adapter를 만든다.
3. `StrokeSessionBuffer`를 `WorldDrawingController`에서 분리한다.
4. `WorldPointerInputSource`가 현재 우클릭 입력을 그대로 재현하게 한다.
5. `Presentation/WorldStrokeVisuals`가 기존 LineRenderer 표현을 그대로 재현하게 한다.
6. recognition coordinator나 service가 `StrokeSessionCompleted`를 받아 `StrokeRecognitionResult`를 만들게 한다.
7. `SyntheticStrokeInputSource` 또는 test helper로 mouse 없이 session을 넣는 테스트를 추가한다.

팀원이 선/형태 판정과 개인화까지 맡는 경우에는 이어서 아래를 진행한다.

8. `IBaseGestureRecognizer`와 `IOverlayGestureRecognizer`를 만든다.
9. 현재 static `GestureRecognizer` / `OverlayRecognizer` 호출을 heuristic 구현체 뒤로 감싼다.
10. user profile, adjusted quality, model/shadow summary는 recognition result의 보조 필드로만 노출한다.
11. `ExamGameController` 또는 `SpellCastingService`는 family/operator/status/confidence를 받은 뒤 게임 효과만 결정하게 만든다.

첫 구현에서 유지해야 하는 플레이 감각:

- 우클릭 hold 중 바닥에 선이 그려진다.
- 우클릭 release 후 짧은 buffer 안에 여러 stroke를 이어 그릴 수 있다.
- buffer가 끝나면 기존과 같은 base/overlay 판정으로 넘어간다.
- 플레이어 이동 입력은 기존 WASD/방향키를 유지한다.
- floor 진행, seal 수명, HUD 문구는 바뀌지 않는다.

## 13. 완료 조건

player input/recognition 분리 PR은 아래를 만족해야 한다.

| 조건 | 확인 방법 |
| --- | --- |
| 기존 우클릭 drawing 동작 유지 | Unity PlayMode 수동 smoke |
| `ExamGameController`가 raw mouse input을 모름 | `Input.GetMouseButton*` 호출이 input source로 이동 |
| `ExamGameController`가 recognizer 내부 구현을 모름 | static recognizer 호출이 service/interface 뒤로 이동 |
| buffer timeout이 독립 클래스에 있음 | `StrokeSessionBuffer` 단위 테스트 또는 inspector smoke |
| synthetic session 주입 가능 | 테스트 helper 또는 public debug hook |
| LineRenderer가 input source에 묶이지 않음 | `WorldStrokeVisuals`가 event 구독 |
| 개인화가 게임 성공을 직접 결정하지 않음 | invalid/incomplete를 profile만으로 success로 승격하지 않는 테스트 |
| raw stroke logging은 opt-in 전까지 기본 off | `ExamLogging` 기본 필드 유지 |

## 14. 이번 PR의 범위

이번 PR은 구현 PR이 아니라 handoff PR이다.

포함:

- 프로젝트 계층 문서화
- Web/Unity recognition contract 초안
- player input과 recognition handoff 문서
- README와 roadmap의 문서 진입점 갱신

제외:

- Unity C# 파일 이동
- 기존 drawing runtime 변경
- AI/fine-tuning/model adapter 구현
- raw stroke 상시 저장
- floor content나 puzzle rule 변경

## 15. 다음 PR 제목 후보

player input/recognition 구현을 시작할 때는 아래처럼 작게 나누는 것을 권장한다.

1. `refactor: add unity stroke input session contract`
2. `refactor: split unity stroke session buffer`
3. `refactor: split world drawing visuals from input source`
4. `test: add synthetic stroke input path for unity casting`
5. `refactor: add unity recognition service boundary`
6. `refactor: add recognition personalization policy boundary`

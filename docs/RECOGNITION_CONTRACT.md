# Recognition Contract

GitHub issue #15 / #41 기준의 Web-Unity 인식 계약 초안입니다. 목적은 Web research lab과 Unity playable이 같은 마법 언어를 쓰게 하고, 다른 작업자가 인식 기능이나 보정 기능을 연결할 때 어느 경계를 따르면 되는지 명확히 하는 것입니다.

## 1. 범위

이 문서는 아래 항목을 고정한다.

- base family 이름과 의미
- overlay operator 이름과 의존성
- recognition status 의미
- stroke/session 데이터 모양
- quality vector 항목
- base/overlay/final result 모양
- Unity attempt log와 Web/HCI 분석 개념 대응
- 새 symbol 또는 인식 기능 추가 시 갱신해야 할 파일

이 문서는 게임 기획 전체 문서가 아니다. 게임의 층 구조, NPC, 카메라, 사운드, 엔딩은 `docs/GAME_DESIGN.md`를 따른다. Unity player input과 recognition handoff는 `docs/INPUT_LAYER_HANDOFF.md`를 따른다.

## 2. 구현 기준 파일

| 영역 | Web 기준 | Unity 기준 |
| --- | --- | --- |
| 공통 타입 | `src/recognizer/types.ts` | `SpellRecognition.cs`, `SpellRuntime.cs` |
| base 인식 | `src/recognizer/recognize.ts` | `GestureRecognizer`, `SpellRuntime.RecognizeBase` |
| overlay 인식 | `src/recognizer/operators.ts` | `OverlayRecognizer` |
| final seal | `src/recognizer/compile.ts` | `CompiledSeal`, `ExamGameController` overlay stack |
| 품질 | `src/recognizer/quality.ts` | `QualityAnalyzer` |
| 개인화/profile | `src/recognizer/user-profile.ts`, `src/recognizer/tutorial-profile.ts` | 미구현, issue #80 |
| 모델/shadow 보조 판독 | `src/recognizer/rerank.ts` | 미구현, 명시적 adapter 경계는 후속 |
| 로그 | Web recognition log / survey contract | `ExamLogging.cs` |

## 3. Base Family Contract

외부 저장, 로그, 분석, fixture에서는 소문자 문자열을 canonical id로 쓴다.

| Canonical id | Unity enum | 한국어 표시 | 의미 | 현재 Web | 현재 Unity |
| --- | --- | --- | --- | --- | --- |
| `fire` | `SpellFamily.Fire` | 불꽃 | 공격, 점화, 에너지 | 구현됨 | 구현됨 |
| `water` | `SpellFamily.Water` | 물 | 순환, 정화, 회복 | 구현됨 | 구현됨 |
| `wind` | `SpellFamily.Wind` | 바람 | 흐름, 이동, 밀기 | 구현됨 | 구현됨 |
| `earth` | `SpellFamily.Earth` | 땅 | 안정, 방어, 축조 | 구현됨 | 구현됨 |
| `life` | `SpellFamily.Life` | 생명 | 성장, 연결 | 구현됨 | 구현됨 |

규칙:

1. 로그와 JSON fixture에서는 `fire`, `water`, `wind`, `earth`, `life`만 사용한다.
2. Unity enum 이름은 C# 관례로 PascalCase를 유지하되, 파일 저장 시 canonical id로 변환한다.
3. family 판정은 형태 인식이 결정한다. quality/profile/model은 품질, confidence, 보조 판독, threshold에만 제한적으로 관여한다.

## 4. Overlay Operator Contract

| Canonical id | Unity enum | 한국어 표시 | 의미 | 주요 조건 |
| --- | --- | --- | --- | --- |
| `steel_brace` | `OverlayOperator.SteelBrace` | 강철 버팀 | 강화, 방어 | base seal 이후 |
| `electric_fork` | `OverlayOperator.ElectricFork` | 번개 갈래 | 분기, 전도 | base seal 이후 |
| `ice_bar` | `OverlayOperator.IceBar` | 얼음 막대 | 냉기, 고정 | base seal 이후 |
| `soul_dot` | `OverlayOperator.SoulDot` | 영혼 점 | 집중, 핵심점 | base seal 이후 |
| `void_cut` | `OverlayOperator.VoidCut` | 공허 절단 | 차단, 해제 | base seal 이후 |
| `martial_axis` | `OverlayOperator.MartialAxis` | 무술 축 | 축, 고급 결합 | 같은 seal에 `void_cut` 선행 필요 |

규칙:

1. overlay는 base seal 이후에만 인식한다.
2. overlay 판정은 모양, 위치(anchor), 크기(scale), 기존 stack 의존성을 함께 본다.
3. `martial_axis`는 `void_cut` 없는 상태에서 recognized가 되면 안 된다.
4. overlay stack의 저장 순서는 실제 부착 순서를 따른다.

## 5. Recognition Status Contract

| Canonical status | Web status | Unity status | 의미 | 게임 처리 |
| --- | --- | --- | --- | --- |
| `recognized` | `recognized` | `RecognitionStatus.Recognized` | 기준을 통과해 family/operator를 확정함 | 주문/overlay 적용 가능 |
| `ambiguous` | `ambiguous` | `RecognitionStatus.Ambiguous` | 후보가 가까워 확정하지 않음 | 약한 실패 피드백, 재시도 유도 |
| `incomplete` | `incomplete` | `RecognitionStatus.Incomplete` | 닫힘, stroke 수, 의존성, 크기 등 일부 조건 부족 | 다음 행동 힌트 제공 |
| `invalid` | `invalid` | `RecognitionStatus.Invalid` | 기준형과 충분히 가깝지 않거나 입력 부족 | 실패 피드백, 로그 기록 |

주의:

- GitHub issue #15 본문에는 `rejected`, `assisted` 검토가 언급되어 있지만 현재 코드의 canonical recognition status는 위 4개다.
- `assisted`는 recognition status가 아니라 Unity attempt log의 boolean/context로 둔다.
- 실패 피드백 문구에는 내부 status만 노출하지 않는다.

## 6. Stroke Session Contract

### Web

```ts
PointSample {
  x: number;
  y: number;
  t: number;
  pressure?: number;
}

Stroke {
  id: string;
  points: PointSample[];
}

StrokeSession {
  strokes: Stroke[];
  startedAt: number;
  endedAt?: number;
}
```

### Unity

```csharp
StrokeSample {
  Vector2 position;
  float time;
}
```

현재 차이:

| 개념 | Web | Unity | 계약 |
| --- | --- | --- | --- |
| 좌표 | `x`, `y` number | `Vector2 position` | fixture/log 저장 시 `{x,y}` |
| 시간 | `t` number | `time` float | stroke 시작 기준 상대 시간 권장 |
| stroke id | 있음 | 런타임 내부에는 없음 | 공유 fixture에는 `id` 포함 |
| pressure | optional | 없음 | optional, Unity는 무시 가능 |
| session timestamps | `startedAt`, `endedAt` | 별도 session/log context | 공유 fixture에는 optional |

공통 fixture를 만들 경우 JSON은 Web 모양을 기준으로 하고, Unity가 importer에서 `StrokeSample`로 변환한다.

## 7. Quality Vector Contract

| 항목 | Web | Unity | 의미 | 현재 대응 |
| --- | --- | --- | --- | --- |
| `closure` | 있음 | 있음 | 닫힘/끝점 간격 | 직접 대응 |
| `smoothness` | 있음 | 있음 | 선의 부드러움 | 직접 대응 |
| `tempo` | 있음 | 있음 | 입력 속도 안정성 | 직접 대응 |
| `stability` | 있음 | 있음 | 흔들림/일관성 | 직접 대응 |
| `rotationBias` | 있음 | 있음 | 회전 편향 | 직접 대응 |
| `symmetry` | 있음 | 없음 | 좌우/축 대칭성 | Unity 미구현 |
| `overshoot` | 있음 | 없음 | 과도한 삐져나감 | Unity 미구현 |

규칙:

1. quality는 family/operator 자체를 마음대로 바꾸지 않는다.
2. quality는 confidence, feedback, effect strength, adjusted quality에 사용한다.
3. user profile 보정은 raw quality를 덮어쓰지 않는다.
4. incomplete/invalid가 personalization만으로 success가 되면 안 된다.

## 8. Base Recognition Result Contract

### Web 주요 필드

| 필드 | 의미 |
| --- | --- |
| `status` | recognition status |
| `canonicalFamily` | sealed + recognized일 때 확정 family |
| `topCandidate` | 가장 높은 후보 |
| `candidates` | 후보 목록과 score |
| `rawQuality` | 원본 품질 |
| `adjustedQuality` | profile 반영 품질 |
| `qualityAdjustment` | raw와 adjusted 차이 |
| `invalidReason` | 실패/보류 이유 |
| `normalizedStrokes` | normalized geometry |
| `personalization` | profile runtime summary |
| `shadow` | model/shadow 판독 summary |
| `dynamicPolicy` | dynamic gating summary |

### Unity 주요 필드

| 필드 | 의미 |
| --- | --- |
| `SpellResult.status` | recognition status |
| `SpellResult.recognizedFamily` | 성공 시 family |
| `SpellResult.targetFamily` | Unity 내부 family별 scoring 대상 |
| `SpellResult.confidence` | confidence |
| `SpellResult.quality` | Unity quality vector |
| `SpellResult.feedbackReason` | 실패/성공 이유 |
| `SpellResult.nextHint` | 다음 행동 힌트 |
| `BaseRecognitionResult.center` | world center |
| `BaseRecognitionResult.worldScale` | world-space scale |
| `BaseRecognitionResult.bufferStrokeCount` | 입력 stroke 수 |

현재 차이:

- Web은 모든 family 후보를 `RecognitionCandidate[]`로 보존한다.
- Unity는 `SpellRuntime.RecognizeBase`에서 family별 `GestureRecognizer.Recognize` 결과를 정렬한 뒤 best만 `BaseRecognitionResult`로 노출한다.
- Unity에는 Web의 `shadow`, `dynamicPolicy`, `adjustedQuality`가 없다.

## 9. Overlay Recognition Result Contract

### Web 주요 필드

| 필드 | 의미 |
| --- | --- |
| `status` | recognition status |
| `operator` | recognized operator |
| `candidates` | 후보 목록 |
| `topCandidate` | 가장 높은 후보 |
| `anchorZoneId` | anchor zone |
| `personalization` | profile runtime summary |
| `shadow` | model/shadow 판독 summary |

### Unity 주요 필드

| 필드 | 의미 |
| --- | --- |
| `status` | recognition status |
| `recognizedOperator` | 성공 또는 조건 부족 후보 |
| `score` | 최종 score |
| `shapeConfidence` | 모양 confidence |
| `scaleRatio` | seal 대비 overlay 크기 |
| `scaleHint` | `None`, `TooSmall`, `TooLarge` |
| `anchorZone` | anchor label |
| `feedbackReason` | 이유/힌트 |

현재 차이:

- Web은 anchor zone enum을 `upper_left`, `core` 등으로 고정한다.
- Unity는 `anchorZone`을 string으로 저장한다.
- Unity에는 overlay model adapter/shadow가 없다.

## 10. Final Seal Contract

Final seal은 아래 순서로 구성된다.

```text
base family
  + recognized overlay operators in stack order
  + quality/profile summary
  + compile timestamp/context
```

### Web

`CompiledSealResult`:

- `phase: "final"`
- `baseFamily`
- `baseResult`
- `overlayOperators`
- `rawQuality`
- `adjustedQuality`
- `qualityAdjustment`
- `profileDelta`
- `compiledAt`
- `summary`

### Unity

`CompiledSeal`:

- `sealId`
- `baseFamily`
- `overlayStack`
- `quality`
- `worldCenter`
- `worldScale`
- `createdAt`
- `expiresAt`
- `Label`

현재 차이:

- Web final seal은 research/demo result 객체에 가깝다.
- Unity final seal은 world object runtime state에 가깝다.
- 공유 로그/fixture에는 `baseFamily`, `overlayStack`, `quality`, `createdAt/compiledAt`만 우선 맞춘다.

## 11. Attempt Log Contract

Unity `AttemptLog` 현재 필드:

| 필드 | 의미 |
| --- | --- |
| `sessionId` | 플레이 세션 |
| `trialId` | 시도 id |
| `targetFamily` | 목표 family |
| `recognizedFamily` | 인식 family |
| `phase` | base/overlay/final |
| `baseFamily` | seal base family |
| `overlayStack` | overlay stack 문자열 |
| `sealId` | seal id |
| `floorId` | 층 id |
| `targetObject` | 목표 오브젝트 |
| `worldEffect` | 발생한 world effect |
| `status` | recognition status |
| `confidence` | confidence/score |
| `closure`, `smoothness`, `tempo`, `stability`, `rotationBias` | Unity quality |
| `worldX`, `worldY` | 시전 위치 |
| `bufferStrokeCount` | stroke 수 |
| `attemptIndex` | 누적 시도 수 |
| `elapsedMs` | 경과 시간 |
| `feedbackViewed` | 피드백 확인 여부 |
| `success` | 현재 Unity에서는 recognition 적용 가능 여부, 장기 계약에서는 게임 목표 처리 결과 |
| `hintShown` | 힌트 표시 여부 |
| `assistLevel` | assist 단계 |
| `assisted` | assist 사용 성공/시도 |

분석 규칙:

1. `status`는 인식 결과다.
2. `success`는 장기 계약에서는 게임 목표 처리 결과다. 현재 Unity base/overlay 로그는 recognition 적용 가능 여부에 더 가깝기 때문에, floor goal 완료 여부가 필요하면 별도 필드를 추가한다.
3. `assisted`는 상태가 아니라 맥락이다.
4. raw stroke는 기본 로그에 저장하지 않는다. 필요하면 별도 동의와 별도 export 경로를 둔다.

## 12. Model / Personalization Boundary

외부 인식 보정, user profile, model 계열 작업은 Recognition Runtime의 보조 판독으로 연결한다.

현재 Web 경계:

- `src/recognizer/rerank.ts`가 tiny ML artifact runtime, rerank, shadow summary를 맡고 있다.
- `src/recognizer/recognize.ts`와 `src/recognizer/operators.ts`는 이 보조 판독을 직접 호출한다.
- 명시적인 `RecognitionModelAdapter` 파일과 외부 주입 interface는 아직 없다.

향후 adapter 경계:

- `RecognitionModelAdapter`
- default tiny ML adapter
- base/overlay candidate rerank entrypoint
- runtime/model status entrypoint

adapter가 해도 되는 일:

- 후보 score 조정
- confidence calibration
- shadow top label 계산
- ambiguity probability 계산
- runtime/model status 제공

adapter가 하면 안 되는 일:

- floor goal 완료 결정
- world effect 직접 실행
- narrative/codex 문구 결정
- invalid/incomplete를 무조건 success로 승격
- 로그 privacy 정책 변경

Unity에는 아직 같은 adapter 경계가 없다. Unity 쪽 후속 작업은 `IBaseGestureRecognizer`, `IOverlayGestureRecognizer`, optional model/profile service로 나눈다.

## 13. 새 Symbol 추가 체크리스트

새 base family나 overlay operator를 추가할 때는 아래를 같이 확인한다.

### Web

- `src/recognizer/types.ts`
- `src/recognizer/templates.ts`
- `src/recognizer/operator-templates.ts`
- `src/recognizer/recognize.ts`
- `src/recognizer/operators.ts`
- `src/recognizer/datacards.ts`
- `src/demo/exemplars.ts`
- 관련 tests

### Unity

- `SpellRecognition.cs`
- `SpellRuntime.cs`
- `SpellLabels`
- `GestureRecognizerTests.cs`
- `ExamGameController.cs`
- scene/floor goal definition
- HUD/Magic Note feedback strings
- attempt log mapping, 필요한 경우

### Research / ML

- `artifacts/ml/feature-spec-v1.json`
- `scripts/tutorial-dataset/ml-contract.mjs`
- `scripts/ml-baseline/*`
- synthetic/dashboard fixtures

### Docs

- `docs/RECOGNITION_CONTRACT.md`
- `docs/GAME_DESIGN.md`
- `docs/PROJECT_ROADMAP.md`

## 14. 열린 결정 사항

| 항목 | 현재 결정 | 후속 |
| --- | --- | --- |
| 공통 fixture | 지금은 보류 | issue #20에 따라 인식기 안정화 뒤 재평가 |
| Unity adjusted quality | 미구현 | issue #80 |
| Unity model/shadow adapter | 미구현 | recognition service 분리 뒤 진행 |
| Unity `symmetry`, `overshoot` | 미구현 | 필요 시 quality 확장 issue 생성 |
| raw stroke 저장 | 기본 저장 안 함 | issue #16 privacy 기준 필요 |

## 15. 다음 구현 순서

1. Unity `Recognition` 폴더와 service interface를 만든다.
2. `GestureRecognizer` static 호출을 감싸는 `HeuristicBaseGestureRecognizer`를 만든다.
3. `OverlayRecognizer` static 호출을 감싸는 `HeuristicOverlayGestureRecognizer`를 만든다.
4. `ExamGameController`가 concrete static recognizer가 아니라 service 경유로 호출하게 바꾼다.
5. 그 다음에 profile/model adapter를 Unity에 붙일지 결정한다.

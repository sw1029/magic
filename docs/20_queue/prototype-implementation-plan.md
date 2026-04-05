# 프로토타입 실구현 실행 계획

이 문서는 현재 디렉토리의 `Magic Recognizer V1`을 출발점으로 삼아,
원본 아이디어와 `chat/request-answer01.md`부터 `18.md`까지의 논의를 반영한 다음 단계 실구현 계획을 정리합니다.

이번 문서의 초점은 아래 한 줄입니다.

**현재 Web UI에서 동작하는 기본 5문양 인식기를 유지한 채, 유저 입력 기반 품질 벡터 개선과 획/연산자 오버레이를 추가하고, 그 결과를 Web UI에서 직접 확인 가능한 상태까지 끌고 가는 것**

---

## 진행 상태 메모

이 문서는 실행 계획 문서지만, 현재 저장소 구현 범위를 짧게 함께 기록합니다.
아래 상태는 **2026-04-05 현재 코드 기준**입니다.

* W01. phase / operator naming 정리: 완료
* W02. user input profile, raw / adjusted quality 분리: 완료
* W03. overlay recognizer와 base reference frame 분리: 완료
* W04. compile 결과 모델과 dependency rule (`martial_axis requires void_cut`): 완료
* W05. Web UI authoring 흐름 (`Seal Base -> Start Overlay -> Seal Final`) 반영: 완료
* W06. 로그와 export 확장: 부분 완료
  현재 `base_seal`, `compile_seal`, `overlayRecords`, `rawQuality`, `adjustedQuality`는 남지만, 별도 profile snapshot 문서화는 아직 부족함
* W07. 자동 테스트 추가: 완료
* W08. 수동 Web UI 검증: 브라우저 수동 점검 계속 필요

---

## 1. 기준 문서

이번 계획은 아래 문서를 구현 기준으로 삼습니다.

* `chat/request-answer01.md`
* `chat/request-answer08.md`
* `chat/request-answer11.md`
* `chat/request-answer12.md`
* `chat/request-answer15.md`
* `chat/request-answer18.md`
* `docs/00_source_map/idea-lineage.md`
* `docs/00_source_map/client-decisions.md`
* `docs/10_direction/final-direction.md`
* `docs/10_direction/prototype-target.md`
* `docs/30_tasks/epic-02-symbols-and-input/task-01-base-symbol-prototypes.md`
* `docs/30_tasks/epic-02-symbols-and-input/task-02-input-interpretation-rules.md`

---

## 2. 현재 구현 상태 요약

현재 코드베이스는 현재 아래 범위를 만족합니다.

* Web 캔버스 입력
* 기본 5문양 base family 인식
* `Seal Base -> Start Overlay -> Seal Final` 단계형 authoring UI
* draw 중 base preview와 overlay preview
* `base seal` 후 canonical family 확정
* raw quality / adjusted quality 동시 표시
* base silhouette 기준 anchor zone / operator ghost debug overlay
* overlay operator 6종 인식
* compiled result 표시
* JSON 로그 export
* recognizer 단위 테스트와 overlay 회귀 테스트

현재 구현의 핵심 파일은 아래입니다.

* `src/app.ts`
* `src/recognizer/compile.ts`
* `src/recognizer/operator-templates.ts`
* `src/recognizer/operators.ts`
* `src/recognizer/recognize.ts`
* `src/recognizer/quality.ts`
* `src/recognizer/templates.ts`
* `src/recognizer/types.ts`
* `src/recognizer/user-profile.ts`
* `tests/recognizer.test.ts`
* `tests/recognizer-v15.test.ts`
* `tests/overlay-operators.test.ts`

현재 상태를 기준으로 보면, 이미 `기본 root radical 인식기`는 존재합니다.
따라서 다음 단계는 새 프로젝트를 만드는 것이 아니라, **V1 recognizer를 V1.5 authoring/compile recognizer로 확장하는 작업**으로 보는 것이 맞습니다.

---

## 3. 이번 단계의 목표 범위

이번 단계에서 실제로 도달해야 하는 범위는 아래입니다.

### 포함할 것

* 현재 Web UI의 기본 5문양 인식은 유지
* 유저 입력 패턴을 누적하여 품질 벡터 해석을 조금 더 안정적으로 보정
* 기본 도형 위에 덧그리는 획/연산자 오버레이를 인식
* `base seal` / `final seal` 시점에 `기본 문양 + 오버레이 + 품질 벡터 + 보정 결과`를 canonical 결과로 묶음
* Web UI에서 base preview, overlay preview, final result, debug overlay, 로그를 함께 확인
* 자동 테스트와 수동 검증 시나리오 확보

### 이번 단계에서 의도적으로 제외할 것

* 여러 마법진 graph 연결
* 완성형 허수아비 전투 루프
* 3D operator 실제 시뮬레이션
* 무거운 AI 모델 연동
* 온라인 기능

즉, 이번 단계는 `입력 언어 확장 + Web UI 검증`까지를 완료선으로 잡습니다.

---

## 4. 핵심 설계 원칙

### 4-1. 종류 고정 원칙 유지

* 기본 문양 family는 여전히 root silhouette로 정한다.
* 오버레이는 family를 바꾸는 것이 아니라 파생/실행 변조로만 작동한다.
* 유저 개인차와 품질 벡터는 family 판정 권한을 가져가면 안 된다.

### 4-2. commit 기반 해석 유지

* draw 중에는 후보와 구조 힌트만 보여 준다.
* canonical 확정은 `Seal Base`와 `Seal Final`에서만 한다.
* 오버레이도 같은 원칙을 따라 `Start Overlay -> overlay preview -> Seal Final` 흐름으로 읽는다.

### 4-3. 개인화는 보정만 수행

`request-answer08.md`의 방향을 그대로 따른다.

* 개인화는 `의미 결정`이 아니라 `입력 해석 보정 + 품질 해석 안정화`에만 들어간다.
* tempo, pause, rotation slant 같은 습관은 family 변경이 아니라 score 미세 조정과 quality 해석 보정에만 사용한다.
* 개인화 가중치는 제한적으로 둔다.

### 4-4. 오버레이는 독립 그림보다 작은 연산자 집합으로 시작

`request-answer11.md`, `request-answer12.md`, `request-answer15.md`의 방향을 따른다.

* root radical은 단순하게 유지한다.
* 복잡성은 root 자체가 아니라 overlay와 결과 규칙으로 이동한다.
* V1.5에서는 전체 overlay 10종을 한 번에 다 넣기보다, Web UI 검증용 최소 팩부터 시작한다.

### 4-5. raw stroke는 끝까지 보존

* UI는 beautify 결과로 원본 선을 대체하지 않는다.
* debug overlay는 ghost layer로만 얹는다.
* 로그에는 raw stroke, normalized stroke, overlay stroke를 모두 남긴다.

---

## 5. 제안하는 V1.5 구조

### 5-1. 단계형 입력 모델

V1.5에서는 자동 혼합 파싱보다 **명시적 2단계 입력**이 더 안전합니다.

1. `Base Draw`
   현재와 동일하게 기본 5문양을 그림
2. `Base Seal`
   기본 family를 먼저 확정
3. `Overlay Draw`
   같은 캔버스 위에 오버레이 획/연산자를 덧그림
4. `Final Seal`
   기본 family + overlay + quality + profile 보정을 묶어 최종 compile

이 구조를 권장하는 이유는 아래와 같습니다.

* 현재 recognizer를 크게 깨지 않고 확장 가능
* root와 overlay가 서로 오인식하는 문제를 줄일 수 있음
* UI 예측 가능성이 높음
* 디버깅과 테스트가 쉬움

### 5-2. 데이터 모델 제안

추가가 필요한 핵심 타입은 아래입니다.

```ts
type InputPhase = "base" | "overlay" | "compiled";

type OverlayOperator =
  | "steel_brace"
  | "electric_fork"
  | "ice_bar"
  | "soul_dot"
  | "void_cut"
  | "martial_axis";

interface UserInputProfile {
  sampleCount: number;
  averageQuality: QualityVector;
  averageDurationMs: number;
  averagePathLength: number;
}

interface OverlayRecognitionResult {
  candidates: Array<{ operator: OverlayOperator; score: number; notes: string[] }>;
  topOperator?: OverlayOperator;
  status: "recognized" | "ambiguous" | "invalid" | "incomplete";
}

interface CompiledSpellResult {
  baseFamily: GlyphFamily;
  overlays: OverlayOperator[];
  rawQuality: QualityVector;
  adjustedQuality: QualityVector;
  profileDelta: Partial<QualityVector>;
  finalStatus: RecognitionStatus;
}
```

### 5-3. 인식 파이프라인 제안

#### 기본 문양 파이프라인

현재 `src/recognizer/recognize.ts` 구조를 유지한다.

1. pointer stroke 수집
2. resample / smooth
3. translation / scale / global rotation normalization
4. feature extraction
5. family scoring
6. live preview
7. `seal`
8. canonical base family 확정

#### 오버레이 파이프라인

Base Seal 이후에만 진입한다.

1. overlay stroke 수집
2. base silhouette 기준 anchor zone 계산
3. overlay stroke 정규화
4. operator 후보 scoring
5. recognized / ambiguous / invalid / incomplete 판정
6. `final seal`에서 compiled result 생성

#### 품질 보정 파이프라인

1. raw quality 계산
2. user profile snapshot 읽기
3. comfort band 비교
4. 허용된 범위 내 score reweight
5. adjusted quality 생성
6. 로그에 raw / adjusted 둘 다 기록

---

## 6. 권장 최소 오버레이 팩

Web UI에서 먼저 확인할 최소 연산자 팩은 아래 6개를 권장합니다.

### 1차 구현 팩

* `steel_brace`
  * 의미: 강화, 보강
  * 형태: 외곽에 붙는 짧은 보강 바
* `electric_fork`
  * 의미: 방전, 분기
  * 형태: 짧은 갈라짐 tick
* `ice_bar`
  * 의미: 정지, 응결
  * 형태: main axis를 가로지르는 arrest bar
* `soul_dot`
  * 의미: 이탈된 생기
  * 형태: 본체 바깥의 detached dot
* `void_cut`
  * 의미: 비움, 결락
  * 형태: 중심 또는 contour의 hollow cut
* `martial_axis`
  * 의미: 전방 체현 실행
  * 형태: 전방 strike axis
  * 제약: `void_cut`가 먼저 있어야 recognized 가능

### 2차 확장 팩

아래는 V1.5 범위 끝단 또는 V1.6로 미룰 수 있습니다.

* `poison_drip`
* `thought_echo`
* `sacred_axis`
* `telekinesis_tether`

이번 단계에서 최소 팩부터 시작하는 이유는, root recognizer 안정성을 해치지 않고 overlay 문법을 먼저 증명하는 것이 우선이기 때문입니다.

---

## 7. 사용자 입력 품질 벡터 개선 전략

현재 품질 벡터는 이미 아래 7개로 고정돼 있습니다.

* `closure`
* `symmetry`
* `smoothness`
* `tempo`
* `overshoot`
* `stability`
* `rotationBias`

이번 단계의 개선은 **벡터 차원 추가**가 아니라 **해석 개선**에 가깝게 잡는 편이 안전합니다.

### 반드시 할 것

* raw quality와 adjusted quality를 분리
* profile 기반 comfort band 도입
* tempo와 rotationBias를 개인화 보정에 사용
* closure와 stability를 incomplete 판정 보조에 재사용

### 보수적으로 할 것

* family score 재가중은 작은 폭으로 제한
* 개인화로 invalid를 recognized로 뒤집지 않음
* profile이 적을 때는 baseline만 사용

### 지금은 하지 않을 것

* 의미 결정용 개인화 모델
* 대규모 사용자 적응 학습
* 외부 저장소 기반 계정 프로필

---

## 8. 세부 작업 분해

아래 작업은 실제 구현 순서 기준입니다.

### W01. 기준 동결 및 사양 파일 추가

목표:

* 이번 단계의 operator 팩과 입력 단계 모델을 문서와 코드 상수로 고정

세부 작업:

* V1.5 operator 최소 팩 확정
* base / overlay / compiled phase 상수화
* 오버레이별 anchor zone과 판독 규칙 문서화

대상 파일:

* `docs/20_queue/prototype-implementation-plan.md`
* `src/recognizer/types.ts`
* `src/recognizer/operator-templates.ts`

완료 기준:

* 코드와 문서가 같은 operator 이름과 phase 모델을 참조

### W02. user input profile 추가

목표:

* 같은 family 인식은 유지하면서, 품질 벡터 해석을 개인 comfort band로 안정화

세부 작업:

* 세션 로그에서 profile을 누적하는 구조 추가
* sampleCount가 적을 때 baseline fallback 유지
* rawQuality와 adjustedQuality 분리
* profileDelta 표시 준비

대상 파일:

* `src/recognizer/types.ts`
* 신규 `src/recognizer/user-profile.ts`
* `src/recognizer/quality.ts`
* `src/recognizer/recognize.ts`

완료 기준:

* fast / slow, straight / slanted 입력에서 family는 유지되고 quality 해석만 달라짐

### W03. overlay recognizer 추가

목표:

* base seal 이후 덧그린 획을 operator 후보로 판독

세부 작업:

* overlay stroke session 분리
* anchor zone 계산
* operator template 정의
* add / subtract / axis 계열 별 scoring 함수 구현
* operator preview status 구현

대상 파일:

* 신규 `src/recognizer/operators.ts`
* 신규 `src/recognizer/operator-templates.ts`
* `src/recognizer/geometry.ts`
* `src/recognizer/types.ts`

완료 기준:

* 최소 6개 operator가 recognized / ambiguous / invalid 기준으로 동작

### W04. compile 결과 모델 추가

목표:

* base family, overlay, quality, profile 보정을 하나의 canonical result로 묶기

세부 작업:

* `CompiledSpellResult` 타입 추가
* `martial_axis requires void_cut` 같은 의존 규칙 구현
* incomplete overlay 상태를 final compile에 반영
* export log schema 확장

대상 파일:

* `src/recognizer/types.ts`
* 신규 `src/recognizer/compile.ts`
* `src/recognizer/recognize.ts`

완료 기준:

* final result가 `base family만 있는 경우`와 `overlay까지 있는 경우`를 모두 설명 가능

### W05. Web UI 단계형 authoring 반영

목표:

* 현재 Web UI에서 base 입력과 overlay 입력을 명확히 나누고, 결과를 바로 확인 가능하게 만들기

세부 작업:

* phase indicator 추가
* `Seal Base`, `Start Overlay`, `Seal Final`, `Reset` 흐름 설계
* overlay preview panel 추가
* raw quality / adjusted quality 비교 UI 추가
* debug overlay에 anchor zone, operator ghost, profile delta 표시

대상 파일:

* `src/app.ts`
* `src/style.css`

완료 기준:

* 브라우저에서 base seal 후 overlay draw를 이어서 수행 가능
* final panel에서 base family, overlay list, adjusted quality를 함께 확인 가능

### W06. 로그와 export 확장

목표:

* 지금 단계에서 가장 중요한 분석 근거를 남기기

세부 작업:

* base session log와 overlay session log 분리
* profile delta와 adjusted quality 기록
* rawQuality / adjustedQuality / operatorCandidates 기록
* export JSON schema 문서화

대상 파일:

* `src/recognizer/types.ts`
* `src/app.ts`

완료 기준:

* export한 로그만으로도 한 번의 authoring 과정을 재현할 수 있음

### W07. 자동 테스트 추가

목표:

* root recognizer 회귀를 막고 overlay 인식과 보정 로직을 고정

세부 작업:

* base 5문양 기존 테스트 유지
* user profile 보정 테스트 추가
* operator 6종 인식 테스트 추가
* dependency rule 테스트 추가
* invalid overlay가 root를 망치지 않는 회귀 테스트 추가

대상 파일:

* `tests/recognizer.test.ts`
* `tests/recognizer-v15.test.ts`
* `tests/overlay-operators.test.ts`

완료 기준:

* `npm test`가 base root와 overlay 확장을 함께 검증

### W08. 수동 Web UI 검증

목표:

* 실제 브라우저 상호작용으로 이번 단계 목표를 확인

세부 작업:

* base canonical draw 검증
* `Seal Base -> Start Overlay -> Seal Final` 버튼 흐름 검증
* root + operator 조합 draw 검증
* overlay preview panel 확인
* debug overlay on/off 확인
* log export 확인
* build 확인

대상 명령:

* `npm test`
* `npm run build`
* `npm run dev`

완료 기준:

* 최소 검증 시나리오 전부 통과

---

## 9. 권장 파일 단위 구조 변경

현재 구조를 크게 깨지 않으려면 아래 분리가 적절합니다.

```text
src/recognizer/
  compile.ts
  geometry.ts
  operator-templates.ts
  operators.ts
  quality.ts
  recognize.ts
  templates.ts
  types.ts
  user-profile.ts
```

핵심 포인트는 아래입니다.

* `recognize.ts`는 base recognizer 중심으로 유지
* overlay 판독 로직은 별도 파일로 분리
* compile 단계는 `compile.ts`로 분리
* user profile 로직은 UI와 분리

---

## 10. 수동 검증 시나리오

브라우저에서 최소한 아래 시나리오는 직접 확인해야 합니다.

### 시나리오 A. root 유지

* 불꽃 기본형을 빠르게 그림
* 불꽃 기본형을 느리게 그림
* 결과: 둘 다 `fire`로 읽혀야 함
* 차이: `tempo`, `stability` 등 quality만 달라져야 함

### 시나리오 B. overlay 추가

* 땅 기본형 seal
* `Start Overlay`
* `steel_brace` 추가
* 결과: base는 `earth`, overlay는 `steel_brace`

### 시나리오 C. dependent overlay

* 기본형 seal
* `martial_axis`만 그림
* 결과: recognized가 아니라 incomplete 또는 invalid
* 이후 `void_cut`를 추가하고 다시 final seal
* 결과: `void_cut + martial_axis` 조합으로 compile

### 시나리오 D. invalid overlay 방어

* 바람 기본형 seal
* 의미 없는 scribble 추가
* 결과: base family는 유지되고 overlay만 invalid

### 시나리오 E. debug / export

* debug overlay on/off 확인
* anchor zone / operator ghost / phase 상태 확인
* overlay preview panel 확인
* export JSON 확인
* raw와 adjusted quality가 둘 다 기록되는지 확인

---

## 11. 주요 리스크와 대응

### 리스크 1. overlay가 base 인식을 다시 흔드는 문제

대응:

* base seal 이후 overlay phase로 분리
* base와 overlay stroke 세션을 물리적으로 분리

### 리스크 2. 개인화가 semantics를 오염시키는 문제

대응:

* 보정 폭 상한 설정
* invalid를 recognized로 뒤집는 권한 제거
* baseline fallback 유지

### 리스크 3. operator 수가 너무 많아져 UI가 복잡해지는 문제

대응:

* 1차 최소 팩 6개만 먼저 구현
* 나머지는 확장 팩으로 분리

### 리스크 4. 현재 테스트 자산이 base recognizer에만 치우친 문제

대응:

* overlay 전용 테스트 파일 추가
* root 회귀 테스트를 계속 유지

---

## 12. 완료 정의

이번 문서 기준의 완료는 아래를 모두 만족하는 상태입니다.

* 현재 5문양 base recognizer가 유지된다.
* base seal 이후 overlay를 덧그릴 수 있다.
* 최소 6개 operator가 Web UI에서 확인된다.
* 유저 입력 profile 기반 adjusted quality가 표시된다.
* `same shape = same family` 원칙이 유지된다.
* `seal` 후 compiled result와 export log를 확인할 수 있다.
* 자동 테스트와 build가 통과한다.

이 완료선까지 도달하면, 이후 단계에서는 `여러 마법진 연결`, `결과 생성`, `허수아비 전투`, `3D operator 확장`으로 넘어갈 수 있습니다.

# 튜토리얼 기반 개인화 인식 확장 실행 설계

이 문서는 현재 `Magic Recognizer V1.5` 구조를 유지한 채,
직접 마법진을 그리기 전 튜토리얼에서 얻는 사용자별 추적 입력을 활용해
**같은 모양인데 점수가 다른 family로 밀리거나 margin 부족으로 확정이 안 되는 문제**를 완화하기 위한 실행 설계를 정리합니다.

이번 문서의 핵심은 아래 한 줄입니다.

**최종 종류 판정 권한은 규칙계에 남겨 두고, 튜토리얼에서 얻은 소량의 사용자별 입력을 이용해 base family와 overlay operator의 후보 재정렬, 확신도 추정, 입력 보정을 개인화한다**

---

## 1. 진행 상태 메모

이 문서는 실행 설계 문서이면서, 현재 작업 트리에서 어디까지 구현됐는지를 함께 추적하는 기준 문서다.
즉, 아이디어 메모가 아니라 downstream task와 검수 작업이 바로 참조할 수 있는 구현 기준으로 쓴다.

현재 코드 기준 구현 상태:

* base family 5종과 overlay operator 6종은 이미 구현되어 있다.
* `same shape = same family`와 `martial_axis requires void_cut` 규칙은 core rule로 유지된다.
* `quality vector` 기반 `user profile`은 계속 유지된다.
* `src/recognizer/types.ts`, `src/recognizer/tutorial-profile.ts`, `src/app.ts`에 tutorial capture/store와 onboarding hook이 추가됐다.
* `src/recognizer/rerank.ts`를 통해 base family top-k rerank와 overlay hard-pair rerank가 구현됐다.
* `scripts/tutorial-dataset/*`에 tutorial/synthetic/public helper와 공통 NDJSON contract가 추가됐다.
* `tests/recognizer-v15.test.ts`, `tests/overlay-operators.test.ts`에 개인화 불변 조건과 hard negative 테스트가 추가됐다.
* 현재 앱 런타임은 `tutorialProfileStore`를 `recognizeSession`/`recognizeOverlayStroke` 호출에 주입하므로, live demo에서도 tutorial personalization이 실제 판정 경로에 연결된다.
* operator tutorial capture는 `baseSnapshot`과 `operatorContext`를 저장하고, overlay personalization은 `anchor/scale/stack` placement-aware rerank를 함께 사용한다.
* full tutorial UI와 학습 파이프라인 orchestration은 아직 이번 wave 범위 밖이다.

현재 작업 트리 검증:

* `npm test` 통과
* `npm run build` 통과
* `npm run validate:docs` 통과

이 문서는 위 구현을 기준으로 아래 남은 통합 범위를 다룬다.

* tutorial capture를 실제 recognizer runtime에 연결
* 사용자별 shape/operator profile의 live personalization 반영
* synthetic / public / tutorial data를 함께 쓰는 hybrid 전략의 후속 학습 파이프라인 연결

---

## 2. 기준 문서

이번 설계는 아래 문서를 기준으로 사용한다.

* `chat/request-answer01.md`
* `chat/request-answer03.md`
* `chat/request-answer08.md`
* `chat/request-answer11.md`
* `chat/request-answer14.md`
* `chat/request-answer18.md`
* `docs/00_source_map/client-decisions.md`
* `docs/10_direction/final-direction.md`
* `docs/10_direction/prototype-target.md`
* `docs/10_direction/symbol-prototypes.md`
* `docs/20_queue/prototype-implementation-plan.md`
* `docs/30_tasks/epic-02-symbols-and-input/task-02-input-interpretation-rules.md`
* `docs/30_tasks/epic-02-symbols-and-input/task-03-lightweight-assist-hook.md`
* `docs/30_tasks/epic-07-future-expansion-backlog/task-02-lightweight-ai-backlog.md`

---

## 3. 현재 문제 정의

현재 user test에서 관찰된 문제는 아래 두 가지다.

* 유저가 의도한 동일 도형에 대해 다른 family의 점수가 더 높게 나온다.
* top score와 2위 score의 차이가 작아 `ambiguous`가 자주 발생한다.

현재 recognizer 구조를 보면 이 현상은 예외가 아니라, 아래 조건에서 자연스럽게 생긴다.

### 3-1. base family

현재 base family 기준형은 아래 5개다.

* 바람: `3개 평행 개방선`
* 땅: `하변이 더 긴 폐합 사다리꼴`
* 불꽃: `상향 인상의 폐합 삼각형`
* 물: `단일 원형 폐합 루프`
* 생명: `줄기와 상단 분기가 있는 rooted Y`

이 구조에서 특히 충돌이 잦은 쌍은 아래로 본다.

* `불꽃` vs `땅`
  * 닫힌 다각형, corner 수, fill ratio가 비슷해질 수 있음
* `물` vs `생명`
  * closure leak, endpoint cluster, branch cue가 흔들리면 충돌 가능
* `불꽃` vs `미완성 불꽃`
  * 같은 의도였지만 closure가 부족하면 `incomplete`
* `바람` vs 기타
  * stroke 수나 평행성이 무너지면 다른 family보다 invalid/ambiguous로 가기 쉬움

### 3-2. overlay operator

현재 operator는 아래 6개다.

* `steel_brace`
* `electric_fork`
* `ice_bar`
* `soul_dot`
* `void_cut`
* `martial_axis`

overlay는 base보다 더 취약한데 이유는 아래와 같다.

* shape 자체가 선형 primitive에 가깝다.
* anchor zone과 scale까지 맞아야 한다.
* `martial_axis`는 `void_cut` dependency가 있다.

특히 아래 충돌을 hard negative로 본다.

* `void_cut` vs `electric_fork`
* `ice_bar` vs 짧은 partial stroke
* `steel_brace` vs open box / slash-like stroke
* `martial_axis` shape는 맞지만 `void_cut`가 없는 케이스

### 3-3. 현재 구조상 중요한 해석

현재 문제는 `quality vector`가 아니라 `family scoring / operator scoring / margin threshold / user articulation bias` 쪽이다.

즉, 이번 확장은 아래를 다룬다.

* 입력이 어떤 family/operator에 더 가까운가
* 현재 유저의 평소 articulation을 고려하면 이 입력을 얼마나 확신해도 되는가

반대로 아래는 그대로 둔다.

* 최종 family semantics
* operator dependency rule
* final canonicalization 권한

---

## 4. 불변 원칙

### 4-1. 최종 semantics는 규칙계가 가진다

아래는 절대 바꾸지 않는다.

* `same shape = same family`
* `same operator shape = same operator meaning`
* `martial_axis requires void_cut`
* `seal` 시점에만 canonical 확정

### 4-2. 개인화는 입력 해석 보조만 한다

개인화가 개입할 수 있는 곳은 아래뿐이다.

* primitive fitting
* candidate reranking
* ambiguity / confidence estimation
* user-specific tolerance 보정

개인화가 개입하면 안 되는 곳은 아래다.

* family semantics 재정의
* operator dependency rule 변경
* compile / final result 의미 결정

### 4-3. 튜토리얼은 소량 정답 데이터로 취급한다

튜토리얼은 “유저가 지금 어떤 라벨을 의도했는지 명백한 입력”을 모으는 시간이다.

따라서 튜토리얼 데이터는 공개 데이터보다 더 높은 우선순위를 가진다.

---

## 5. 튜토리얼 수집 설계

### 5-1. 전제 UX

직접 마법진을 그리기 전,
다음과 같은 프롬프트로 짧은 튜토리얼을 제공한다.

* `다음 화면의 도형을 따라 그려 보세요`
* `이번에는 보지 않고 다시 그려 보세요`
* `이번에는 조금 더 빠르게 그려 보세요`

이 설계의 목적은 단순 onboarding이 아니라,
**유저의 shape bias와 stroke articulation bias를 소량 수집**하는 것이다.

### 5-2. 권장 튜토리얼 구성

최소 수집 단위는 아래로 잡는다.

#### base family

각 family마다 2~3회.

권장 방식:

1. 기준형 따라 그리기 1회
2. 기준형을 숨기고 기억 재현 1회
3. 속도 변형 또는 기울기 변형 1회

즉, base 5종 기준 최소 10회, 권장 15회다.

#### overlay operator

각 operator마다 1회 이상.

권장 방식:

* shape를 base 위 특정 anchor zone에 맞춰 그리게 함
* `martial_axis`는 반드시 `void_cut` 이후 예시와 함께 보여 줌

즉, operator 6종 기준 최소 6회다.

#### 전체

권장 초기 튜토리얼 총량:

* 최소: `base 10 + operator 6 = 16` 샘플
* 권장: `base 15 + operator 6~12 = 21~27` 샘플

### 5-3. 튜토리얼 과제 설계 원칙

* 기준형 따라 그리기만으로 끝내지 않는다.
* 기억 재현 샘플을 반드시 포함한다.
* 속도/기울기 variation을 약하게 포함한다.
* `same shape = same family` 규칙을 튜토리얼에서 바로 설명한다.
* 튜토리얼 입력은 별도 flag로 저장한다.

---

## 6. 데이터 전략

이번 설계는 데이터를 3층으로 나눈다.

### 6-1. 내부 합성 데이터

주력 데이터다.

현재 template와 operator shape가 이미 코드로 정의되어 있으므로,
아래 변형을 자동 생성한다.

* scale
* rotation
* translation
* stroke order 변화
* closure leak
* jitter
* overshoot
* corner rounding
* partial stroke
* anchor zone shift

합성 데이터의 역할:

* base family prototype bootstrap
* operator prototype bootstrap
* hard negative 세트 생성
* 작은 모델 사전학습

### 6-2. 공개 데이터

공개 데이터는 직접 라벨 대응보다 **표현 학습과 전처리 보조** 용도로 쓴다.

후보군:

* Quick, Draw! raw vector data
  * triangle / circle 계열 pretraining에 유리
* $-family gesture logs
  * multistroke articulation과 gesture invariance 실험에 유리
* CROHME InkML
  * online ink normalization, slash/line/dot primitive 표현 보조에 유리

공개 데이터의 역할:

* stroke encoder 사전학습
* denoising / smoothing prior
* sequence representation 학습

직접 family semantics 학습에는 쓰지 않는다.

### 6-3. 튜토리얼 데이터

튜토리얼 데이터는 user-specific adaptation의 핵심이다.

튜토리얼 데이터의 역할:

* user prototype bank 생성
* family/operator별 tolerance 추정
* 개인화 rerank
* confidence calibration

### 6-4. 우선순위

실행 우선순위는 아래와 같다.

1. 내부 합성 데이터
2. 튜토리얼 데이터
3. 공개 데이터

즉, 공개 데이터는 있으면 좋지만,
현재 구조에서 가장 큰 효과는 `합성 + 튜토리얼` 조합에서 나온다.

---

## 7. base family와 operator를 함께 고려한 적용 포인트

### 7-1. base family

#### 바람

* 핵심 cue: stroke count, parallelism, openness
* 튜토리얼에서 얻을 것:
  * 사용자의 평행선 간격 습관
  * stroke ordering
  * line wobble
* 개인화 적용:
  * parallelism tolerance
  * stroke count completeness confidence

#### 땅

* 핵심 cue: closure, 4-corner structure, fill ratio
* 튜토리얼에서 얻을 것:
  * 사용자의 사다리꼴 찌그러짐 패턴
  * base width bias
* 개인화 적용:
  * `earth vs fire` rerank
  * closure threshold calibration

#### 불꽃

* 핵심 cue: closure, 3-corner structure, upward triangle impression
* 튜토리얼에서 얻을 것:
  * apex 위치 편향
  * 삼각형 기울기 편향
* 개인화 적용:
  * `fire vs earth` rerank
  * tilt 허용 범위 보정

#### 물

* 핵심 cue: closure, circularity, smoothness
* 튜토리얼에서 얻을 것:
  * 원형을 타원처럼 그리는 습관
  * pen lift / open loop 습관
* 개인화 적용:
  * `water vs incomplete`
  * `water vs life` rerank

#### 생명

* 핵심 cue: branch cue, endpoint cluster, openness
* 튜토리얼에서 얻을 것:
  * 가지 각도 편향
  * rooted Y의 중심축 흔들림
* 개인화 적용:
  * branch cue 가중치 calibration
  * `life vs water/open-loop` rerank

### 7-2. overlay operator

#### steel_brace

* 핵심 cue: open rectangular brace shape
* 개인화 적용:
  * aspect ratio tolerance
  * open-form completeness confidence

#### electric_fork

* 핵심 cue: broken zigzag / forked direction
* 개인화 적용:
  * corner count vs diagonal slash 분리
  * `void_cut`와 top-2 rerank

#### ice_bar

* 핵심 cue: straight horizontal bar
* 개인화 적용:
  * minimum scale / straightness threshold calibration

#### soul_dot

* 핵심 cue: small closed circular dot
* 개인화 적용:
  * circle smallness / closure tolerance

#### void_cut

* 핵심 cue: strong ascending slash
* 개인화 적용:
  * angle tolerance
  * `electric_fork`와 rerank

#### martial_axis

* 핵심 cue: vertical axis + crossbar
* 개인화 적용:
  * shape confidence 보조만 허용
  * `requiresOperator`는 규칙계가 그대로 강제

---

## 8. 현재 구현에 붙일 구체 변경안

### 8-1. 타입

`src/recognizer/types.ts`에 아래 계열을 추가한다.

```ts
type TutorialTargetKind = "family" | "operator";

interface TutorialCapture {
  id: string;
  kind: TutorialTargetKind;
  expectedFamily?: GlyphFamily;
  expectedOperator?: OverlayOperator;
  strokes: Stroke[];
  source: "trace" | "recall" | "variant";
  timestamp: number;
}

interface FamilyPrototype {
  family: GlyphFamily;
  normalizedClouds: PointSample[][];
  averageFeatures: Partial<RecognitionFeatures>;
  sampleCount: number;
}

interface OperatorPrototype {
  operator: OverlayOperator;
  normalizedClouds: PointSample[][];
  sampleCount: number;
}

interface UserShapeProfile {
  tutorialSampleCount: number;
  familyPrototypes: Partial<Record<GlyphFamily, FamilyPrototype>>;
  operatorPrototypes: Partial<Record<OverlayOperator, OperatorPrototype>>;
  confusionPairs: Array<{ left: string; right: string; weight: number }>;
  updatedAt: number;
}

interface RecognitionCalibration {
  userPrototypeWeight: number;
  rerankStrength: number;
  confidenceBias: number;
}
```

### 8-2. 프로필

기존 `src/recognizer/user-profile.ts`는 유지하되,
shape/operator personalization은 새 모듈로 분리한다.

신규 파일:

* `src/recognizer/tutorial-profile.ts`

이 파일의 역할:

* 튜토리얼 샘플 누적
* family/operator prototype 계산
* user-specific calibration 계산

### 8-3. base recognizer 보조층

신규 파일:

* `src/recognizer/rerank.ts`

이 파일의 역할:

* base candidates와 feature를 입력으로 받음
* global score + user prototype similarity + confusion pair bias를 합쳐 rerank
* final `recognized / ambiguous / incomplete / invalid` 판단에 참고할 confidence score 반환

중요:

* `recognize.ts`는 여전히 candidate 생성의 중심이다.
* `rerank.ts`는 top-k 재정렬과 confidence 보조만 한다.

### 8-4. overlay recognizer 보조층

`src/recognizer/operators.ts`는 아래 구조로 확장한다.

* 기존 candidate 생성 유지
* optional user operator prototype similarity 계산 추가
* angle/scale/anchor 기반 confusion pair rerank 허용
* `requiresOperator`는 규칙 그대로 유지

### 8-5. app/UI hook

현재 wave에서는 full tutorial UI를 구현하지 않는다.
대신 아래 hook만 설계한다.

* 튜토리얼 진입 전용 state
* 튜토리얼 샘플 저장 함수
* `UserShapeProfile` local storage key
* later integration용 panel / onboarding 진입점

즉, 이번 wave는 **profile and recognizer core first**로 본다.

### 8-6. offline dataset helper

필요 시 신규 파일:

* `scripts/tutorial-dataset/README.md`
* `scripts/tutorial-dataset/convert-quickdraw.mjs`
* `scripts/tutorial-dataset/generate-synthetic.mjs`

이들은 즉시 구현이 아니라 backlog와 연결되는 helper 자리로 둔다.

---

## 9. 적용 방식

### 단계 1. heuristic-only personalization

구현 우선순위 1.

구성:

* user family prototype
* user operator prototype
* score blending
* confidence calibration

이 단계에서는 ML 없이도 상당수 개선이 가능하다.

### 단계 2. feature-based tiny reranker

구현 우선순위 2.

입력:

* current feature vector
* top-k candidate score
* user profile summary

후보 모델:

* logistic regression
* tiny gradient boosting
* tiny MLP

출력:

* reranked family/operator confidence

### 단계 3. pretrained encoder + few-shot support

구현 우선순위 3.

공개 데이터와 합성 데이터로 작은 encoder를 pretrain한 뒤,
tutorial support set으로 user adaptation을 수행한다.

이 단계는 backlog로 둔다.

---

## 10. 테스트와 수용 기준

### 10-1. base family

아래 케이스를 반드시 둔다.

* `fire` 튜토리얼 profile 적용 전후 비교
* `earth` 튜토리얼 profile 적용 전후 비교
* `water vs life` confusion pair 비교
* same shape에서 family flip이 증가하지 않는지 확인

### 10-2. overlay operator

아래 케이스를 반드시 둔다.

* `void_cut` / `electric_fork` top-2 rerank
* `steel_brace` user bias 반영
* `martial_axis requires void_cut`가 personalization 후에도 유지되는지 확인

### 10-3. acceptance 기준

이번 설계의 수용 기준은 아래다.

* 튜토리얼 profile 적용 후 `family flip`이 증가하면 실패
* `ambiguous`가 줄거나 confidence calibration이 개선돼야 한다
* operator dependency가 깨지면 실패
* same shape invariance가 흔들리면 실패

중요:

* accuracy 하나만으로 성공을 판단하지 않는다.
* `false confidence 증가`도 실패로 본다.

---

## 11. 리스크와 중단선

### 리스크 1. 따라 그리기 bias

튜토리얼이 기준형 trace에만 치우치면 실제 자유 입력과 분포가 달라진다.

대응:

* trace + recall + variation을 함께 수집

### 리스크 2. user overfit

튜토리얼 데이터가 너무 적을 때 특정 family/operator에 과하게 끌릴 수 있다.

대응:

* prototype weight 상한 고정
* global heuristic 최소 비중 유지

### 리스크 3. semantics leakage

개인화가 family semantics를 바꾸는 것처럼 보일 수 있다.

대응:

* final canonicalization은 여전히 규칙계
* personalization은 top-k rerank와 confidence 보조에만 제한

### 리스크 4. operator dependency 붕괴

shape는 맞지만 grammar가 틀린 경우를 모델이 정답처럼 밀 수 있다.

대응:

* dependency는 모델 밖 규칙계가 강제

---

## 12. 구현 wave 제안

### W01. 튜토리얼 입력 캡처 타입과 저장 형식 정의

대상:

* `src/recognizer/types.ts`
* `src/recognizer/tutorial-profile.ts`

완료 기준:

* 튜토리얼 입력을 저장할 타입이 생긴다.

현재 상태:

* 구현됨. `TutorialCapture`, `TutorialProfileStore`, `UserShapeProfile`, `RecognitionCalibration`과 append/hydrate helper가 추가됐다.

### W02. user family/operator prototype 계산

대상:

* `src/recognizer/tutorial-profile.ts`

완료 기준:

* base 5종, operator 6종에 대한 user prototype을 계산할 수 있다.

현재 상태:

* 구현됨. family/operator prototype 집계, confusion pair 계산, calibration 산출이 `src/recognizer/tutorial-profile.ts`에 들어갔다.

### W03. base family personalization rerank

대상:

* `src/recognizer/recognize.ts`
* `src/recognizer/rerank.ts`

완료 기준:

* top-k rerank와 confidence calibration이 base family에 적용된다.

현재 상태:

* 구현됨. `recognize.ts`가 `rerankBaseCandidates`를 호출하고, test에서 earth-vs-fire near tie 보정과 clear fire 유지가 검증된다.

### W04. overlay personalization rerank

대상:

* `src/recognizer/operators.ts`
* `src/recognizer/rerank.ts`

완료 기준:

* operator confidence와 ambiguous handling에 user profile이 반영된다.

현재 상태:

* 구현됨. `operators.ts`가 `rerankOverlayCandidates`를 호출하고, dependency 유지, off-anchor 보호, hard pair rerank cap이 test로 고정됐다.

### W05. tutorial/onboarding hook

대상:

* `src/app.ts`

완료 기준:

* later integration 가능한 tutorial entry hook가 정리된다.

현재 상태:

* 부분 구현. `src/app.ts`에 localStorage 기반 tutorial hook과 metadata 노출이 추가됐지만, recognizer 호출부는 아직 `tutorialProfileStore`를 직접 사용하지 않는다.

### W06. synthetic/public/tutorial hybrid 데이터 파이프라인

대상:

* `scripts/tutorial-dataset/*`
* 별도 backlog task

완료 기준:

* offline dataset strategy가 구현 준비 수준으로 정리된다.

현재 상태:

* 구현됨. `scripts/tutorial-dataset/*` helper와 appendix contract가 추가됐고, 실제 external raw cache downloader(`download-quickdraw`, `download-dollar-family`, `download-crohme`, `download-all`)와 public auxiliary batch converter(`convert-all-public`)가 들어와 있다. 다만 실제 학습 orchestration은 아직 범위 밖이다.

---

## 13. queue와 task 연결

이 문서는 아래 downstream task의 기준 문서로 사용한다.

* `T02-04 튜토리얼 입력 수집 기준 정리`
* `T02-05 사용자 shape profile과 prototype bank 설계`
* `T02-06 개인화 rerank와 confidence calibration 정리`
* `T07-05 hybrid tutorial/public/synthetic 데이터 전략 backlog`
* `T07-06 소형 모델 실험안 backlog`
* `T07-08 tiny ML baseline 설계와 offline 실험안`
* `T07-09 tutorial-aware personalization adapter 설계`

즉, 이 문서는 구현보다 앞서지 않는 범위에서,
**현재 구현 상태와 남은 통합 작업을 함께 관리하는 설계 기준서**로 유지한다.

tiny ML baseline과 tutorial-aware personalization의 상세 후속 설계는
[`tiny-ml-baseline-plan.md`](tiny-ml-baseline-plan.md)를 직접 참조한다.

---

## Appendix A. hybrid dataset helper contract

W06은 대형 학습 파이프라인부터 여는 것이 아니라,
아래 helper를 먼저 두고 dataset contract를 고정하는 방식으로 시작한다.

대상 helper:

* `scripts/tutorial-dataset/README.md`
* `scripts/tutorial-dataset/generate-synthetic.mjs`
* `scripts/tutorial-dataset/convert-tutorial-captures.mjs`
* `scripts/tutorial-dataset/convert-quickdraw.mjs`
* `scripts/tutorial-dataset/convert-dollar-family.mjs`
* `scripts/tutorial-dataset/convert-crohme.mjs`

### A-1. 공통 출력 형식

helper 출력은 모두 line-delimited NDJSON으로 맞춘다.

필수 필드:

* `dataset`
* `kind`
* `label`
* `split`
* `priority`
* `usage.allowed`
* `usage.forbidden`
* `strokes`
* `normalizedStrokes`
* `metadata`

의도:

* tutorial / synthetic / public 보조 데이터를 한 포맷으로 비교 가능하게 만든다.
* recognizer core를 바꾸지 않고 offline dataset helper만 먼저 돌릴 수 있게 한다.

### A-2. tutorial 우선순위

hybrid merge 우선순위는 아래로 고정한다.

1. `tutorial_primary`
2. `synthetic_bootstrap` 또는 `synthetic_tutorial_like`
3. `public_auxiliary`

즉, user adaptation, prototype bank, rerank calibration은 tutorial 데이터를 먼저 쓴다.
synthetic는 bootstrap과 hard negative 보강용이고,
공개 데이터는 그 뒤 보조 계층으로만 붙는다.

### A-3. synthetic 생성 규칙

synthetic helper는 현재 canonical template를 seed로 하여 아래 변형만 조합한다.

* scale
* rotation
* translation
* stroke order 변화
* stroke direction 변화
* closure leak
* jitter
* overshoot
* corner rounding
* partial stroke
* operator anchor shift

preset은 아래 3종으로 제한한다.

* `bootstrap`
* `tutorial-like`
* `hard-negative`

특히 `hard-negative`는 아래 confusion pair 중심으로 만든다.

* `earth` vs `fire`
* `water` vs `life`
* `void_cut` vs `electric_fork`
* `steel_brace` vs partial/open box 유사형
* `ice_bar` vs 짧은 partial stroke

### A-4. 공개 데이터 제한

Quick, Draw / `$-family` / CROHME는 직접 family/operator classifier를 학습시키는 정답셋이 아니다.

허용 용도:

* stroke encoder pretraining
* denoising / smoothing prior
* normalization regression
* sequence representation 학습

금지 용도:

* public label을 `wind`/`earth`/`fire`/`water`/`life`의 직접 정답처럼 매핑
* public label을 overlay operator 정답처럼 매핑
* 공개 데이터 기반 모델이 canonical semantics를 덮어쓰게 만드는 것

즉, 공개 데이터는 어디까지나 **전처리/표현학습 보조**다.

### A-5. 이번 wave의 stop condition

이번 wave에서 필요한 것은 아래까지다.

* synthetic 생성 규칙이 코드와 README에 남아 있다.
* tutorial export를 공통 포맷으로 변환할 수 있다.
* Quick, Draw / `$-family` / CROHME를 auxiliary NDJSON으로 변환할 수 있다.
* tutorial-first 원칙과 public-data 제한이 문서와 helper에 함께 적혀 있다.

반대로 이번 wave에서 아직 하지 않는 것은 아래다.

* 대용량 다운로드 자동화
* 학습 파이프라인 orchestration
* 라이선스 audit 세부안
* recognizer core 직접 수정

### A-6. operator tutorial context v2

현재 구현은 operator tutorial capture에 아래 정보를 함께 저장한다.

* `baseSnapshot`
  * base reference frame의 centroid / bounds / diagonal / axisAngleRadians
* `operatorContext`
  * `stackIndex`
  * `existingOperators`
  * `strokeBounds`
  * `anchorZoneId`
  * `anchorScore`
  * `scaleRatio`
  * `angleRadians`

store rebuild는 이 정보를 사용해 아래 placement summary를 계산한다.

* `averageAnchorZoneId`
* `averageScaleRatio`
* `averageStackIndex`
* `existingOperatorBiases`

live overlay personalization은 아래 순서를 따른다.

1. 기존 heuristic candidate 생성
2. dependency / anchor / scale gate 적용
3. shape similarity 계산
4. placement similarity 계산
5. gated rerank 적용

중요:

* placement personalization은 **보너스/패널티 보조층**이며, canonical meaning을 바꾸지 않는다.
* `blockedBy`, `anchorScore`, `scaleScore`는 personalization이 우회할 수 없다.
* `shape confidence`는 계속 shape similarity 중심으로 계산하고, placement는 score 보조에만 더 가깝게 사용한다.

현재 wave의 수용 기준:

* `void_cut` / `electric_fork` hard-negative에서 placement-aware personalization이 작동한다.
* off-anchor stroke는 personalization이 있어도 인정되지 않는다.
* underscaled `ice_bar` 같은 wrong-scale 입력은 personalization이 rescue하지 않는다.
* `martial_axis requires void_cut` dependency는 계속 규칙계에서 막힌다.

이번 wave 이후에 남는 후속 범위:

* richer placement context
  * overlay stack 중 n번째였는지에 대한 더 정밀한 slot prior
  * base silhouette sector / seam context
* learned placement embedding 또는 tiny model
  * heuristic/profile stop condition을 만족한 뒤에만 backlog로 다룬다.

# 소형 ML baseline + 유저 개인화 확장 실행 설계

이 문서는 현재 `Magic Recognizer V1.5`의 규칙 중심 구조를 유지한 채,
public auxiliary 데이터, synthetic 데이터, 향후 tutorial vector capture를 함께 활용하는
**첫 tiny ML baseline**을 어떻게 설계하고 붙일지 정리한다.

핵심 한 줄:

**모델은 canonical family/operator를 직접 결정하지 않고, 기존 heuristic candidate를 보정하는 rerank/calibration 보조층으로만 작동한다.**

---

## 1. 현재 기준과 목표

현재 작업 트리 기준으로 이미 구현된 것:

* base family 5종, overlay operator 6종의 규칙 기반 recognizer
* base top-k rerank와 operator hard-pair rerank
* tutorial profile store, family/operator prototype, placement-aware operator personalization
* public auxiliary raw cache와 NDJSON helper
  * Quick Draw raw 4개 category
  * `$-family` XML / MMG
  * CROHME package loose InkML 일부
* `scripts/tutorial-dataset/build-ml-baseline-manifests.mjs`와 `scripts/tutorial-dataset/ml-contract.mjs`
  * `artifacts/ml/dataset-split-v1.json`
  * `artifacts/ml/feature-spec-v1.json`
* `scripts/ml-baseline/*` Python sidecar와 base/operator baseline artifact
  * `artifacts/ml/base-rerank-v1.json`
  * `artifacts/ml/base-confidence-v1.json`
  * `artifacts/ml/operator-rerank-v1.json`
  * `artifacts/ml/operator-confidence-v1.json`
* `src/recognizer/rerank.ts`, `src/recognizer/recognize.ts`, `src/recognizer/operators.ts`, `src/app.ts`에 artifact loader와 shadow-mode runtime summary가 연결돼 있다.

현재 작업 트리 기준으로 아직 계획/후속 상태로 남아 있는 것:

* public auxiliary encoder/normalizer pretrain을 runtime 필수 의존으로 여는 작업
* tutorial vector capture UI와 실제 `adaptation` / `acceptance_eval` 수집 루프
* shadow mode 결과를 최종 decision에 반영하는 gate-open rollout
* `T02-08` 계약 위에서 tutorial-aware ML adapter를 여는 최종 단계

현재 구현 검증:

* `npm test` 통과
* `npm run build` 통과
* `npm run validate:docs` 통과

이 문서의 목표:

* 현재 수집된 public auxiliary와 synthetic helper를 기반으로 **offline tiny ML baseline**을 설계한다.
* 이후 tutorial vector capture가 들어왔을 때 **유저 개인화**를 어떤 방식으로 결합할지 결정 완료 수준으로 정리한다.
* 모델이 개입하면 안 되는 경계를 명확히 고정한다.

---

## 2. 불변 원칙

아래는 tiny ML baseline 이후에도 바꾸지 않는다.

* `same shape = same family`
* `same operator shape = same operator meaning`
* `martial_axis requires void_cut`
* canonical family/operator semantics는 규칙계가 가진다.
* 모델은 `primitive/normalization 보조`, `top-k rerank`, `confidence calibration`, `tutorial-aware adaptation`까지만 허용한다.
* off-anchor / wrong-scale 입력을 모델이 구조적으로 rescue하면 안 된다.
* tutorial capture는 **vector stroke** 기준으로만 다루고, raster image 혼합은 v1 baseline에서 제외한다.

---

## 3. 현재 데이터 분석

### 3-1. 실제 수집 상태

현재 auxiliary NDJSON 기준 수량:

* Quick Draw: `514,739`
* `$-family`: `14,878`
* CROHME: `488`
* 합계: `530,105`

raw cache 크기:

* `external/quickdraw`: 약 `755MB`
* `external/dollar`: 약 `99MB`
* `external/crohme`: 약 `68MB`
* `tmp/tutorial-dataset`: 약 `3.0GB`

### 3-2. 데이터별 품질과 적합도

#### Quick Draw

관찰:

* 4개 category만 수집: `circle`, `triangle`, `line`, `square`
* `recognized=true` 비율이 높다.
* `82.68%`가 1획이다.
* 평균 `80.54` point / sample

강점:

* `circle`, `triangle`, `line`, `square` primitive prior에 강하다.
* raw timestamp vector라 online stroke encoder pretrain에 바로 쓸 수 있다.
* denoising / normalization regression의 초기 데이터로 충분히 크다.

약점:

* `wind`, `life`, `electric_fork`, `steel_brace`, `martial_axis` 직접 대응이 약하다.
* 국가 분포가 크고, US 비중이 매우 높다.
* 단일 stroke 편향이 강하다.

도메인 적합도:

* `water`: 높음
* `fire`: 높음
* `earth`: 중간
* `ice_bar`, `void_cut`: 중간~높음
* `wind`, `life`, `electric_fork`, `steel_brace`, `martial_axis`: 낮음

#### `$-family`

관찰:

* 총 `14,878` XML이 그대로 NDJSON으로 들어왔다.
* `56.45%`가 2획 이상이다.
* 평균 `73.21` point / sample
* label은 semantic class가 아니라 gesture log id에 가깝다.

강점:

* multistroke articulation
* stroke order / direction invariance
* grouping robustness

약점:

* family/operator semantic supervision으로는 쓸 수 없다.
* label 그대로는 도메인 정답과 연결되지 않는다.

도메인 적합도:

* `wind`, `life`, `electric_fork`, `martial_axis`처럼 획 구조가 중요한 대상의 encoder pretrain에 유리
* canonical family/operator 분류에는 직접 부적합

#### CROHME

관찰:

* 현재 loose InkML로 잡힌 것은 `488`건뿐이다.
* 실제 loose `.inkml`는 `CROHME2012_data/testDataGT` 편향이다.
* 평균 `18.17` stroke / sample, `444.66` point / sample
* expression-level LaTeX label이 메타데이터에 있다.

강점:

* dense online ink normalization
* long sequence representation
* expression-level stroke robustness

약점:

* 현재 수집본은 partial coverage다.
* symbol-level direct supervision이 아니다.
* expression 전체라 operator primitive 학습에는 직접 쓰기 어렵다.

도메인 적합도:

* normalization / sequence robustness에는 중간~높음
* base/operator 직접 분류에는 낮음

### 3-3. 핵심 해석

현재 auxiliary 데이터는 아래 용도로만 충분히 유효하다.

* primitive proposal 보조
* normalization / smoothing prior
* stroke encoder pretrain
* sequence representation

반대로 아래는 아직 부적합하다.

* canonical family direct classifier
* canonical operator direct classifier
* dependency-aware operator resolver
* tutorial 없이 user personalization 직접 학습

즉, public auxiliary는 **generic stroke prior**, synthetic는 **도메인 supervision**, tutorial은 **유저 adaptation**으로 역할을 분리해야 한다.

---

## 4. tiny ML baseline 설계

### 4-1. 전체 구조

baseline은 4층으로 나눈다.

1. public auxiliary pretrain
2. synthetic-supervised rerank baseline
3. shadow-mode runtime scorer
4. tutorial-aware personalization adapter

초기 구현은 1~3까지를 완성하고, 4는 tutorial vector capture가 준비된 뒤 활성화한다.
현재 작업 트리 기준으로는 2~3과 artifact/runtime shadow hook이 먼저 구현되어 있고,
1은 manifest/export 수준, 4는 `T02-08` 이후 gate-open 전 준비 단계로 남아 있다.

### 4-2. 구현 스택

기본 스택은 아래로 고정한다.

* 학습/실험: Python sidecar
  * `scikit-learn` 중심
* runtime 추론: Node/TS
  * JSON artifact 로딩
  * 규칙계 우선, ML은 optional 보조

이유:

* feature 기반 baseline은 `scikit-learn`이 가장 빠르고 안정적이다.
* 현재 런타임은 TypeScript이므로, 학습 결과는 경량 artifact로 export하는 편이 결합도가 낮다.

### 4-3. base family baseline

#### 목적

* `earth vs fire`
* `water vs life`
* `recognized vs ambiguous`

를 더 안정적으로 보정한다.

#### 입력 feature

기존 `recognize.ts`와 `rerank.ts`에서 이미 계산하는 값을 그대로 쓴다.

핵심 입력:

* top-k candidate score
* top-k `templateDistance`
* `RecognitionFeatures`
  * `strokeCount`
  * `closureGap`
  * `dominantCorners`
  * `endpointClusters`
  * `circularity`
  * `fillRatio`
  * `parallelism`
  * `rawAngleRadians`
* quality 일부
  * `closure`
  * `smoothness`
  * `stability`
  * `rotationBias`
* top-1 / top-2 margin
* candidate pair id

#### 출력

* per-candidate rerank delta
* recognized/ambiguous confidence
* optional ambiguity probability

#### 모델 후보

* baseline 1: logistic regression
* baseline 2: gradient boosting
* baseline 3: tiny MLP

기본 선택:

* `gradient boosting`를 v1 main baseline으로 사용
* `logistic regression`을 parity baseline으로 둔다.

이유:

* 현재 feature는 전형적인 tabular feature다.
* data 규모와 해석 가능성을 고려하면 GBDT가 첫 baseline으로 가장 안정적이다.

### 4-4. overlay operator baseline

#### 목적

* `void_cut vs electric_fork`
* `ice_bar vs partial stroke`
* `steel_brace vs open box-like`
* `shape-confidence vs placement-confidence`

를 보정한다.

#### 입력 feature

기존 `operators.ts`, `rerank.ts`, `tutorial-profile.ts` 기반으로 아래를 사용한다.

* top-k operator score
* `templateDistance`
* `shapeConfidence`
* `anchorScore`
* `scaleScore`
* `angleRadians`
* `scaleRatio`
* `straightness`
* `corners`
* `closure`
* `placementAnchorZoneId`
* `stackIndex`
* `existingOperators`
* hard-pair id
* `blockedBy` 여부

#### 출력

* pairwise rerank delta
* operator confidence scalar
* false-positive suppression score

#### 모델 구조

v1은 두 갈래로 고정한다.

* `pairwise GBDT`
  * `void_cut/electric_fork` 등 hard pair 전용
* `confidence calibrator`
  * operator top-1에 대한 calibrated confidence

중요:

* `blockedBy`가 있는 경우 모델 출력은 무시한다.
* `anchorScore`와 `scaleScore` gate가 약할 때는 모델 delta를 cap으로 더 줄인다.
* 모델은 operator semantics를 직접 예측하지 않는다.

### 4-5. primitive/normalization 보조 baseline

public auxiliary는 먼저 별도 encoder/normalizer에 쓴다.

v1 목적:

* noisy stroke를 current heuristic feature가 더 안정적으로 읽게 만들기
* raw stroke -> normalized feature extraction을 더 안정적으로 보조하기

권장 형태:

* stroke embedding encoder 또는 autoencoder
* denoising / smoothing model
* sequence representation extractor

하지만 이 층은 v1 runtime 의존으로 넣지 않는다.
먼저 offline feature augmentation까지만 설계한다.

---

## 5. 데이터와 split 설계

### 5-1. 레이어 정의

#### `public_auxiliary`

역할:

* encoder pretrain
* normalization prior
* denoising prior

절대 금지:

* direct family label training
* direct operator label training

#### `synthetic_primary`

역할:

* 도메인 supervision 주력
* hard negative 생성
* base/operator rerank baseline 학습

#### `tutorial_primary`

역할:

* user-specific adaptation
* prototype bank
* personalized calibration

현재 상태:

* 런타임 hook와 store는 있음
* 실제 tutorial vector capture 수집은 다음 단계

### 5-2. split 원칙

v1 baseline은 아래 split으로 고정한다.

* public auxiliary
  * `pretrain`
* synthetic
  * `train`
  * `eval`
  * `hard_negative_eval`
* tutorial
  * 다음 단계부터 `adaptation`
  * `acceptance_eval`

보강 규칙:

* `synthetic_tutorial_like`는 onboarding 분포를 흉내 내는 synthetic preset일 뿐이다.
* 따라서 이름과 무관하게 `synthetic_primary`의 `train` lane에 남고, `adaptation` split을 쓰지 않는다.
* `adaptation` / `acceptance_eval`은 실제 tutorial vector capture export 전용 lane으로 유지한다.

### 5-3. 실제 학습 데이터 구성

#### base family baseline

학습:

* synthetic `bootstrap`
* synthetic `tutorial-like`
* synthetic `hard-negative`

보조:

* Quick Draw / `$-family` / CROHME에서 만든 auxiliary embedding 또는 normalization feature

#### operator baseline

학습:

* synthetic operator `bootstrap`
* synthetic operator `hard-negative`
* placement-shift synthetic

보조:

* Quick Draw `line/circle/triangle/square`
* `$-family` multistroke articulation
* CROHME dense ink normalization

### 5-4. 데이터 reweight

Quick Draw가 수량상 압도하므로 아래를 고정한다.

* public auxiliary는 pretrain 단계에서만 사용
* synthetic supervised 단계에서는 public raw count로 loss 비중을 키우지 않는다.
* pretrain feature를 사용할 경우 dataset-level contribution cap을 둔다.

권장 기본값:

* public auxiliary contribution cap: `0.35`
* synthetic supervised contribution: `0.65`

---

## 6. 유저 개인화 결합 방식

### 6-1. activation 단계

개인화는 sample 수에 따라 3단계로 나눈다.

* `0 samples`
  * global heuristic + global ML baseline만 사용
* `few-shot`
  * heuristic tutorial prototype + weak ML feature injection
* `enough-shot`
  * user-specific calibration / adapter 활성화

기본 threshold:

* base adaptation weak start: `>= 6`
* base stronger calibration: `>= 12`
* operator weak start: `>= 4`
* operator stronger calibration: `>= 8`

### 6-2. personalization feature

#### base family

* tutorial prototype similarity
* confusion pair bias
* average closure drift
* average angle drift
* average branch / parallelism drift
* user sample count

#### overlay operator

* averageAnchorZoneId
* averageScaleRatio
* averageStackIndex
* existingOperatorBiases
* shape similarity to user prototype
* placement deviation from user norm

### 6-3. personalization 방식

v1은 아래 두 가지로 제한한다.

* global model + user feature injection
* global confidence + per-user threshold bias

하지 않는 것:

* per-user end-to-end retraining
* per-user direct classifier
* raster image 기반 few-shot

### 6-4. tutorial capture 이미지 혼합 여부

현재 baseline에서는 **혼합하지 않는다**.

이유:

* 현재 runtime과 dataset contract는 `Stroke[]` 기반이다.
* raster image를 섞으려면 vectorization 또는 별도 image encoder가 필요하다.
* v1 baseline의 목적은 existing recognizer hook 위에 작은 보조층을 얹는 것이다.

따라서 현재 개인화는 모두 **vector stroke tutorial capture**를 기준으로 설계한다.

---

## 7. artifact / runtime 계약

### 7-1. offline artifact

권장 위치:

* `artifacts/ml/base-rerank-v1.json`
* `artifacts/ml/base-confidence-v1.json`
* `artifacts/ml/operator-rerank-v1.json`
* `artifacts/ml/operator-confidence-v1.json`
* `artifacts/ml/feature-spec-v1.json`

artifact 필수 필드:

* `modelType`
* `version`
* `featureOrder`
* `featureNormalization`
* `labelSpace`
* `supportedPairs`
* `trainingManifest`
* `weights` 또는 `treeParams`
* `gatePolicy`

### 7-2. runtime 적용 방식

Node runtime contract는 아래로 고정한다.

* artifact가 없으면 heuristic-only fallback
* artifact가 있어도 rule gate가 먼저 실행
* 초기 배포는 `shadow mode`
  * ML score는 계산하지만 최종 decision에는 반영하지 않음
* shadow log에 아래를 남긴다.
  * heuristic top-k
  * ML delta
  * calibrated confidence
  * final decision difference

gate-open 조건:

* offline eval이 acceptance를 만족
* family flip 증가 0
* dependency violation 0
* off-anchor rescue 0

현재 작업 트리 상태:

* artifact loader와 shadow summary는 이미 구현되어 있다.
* 최종 decision gate-open은 아직 닫혀 있으며, 실제 판정은 계속 규칙/heuristic 경로가 우선한다.

---

## 8. 평가 기준

### 8-1. base family

필수 시나리오:

* `earth vs fire`
* `water vs life`
* `wind incomplete`
* clear canonical template no-flip

### 8-2. operator

필수 시나리오:

* `void_cut vs electric_fork`
* `ice_bar vs partial stroke`
* `steel_brace vs open box-like`
* `martial_axis blockedBy`
* off-anchor / wrong-scale suppression

### 8-3. 핵심 지표

* top-1 accuracy
* ambiguity rate
* false confidence
* family flip increase
* operator false positive
* blockedBy violation count
* off-anchor false recognition
* wrong-scale rescue count

성공 기준:

* ambiguity rate 또는 hard-negative error 감소
* family flip 증가 `0`
* dependency violation `0`
* off-anchor rescue `0`
* false confidence 증가 없음

---

## 9. 단계별 구현 순서

### W01. dataset analysis + split manifest

산출물:

* public/synthetic split manifest
* training/eval dataset summary

### W02. offline feature export

산출물:

* base feature row exporter
* operator feature row exporter
* public auxiliary embedding/normalization feature exporter

### W03. tiny ML baseline training

산출물:

* base rerank GBDT
* base confidence calibrator
* operator pairwise GBDT
* operator confidence calibrator

### W04. artifact export + runtime shadow hook

산출물:

* JSON artifact contract
* Node runtime loader
* shadow log schema

### W05. tutorial-aware personalization adapter

산출물:

* user feature injection
* per-user threshold bias
* activation threshold policy

---

## 10. 현재 기준의 downstream task 연결

이 설계는 아래 후속 task의 기준 문서로 쓴다.

* `T07-08 tiny ML baseline 설계와 offline 실험안`
  * offline baseline, artifact, shadow acceptance 기준
* `T02-08 tutorial vector capture와 ML adaptation contract`
  * vector capture를 `adaptation` / `acceptance_eval` 계약으로 고정하는 bridge
* `T07-09 tutorial-aware personalization adapter 설계`
  * `T07-08` shadow baseline 위에 user-specific adapter를 여는 후속 단계

즉, 이 문서는 현재 구현을 건드리지 않고도
**first ML baseline과 future user personalization을 어떻게 결합할지**를 고정하는 실행 기준서다.

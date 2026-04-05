# tutorial-dataset helper

이 디렉토리는 `docs/20_queue/tutorial-personalization-plan.md`의 W06을 바로 시작할 수 있게 만드는 **작은 offline helper 묶음**이다.

중요 원칙:

* `tutorial` 데이터가 최우선이다. user adaptation, prototype bank, confidence calibration은 공개 데이터보다 tutorial 입력을 먼저 본다.
* `synthetic` 데이터는 bootstrap과 hard negative 보강의 주력이다.
* Quick, Draw / $-family / CROHME 같은 공개 데이터는 직접 family classifier나 operator classifier를 학습시키는 용도가 아니다.
* recognizer core를 수정하지 않고, 실행 가능한 helper와 공통 포맷만 먼저 정리한다.

## 공통 출력 포맷

모든 helper는 NDJSON 한 줄당 1샘플 형식으로 저장한다.

핵심 필드:

* `schemaVersion`: 현재 `tutorial-hybrid-v1`
* `dataset`: `tutorial`, `synthetic`, `quickdraw`, `dollar_family`, `crohme`
* `kind`: `family`, `operator`, `auxiliary`
* `label`: tutorial/synthetic에서는 canonical label, 공개 데이터에서는 `null`
* `split`: `adaptation`, `train`, `eval`, `pretrain` 등
* `priority`: `tutorial_primary`, `synthetic_bootstrap`, `public_auxiliary` 등
* `source`: tutorial source 또는 public source id
* `usage.allowed`: 허용된 downstream 용도
* `usage.forbidden`: 금지된 용도
* `strokes`: 원본에 가까운 stroke 좌표
* `normalizedStrokes`: center-fit 정규화 좌표
* `metadata`: source label, transform, trace count 같은 부가 정보

예시:

```json
{
  "schemaVersion": "tutorial-hybrid-v1",
  "dataset": "tutorial",
  "kind": "family",
  "label": "fire",
  "split": "adaptation",
  "priority": "tutorial_primary",
  "source": "recall",
  "usage": {
    "allowed": ["user_adaptation", "prototype_bank", "rerank_calibration", "acceptance_eval"],
    "forbidden": []
  },
  "strokes": [[{"x":0,"y":-0.7,"t":0},{"x":0.7,"y":0.6,"t":16}]],
  "normalizedStrokes": [[{"x":0,"y":-1,"t":0},{"x":1,"y":0.9,"t":16}]],
  "metadata": {
    "layerRole": "tutorial_primary",
    "captureId": "tutorial-1",
    "timestamp": 1710000000000,
    "tutorialPriorityRank": 0
  }
}
```

## Helper 목록

* `generate-synthetic.mjs`: family/operator canonical template에서 synthetic bootstrap 샘플을 생성한다.
* `convert-tutorial-captures.mjs`: `TutorialCapture[]` 또는 `{ captures: TutorialCapture[] }`를 공통 NDJSON으로 바꾼다.
* `convert-quickdraw.mjs`: Quick, Draw NDJSON을 auxiliary NDJSON으로 바꾼다.
* `convert-dollar-family.mjs`: `$-family` XML gesture log를 auxiliary NDJSON으로 바꾼다.
* `convert-crohme.mjs`: CROHME InkML을 auxiliary NDJSON으로 바꾼다.
* `common.mjs`: 공통 manifest, geometry transform, NDJSON writer.
* `synthetic-presets.mjs`: synthetic preset/priority 계약을 고정한다.
* `ml-contract.mjs`: tiny ML baseline의 split, feature row, gate policy 계약을 고정한다.
* `build-ml-baseline-manifests.mjs`: `artifacts/ml/dataset-split-v1.json`, `artifacts/ml/feature-spec-v1.json`을 생성한다.
* `manifest/public-datasets.json`: 실제 다운로드 대상과 raw cache 위치를 고정한다.
* `download-quickdraw.mjs`: Quick, Draw raw NDJSON을 `external/quickdraw`에 받는다.
* `download-dollar-family.mjs`: `$-family` XML zip을 `external/dollar`에 받는다.
* `download-crohme.mjs`: CROHME dataset page에서 공식 zip 링크를 찾아 `external/crohme`에 받는다.
* `download-all.mjs`: 위 downloader를 순차 실행한다.
* `convert-all-public.mjs`: raw cache를 public auxiliary NDJSON으로 한 번에 변환한다.

## Synthetic 생성 규칙

Synthetic는 canonical template를 직접 늘리는 것이 아니라 아래 변형을 조합해 만든다.

기본 변형:

* scale: isotropic scale과 axis stretch를 함께 건다.
* rotation: family/operator별 허용 범위 안에서 회전한다.
* translation: 중심 이동을 가해 normalization regression에도 쓸 수 있게 한다.
* stroke order 변화: multi-stroke target은 stroke order를 바꿀 수 있다.
* stroke direction 변화: 각 stroke point 순서를 뒤집을 수 있다.
* closure leak: closed target은 끝점을 일부 열어 `water`/`fire` incomplete edge를 만든다.
* jitter: 내부 point에 미세 노이즈를 넣는다.
* overshoot: 시작점/끝점을 조금 연장한다.
* corner rounding: corner를 인접 point 평균으로 당긴다.
* partial stroke: 양 끝 일부 point를 잘라 incomplete edge를 만든다.
* anchor zone shift: operator는 x축 이동을 더 크게 줘 anchor tolerance를 테스트한다.

preset:

* `bootstrap`: 가장 약한 변형. family/operator prototype bootstrap용.
* `tutorial-like`: trace/recall/variation 비율을 섞어 onboarding 분포를 흉내 낸다. 그래도 synthetic lane에 남아 있으며 실제 tutorial `adaptation` split을 대체하지 않는다.
* `hard-negative`: confusion pair 근처까지 밀어 넣는 강한 변형. direct semantics 대체가 아니라 rerank/calibration 평가용이다.
* `placement-shift`: operator anchor/scale robustness를 흔드는 preset. overlay placement hard negative를 만들 때 쓴다.

priority:

* `bootstrap` -> `synthetic_bootstrap`
* `tutorial-like` -> `synthetic_tutorial_like`
* `hard-negative` -> `synthetic_hard_negative`
* `placement-shift` -> `synthetic_placement_shift`

known confusion focus:

* `earth` <-> `fire`
* `water` <-> `life`
* `void_cut` <-> `electric_fork`
* `steel_brace` <-> partial/open box 유사형
* `ice_bar` <-> 짧은 partial stroke

예시:

```bash
node scripts/tutorial-dataset/generate-synthetic.mjs \
  --target family \
  --preset tutorial-like \
  --count 18 \
  --seed 7 \
  --out tmp/tutorial-dataset/family-synthetic.ndjson

node scripts/tutorial-dataset/generate-synthetic.mjs \
  --label void_cut \
  --label electric_fork \
  --preset hard-negative \
  --count 32 \
  --out tmp/tutorial-dataset/operator-hard-negative.ndjson
```

## Tutorial 우선 원칙

offline dataset merge를 할 때는 아래 우선순위를 고정한다.

1. `tutorial_primary`
2. `synthetic_bootstrap` 또는 `synthetic_tutorial_like`
3. `public_auxiliary`

실무 규칙:

* user profile 계산은 tutorial만으로 시작한다.
* synthetic는 tutorial이 적을 때 prototype bank를 초기화하거나 hard negative를 보강하는 용도다.
* 공개 데이터는 stroke encoder, denoising, normalization 같은 보조학습에만 쓴다.
* 공개 데이터의 `metadata.sourceLabel`은 family/operator label로 직접 매핑하지 않는다.

## Tiny ML baseline layer 역할

`docs/20_queue/tiny-ml-baseline-plan.md` 기준으로 layer 역할은 아래로 고정한다.

* `public_auxiliary`: encoder pretrain, normalization prior, denoising prior 전용이다.
* `synthetic_primary`: base family / operator rerank와 confidence calibration의 주 supervision 세트다.
* `tutorial_primary`: user adaptation, prototype bank, personalized calibration, acceptance eval에 쓴다.

split 계약:

* `public_auxiliary` -> `pretrain`
* `synthetic_primary` -> `train`, `eval`, `hard_negative_eval`
* `tutorial_primary` -> `adaptation`, `acceptance_eval`

중요:

* `synthetic_tutorial_like`는 이름과 달리 `synthetic_primary`의 `train` lane에 남는다.
* `adaptation` / `acceptance_eval` split은 실제 tutorial vector capture export에만 예약한다.

중요:

* public data는 `usage.forbidden`으로 direct family/operator supervision이 계속 금지된다.
* synthetic record와 tutorial record는 `metadata.layerRole`에 역할을 남긴다.
* public record도 `metadata.layerRole=public_auxiliary`를 유지한다.

## Tiny ML baseline manifest

manifest 생성:

```bash
node scripts/tutorial-dataset/build-ml-baseline-manifests.mjs
```

생성물:

* `artifacts/ml/dataset-split-v1.json`
* `artifacts/ml/feature-spec-v1.json`

`feature-spec-v1.json`에는 아래 계약이 포함된다.

* `base_candidate_row_v1`: heuristic family candidate 1개당 1 row
* `operator_candidate_row_v1`: heuristic operator candidate 1개당 1 row
* `featureOrder`, `featureNormalization`, `supportedPairs`, `trainingManifest`, `gatePolicy`

base row 핵심 feature:

* heuristic score / template distance / top gap
* `RecognitionFeatures`
* quality 일부: `closure`, `smoothness`, `stability`, `rotationBias`
* candidate family id / confusion pair id

operator row 핵심 feature:

* heuristic score / base score / template distance / shape confidence
* `anchorScore`, `scaleScore`, `gateStrength`
* `angleRadians`, `scaleRatio`, `straightness`, `corners`, `closure`
* anchor zone / placement anchor / existing operator context / `blockedBy`

Quick Draw dominance 제어:

* public auxiliary 전체 기여도 cap: `0.35`
* supervised mainline 최소 비중: `0.65`
* Quick Draw는 public auxiliary 내부에서 최대 `0.6`
* Quick Draw는 pretrain split에서 label당 최대 `6000` 샘플 cap
* public auxiliary 출력은 frozen normalization/embedding feature로만 supervised row에 재주입한다.

## Public 데이터 제한

공개 데이터 helper는 모두 아래 제한을 출력 레코드에 명시한다.

허용:

* `normalization_regression`
* `stroke_encoder_pretrain`
* `denoising_prior`
* `sequence_representation`

금지:

* `direct_family_classifier_training`
* `direct_operator_classifier_training`
* `semantic_label_override`

즉, Quick, Draw의 `triangle`이나 CROHME의 수식 symbol을 곧바로 `fire`나 `void_cut` 정답처럼 쓰지 않는다.

## 실제 raw cache 수집

외부 원본 데이터는 git에 넣지 않고 `external/` 아래 로컬 캐시로만 둔다.

권장 순서:

```bash
node scripts/tutorial-dataset/download-all.mjs
node scripts/tutorial-dataset/convert-all-public.mjs
```

개별 수집:

```bash
node scripts/tutorial-dataset/download-quickdraw.mjs --label circle --label triangle --label line --label square
node scripts/tutorial-dataset/download-dollar-family.mjs
node scripts/tutorial-dataset/download-crohme.mjs
```

저장 위치:

* `external/quickdraw/*`
* `external/dollar/*`
* `external/crohme/*`
* `tmp/tutorial-dataset/*`

## 실행 예시

Tutorial export를 adaptation 세트로 바꾸기:

```bash
node scripts/tutorial-dataset/convert-tutorial-captures.mjs \
  --input exports/tutorial-captures.json \
  --out tmp/tutorial-dataset/tutorial-adaptation.ndjson
```

Quick, Draw circle만 auxiliary set으로 변환:

```bash
node scripts/tutorial-dataset/convert-quickdraw.mjs \
  --input external/quickdraw/circle.ndjson \
  --word circle \
  --limit 2000 \
  --out tmp/tutorial-dataset/quickdraw-circle.ndjson
```

`$-family` gesture log를 표현학습 보조 세트로 변환:

```bash
node scripts/tutorial-dataset/convert-dollar-family.mjs \
  --input external/dollar \
  --out tmp/tutorial-dataset/dollar-pretrain.ndjson
```

CROHME InkML을 정규화/encoder pretrain 세트로 변환:

```bash
node scripts/tutorial-dataset/convert-crohme.mjs \
  --input external/crohme \
  --limit 500 \
  --out tmp/tutorial-dataset/crohme-pretrain.ndjson
```

## 운영 메모

* `common.mjs`의 canonical template manifest는 현재 recognizer template와 같은 이름을 사용한다. recognizer template가 바뀌면 helper manifest도 같이 갱신해야 한다.
* 이 디렉토리는 dataset ingestion helper만 다룬다. 실제 학습 파이프라인, license audit, 대용량 다운로드 자동화는 아직 범위 밖이다.

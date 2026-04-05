# tutorial ML adaptation rollout plan

이 문서는 현재 `shadow baseline + heuristic personalization groundwork` 이후에 남아 있는 장기 작업을,
`T02-08`과 `T07-09` 중심으로 실제 실행 순서까지 분해한 계획서다.

핵심 전제:

* `same shape = same family`
* `same operator shape = same operator meaning`
* `martial_axis requires void_cut`
* tiny ML과 user adaptation은 `top-k rerank`, `confidence calibration`, `threshold bias`까지만 허용한다.
* canonical family/operator 결정권은 계속 규칙계가 가진다.

---

## 1. 왜 아직 blocked인가

현재 작업 트리에는 아래가 이미 있다.

* tutorial store와 operator placement summary
* base/operator shadow artifact loader
* sample 수 기반 personalization activation policy
* public/synthetic/tutorial 역할 분리 문서와 offline baseline artifact

하지만 아래 두 가지는 아직 닫히지 않았다.

1. 실제 tutorial vector capture를 `adaptation` / `acceptance_eval` contract로 export하는 end-to-end 경로
2. 그 export를 읽는 user-specific adapter를 shadow baseline 위에 안전하게 얹는 gate-open 이전 단계

즉, 지금 blocked인 이유는 “개념이 비어 있어서”가 아니라, **capture export와 adapter input 계약이 실제 런타임 연결 기준으로 아직 닫히지 않았기 때문**이다.

---

## 2. 범위 구분

### 2-1. `T02-08`

이 task는 tutorial capture를 tiny ML personalization 쪽으로 넘기는 **데이터 계약 bridge**다.

닫아야 하는 것:

* family/operator capture를 어떤 split으로 내보낼지
* `trace / recall / variation` source를 그대로 유지할지
* base/operator feature export shape를 무엇으로 고정할지
* per-user / per-session / acceptance cohort 분리를 어떻게 할지

아직 열지 않는 것:

* per-user direct retraining
* raster image 혼합
* gate-open rollout

### 2-2. `T07-09`

이 task는 `T02-08`에서 고정한 export contract를 받아 **global shadow baseline 위에 user adapter를 얹는 방식**을 닫는 task다.

닫아야 하는 것:

* weak / strong adaptation에서 어떤 feature를 주입하는지
* per-user threshold bias를 어디까지 허용하는지
* acceptance_eval에서 어떤 stop condition을 볼지
* shadow adapter에서 gate-open candidate로 언제 승격할지

아직 열지 않는 것:

* online retraining
* per-user artifact cache 최적화
* direct classifier 전환

---

## 3. 단계별 실행 순서

### Phase A. tutorial export contract 닫기

목표:

* `T02-08`을 blocked에서 ready로 옮길 수 있게 export contract를 확정한다.

구체 작업:

* tutorial capture export schema를 `tutorial-hybrid-v1` 위에서 `adaptation` / `acceptance_eval`로 고정
* family/operator 공통 필드와 전용 필드를 분리
* operator export에는 이미 있는 `baseSnapshot` / `operatorContext`를 그대로 포함
* per-user partition key, session key, capture ordering key를 명시
* acceptance_eval holdout rule을 고정

완료 기준:

* tutorial export가 synthetic/public과 혼동되지 않는다.
* `adaptation`과 `acceptance_eval`이 실제 tutorial lane 전용임이 문서와 helper에서 일치한다.
* `T07-09`가 더 이상 “입력 계약 미정” 때문에 blocked가 아니게 된다.

### Phase B. tutorial export helper 구현

목표:

* 앱/스토어에 쌓인 tutorial capture를 offline adapter input으로 내보낼 수 있게 한다.

구체 작업:

* `convert-tutorial-captures.mjs`를 capture export contract에 맞춰 보강
* user/session 기준 split helper 추가
* acceptance_eval holdout builder 추가
* export manifest에 provenance와 sample count summary 추가

완료 기준:

* 실제 tutorial vector capture JSON에서 `adaptation` / `acceptance_eval` NDJSON을 재현 가능하게 만든다.
* tutorial export 산출물이 `T07-08` artifact training manifest에 optional role로 꽂힐 수 있다.

### Phase C. personalization adapter shadow 연결

목표:

* `T07-09` 범위의 핵심인 user adapter를 **shadow-only**로 먼저 붙인다.

구체 작업:

* global model feature row에 user-specific injection 필드를 연결
* few-shot / enough-shot 단계별 mix와 threshold bias를 분리
* base/operator 각각에 대해 adapter delta를 로그와 metadata에 노출
* actual decision은 계속 heuristic/runtime actual path가 사용하고, adapter는 shadow summary로만 기록

완료 기준:

* tutorial이 없는 경우와 있는 경우의 shadow delta가 비교 가능하다.
* family flip 증가 `0`, dependency violation `0`, off-anchor rescue `0`을 유지한다.

### Phase D. gate-open readiness 검증

목표:

* adapter를 실제 decision path에 열 수 있는지 판단할 근거를 만든다.

구체 작업:

* acceptance_eval 기준 hard-negative 개선 여부 확인
* shadow -> actual 괴리 로그 집계
* per-user cohort별 false confidence 점검
* raw public/synthetic/tutorial provenance 재생성 검증

gate-open 후보 조건:

* ambiguity 또는 hard-negative error 개선
* family flip 증가 `0`
* dependency violation `0`
* off-anchor rescue `0`
* clear template no-flip regression `0`

이 조건을 만족하지 못하면 gate-open은 열지 않는다.

---

## 4. 데이터/모델 적용 지점

### 4-1. base family

튜토리얼 export가 들어오면 아래를 user adapter 입력으로 사용한다.

* prototype similarity
* confusion pair bias
* closure / angle / branch / parallelism drift
* user sample count

주의:

* clear canonical template는 user adapter로 뒤집지 않는다.
* near-tie / ambiguity 영역에서만 delta가 강해진다.

### 4-2. operator

튜토리얼 export가 들어오면 아래를 user adapter 입력으로 사용한다.

* averageAnchorZoneId
* averageScaleRatio
* averageStackIndex
* existingOperatorBiases
* shape similarity to user prototype
* placement deviation from user norm

주의:

* `blockedBy`, `anchorScore`, `scaleScore`는 계속 상위 rule gate다.
* `martial_axis requires void_cut`는 adapter 이후에도 변하지 않는다.

---

## 5. provenance와 재현성

이번 wave 이후에는 단순 artifact 존재 확인으로 충분하지 않다.

필수 후속 검증:

* raw public cache -> public auxiliary NDJSON 재생성
* synthetic preset -> split manifest 재생성
* tutorial export -> adaptation/acceptance_eval 재생성
* 위 입력으로부터 shadow artifact를 다시 만들었을 때 manifest summary가 일치하는지 확인

즉, 다음 단계의 검수는 “파일이 있다”가 아니라 **입력부터 artifact까지 다시 재생성 가능한가**를 봐야 한다.

---

## 6. 실제 분할 기준

### 단기

이미 이번 턴에서 끝낼 수 있는 작업:

* synthetic/tutorial lane 의미 충돌 제거
* shadow metadata가 actual decision과 분리된다는 회귀 추가

### 장기

별도 wave로 빼야 하는 작업:

* tutorial export contract 닫기
* tutorial acceptance holdout 규칙 구현
* user adapter shadow artifact/feature row 연결
* provenance 재생성 검증
* gate-open readiness 평가

---

## 7. 문서 읽기 순서

이 branch를 이어서 작업할 때는 아래 순서를 권장한다.

1. [`tiny-ml-baseline-plan.md`](tiny-ml-baseline-plan.md)
2. [`tutorial-personalization-plan.md`](tutorial-personalization-plan.md)
3. 이 문서
4. [T02-08 tutorial vector capture와 ML adaptation contract](../30_tasks/epic-02-symbols-and-input/task-08-tutorial-vector-capture-and-ml-adaptation-contract.md)
5. [T07-09 tutorial-aware personalization adapter 설계](../30_tasks/epic-07-future-expansion-backlog/task-09-tutorial-aware-personalization-adapter.md)

---

## 8. 최종 판단

현재 상태에서 `T02-08`과 `T07-09`를 억지로 닫는 것은 타당하지 않다.

이유:

* tutorial vector capture export가 아직 실제 acceptance lane으로 나오지 않는다.
* gate-open 판단 근거가 되는 provenance 재생성 검증도 아직 없다.

따라서 다음 wave는 **contract/export -> adapter shadow -> provenance -> gate-open 판단** 순으로 가는 것이 맞다.

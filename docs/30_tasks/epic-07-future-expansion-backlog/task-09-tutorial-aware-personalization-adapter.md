# T07-09 tutorial-aware personalization adapter 설계

- id: T07-09
- parent: E07
- priority: P2
- status: blocked
- depends_on: T02-08, T07-08
- blocks: -
- source_chat: request-answer03, request-answer11, request-answer18
- phase: Phase 6

## 요약

tutorial vector capture가 들어왔을 때 global tiny baseline 위에 user-specific adaptation을 어떻게 얹을지 정리한다.

## 현재 작업 트리 반영 상태

현재 작업 트리 기준으로 아래 groundwork는 이미 있다.

* base/operator personalization runtime이 tutorial sample count에 따라 weak/strong threshold bias를 계산한다.
* tutorial profile store와 operator placement summary가 이미 존재한다.
* `T07-08` shadow baseline artifact와 runtime hook가 먼저 연결돼 있다.

하지만 이 task는 아직 blocked다.

* `T02-08`의 vector capture export contract가 닫히지 않았다.
* 따라서 user-specific adapter는 실제 capture 입력 기준으로 gate-open 하지 않는다.

## 아이디어 원본

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

대상은 아래 정도로 제한한다.

* global model + user feature injection
* per-user threshold bias
* few-shot personalization activation policy

반대로 아래는 제외한다.

* per-user end-to-end retraining
* tutorial image 기반 adaptation
* dependency rule을 모델에게 위임

## 구현 의도

이 task의 의도는 tutorial이 들어왔을 때 유저별 입력 습관을 의미 있는 수준으로 반영하되, 규칙계의 의미론 권한을 그대로 유지하게 만드는 것이다.

## 관련 문서

* [`../../../20_queue/tiny-ml-baseline-plan.md`](../../../20_queue/tiny-ml-baseline-plan.md)
* [`../../../20_queue/tutorial-personalization-plan.md`](../../../20_queue/tutorial-personalization-plan.md)
* [`../../../20_queue/tutorial-ml-adaptation-rollout-plan.md`](../../../20_queue/tutorial-ml-adaptation-rollout-plan.md)
* [T02-08 tutorial vector capture와 ML adaptation contract](../epic-02-symbols-and-input/task-08-tutorial-vector-capture-and-ml-adaptation-contract.md)
* [T07-08 tiny ML baseline 설계와 offline 실험안](task-08-tiny-ml-baseline-and-offline-eval.md)

## 선행 task

* [T02-08 tutorial vector capture와 ML adaptation contract](../epic-02-symbols-and-input/task-08-tutorial-vector-capture-and-ml-adaptation-contract.md)
* [T07-08 tiny ML baseline 설계와 offline 실험안](task-08-tiny-ml-baseline-and-offline-eval.md)

## 후속 task

없음

## 완료 기준

* tutorial sample 수 기준 weak/strong adaptation 단계가 정리된다.
* base/operator별 personalization feature가 분리돼 있다.
* `T02-08` export contract를 읽어 `adaptation` / `acceptance_eval`에 그대로 연결할 수 있다.
* `ambiguity` 또는 `hard-negative error` 개선이 있어도 `family flip` 증가 `0`, `dependency violation` `0`을 유지하는 stop condition이 명시된다.
* shadow-only 상태와 gate-open 이후 상태가 구분돼 있다.

## 지금은 보류하지만 자리 남길 요소

* per-user artifact cache
* online recalibration 빈도

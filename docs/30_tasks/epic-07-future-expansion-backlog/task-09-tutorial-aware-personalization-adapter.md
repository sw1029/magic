# T07-09 tutorial-aware personalization adapter 설계

- id: T07-09
- parent: E07
- priority: P2
- status: backlog
- depends_on: T02-08, T07-08
- blocks: -
- source_chat: request-answer03, request-answer11, request-answer18
- phase: Phase 6

## 요약

tutorial vector capture가 들어왔을 때 global tiny baseline 위에 user-specific adaptation을 어떻게 얹을지 정리한다.

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

## 선행 task

* [T02-08 tutorial vector capture와 ML adaptation contract](../epic-02-symbols-and-input/task-08-tutorial-vector-capture-and-ml-adaptation-contract.md)
* [T07-08 tiny ML baseline 설계와 offline 실험안](task-08-tiny-ml-baseline-and-offline-eval.md)

## 후속 task

없음

## 완료 기준

* tutorial sample 수 기준 weak/strong adaptation 단계가 정리된다.
* base/operator별 personalization feature가 분리돼 있다.
* family flip 증가 0, dependency violation 0을 유지하는 stop condition이 명시된다.

## 지금은 보류하지만 자리 남길 요소

* per-user artifact cache
* online recalibration 빈도

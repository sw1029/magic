# T02-08 tutorial vector capture와 ML adaptation contract

- id: T02-08
- parent: E02
- priority: P1
- status: blocked
- depends_on: T02-04, T02-07, T06-01
- blocks: T07-09
- source_chat: request-answer03, request-answer11, request-answer18
- phase: Phase 5

## 요약

tutorial vector capture가 tiny ML baseline의 user personalization 입력으로 어떻게 들어갈지 계약을 정리한다.

## 현재 작업 트리 반영 상태

현재 작업 트리 기준으로 아래 groundwork는 이미 있다.

* tutorial capture/store와 operator context snapshot path가 존재한다.
* base/operator personalization runtime은 tutorial sample count와 threshold bias를 이미 읽는다.
* `T07-08` 쪽에서는 shadow artifact와 offline acceptance 기준이 먼저 고정돼 있다.

아직 이 task에서 닫히지 않은 범위는 아래다.

* 실제 tutorial vector capture를 `adaptation` / `acceptance_eval` split contract로 export하는 최종 문서화
* `T07-09`가 참조할 base/operator personalization feature schema의 확정

## 아이디어 원본

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

이번 task에서는 아래를 정리한다.

* tutorial capture를 raster image가 아니라 `Stroke[]` 기반 adaptation 입력으로 고정
* base family와 operator 각각에 대해 personalization feature를 어떤 형태로 export할지 정리
* sample 수에 따른 activation threshold와 weak/strong adaptation 단계를 정리
* runtime이 user-specific feature injection과 threshold bias를 어떻게 읽을지 정리

중요한 전제:

* tutorial capture는 canonical semantics를 바꾸지 않는다.
* user adaptation은 top-k rerank와 confidence calibration 보조에만 제한한다.
* dependency rule과 same-shape invariance는 계속 규칙계가 먼저 보장한다.

## 관련 문서

* [`../../../20_queue/tiny-ml-baseline-plan.md`](../../../20_queue/tiny-ml-baseline-plan.md)
* [`../../../20_queue/tutorial-personalization-plan.md`](../../../20_queue/tutorial-personalization-plan.md)
* [`../../../20_queue/tutorial-ml-adaptation-rollout-plan.md`](../../../20_queue/tutorial-ml-adaptation-rollout-plan.md)
* [T07-08 tiny ML baseline 설계와 offline 실험안](../epic-07-future-expansion-backlog/task-08-tiny-ml-baseline-and-offline-eval.md)

## 선행 task

* [T02-04 튜토리얼 입력 수집 기준 정리](task-04-tutorial-capture-guidelines.md)
* [T02-07 operator tutorial context snapshot과 placement-aware personalization](task-07-operator-tutorial-context-and-placement-personalization.md)
* [T06-01 로그 지점과 로그 형식 정리](../epic-06-logging-and-debug-hooks/task-01-log-points-and-schema.md)

## 후속 task

* [T07-09 tutorial-aware personalization adapter 설계](../epic-07-future-expansion-backlog/task-09-tutorial-aware-personalization-adapter.md)

## 완료 기준

* tutorial vector capture가 tiny ML baseline의 user adaptation 입력으로 쓰일 contract가 정리된다.
* sample 수 기준 activation policy가 정리된다.
* `adaptation` / `acceptance_eval` split과 base/operator export shape가 `T07-08` / `T07-09`에서 그대로 참조 가능하게 정리된다.
* raster image 혼합을 현재 baseline에서 제외한다는 결정이 문서상 명확하다.

## 지금은 보류하지만 자리 남길 요소

* raster -> vector 변환
* per-user online retraining

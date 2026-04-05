# T07-07 placement-aware operator tiny model backlog

- id: T07-07
- parent: E07
- priority: P2
- status: backlog
- depends_on: T02-07, T07-05
- blocks: -
- source_chat: request-answer03, request-answer11, request-answer18
- phase: Phase 6

## 요약

heuristic/profile 기반 placement-aware personalization 이후에도 남는 overlay hard-negative를 줄이기 위한 tiny model 실험 범위를 backlog 수준으로 정리한다.

## 아이디어 원본

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

대상은 아래 정도로 제한한다.

* anchor/scale/stack context를 포함한 top-k rerank 보조
* placement-aware confidence calibration
* user-specific few-shot adaptation 보조

반대로 아래는 제외한다.

* 최종 operator semantics 직접 분류
* dependency rule을 모델에게 위임
* heavy model 전제 구조

## 구현 의도

이 task의 의도는 placement-aware personalization을 더 정밀하게 만들 수 있는 tiny model 후보를 backlog로 남기되, 현재 규칙 중심 구조를 깨지 않는 실험 범위만 허용하는 것이다.

## 관련 chat 문서

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T02-07 operator tutorial context snapshot과 placement-aware personalization](../epic-02-symbols-and-input/task-07-operator-tutorial-context-and-placement-personalization.md)
* [T07-05 hybrid tutorial/public/synthetic 데이터 전략 backlog](task-05-hybrid-data-strategy-backlog.md)

## 후속 task

없음

## 완료 기준

* placement-aware tiny model 후보군과 사용 위치가 backlog 문서로 정리된다.
* operator semantics와 dependency rule을 규칙계에 남기는 실험 범위가 명시된다.

## 지금은 보류하지만 자리 남길 요소

* 실제 모델 선택
* 학습/서빙 비용 산정

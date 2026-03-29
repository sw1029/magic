# T04-03 3D 확장 계약 정의

- id: T04-03
- parent: E04
- priority: P2
- status: blocked
- depends_on: T03-01, T04-01
- blocks: T07-01
- source_chat: request-answer07, request-answer09, request-answer18
- phase: Phase 6

## 요약

지금은 2D 우선이지만, 나중에 3D를 붙일 수 있도록 어떤 정보와 구조를 남겨야 하는지 정리한다.

## 아이디어 원본

* [request-answer07](../../../chat/request-answer07.md)
* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

지금은 3D를 만들지 않는다.
대신 아래를 미리 정리한다.

* 어떤 데이터가 3D 확장을 위해 남아 있어야 하는가
* 2D 구조를 나중에 어떻게 확장할 수 있는가
* 현재 결과 생성 구조가 3D 후속 구현을 막지 않으려면 무엇을 지켜야 하는가

## 구현 의도

이 task의 의도는 2D 우선 개발을 하면서도 나중에 3D 때문에 처음 구조를 다시 만들지 않게 하는 것이다.

## 관련 chat 문서

* [request-answer07](../../../chat/request-answer07.md)
* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T03-01 단일 마법진 구조 정리](../epic-03-spell-structure-and-stacking/task-01-single-circle-structure.md)
* [T04-01 마법 종류 고정과 결과 변화 규칙 정리](task-01-spell-fixity-and-result-variation.md)

## 후속 task

* [T07-01 3D 후속 구현 backlog](../epic-07-future-expansion-backlog/task-01-3d-follow-up-backlog.md)

## 완료 기준

* 3D 후속 구현을 위한 확장 계약이 문서상 정리된다.
* 현재 2D 설계가 무엇을 반드시 남겨야 하는지 명시된다.

## 지금은 보류하지만 자리 남길 요소

* 실제 3D 전투 UX
* 3D 입력 방식 세부 설계

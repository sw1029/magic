# T01-01 원본 논의 타임라인 동결

- id: T01-01
- parent: E01
- priority: P0
- status: done
- depends_on: -
- blocks: T01-02, T01-03, T03-01
- source_chat: request-answer01, request-answer02, request-answer03, request-answer04, request-answer09, request-answer18
- phase: Phase 1

## 요약

`chat/`에 있는 논의 순서를 구현 관점에서 다시 읽을 수 있도록 정리하고, 각 문서가 어떤 task로 이어지는지 source map에 고정한다.

## 아이디어 원본

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer02](../../../chat/request-answer02.md)
* [request-answer03](../../../chat/request-answer03.md)
* [request-answer04](../../../chat/request-answer04.md)
* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

원본 문서를 직접 수정하지 않고, `docs/00_source_map/`에서

* 논의 순서
* 당시 아이디어
* 변경 지점
* 최종 연결 task

를 추적 가능한 형태로 묶는다.

## 구현 의도

이 task의 의도는 이후 task 문서가 “왜 이 일이 생겼는가”를 바로 거슬러 올라갈 수 있게 만드는 것이다.

## 관련 chat 문서

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer02](../../../chat/request-answer02.md)
* [request-answer03](../../../chat/request-answer03.md)
* [request-answer04](../../../chat/request-answer04.md)
* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

없음

## 후속 task

* [T01-02 클라이언트 결정 동결](task-02-client-decision-freeze.md)
* [T01-03 프로토타입 목표 동결](task-03-prototype-target-freeze.md)
* [T03-01 단일 마법진 구조 정리](../epic-03-spell-structure-and-stacking/task-01-single-circle-structure.md)

## 완료 기준

* `chat/request-answer01.md`부터 `18.md`까지 빠짐없이 타임라인 문서에 반영된다.
* 각 항목에 “당시 아이디어 / 이후 변경 / 최종 연결”이 포함된다.
* 타임라인 문서에서 관련 task 문서로 바로 이동할 수 있다.

## 지금은 보류하지만 자리 남길 요소

* 세부 문장 단위 비교표
* 차후 추가 논의 문서에 대한 확장 슬롯

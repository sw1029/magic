# T03-01 단일 마법진 구조 정리

- id: T03-01
- parent: E03
- priority: P0
- status: ready
- depends_on: T01-01, T01-02, T02-01
- blocks: T03-02, T03-03, T04-01
- source_chat: request-answer03, request-answer04, request-answer15
- phase: Phase 3

## 요약

한 마법진 안에서 무엇이 들어가고 어떤 순서로 읽히는지 단일 구조 기준을 정리한다.

## 아이디어 원본

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer04](../../../chat/request-answer04.md)
* [request-answer15](../../../chat/request-answer15.md)

## 구현하고자 하는 방향

단일 마법진 안에서는 아래 층을 기본으로 본다.

* 기본 심볼
* 성질 변형
* 작동 요소
* 방향/포트
* 바깥 검사/봉인 층

다만 현재는 단일 성질 변형 제한이 아니라, 이후 task에서 다중 변형 중첩 가능성을 열어 둔 구조로 정리한다.

## 구현 의도

이 task의 의도는 나중에 다중 중첩과 여러 마법진 연결 규칙을 얹어도 흔들리지 않는 기본 골격을 확보하는 것이다.

## 관련 chat 문서

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer04](../../../chat/request-answer04.md)
* [request-answer15](../../../chat/request-answer15.md)

## 선행 task

* [T01-01 원본 논의 타임라인 동결](../epic-01-source-and-freeze/task-01-timeline-and-source-map-freeze.md)
* [T01-02 클라이언트 결정 동결](../epic-01-source-and-freeze/task-02-client-decision-freeze.md)
* [T02-01 기본 심볼 시안과 판독 기준 정리](../epic-02-symbols-and-input/task-01-base-symbol-prototypes.md)

## 후속 task

* [T03-02 다중 성질 변형 중첩 규칙 정리](task-02-multi-attribute-stacking.md)
* [T03-03 여러 마법진 연결과 無/武 정리](task-03-multi-circle-composition-and-null-mu.md)
* [T04-01 마법 종류 고정과 결과 변화 규칙 정리](../epic-04-result-resolution-and-runtime/task-01-spell-fixity-and-result-variation.md)

## 완료 기준

* 단일 마법진의 층과 읽기 순서가 문서상 정리된다.
* 이후 중첩과 연결 task가 참고할 기준 골격이 고정된다.

## 지금은 보류하지만 자리 남길 요소

* 후속 3D 구조 확장 슬롯
* 고급 작동 요소 추가

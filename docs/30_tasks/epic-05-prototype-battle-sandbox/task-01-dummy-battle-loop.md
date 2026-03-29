# T05-01 허수아비 전투 시전 루프

- id: T05-01
- parent: E05
- priority: P0
- status: todo
- depends_on: T01-03, T02-02, T03-03, T04-01, T04-02
- blocks: T05-02, T05-03
- source_chat: request-answer01, request-answer15, request-answer18
- phase: Phase 4

## 요약

마법진 입력에서 결과 생성까지의 흐름을 허수아비 대상 최소 전투 검증 루프로 정리한다.

## 아이디어 원본

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer15](../../../chat/request-answer15.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

첫 프로토타입에서는 아래 흐름만 성립하면 된다.

* 유저가 마법진을 그린다.
* 시스템이 종류를 고정해 읽는다.
* 속도와 각도에 따른 결과 차이를 반영한다.
* 허수아비가 결과를 받는다.
* 로그가 남는다.

즉, 작은 전투 장면 위에서 마법진 시스템을 검증하는 루프를 만든다.

## 구현 의도

이 task의 의도는 마법진 시스템이 문서 위 개념이 아니라 실제 게임 루프로 작동하는지 빠르게 증명하는 것이다.

## 관련 chat 문서

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer15](../../../chat/request-answer15.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T01-03 프로토타입 목표 동결](../epic-01-source-and-freeze/task-03-prototype-target-freeze.md)
* [T02-02 입력 해석 규칙 정리](../epic-02-symbols-and-input/task-02-input-interpretation-rules.md)
* [T03-03 여러 마법진 연결과 無/武 정리](../epic-03-spell-structure-and-stacking/task-03-multi-circle-composition-and-null-mu.md)
* [T04-01 마법 종류 고정과 결과 변화 규칙 정리](../epic-04-result-resolution-and-runtime/task-01-spell-fixity-and-result-variation.md)
* [T04-02 결과 생성 뼈대 정리](../epic-04-result-resolution-and-runtime/task-02-runtime-effect-skeleton.md)

## 후속 task

* [T05-02 공방 토글 규칙 정리](task-02-workshop-toggle-rules.md)
* [T05-03 허수아비 검증 시나리오 정리](task-03-sandbox-validation-scenarios.md)

## 완료 기준

* 허수아비를 대상으로 한 최소 시전 루프가 문서상 정리된다.
* 입력에서 결과까지의 단계가 일관된 흐름으로 연결된다.

## 지금은 보류하지만 자리 남길 요소

* 실제 적 AI
* 온라인 전투

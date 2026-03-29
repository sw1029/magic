# T03-03 여러 마법진 연결과 無/武 정리

- id: T03-03
- parent: E03
- priority: P0
- status: todo
- depends_on: T01-02, T03-01, T03-02
- blocks: T04-02, T05-01
- source_chat: request-answer03, request-answer15, request-answer18
- phase: Phase 3

## 요약

여러 마법진을 연결하는 규칙과, 無/武가 단일 구조와 복합 구조에서 어떻게 다르게 기능하는지를 정리한다.

## 아이디어 원본

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer15](../../../chat/request-answer15.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

이 task는 아래를 정리한다.

* 여러 마법진 연결 방식
* 연결 순서와 역할 분담
* 無의 단독 기능
* 無가 여러 마법진 설계에서 추가 기능을 내는 방식
* 武가 신체 강화나 체현 인상으로 보이는 방식

즉, 현재 클라이언트 방향에 맞춰 無/武를 “전투 방식”보다 “구조와 의미 차이”로 정리한다.

## 구현 의도

이 task의 의도는 복합 마법진 설계에서 현재 세계관 방향과 UX 방향이 동시에 유지되게 만드는 것이다.

## 관련 chat 문서

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer15](../../../chat/request-answer15.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T01-02 클라이언트 결정 동결](../epic-01-source-and-freeze/task-02-client-decision-freeze.md)
* [T03-01 단일 마법진 구조 정리](task-01-single-circle-structure.md)
* [T03-02 다중 성질 변형 중첩 규칙 정리](task-02-multi-attribute-stacking.md)

## 후속 task

* [T04-02 결과 생성 뼈대 정리](../epic-04-result-resolution-and-runtime/task-02-runtime-effect-skeleton.md)
* [T05-01 허수아비 전투 시전 루프](../epic-05-prototype-battle-sandbox/task-01-dummy-battle-loop.md)

## 완료 기준

* 여러 마법진 연결 규칙이 문서상 정리된다.
* 無와 武의 차이가 단일 구조와 복합 구조 기준으로 정리된다.
* 이후 허수아비 전투 task가 이를 참조할 수 있다.

## 지금은 보류하지만 자리 남길 요소

* 후속 고급 연결 문법
* 세계관 연출 문장 보강

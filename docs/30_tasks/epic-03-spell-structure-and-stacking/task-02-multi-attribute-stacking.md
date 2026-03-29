# T03-02 다중 성질 변형 중첩 규칙 정리

- id: T03-02
- parent: E03
- priority: P0
- status: blocked
- depends_on: T01-02, T03-01
- blocks: T03-03, T04-01, T04-02
- source_chat: request-answer12, request-answer13, request-answer18
- phase: Phase 3

## 요약

한 마법진 안에서 성질 변형이 여러 번 겹칠 수 있다는 현재 방향을 실제 규칙 수준으로 정리한다.

## 아이디어 원본

* [request-answer12](../../../chat/request-answer12.md)
* [request-answer13](../../../chat/request-answer13.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

이 task는 아래를 정리한다.

* 성질 변형이 여러 번 겹칠 수 있는가
* 겹치는 순서를 어떻게 읽는가
* 표시 순서와 해석 순서를 어떻게 맞출 것인가
* 너무 복잡한 상태를 어디서 제한할 것인가

즉, 자유도를 키우되 직관성을 잃지 않는 중첩 규칙을 만드는 것이 목표다.

## 구현 의도

이 task의 의도는 클라이언트가 요구한 창발성 확대를 실제 구현 가능한 규칙으로 바꾸는 것이다.

## 관련 chat 문서

* [request-answer12](../../../chat/request-answer12.md)
* [request-answer13](../../../chat/request-answer13.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T01-02 클라이언트 결정 동결](../epic-01-source-and-freeze/task-02-client-decision-freeze.md)
* [T03-01 단일 마법진 구조 정리](task-01-single-circle-structure.md)

## 후속 task

* [T03-03 여러 마법진 연결과 無/武 정리](task-03-multi-circle-composition-and-null-mu.md)
* [T04-01 마법 종류 고정과 결과 변화 규칙 정리](../epic-04-result-resolution-and-runtime/task-01-spell-fixity-and-result-variation.md)
* [T04-02 결과 생성 뼈대 정리](../epic-04-result-resolution-and-runtime/task-02-runtime-effect-skeleton.md)

## 완료 기준

* 여러 겹 성질 변형 허용 규칙이 문서상 정리된다.
* 해석 순서와 표시 순서의 기준이 정리된다.
* 이후 결과 생성 task가 중첩 구조를 참조할 수 있다.

## 지금은 보류하지만 자리 남길 요소

* 심볼 시각 연출의 세밀한 정렬 규칙
* 고급 중첩 제한 알고리즘

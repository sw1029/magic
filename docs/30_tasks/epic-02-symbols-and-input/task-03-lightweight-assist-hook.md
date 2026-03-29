# T02-03 가벼운 보조 도구 연결 자리 정리

- id: T02-03
- parent: E02
- priority: P1
- status: later
- depends_on: T02-02, T06-01
- blocks: T07-02
- source_chat: request-answer11, request-answer18
- phase: Phase 5

## 요약

무거운 AI를 전제로 두지 않고, 나중에 가벼운 보조 도구를 붙일 수 있는 입력 단계 hook만 정리한다.

## 아이디어 원본

* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

보조 도구는 아래 수준까지만 고려한다.

* 그림 읽기 보조
* 후보 좁히기
* 가벼운 입력 보정

반대로 종류 판정의 핵심 권한은 여전히 고정 규칙 쪽에 둔다.

## 구현 의도

이 task의 의도는 나중에 보조 도구를 붙일 수 있게 하되, 현재 구조가 그것에 종속되지 않게 만드는 것이다.

## 관련 chat 문서

* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T02-02 입력 해석 규칙 정리](task-02-input-interpretation-rules.md)
* [T06-01 로그 지점과 로그 형식 정리](../epic-06-logging-and-debug-hooks/task-01-log-points-and-schema.md)

## 후속 task

* [T07-02 가벼운 AI backlog](../epic-07-future-expansion-backlog/task-02-lightweight-ai-backlog.md)

## 완료 기준

* 보조 도구가 개입할 수 있는 지점이 문서상 정리된다.
* 보조 도구가 개입하면 안 되는 지점도 함께 정리된다.

## 지금은 보류하지만 자리 남길 요소

* 학습 데이터 준비 방식
* 디바이스별 성능 기준

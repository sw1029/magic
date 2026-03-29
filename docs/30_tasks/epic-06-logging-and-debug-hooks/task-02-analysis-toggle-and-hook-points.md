# T06-02 분석 토글과 후속 hook 정리

- id: T06-02
- parent: E06
- priority: P1
- status: blocked
- depends_on: T05-02, T06-01
- blocks: T07-04
- source_chat: request-answer05, request-answer07, request-answer18
- phase: Phase 5

## 요약

공방 분석 토글과 나중에 붙일 실패 흔적·추가 반응층·실험 도구를 위한 hook 지점을 정리한다.

## 아이디어 원본

* [request-answer05](../../../chat/request-answer05.md)
* [request-answer07](../../../chat/request-answer07.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

현재는 아래를 우선 정리한다.

* 공방에서 어떤 분석 표시를 켜고 끌 수 있는가
* 나중에 실패 흔적을 붙일 지점은 어디인가
* 나중에 추가 반응층을 붙일 지점은 어디인가
* 실험 스테이지가 붙을 때 재사용할 정보는 무엇인가

## 구현 의도

이 task의 의도는 지금은 적게 만들더라도, 나중에 붙일 기능이 현재 구조를 갈아엎지 않도록 하는 것이다.

## 관련 chat 문서

* [request-answer05](../../../chat/request-answer05.md)
* [request-answer07](../../../chat/request-answer07.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T05-02 공방 토글 규칙 정리](../epic-05-prototype-battle-sandbox/task-02-workshop-toggle-rules.md)
* [T06-01 로그 지점과 로그 형식 정리](task-01-log-points-and-schema.md)

## 후속 task

* [T07-04 실패 흔적과 추가 반응층 backlog](../epic-07-future-expansion-backlog/task-04-failure-trace-and-extra-layer-backlog.md)

## 완료 기준

* 공방 분석 토글과 후속 확장 hook가 정리된다.
* 지금 보류한 층을 나중에 어디에 붙일지 문서상 분명해진다.

## 지금은 보류하지만 자리 남길 요소

* 자동 비교 UI
* 풍부한 실패 연출 패턴

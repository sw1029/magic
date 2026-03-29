# T07-02 가벼운 AI backlog

- id: T07-02
- parent: E07
- priority: P2
- status: backlog
- depends_on: T02-03
- blocks: -
- source_chat: request-answer11, request-answer18
- phase: Phase 6

## 요약

무거운 AI 대신, 학습 데이터를 마련 가능한 범위에서 붙일 수 있는 가벼운 보조 도구 확장안을 backlog로 정리한다.

## 아이디어 원본

* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

대상은 아래 정도로 제한한다.

* 입력 보정
* 후보 좁히기
* 가벼운 판독 보조

종류 판정의 핵심을 AI에게 넘기는 방향은 제외한다.

## 구현 의도

이 task의 의도는 나중에 보조 도구를 붙이더라도, 현재의 “고정 규칙 중심” 방향을 지키게 만드는 것이다.

## 관련 chat 문서

* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T02-03 가벼운 보조 도구 연결 자리 정리](../epic-02-symbols-and-input/task-03-lightweight-assist-hook.md)

## 후속 task

없음

## 완료 기준

* 가벼운 AI가 개입할 수 있는 확장 영역이 backlog 문서로 정리된다.
* 현재 구조를 흔들지 않는 범위가 적힌다.

## 지금은 보류하지만 자리 남길 요소

* 실제 모델 후보
* 실제 학습 데이터 수집 방식

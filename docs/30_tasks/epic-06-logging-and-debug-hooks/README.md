# Epic 06. logging-and-debug-hooks

## 요약

지금은 로그를 먼저 남기고 후속 층은 보류한다는 원칙에 맞춰, 로그 지점, 로그 형식, 공방 분석 토글, 후속 확장 hook를 정리하는 epic이다.

## 아이디어 원본

창발 UX 논의, 물리계층 초안의 이벤트 추출 구조, 클라이언트의 “로그만 우선” 의견을 함께 반영한다.

## 구현하고자 하는 방향

화면에 복잡한 연출을 먼저 올리기보다, 지금은 무엇이 일어났는지를 기록하고 분석 가능한 구조를 먼저 만든다.

## 구현 의도

이 epic의 목적은 인력 부담을 줄이면서도, 나중에 실패 흔적과 추가 반응층을 붙일 기반을 미리 남기는 것이다.

## 관련 chat 문서

* [request-answer05](../../../chat/request-answer05.md)
* [request-answer07](../../../chat/request-answer07.md)
* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 하위 task

* [T06-01 로그 지점과 로그 형식 정리](task-01-log-points-and-schema.md)
* [T06-02 분석 토글과 후속 hook 정리](task-02-analysis-toggle-and-hook-points.md)

## 선행 task

* [Epic 04 result-resolution-and-runtime](../epic-04-result-resolution-and-runtime/README.md)
* [Epic 05 prototype-battle-sandbox](../epic-05-prototype-battle-sandbox/README.md)

## 후속 task

* [Epic 07 future-expansion-backlog](../epic-07-future-expansion-backlog/README.md)

## 완료 기준

* 로그 지점과 형식이 정리된다.
* 공방 분석 토글과 후속 확장 hook가 정리된다.

## 지금은 보류하지만 자리 남길 요소

* 풍부한 실패 흔적 시각 연출
* 고급 실험 분석 UI

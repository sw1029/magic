# Epic 05. prototype-battle-sandbox

## 요약

허수아비 대상 최소 전투, 시전 루프, 공방 표시 on/off, 프로토타입 검증 시나리오를 정리하는 epic이다.

## 아이디어 원본

초기 게임 아이디어의 실시간 시전, 창발 UX 논의, 통합 작성 양식, 클라이언트의 “허수아비부터 검증” 의견을 함께 반영한다.

## 구현하고자 하는 방향

전투를 먼저 크게 만들지 않고, 마법진 시스템이 실제 결과를 만들어 내는지를 허수아비 대상 최소 전투에서 검증하는 방향으로 간다.

## 구현 의도

이 epic의 목적은 “마법진이 게임 안에서 실제로 살아 움직이는가”를 가장 작은 형태로 빨리 확인하는 것이다.

## 관련 chat 문서

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer05](../../../chat/request-answer05.md)
* [request-answer14](../../../chat/request-answer14.md)
* [request-answer15](../../../chat/request-answer15.md)
* [request-answer18](../../../chat/request-answer18.md)

## 하위 task

* [T05-01 허수아비 전투 시전 루프](task-01-dummy-battle-loop.md)
* [T05-02 공방 토글 규칙 정리](task-02-workshop-toggle-rules.md)
* [T05-03 허수아비 검증 시나리오 정리](task-03-sandbox-validation-scenarios.md)

## 선행 task

* [Epic 01 source-and-freeze](../epic-01-source-and-freeze/README.md)
* [Epic 02 symbols-and-input](../epic-02-symbols-and-input/README.md)
* [Epic 03 spell-structure-and-stacking](../epic-03-spell-structure-and-stacking/README.md)
* [Epic 04 result-resolution-and-runtime](../epic-04-result-resolution-and-runtime/README.md)

## 후속 task

* [Epic 06 logging-and-debug-hooks](../epic-06-logging-and-debug-hooks/README.md)
* [Epic 07 future-expansion-backlog](../epic-07-future-expansion-backlog/README.md)

## 완료 기준

* 허수아비 대상 최소 전투 검증 흐름이 문서상 정리된다.
* 공방의 표시 on/off 규칙이 정리된다.
* 프로토타입 검증 시나리오가 정리된다.

## 지금은 보류하지만 자리 남길 요소

* 완성형 전투 밸런스
* 복잡한 적 패턴

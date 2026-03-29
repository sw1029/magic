# Epic 01. source-and-freeze

## 요약

`chat/`에 쌓인 논의를 구현 기준으로 다시 읽을 수 있게 정리하고, 현재 클라이언트 결정과 첫 프로토타입 목표를 흔들리지 않게 고정하는 epic이다.

## 아이디어 원본

초기 아이디어, 언어 설계 종합본, 구현 전 확정 항목, 승인 문서, 클라이언트 최종 정리 문서에서 출발한다.

## 구현하고자 하는 방향

원본 논의 흐름, 현재 확정값, 프로토타입 범위를 문서상으로 먼저 고정한 뒤 나머지 task가 그 위에서 움직이게 한다.

## 구현 의도

이 epic의 목적은 “이전 문서마다 말이 달랐던 부분”을 정리하고, 이후 task가 서로 다른 기준을 참조하지 않게 만드는 것이다.

## 관련 chat 문서

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer04](../../../chat/request-answer04.md)
* [request-answer09](../../../chat/request-answer09.md)
* [request-answer16](../../../chat/request-answer16.md)
* [request-answer17](../../../chat/request-answer17.md)
* [request-answer18](../../../chat/request-answer18.md)

## 하위 task

* [T01-01 원본 논의 타임라인 동결](task-01-timeline-and-source-map-freeze.md)
* [T01-02 클라이언트 결정 동결](task-02-client-decision-freeze.md)
* [T01-03 프로토타입 목표 동결](task-03-prototype-target-freeze.md)

## 선행 task

없음

## 후속 task

* [Epic 02 symbols-and-input](../epic-02-symbols-and-input/README.md)
* [Epic 03 spell-structure-and-stacking](../epic-03-spell-structure-and-stacking/README.md)
* [Epic 04 result-resolution-and-runtime](../epic-04-result-resolution-and-runtime/README.md)

## 완료 기준

* `chat/request-answer01.md`부터 `18.md`까지의 흐름이 source map에 반영된다.
* 현재 클라이언트 확정값이 한 문서로 정리된다.
* 첫 프로토타입 목표가 결정 완료 상태로 정리된다.

## 지금은 보류하지만 자리 남길 요소

* 향후 추가 승인 문서
* 이후 클라이언트 피드백 반영 이력

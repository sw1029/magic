# T01-02 클라이언트 결정 동결

- id: T01-02
- parent: E01
- priority: P0
- status: done
- depends_on: T01-01
- blocks: T01-03, T02-01, T02-02, T03-01, T03-02, T03-03, T04-01, T05-01
- source_chat: request-answer16, request-answer17, request-answer18
- phase: Phase 1

## 요약

클라이언트가 최종 승인하거나 수정한 내용을 한 문서로 고정하고, 이전 문서의 낡은 가정을 덮어쓴다.

## 아이디어 원본

* [request-answer16](../../../chat/request-answer16.md)
* [request-answer17](../../../chat/request-answer17.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

현재 기준에서는 아래를 고정값으로 사용한다.

* 같은 모양은 같은 종류
* 속도와 각도는 결과 차이
* 성질 변형 여러 겹 허용
* 無와 武는 세계관 차이로 구분
* 마법진 우선 구현
* 2D 우선, 3D 후속
* 로그 우선, 후속 층 보류
* 온라인 완전 배제

## 구현 의도

이 task의 의도는 이후 task가 더 이상 “예전 안”을 기준으로 움직이지 않게 만드는 것이다.

## 관련 chat 문서

* [request-answer16](../../../chat/request-answer16.md)
* [request-answer17](../../../chat/request-answer17.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T01-01 원본 논의 타임라인 동결](task-01-timeline-and-source-map-freeze.md)

## 후속 task

* [T01-03 프로토타입 목표 동결](task-03-prototype-target-freeze.md)
* [T02-01 기본 심볼 시안과 판독 기준 정리](../epic-02-symbols-and-input/task-01-base-symbol-prototypes.md)
* [T03-02 다중 성질 변형 중첩 규칙 정리](../epic-03-spell-structure-and-stacking/task-02-multi-attribute-stacking.md)
* [T03-03 여러 마법진 연결과 無/武 정리](../epic-03-spell-structure-and-stacking/task-03-multi-circle-composition-and-null-mu.md)

## 완료 기준

* `request-answer18` 기준 고정값이 `client-decisions.md`에 반영된다.
* 이전 문서의 뒤집힌 결정이 있으면 현재 기준으로 덮어쓴다.
* 이후 queue와 task 문서가 같은 고정값을 참조한다.

## 지금은 보류하지만 자리 남길 요소

* 차후 클라이언트 추가 의견용 change log

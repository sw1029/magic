# T05-03 허수아비 검증 시나리오 정리

- id: T05-03
- parent: E05
- priority: P1
- status: blocked
- depends_on: T01-03, T05-01
- blocks: -
- source_chat: request-answer05, request-answer14, request-answer18
- phase: Phase 5

## 요약

첫 프로토타입에서 어떤 마법진 조합과 결과 차이를 허수아비 기준으로 확인할지 검증 시나리오를 정리한다.

## 아이디어 원본

* [request-answer05](../../../chat/request-answer05.md)
* [request-answer14](../../../chat/request-answer14.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

아래 유형의 시나리오를 최소 세트로 잡는다.

* 기본 심볼 5종 단일 시전
* 성질 변형 중첩 시전
* 속도 차이 비교
* 각도 차이 비교
* 無/武 관련 구조 비교
* 여러 마법진 연결 시전

## 구현 의도

이 task의 의도는 “무언가 나오긴 한다” 수준이 아니라, 현재 방향이 실제로 구현되었는지 문서 기반으로 검증 가능한 기준을 만드는 것이다.

## 관련 chat 문서

* [request-answer05](../../../chat/request-answer05.md)
* [request-answer14](../../../chat/request-answer14.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T01-03 프로토타입 목표 동결](../epic-01-source-and-freeze/task-03-prototype-target-freeze.md)
* [T05-01 허수아비 전투 시전 루프](task-01-dummy-battle-loop.md)

## 후속 task

* [T07-03 실험 스테이지 분리 backlog](../epic-07-future-expansion-backlog/task-03-experiment-stage-backlog.md)

## 완료 기준

* 허수아비 기준 검증 시나리오 목록이 정리된다.
* 현재 방향의 핵심 규칙이 시나리오로 확인 가능하다.

## 지금은 보류하지만 자리 남길 요소

* 실제 적 패턴 테스트
* 복잡한 환경 변수 테스트

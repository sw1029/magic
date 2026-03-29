# T04-01 마법 종류 고정과 결과 변화 규칙 정리

- id: T04-01
- parent: E04
- priority: P0
- status: todo
- depends_on: T01-02, T02-02, T03-01, T03-02
- blocks: T04-02, T05-01, T06-01
- source_chat: request-answer01, request-answer08, request-answer09, request-answer18
- phase: Phase 4

## 요약

같은 모양은 같은 종류로 고정하되, 속도·각도·안정성·중첩·연결 차이가 결과 변화로 반영되는 규칙을 정리한다.

## 아이디어 원본

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

아래를 명확히 구분한다.

* 종류를 정하는 것
* 결과를 바꾸는 것

현재 기준에서는

* 모양은 종류를 정한다.
* 속도와 각도는 결과 차이에 들어간다.
* 성질 변형 여러 겹과 여러 마법진 연결도 결과 변화 요소에 들어간다.

## 구현 의도

이 task의 의도는 시스템 신뢰성과 창발성을 동시에 살리기 위해 “무엇이 고정이고 무엇이 가변인지”를 분명히 나누는 것이다.

## 관련 chat 문서

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T01-02 클라이언트 결정 동결](../epic-01-source-and-freeze/task-02-client-decision-freeze.md)
* [T02-02 입력 해석 규칙 정리](../epic-02-symbols-and-input/task-02-input-interpretation-rules.md)
* [T03-01 단일 마법진 구조 정리](../epic-03-spell-structure-and-stacking/task-01-single-circle-structure.md)
* [T03-02 다중 성질 변형 중첩 규칙 정리](../epic-03-spell-structure-and-stacking/task-02-multi-attribute-stacking.md)

## 후속 task

* [T04-02 결과 생성 뼈대 정리](task-02-runtime-effect-skeleton.md)
* [T05-01 허수아비 전투 시전 루프](../epic-05-prototype-battle-sandbox/task-01-dummy-battle-loop.md)
* [T06-01 로그 지점과 로그 형식 정리](../epic-06-logging-and-debug-hooks/task-01-log-points-and-schema.md)

## 완료 기준

* 종류 고정 규칙과 결과 변화 규칙이 분리된다.
* 이후 결과 생성 task가 바로 참조할 수 있는 기준이 생긴다.

## 지금은 보류하지만 자리 남길 요소

* 더 깊은 환경 상호작용 수치
* 후속 밸런스 세부 조정

# T01-03 프로토타입 목표 동결

- id: T01-03
- parent: E01
- priority: P0
- status: done
- depends_on: T01-01, T01-02
- blocks: T05-01, T05-03
- source_chat: request-answer09, request-answer14, request-answer18
- phase: Phase 1

## 요약

첫 프로토타입에서 반드시 구현할 것과 제외할 것을 명확히 나눠, 이후 task의 범위를 고정한다.

## 아이디어 원본

* [request-answer09](../../../chat/request-answer09.md)
* [request-answer14](../../../chat/request-answer14.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

첫 프로토타입 목표는 아래 흐름을 성립시키는 것이다.

`마법진 입력 -> 결과 생성 -> 허수아비 검증 -> 로그 저장`

이때 3D, 실험 전용 스테이지, 확장 AI, 풍부한 실패 연출은 후속으로 둔다.

## 구현 의도

이 task의 의도는 “가능하면 많이 넣자”와 “첫 버전 검증”을 동시에 만족할 수 있는 경계를 만드는 것이다.

## 관련 chat 문서

* [request-answer09](../../../chat/request-answer09.md)
* [request-answer14](../../../chat/request-answer14.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T01-01 원본 논의 타임라인 동결](task-01-timeline-and-source-map-freeze.md)
* [T01-02 클라이언트 결정 동결](task-02-client-decision-freeze.md)

## 후속 task

* [T05-01 허수아비 전투 시전 루프](../epic-05-prototype-battle-sandbox/task-01-dummy-battle-loop.md)
* [T05-03 허수아비 검증 시나리오 정리](../epic-05-prototype-battle-sandbox/task-03-sandbox-validation-scenarios.md)

## 완료 기준

* 포함 범위와 제외 범위가 문서상 분리된다.
* 허수아비 대상 최소 검증 루프가 프로토타입 목표로 명시된다.
* 이후 task가 범위를 이유로 흔들리지 않는다.

## 지금은 보류하지만 자리 남길 요소

* 이후 v2 범위 표
* 3D 시범 목표

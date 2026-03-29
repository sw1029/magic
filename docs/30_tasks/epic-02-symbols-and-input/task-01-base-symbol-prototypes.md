# T02-01 기본 심볼 시안과 판독 기준 정리

- id: T02-01
- parent: E02
- priority: P0
- status: done
- depends_on: T01-02
- blocks: T02-02, T03-01
- source_chat: request-answer10, request-answer12, request-answer13, request-answer18
- phase: Phase 2

## 요약

기본 심볼 5종의 프로토타입 시안을 고정하고, 서로 헷갈리지 않게 읽히기 위한 최소 판독 기준을 정리한다.

## 아이디어 원본

* [request-answer10](../../../chat/request-answer10.md)
* [request-answer12](../../../chat/request-answer12.md)
* [request-answer13](../../../chat/request-answer13.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

현재 V1 recognizer는 아래 기준형을 canonical silhouette로 사용한다.

* 바람: 3개의 평행 개방선
* 땅: 하변이 더 긴 폐합 사다리꼴
* 불꽃: 상향 인상의 폐합 삼각형
* 물: 단일 원형 폐합 루프
* 생명: 줄기와 상단 분기가 있는 rooted Y

이 기준형은 최종 아트가 아니라 입력과 판독을 검증하기 위한 canonical 형태로 사용한다.
V1에서는 `물방울`, `동심원`, 복잡한 `새싹` variation을 허용하지 않는다.

## 구현 의도

이 task의 의도는 “먼저 읽히는가”를 확인할 수 있는 최소 형태를 확보하는 것이다.

## 관련 chat 문서

* [request-answer10](../../../chat/request-answer10.md)
* [request-answer12](../../../chat/request-answer12.md)
* [request-answer13](../../../chat/request-answer13.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T01-02 클라이언트 결정 동결](../epic-01-source-and-freeze/task-02-client-decision-freeze.md)

## 후속 task

* [T02-02 입력 해석 규칙 정리](task-02-input-interpretation-rules.md)
* [T03-01 단일 마법진 구조 정리](../epic-03-spell-structure-and-stacking/task-01-single-circle-structure.md)

## 완료 기준

* 기본 심볼 5종 임시 시안이 문서로 고정된다.
* family마다 기준형 1개만 사용한다.
* 각 심볼의 의도와 구분 포인트가 정리된다.
* 이후 입력 규칙과 구조 task가 같은 기준형을 참조한다.

## 지금은 보류하지만 자리 남길 요소

* 최종 아트 방향
* 학파별 시각 연출 차이

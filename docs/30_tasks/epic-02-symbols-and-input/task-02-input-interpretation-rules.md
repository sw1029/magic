# T02-02 입력 해석 규칙 정리

- id: T02-02
- parent: E02
- priority: P0
- status: done
- depends_on: T01-02, T02-01
- blocks: T04-01, T05-01
- source_chat: request-answer01, request-answer08, request-answer18
- phase: Phase 2

## 요약

같은 모양은 같은 종류로 읽히게 하되, 속도·각도·리듬·안정성 차이는 결과 차이로 반영하는 입력 해석 규칙을 정리한다.

## 아이디어 원본

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

아래 기준을 고정한다.

* 종류는 모양으로 정한다.
* 속도와 각도는 종류를 바꾸지 않는다.
* 속도와 각도는 결과 차이에만 들어간다.
* 유저 손버릇은 종류보다 결과 감각에 더 크게 반영한다.
* draw 중에는 low-level feedback만 보여 주고, canonical 확정은 `seal`에서만 한다.

이 task는 특히 “의미 있는 각도”와 “손버릇 각도”를 분리하는 기준을 잡는다.

V1 recognizer 파이프라인은 아래 순서로 고정한다.

1. pointer stroke 수집
2. resample / smooth
3. translation / scale / global rotation normalization
4. feature extraction
5. family scoring
6. live candidate preview
7. `seal`
8. canonical 결과 확정

V1 status 규칙은 아래를 따른다.

* `recognized`: top score `>= 0.70` 이고 2위와 margin `>= 0.15`
* `ambiguous`: top score `>= 0.55` 이고 margin `< 0.15`
* `incomplete`: skeleton은 가깝지만 폐합 또는 구조 조건이 모자람
* `invalid`: 나머지 전부

V1 품질 벡터는 아래 7개로 고정한다.

* `closure`
* `symmetry`
* `smoothness`
* `tempo`
* `overshoot`
* `stability`
* `rotation_bias`

## 구현 의도

이 task의 의도는 유저가 “내가 의도한 것과 다른 종류로 읽혔다”고 느끼는 상황을 줄이면서, 손맛 차이는 살아 있게 만드는 것이다.

## 관련 chat 문서

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T01-02 클라이언트 결정 동결](../epic-01-source-and-freeze/task-02-client-decision-freeze.md)
* [T02-01 기본 심볼 시안과 판독 기준 정리](task-01-base-symbol-prototypes.md)

## 후속 task

* [T04-01 마법 종류 고정과 결과 변화 규칙 정리](../epic-04-result-resolution-and-runtime/task-01-spell-fixity-and-result-variation.md)
* [T05-01 허수아비 전투 시전 루프](../epic-05-prototype-battle-sandbox/task-01-dummy-battle-loop.md)

## 완료 기준

* 종류를 정하는 요소와 결과 차이에만 들어가는 요소가 분리된다.
* 속도/각도/안정성 차이를 기록할 입력 기준이 정리된다.
* `seal` 전 preview와 `seal` 후 canonical 확정 규칙이 정리된다.
* recognized / ambiguous / incomplete / invalid 판정 기준이 정리된다.
* 이후 결과 생성 task가 같은 규칙을 참조한다.

## 지금은 보류하지만 자리 남길 요소

* 입력 장치별 차이 보정
* 고급 개인화 튜닝

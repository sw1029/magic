# T02-05 사용자 shape profile과 prototype bank 설계

- id: T02-05
- parent: E02
- priority: P1
- status: blocked
- depends_on: T02-02, T02-04
- blocks: T02-06, T07-05
- source_chat: request-answer01, request-answer08, request-answer11, request-answer14, request-answer18
- phase: Phase 5

## 요약

튜토리얼에서 얻은 입력을 base family와 overlay operator별 사용자 prototype으로 축적하는 구조를 설계한다.

## 아이디어 원본

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer14](../../../chat/request-answer14.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

이번 task에서는 아래를 정리한다.

* `UserShapeProfile`, `FamilyPrototype`, `OperatorPrototype` 타입
* base 5종과 operator 6종 각각에 대해 저장할 normalized cloud와 average feature
* 기존 `user-profile.ts`의 quality comfort band와 분리된 shape/operator personalization 구조
* local storage 또는 session persistence 키 전략
* later integration을 위한 profile update / read API 초안

구현 기준 문서는 아래를 직접 참조한다.

* [`../../../20_queue/tutorial-personalization-plan.md`](../../../20_queue/tutorial-personalization-plan.md)

## 구현 의도

이 task의 의도는 개인화의 범위를 quality interpretation에서 한 단계 확장하되, family semantics와 operator dependency는 건드리지 않게 만드는 것이다.

## 관련 chat 문서

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer14](../../../chat/request-answer14.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T02-02 입력 해석 규칙 정리](task-02-input-interpretation-rules.md)
* [T02-04 튜토리얼 입력 수집 기준 정리](task-04-tutorial-capture-guidelines.md)

## 후속 task

* [T02-06 개인화 rerank와 confidence calibration 정리](task-06-personalized-rerank-and-confidence-calibration.md)
* [T07-05 hybrid tutorial/public/synthetic 데이터 전략 backlog](../epic-07-future-expansion-backlog/task-05-hybrid-data-strategy-backlog.md)

## 완료 기준

* 사용자별 base/operator prototype 구조가 문서상 결정된다.
* 기존 quality comfort band와 새 shape/operator profile의 경계가 분명해진다.
* 이후 rerank/calibration task가 바로 참조 가능한 profile API 초안이 생긴다.

## 지금은 보류하지만 자리 남길 요소

* profile compression 최적화
* cross-device profile merge

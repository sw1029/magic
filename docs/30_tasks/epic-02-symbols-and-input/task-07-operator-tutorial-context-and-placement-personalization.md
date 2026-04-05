# T02-07 operator tutorial context snapshot과 placement-aware personalization

- id: T02-07
- parent: E02
- priority: P1
- status: done
- depends_on: T02-05, T02-06
- blocks: T07-07
- source_chat: request-answer03, request-answer11, request-answer18
- phase: Phase 5

## 요약

operator tutorial capture에 base reference frame 기준 placement metadata를 저장하고, overlay personalization을 shape-only에서 placement-aware rerank까지 확장한다.

## 아이디어 원본

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

이번 task에서는 아래를 구현 기준으로 정리한다.

* operator tutorial capture에 `baseSnapshot`, `operatorContext` 저장
* `anchorZoneId`, `scaleRatio`, `stackIndex`, `existingOperators` snapshot 유지
* operator prototype에 `averageAnchorZoneId`, `averageScaleRatio`, `averageStackIndex`, `existingOperatorBiases` 추가
* overlay recognizer에서 shape similarity 위에 placement-aware rerank 적용

중요한 전제는 아래다.

* final canonical operator 의미 결정권은 여전히 규칙계가 가진다.
* placement personalization은 top-k 재정렬과 ambiguous handling 보조에만 제한한다.
* `martial_axis requires void_cut`는 모델 밖 규칙으로 유지한다.
* off-anchor / wrong-scale 입력은 personalization이 rescue하면 안 된다.

구현 기준 문서는 아래를 직접 참조한다.

* [`../../../20_queue/tutorial-personalization-plan.md`](../../../20_queue/tutorial-personalization-plan.md)

## 구현 의도

이 task의 의도는 operator personalization을 조금 더 현실적인 배치 습관까지 확장하되, shape semantics나 dependency rule을 건드리지 않게 만드는 것이다.

## 관련 chat 문서

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T02-05 사용자 shape profile과 prototype bank 설계](task-05-user-shape-profile-and-prototype-bank.md)
* [T02-06 개인화 rerank와 confidence calibration 정리](task-06-personalized-rerank-and-confidence-calibration.md)

## 후속 task

* [T07-07 placement-aware operator tiny model backlog](../epic-07-future-expansion-backlog/task-07-placement-aware-operator-tiny-model-backlog.md)

## 완료 기준

* operator tutorial capture가 shape 정보 외에 placement snapshot을 함께 저장한다.
* store rebuild가 placement-aware operator summary를 계산한다.
* overlay rerank가 shape gate를 유지한 채 anchor/scale/stack context를 보조 신호로 쓴다.
* `void_cut` / `electric_fork` hard-negative, off-anchor 금지, dependency 유지 테스트가 존재한다.

## 지금은 보류하지만 자리 남길 요소

* richer seam / sector context
* learned placement embedding

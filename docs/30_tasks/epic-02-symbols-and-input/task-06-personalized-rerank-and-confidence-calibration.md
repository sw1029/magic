# T02-06 개인화 rerank와 confidence calibration 정리

- id: T02-06
- parent: E02
- priority: P1
- status: blocked
- depends_on: T02-05, T06-01
- blocks: T07-05
- source_chat: request-answer01, request-answer03, request-answer08, request-answer11, request-answer18
- phase: Phase 5

## 요약

현재 heuristic recognizer 위에 사용자별 rerank와 confidence calibration을 어떤 범위로 얹을지 정리한다.

## 아이디어 원본

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer03](../../../chat/request-answer03.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

이번 task에서는 아래를 정리한다.

* base family top-k rerank 구조
* overlay operator top-k rerank 구조
* family/operator별 confusion pair 보정 규칙
* fixed threshold 대신 confidence calibration을 도입하는 방법
* same shape invariance와 dependency rule을 유지하는 stop condition

중요한 전제는 아래다.

* final canonical family와 operator 결정권은 여전히 규칙계가 가진다.
* personalization은 top-k 재정렬과 ambiguous handling 보조에만 제한한다.
* `martial_axis requires void_cut` 같은 문법은 모델 밖 규칙으로 유지한다.

구현 기준 문서는 아래를 직접 참조한다.

* [`../../../20_queue/tutorial-personalization-plan.md`](../../../20_queue/tutorial-personalization-plan.md)

## 구현 의도

이 task의 의도는 “AI를 넣으면 잘 될 것 같다” 수준의 막연한 확장을 피하고, 현재 구조와 충돌하지 않는 작은 보조층만 허용하게 만드는 것이다.

## 관련 chat 문서

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer03](../../../chat/request-answer03.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T02-05 사용자 shape profile과 prototype bank 설계](task-05-user-shape-profile-and-prototype-bank.md)
* [T06-01 로그 지점과 로그 형식 정리](../epic-06-logging-and-debug-hooks/task-01-log-points-and-schema.md)

## 후속 task

* [T07-05 hybrid tutorial/public/synthetic 데이터 전략 backlog](../epic-07-future-expansion-backlog/task-05-hybrid-data-strategy-backlog.md)

## 완료 기준

* base family와 operator 모두에서 rerank/calibration 적용 위치가 분명해진다.
* same shape invariance를 깨지 않는 stop condition이 문서상 명확해진다.
* 이후 작은 모델 실험 backlog가 이 규칙을 그대로 참조할 수 있다.

## 지금은 보류하지만 자리 남길 요소

* 실제 tiny model 파라미터
* online adaptation 빈도

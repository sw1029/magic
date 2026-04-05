# T02-04 튜토리얼 입력 수집 기준 정리

- id: T02-04
- parent: E02
- priority: P1
- status: ready
- depends_on: T02-02
- blocks: T02-05
- source_chat: request-answer01, request-answer08, request-answer11, request-answer18
- phase: Phase 5

## 요약

직접 마법진 입력 전 튜토리얼에서 base family와 overlay operator에 대한 사용자별 입력 샘플을 어떤 방식으로 받을지 정리한다.

## 아이디어 원본

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

튜토리얼은 단순 onboarding이 아니라 아래 목적을 가진다.

* 사용자별 stroke articulation 수집
* family/operator별 초기 style bias 추정
* 이후 개인화 rerank와 confidence calibration의 seed 데이터 확보

이번 task에서는 아래를 정리한다.

* base 5종과 operator 6종의 최소 튜토리얼 과제 수
* `trace / recall / variation` 3종 입력의 비율
* 저장해야 하는 raw stroke, normalized stroke, expected label, device/session 메타
* 튜토리얼 데이터가 semantics가 아니라 입력 해석 보조에만 쓰인다는 원칙

구현 기준 문서는 아래를 직접 참조한다.

* [`../../../20_queue/tutorial-personalization-plan.md`](../../../20_queue/tutorial-personalization-plan.md)

## 구현 의도

이 task의 의도는 공개 데이터가 부족한 상황에서도, 사용자 본인의 초기 입력을 가장 가치 높은 정답 데이터로 확보하게 만드는 것이다.

## 관련 chat 문서

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T02-02 입력 해석 규칙 정리](task-02-input-interpretation-rules.md)

## 후속 task

* [T02-05 사용자 shape profile과 prototype bank 설계](task-05-user-shape-profile-and-prototype-bank.md)

## 완료 기준

* 튜토리얼 과제 구성이 문서상 결정된다.
* base family와 operator를 모두 포함한 최소 수집 기준이 정리된다.
* 저장 형식과 수집 목적이 구현자가 바로 사용할 수준으로 정리된다.

## 지금은 보류하지만 자리 남길 요소

* 실제 튜토리얼 UI 연출
* 디바이스별 입력 차이 보정

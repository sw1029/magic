# Epic 02. symbols-and-input

## 요약

기본 심볼 5종의 프로토타입 기준형, 입력 방식, 판독 규칙, 속도/각도에 따른 결과 차이 반영 규칙, 그리고 튜토리얼 기반 개인화 보조층 준비를 다루는 epic이다.

## 아이디어 원본

초기 아이디어의 “같은 모양은 같은 종류, 그리는 방식은 결과 차이”에서 출발하고, 단순화된 기본 심볼 시안, 가벼운 보조 도구 방향, 튜토리얼 기반 개인화 준비를 함께 묶는다.

## 구현하고자 하는 방향

심볼은 단순하게 유지하되, 입력 규칙과 판독 기준은 분명히 잡고, 속도·각도·안정성 차이는 결과 변화 요소로만 사용한다. 이후 튜토리얼에서 얻는 사용자별 입력은 semantics가 아니라 입력 해석 보조로만 활용한다.

## 구현 의도

이 epic의 목적은 입력 단계에서 유저 의도와 시스템 판정을 어긋나지 않게 만드는 것이다.

## 관련 chat 문서

* [request-answer08](../../../chat/request-answer08.md)
* [request-answer10](../../../chat/request-answer10.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer12](../../../chat/request-answer12.md)
* [request-answer13](../../../chat/request-answer13.md)
* [request-answer18](../../../chat/request-answer18.md)

## 하위 task

* [T02-01 기본 심볼 시안과 판독 기준 정리](task-01-base-symbol-prototypes.md)
* [T02-02 입력 해석 규칙 정리](task-02-input-interpretation-rules.md)
* [T02-03 가벼운 보조 도구 연결 자리 정리](task-03-lightweight-assist-hook.md)
* [T02-04 튜토리얼 입력 수집 기준 정리](task-04-tutorial-capture-guidelines.md)
* [T02-05 사용자 shape profile과 prototype bank 설계](task-05-user-shape-profile-and-prototype-bank.md)
* [T02-06 개인화 rerank와 confidence calibration 정리](task-06-personalized-rerank-and-confidence-calibration.md)
* [T02-07 operator tutorial context snapshot과 placement-aware personalization](task-07-operator-tutorial-context-and-placement-personalization.md)
* [T02-08 tutorial vector capture와 ML adaptation contract](task-08-tutorial-vector-capture-and-ml-adaptation-contract.md)

## Tiny ML 연결

* [T02-08 tutorial vector capture와 ML adaptation contract](task-08-tutorial-vector-capture-and-ml-adaptation-contract.md)은 [T07-08 tiny ML baseline 설계와 offline 실험안](../epic-07-future-expansion-backlog/task-08-tiny-ml-baseline-and-offline-eval.md)과 [T07-09 tutorial-aware personalization adapter 설계](../epic-07-future-expansion-backlog/task-09-tutorial-aware-personalization-adapter.md)를 잇는 vector-only contract bridge다.
* 현재 작업 트리에는 tutorial store와 personalization runtime groundwork가 있지만, actual ML adaptation activation은 `T02-08`이 닫히기 전까지 열지 않는다.

## 선행 task

* [Epic 01 source-and-freeze](../epic-01-source-and-freeze/README.md)

## 후속 task

* [Epic 03 spell-structure-and-stacking](../epic-03-spell-structure-and-stacking/README.md)
* [Epic 04 result-resolution-and-runtime](../epic-04-result-resolution-and-runtime/README.md)

## 완료 기준

* 기본 심볼 5종의 프로토타입 기준형이 문서상 고정된다.
* 같은 모양은 같은 종류라는 입력 기준이 정리된다.
* 속도와 각도가 결과 차이에 어떻게 들어가는지 정리된다.
* 튜토리얼 기반 개인화 보조층의 경계와 구조가 정리된다.
* operator placement-aware personalization의 허용 범위와 stop condition이 정리된다.
* tutorial vector capture가 tiny ML personalization과 어떻게 연결될지 계약이 `T07-08` offline baseline과 `T07-09` adapter 기준에 맞게 정리된다.

## 지금은 보류하지만 자리 남길 요소

* 최종 심볼 아트
* 고성능 보조 도구

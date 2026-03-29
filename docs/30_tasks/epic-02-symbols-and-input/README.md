# Epic 02. symbols-and-input

## 요약

기본 심볼 5종의 프로토타입 기준형, 입력 방식, 판독 규칙, 속도/각도에 따른 결과 차이 반영 규칙을 다루는 epic이다.

## 아이디어 원본

초기 아이디어의 “같은 모양은 같은 종류, 그리는 방식은 결과 차이”에서 출발하고, 단순화된 기본 심볼 시안과 가벼운 보조 도구 방향을 함께 묶는다.

## 구현하고자 하는 방향

심볼은 단순하게 유지하되, 입력 규칙과 판독 기준은 분명히 잡고, 속도·각도·안정성 차이는 결과 변화 요소로만 사용한다.

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

## 선행 task

* [Epic 01 source-and-freeze](../epic-01-source-and-freeze/README.md)

## 후속 task

* [Epic 03 spell-structure-and-stacking](../epic-03-spell-structure-and-stacking/README.md)
* [Epic 04 result-resolution-and-runtime](../epic-04-result-resolution-and-runtime/README.md)

## 완료 기준

* 기본 심볼 5종의 프로토타입 기준형이 문서상 고정된다.
* 같은 모양은 같은 종류라는 입력 기준이 정리된다.
* 속도와 각도가 결과 차이에 어떻게 들어가는지 정리된다.

## 지금은 보류하지만 자리 남길 요소

* 최종 심볼 아트
* 고성능 보조 도구

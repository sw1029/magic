# Epic 04. result-resolution-and-runtime

## 요약

마법 종류를 고정하고, 속도·각도·안정성·조합 차이를 결과 변화 요소로 연결하며, 허수아비 대상 최소 결과 흐름과 3D 확장 자리를 정리하는 epic이다.

## 아이디어 원본

초기 아이디어의 “모양은 고정, 결과는 달라짐”, 창발성 논의, 물리 초안, 클라이언트 최종 방향을 함께 반영한다.

## 구현하고자 하는 방향

현재는 결과를 완성형 대규모 물리 시스템까지 밀어붙이기보다, 첫 프로토타입에서 필요한 결과 뼈대를 먼저 정리하고 허수아비 검증이 가능하게 만든다.

## 구현 의도

이 epic의 목적은 입력 구조가 실제 게임 결과로 이어지는 중간 다리를 만드는 것이다.

## 관련 chat 문서

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer06](../../../chat/request-answer06.md)
* [request-answer07](../../../chat/request-answer07.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 하위 task

* [T04-01 마법 종류 고정과 결과 변화 규칙 정리](task-01-spell-fixity-and-result-variation.md)
* [T04-02 결과 생성 뼈대 정리](task-02-runtime-effect-skeleton.md)
* [T04-03 3D 확장 계약 정의](task-03-3d-extension-contract.md)

## 선행 task

* [Epic 02 symbols-and-input](../epic-02-symbols-and-input/README.md)
* [Epic 03 spell-structure-and-stacking](../epic-03-spell-structure-and-stacking/README.md)

## 후속 task

* [Epic 05 prototype-battle-sandbox](../epic-05-prototype-battle-sandbox/README.md)
* [Epic 06 logging-and-debug-hooks](../epic-06-logging-and-debug-hooks/README.md)
* [Epic 07 future-expansion-backlog](../epic-07-future-expansion-backlog/README.md)

## 완료 기준

* 종류를 고정하는 기준과 결과를 바꾸는 기준이 정리된다.
* 허수아비 대상 최소 결과 뼈대가 정리된다.
* 3D 후속 구현을 위한 자리도 문서상 확보된다.

## 지금은 보류하지만 자리 남길 요소

* 대규모 환경 상호작용
* 완성형 3D 전투 결과 체계

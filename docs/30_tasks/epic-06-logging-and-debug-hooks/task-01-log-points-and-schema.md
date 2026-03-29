# T06-01 로그 지점과 로그 형식 정리

- id: T06-01
- parent: E06
- priority: P0
- status: blocked
- depends_on: T04-01, T04-02
- blocks: T05-02, T06-02
- source_chat: request-answer07, request-answer09, request-answer18
- phase: Phase 5

## 요약

현재 프로토타입에서 어떤 시점에 어떤 정보를 로그로 남길지 정리한다.

## 아이디어 원본

* [request-answer07](../../../chat/request-answer07.md)
* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

아래 정보가 우선 로그 대상이다.

* 어떤 마법진이 입력되었는가
* 어떤 종류로 읽혔는가
* 속도/각도 차이가 어떻게 반영되었는가
* 어떤 결과가 나왔는가
* 허수아비에 어떤 검증 결과가 나타났는가

즉, 지금은 복잡한 연출보다 기록 가능한 구조를 먼저 만든다.

## 구현 의도

이 task의 의도는 이후 실패 흔적, 추가 반응층, 보조 도구, 실험 공간이 붙더라도 같은 기록 기반을 공유하게 만드는 것이다.

## 관련 chat 문서

* [request-answer07](../../../chat/request-answer07.md)
* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T04-01 마법 종류 고정과 결과 변화 규칙 정리](../epic-04-result-resolution-and-runtime/task-01-spell-fixity-and-result-variation.md)
* [T04-02 결과 생성 뼈대 정리](../epic-04-result-resolution-and-runtime/task-02-runtime-effect-skeleton.md)

## 후속 task

* [T05-02 공방 토글 규칙 정리](../epic-05-prototype-battle-sandbox/task-02-workshop-toggle-rules.md)
* [T06-02 분석 토글과 후속 hook 정리](task-02-analysis-toggle-and-hook-points.md)

## 완료 기준

* 로그를 남길 핵심 지점이 정리된다.
* 로그에 남길 핵심 정보가 정리된다.
* 이후 공방 분석과 backlog task가 같은 로그 기준을 참조한다.

## 지금은 보류하지만 자리 남길 요소

* 대용량 리플레이 데이터
* 시각적 로그 뷰어

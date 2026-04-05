# T07-05 hybrid tutorial/public/synthetic 데이터 전략 backlog

- id: T07-05
- parent: E07
- priority: P2
- status: backlog
- depends_on: T02-06
- blocks: T07-06
- source_chat: request-answer03, request-answer08, request-answer11, request-answer18
- phase: Phase 6

## 요약

튜토리얼 데이터, 내부 합성 데이터, 공개 online stroke 데이터를 함께 쓰는 hybrid 데이터 전략을 backlog 수준으로 정리한다.

## 아이디어 원본

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

이번 backlog에서는 아래를 정리한다.

* 내부 합성 데이터가 주력이라는 원칙
* tutorial data를 user-specific adaptation 핵심으로 쓰는 방식
* Quick, Draw / $-family / CROHME 같은 공개 데이터의 보조적 활용 범위
* base family와 overlay operator를 함께 고려한 hard negative 생성 전략

종류 판정의 핵심을 공개 데이터 기반 모델에게 넘기는 방향은 제외한다.

## 구현 의도

이 task의 의도는 실제 유저 테스트 데이터가 아직 부족할 때도, 어떤 데이터를 어떤 우선순위로 활용할지 혼동하지 않게 만드는 것이다.

## 관련 chat 문서

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T02-06 개인화 rerank와 confidence calibration 정리](../epic-02-symbols-and-input/task-06-personalized-rerank-and-confidence-calibration.md)

## 후속 task

* [T07-06 소형 모델 실험안 backlog](task-06-tiny-model-experiment-backlog.md)

## 완료 기준

* hybrid 데이터 전략의 우선순위가 backlog 문서로 정리된다.
* tutorial/public/synthetic의 역할 분리가 명확히 적힌다.

## 지금은 보류하지만 자리 남길 요소

* 실제 외부 데이터 다운로드 파이프라인
* 데이터 라이선스 검토 세부안

# T07-06 소형 모델 실험안 backlog

- id: T07-06
- parent: E07
- priority: P2
- status: backlog
- depends_on: T07-05
- blocks: -
- source_chat: request-answer01, request-answer08, request-answer11, request-answer18
- phase: Phase 6

## 요약

현재 heuristic recognizer 위에 붙일 수 있는 소형 모델 실험안을 backlog 수준으로 정리한다.

## 아이디어 원본

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

대상은 아래 정도로 제한한다.

* top-k rerank
* confidence calibration
* user-specific few-shot adaptation
* primitive proposal 보조

반대로 아래는 제외한다.

* 최종 family semantics 직접 분류
* operator dependency를 모델에게 위임
* heavy model 전제 구조

## 구현 의도

이 task의 의도는 “작은 모델을 쓸 수 있다”는 사실과 “그래서 무엇을 모델에 맡길 것인가”를 분명히 분리하는 것이다.

## 관련 chat 문서

* [request-answer01](../../../chat/request-answer01.md)
* [request-answer08](../../../chat/request-answer08.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T07-05 hybrid tutorial/public/synthetic 데이터 전략 backlog](task-05-hybrid-data-strategy-backlog.md)

## 후속 task

없음

## 완료 기준

* tiny model 후보군과 사용 위치가 backlog 문서로 정리된다.
* 현재 규칙 중심 구조를 깨지 않는 실험 범위가 적힌다.

## 지금은 보류하지만 자리 남길 요소

* 실제 모델 선택
* 실제 학습/서빙 비용 산정

# T07-08 tiny ML baseline 설계와 offline 실험안

- id: T07-08
- parent: E07
- priority: P2
- status: backlog
- depends_on: T07-05
- blocks: T07-09
- source_chat: request-answer03, request-answer11, request-answer18
- phase: Phase 6

## 요약

public auxiliary / synthetic / tutorial 계층을 분리한 상태에서 첫 tiny ML baseline과 offline eval 방식을 정리한다.

## 아이디어 원본

* [request-answer03](../../../chat/request-answer03.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

대상은 아래 정도로 제한한다.

* base family top-k rerank baseline
* operator hard-pair rerank baseline
* confidence calibration baseline
* Python sidecar training + Node runtime artifact export

반대로 아래는 제외한다.

* canonical family/operator direct classifier
* heavy model 전제 구조
* tutorial 없이 user-specific model 직접 학습

## 구현 의도

이 task의 의도는 “작은 모델을 하나 넣어 보자” 수준이 아니라,
현재 규칙계 위에 올릴 수 있는 첫 ML baseline의 범위를 결정 완료 상태로 만드는 것이다.

## 관련 문서

* [`../../../20_queue/tiny-ml-baseline-plan.md`](../../../20_queue/tiny-ml-baseline-plan.md)

## 선행 task

* [T07-05 hybrid tutorial/public/synthetic 데이터 전략 backlog](task-05-hybrid-data-strategy-backlog.md)

## 후속 task

* [T07-09 tutorial-aware personalization adapter 설계](task-09-tutorial-aware-personalization-adapter.md)

## 완료 기준

* base/operator baseline의 모델 타입, 입력 feature, output artifact 형식이 정리된다.
* offline eval 지표와 acceptance 기준이 문서상 고정된다.
* runtime shadow mode 적용 순서가 정리된다.

## 지금은 보류하지만 자리 남길 요소

* 실제 모델 하이퍼파라미터 탐색
* 실제 서빙 비용 산정

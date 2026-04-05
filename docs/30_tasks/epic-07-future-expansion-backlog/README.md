# Epic 07. future-expansion-backlog

## 요약

현재 범위에서 바로 구현하지 않는 3D, 가벼운 AI, hybrid 데이터 전략, 실험 스테이지, 실패 흔적과 추가 반응층을 backlog task로 정리하는 epic이다.
다만 tiny ML baseline branch처럼 현재 작업 트리에서 shadow-mode까지 먼저 들어온 항목도 이 epic 안에서 함께 추적한다.

## 아이디어 원본

창발 UX 강화 논의, 물리계층 초안, 가벼운 AI 방향, 실험 공간 분리 아이디어, 클라이언트 최종 결정에서 출발한다.

## 구현하고자 하는 방향

지금 안 만드는 것을 그냥 잊어버리지 않고, 나중에 붙일 수 있는 구조와 순서까지 backlog 문서로 남긴다.

## 구현 의도

이 epic의 목적은 현재 범위를 지키면서도, 향후 확장 방향을 잃지 않게 만드는 것이다.

## 관련 chat 문서

* [request-answer05](../../../chat/request-answer05.md)
* [request-answer07](../../../chat/request-answer07.md)
* [request-answer11](../../../chat/request-answer11.md)
* [request-answer18](../../../chat/request-answer18.md)

## 하위 task

* [T07-01 3D 후속 구현 backlog](task-01-3d-follow-up-backlog.md)
* [T07-02 가벼운 AI backlog](task-02-lightweight-ai-backlog.md)
* [T07-03 실험 스테이지 분리 backlog](task-03-experiment-stage-backlog.md)
* [T07-04 실패 흔적과 추가 반응층 backlog](task-04-failure-trace-and-extra-layer-backlog.md)
* [T07-05 hybrid tutorial/public/synthetic 데이터 전략 backlog](task-05-hybrid-data-strategy-backlog.md)
* [T07-06 소형 모델 실험안 backlog](task-06-tiny-model-experiment-backlog.md)
* [T07-07 placement-aware operator tiny model backlog](task-07-placement-aware-operator-tiny-model-backlog.md)
* [T07-08 tiny ML baseline 설계와 offline 실험안](task-08-tiny-ml-baseline-and-offline-eval.md)
* [T07-09 tutorial-aware personalization adapter 설계](task-09-tutorial-aware-personalization-adapter.md)

## Tiny ML Branch

* [T07-08 tiny ML baseline 설계와 offline 실험안](task-08-tiny-ml-baseline-and-offline-eval.md)은 현재 작업 트리의 dataset split/feature spec, base/operator artifact, runtime shadow-mode acceptance 기준을 맡는다.
* [T02-08 tutorial vector capture와 ML adaptation contract](../epic-02-symbols-and-input/task-08-tutorial-vector-capture-and-ml-adaptation-contract.md)은 `T07-09`로 가는 vector capture contract bridge다.
* [T07-09 tutorial-aware personalization adapter 설계](task-09-tutorial-aware-personalization-adapter.md)는 runtime groundwork가 있어도 `T02-08`이 닫히기 전까지 blocked로 유지한다.

## 선행 task

* [Epic 04 result-resolution-and-runtime](../epic-04-result-resolution-and-runtime/README.md)
* [Epic 05 prototype-battle-sandbox](../epic-05-prototype-battle-sandbox/README.md)
* [Epic 06 logging-and-debug-hooks](../epic-06-logging-and-debug-hooks/README.md)

## 후속 task

없음. 이 epic은 후속 개발의 시작점으로 사용한다.

## 완료 기준

* 지금 보류한 확장 요소가 각 backlog task로 분리된다.
* 향후 확장이 현재 구조와 어떻게 이어지는지 문서상 보인다.
* lightweight personalization과 tiny model 실험의 범위가 backlog 수준으로 정리된다.
* placement-aware operator personalization 이후의 tiny model 확장 범위가 backlog로 정리된다.
* public/synthetic/tutorial을 함께 쓰는 첫 tiny ML baseline과 user personalization 후속 설계가 현재 상태와 다음 상태를 구분한 채 추적된다.
* tiny ML gate-open acceptance는 `ambiguity` 또는 `hard-negative error` 감소, `family flip` 증가 `0`, `dependency violation` `0` 기준으로 유지된다.

## 지금은 보류하지만 자리 남길 요소

* 후속 버전 일정
* 실제 리소스 배정

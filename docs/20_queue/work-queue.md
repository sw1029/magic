# 작업 큐

이 문서는 살아있는 작업 큐다.

이 문서의 상태/의존성 값은 각 task 문서 frontmatter를 요약한 것이다.

운영 원칙:

* `할당`은 담당자가 아니라 우선순위와 선행관계를 뜻한다.
* P0/P1 task는 dependency 순서대로 정렬한다.
* 설명은 길게 적지 않고 실제 task 문서로만 연결한다.

상태 값:

* `todo`: 아직 시작 전이지만 현재 wave에서 바로 집지 않은 항목
* `ready`: 선행 task가 모두 충족되어 바로 시작 가능
* `in_progress`: 현재 진행 중
* `blocked`: 선행 task가 끝나지 않아 시작 불가
* `done`: 완료 기준 충족
* `backlog`: 지금은 보류

---

## P0

| priority | status | task | depends_on | blocks | next |
| --- | --- | --- | --- | --- | --- |
| P0 | done | [T01-01 원본 논의 타임라인 동결](../30_tasks/epic-01-source-and-freeze/task-01-timeline-and-source-map-freeze.md) | - | T01-02, T01-03, T03-01 | T01-02 |
| P0 | done | [T01-02 클라이언트 결정 동결](../30_tasks/epic-01-source-and-freeze/task-02-client-decision-freeze.md) | T01-01 | T01-03, T02-01, T02-02, T03-01, T03-02, T03-03, T04-01, T05-01 | T01-03 |
| P0 | done | [T01-03 프로토타입 목표 동결](../30_tasks/epic-01-source-and-freeze/task-03-prototype-target-freeze.md) | T01-01, T01-02 | T05-01, T05-03 | T02-01 |
| P0 | done | [T02-01 기본 심볼 시안과 판독 기준 정리](../30_tasks/epic-02-symbols-and-input/task-01-base-symbol-prototypes.md) | T01-02 | T02-02, T03-01 | T02-02 |
| P0 | done | [T02-02 입력 해석 규칙 정리](../30_tasks/epic-02-symbols-and-input/task-02-input-interpretation-rules.md) | T01-02, T02-01 | T04-01, T05-01 | T03-01 |
| P0 | ready | [T03-01 단일 마법진 구조 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-01-single-circle-structure.md) | T01-01, T01-02, T02-01 | T03-02, T03-03, T04-01 | T03-02 |
| P0 | blocked | [T03-02 다중 성질 변형 중첩 규칙 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-02-multi-attribute-stacking.md) | T01-02, T03-01 | T03-03, T04-01, T04-02 | T03-03 |
| P0 | blocked | [T03-03 여러 마법진 연결과 無/武 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-03-multi-circle-composition-and-null-mu.md) | T01-02, T03-01, T03-02 | T04-02, T05-01 | T04-01 |
| P0 | blocked | [T04-01 마법 종류 고정과 결과 변화 규칙 정리](../30_tasks/epic-04-result-resolution-and-runtime/task-01-spell-fixity-and-result-variation.md) | T01-02, T02-02, T03-01, T03-02 | T04-02, T05-01, T06-01 | T04-02 |
| P0 | blocked | [T04-02 결과 생성 뼈대 정리](../30_tasks/epic-04-result-resolution-and-runtime/task-02-runtime-effect-skeleton.md) | T03-02, T03-03, T04-01 | T05-01, T06-01 | T05-01 |
| P0 | blocked | [T05-01 허수아비 전투 시전 루프](../30_tasks/epic-05-prototype-battle-sandbox/task-01-dummy-battle-loop.md) | T01-03, T02-02, T03-03, T04-01, T04-02 | T05-02, T05-03 | T06-01 |
| P0 | blocked | [T06-01 로그 지점과 로그 형식 정리](../30_tasks/epic-06-logging-and-debug-hooks/task-01-log-points-and-schema.md) | T04-01, T04-02 | T05-02, T06-02 | T05-02 |

## P1

| priority | status | task | depends_on | blocks | next |
| --- | --- | --- | --- | --- | --- |
| P1 | ready | [T02-04 튜토리얼 입력 수집 기준 정리](../30_tasks/epic-02-symbols-and-input/task-04-tutorial-capture-guidelines.md) | T02-02 | T02-05 | T02-05 |
| P1 | blocked | [T02-05 사용자 shape profile과 prototype bank 설계](../30_tasks/epic-02-symbols-and-input/task-05-user-shape-profile-and-prototype-bank.md) | T02-02, T02-04 | T02-06, T07-05 | T02-06 |
| P1 | blocked | [T02-06 개인화 rerank와 confidence calibration 정리](../30_tasks/epic-02-symbols-and-input/task-06-personalized-rerank-and-confidence-calibration.md) | T02-05, T06-01 | T07-05 | T05-02 |
| P1 | done | [T02-07 operator tutorial context snapshot과 placement-aware personalization](../30_tasks/epic-02-symbols-and-input/task-07-operator-tutorial-context-and-placement-personalization.md) | T02-05, T02-06 | T07-07 | T05-02 |
| P1 | done | [T02-08 tutorial vector capture와 ML adaptation contract](../30_tasks/epic-02-symbols-and-input/task-08-tutorial-vector-capture-and-ml-adaptation-contract.md) | T02-04, T02-07 | T07-09 | T07-09 |
| P1 | blocked | [T05-02 공방 토글 규칙 정리](../30_tasks/epic-05-prototype-battle-sandbox/task-02-workshop-toggle-rules.md) | T05-01, T06-01 | T06-02 | T05-03 |
| P1 | blocked | [T05-03 허수아비 검증 시나리오 정리](../30_tasks/epic-05-prototype-battle-sandbox/task-03-sandbox-validation-scenarios.md) | T01-03, T05-01 | - | T06-02 |
| P1 | blocked | [T06-02 분석 토글과 후속 hook 정리](../30_tasks/epic-06-logging-and-debug-hooks/task-02-analysis-toggle-and-hook-points.md) | T05-02, T06-01 | T07-04 | T02-03 |
| P1 | blocked | [T02-03 가벼운 보조 도구 연결 자리 정리](../30_tasks/epic-02-symbols-and-input/task-03-lightweight-assist-hook.md) | T02-02, T06-01 | T07-02 | T04-03 |

## P2

| priority | status | task | depends_on | blocks | next |
| --- | --- | --- | --- | --- | --- |
| P2 | blocked | [T04-03 3D 확장 계약 정의](../30_tasks/epic-04-result-resolution-and-runtime/task-03-3d-extension-contract.md) | T03-01, T04-01 | T07-01 | T07-01 |
| P2 | backlog | [T07-01 3D 후속 구현 backlog](../30_tasks/epic-07-future-expansion-backlog/task-01-3d-follow-up-backlog.md) | T04-03 | - | T07-02 |
| P2 | backlog | [T07-02 가벼운 AI backlog](../30_tasks/epic-07-future-expansion-backlog/task-02-lightweight-ai-backlog.md) | T02-03 | - | T07-03 |
| P2 | done | [T07-05 hybrid tutorial/public/synthetic 데이터 전략 backlog](../30_tasks/epic-07-future-expansion-backlog/task-05-hybrid-data-strategy-backlog.md) | T02-06 | T07-06 | T07-06 |
| P2 | backlog | [T07-06 소형 모델 실험안 backlog](../30_tasks/epic-07-future-expansion-backlog/task-06-tiny-model-experiment-backlog.md) | T07-05 | - | - |
| P2 | backlog | [T07-07 placement-aware operator tiny model backlog](../30_tasks/epic-07-future-expansion-backlog/task-07-placement-aware-operator-tiny-model-backlog.md) | T02-07, T07-05 | - | - |
| P2 | done | [T07-08 tiny ML baseline 설계와 offline 실험안](../30_tasks/epic-07-future-expansion-backlog/task-08-tiny-ml-baseline-and-offline-eval.md) | T07-05 | T07-09 | T02-08 |
| P2 | in_progress | [T07-09 tutorial-aware personalization adapter 설계](../30_tasks/epic-07-future-expansion-backlog/task-09-tutorial-aware-personalization-adapter.md) | T02-08, T07-08 | - | T07-09 |
| P2 | backlog | [T07-03 실험 스테이지 분리 backlog](../30_tasks/epic-07-future-expansion-backlog/task-03-experiment-stage-backlog.md) | T05-03 | - | T07-04 |
| P2 | backlog | [T07-04 실패 흔적과 추가 반응층 backlog](../30_tasks/epic-07-future-expansion-backlog/task-04-failure-trace-and-extra-layer-backlog.md) | T06-02 | - | - |

tiny ML branch 메모:

* `T07-08`은 현재 작업 트리의 shadow baseline acceptance 기준 문서다.
* `T02-08`은 tutorial vector capture를 `T07-09` adapter에 연결하는 계약 bridge였고, 현재 작업 트리에서 contract/helper까지 닫혔다.
* `T07-09`는 shadow-only adapter까지 연결돼 있으며, 현재 직접 남은 범위는 acceptance/provenance와 gate-open readiness다.
* 잔여 장기 작업은 `contract -> export/provenance -> adapter shadow -> gate-open readiness` 4개 wave로 본다.
* 이 재정리 기준 문서는 [`tutorial-ml-adaptation-rollout-plan.md`](tutorial-ml-adaptation-rollout-plan.md)다.

## P3

현재 없음. 새 task가 생기면 backlog 성격의 조사 항목이나 미정 확장 항목을 여기에 둔다.

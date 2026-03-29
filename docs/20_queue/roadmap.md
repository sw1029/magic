# 구현 로드맵

현재 프로젝트는 아래 6단계 순서로 진행합니다.

각 단계는 앞 단계의 산출물을 다음 단계의 기준으로 사용합니다.

---

## Phase 1. 문서 정리와 기준 동결

목표:

* `chat/` 논의 흐름을 정리한다.
* 클라이언트 확정값을 문서로 고정한다.
* 첫 프로토타입 목표를 결정 완료 상태로 정리한다.

주요 task:

* [T01-01 원본 논의 타임라인 동결](../30_tasks/epic-01-source-and-freeze/task-01-timeline-and-source-map-freeze.md)
* [T01-02 클라이언트 결정 동결](../30_tasks/epic-01-source-and-freeze/task-02-client-decision-freeze.md)
* [T01-03 프로토타입 목표 동결](../30_tasks/epic-01-source-and-freeze/task-03-prototype-target-freeze.md)

완료 기준:

* 이후 task가 참고할 기준 문서가 흔들리지 않는다.

---

## Phase 2. 2D 입력과 판독 규칙

목표:

* 기본 문양 5종을 프로토타입 기준형으로 고정한다.
* 같은 모양은 같은 종류라는 원칙을 판독 규칙으로 정리한다.
* 속도와 각도가 결과 차이에 어떻게 들어가는지 정리한다.

주요 task:

* [T02-01 기본 심볼 시안과 판독 기준 정리](../30_tasks/epic-02-symbols-and-input/task-01-base-symbol-prototypes.md)
* [T02-02 입력 해석 규칙 정리](../30_tasks/epic-02-symbols-and-input/task-02-input-interpretation-rules.md)

완료 기준:

* 기본 입력과 결과 차이의 연결 규칙이 정해진다.

---

## Phase 3. 마법진 구조와 중첩

목표:

* 한 마법진 안의 구조를 정리한다.
* 성질 변형 여러 겹 허용 규칙을 정리한다.
* 여러 마법진 연결 규칙과 無/武의 차이를 정리한다.

주요 task:

* [T03-01 단일 마법진 구조 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-01-single-circle-structure.md)
* [T03-02 다중 성질 변형 중첩 규칙 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-02-multi-attribute-stacking.md)
* [T03-03 여러 마법진 연결과 無/武 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-03-multi-circle-composition-and-null-mu.md)

완료 기준:

* 단일 구조와 복합 구조가 모두 문서상 결정 완료 상태가 된다.

---

## Phase 4. 결과 연결과 허수아비 전투

목표:

* 마법 종류 고정과 결과 차이 규칙을 연결한다.
* 허수아비 대상 최소 전투 결과를 만들 수 있게 한다.
* 3D 확장을 나중에 붙일 수 있는 자리도 정의한다.

주요 task:

* [T04-01 마법 종류 고정과 결과 변화 규칙 정리](../30_tasks/epic-04-result-resolution-and-runtime/task-01-spell-fixity-and-result-variation.md)
* [T04-02 결과 생성 뼈대 정리](../30_tasks/epic-04-result-resolution-and-runtime/task-02-runtime-effect-skeleton.md)
* [T05-01 허수아비 전투 시전 루프](../30_tasks/epic-05-prototype-battle-sandbox/task-01-dummy-battle-loop.md)

완료 기준:

* `마법진 입력 -> 결과 생성 -> 허수아비 검증` 흐름이 성립한다.

---

## Phase 5. 로그와 보류층 자리 확보

목표:

* 지금은 로그만 남기고, 후속 층을 붙일 hook를 정리한다.
* 공방 토글과 최소 분석 흐름을 정리한다.

주요 task:

* [T06-01 로그 지점과 로그 형식 정리](../30_tasks/epic-06-logging-and-debug-hooks/task-01-log-points-and-schema.md)
* [T06-02 분석 토글과 후속 hook 정리](../30_tasks/epic-06-logging-and-debug-hooks/task-02-analysis-toggle-and-hook-points.md)
* [T05-02 공방 토글 규칙 정리](../30_tasks/epic-05-prototype-battle-sandbox/task-02-workshop-toggle-rules.md)

완료 기준:

* 로그와 최소 분석 구조가 있고, 후속 층을 붙일 자리가 문서상 확보된다.

---

## Phase 6. 후속 확장 backlog

목표:

* 3D, 가벼운 AI, 실험 전용 스테이지, 실패 흔적/추가 반응층을 backlog로 정리한다.

주요 task:

* [Epic 07 후속 확장 backlog](../30_tasks/epic-07-future-expansion-backlog/README.md)

완료 기준:

* 지금 안 만드는 것과 나중에 붙일 것을 혼동하지 않는다.

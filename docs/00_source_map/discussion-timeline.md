# 논의 타임라인

이 문서는 `chat/request-answer01.md`부터 `chat/request-answer18.md`까지의 흐름을 시간 순서대로 정리한 문서입니다.

각 항목은 세 가지를 남깁니다.

* 당시 아이디어
* 이후 어떻게 바뀌었는지
* 최종적으로 어느 task로 이어졌는지

---

## 01. [request-answer01](../../chat/request-answer01.md)

* 당시 아이디어: 화면에 마법진을 그리면 실시간으로 마법이 나가고, 속도·각도·품질·환경에 따라 결과가 달라지는 게임 아이디어가 제안되었다.
* 이후 변경: “모양은 종류를 정하고, 손맛은 결과 차이를 만든다”는 큰 원칙의 출발점이 되었다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
  * [`../10_direction/prototype-target.md`](../10_direction/prototype-target.md)
* 최종 연결:
  * [T01-01 원본 논의 타임라인 동결](../30_tasks/epic-01-source-and-freeze/task-01-timeline-and-source-map-freeze.md)
  * [T04-01 마법 종류 고정과 결과 변화 규칙 정리](../30_tasks/epic-04-result-resolution-and-runtime/task-01-spell-fixity-and-result-variation.md)
  * [T05-01 허수아비 전투 시전 루프](../30_tasks/epic-05-prototype-battle-sandbox/task-01-dummy-battle-loop.md)

## 02. [request-answer02](../../chat/request-answer02.md)

* 당시 아이디어: 비선형 공간 언어, 직관적 해석, 2D 우선, 3D 확장이라는 언어 설계 방향이 잡혔다.
* 이후 변경: 실제 문자처럼 모든 것을 표현하는 체계가 아니라, 마법이라는 좁은 도메인을 위한 시각 문법으로 수렴했다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
* 최종 연결:
  * [T01-01 원본 논의 타임라인 동결](../30_tasks/epic-01-source-and-freeze/task-01-timeline-and-source-map-freeze.md)
  * [T03-01 단일 마법진 구조 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-01-single-circle-structure.md)

## 03. [request-answer03](../../chat/request-answer03.md)

* 당시 아이디어: 5개 기본 속성과 10개 파생 속성, core primitive, 3D operator, 오류 정정 방향이 구체화되었다.
* 이후 변경: 속성 체계가 나중에 더 단순한 기본 문양과 중첩 규칙으로 재정리되었지만, 여기서 제안된 분류와 primitive는 계속 뼈대로 남았다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
  * [`../10_direction/symbol-prototypes.md`](../10_direction/symbol-prototypes.md)
* 최종 연결:
  * [T03-01 단일 마법진 구조 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-01-single-circle-structure.md)
  * [T03-03 여러 마법진 연결과 無/武 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-03-multi-circle-composition-and-null-mu.md)

## 04. [request-answer04](../../chat/request-answer04.md)

* 당시 아이디어: 언어 정체성, 시각 층위, 오류 정정, 컴파일 구조, 3D 규칙을 포함한 첫 종합안이 만들어졌다.
* 이후 변경: 이후 논의에서 더 단순한 기본 문양 체계로 바뀌었지만, 전체 구조와 읽기 순서는 유지되었다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
* 최종 연결:
  * [T01-02 클라이언트 결정 동결](../30_tasks/epic-01-source-and-freeze/task-02-client-decision-freeze.md)
  * [T03-01 단일 마법진 구조 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-01-single-circle-structure.md)
  * [T04-01 마법 종류 고정과 결과 변화 규칙 정리](../30_tasks/epic-04-result-resolution-and-runtime/task-01-spell-fixity-and-result-variation.md)

## 05. [request-answer05](../../chat/request-answer05.md)

* 당시 아이디어: 현재 구조가 창발적 UX의 필요조건은 만족하지만, 실험 도구 계층이 부족하다는 진단이 나왔다.
* 이후 변경: 공방, 비교, 다시 보기, 환경 스윕 같은 기능이 “나중에 붙일 층”으로 분리되었다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
  * [`../10_direction/prototype-target.md`](../10_direction/prototype-target.md)
* 최종 연결:
  * [T05-03 허수아비 검증 시나리오 정리](../30_tasks/epic-05-prototype-battle-sandbox/task-03-sandbox-validation-scenarios.md)
  * [T07-03 실험 스테이지 분리 backlog](../30_tasks/epic-07-future-expansion-backlog/task-03-experiment-stage-backlog.md)

## 06. [request-answer06](../../chat/request-answer06.md)

* 당시 아이디어: 언어 자체는 강하지만, 진짜 창발성은 공통 물리층과 피드백 구조가 붙어야 생긴다는 판단이 정리되었다.
* 이후 변경: “shared dream physics”에 해당하는 공통 결과 규칙과 반응 구조를 구현해야 한다는 방향으로 이어졌다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
* 최종 연결:
  * [T04-02 결과 생성 뼈대 정리](../30_tasks/epic-04-result-resolution-and-runtime/task-02-runtime-effect-skeleton.md)
  * [T07-04 실패 흔적과 추가 반응층 backlog](../30_tasks/epic-07-future-expansion-backlog/task-04-failure-trace-and-extra-layer-backlog.md)

## 07. [request-answer07](../../chat/request-answer07.md)

* 당시 아이디어: 상태 채널, 환경 변수, 사용자 상태, 이벤트 추출, 로그, 3D 확장 자리까지 포함하는 물리계층 초안이 제안되었다.
* 이후 변경: 클라이언트 최종 의견에 따라 초기 구현에서는 로그만 먼저 남기고, 시각적 실패 흔적과 후속 반응층은 보류하는 방향으로 다듬어졌다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
  * [`../10_direction/prototype-target.md`](../10_direction/prototype-target.md)
* 최종 연결:
  * [T04-02 결과 생성 뼈대 정리](../30_tasks/epic-04-result-resolution-and-runtime/task-02-runtime-effect-skeleton.md)
  * [T06-01 로그 지점과 로그 형식 정리](../30_tasks/epic-06-logging-and-debug-hooks/task-01-log-points-and-schema.md)
  * [T04-03 3D 확장 계약 정의](../30_tasks/epic-04-result-resolution-and-runtime/task-03-3d-extension-contract.md)

## 08. [request-answer08](../../chat/request-answer08.md)

* 당시 아이디어: 속도는 두 번 쓰지 말 것, 각도는 의미 있는 각도와 손버릇 각도를 분리할 것, 개인차는 종류가 아니라 결과 차이에 반영할 것이라는 원칙이 정리되었다.
* 이후 변경: 클라이언트가 “종류는 고정하되 속도와 각도는 결과물에 영향을 줘야 한다”고 승인하면서 현재 방향의 핵심 규칙이 되었다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
  * [`../10_direction/prototype-target.md`](../10_direction/prototype-target.md)
* 최종 연결:
  * [T02-02 입력 해석 규칙 정리](../30_tasks/epic-02-symbols-and-input/task-02-input-interpretation-rules.md)
  * [T04-01 마법 종류 고정과 결과 변화 규칙 정리](../30_tasks/epic-04-result-resolution-and-runtime/task-01-spell-fixity-and-result-variation.md)

## 09. [request-answer09](../../chat/request-answer09.md)

* 당시 아이디어: 언어, 물리, 개인화, UX, combat/forge 분리를 모두 묶은 대형 종합본이 나왔다.
* 이후 변경: 이후 문서들은 대부분 이 문서를 바탕으로 단순화와 클라이언트 결정 반영을 수행했다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
  * [`../10_direction/prototype-target.md`](../10_direction/prototype-target.md)
* 최종 연결:
  * [T01-02 클라이언트 결정 동결](../30_tasks/epic-01-source-and-freeze/task-02-client-decision-freeze.md)
  * [T01-03 프로토타입 목표 동결](../30_tasks/epic-01-source-and-freeze/task-03-prototype-target-freeze.md)
  * [T04-01 마법 종류 고정과 결과 변화 규칙 정리](../30_tasks/epic-04-result-resolution-and-runtime/task-01-spell-fixity-and-result-variation.md)

## 10. [request-answer10](../../chat/request-answer10.md)

* 당시 아이디어: 기본 심볼 실루엣과 학파별 VFX 방향이 제안되었다.
* 이후 변경: 클라이언트는 기본 심볼 5종을 별도 디자인 예정으로 두었고, 현재는 프로토타입용 단순 시안을 먼저 사용한다.
* 관련 방향 문서:
  * [`../10_direction/symbol-prototypes.md`](../10_direction/symbol-prototypes.md)
* 최종 연결:
  * [T02-01 기본 심볼 시안과 판독 기준 정리](../30_tasks/epic-02-symbols-and-input/task-01-base-symbol-prototypes.md)
  * [T07-01 3D 후속 backlog](../30_tasks/epic-07-future-expansion-backlog/task-01-3d-follow-up-backlog.md)

## 11. [request-answer11](../../chat/request-answer11.md)

* 당시 아이디어: AI는 필수가 아니며, 필요하더라도 보조 도구로만 쓰고 심볼은 코드로 생성 가능한 형태로 다루는 방향이 제안되었다.
* 이후 변경: 클라이언트는 무거운 AI를 배제하고, 학습 데이터를 마련 가능한 범위에서만 가벼운 보조 도구를 쓰는 방향으로 확정했다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
  * [`../10_direction/prototype-target.md`](../10_direction/prototype-target.md)
* 최종 연결:
  * [T02-03 가벼운 보조 도구 연결 자리 정리](../30_tasks/epic-02-symbols-and-input/task-03-lightweight-assist-hook.md)
  * [T07-02 가벼운 AI backlog](../30_tasks/epic-07-future-expansion-backlog/task-02-lightweight-ai-backlog.md)

## 12. [request-answer12](../../chat/request-answer12.md)

* 당시 아이디어: 기본 심볼을 radical로 더 줄이고, 파생을 overlay로 보내는 방향이 제안되었다.
* 이후 변경: 현재는 기본 문양을 단순한 프로토타입 기준형으로 두고, 실제 복잡성은 결과 규칙과 중첩 구조에서 회수하는 방향으로 이어졌다.
* 관련 방향 문서:
  * [`../10_direction/symbol-prototypes.md`](../10_direction/symbol-prototypes.md)
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
* 최종 연결:
  * [T02-01 기본 심볼 시안과 판독 기준 정리](../30_tasks/epic-02-symbols-and-input/task-01-base-symbol-prototypes.md)
  * [T03-02 다중 성질 변형 중첩 규칙 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-02-multi-attribute-stacking.md)

## 13. [request-answer13](../../chat/request-answer13.md)

* 당시 아이디어: 간략화 radical 체계를 전면 채택한 최종 설계안이 정리되었다.
* 이후 변경: 클라이언트는 “한 마법진 안에 성질 변형 1개” 제한을 뒤집고, 여러 번 가능해야 한다고 확정했다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
  * [`../10_direction/prototype-target.md`](../10_direction/prototype-target.md)
* 최종 연결:
  * [T01-02 클라이언트 결정 동결](../30_tasks/epic-01-source-and-freeze/task-02-client-decision-freeze.md)
  * [T02-01 기본 심볼 시안과 판독 기준 정리](../30_tasks/epic-02-symbols-and-input/task-01-base-symbol-prototypes.md)
  * [T03-02 다중 성질 변형 중첩 규칙 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-02-multi-attribute-stacking.md)

## 14. [request-answer14](../../chat/request-answer14.md)

* 당시 아이디어: 단순화된 체계가 UX와 창발성 측면에서 기존보다 더 낫다는 비교 평가가 제시되었다.
* 이후 변경: “심볼 자체보다 시스템 전체가 강해야 한다”는 방향이 문서 체계와 작업 우선순위에 반영되었다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
* 최종 연결:
  * [T01-03 프로토타입 목표 동결](../30_tasks/epic-01-source-and-freeze/task-03-prototype-target-freeze.md)
  * [T05-03 허수아비 검증 시나리오 정리](../30_tasks/epic-05-prototype-battle-sandbox/task-03-sandbox-validation-scenarios.md)

## 15. [request-answer15](../../chat/request-answer15.md)

* 당시 아이디어: 수정된 체계 기준으로 마법진 작성 양식, 단계별 작성 순서, 각 요소의 UX 역할과 기능 역할이 통합 정리되었다.
* 이후 변경: 현재 task 구조에서 단일 마법진, 여러 마법진 연결, 공방/전투 흐름을 설계하는 직접 근거가 되었다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
  * [`../10_direction/prototype-target.md`](../10_direction/prototype-target.md)
* 최종 연결:
  * [T03-01 단일 마법진 구조 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-01-single-circle-structure.md)
  * [T03-03 여러 마법진 연결과 無/武 정리](../30_tasks/epic-03-spell-structure-and-stacking/task-03-multi-circle-composition-and-null-mu.md)
  * [T05-01 허수아비 전투 시전 루프](../30_tasks/epic-05-prototype-battle-sandbox/task-01-dummy-battle-loop.md)

## 16. [request-answer16](../../chat/request-answer16.md)

* 당시 아이디어: 구현 전에 무엇을 확정해야 하는지 비전문가용으로 정리한 문서가 만들어졌다.
* 이후 변경: 이후 승인 문서와 클라이언트 최종 정리 문서의 바탕이 되었다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
  * [`../10_direction/prototype-target.md`](../10_direction/prototype-target.md)
* 최종 연결:
  * [T01-02 클라이언트 결정 동결](../30_tasks/epic-01-source-and-freeze/task-02-client-decision-freeze.md)
  * [T01-03 프로토타입 목표 동결](../30_tasks/epic-01-source-and-freeze/task-03-prototype-target-freeze.md)

## 17. [request-answer17](../../chat/request-answer17.md)

* 당시 아이디어: 클라이언트 승인용 체크리스트와 승인 문서 형식이 정리되었다.
* 이후 변경: 실제 클라이언트 피드백은 다음 문서에서 반영되었고, 이 문서는 승인 프로세스 기록으로 남는다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
* 최종 연결:
  * [T01-02 클라이언트 결정 동결](../30_tasks/epic-01-source-and-freeze/task-02-client-decision-freeze.md)

## 18. [request-answer18](../../chat/request-answer18.md)

* 당시 아이디어: 클라이언트 의견을 반영한 최종 방향, 첫 프로토타입 우선순위, 기본 심볼 임시 시안이 정리되었다.
* 이후 변경: 현재 기준에서 가장 최신의 고정 문서이며, 이후 task와 queue는 이 문서를 기준으로 잡는다.
* 관련 방향 문서:
  * [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
  * [`../10_direction/prototype-target.md`](../10_direction/prototype-target.md)
  * [`../10_direction/symbol-prototypes.md`](../10_direction/symbol-prototypes.md)
* 최종 연결:
  * [T01-02 클라이언트 결정 동결](../30_tasks/epic-01-source-and-freeze/task-02-client-decision-freeze.md)
  * [T01-03 프로토타입 목표 동결](../30_tasks/epic-01-source-and-freeze/task-03-prototype-target-freeze.md)
  * [T02-01 기본 심볼 시안과 판독 기준 정리](../30_tasks/epic-02-symbols-and-input/task-01-base-symbol-prototypes.md)

---

## 현재 기준 문서

원본 흐름을 모두 읽지 않고 현재 결정만 보고 싶다면 아래 문서를 기준으로 본다.

* [`client-decisions.md`](client-decisions.md)
* [`../10_direction/final-direction.md`](../10_direction/final-direction.md)
* [`../10_direction/prototype-target.md`](../10_direction/prototype-target.md)
* [`../20_queue/work-queue.md`](../20_queue/work-queue.md)

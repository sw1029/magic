# 작업 큐와 계획 문서

이 디렉토리는 실제 작업 순서와 문서 운영 규칙을 다룹니다.

문서 구성:

* [`roadmap.md`](roadmap.md): 단계별 구현 흐름
* [`work-queue.md`](work-queue.md): 우선순위와 선행관계 기준 작업 큐
* [`prototype-implementation-plan.md`](prototype-implementation-plan.md): 현재 Web UI recognizer를 다음 단계 실구현으로 확장하기 위한 상세 실행 계획
* [`prototype-prompt-pack.md`](prototype-prompt-pack.md): 위 실행 계획을 실제 작업으로 옮길 때 쓰는 프롬프트 묶음
* [`tutorial-personalization-plan.md`](tutorial-personalization-plan.md): 튜토리얼 기반 개인화 인식 보조층 확장 실행 설계
* [`tutorial-personalization-prompt-pack.md`](tutorial-personalization-prompt-pack.md): 위 개인화 설계를 실제 작업으로 옮길 때 쓰는 프롬프트 묶음
* [`hci-ux-demo-plan.md`](hci-ux-demo-plan.md): 클라이언트 HCI 검토를 위한 Web UI 데모 확장 계획과 현재 구현 반영 상태
* [`hci-ux-demo-prompt-pack.md`](hci-ux-demo-prompt-pack.md): 위 HCI 데모 확장을 실제 작업으로 옮길 때 쓰는 프롬프트 묶음
* [`doc-conventions.md`](doc-conventions.md): 문서 템플릿과 링크 규칙
* [`../../scripts/validate-doc-state.mjs`](../../scripts/validate-doc-state.mjs): task frontmatter와 queue 요약의 동기화 검증 스크립트

현재 구현된 HCI demo를 먼저 확인하려면 아래 순서가 가장 빠릅니다.

1. 루트 [`README.md`](../../README.md)
2. [`hci-ux-demo-plan.md`](hci-ux-demo-plan.md)
3. [`hci-ux-demo-prompt-pack.md`](hci-ux-demo-prompt-pack.md)

읽는 순서:

1. [`roadmap.md`](roadmap.md)
2. [`work-queue.md`](work-queue.md)
3. [`prototype-implementation-plan.md`](prototype-implementation-plan.md)
4. [`tutorial-personalization-plan.md`](tutorial-personalization-plan.md)
5. [`hci-ux-demo-plan.md`](hci-ux-demo-plan.md)
6. [`prototype-prompt-pack.md`](prototype-prompt-pack.md)
7. [`tutorial-personalization-prompt-pack.md`](tutorial-personalization-prompt-pack.md)
8. [`hci-ux-demo-prompt-pack.md`](hci-ux-demo-prompt-pack.md)
9. [`doc-conventions.md`](doc-conventions.md)
10. [`../../scripts/validate-doc-state.mjs`](../../scripts/validate-doc-state.mjs)

이 디렉토리는 “무엇을 어떤 순서로 진행할 것인가”를 설명합니다. 개별 작업 정의는 [`../30_tasks/README.md`](../30_tasks/README.md) 아래에서 봅니다.

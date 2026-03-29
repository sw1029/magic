# T05-02 공방 토글 규칙 정리

- id: T05-02
- parent: E05
- priority: P1
- status: blocked
- depends_on: T05-01, T06-01
- blocks: T06-02
- source_chat: request-answer09, request-answer18
- phase: Phase 5

## 요약

공방 안에서 분석용 표시를 유저가 켜고 끌 수 있도록 하는 규칙과 범위를 정리한다.

## 아이디어 원본

* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 구현하고자 하는 방향

공방은 단순히 “항상 자세한 정보가 켜져 있는 공간”으로 두지 않는다.
현재 방향은 아래와 같다.

* 기본 화면은 깔끔하게 유지 가능
* 필요할 때만 자세한 분석 표시를 켤 수 있음
* on/off가 유저 선택권으로 제공됨

## 구현 의도

이 task의 의도는 공방을 학습 가능한 공간으로 만들되, 정보 과밀 때문에 오히려 불친절해지는 일을 막는 것이다.

## 관련 chat 문서

* [request-answer09](../../../chat/request-answer09.md)
* [request-answer18](../../../chat/request-answer18.md)

## 선행 task

* [T05-01 허수아비 전투 시전 루프](task-01-dummy-battle-loop.md)
* [T06-01 로그 지점과 로그 형식 정리](../epic-06-logging-and-debug-hooks/task-01-log-points-and-schema.md)

## 후속 task

* [T06-02 분석 토글과 후속 hook 정리](../epic-06-logging-and-debug-hooks/task-02-analysis-toggle-and-hook-points.md)

## 완료 기준

* 공방 표시 on/off 대상 정보가 정리된다.
* 공방 토글이 로그와 분석 구조와 연결되는 지점이 정리된다.

## 지금은 보류하지만 자리 남길 요소

* 더 고급 비교 UI
* 실험 스테이지 전용 화면

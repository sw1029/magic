# 문서 규칙

이 문서는 `docs/` 아래 문서를 작성할 때 지켜야 할 공통 규칙을 정리한 문서입니다.

---

## 1. 문서 언어

* 문서 언어는 한국어로 통일한다.
* 불필요한 영어 혼용을 줄인다.
* 용어가 꼭 필요할 때만 괄호로 병기한다.

---

## 2. 상위 구조 규칙

* `00_source_map`: 원본 논의와 결정 근거
* `10_direction`: 현재 구현 방향과 목표
* `20_queue`: 순서, 우선순위, 문서 규칙
* `30_tasks`: epic 및 상/하위 task

`chat/`는 수정하지 않고 원본 보관소로 유지한다.

---

## 3. task 문서 공통 템플릿

모든 epic README와 subtask 문서는 아래 섹션을 고정으로 가진다.

* `요약`
* `아이디어 원본`
* `구현하고자 하는 방향`
* `구현 의도`
* `관련 chat 문서`
* `선행 task`
* `후속 task`
* `완료 기준`
* `지금은 보류하지만 자리 남길 요소`

subtask 문서는 위 섹션에 더해 아래 메타 필드를 문서 상단에 둔다.

* `id`
* `parent`
* `priority`
* `status`
* `depends_on`
* `blocks`
* `source_chat`
* `phase`

---

## 4. 링크 규칙

* `docs/README.md`는 전체 허브로만 사용한다.
* `discussion-timeline.md`는 모든 `chat/request-answerNN.md`를 빠짐없이 링크해야 한다.
* 각 timeline 항목은 관련 task 또는 epic 문서로 이어져야 한다.
* 각 epic README는 하위 task 목록과 선행/후속 epic를 링크해야 한다.
* 각 subtask 문서는 부모 epic, 관련 source map 문서, 선행/후속 task를 링크해야 한다.
* `work-queue.md`는 설명을 길게 적지 않고 task 문서 링크 위주로 유지한다.

---

## 5. 큐 운영 규칙

* `할당`은 담당자가 아니라 우선순위와 선행관계를 뜻한다.
* 우선순위는 `P0`, `P1`, `P2`, `P3`를 사용한다.
* 상태는 `todo`, `ready`, `later`, `backlog`를 기본값으로 사용한다.
* `ready`는 선행 task가 모두 충족된 경우에만 쓴다.
* P0/P1 task는 dependency 순서대로 정렬한다.

---

## 6. 문서 길이 원칙

* 허브 문서는 짧고 링크 중심으로 쓴다.
* 방향 문서는 결정 내용을 충분히 설명한다.
* task 문서는 구현자가 바로 사용할 수 있을 정도로 구체적으로 쓴다.
* 동일한 설명을 여러 문서에 길게 반복하지 않는다.

---

## 7. 변경 원칙

* 클라이언트 확정 방향이 바뀌면 먼저 `00_source_map/client-decisions.md`를 갱신한다.
* 이후 `10_direction/` 문서를 갱신한다.
* 마지막으로 `20_queue/`와 `30_tasks/`를 갱신한다.

즉, 변경 순서는 항상
`결정 -> 방향 -> 큐 -> task`
순으로 맞춘다.

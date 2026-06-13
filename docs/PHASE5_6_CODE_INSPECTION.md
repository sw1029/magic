# Phase 5·6 코드 레벨 검수 보고서

검수일: 2026-06-12 / 기준 커밋: main `f868754`
검수 방식: 코드·테스트 대조 (빌드 플레이 검수 아님 — 사람 검증 항목은 별도 표기)
검수 도구: EditMode 105/105, PlayMode 47/47 통과 (Unity 6000.3.14f1 batchmode, 2026-06-12 로컬)

이 문서는 `docs/FINAL_COMPLETION_PLAN.md` Phase 5·6 체크리스트를 현재 main 구현과 대조한 결과다. 체크박스를 임의로 갱신하지 않고, 발견 사항을 먼저 보고한다.

---

## 핵심 발견: 스펙·계획서·구현의 3자 불일치 (설계 결정 필요)

**구현된 2~4층은 계획서 Phase 5 체크리스트(= `GAME_DESIGN.md` §9)와 다른 디자인이다.**
커스텀 도형(two-track personalization) 시스템 머지(PR #115~#117 계열) 과정에서 층 디자인이 피벗됐으나, `GAME_DESIGN.md` §9와 `FINAL_COMPLETION_PLAN.md` Phase 5 체크리스트는 구버전 그대로다.

| 층 | §9 / 계획서 기준 | 실제 구현 (`ExamGameController.BuildFloorDefinitions`, 8240행~) |
| --- | --- | --- |
| 1층 | 인덱스 룬 5 (불씨·물웅덩이·바람개비·돌기둥·마른 덩굴) | **일치** — 동일 5종 base 목표 (8254~8258행) |
| 2층 | 깨진 룬 6개 ↔ overlay 6종 1:1, 4개 이상 복구 | **다름** — "반응층": 커스텀 base 목표 5종 (불꽃 직선·물 보호막·바람 화살표·사각 방벽·생명 연결선, 8272~8276행). overlay 복구 퍼즐 아님 |
| 3층 | 해결 루트 4종 (`earth+steel_brace` 등 base+overlay 조합) | **유사 구조, 다른 내용** — "건널목층": 커스텀 스펠 4종 (강물 얼리기·구멍 메우기·덩굴 다리·바람 발판, 8290~8293행). 루트 4개라는 밀도는 충족 |
| 4층 | 균열 3개(진행형) + 번개 회로 1개 | **다름** — "타격층": 훈련 표적 4종 (얼음 제압·번개 타격·정화 수막·사각 방벽, 8307~8310행). 진행형 균열·회로 없음 |
| 5층 | 슬롯 6종 (정화/안정/연결/집중/흐름/절단) | **일치** — 동일 6슬롯 (8324~8329행) |

**영향받는 문서 항목:**

- DoD "5개 층이 §9의 퍼즐 밀도를 만족한다 (…2층 깨진 룬 6… 4층 균열 3+회로 1…)" — `[x]`로 표시돼 있으나 괄호 안 내용이 현재 구현과 불일치.
- P5-1(2층), P5-3(4층) 체크리스트 전체 — 구 디자인 기준이라 그대로는 검수 불가.
- DoD "README·로드맵·이 문서의 상태 표기가 실제 코드와 일치한다" `[x]` — §9 한정으로 재검토 필요.

**권장 처리:** 커스텀 도형 피벗은 연구 목표(개인화 인식)와 정합하는 의도적 변경으로 보인다. 코드를 §9로 되돌리기보다 ① `GAME_DESIGN.md` §9를 as-built로 개정하고 ② Phase 5 체크리스트를 신 디자인 기준으로 다시 쓰는 쪽을 권장. **단, 이는 A/B 공동의 설계 결정 사항.**

---

## P5-4. 5층 성좌심 검수 (구현과 스펙이 일치하는 층)

| # | 항목 | 판정 | 증거 |
| --- | --- | --- | --- |
| 1 | 슬롯 6종이 상태 요구로 동작 | **부분** | 6슬롯 구현 ✓. 단 만족 방법: 안정=Combo(Earth+SteelBrace), 정화=Base(Water), 연결=Combo(Life+SoulDot), 절단=Overlay(VoidCut), 집중=Overlay(SoulDot), 흐름=Base(Wind) — 각 1경로 중심. "만족 방법 최소 2종" 요건은 base 슬롯(표준/커스텀 양쪽 인정) 외에는 미충족으로 보임 |
| 2 | 슬롯 라벨 흐릿→또렷 + `goal_satisfied` | 빌드 검증 필요 | `goal_satisfied` SFX 재생 코드 존재. 라벨 선명도 전환은 코드상 직접 확인 못 함 |
| 3 | 4개 만족 시 마법진 미세 떨림 | **미구현** | Runtime 전체에 shake/tremor/떨림 코드 없음 (grep 0건) |
| 4 | 5번째 만족 연출 (모먼트 8) | **구현** | PlayMode `FinalFloorCompletionShowsFinalSealCelebrationBeforeReport`, `FinalFloorCanPassAtFiveOfSixGoals` 통과. `FinalFloorPassReportDelaySeconds = 4.8f` |
| 5 | 6번째 만족 진엔딩 (모먼트 8b) | **구현** | PlayMode `FinalFloorFiveGoalPassCanUpgradeToTrueEndingBeforeReport` 통과 — 5/6 후 6번째 시도 가능 검증됨 |
| 6 | `climax_seal` 슬롯별 레이어 BGM | **미구현** | `AudioDirector`에 레이어 로직 없음 (단일 PadLoop, 169~183행 부근). 외부 파일 로더만 존재 |

## P5-5. 1층 마감

| # | 항목 | 판정 | 증거 |
| --- | --- | --- | --- |
| 1 | 인덱스 룬 5개 base 반응 + 승강 룬 시각화 | **구현** | 5종 목표 정의 ✓ (8254~8258행). PlayMode `FirstFloorShowsGoalLabelsAndDrawingLocationGuidance`, `FirstFloorRevealsSequentialTransparentSymbolsAfterEachCapture`, `FirstFloorGhostTutorialAndDiscoveryCodexWork` 통과 |
| 2 | 진행 시간 5~8분 + 첫 30초 규칙 회귀 | **사람 필요** | 시간 측정은 빌드 플레이 필요 |

## P6-1. Exam Mentor 컨텍스트 소환

| # | 항목 | 판정 | 증거 |
| --- | --- | --- | --- |
| 1 | 트리거 6종 | **부분** | 5분 침묵(300s) ✓ (`firstFloorLongSilenceShown`, 5343행). 실패 escalator ✓ (PlayMode `FailedBaseCastsEscalateMagicNoteHints`). 1층 8초 침묵 ✓ — **정정(2026-06-12): `firstFloorGhostShown` 8초 트리거가 구현돼 있음** (ExamGameController 5331행, ghost gesture + 노트). 초기 검수의 grep 누락이었음 |
| 2 | 의도적 미등장 | 빌드 검증 필요 | 코드 구조상 모순 없음, 전수 확인은 플레이 필요 |
| 3 | 대사 풀 트리거당 3~5변주 ≈ 25줄 | **부분** | `MentorPresentationController`에 층별 멘토 프로필 5종(벽화 연구원·균열 감시자 등) 구현. 변주 수 전수 카운트는 미실시 |
| 4 | 등장 연출 (페이드인+머리 위 룬+`npc_appear`, 4층 가장자리 윈도우) | **부분** | `npc_appear` SFX ✓, 멘토 프레젠테이션 ✓. 4층 가장자리 윈도우는 4층 디자인 자체가 변경돼 스펙 재정의 필요 |
| 5 | EditMode 테스트 (쿨다운·1회성·풀 무작위) | **부분 (2026-06-12 갱신)** | `HintAssistanceTests` 추가 — escalator 단계 상승·캡·assisted 플래그·체크리스트·멘토 프로필을 EditMode로 검증. 단 "쿨다운·풀 무작위"는 **대사 풀/쿨다운 시스템 자체가 미구현**이라 테스트 대상이 없음 (스펙 §13 잔여 기능) |

## P6-2. 결정적 모먼트 검수

| # | 항목 | 판정 | 증거 |
| --- | --- | --- | --- |
| 1 | 모먼트 9개+8b 2채널 | **대부분 구현** | 모먼트 1·2(시전 성공 연출+SFX), 5(노트), 7·8·8b(5층, PlayMode 검증), 9(엔딩 리포트) 코드 확인. 모먼트 3(3층 줌인 0.4초)·4(4층 리셋 — hazard reset 코드는 존재: "균열 접촉 - 안전 지점 복귀", 5308~5312행)·6(ghost trace ✓ PlayMode)은 신 디자인 기준 재정의 필요 |
| 2 | 시각 임팩트 톤 통일 (`PolishConstants` 류) | **미구현** | `PolishConstants` 또는 동등 중앙화 클래스 없음 — 펄스/글로우 값이 호출부마다 산재 |

## P6-3. HUD·라벨 가독성

| # | 항목 | 판정 | 증거 |
| --- | --- | --- | --- |
| 1~4 | 배치·겹침·텍스트 크기·카메라 값 | 빌드 검증 필요 | HUD 패널 구조(좌상 HUD·우상 결과·토스트·퀘스트 스크롤·체력바) 구현 확인. §11 수치 일치 여부는 화면 검수 필요 |

## 종합

| 분류 | 항목 수 |
| --- | --- |
| 코드+테스트로 구현 확인 | 8 |
| 부분 구현 / 보강 필요 | 5 |
| 미구현 (작업 필요) | 4 — 5층 4슬롯 떨림, climax 레이어 BGM, 멘토 EditMode 테스트, PolishConstants 중앙화 |
| 스펙 재정의 필요 (2~4층 피벗) | P5-1·P5-3 전체, P5-2/P6-1-4/P6-2-1 일부 |
| 사람(빌드 플레이) 필요 | 진행 시간 측정, HUD 화면 검수, 라벨 연출, 미등장 전수 확인 |

### 다음 행동 제안 (우선순위순)

1. **[설계 결정]** §9·Phase 5 체크리스트를 as-built 디자인으로 개정할지 결정 (A/B 합의).
2. **[코드]** 멘토 트리거 EditMode 테스트 추가 (P6-1-5) — 쿨다운·1회성·풀 무작위.
3. **[코드]** 1층 8초 첫 침묵 트리거 구현 또는 스펙에서 제거.
4. **[코드]** 5층 4슬롯 떨림 + climax_seal 레이어 (또는 스펙 완화 결정).
5. **[정리]** 펄스/글로우/shake 상수 `PolishConstants` 중앙화 (P6-2-2).
6. **[사람]** 빌드 플레이 검수: 층별 진행 시간, HUD §11 수치, 미등장 전수.

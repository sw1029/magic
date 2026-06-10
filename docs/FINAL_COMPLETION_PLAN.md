# Final Completion Plan — Magic Exam Hall

작성 기준일: 2026-06-10
기준 커밋: `6d3d0ef` (main)
현재 로컬 완료 브랜치: `codex/finalize-game`

이 문서는 `Magic Exam Hall`을 **최종 완성**까지 끌고 가는 단일 실행 계획서다. 위에서 아래로 따라가면 게임이 완성되도록, Phase → 작업 → 세부 단계 → 완료 조건 → 검증 순으로 적는다.

다른 문서와의 관계:

- `docs/GAME_DESIGN.md` — 무엇을 만드는가 (스펙의 원본, §번호 인용은 모두 이 문서)
- `docs/PROJECT_ROADMAP.md` — 제품 관점 마일스톤 (이 계획이 우선하며, 어긋나면 로드맵을 갱신한다)
- `docs/ART_OVERHAUL_PLAN.md` — 아트 Phase 상세 (이 계획의 Phase 2가 참조)
- `docs/TEAM_DEVELOPMENT_PLAN.md` — 역할 경계와 PR 운영
- `docs/RELEASE_CHECKLIST.md` — 제출 직전 실행 체크리스트

## 사용 규칙

1. 작업은 Phase 순서대로 진행한다. 같은 Phase 안에서 트랙(A/B/C)이 다르면 병렬 가능하다.
2. 각 작업을 끝내면 이 문서의 체크박스를 같은 PR에서 체크한다.
3. Phase 끝의 **게이트 조건**을 만족하지 못하면 다음 Phase를 시작하지 않는다.
4. 결정 사항(D1~D5)은 명시된 시점 전에 반드시 결정하고 결과를 이 문서에 적는다.

### 역할 표기

| 표기 | 담당 | 범위 |
| --- | --- | --- |
| A | sw1029 | 입력·인식·1~3층 콘텐츠 |
| B | SilverSupplier | 게임 런타임·4~5층·아트(잠정) |
| C | TBD (합류 전까지 B가 대행) | 튜토리얼·피드백·문구·사운드 통합 |

---

## 0. 완성의 정의 (Definition of Final Done)

아래 전부를 만족하면 이 게임은 "최종 완성"이다. 모호한 항목은 없다.

### 플레이 기준

- [x] 타이틀 → 메인 메뉴 → 새 게임 → 1~5층 → 엔딩 리포트 → 타이틀 복귀가 키보드+마우스만으로 끊김 없이 된다.
- [ ] 외부 테스터(개발 미참여자) 1인이 설명 없이 60분 안에 통과 엔딩(5/6)에 도달한다.
- [x] 5개 층이 §9의 퍼즐 밀도를 만족한다 (1층 인덱스 룬 5, 2층 깨진 룬 6, 3층 해결 루트 4종, 4층 균열 3+회로 1, 5층 슬롯 6).
- [x] §14의 결정적 모먼트 9개+8b가 전부 시각+청각 중 2채널 이상으로 구현돼 있다.
- [x] §12의 SFX 12종 + BGM 2종이 모두 재생된다.
- [x] §15 접근성 옵션 6항목이 옵션 화면에서 동작한다.

### 기술 기준

- [ ] `npm run validate:docs`, `npm test`, `npm run build` 통과 (CI green).
- [ ] Unity EditMode/PlayMode 테스트 전부 통과, 그리고 **CI에서 자동 실행**된다.
- [x] Windows 빌드가 batchmode 한 줄 명령으로 생성되고 player smoke script가 통과한다.
- [ ] 새 clone + `npm ci` + Unity 프로젝트 열기만으로 위 전부가 재현된다.

### 연구·제출 기준

- [x] `docs/RESEARCH_PROTOCOL.md`, `docs/LOGGING_AND_PRIVACY.md`가 존재하고 실제 로그 스키마와 일치한다.
- [ ] 사용자 테스트 최소 5인 데이터(attempts/survey)가 수집·분석돼 있다.
- [ ] 발표 자료(5~8분: 문제→설계→구현→검증→결과)가 완성돼 있다.
- [x] `docs/asset-licenses/`에 모든 외부 자산의 라이선스가 보관되고 `docs/CREDITS.md`가 최신이다.
- [x] README·로드맵·이 문서의 상태 표기가 실제 코드와 일치한다.

### 2026-06-10 로컬 완료 상태

로컬에서 완료된 항목:

- 타이틀, 메뉴, 새 게임, 이어하기, 옵션, 일시정지, codex, 엔딩 복귀를 `GameBootController`로 통합했다.
- 3개 저장 슬롯, 층 완료 자동 저장, codex 수동 저장을 구현했다.
- 5층 synthetic full playthrough, 5/6 통과 엔딩, 6/6 진엔딩, 엔딩 리포트가 PlayMode 테스트로 검증된다.
- 절차 생성 SFX 12종과 BGM 2종을 `AudioDirector`에 넣고 옵션 볼륨과 연결했다.
- stroke 성공/실패 비주얼, 1층 ghost gesture, codex discovery, 접근성 옵션 6항목을 구현했다.
- Windows batch build wrapper, player smoke, Unity test workflow, 연구/로그/라이선스 문서를 추가했다.

로컬에서 대신 완료할 수 없는 항목:

- 외부 테스터 1인 무설명 60분 통과 검증과 사용자 테스트 5인 데이터 수집.
- 새 clone + `npm ci` 재현 검증과 Web `npm` 검증. 현재 이 작업 환경에는 npm이 없다.
- GitHub 원격 PR/issue 정리, GitHub secrets 등록, 실제 CI green 확인, GitHub Pages/Web 배포, release upload.
- 발표 자료 제작과 시연 영상 녹화.

---

## 결정 사항 (Decision Log)

구현 전에 결정해야 하는 항목. 결정되면 결과와 날짜를 적는다.

| ID | 질문 | 마감 시점 | 권장 | 결정 |
| --- | --- | --- | --- | --- |
| D1 | 최종 게임명 (`Magic Exam Hall` 유지 여부) | Phase 1 타이틀 화면 착수 전 | `Magic Exam Hall` 유지, 기술명 `Magic Recognizer` 병기 | `Magic Exam Hall` 유지 (2026-06-10) |
| D2 | 아트 팔레트·캐릭터 해상도 | Phase 2 자산 제작 착수 전 | AAP-64 / 48×64 / 타일 32×32 / PPU 32 | 현재 로컬 빌드는 절차 생성/32px fallback 유지, 외부 PNG 아트는 별도 Phase로 보류 |
| D3 | 입력 버퍼 값 (0.6/0.8/1.0초) | Phase 4 (playtest 결과로) | playtest A/B 후 확정 | 외부 playtest 전까지 `WorldDrawingController.DefaultBufferSeconds` 유지 |
| D4 | Web demo 배포 여부·경로 | Phase 7 착수 전 | GitHub Pages 정적 배포 | 로컬 Unity 빌드 우선, Web 배포는 원격/릴리스 단계에서 결정 |
| D5 | draft PR #102/#103 처리 | Phase 0 | #80 작업으로 흡수 후 close | 원격 GitHub 권한/네트워크 필요, 로컬 완료 브랜치에서는 차단 항목으로 기록 |

---

## Phase 개요와 의존성

```
Phase 0  정리 스프린트 (1주) ──────────────┐
   │                                       │
Phase 1  M1 vertical slice (2~3주)         │
   │                                       ▼
   ├──────────────► Phase 2  아트 파이프라인 (병렬, 3~4주)
   ▼
Phase 3  사용자 테스트 1차 (1.5주)
   ▼
Phase 4  인식·조작감 튜닝 (1~2주)
   ▼
Phase 5  층별 콘텐츠 확장 (3~4주)  ◄── Phase 2 자산 합류
   ▼
Phase 6  NPC·모먼트·폴리시 (2주)
   ▼
Phase 7  접근성·리포트·사용자 테스트 2차 (1.5주)
   ▼
Phase 8  릴리스·제출 패키지 (1~2주)
```

총 예상: **13~16주** (3인 병렬 기준). 1인이 줄면 Phase 2와 5가 직렬화되어 +4주.

---

## Phase 0 — 정리 스프린트 (1주, 전원)

목적: 쌓인 PR·이슈·문서 부채를 청산해서 이후 모든 Phase가 깨끗한 main 위에서 출발하게 한다. **이 Phase를 건너뛰면 이후 모든 작업이 충돌 비용을 낸다.**

### P0-1. 열린 PR 교통정리 (담당 B, A 리뷰)

현재 열린 PR 5개를 1주 안에 전부 머지 또는 close한다. 순서가 중요하다.

1. [ ] [#115 pixel art phase runtime polish](https://github.com/sw1029/magic/pull/115) (100파일, URP 포함) — 가장 먼저. 머지 전 확인:
   - `Packages/manifest.json`에 `com.unity.render-pipelines.universal` 추가됐는지
   - EditMode/PlayMode 테스트 통과 로그가 PR에 첨부됐는지
   - Windows 빌드 성공 로그 첨부됐는지 (URP 마이그레이션은 빌드 깨짐 위험이 가장 큼)
2. [ ] [#107 커스텀 도형 authoring](https://github.com/sw1029/magic/pull/107) (40파일) — #115 머지 후 rebase → 테스트 → 머지.
3. [ ] [#108 커스텀 주문 효과·3~4층 구성](https://github.com/sw1029/magic/pull/108) (91파일) — #107 머지 후 rebase. **Phase 5의 3~4층 작업과 겹치므로, 이 PR에 든 3~4층 구성을 기준으로 Phase 5 작업량을 재산정한다.**
4. [ ] draft [#102](https://github.com/sw1029/magic/pull/102)/[#103](https://github.com/sw1029/magic/pull/103) — D5 결정에 따라 close하고 유효한 커밋만 cherry-pick 목록으로 #80에 코멘트.

완료 조건: 열린 PR 0개 (이후 새 PR은 2주 이상 열어두지 않는다는 규칙 합의 포함).

### P0-2. 이슈 정리 (담당 B)

[#93](https://github.com/sw1029/magic/issues/93)의 실행. 다음을 수행한다.

- [ ] 문서가 이미 존재해 stale한 이슈 close: [#41](https://github.com/sw1029/magic/issues/41)(RECOGNITION_CONTRACT 존재), [#52](https://github.com/sw1029/magic/issues/52)(RELEASE_CHECKLIST 존재).
- [ ] 구현 완료로 보이는 이슈 검증 후 close: [#30](https://github.com/sw1029/magic/issues/30)/[#31](https://github.com/sw1029/magic/issues/31)(README·로드맵 동기화분), [#49](https://github.com/sw1029/magic/issues/49)(엔딩 연출 — 5/6, 6/6 분기 구현됨, 잔여분은 #79로 통합).
- [ ] legacy epic [#21](https://github.com/sw1029/magic/issues/21)을 close하고 잔여 항목을 [#73](https://github.com/sw1029/magic/issues/73)으로 이관.
- [ ] 남은 모든 이슈에 이 문서의 Phase 라벨(`phase:1`~`phase:8`)을 단다.

### P0-3. 로드맵 상태 동기화 (담당 B)

`docs/PROJECT_ROADMAP.md`의 상태 표가 2026-05-14 기준이라 코드와 어긋난다. 다음을 실제 코드 기준으로 수정:

- [x] Epic B3 "Unity overlay recognizer 미구현" → 구현됨 (`OverlayRecognizer`, 테스트 존재).
- [x] M4 "엔딩 요약 미구현" → 구현됨 (`EndingReport.BuildText`, 진엔딩 분기 포함).
- [x] M2 anchor zone, final spell feedback 상태 재확인 후 갱신.
- [x] 표 전체를 한 번 훑어 구현됨/부분/미구현 재표기. 이후로는 §17 유지보수 규칙(기능 PR과 같은 PR에서 갱신)을 강제한다.

### P0-4. 개발 환경 정비 (담당 각자)

- [ ] 로컬에 Node 20+ 설치, PATH 등록 (`npm test` 로컬 실행 가능 상태로).
- [ ] Unity 6000.3.14f1 + 라이선스 전원 확인.
- [ ] `npm ci && npm test && npm run build` 전원 통과 확인.

### 게이트 0 → 1

- 열린 PR 0개, main CI green, Unity EditMode/PlayMode 로컬 통과.
- D5 결정 완료.

---

## Phase 1 — M1 Vertical Slice: "게임"으로 만들기 (2~3주)

목적: GAME_DESIGN §17 M1의 완성. **타이틀에서 시작해 엔딩 리포트로 끝나는 한 편의 짧은 경험**. 여기서 막히면 이후 Phase를 시작하지 않는다 (디자인 문서의 원칙 그대로).

현재 상태 요약: 5층 루프·인식·overlay·엔딩 분기·로그는 이미 동작한다. 없는 것은 **게임의 껍데기(타이틀·메뉴·옵션·저장)와 살(사운드·선 비주얼·codex·첫 30초)**이다.

### P1-1. 게임 시작 흐름 — 타이틀·메뉴·트랜지션 (담당 B) — 이슈 [#86](https://github.com/sw1029/magic/issues/86)

스펙: §4 게임 시작 흐름. D1(게임명) 결정 후 착수.

세부 단계:

1. [x] `GameBootController` 신규 작성 (`Scripts/Runtime/`). 별도 scene을 만들지 말고 기존 `MagicExamHall.unity` 안에서 상태 머신으로 처리한다 (`Title → Menu → FadeOut → Gameplay → Ending → Title`). scene 추가는 빌드 설정·scene builder·smoke test를 모두 건드리므로 피한다.
2. [x] 타이틀 화면: 로고 텍스트 + 부제 + "아무 키나 눌러 시작" + 마법탑 실루엣 배경(절차 생성 또는 단색 그라데이션로 시작, Phase 2에서 교체). `ambient_tower` BGM 시작 지점.
3. [x] 메인 메뉴 4항목: 새 게임 / 이어하기(저장 없으면 회색) / 옵션 / 종료. 마우스 + 방향키/Enter 둘 다 지원.
4. [x] 옵션 패널 v1: BGM 볼륨, SFX 볼륨 슬라이더 2개만 (`PlayerPrefs` 저장). 접근성 항목은 Phase 7에서 같은 패널에 추가하므로 패널 구조는 리스트형으로.
5. [x] 새 게임 트랜지션: 선택 → 0.6초 페이드아웃 → 1층 spawn → 0.8초 페이드인 → 멘토 환영 트리거 호출.
6. [x] 엔딩 리포트 닫기 → 1초 페이드 → 타이틀 복귀. 진행 완료 슬롯은 "완료" 표시.
7. [x] 저장 v1 (최소): 층 완료 시점 자동 저장 1슬롯 (`Application.persistentDataPath/save.json` — 층 번호·완료 목표·노트 줄만). 슬롯 3개와 수동 저장까지 함께 완료.
8. [x] ESC = 일시정지 메뉴 (계속 / 옵션 / 타이틀로). 현재 ESC 동작과 충돌 확인.

완료 조건:

- 실행 → 타이틀 → 새 게임 → 1층까지 마우스만으로 도달.
- 엔딩 후 타이틀 복귀, 이어하기로 마지막 완료 층 다음에서 재개.
- PlayMode 테스트: boot 상태 머신 전이 검증 1본 추가.

### P1-2. 핵심 사운드 1차 (담당 C, 통합 B) — 이슈 [#74](https://github.com/sw1029/magic/issues/74)

스펙: §12. M1 범위는 핵심 7종 + BGM 1종이지만, 소싱은 한 번에 하는 게 싸므로 12종을 모두 수집하고 통합만 7종 먼저 한다.

세부 단계:

1. [x] 소싱 또는 내부 절차 생성: Kenney Audio(CC0), freesound(CC0 필터) 등 외부 파일을 쓰지 않고 `AudioDirector`에서 아래 목록을 절차 생성. 라이선스를 `docs/asset-licenses/audio.md`에 기록, `docs/CREDITS.md` 갱신.
   - 성공 3종: `cast_base_success`(0.4s 차임), `cast_overlay_success`(0.3s), `cast_final_effect`(0.8s)
   - 실패 3종: `cast_invalid`(0.2s), `cast_incomplete`(0.3s), `cast_dependency_missing`(0.25s 잠긴 클릭)
   - 기타 5종: `goal_satisfied`(0.6s 상승), `floor_complete`(1.2s stinger), `note_unlock`(0.3s 책장), `hazard_reset`(0.5s 진동), `npc_appear`(0.4s 글로우)
   - BGM: `ambient_tower`(8~10분 루프), `climax_seal`(5층용)
2. [x] 배치: 절차 생성 클립이므로 파일 배치 없이 `AudioDirector`가 런타임에 생성·보관.
3. [x] `AudioDirector` 신규 작성: SFX 재생 단일 진입점. family pitch 변주(±2~3 semitone, `AudioSource.pitch`), low-quality 성공은 동일 SFX -6dB 재생 규칙 구현.
4. [x] 통합 지점 연결 (M1 7종 우선):
   - base 성공/overlay 성공 → `SpellCastingService` 결과 처리부
   - invalid/incomplete/dependency → `ExamGameController`의 실패 분기 (escalator 1단계 SFX 분리 — §7)
   - `goal_satisfied`/`floor_complete` → `FloorGoalSystem` 완료 콜백
   - `note_unlock` → `ShowMagicNote` 호출부
   - `npc_appear` → `MentorPresentationController` 등장부
   - `ambient_tower` → 타이틀+1~3층, 4층은 BGM off(§12), 5층 `climax_seal`
5. [x] 옵션 볼륨 슬라이더와 연결 (BGM/SFX 채널 분리).

완료 조건: 성공·실패 3종이 눈 감고도 구분된다(팀원 청취 확인). 빌드에 포함되고 라이선스 문서 존재.

### P1-3. 그리는 선 비주얼 (담당 A) — 이슈 [#87](https://github.com/sw1029/magic/issues/87)

스펙: §17 M1 — 인식 전 흰빛, 인식 후 family 색 전환, 잔상 0.5초 페이드.

1. [x] `WorldDrawingController`의 stroke 렌더링에 상태 추가: drawing(흰빛) → recognized(family accent 색으로 0.2초 lerp) → fade(0.5초).
2. [x] invalid 판정 시: 색 전환 없이 흐릿한 파문 1초 (§7 맵 피드백 표).
3. [ ] URP 머지(P0-1) 후라면 stroke에 약한 glow (2D Light 또는 emissive 머티리얼).

완료 조건: 성공/실패가 소리 없이 선 색만으로도 구분된다. PlayMode smoke에 회귀 없음.

### P1-4. 1층 첫 30초 튜토리얼 + ghost gesture (담당 C, 인식 협조 A) — 이슈 [#75](https://github.com/sw1029/magic/issues/75)

스펙: §9 1층 — NPC 트리거 3자리(입장/8초 침묵/5분 침묵) + 8초 ghost gesture 1회.

1. [x] 입장 직후 멘토 환영 1줄 ("바닥은 너의 종이다." 류 — 변주 3개).
2. [x] 입장 후 8초간 시전 시도가 없으면: 멘토 1줄("우클릭을 누른 채 바닥에 선을 그어 보라") + **가장 가까운 인덱스 룬 위에 0.8초 ghost gesture 1회 자동 재생** (흐릿한 흰빛으로 점→선→닫힘). 기존 `HintAssistance`의 ghost trace 렌더러 재사용.
3. [x] 5분 무발동 시 같은 안내 다른 표현 1회 (ghost gesture는 재재생 안 함).
4. [x] ghost gesture는 게임 전체에서 1층 1회만 (저장 데이터에 seen 플래그).

완료 조건: 게임을 처음 보는 1인 테스트에서 60초 안에 첫 시전 발생.

### P1-5. 마법 노트 codex 화면 (담당 C) — 이슈 [#84](https://github.com/sw1029/magic/issues/84)

스펙: §13.1. 현재 `MagicNote`는 HUD 한 줄짜리 — 이를 기록 저장소로 확장한다.

1. [x] `MagicNote`를 라인 누적 구조로 확장: `(category: 대사|층노트|발견, floor, text, timestamp)`.
2. [x] 기존 `ShowMagicNote` 호출 전부 분류 태깅 (NPC 대사 → 대사, 층 완료 → 층노트, 자동 관찰문 → 발견).
3. [x] 우상단 노트 버튼: gameplay 중 표시, 새 줄 추가 시 0.4초 펄스 + `note_unlock`.
4. [x] `Tab` 또는 아이콘 클릭 → soft pause + codex 패널: 탭 3개 (대사 / 층별 노트 / 발견).
5. [x] 발견 탭: base 5 + overlay 6 그리드. 첫 성공 항목은 또렷, 미발견은 실루엣 (스프라이트는 기존 룬 PNG 재사용).
6. [x] 엔딩 리포트의 "대표 관찰문 3줄 자동 발췌" 연결: 발견 카테고리에서 최신·고유 3줄 (§16). `EndingReport.BuildText`에 주입.

완료 조건: 1층 완료 시점에 codex에 6줄 이상 쌓이고, Tab으로 열람·닫기 가능, 엔딩 리포트에 3줄 발췌가 표시된다.

### P1-6. M1 통합 검수 (전원) — 이슈 [#85](https://github.com/sw1029/magic/issues/85)

1. [ ] 1층 + 5층 경로로 15분 완주 시나리오를 팀원 3인이 각자 1회 완주 (2~4층은 이미 구현돼 있으므로 M1 정의의 "축약"은 건너뛰지 않고 그대로 플레이하되 15분 타임박스만 확인).
2. [x] PlayMode smoke 테스트에 boot→title→gameplay 전이 포함.
3. [x] Windows 빌드 + `scripts/smoke-magic-exam-hall-player.ps1` 통과.
4. [ ] `docs/RELEASE_CHECKLIST.md` §1~3을 한 번 완주 (제출용 아님, 리허설). Web 검증은 npm 환경 필요.

### 게이트 1 → 2/3

- 외부인 아닌 팀원 기준, 타이틀부터 엔딩 리포트까지 무설명 완주 가능.
- SFX 7종+BGM 재생, 선 비주얼, ghost gesture, codex 동작.
- CI green + Unity 테스트 통과 + 빌드 smoke 통과.

---

## Phase 2 — 아트 파이프라인 (3~4주, Phase 1·3과 병렬, 담당 B 또는 아트 합류자)

목적: `docs/ART_OVERHAUL_PLAN.md`의 실행. 절차 생성 그림 → PNG 자산 + URP 렌더링. 작업 상세는 ART_OVERHAUL_PLAN이 원본이고 여기는 순서·게이트만 정한다.

전제: P0-1에서 #115(URP) 머지 완료. D2(팔레트·해상도) 결정.

| 순서 | 작업 | work-queue ID | 기간 | 비고 |
| --- | --- | --- | --- | --- |
| 2-1 | [ ] URP+2D Light+Pixel Perfect 검증 마감 | T30-01 | 0.5주 | #115 머지 후 잔여 정리. 양초·룬 Point Light 2D, 384×216/PPU32, 카메라 jitter 없음 확인 |
| 2-2 | [ ] 플레이어 PNG 4방향 + idle/walk/cast 애니 | T30-02 | 1주 | Mystic Woods(CC-BY) 또는 Penzilla 소싱 권장. `PixelArtFactory`→`SpriteSet` 확장, frame ticker. **체감 변화 최대 — 최우선** |
| 2-3 | [ ] 룬 손그림 5종 + charge 프레임 | T30-05 | 1주 | 64×64, 자체 제작(게임 정체성). 40% 축소 식별 테스트 |
| 2-4 | [ ] 오토타일 (47-blob 또는 16-Wang) | T30-03 | 1주 | `com.unity.2d.tilemap.extras`, FloorTile/WallTrim blob sheet 교체 |
| 2-5 | [ ] Prop 변이 (책장3·양초3·가드4·코너4·기둥2) | T30-04 | 지속 | hash 기반 결정적 변이 선택 |
| 2-6 | [ ] Post FX (Bloom 0.5/0.9, Vignette 0.25, CA 옵션) | T30-06 | 0.5주 | HUD 가독성 확인. CA는 기본 OFF 옵션 |
| 2-7 | [ ] 타이틀 화면 배경 아트 교체 | — | 0.5주 | P1-1의 placeholder 교체 |

운영 규칙:

- 각 단계는 독립 PR. 외부 자산 PR에는 반드시 `docs/asset-licenses/` 파일과 CREDITS 갱신 포함.
- 매 단계 후 EditMode/PlayMode + Windows 빌드 확인 (특히 2-1, 2-4).
- `docs/20_queue/work-queue.md` 상태를 같은 PR에서 갱신 (`npm run validate:docs` 통과 유지).
- 이미지 자산 전체 목록은 [#98](https://github.com/sw1029/magic/issues/98)에서 관리 — 2-2 착수 전에 목록을 확정한다.

### 게이트 2 (Phase 5 합류 조건)

- 2-1~2-3 완료 (플레이어·룬이 PNG로 교체됨). 2-4 이후는 Phase 5와 병렬 계속.

---

## Phase 3 — 사용자 테스트 1차 (1.5주: 준비 0.5 + 실행·정리 1)

목적: M1 산출물을 5인에게 테스트해 **버퍼 값·인식 정확도·NPC 빈도·엔딩 만족도**의 실측 데이터를 얻는다. 이슈 [#76](https://github.com/sw1029/magic/issues/76), [#44](https://github.com/sw1029/magic/issues/44)~[#47](https://github.com/sw1029/magic/issues/47), [#33](https://github.com/sw1029/magic/issues/33)/[#34](https://github.com/sw1029/magic/issues/34).

### P3-1. 연구 문서 작성 (담당 B, 검토 전원)

1. [x] `docs/RESEARCH_PROTOCOL.md` — [#44](https://github.com/sw1029/magic/issues/44)
   - 참가자 과제: 타이틀부터 통과 엔딩까지 무설명 완주 (60분 상한)
   - 측정 항목: 첫 시전까지 시간, 층별 완료 시간, family별 첫 시도 성공률, 재시도 수, assist level 도달률, 실패 피드백 이해도(인터뷰), 공정성·몰입감(설문)
   - **입력 버퍼 A/B**: 참가자를 0.6/0.8/1.0초 그룹으로 나눠 D3 결정 데이터 수집
   - 진행 순서 스크립트, 동의 절차, 중단 기준
2. [x] `docs/LOGGING_AND_PRIVACY.md` — [#45](https://github.com/sw1029/magic/issues/45)
   - attempts/survey 스키마 명세 (현 `ExamLogging` 출력과 1:1 대조표)
   - raw stroke 미저장 원칙, 익명화 절차, 보관·공유 범위
3. [x] 설문 문항·척도 확정 — [#46](https://github.com/sw1029/magic/issues/46): 명확성/공정성/피드백 도움/조작감/몰입감 5점 척도 + 자유 의견. Unity 자동 설문 로그와 필드명 일치 확인.
4. [x] 분석 템플릿 — [#47](https://github.com/sw1029/magic/issues/47): 관찰표 (층별 막힘 지점, 발화 기록) + 로그 집계 시트.

### P3-2. 테스트 실행 (전원)

1. [ ] 빌드 고정: 테스트용 태그 (`playtest-1`), Windows 빌드 산출.
2. [ ] 파일럿 1인 (팀 외부, 비공식) → 프로토콜 결함 수정.
3. [ ] 본 테스트 5인. 1인당 60~75분 (플레이 + 사후 인터뷰 15분).
4. [ ] 로그·설문·관찰표 취합, 익명화.

### P3-3. 분석과 백로그 반영 (담당 A 인식 / B 게임)

1. [ ] family·overlay별 오인식 상위 3개 도출 → Phase 4 튜닝 목록.
2. [ ] 버퍼 값 결정 (D3 기록).
3. [ ] 막힘 지점 상위 3개 → Phase 5 콘텐츠 작업에 반영.
4. [ ] NPC 빈도·톤 피드백 → Phase 6 대사 풀 설계에 반영.
5. [ ] 결과 요약을 `outputs/playtest-1/` 에 저장하고 이슈 [#34](https://github.com/sw1029/magic/issues/34) close.

### 게이트 3 → 4

- 5인 데이터 수집 완료, 분석 요약 존재, D3 결정.

---

## Phase 4 — 인식·조작감 튜닝 (1~2주, 담당 A 중심)

목적: playtest 데이터로 인식기를 손본다. GAME_DESIGN M3.

1. [ ] 오인식 상위 케이스 보정: 해당 family 판별 특징 가중치 조정 + 회귀 fixture 추가 (`GestureRecognizerTests`에 실제 playtest 실패 stroke를 synthetic으로 재현).
2. [ ] 버퍼 값 D3 반영 + seal 유지시간·overlay attach 반경 재점검 (§19 열린 질문의 잠정값 검증).
3. [ ] **user profile 보정 Unity 이식** — 이슈 [#80](https://github.com/sw1029/magic/issues/80):
   - Web `src/recognizer/user-profile.ts`의 정책(반복 입력 기반 adjusted quality, 판정 자체는 뒤집지 않음)을 C#으로 이식
   - draft #102/#103에서 살릴 커밋이 있으면 cherry-pick (P0-1의 D5 목록)
   - `RECOGNITION_CONTRACT.md`에 profile 필드 대응 추가
   - EditMode 테스트: 동일 입력 반복 시 adjusted quality 상승, invalid는 여전히 invalid
4. [ ] Web/Unity 로그 스키마 대응표 마감 — 이슈 [#15](https://github.com/sw1029/magic/issues/15): `RECOGNITION_CONTRACT.md`에 표 완성, 불일치 필드 수정.
5. [ ] (선택) anchor zone guide visual — overlay를 어디에 그려야 하는지 점선 링 표시 (로드맵 M2 잔여).

완료 조건: playtest에서 나온 오인식 케이스가 회귀 테스트로 고정되고 통과. profile 이식 후에도 reject 케이스 전부 유지.

---

## Phase 5 — 층별 콘텐츠 확장 (3~4주, A: 1~3층 / B: 4~5층 병렬)

목적: 각 층을 §9의 퍼즐 밀도와 시나리오대로 완성한다. **PR #108에 이미 든 3~4층 구성을 먼저 흡수했으므로(P0-1), 아래는 그 위에서의 잔여분이다 — 착수 전 각 층 현황을 §9 표와 대조해 체크리스트를 갱신할 것.**

### P5-1. 2층 반응층 (담당 A) — 이슈 [#89](https://github.com/sw1029/magic/issues/89)

§9 2층 스펙 기준:

1. [ ] 깨진 룬 6개가 overlay 6종에 1:1 대응, 4개 이상 복구 시 통과.
2. [ ] 벽화에 overlay 6종 실루엣 단서 배치 (Phase 2-5 자산 활용).
3. [ ] 첫 overlay 부착 모먼트: 룬 가장자리 마크 + 라벨 `불꽃 + 집중` 형식 전환.
4. [ ] `martial_axis` 의존성 차단 시 `cast_dependency_missing` SFX + "축이 설 자리가 없음" 라벨 1초 (NPC 없음).
5. [ ] 같은 룬 2회 실패 시 5초 진동(비언어 단서), 3회째부터 NPC escalator.
6. [ ] 진행 시간 8~12분 검증.

### P5-2. 3층 흐름층 (담당 A) — 이슈 [#90](https://github.com/sw1029/magic/issues/90)

1. [ ] 의도된 해결 루트 4종 구현: `earth+steel_brace`(지지대), `wind+martial_axis`(발판 정렬), `life+soul_dot`(덩굴), `water+ice_bar`(녹는 얼음 다리 — 시간 제한).
2. [ ] 변주 2종: `earth` 2회 연결, `electric_fork` 회로 잇기 (성공 시 멘토 보너스 대사).
3. [ ] 근접 시전 시 빈 공간에 효과 프리뷰 힌트 (예: earth 시 돌 부스러기).
4. [ ] 첫 경로 통과 시 카메라 0.4초 줌인 (§14 모먼트 3).
5. [ ] 경로 시간 약화 시 같은 주문 덧그리기로 보수 가능, 부분 진행 유지.

### P5-3. 4층 균열층 (담당 B) — 이슈 [#91](https://github.com/sw1029/magic/issues/91)

1. [ ] 균열 3개(진행형) + 번개 회로 1개. 해법: `earth+steel_brace` 봉합 / `ice_bar` 동결 / `void_cut` 차단 / `wind` 발판 밀기(보조).
2. [ ] 안전 지점 항상 1개 이상 시야 내 — 카메라 약한 끌림 (lerp 0.05, §11 카메라 표).
3. [ ] 위험 접촉: 붉은 비네트 + `hazard_reset` + 미세 shake ±2px 0.15초 + 안전 지점 즉시 이동. 체력·죽음 없음.
4. [ ] BGM off, 환경음(번개 잡음·균열음)만 (§12).
5. [ ] 같은 균열 5회 실패 시 멘토가 가장자리 작은 윈도우로 등장 (그리는 손 안 가림).
6. [ ] 첫 리셋 시 노트 자동 관찰문 "탑은 떨어진 사람을 다시 돌려보낸다".

### P5-4. 5층 성좌심 확장 (담당 B) — 이슈 [#92](https://github.com/sw1029/magic/issues/92), [#79](https://github.com/sw1029/magic/issues/79)

5/6·6/6 엔딩 분기와 리포트는 구현돼 있다. 잔여는 §9 5층 표의 슬롯 의미론과 클라이맥스 연출:

1. [ ] 슬롯 6종(정화/안정/연결/집중/흐름/절단)이 각각 **상태 요구**로 동작하고 만족 방법이 최소 2종인지 검증·보강 (§9 표의 만족 예시 전부 동작).
2. [ ] 슬롯 라벨: 평소 흐릿 → 만족 시 또렷 + 조각 점등 + `goal_satisfied`.
3. [ ] 4개 만족 시 마법진 미세 떨림 ("거의 다 됐다" 단서).
4. [ ] 5번째 만족: 화면 천천히 밝아짐 + `climax_seal` 정점 + 카메라 마법진 중심 이동·정지 + 멘토 마지막 인사 (§14 모먼트 8).
5. [ ] 6번째 만족(진엔딩): 한 박 더 밝아짐 + BGM 한 단계 + 멘토 추가 1줄 (모먼트 8b). 5개 만족 후에도 마법진이 잠시 유지돼 6번째 시도 가능.
6. [ ] `climax_seal` BGM이 슬롯 만족마다 음이 추가되는 레이어 구조 (최소: 만족 수에 따라 트랙 레이어 on).

### P5-5. 1층 마감 (담당 A)

1. [ ] 인덱스 룬 5개(불씨·물웅덩이·바람개비·돌기둥·마른 덩굴)가 각 base에 명확히 반응하고 승강 룬이 차오르는 시각화.
2. [ ] 진행 시간 5~8분 검증, 첫 30초 규칙(P1-4) 회귀 확인.

### 게이트 5 → 6

- 5개 층 전부 §9 퍼즐 밀도 표 충족, 팀원 교차 플레이로 층당 목표 시간 ±50% 안에 완료.
- PlayMode 테스트에 층별 핵심 경로 1개씩 추가 (synthetic stroke로 자동 완주).

---

## Phase 6 — NPC 대사 풀·결정적 모먼트·게임 느낌 (2주, C 중심 + B)

### P6-1. Exam Mentor 컨텍스트 소환 완성 (담당 C) — 이슈 [#78](https://github.com/sw1029/magic/issues/78)

스펙: §13. `MentorPresentationController`(표시)는 머지됨 — 트리거 모델과 대사 풀이 잔여.

1. [ ] 트리거 6종 구현·검증: 1층 입장 / 1층 8초·5분 침묵 / 2회 실패(60초 쿨다운) / 3회+ 실패(ghost trace 동반) / 층 완료 / 5층 5·6번째 슬롯.
2. [ ] 의도적 미등장 확인: base 첫 발동, 첫 overlay, 의존성 차단, 1회 실패, 5층 1~4슬롯 — 환경 채널만.
3. [ ] 대사 풀을 ScriptableObject로 분리: 트리거당 3~5변주 × 6트리거 ≈ 25줄 작성. 원칙: 1~3줄, 정답 금지, 행동 동사 포함, 12자 라벨 규칙과 별개.
4. [ ] 등장 연출: 페이드인 + 머리 위 룬 + `npc_appear`, 4층은 가장자리 윈도우. 발화는 노트 자동 기록.
5. [ ] EditMode 테스트: 쿨다운, 트리거 1회성, 풀 무작위 선택.

### P6-2. 결정적 모먼트 9개+8b 마감 패스 (담당 B) — 이슈 [#77](https://github.com/sw1029/magic/issues/77), [#83](https://github.com/sw1029/magic/issues/83)

§14 목록을 체크리스트로 검수 — 각 모먼트가 시각+청각 2채널 이상인지:

1. [ ] ① 첫 base 발동 ② 첫 overlay 부착 ③ 3층 첫 통과 ④ 4층 첫 리셋 ⑤ 노트 새 줄 ⑥ ghost trace 등장 ⑦ 5층 슬롯 1개 ⑧ 5번째 슬롯 ⑧b 6번째 슬롯 ⑨ 엔딩 리포트 열림.
2. [ ] 시각 임팩트 톤 통일: 펄스 길이·글로우 강도·shake 진폭을 한 곳(`PolishConstants` 류)에서 관리.

### P6-3. HUD·라벨 가독성 정비 (담당 C) — 이슈 [#81](https://github.com/sw1029/magic/issues/81)

§11 기준:

1. [ ] 층 라벨·노트 아이콘 우상단 고정, HUD 메시지 하단 중앙, 동시 표시 3개 상한.
2. [ ] NPC 대사와 룬 라벨 겹침 방지 (대사 중 라벨 페이드).
3. [ ] 텍스트 16pt+, 외곽선, AA 대비. 룬 라벨 12자 한국어 규칙 전수 점검.
4. [ ] 카메라 §11 표 값 적용 확인 (이동 lerp 0.12, NPC 등장 시 고정 등).

### P6-4. 저장 완성 (담당 B)

1. [x] 슬롯 3개 + 수동 저장(codex 화면 하단 버튼) + 자동 저장(층 완료, 5층 첫 슬롯).
2. [x] 이어하기 메뉴에 슬롯별 진행 표시 (층, 시간, 완료 여부).

---

## Phase 7 — 접근성·리포트 폴리시·사용자 테스트 2차 (1.5주)

### P7-1. 접근성 옵션 (담당 C) — 이슈 [#82](https://github.com/sw1029/magic/issues/82)

§15 전 항목을 옵션 패널에 추가:

1. [x] 좌·우클릭 스왑 / 이동 키 프리셋 / 마우스 감도.
2. [x] 색약 보조: 성공 빛과 위험 피드백의 색 대비 강화.
3. [x] 텍스트 크기 1.0/1.25/1.5×.
4. [x] BGM·SFX 볼륨 (P1-1에서 완료 — 같은 패널 통합 확인).
5. [x] (검토) "관찰 모드": escalator 1단계 강화 시작 옵션.

### P7-2. 엔딩 리포트 폴리시 (담당 B)

§16 항목 전수 검수 (`EndingReport.BuildText` 보강):

1. [x] 최다 사용 base/overlay·발견 조합 수·품질 경향(단어)·최안정/최다실패 문양·노트 발췌 3줄·자기 평가 4문항이 모두 1화면에.
2. [x] 헤더 분기 ("당신은 이런 마법사였습니다" / "...완전히 기억한 마법사였습니다") — 구현돼 있으므로 문구 일치만 확인.
3. [x] 단정 문구("잘했어요") 금지 규칙 검수.

### P7-3. 사용자 테스트 2차 (전원)

1. [ ] 동일 프로토콜로 3~5인 (1차 참가자와 다른 사람). 전 층 + 접근성 옵션 사용 관찰.
2. [ ] 1차 대비 지표 비교 (첫 시도 성공률, 층 완료 시간, assist 사용률).
3. [ ] 발견된 P0/P1 결함만 수정하는 버그픽스 패스 (新기능 금지 — content freeze 시작).

### 게이트 7 → 8

- 외부 1인 무설명 60분 내 통과 엔딩 도달 (완성 정의의 핵심 항목).
- P0 결함 0, content freeze 선언.

---

## Phase 8 — 릴리스·제출 패키지 (1~2주, 전원)

### P8-1. Unity 테스트 CI 통합 (담당 B)

현재 CI는 web만 검증한다. Unity 회귀를 자동으로 잡는다.

1. [x] GameCI(`game-ci/unity-test-runner`) 또는 self-hosted runner로 EditMode 테스트를 PR CI에 추가. Unity 라이선스는 GitHub secret으로.
2. [ ] (여유 시) PlayMode + Windows 빌드를 주 1회 scheduled workflow로.
3. [ ] 실패 시 merge block 규칙 적용.

### P8-2. 빌드·배포 (담당 B)

1. [x] Windows 빌드 batchmode 명령을 `scripts/build-windows.ps1`로 고정 (버전 문자열 자동 삽입: git tag 기반).
2. [x] player smoke ([#53](https://github.com/sw1029/magic/issues/53)) 통과 확인 자동화.
3. [ ] D4가 yes면: `npm run build` 산출물 GitHub Pages 배포 workflow (web demo는 연구 보조 자료).
4. [ ] 릴리스 태그 `v1.0.0` + GitHub Release에 빌드 zip 첨부.

### P8-3. 문서·발표 마감 (담당 전원)

1. [ ] `docs/RELEASE_CHECKLIST.md` 전체를 위에서 아래로 실행 (Web 검증 → Unity 자동 검증 → 수동 5층 완주 → 로그·개인정보 → 제출 패키지).
2. [ ] README 최신화: 빠른 시작, 조작법, 빌드 다운로드 링크, 스크린샷 3장+. 빠른 시작과 조작법은 갱신됨, 다운로드 링크/스크린샷은 release 산출 후 추가.
3. [x] `PROJECT_ROADMAP.md` 상태 표 최종 동기화, 이 문서 체크박스 마감.
4. [ ] 발표 자료 5~8분: 문제(§1 빈자리) → 설계(문법·품질·escalator) → 구현(웹 검증→Unity 이식 구조) → 검증(playtest 1·2차 지표) → 결과(엔딩 리포트·로그). 시연 영상 2분 백업 녹화.
5. [ ] anonymized sample log 포함 여부 결정·반영 (로드맵 E5).
6. [ ] known issues를 Release 노트에 명시.

### 최종 게이트 — 완성 선언

- §0 "완성의 정의" 체크박스 전부 충족.
- 새 clone에서 README만 보고 빌드·실행 재현 성공 (팀원 아닌 1인 검증).

---

## 주차별 캘린더 (3인 기준, 2026-06-15 시작 가정)

| 주차 | A (입력·인식·1~3층) | B (런타임·4~5층·아트) | C (튜토리얼·피드백·사운드) |
| --- | --- | --- | --- |
| W1 | P0: PR 리뷰·환경 | P0: PR 머지·이슈·로드맵 | P0: 환경·D1 의견 |
| W2 | P1-3 선 비주얼 | P1-1 타이틀·메뉴·저장v1 | P1-2 사운드 소싱·라이선스 |
| W3 | P1-4 ghost gesture 협조 | P1-1 마감 + P2-1 URP 검증 | P1-2 통합 + P1-5 codex |
| W4 | P1-6 통합 검수 | P2-2 플레이어 애니 | P1-4·P1-5 마감, P3-1 문서 검토 |
| W5 | P3 테스트 진행·관찰 | P3 + P2-2 마감 | P3-1 문서 주필·테스트 진행 |
| W6 | P3-3 분석 → P4 튜닝 착수 | P2-3 룬 손그림 | P3 설문 분석 |
| W7 | P4 인식 튜닝·fixture | P2-4 오토타일 | P6-1 대사 풀 초안 (선행) |
| W8 | P4 profile 이식 (#80) | P2-5 prop / P5-3 4층 착수 | P6-1 계속 |
| W9 | P5-1 2층 | P5-3 4층 | P6-3 HUD 정비 |
| W10 | P5-2 3층 | P5-4 5층 확장 | P6-1 멘토 트리거 통합 |
| W11 | P5-5 1층 마감·교차 플레이 | P5-4 마감 + P2-6 PostFX | P6-2 모먼트 검수 |
| W12 | 게이트5 검증·회귀 테스트 | P6-4 저장 완성 | P7-1 접근성 |
| W13 | P7-3 테스트 2차 | P7-2 리포트 폴리시·버그픽스 | P7-3 테스트 2차 |
| W14 | 버그픽스 | P8-1 CI / P8-2 빌드·배포 | P8-3 README·발표 |
| W15 | 예비 | P8-3 마감·릴리스 | 발표 리허설 |

여유 버퍼: 1주 (W16). C 역할이 미합류면 W2~W8의 C 항목을 B가 흡수하고 P2를 2주 지연시킨다 (아트보다 게임 완결이 우선).

---

## 운영 규칙 (전 Phase 공통)

1. **PR 규모**: 한 PR은 한 작업 ID. 리뷰 없이 merge 금지. 2주 이상 열린 PR 금지 (P0-1의 재발 방지).
2. **PR 종류 경계**: TEAM_DEVELOPMENT_PLAN §5의 Contract/Runtime/Feedback/Content 구분을 따른다.
3. **문서 동기화**: 기능 PR은 이 문서 체크박스 + 로드맵 상태를 같은 PR에서 갱신.
4. **테스트 추가 기준**: recognizer 변경 → fixture 추가 필수. 층 로직 변경 → synthetic stroke PlayMode 경로 갱신.
5. **외부 자산**: 라이선스 파일 + CREDITS 갱신 없는 자산 PR은 reject.
6. **로그·개인정보**: 플레이테스트 로그는 익명화 후 `outputs/`에만. 실명·연락처 절대 미기록.

## 리스크 워치리스트

| 리스크 | 감시 신호 | 발동 시 대응 |
| --- | --- | --- |
| URP 머지로 빌드·테스트 파손 | P0-1에서 smoke 실패 | #115를 분할 (패키지만 / 라이트만) 재시도 |
| PR #108 콘텐츠와 Phase 5 계획 충돌 | rebase 충돌 다발 | #108 기준으로 P5-3/P5-4 체크리스트 재작성 |
| C 역할 미합류 | W4까지 충원 없음 | 캘린더 C열을 B로 이관, P2 2주 지연 수용 |
| playtest 모집 지연 | W5 시작 시 참가자 <3 | 교내 모집 채널 추가, 일정 1주 순연 (이후 Phase 연쇄 순연 수용) |
| 인식 정확도가 테스트에서 크게 나쁨 | 첫 시도 성공률 <40% | Phase 4를 2주로 확장, Phase 5 착수를 A 트랙만 1주 지연 |
| 사운드 톤 불일치 | 팀 청취에서 이질감 | 단일 팩(같은 제작자) 위주로 재소싱 |
| 범위 팽창 | freeze 후 신기능 PR | 게이트 7의 content freeze를 명문 규칙으로 거부 |

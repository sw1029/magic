# Research Protocol - Magic Exam Hall

작성 기준일: 2026-06-10

## 목적

`Magic Exam Hall`의 최종 플레이어블이 설명 없이도 60분 안에 통과 엔딩에 도달 가능한지 검증한다. 측정 초점은 문양 인식의 공정성, 실패 피드백의 이해도, floor별 막힘 지점, 그리고 직접 바닥에 그려 시전한다는 체감이다.

## 참가자

- 목표 인원: 1차 5명, 2차 3-5명
- 조건: 프로젝트 개발에 참여하지 않은 사람
- 제한: 마우스와 키보드를 사용할 수 있고, 연구 참여와 익명 로그 수집에 동의한 사람

## 준비물

- 고정 빌드: Windows player 또는 Unity Editor Play
- 관찰 기록지: `outputs/playtest-*/observer-notes.md`
- 자동 로그: `Application.persistentDataPath/MagicExamHallLogs/<sessionId>/`
- 사후 설문: 명확성, 공정성, 피드백 도움, 조작감, 몰입감 5점 척도 + 자유 의견

## 진행 순서

1. 참가자에게 연구 목적과 익명 로그 수집 원칙을 설명한다.
2. 조작법은 게임 안에서 확인하도록 하고, 진행자는 정답이나 문양 모양을 설명하지 않는다.
3. 참가자는 타이틀에서 새 게임을 시작해 5층 통과 엔딩 이상을 목표로 플레이한다.
4. 제한 시간은 60분이다. 중단을 원하면 즉시 중단한다.
5. 플레이 직후 사후 설문과 10-15분 인터뷰를 진행한다.

## 측정 항목

| 항목 | 정의 | 출처 |
| --- | --- | --- |
| 첫 시전까지 시간 | 새 게임 시작부터 첫 recognized 또는 failed attempt까지 | attempts 로그, 관찰 |
| 층별 완료 시간 | 각 floor completion 기록 | ending report, attempts 로그 |
| family별 첫 시도 성공률 | base family별 첫 attempt success 비율 | attempts.csv |
| overlay별 실패 유형 | invalid, incomplete, dependency, detached, no seal | attempts.csv |
| assist level 도달률 | assistLevel 1/2/3 사용 횟수 | attempts.csv |
| 막힘 지점 | 2분 이상 진행 정체 또는 같은 목표 3회 이상 실패 | 관찰 |
| 피드백 이해도 | 실패 후 다음 행동을 말로 설명 가능한지 | 인터뷰 |
| 몰입감 | 직접 마법을 시전한다는 느낌 | 설문 |

## 입력 버퍼 A/B

참가자를 0.6초, 0.8초, 1.0초 그룹에 균등 배정한다. 각 그룹은 동일 빌드를 사용하되 `WorldDrawingController.bufferSeconds`만 다르게 설정한 테스트 빌드를 사용한다. 결정 기준은 첫 시전까지 시간, family 첫 시도 성공률, overlay detached 비율, 주관 조작감 점수다.

## 중단 기준

- 참가자가 중단을 요청한다.
- 멀미, 피로, 손목 통증 등 불편을 보고한다.
- 로그 저장 오류나 fatal exception이 발생해 플레이가 더 이상 유효하지 않다.

## 분석

1. `attempts.csv`를 session 단위로 모은다.
2. 직접 식별 정보가 없는지 확인한다.
3. floor, phase, recognizedFamily, worldEffect, success, assistLevel을 pivot한다.
4. 상위 오인식/막힘 3개를 Phase 4/5 백로그로 옮긴다.
5. 입력 버퍼 D3 결정을 `docs/FINAL_COMPLETION_PLAN.md` 결정 로그에 기록한다.

## 보고 산출물

- `outputs/playtest-1/summary.md`
- `outputs/playtest-1/aggregated-attempts.csv`
- `outputs/playtest-1/observer-notes.md`
- `outputs/playtest-1/interview-themes.md`

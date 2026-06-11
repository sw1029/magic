# Release Checklist

Magic Recognizer V1.5 / Magic Exam Hall 제출 후보를 만들기 전에 이 문서를 위에서 아래로 실행한다.

관련 이슈: #13, #16, #18, #52

## 1. Web 검증

- [ ] `npm ci`
- [ ] `npm run validate:docs`
- [ ] `npm test`
- [ ] `npm run build`
- [ ] 브라우저에서 Vite dev server를 열고 base family, overlay, final seal compile 흐름을 짧게 확인한다.
- [ ] survey API나 export를 켠 경우, 테스트 데이터와 실제 제출용 데이터가 섞이지 않았는지 확인한다.

## 2. Unity 자동 검증

PowerShell에서 저장소 루트 기준으로 실행한다.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.com' -batchmode -quit -projectPath 'C:\Users\silve\source\repos\magic\unity\MagicExamHall' -executeMethod MagicExamHall.Editor.MagicExamHallSceneBuilder.BuildAll
& 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.com' -batchmode -projectPath 'C:\Users\silve\source\repos\magic\unity\MagicExamHall' -runTests -testPlatform editmode -testResults 'C:\Users\silve\source\repos\magic\unity\MagicExamHall\EditModeTestResults.xml'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.com' -batchmode -projectPath 'C:\Users\silve\source\repos\magic\unity\MagicExamHall' -runTests -testPlatform playmode -testResults 'C:\Users\silve\source\repos\magic\unity\MagicExamHall\PlayModeTestResults.xml'
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-windows.ps1 -BuildPath 'tmp\MagicExamHallFinalize\MagicExamHall.exe' -LogPath 'unity\MagicExamHall\unity-build-finalize.log'
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-magic-exam-hall-player.ps1 -BuildPath 'tmp\MagicExamHallFinalize\MagicExamHall.exe' -LogPath 'tmp\MagicExamHallFinalize\player-smoke.log'
```

- [ ] EditMode 결과가 `Passed`, failed `0`이다.
- [ ] PlayMode 결과가 `Passed`, failed `0`이다.
- [ ] Windows player build log에 `Build Finished, Result: Success.`가 있다.
- [ ] Windows player build log에 `0 errors`가 있다.
- [ ] player smoke log에 `Initialize engine version`과 `UnloadTime`이 있다.
- [ ] player smoke script가 early exit나 fatal startup pattern 없이 통과한다.

## 3. Unity 수동 5층 완주

Unity Editor Play 또는 생성된 `unity/MagicExamHall/Builds/MagicExamHall.exe`로 확인한다.

- [ ] 타이틀 → 메인 메뉴 → 저장 슬롯 선택 → 새 게임 흐름이 끊기지 않는다.
- [ ] 이어하기가 선택 슬롯의 저장 상태를 복원한다.
- [ ] 옵션에서 BGM/SFX, 마우스 감도, 좌/우클릭 스왑, 이동 키, 텍스트 크기, 색 보조, 관찰 모드가 동작한다.
- [ ] Tab으로 codex를 열고 대사/층노트/발견 탭과 수동 저장을 확인할 수 있다.
- [ ] WASD 또는 방향키로 이동할 수 있다.
- [ ] 우클릭 hold로 바닥에 stroke가 보이고, release 후 주문이 판정된다.
- [ ] 1층에서 base family 실험과 목표 완료가 가능하다.
- [ ] 2층에서 overlay operator 실험과 목표 완료가 가능하다.
- [ ] 3층에서 bridge/flow 계열 반응이 읽힌다.
- [ ] 4층에서 hazard reset과 stabilizer 반응이 읽힌다.
- [ ] 5층에서 final seal 목표를 완료할 수 있다.
- [ ] 엔딩 리포트가 표시된다.
- [ ] 플레이 중 콘솔이나 player log에 fatal exception이 없다.

## 4. 로그와 개인정보

Unity 로그 경로:

```text
Application.persistentDataPath/MagicExamHallLogs/<sessionId>/
```

- [ ] `attempts.jsonl`과 `attempts.csv`가 생성된다.
- [ ] `survey.jsonl`과 `survey.csv`가 생성된다.
- [ ] 로그에 참가자 실명, 연락처, 학번 같은 직접 식별 정보가 들어가지 않는다.
- [ ] 연구용으로 공유할 로그는 session id를 익명화한다.
- [ ] 테스트 실행으로 생긴 임시 로그와 실제 플레이테스트 로그를 구분한다.
- [ ] 개인정보와 연구 데이터 처리 기준은 #16 범위 문서와 일치한다.

## 5. 제출 패키지

- [ ] 루트 `README.md`의 빠른 시작과 Unity 실행 안내가 최신이다.
- [ ] `unity/MagicExamHall/README.md`의 Unity 버전, 조작법, 검증 명령이 최신이다.
- [ ] `docs/PROJECT_ROADMAP.md`가 현재 구현 상태와 크게 어긋나지 않는다.
- [ ] Windows build 산출물을 새로 생성했다.
- [ ] 제출물에 포함할 build, README, 발표 자료, playtest notes의 버전을 기록했다.
- [ ] known issue가 있으면 PR, issue, 또는 발표 자료에 명시했다.

## Known Benign Messages

- Unity batchmode 종료 중 `abort_threads: Failed aborting id ... mono_thread_manage will ignore it`가 보일 수 있다.
- Unity licensing에서 access token 갱신 경고가 보일 수 있다.

이 메시지는 같은 로그에 compile error, failed test, build failure, fatal exception이 없을 때만 benign으로 취급한다.

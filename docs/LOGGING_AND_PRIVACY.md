# Logging and Privacy

작성 기준일: 2026-06-10

## 원칙

- 로그는 플레이 품질 분석용으로만 사용한다.
- 실명, 연락처, 학번, 이메일, 음성, 화면 녹화 원본 같은 직접 식별 정보는 저장하지 않는다.
- raw stroke 좌표는 현재 Unity 로그에 저장하지 않는다. 저장되는 값은 판정 결과, 품질 요약, world position, floor/goal metadata다.
- 공유용 데이터는 session id를 재매핑한 뒤 `outputs/playtest-*` 아래에 둔다.

## 저장 위치

Unity 런타임은 아래 경로에 session별 로그를 만든다.

```text
Application.persistentDataPath/MagicExamHallLogs/<sessionId>/
```

생성 파일:

- `attempts.jsonl`
- `attempts.csv`
- `survey.jsonl`
- `survey.csv`

## attempts 스키마

| 필드 | 의미 | 개인정보 위험 |
| --- | --- | --- |
| sessionId | 익명 세션 식별자 | 낮음, 공유 전 재매핑 |
| trialId | 세션 내부 시도 번호 | 없음 |
| targetFamily | 실험 target family, 자유 플레이에서는 빈 값 가능 | 없음 |
| recognizedFamily | 판정된 base 또는 overlay label | 없음 |
| phase | `Base` 또는 `Overlay` | 없음 |
| baseFamily | active seal base family | 없음 |
| overlayStack | `>`로 연결한 overlay stack | 없음 |
| sealId | 런타임 seal id | 낮음 |
| floorId | 층 번호 | 없음 |
| targetObject | 목표 오브젝트 또는 실패 원인 | 없음 |
| worldEffect | world state effect id | 없음 |
| status | Recognized/Invalid/Incomplete/Ambiguous | 없음 |
| confidence | 판정 confidence | 없음 |
| closure/smoothness/tempo/stability/rotationBias | 품질 요약 벡터 | 없음 |
| worldX/worldY | 시전 중심 world position | 낮음, raw stroke 아님 |
| bufferStrokeCount | buffer에 묶인 stroke 수 | 없음 |
| attemptIndex | 세션 내부 누적 시도 | 없음 |
| elapsedMs | 층 시작 후 경과 시간 | 없음 |
| feedbackViewed | 피드백 표시 여부 | 없음 |
| success | world/gameplay 성공 여부 | 없음 |
| hintShown | 힌트 표시 여부 | 없음 |
| assistLevel | 0-3 보조 단계 | 없음 |
| assisted | 힌트 후 성공 여부 | 없음 |

## survey 스키마

| 필드 | 의미 |
| --- | --- |
| sessionId | 익명 세션 식별자 |
| clarity | 1-5 명확성 |
| fairness | 1-5 공정성 |
| feedbackHelpfulness | 1-5 피드백 도움 |
| controlFeeling | 1-5 조작감 |
| immersion | 1-5 몰입감 |
| comment | 자유 의견, 직접 식별 정보 제거 필요 |
| completedTrials | 완료 또는 발견 수 |
| totalAttempts | 총 시도 수 |

## 익명화 절차

1. 원본 로그를 `outputs/private-playtest-raw/` 같은 비공개 위치에 둔다.
2. `sessionId`를 `P01`, `P02` 같은 연구용 id로 재매핑한다.
3. `comment`에 이름, 소속, 연락처, 구체적 장소가 있으면 삭제 또는 일반화한다.
4. 재매핑된 파일만 PR 또는 발표 자료에 포함한다.
5. 원본은 제출 후 팀 정책에 따라 삭제하거나 로컬 비공개 보관한다.

## 공유 금지

- 플레이 화면 녹화 원본
- 참가자 연락처
- 원본 session id와 참가자를 연결하는 표
- 자유 의견 중 개인을 특정할 수 있는 문장

## 확인 체크

- [ ] 로그 파일 4종이 생성된다.
- [ ] raw stroke point list가 없다.
- [ ] 직접 식별 정보가 없다.
- [ ] 공유본의 session id가 재매핑됐다.
- [ ] 연구 프로토콜의 측정 항목과 attempts/survey 필드가 대응된다.

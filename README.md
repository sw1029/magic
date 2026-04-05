# Magic Recognizer V1.5

기본 5문양 base seal recognizer를 유지하면서, 같은 캔버스에서 overlay operator를 누적하고 `final seal`로 compile 결과를 확정하는 마법진 인식체계 프로토타입입니다.

현재 범위:

* 웹 캔버스 입력
* 기본 5문양 base family 인식
* user input profile 누적
* raw quality / adjusted quality 분리
* base seal 이후 overlay operator stack 인식
* `final seal` compile 결과 표시
* JSON 로그 export
* 문서 상태 동기화 검증

## 요구 사항

* Node.js `20.x` 이상
* npm `10.x` 이상

확인:

```bash
node -v
npm -v
```

## 빠른 시작

처음 clone 후 가장 먼저 할 일:

```bash
git clone <repo-url>
cd magic
npm ci
```

개발 서버 실행:

```bash
npm run dev
```

Vite가 출력하는 로컬 주소를 브라우저에서 열면 됩니다. 보통은 `http://localhost:5173` 입니다.

## 초기 검증 튜토리얼

처음 받았을 때는 아래 순서로 확인하면 됩니다.

### 1. 문서 상태 검증

큐와 task frontmatter가 맞는지 확인합니다.

```bash
npm run validate:docs
```

기대 결과:

```bash
validated 21 task documents against work queue
```

### 2. 자동 테스트 실행

recognizer 코어의 기본 family 인식, 거부 케이스, 품질 벡터 동작을 확인합니다.

```bash
npm test
```

watch 모드가 필요하면:

```bash
npm run test:watch
```

### 3. 프로덕션 빌드 확인

TypeScript 타입 검사와 Vite 번들링을 함께 확인합니다.

```bash
npm run build
```

정상 종료 시 `dist/`가 생성됩니다.

## 데모 사용 방법

현재 Web UI에는 두 층이 함께 들어 있습니다.

* core recognizer 기능:
  * base family 인식
  * overlay operator stack 인식
  * raw / adjusted quality 계산
  * final seal compile
* HCI 검토용 demo 기능:
  * `quality vector use` on/off compare
  * `clean / explain / workshop` 프리셋
  * why panel
  * guided scenario
  * recent seals / quick compare

개발 서버를 띄운 뒤 브라우저에서 아래 순서로 확인합니다.

### 1. 가장 빠른 HCI 시연 순서

1. 상단 `View Preset`에서 `clean`을 먼저 확인합니다.
2. `Guided Demo Scenario`에서 `빠른 불꽃` 또는 `느린 불꽃`을 선택합니다.
3. 캔버스에 기본 문양을 그리고 `Seal Base`를 눌러 base family를 고정합니다.
4. 오른쪽 `Outcome Compare`에서 `quality off / quality on`을 비교합니다.
5. `explain` 프리셋으로 바꿔 why panel과 quick compare를 함께 확인합니다.
6. 필요하면 `workshop` 프리셋으로 바꿔 analysis overlay와 detail panel을 엽니다.
7. `Start Overlay` 이후 같은 캔버스에서 overlay operator를 덧그린 뒤 `Seal Final`로 compile 결과를 확인합니다.

### 2. HCI 검토 포인트

시연 중 아래 문구와 카드가 바로 보여야 합니다.

* `same shape, same family`
* `quality affects execution, not family`
* `quality off`는 canonical baseline outcome만 보여 줌
* `quality on`은 quality vector 기반 outcome delta만 반영
* why panel은 상태 이유와 quality 영향 설명을 짧은 문장으로 제공
* quick compare는 현재 시도와 직전 결과를 로그 없이 비교 가능하게 제공

### 3. 버튼과 토글 설명

프리셋:

* `clean`: 설명/분석을 줄이고 draw와 핵심 결과 위주로 봄
* `explain`: why panel과 quality compare 중심으로 봄
* `workshop`: analysis overlay와 세부 패널까지 함께 봄

개별 토글:

* `quality vector use`: outcome layer에 quality delta를 반영할지 제어
* `quality compare`: quality off / on compare card 표시
* `why panel`: 결과 이유 설명 패널 표시
* `analysis overlay`: axis, closure, anchor zone, ghost guide 표시
* `detail panels`: raw / adjusted quality, profile, 로그 같은 세부 패널 표시

버튼 설명:

* `Seal Base`: 현재 base 입력을 canonical family로 확정
* `Start Overlay`: base seal 이후 overlay 입력 단계를 시작
* `Seal Final`: base family와 overlay stack을 compile 결과로 고정
* `Undo`: 마지막 stroke 제거
* `Reset`: 현재 입력 초기화
* `Export Logs`: 누적 recognition 로그를 JSON으로 저장

브라우저에서 빠르게 확인할 최소 시나리오:

1. `빠른 불꽃` 시나리오를 선택
2. `clean`에서 불꽃 삼각형을 그림
2. `Seal Base`
3. `quality vector use` on/off를 바꾸며 `Outcome Compare` 확인
4. `explain`으로 전환해 why panel 확인
5. `Start Overlay`
6. `steel_brace`, `ice_bar`, `void_cut` 중 하나를 덧그림
7. `Seal Final`
8. `quick compare`에서 현재와 직전 결과를 비교

## Base Family

현재 recognizer의 canonical silhouette는 family마다 1개만 사용합니다.

* 바람: `3개 평행 개방선`
* 땅: `하변이 더 긴 폐합 사다리꼴`
* 불꽃: `상향 인상의 폐합 삼각형`
* 물: `단일 원형 폐합 루프`
* 생명: `줄기 + 상단 분기 rooted Y`

Overlay operator:

* `steel_brace`
* `electric_fork`
* `ice_bar`
* `soul_dot`
* `void_cut`
* `martial_axis` (`void_cut` 이후에만 활성화)

주의:

* base family 판정은 profile이 가져가지 않습니다.
* overlay는 base seal 이후에만 해석합니다.
* base stroke는 overlay phase에서 ghost overlay로만 보조합니다.

## 주요 스크립트

```bash
npm run dev            # 개발 서버
npm run build          # 타입 검사 + 프로덕션 빌드
npm test               # Vitest 일괄 실행
npm run test:watch     # Vitest watch
npm run validate:docs  # docs 상태/의존성 검증
```

## 디렉토리 구조

```text
src/       웹 데모와 recognizer 코어
tests/     Vitest 테스트
scripts/   문서 검증 스크립트
docs/      방향/큐/task 문서
chat/      원본 논의 보관소
```

## 문서 읽기 순서

문서 기준을 먼저 보고 싶다면 아래 순서가 가장 빠릅니다.

1. `docs/README.md`
2. `docs/10_direction/final-direction.md`
3. `docs/10_direction/prototype-target.md`
4. `docs/20_queue/work-queue.md`
5. `docs/30_tasks/epic-02-symbols-and-input/task-01-base-symbol-prototypes.md`
6. `docs/30_tasks/epic-02-symbols-and-input/task-02-input-interpretation-rules.md`

## 트러블슈팅

`vitest: not found` 가 뜨면 의존성이 설치되지 않은 상태입니다.

```bash
npm ci
```

문서 검증이 실패하면 `work-queue.md`와 각 task 문서의 frontmatter 상태/의존성이 어긋난 것입니다. 먼저 아래를 다시 실행해 원인을 확인합니다.

```bash
npm run validate:docs
```

# Magic Recognizer V1

기본 5문양을 웹 캔버스에서 그리고, `seal` 시점에 canonical 결과와 품질 벡터를 확정하는 초기 마법진 인식체계 프로토타입입니다.

현재 범위:

* 웹 캔버스 입력
* 기본 5문양 인식
* draw 중 후보 preview
* `seal` 후 최종 결과 확정
* 품질 벡터 표시
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

개발 서버를 띄운 뒤 브라우저에서 아래 순서로 확인합니다.

1. 캔버스에 기본 문양 하나를 그립니다.
2. 오른쪽 `Preview` 패널에서 live candidate와 status를 봅니다.
3. 필요하면 `debug overlay`를 켜 둔 상태로 symmetry axis, closure line을 확인합니다.
4. `Seal` 버튼을 눌러 canonical 결과를 확정합니다.
5. `Final Result`와 `Quality Vector`를 확인합니다.
6. `Export Logs`로 JSON 로그를 내려받습니다.

버튼 설명:

* `Seal`: 현재 입력을 canonical 결과로 확정
* `Undo`: 마지막 stroke 제거
* `Reset`: 현재 입력 초기화
* `Export Logs`: 누적 recognition 로그를 JSON으로 저장

## V1 기준 문양

현재 recognizer의 canonical silhouette는 family마다 1개만 사용합니다.

* 바람: `3개 평행 개방선`
* 땅: `하변이 더 긴 폐합 사다리꼴`
* 불꽃: `상향 인상의 폐합 삼각형`
* 물: `단일 원형 폐합 루프`
* 생명: `줄기 + 상단 분기 rooted Y`

주의:

* draw 중에는 의미를 확정하지 않습니다.
* 최종 결과는 `seal` 시점에만 확정합니다.
* `물방울`, `동심원 변형`, 복잡한 `새싹` variation은 V1 범위가 아닙니다.

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

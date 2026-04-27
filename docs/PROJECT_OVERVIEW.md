# Project Overview

이 문서는 `Magic Recognizer V1.5`의 프로젝트 개요 문서입니다. 문제 정의, 핵심 인터랙션, recognizer 구조, 실행 방법, 검증 계획, 향후 작업을 함께 정리합니다.

## 1. 프로젝트 한줄 요약

- 프로젝트명: Magic Recognizer V1.5
- 저장소: [sw1029/magic](https://github.com/sw1029/magic)
- 주제: 마법진/기호 기반 제스처 입력을 통해 마법 스킬을 시전하는 웹 프로토타입
- 핵심 목표: 사용자가 단순한 base symbol을 그리고, 그 위에 overlay operator를 조합하여 final seal을 완성하며, 시스템이 입력 품질과 사용자 프로필을 반영해 결과를 설명하는 인터랙션을 구현한다.
- 주요 사용자: 제스처 기반 인터랙션과 판타지 게임에 관심 있는 사용자
- 주요 산출물: 웹 기반 구현 프로토타입, recognizer 모듈, 테스트 코드, 프로젝트 문서

## 2. 문제 정의

특징적인 게임 입력 시스템은 강한 몰입감을 줄 수 있지만, 초반에는 사용자가 규칙을 해독해야 하는 부담을 만들 수 있습니다. 마법진이나 기호 기반 입력은 버튼식 입력보다 표현적이지만, 사용자가 기호의 의미, 입력 순서, 실패 원인을 이해하지 못하면 오히려 진입장벽이 됩니다.

- 현재 사용자가 겪는 불편: 많은 기호와 스킬을 한 번에 외워야 하면 인지적 부담이 커진다.
- 기존 방식의 한계: 단순 단축키 입력은 효율적이지만 마법을 직접 시전한다는 몰입감이 약하다.
- 제스처 입력의 위험: 입력이 실패했을 때 왜 실패했는지 알기 어렵다면 사용자는 시스템을 불공정하게 느낀다.
- 프로젝트가 제공할 개선점: 기호의 형태, 조합 규칙, 입력 품질, 결과 설명을 하나의 온보딩 경험으로 연결한다.

## 3. 핵심 설계 질문

1. 사용자는 base symbol의 의미를 형태만으로 얼마나 쉽게 이해할 수 있는가?
2. 사용자는 base symbol 이후 overlay operator를 조합해 final seal 결과를 예측할 수 있는가?
3. raw quality와 adjusted quality를 분리해 보여주는 방식이 사용자의 실패 이해를 돕는가?
4. user input profile은 개인별 드로잉 습관을 반영해 입력 경험을 더 공정하게 만드는가?
5. 튜토리얼은 게임 흐름을 끊지 않으면서 사용자가 핵심 규칙을 익히도록 돕는가?

## 4. 프로젝트 목표

### 핵심 목표

1. 웹 캔버스에서 마우스 기반 제스처 입력을 구현한다.
2. 기본 5문양 base family를 인식하고, base seal로 확정할 수 있게 한다.
3. base seal 이후 overlay operator stack을 누적해 final seal을 compile한다.
4. quality vector, why panel, outcome compare를 통해 입력 결과의 이유를 설명한다.
5. user input profile을 누적해 개인별 입력 습관을 반영한다.
6. 사용성과 디버깅을 함께 고려한 튜토리얼/데모 흐름을 제공한다.

### 제외 범위

현재 버전에서는 전체 게임 완성보다 recognizer와 온보딩 경험 검증에 집중합니다.

- 복잡한 전투 시스템
- 다수의 스테이지
- 온라인 멀티플레이
- 상용 게임 수준의 아트/사운드 제작
- 완전한 게임 밸런싱 시스템

## 5. 현재 구현 범위

저장소 README 기준 현재 프로토타입은 다음 기능을 포함합니다.

| 기능 | 설명 | 상태 |
| --- | --- | --- |
| 웹 캔버스 입력 | 브라우저에서 제스처를 직접 그림 | Implemented |
| base family 인식 | 기본 5문양 base seal 인식 | Implemented |
| user input profile | 사용자 입력 profile 누적 | Implemented |
| raw/adjusted quality | 원본 입력 품질과 보정된 품질을 분리 | Implemented |
| overlay operator stack | base 이후 추가 연산자 입력 누적 | Implemented |
| final seal compile | base + overlay 결과 확정 | Implemented |
| JSON 로그 export | recognition 로그를 JSON으로 저장 | Implemented |
| 문서 상태 검증 | 문서/task 상태 동기화 검증 스크립트 | Implemented |

## 6. 핵심 인터랙션

현재 프로젝트의 핵심 인터랙션은 "Draw, Seal, Overlay, Compile"입니다.

- Draw: 사용자가 캔버스에 base symbol을 그린다.
- Seal Base: 현재 입력을 base family로 확정한다.
- Start Overlay: 같은 캔버스에서 overlay operator 입력 단계로 넘어간다.
- Seal Final: base family와 overlay stack을 compile해 final seal 결과를 확정한다.
- Explain: why panel, quality compare, quick compare를 통해 결과의 이유를 보여준다.

이 구조는 사용자가 단순히 정답/오답을 받는 것이 아니라, 자신의 입력이 어떤 계열로 인식됐고 어떤 품질 차이가 결과에 영향을 주었는지 이해하도록 돕습니다.

## 7. 기호 체계

### Base Family

현재 recognizer의 canonical silhouette는 family마다 1개씩 사용합니다.

| Base family | 문양 | 의미/역할 |
| --- | --- | --- |
| 바람 | 3개 평행 개방선 | 흐름, 이동, 확산 |
| 땅 | 하변이 더 긴 폐합 사다리꼴 | 안정, 고정, 방어 |
| 불꽃 | 상향 인상의 폐합 삼각형 | 공격, 상승, 에너지 |
| 물 | 단일 원형 폐합 루프 | 순환, 보호, 회복 |
| 생명 | 줄기 + 상단 분기 rooted Y | 성장, 연결, 생명력 |

### Overlay Operator

Overlay는 base seal 이후에만 해석되며, base symbol 위에 추가적인 속성이나 변형을 더하는 역할을 합니다.

| Operator | 역할 |
| --- | --- |
| `steel_brace` | 방어/강화 계열 |
| `electric_fork` | 전기/분기 계열 |
| `ice_bar` | 냉기/고정 계열 |
| `soul_dot` | 집중/핵심점 계열 |
| `void_cut` | 절단/무효화 계열 |
| `martial_axis` | `void_cut` 이후 활성화되는 축/전투 계열 |

## 8. 사용자 흐름

1. 사용자가 웹 데모에 진입한다.
2. `View Preset`에서 `clean`, `explain`, `workshop` 중 하나를 선택한다.
3. guided scenario에서 예시 상황을 선택한다.
4. 캔버스에 base symbol을 그린다.
5. `Seal Base`로 base family를 확정한다.
6. quality compare 또는 why panel을 통해 인식 결과를 확인한다.
7. `Start Overlay`로 overlay 단계에 진입한다.
8. 같은 캔버스에서 overlay operator를 덧그린다.
9. `Seal Final`로 final seal 결과를 확정한다.
10. quick compare와 recent seals를 통해 직전 결과와 현재 결과를 비교한다.

## 9. 온보딩 및 튜토리얼 설계

튜토리얼은 모든 기호와 연산자를 한 번에 설명하지 않습니다. 사용자가 base symbol을 먼저 익히고, 이후 overlay operator를 추가하며 조합 규칙을 단계적으로 이해하도록 설계합니다.

### 튜토리얼 원칙

- 처음에는 base family의 의미와 입력 방식만 제시한다.
- overlay operator는 base seal 이후에 소개한다.
- 입력 중에는 feedforward를, 입력 후에는 feedback을 제공한다.
- 실패했을 때는 단순 실패가 아니라 quality, closure, axis, anchor zone 같은 원인을 설명한다.
- `clean` 프리셋은 초보자에게, `explain`과 `workshop` 프리셋은 분석과 검토에 사용한다.

### 데모 프리셋

| Preset | 목적 |
| --- | --- |
| `clean` | 설명/분석을 줄이고 draw와 핵심 결과 위주로 확인 |
| `explain` | why panel과 quality compare 중심으로 인식 이유 확인 |
| `workshop` | analysis overlay와 detail panel까지 포함한 심화 검토 |

## 10. 화면 및 콘텐츠 구성

| 화면/영역 | 목적 | 포함 요소 |
| --- | --- | --- |
| Drawing Canvas | 사용자가 base/overlay를 그림 | 캔버스, stroke, reset/undo |
| View Preset | 데모의 정보 밀도 조절 | clean, explain, workshop |
| Guided Demo Scenario | 데모 흐름 제공 | 빠른 불꽃, 느린 불꽃 등 |
| Recognition Result | base family 인식 결과 표시 | family, confidence, quality |
| Outcome Compare | quality off/on 결과 비교 | canonical baseline, quality delta |
| Why Panel | 결과 이유 설명 | 상태 이유, quality 영향 |
| Analysis Overlay | 입력 구조 시각화 | axis, closure, anchor zone, ghost guide |
| Final Seal Result | base + overlay compile 결과 | final seal, operator stack |
| Recent Seals / Quick Compare | 이전 결과와 비교 | 직전 결과, 현재 결과 |

## 11. 개인화 및 품질 인식 설계

같은 문양이라도 사용자마다 그리는 방식이 다릅니다. 이 프로젝트는 입력 차이를 단순 오류로만 처리하지 않고, profile과 quality vector를 통해 사용자의 스타일과 입력 상태를 분리해서 해석합니다.

### 주요 개념

- raw quality: 사용자가 실제로 그린 입력의 원본 품질
- adjusted quality: user input profile 등을 반영해 조정된 품질
- quality vector: family 자체를 바꾸기보다 실행 결과의 세기나 안정성에 영향을 주는 품질 정보
- user input profile: 사용자의 반복 입력 패턴을 누적하는 개인화 정보

### 설계상 장점

- 인식 실패 이유를 설명해 사용자가 다음 입력을 개선할 수 있게 한다.
- 사용자를 시스템에 맞추는 대신, 시스템이 사용자의 반복 스타일을 일부 반영한다.
- 같은 family 안에서도 quality 차이가 결과에 영향을 준다는 점을 시각적으로 이해시킨다.

## 12. 저장소 구조

GitHub 저장소 구조를 기준으로 현재 프로젝트는 다음과 같이 구성되어 있습니다.

```text
.
├─ index.html
├─ package.json
├─ vite.config.ts
├─ tsconfig.json
├─ src/
│  ├─ app.ts
│  ├─ demo-layer.ts
│  ├─ main.ts
│  ├─ style.css
│  ├─ demo/
│  │  ├─ exemplars.ts
│  │  ├─ explain.ts
│  │  ├─ outcome-summary.ts
│  │  └─ tutorial-flow.ts
│  └─ recognizer/
│     ├─ compile.ts
│     ├─ geometry.ts
│     ├─ operator-templates.ts
│     ├─ overlay.ts
│     ├─ recognize.ts
│     ├─ rerank.ts
│     ├─ operators.ts
│     ├─ templates.ts
│     ├─ tutorial-profile.ts
│     ├─ user-profile.ts
│     └─ types.ts
├─ tests/
├─ scripts/
│  ├─ tutorial-dataset/
│  ├─ ml-baseline/
│  └─ validate-doc-state.mjs
└─ artifacts/
   └─ ml/
```

### 주요 디렉토리 역할

| 경로 | 역할 |
| --- | --- |
| `src/` | 웹 데모와 recognizer 코어 |
| `src/demo/` | 데모 기능, 설명 패널, 튜토리얼 플로우 |
| `src/recognizer/` | 제스처 인식, geometry, rerank, 품질 평가, profile |
| `tests/` | Vitest 기반 자동 테스트 |
| `scripts/tutorial-dataset/` | 튜토리얼/공개 데이터셋 변환 및 manifest 생성 |
| `scripts/ml-baseline/` | baseline 학습/내보내기 스크립트 |
| `artifacts/ml/` | ML baseline, feature spec, dataset split 결과물 |

## 13. 기술 스택

| 영역 | 기술 | 선택 이유 |
| --- | --- | --- |
| Frontend | Vite, TypeScript, HTML/CSS | 빠른 웹 프로토타입과 캔버스 입력 구현에 적합 |
| Recognizer | TypeScript geometry/recognition modules | 브라우저에서 즉시 피드백 가능 |
| Testing | Vitest | recognizer와 demo logic 자동 검증 |
| Dataset/ML scripts | Node.js scripts, Python scripts | 데이터 변환과 baseline 생성 |
| Build | TypeScript, Vite | 타입 검사와 번들링 |

## 14. 실행 및 검증 방법

### 요구 사항

- Node.js `20.x` 이상
- npm `10.x` 이상

### 설치

```bash
git clone https://github.com/sw1029/magic.git
cd magic
npm ci
```

### 개발 서버

```bash
npm run dev
```

Vite가 출력하는 로컬 주소를 브라우저에서 엽니다. 일반적으로 `http://localhost:5173`입니다.

### 테스트 및 빌드

```bash
npm test
npm run build
npm run validate:docs
```

| 명령 | 목적 |
| --- | --- |
| `npm run dev` | 개발 서버 실행 |
| `npm test` | Vitest 테스트 실행 |
| `npm run test:watch` | watch 모드 테스트 |
| `npm run build` | TypeScript 타입 검사 + Vite 빌드 |
| `npm run validate:docs` | 문서/task 상태 검증 |

## 15. 검증 계획

### 검증 목적

사용자가 기호 기반 입력 시스템을 이해하고 사용할 수 있는지, 그리고 quality explanation과 profile 기반 보정이 실패 이해와 입력 개선에 도움이 되는지 확인합니다.

### 검증 방법

- 관찰 테스트: 사용자가 튜토리얼을 진행하는 동안 막히는 지점을 기록한다.
- 태스크 기반 테스트: base seal과 overlay operator를 입력하도록 요청한다.
- 비교 테스트: quality off/on 결과를 사용자가 이해하는지 확인한다.
- 설문: 몰입감, 재미, 학습 난이도, 피드백 이해도, 실패 납득도를 측정한다.

### 측정 지표

| 지표 | 설명 |
| --- | --- |
| 첫 시도 성공률 | base symbol을 처음 보고 그렸을 때 인식에 성공하는 비율 |
| 튜토리얼 완료 시간 | 기본 데모 흐름을 끝내는 데 걸리는 시간 |
| overlay 적용 성공률 | base 이후 operator를 올바르게 적용하는 비율 |
| 실패 피드백 이해도 | 실패 후 무엇을 고쳐야 하는지 이해하는 정도 |
| quality compare 이해도 | quality off/on 차이를 설명할 수 있는 정도 |
| 주관적 몰입감 | 마법을 직접 시전한다고 느끼는 정도 |

## 16. 프로젝트 일정

| 단계 | 작업 | 산출물 |
| --- | --- | --- |
| 1단계 | 주제 확정 및 문제 정의 | 프로젝트 개요 문서 |
| 2단계 | base/overlay 기호 체계 정리 | 기호 목록, operator 규칙 |
| 3단계 | 웹 프로토타입 구현 | Vite/TypeScript demo |
| 4단계 | recognizer 및 quality 설명 개선 | 인식 결과, why panel, compare |
| 5단계 | 사용성 검증 | 테스트 결과, 관찰 기록, 설문 |
| 6단계 | 문서 및 릴리스 정리 | README, 프로젝트 문서, 검증 결과 |

## 17. 주요 산출물

- GitHub repository: [sw1029/magic](https://github.com/sw1029/magic)
- 웹 기반 구현 프로토타입
- 프로젝트 개요 및 설계 문서
- recognizer 테스트 코드
- 데모 및 검증 결과

## 18. AI 작업 지침

AI가 이 프로젝트를 이해하고 이어서 작업할 수 있도록 다음 기준을 따릅니다.

### AI가 먼저 확인할 것

1. 현재 요청이 기획, 디자인, 구현, 검증, 문서화 중 어느 단계인지 확인한다.
2. 현재 구현은 Vite/TypeScript 기반 `Magic Recognizer V1.5`임을 전제로 한다.
3. base family와 overlay operator의 구분을 유지한다.
4. family 인식과 quality 영향은 분리해서 설명한다.
5. 사용자의 실패를 단순 오류가 아니라 학습 가능한 피드백으로 다룬다.

### AI에게 요청할 때 포함하면 좋은 정보

- 작업 목적:
- 수정해야 할 화면 또는 기능:
- 기대 동작:
- 관련 파일 또는 모듈:
- 포함해야 하는 설계 키워드:
- 검증 방법:

### AI가 변경할 때 지켜야 할 점

- 복잡한 게임 시스템보다 입력 인식과 피드백 설계를 우선한다.
- base symbol, overlay operator, quality explanation, profile이라는 네 축을 유지한다.
- 실패 피드백과 튜토리얼 흐름을 반드시 고려한다.
- 구현 변경 시 `npm test`, `npm run build`, 필요하면 `npm run validate:docs`로 검증한다.

## 19. 현재 상태

- 저장소: [sw1029/magic](https://github.com/sw1029/magic)
- 기본 브랜치: `main`
- 주요 언어: TypeScript
- 프로젝트 패키지명: `magic-recognizer-v1-5`
- 현재 단계: 구현 프로토타입 및 검증 준비
- 다음 작업: 실제 데모 화면 검토, 구현 화면의 용어 통일, 사용성 검증 태스크 작성

## 20. 결정 사항 기록

| 날짜 | 결정 | 이유 | 영향 |
| --- | --- | --- | --- |
| 2026-04-27 | GitHub 저장소 기준으로 문서 구조 재정리 | 실제 구현 구조와 문서가 달라지는 문제를 막기 위함 | 기술 스택, 실행 방법, 저장소 구조가 현재 repo와 일치 |
| 2026-04-27 | 프로젝트명을 `Magic Recognizer V1.5`로 문서화 | README와 package 기준 명칭을 따르기 위함 | 구현체 이름을 일관되게 사용 |
| 2026-04-27 | base family와 overlay operator를 분리해 설명 | 현재 recognizer 구조가 두 단계로 구성되어 있음 | 조합형 스킬 시스템과 직접 연결됨 |
| 2026-04-27 | quality explanation을 핵심 설계 요소로 설정 | 실패 원인 이해와 입력 개선을 돕는 기능이기 때문 | 검증 지표에 quality compare 이해도 추가 |

## 21. 열린 질문

- 공개 프로젝트명은 `Magic Recognizer V1.5`로 유지할 것인가, 별도 게임명과 병기할 것인가?
- 사용성 검증에서는 `clean`, `explain`, `workshop` 중 어떤 프리셋을 기본으로 사용할 것인가?
- 디자인 문서는 현재 웹 구현 화면을 재정리하는 방식으로 만들 것인가, 이상적인 최종 화면으로 따로 정리할 것인가?
- 개인화 profile의 효과를 실제 검증에서 어떻게 보여줄 것인가?
- final seal 결과를 게임 내 스킬 효과와 얼마나 직접적으로 연결할 것인가?

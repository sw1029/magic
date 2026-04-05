# HCI UX 데모 확장 계획

이 문서는 현재 Web UI 데모를 **HCI 관점의 클라이언트 검토용 데모**로 확장하기 위한 계획을 정리합니다.
현재 작업 트리 기준으로는 계획 문서이면서 동시에 **실제 구현 반영 상태**를 함께 기록합니다.

이번 문서의 목적은 단순히 기능을 더 넣는 것이 아닙니다.
핵심은 아래 질문에 데모 화면만으로 답할 수 있게 만드는 것입니다.

* 유저가 지금 무엇을 하고 있는지 즉시 이해할 수 있는가
* 같은 모양은 같은 종류라는 규칙이 납득 가능한가
* 품질 벡터가 결과 차이에 어떻게 개입하는지 보이는가
* 분석 정보는 필요할 때만 켜고 끌 수 있는가
* 이 데모가 “기술 검증 화면”이 아니라 “유저 친화적 마법진 authoring 경험”처럼 보이는가

---

## 0. 현재 구현 반영 상태

현재 구현된 항목:

* `quality vector use` on/off 비교 UI
* `quality off` canonical baseline outcome / `quality on` quality delta outcome 분리
* `clean / explain / workshop` 프리셋
* why panel 및 explain helper
* `same shape, same family` 보증 배지
* `quality affects execution, not family` 고정 문구
* guided scenario chip 4개
* narration strip
* outcome fingerprint
* recent seals / quick compare
* analysis overlay를 기존 debug 표현보다 명확한 UX로 정리

core recognizer 기능과 HCI demo 기능의 경계:

* core recognizer:
  * base family 인식
  * overlay operator stack 인식
  * raw / adjusted quality 계산
  * final seal compile
* HCI demo layer:
  * preset
  * compare card
  * why panel
  * guided scenario
  * quick compare
  * 보증 문구와 narration strip

현재 구현 상태 기준:

* H01 ~ H07: 구현됨
* H08: README 사용 방법 보강 수준으로 반영됨. 별도 전용 시연 스크립트 문서는 아직 없음

---

## 1. 기준 문서

이번 계획은 아래 문서를 함께 기준으로 사용합니다.

* `chat/request-answer05.md`
* `chat/request-answer08.md`
* `chat/request-answer14.md`
* `chat/request-answer16.md`
* `chat/request-answer18.md`
* `docs/00_source_map/client-decisions.md`
* `docs/10_direction/final-direction.md`
* `docs/10_direction/prototype-target.md`
* `docs/20_queue/prototype-implementation-plan.md`
* `docs/30_tasks/epic-05-prototype-battle-sandbox/task-02-workshop-toggle-rules.md`
* `docs/30_tasks/epic-06-logging-and-debug-hooks/task-02-analysis-toggle-and-hook-points.md`

---

## 2. 현재 데모의 강점과 부족한 점

### 현재 데모의 강점

현재 Web UI는 이미 아래 장점을 갖고 있습니다.

* 기본 5문양을 바로 그려 볼 수 있다
* draw 중 후보군을 보여 준다
* `seal` 후 canonical 결과를 보여 준다
* 품질 벡터를 수치와 bar로 보여 준다
* debug overlay on/off가 있다
* 로그 export가 가능하다

즉, 현재 데모는 **입력기와 recognizer 검증용 화면**으로는 충분히 의미가 있습니다.

### 현재 데모의 부족한 점

하지만 HCI 검토용 관점에서는 아래가 아직 약했습니다.

* 품질 벡터가 “보이기만” 하고 실제 체감 차이로 연결돼 보이지 않는다
* 유저가 “왜 이 결과가 나왔는가”를 한눈에 이해하기 어렵다
* 분석 정보와 설명 정보가 하나의 층으로 섞여 있다
* `품질 벡터를 쓰는 모드`와 `안 쓰는 모드`를 비교할 수 없다
* 데모를 처음 보는 클라이언트가 바로 시연 포인트를 잡기 어렵다
* 화면이 `개발 디버그 화면`에 가깝고 `설명 가능한 authoring UI`로는 아직 덜 정리돼 있다

따라서 이번 단계는 recognizer 자체보다 **보여 주는 방식과 비교 구조**를 강화하는 데 초점을 둡니다.

---

## 3. 이번 단계의 핵심 목표

이번 HCI 데모 확장의 완료 기준은 아래 한 줄로 요약합니다.

**클라이언트가 Web UI만 보고도 “이 시스템이 유저 친화적으로 설계됐는지”, “품질 벡터가 어떻게 개입하는지”, “정보 과밀 없이 설명 가능한지”를 판단할 수 있어야 한다**

이를 위해 이번 단계에서는 아래 3가지를 반드시 보여 줘야 합니다.

### 1. 규칙의 예측 가능성

* 같은 모양은 같은 family로 읽힌다
* 품질 차이는 종류가 아니라 결과 차이에만 영향을 준다

### 2. 분석 정보의 선택 가능성

* 분석 UI는 필요할 때만 켤 수 있다
* 기본 화면은 깔끔하게 유지할 수 있다

### 3. 설명 가능한 차이 시각화

* 품질 벡터 on/off 비교가 가능하다
* 어떤 벡터가 어떤 user-facing 결과 차이로 이어지는지 보인다

---

## 4. 설계 원칙

### 4-1. progressive disclosure

기본 화면은 간결해야 합니다.
설명, 분석, 비교는 단계적으로 열려야 합니다.

즉, 아래 순서를 권장합니다.

* 기본: draw + preview + final
* 설명: why panel, result delta
* 분석: debug overlay, raw quality, log

### 4-2. compare, do not just display

HCI 검토에서 중요한 것은 숫자 표시보다 **차이의 비교**입니다.
따라서 품질 벡터는 단독 패널보다 아래 형태가 더 중요합니다.

* `quality off`
* `quality on`
* `delta`

### 4-3. family semantics는 고정

품질벡터 on/off는 `family recognition`을 흔드는 토글이 되어서는 안 됩니다.
이번 단계의 `quality vector use on/off`는 아래 비교를 보여 주는 용도로 제한합니다.

* canonical family는 동일
* derived outcome card만 달라짐

### 4-4. raw stroke 우선

설명용 시각화는 원본 선을 덮어쓰지 않습니다.

* ghost overlay
* badge
* subtle highlight
* side panel explanation

으로만 보조합니다.

### 4-5. demo-friendly affordance

클라이언트 검토용 화면에는 즉시 어필 가능한 포인트가 필요합니다.
하지만 엔진 구조를 왜곡하는 과장은 피해야 합니다.

따라서 이번 단계의 어필 요소는 아래 조건을 만족해야 합니다.

* 현재 규칙과 모순되지 않을 것
* 한 번의 시연에서 바로 이해될 것
* 나중에 실제 UX 자산으로 재사용 가능할 것

---

## 5. 이번 단계에서 넣을 UI 기능

아래 기능은 `must`, `recommended`, `optional appeal` 세 층으로 나눕니다.

### 5-1. must: 반드시 넣을 기능

#### A. `quality vector use` 토글

목적:

* 품질 벡터 개입 여부를 클라이언트가 직접 확인

권장 동작:

* `off`: canonical family와 중립 outcome만 표시
* `on`: canonical family는 그대로 두고, 품질 벡터 기반 outcome delta를 반영

중요:

* 이 토글은 recognizer family 판정 토글이 아니다
* `결과 차이 해석`의 on/off여야 한다

#### B. `explain result` 토글

목적:

* 왜 이 결과가 나왔는지 자연어에 가까운 짧은 설명 제공

예시:

* `shape fixed: fire`
* `quality applied: tempo +12, stability -7`
* `reason: 빠른 tempo로 발동감이 증가했지만 안정성은 낮아짐`

#### C. `analysis overlay` 토글

목적:

* 기존 debug overlay를 HCI 검토 관점에서 더 명확하게 정리

표시 대상:

* symmetry axis
* closure gap
* live status
* anchor hint
* quality-active badge

#### D. `view mode` 프리셋

최소 3모드가 적절합니다.

* `clean`
  * 최소 정보만 표시
* `explain`
  * why panel, quality delta, user-facing explanation 표시
* `workshop`
  * debug/analysis까지 모두 표시

이 프리셋은 클라이언트에게 `정보를 항상 다 보여 주지 않는다`는 점을 설득하는 데 중요합니다.

#### E. `quality compare card`

목적:

* `quality off`와 `quality on`을 같은 화면에서 비교

권장 표시:

* family
* output intensity
* control
* stability
* risk

이 카드가 있으면 클라이언트는 품질벡터의 역할을 즉시 이해할 수 있습니다.

### 5-2. recommended: 강하게 권장하는 기능

#### F. `same shape guarantee` 배지

목적:

* 현재 프로젝트의 가장 중요한 신뢰성 원칙을 화면에 고정

예시 문구:

* `same shape, same family`
* `quality changes execution, not spell type`

#### G. `why this status?` 패널

목적:

* recognized / ambiguous / incomplete / invalid 상태의 이유를 더 설명 가능하게 제공

예시:

* `recognized: score 0.82, margin 0.18`
* `incomplete: closure missing`
* `ambiguous: water vs life margin too small`

#### H. `guided demo scenario` 칩

목적:

* 시연자가 클라이언트 앞에서 바로 보여 줄 포인트를 잡게 함

권장 시나리오:

* `같은 모양, 빠른 속도`
* `같은 모양, 느린 속도`
* `closure leak`
* `rotation bias`

초기에는 버튼이 실제 sample stroke를 로드하지 않아도 됩니다.
문구 가이드만 있어도 데모성이 올라갑니다.

#### I. `recent seals` 미니 히스토리

목적:

* 한 번의 입력만 보는 것이 아니라 바로 이전 결과와 비교 가능

최소 표시:

* family
* status
* quality on/off 당시 delta

### 5-3. optional appeal: 여유가 있으면 넣을 요소

#### J. subtle motion

목적:

* 화면을 더 살아 있게 보이게 하되 과장하지 않음

권장 범위:

* result card reveal
* bar fill animation
* active toggle glow

#### K. demo narration strip

목적:

* 현재 단계에서 사용자에게 무엇을 해 보라고 권하는지 한 줄로 안내

예시:

* `불꽃 삼각형을 빠르게 그리고 quality on/off를 비교해 보세요`

#### L. outcome fingerprint

목적:

* 결과 차이를 카드 하나로 압축 표현

예시:

* `sharp / stable / risky`
* `soft / controlled / slow`

---

## 6. 품질벡터 on/off의 권장 해석 모델

현재 V1 recognizer는 품질벡터를 표시하지만, 아직 user-facing 결과 차이와 직접적으로 묶인 UI는 약합니다.
따라서 이번 단계에서는 아래처럼 **데모용 결과 카드 레이어**를 도입하는 것이 가장 안전합니다.

### 6-1. 분리 원칙

* `recognition layer`
  * family, status, candidate
* `demo outcome layer`
  * quality on/off에 따라 달라지는 user-facing result summary

### 6-2. outcome card 제안

아래 4축이 가장 설명하기 쉽습니다.

* `output`
* `control`
* `stability`
* `risk`

예시 매핑:

* `tempo` 증가 -> `output` 또는 `responsiveness` 상승
* `stability` 감소 -> `risk` 상승
* `closure` 상승 -> `control` 상승
* `overshoot` 증가 -> `control` 하락
* `rotationBias` 증가 -> `stability` 또는 `risk`에 약한 패널티

중요:

* 이 매핑은 데모 설명용이다
* 전투 밸런스 최종 수치로 고정하지 않는다
* 클라이언트가 `품질벡터가 죽어 있지 않다`는 점을 확인하는 용도다

### 6-3. 토글 동작 제안

현재 구현 반영:

* `quality off`: canonical baseline outcome만 표시
* `quality on`: quality vector 기반 delta를 outcome card와 why panel에 반영
* family는 두 경우 모두 동일하게 유지
* 고정 문구는 현재 구현에서 `quality affects execution, not family`로 표기

#### `quality off`

* family는 그대로
* outcome card는 중립값 또는 canonical baseline
* 화면에 `quality influence disabled` 뱃지 표시

#### `quality on`

* family는 그대로
* raw quality 또는 adjusted quality 기반 delta 반영
* delta bar와 짧은 설명 문구 표시

---

## 7. UI 상태 모델 제안

```ts
interface DemoViewState {
  analysisOverlay: boolean;
  qualityInfluence: boolean;
  explainResult: boolean;
  compareMode: boolean;
  guidanceMode: boolean;
  showRecentSeals: boolean;
  showQualitySplit: boolean;
  showProfilePanel: boolean;
  showLogViewer: boolean;
  selectedScenarioId: GuidedDemoScenarioId;
  viewPreset: "clean" | "explain" | "workshop";
}
```

현재 구현에서는 위 상태가 `src/demo-layer.ts`에 있고, 결과 레이어는 아래처럼 `src/demo/outcome-summary.ts`로 분리되어 있습니다.

```ts
interface DemoOutcomeSummary {
  family: GlyphFamily | null;
  qualityEnabled: boolean;
  output: number;
  control: number;
  stability: number;
  risk: number;
  explanation: string[];
}
```

---

## 8. 화면 구성 제안

현재 구현 반영 요약:

* 상단 hero에 guarantee badge와 execution/family 문구가 있음
* 상단 rail에 preset, manual toggle, guided scenario가 먼저 배치됨
* narration strip이 별도 한 줄로 존재함
* 우측 패널에는 outcome compare, why panel, recent seals / quick compare가 포함됨

### 8-1. 상단

* 현재 데모 제목
* `same shape, same family` 보증 뱃지
* 모드 프리셋 선택

### 8-2. 좌측 메인

* 캔버스
* 현재와 동일한 draw 영역
* analysis overlay가 켜질 때만 ghost layer 표시

### 8-3. 우측 정보 패널

카드를 아래 순서로 정리하는 것이 좋습니다.

1. `preview`
2. `final result`
3. `quality vector`
4. `quality compare`
5. `why panel`
6. `recent seals`
7. `log viewer`

### 8-4. 토글 영역

현재 단일 debug toggle 대신 아래 묶음을 권장합니다.

* `quality vector use`
* `explain result`
* `analysis overlay`
* `guidance hints`

이 영역은 상단 툴바 또는 캔버스 하단에 두되, 시각적으로 한 그룹으로 묶는 것이 좋습니다.

---

## 9. 세부 작업 분해

### H01. HCI 검토 질문 동결

목표:

* 이번 데모가 어떤 질문에 답해야 하는지 명시

세부 작업:

* 클라이언트 관점의 검토 질문 5개 고정
* must / recommended / optional 구분 고정

대상 파일:

* `docs/20_queue/hci-ux-demo-plan.md`

완료 기준:

* 이후 구현자가 무엇을 위한 토글인지 혼동하지 않음

### H02. demo outcome layer 정의

목표:

* recognizer 결과와 HCI 설명용 결과 카드를 분리

세부 작업:

* `quality off` baseline 정의
* `quality on` delta 매핑 정의
* 설명용 stat 4축 고정

대상 파일:

* 신규 `src/demo/outcome-summary.ts`
* `src/recognizer/types.ts`

완료 기준:

* family와 outcome 차이가 타입 단계에서 분리됨

현재 상태:

* 완료
* `src/demo/outcome-summary.ts`에서 실제 구현됨

### H03. 품질벡터 on/off 토글 추가

목표:

* 품질벡터 개입 여부를 직접 비교 가능하게 만들기

세부 작업:

* `qualityInfluence` UI state 추가
* quality compare card 추가
* on/off 상태 뱃지와 delta 표시 추가

대상 파일:

* `src/app.ts`
* `src/style.css`

완료 기준:

* 같은 입력에 대해 quality off / on 차이가 즉시 보임

현재 상태:

* 완료
* compare card와 delta 강조까지 포함

### H04. explain / why 패널 추가

목표:

* 결과 이유를 짧은 문장으로 설명

세부 작업:

* recognized / ambiguous / incomplete / invalid 설명 강화
* quality delta 자연어 설명 추가
* top candidate와 margin 설명 추가

대상 파일:

* `src/app.ts`
* 필요 시 신규 `src/demo/explain.ts`

완료 기준:

* 숫자만 보지 않아도 데모 시 설명 가능

현재 상태:

* 완료
* `src/demo/explain.ts`에서 상태 설명과 quality 영향 설명을 생성

### H05. 모드 프리셋과 토글 그룹화

목표:

* 정보 밀도를 사용자 선택으로 조절

세부 작업:

* `clean`, `explain`, `workshop` 프리셋 추가
* 토글 간 연동 규칙 정의

예:

* `clean` -> explain off, analysis off
* `explain` -> quality on, why on
* `workshop` -> 전부 on

대상 파일:

* `src/app.ts`
* `src/style.css`

완료 기준:

* 클라이언트 앞에서 정보 과밀과 정보 부족을 모두 피할 수 있음

현재 상태:

* 완료
* preset이 manual toggle보다 먼저 보이고, toggle은 preset 이후 세밀 조정 가능

### H06. guided scenario와 appeal 요소 추가

목표:

* 첫 시연에서 바로 보여 줄 포인트 제공

세부 작업:

* guided scenario chip 4개 이상
* same shape guarantee badge 추가
* outcome fingerprint 또는 narration strip 중 하나 추가

대상 파일:

* `src/app.ts`
* `src/style.css`

완료 기준:

* 처음 보는 사람도 무엇을 봐야 하는지 알 수 있음

현재 상태:

* 완료
* same shape guarantee, guided scenario chip 4개, narration strip, outcome fingerprint 포함

### H07. recent seals / quick compare 추가

목표:

* 방금 전 결과와 지금 결과를 비교 가능하게 만들기

세부 작업:

* 최근 seal 3~5개 미니 목록 추가
* quality on/off 또는 tempo 차이 비교를 쉽게 함

대상 파일:

* `src/app.ts`

완료 기준:

* `why previous vs now` 비교가 가능해짐

현재 상태:

* 완료
* 최근 3개 슬롯과 current vs previous summary까지 포함

### H08. 수동 검증과 데모 스크립트 정리

목표:

* 클라이언트 시연 흐름을 고정

세부 작업:

* 3분 시연 스크립트 작성
* HCI 관점 체크리스트 작성
* `clean -> explain -> workshop` 순서 검증

대상 파일:

* 필요 시 `README.md`
* 필요 시 별도 문서 또는 본 문서 부록

완료 기준:

* 누구든 같은 순서로 데모 가능

현재 상태:

* 부분 완료
* `README.md`에 사용 방법과 최소 시연 순서가 반영됨
* 별도 전용 데모 스크립트 문서는 아직 없음

---

## 10. 수동 검증 시나리오

### 시나리오 A. quality off / on 비교

* 불꽃 삼각형을 그림
* `quality vector use off` 상태로 seal
* `quality vector use on` 상태로 동일 family 입력을 다시 seal
* 결과:
  * family는 둘 다 `fire`
  * outcome card만 달라짐

### 시나리오 B. clean / workshop 비교

* `clean` 모드에서 입력
* 같은 입력을 `workshop` 모드에서 다시 확인
* 결과:
  * 기본 입력 경험은 유지
  * workshop에서 analysis overlay와 detail panel이 열림
  * compare / why는 explain 또는 workshop에서 집중적으로 사용

### 시나리오 C. status explanation

* incomplete 케이스 입력
* why panel 확인
* 결과:
  * 단순히 `incomplete`라고만 쓰지 않고 이유를 보여 줌

### 시나리오 D. same shape guarantee

* 빠른 불꽃
* 느린 불꽃
* 결과:
  * family는 동일
  * quality delta만 차이
  * 보증 뱃지와 설명 문구가 일관됨

### 시나리오 E. first-time demo guidance

* guided scenario chip를 확인
* 시연자가 칩 순서대로 진행
* 결과:
  * 별도 구두 설명 없이도 시연 흐름이 잡힘

---

## 11. 주요 리스크와 대응

### 리스크 1. 토글이 많아져 화면이 더 복잡해지는 문제

대응:

* 모드 프리셋 우선
* 개별 토글은 workshop에서만 적극 노출

### 리스크 2. quality on/off가 core rules를 바꾸는 것처럼 보이는 문제

대응:

* family badge를 고정 표시
* outcome compare card를 별도 영역으로 분리
* `quality affects execution, not family` 문구 고정

### 리스크 3. 너무 많은 설명이 디버그 화면처럼 보이는 문제

대응:

* explain panel은 짧은 문장 위주
* raw numeric debug는 workshop 모드에만 집중

### 리스크 4. 어필 요소가 실제 제품 가치로 이어지지 않는 문제

대응:

* 모든 appeal 요소를 재사용 가능한 UX 자산으로 제한
* 과장된 시네마틱보다 비교, 설명, 선택권 위주로 설계

---

## 12. 완료 정의

이번 문서 기준의 완료는 아래를 모두 만족하는 상태입니다.

* `quality vector use` on/off가 Web UI에서 직접 비교된다
* `clean`, `explain`, `workshop` 프리셋이 존재한다
* 분석 정보는 토글로 켜고 끌 수 있다
* why panel 또는 동등한 설명 패널이 존재한다
* same shape guarantee가 시각적으로 전달된다
* 클라이언트 시연용 guided scenario가 존재한다
* 최근 결과 비교 또는 동등한 quick compare 수단이 있다

현재 구현은 위 완료선에 도달한 상태로 본다.
다만 별도 시연 스크립트 문서는 아직 분리되어 있지 않으므로, 실제 시연 순서는 `README.md`의 데모 사용 방법을 우선 기준으로 삼는다.

# HCI UX 데모 프롬프트 pack

이 문서는 `docs/20_queue/hci-ux-demo-plan.md`를 실제 작업으로 옮길 때 사용할 프롬프트 묶음입니다.

이번 pack의 초점은 recognizer 알고리즘 자체보다 **설명 가능한 UX 데모 레이어**입니다.
특히 아래를 직접 시연 가능하게 만드는 것이 목표입니다.

* 품질벡터 사용 on/off
* 같은 모양은 같은 family
* 분석 정보의 on/off
* clean / explain / workshop 모드 전환

---

## 1. 사용 원칙

모든 프롬프트는 아래 전제를 공유합니다.

* 현재 Web UI와 recognizer를 출발점으로 사용한다.
* family recognition 원칙은 바꾸지 않는다.
* `quality vector use` 토글은 family 판정을 바꾸는 토글이 아니다.
* 품질벡터 on/off는 demo outcome layer 비교에 우선 적용한다.
* 기본 화면은 깔끔하게, 분석 화면은 선택적으로 제공한다.
* 설명은 숫자 나열보다 짧고 해석 가능한 문장 위주로 만든다.

구현 전에 반드시 읽을 파일:

* `docs/20_queue/hci-ux-demo-plan.md`
* `docs/20_queue/prototype-implementation-plan.md`
* `src/app.ts`
* `src/style.css`
* `src/recognizer/recognize.ts`
* `src/recognizer/quality.ts`
* `src/recognizer/types.ts`

---

## 2. 통합 프롬프트

```text
현재 레포의 Web UI demo를 HCI 검토용 UX demo로 확장하라.

핵심 목표:
1. quality vector use on/off 토글을 추가한다.
2. 이 토글은 family recognition이 아니라 demo outcome layer 비교에 적용한다.
3. clean / explain / workshop 3가지 view preset을 추가한다.
4. explain result 토글 또는 동등한 why panel을 추가한다.
5. analysis overlay를 기존 debug toggle보다 더 명확한 UX로 정리한다.
6. same shape, same family 보증 문구 또는 배지를 넣는다.
7. guided demo scenario chip를 추가한다.
8. recent seals 또는 동등한 quick compare UI를 추가한다.

필수 제약:
* 같은 모양은 같은 family 원칙을 깨면 안 된다.
* quality on/off는 family를 바꾸면 안 된다.
* 기본 화면은 과도하게 복잡해지면 안 된다.
* 원본 stroke는 유지하고 ghost overlay로만 보조한다.
* 설명 UI는 현재 recognizer 구조와 모순되면 안 된다.

권장 구현 순서:
1. demo view state와 outcome summary 타입을 정의한다.
2. quality on/off compare card를 만든다.
3. explain / why panel을 추가한다.
4. view preset을 추가한다.
5. guided scenario와 same shape guarantee를 추가한다.
6. 필요하면 recent seals 비교 UI를 추가한다.
7. npm run build와 가능한 테스트를 수행한다.

결과는 클라이언트 앞에서 3분 안에 설명 가능한 UI여야 한다.
```

---

## 3. 단계별 프롬프트 A. demo state와 outcome layer

```text
현재 Web UI에 HCI 데모용 상태 모델과 outcome summary 레이어를 추가하라.

필수 요구:
* DemoViewState를 추가한다.
* 최소 상태는 debugOverlay, qualityInfluence, explainMode, guidanceMode, viewPreset를 포함한다.
* recognizer result와 별도로 DemoOutcomeSummary를 만든다.
* DemoOutcomeSummary는 family, qualityEnabled, output, control, stability, risk, explanation을 가진다.

중요:
* 이 레이어는 recognizer semantics를 대체하지 않는다.
* 설명 가능한 비교 UI를 위한 파생 데이터여야 한다.

작업 대상:
* src/app.ts
* 필요 시 src/demo/outcome-summary.ts
* 필요 시 src/recognizer/types.ts
```

---

## 4. 단계별 프롬프트 B. quality vector use on/off

```text
quality vector use on/off 토글과 quality compare card를 추가하라.

목표:
* 같은 입력에 대해 quality off / quality on 결과를 비교 가능하게 만든다.
* family는 동일하게 유지한다.
* quality off에서는 canonical baseline outcome을 보여 준다.
* quality on에서는 quality vector 기반 delta를 outcome card에 반영한다.

결과 카드 최소 항목:
* output
* control
* stability
* risk

설명 규칙:
* quality on/off가 family를 바꾸지 않는다는 문구를 고정 표시한다.
* delta가 생긴 항목만 강조한다.

작업 대상:
* src/app.ts
* src/style.css
* 필요 시 src/demo/outcome-summary.ts
```

---

## 5. 단계별 프롬프트 C. explain / why panel

```text
결과 이유를 설명하는 explain panel 또는 why panel을 추가하라.

필수 요구:
* recognized / ambiguous / incomplete / invalid 상태의 이유를 짧게 설명한다.
* top score, margin, closure 등 핵심 근거를 사람이 읽기 쉬운 문장으로 바꾼다.
* quality on 상태에서는 어떤 품질 벡터가 어떤 outcome 변화로 이어졌는지도 설명한다.

예시 출력 톤:
* shape fixed as fire
* quality increased output but reduced stability
* incomplete because closure is still open

제약:
* 디버그 숫자 나열처럼 보이면 안 된다.
* 완전히 새로운 의미를 만들어 내면 안 된다.

작업 대상:
* src/app.ts
* 필요 시 src/demo/explain.ts
* src/style.css
```

---

## 6. 단계별 프롬프트 D. view preset과 토글 정리

```text
현재 데모 UI에 clean / explain / workshop 프리셋을 추가하라.

목표:
* 정보 밀도를 preset으로 조절할 수 있게 한다.
* 개별 토글보다 preset이 먼저 보이게 한다.

권장 규칙:
* clean: 설명/분석 최소화
* explain: why panel과 quality compare를 켠다
* workshop: analysis overlay와 세부 정보까지 모두 켠다

추가 요구:
* 개별 토글은 preset 변경 후에도 세밀 조정 가능하게 할 수 있다.
* 토글 영역은 하나의 그룹으로 정리한다.

작업 대상:
* src/app.ts
* src/style.css
```

---

## 7. 단계별 프롬프트 E. guided scenario와 appeal 요소

```text
클라이언트 시연용 guided scenario와 기본 appeal 요소를 추가하라.

필수 요소:
* same shape, same family 배지
* guided scenario chip 최소 4개
* quality affects execution, not family 문구

권장 시나리오:
* 빠른 불꽃
* 느린 불꽃
* closure leak
* rotation bias

여유가 있으면 추가:
* narration strip
* outcome fingerprint
* subtle reveal animation

중요:
* 과장된 시네마틱은 피한다.
* 실제 규칙 설명에 도움이 되는 appeal 요소만 넣는다.

작업 대상:
* src/app.ts
* src/style.css
```

---

## 8. 단계별 프롬프트 F. recent seals와 quick compare

```text
recent seals 또는 동등한 quick compare UI를 추가하라.

목표:
* 방금 전 시도와 현재 시도를 바로 비교할 수 있게 한다.
* HCI 검토에서 replay 없이도 차이를 읽을 수 있게 만든다.

최소 요구:
* 최근 3개 이상의 seal 결과를 보여 준다.
* family, status, quality on/off delta 또는 핵심 설명을 표시한다.

중요:
* 로그 뷰어 전체를 읽지 않아도 비교가 가능해야 한다.

작업 대상:
* src/app.ts
* src/style.css
```

---

## 9. 단계별 프롬프트 G. 검증과 마감

```text
이번 HCI UX demo 확장 작업을 마감 검증하라.

체크 항목:
* quality vector use on/off가 직접 비교되는가
* family는 on/off와 관계없이 유지되는가
* clean / explain / workshop 프리셋이 동작하는가
* explain 또는 why panel이 존재하는가
* same shape guarantee가 보이는가
* guided scenario가 존재하는가
* recent seals 또는 quick compare가 존재하는가

실행:
* 가능한 테스트를 실행한다
* npm run build를 실행한다

보고 형식:
* 구현된 항목
* 검증 결과
* 남은 UX 리스크
```

---

## 10. 문서 업데이트 프롬프트

```text
방금 구현한 HCI UX demo 확장 내용을 문서에 반영하라.

필수 반영:
* README.md의 데모 사용 방법 보강
* docs/20_queue/hci-ux-demo-plan.md의 실제 구현 상태 반영
* 필요하면 docs/20_queue/README.md 링크 및 읽기 순서 보강

문서 원칙:
* 한국어 유지
* HCI 검토용 기능과 core recognizer 기능을 혼동하지 않게 적는다
* 문서가 실제 구현보다 앞서 나가지 않게 한다
```

---

## 11. 최종 검수 프롬프트

```text
현재 작업 트리를 기준으로 HCI UX demo 목표가 충족됐는지 최종 점검하라.

핵심 질문:
* 처음 보는 클라이언트가 품질벡터 역할을 이해할 수 있는가
* 분석 정보를 켜고 끄는 선택권이 분명한가
* 같은 모양은 같은 family라는 신뢰성이 시각적으로 전달되는가
* 현재 화면이 디버그 툴이 아니라 설명 가능한 UX demo처럼 보이는가

출력 형식:
* 충족된 점
* 부족한 점
* 가장 효과적인 후속 개선 3개 이내
```

---

## 12. 권장 실행 순서

가장 권장하는 실제 사용 순서는 아래입니다.

1. 통합 프롬프트로 전체 구조를 먼저 반영
2. 단계별 프롬프트 B, C, D로 핵심 UX 비교 구조를 보강
3. 단계별 프롬프트 E, F로 시연성과 비교성을 추가
4. 단계별 프롬프트 G로 검증
5. 문서 업데이트 프롬프트와 최종 검수 프롬프트 수행

이 순서를 따르면, 현재 Web UI는 recognizer 검증 화면에서 **클라이언트용 HCI UX demo**로 확장될 수 있습니다.

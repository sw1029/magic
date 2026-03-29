request


현재까지 논의된 사항 일괄 재검토하여 최종 정리(특허 관련 논의는 제외).
---



answer



아래를 지금까지의 논의를 반영한 **마법진 작성 체계 최종 정리본**으로 둡니다.
특허 관련 논의는 제외하고, **언어체계·컴파일 구조·물리계층·개인화·UX·밸런스 원칙**까지 한 번에 묶어 정리합니다.

---

# 1. 최종 목표

이 체계의 핵심 목표는 다음 하나로 압축됩니다.

**유저가 유한한 primitive와 문법 위에서, 품질·조합·환경·3D 변환을 활용해 자기만의 마법을 실험·발견·숙련할 수 있는 비선형 마법 언어를 제공하는 것.**

여기서 동시에 지켜야 할 조건은 네 가지입니다.

1. **예측 가능성**

   * 같은 구조는 같은 주문이어야 한다.
   * 시스템이 멋대로 다른 주문으로 읽으면 안 된다.

2. **장인정신**

   * 단순 버튼 입력이 아니라, 직접 설계하고 다듬는 감각이 있어야 한다.
   * 손맛은 살아 있어야 하지만 손기술 세금이 되면 안 된다.

3. **공유 재현성**

   * 다른 유저가 같은 구조를 따라 만들면 같은 계열 결과를 얻어야 한다.
   * 개인차는 “실행 accent”에서만 살아야 한다.

4. **창발성**

   * 조합이 단순 레시피 암기가 아니라, 실제로 새로운 현상을 발견하는 경험으로 이어져야 한다.
   * 이를 위해 언어 자체만이 아니라, 공통 물리계층과 실험 UX가 필요하다.

---

# 2. 최상위 설계 원칙

## 2.1 이 언어의 정체성

이 체계는 **2D 중심의 비선형 표의(semasiographic) 공간 언어**입니다.

즉,

* 말소리를 적는 문자체계가 아니라
* **의미를 도형·배치·관계로 직접 표현하는 언어**입니다.

## 2.2 2D가 본체, 3D는 확장

* **2D가 core language**
* **3D는 2D 의미의 상위 굴절형(aspectual extension)**

3D는 독립 어휘 체계가 아닙니다.
2D에서 확정된 구조를 어떻게 들어올리고, 누적하고, 연결하고, 유지하느냐를 바꾸는 층입니다.

## 2.3 의미와 솜씨의 분리

* **주문의 정체성**은 canonical structure에서 결정
* **시전의 성능/질감**은 품질·tempo·환경·개인 작법에서 결정

즉,

* shape는 “무슨 주문인가”
* execution은 “그 주문이 얼마나 안정적이고 효율적이며 위험한가”

를 담당합니다.

## 2.4 AI는 보조자

ML/AI는 다음에만 사용합니다.

* primitive proposal ranking
* ambiguity scoring
* user adaptation
* beautification hint
* parser prior

반대로 최종 의미 해석은 **deterministic grammar/compiler**가 담당합니다.

---

# 3. 세계관 기반 의미 프레임

## 3.1 마법의 원리

* 세계는 누군가의 꿈
* 마법은 그 **꿈결**을 응집하여 사용하는 행위
* 마법진은 꿈결을 모으고 정렬하고 방출하는 **응집기**
* 사용자는 정신력/체력을 대가로 지불
* 과부하 시 사용자 역시 꿈결로 흩어질 수 있음

## 3.2 학파

학파는 “다른 언어”가 아니라, **같은 언어를 해석하는 다른 세계관적 해석 프레임**입니다.

예:

* 길몽
* 악몽
* 몽중몽
* 곁잠

최종 원칙은 다음과 같습니다.

* **2D에서는 학파가 core semantics를 바꾸지 않음**
* **2D에서는 주로 이펙트 질감·부가 경향·사운드·accent만 변화**
* **3D에서는 학파가 operator 해석을 강하게 바꿈**

즉, 2D는 공유 문법, 3D는 학파별 극적 차이입니다.

---

# 4. 최종 언어 구조

## 4.1 내부 표현

```text
SpellIR =
< RootSeed,
  MaterialDerivation?,
  OnticAspect?,
  DeliveryPraxis?,
  ProcessGraph,
  ScopePorts,
  School2DAccent?,
  CheckCode,
  SealState,
  LayerOps*,
  School3DProfile? >
```

## 4.2 핵심 읽기 순서

이 언어는 좌→우가 아니라 아래 순서로 읽습니다.

1. **중심 root**
2. **파생/상태 ring**
3. **작동 문법 ring**
4. **범위/대상 port**
5. **외곽 seal / checksum / 학파 accent**
6. **3D lift**

즉,
**무엇이 응집되었는가 → 어떤 방식으로 변형되었는가 → 어떻게 작동하는가 → 어디에 작동하는가 → 어떤 상태로 봉인되었는가 → 어떻게 확장되었는가**
의 순서입니다.

---

# 5. 핵심 분류 체계

## 5.1 Root Seed: 중심 매질

이 다섯 개만 center-root로 허용합니다.

| 분류   | 항목     | 의미             |
| ---- | ------ | -------------- |
| Root | 바람     | 이동, 편류, 전달, 개방 |
| Root | 땅      | 지지, 질량, 구조, 정착 |
| Root | 불꽃     | 방출, 상승, 전환, 격발 |
| Root | 물      | 흐름, 순환, 적응, 누수 |
| Root | 식물(생명) | 발아, 생장, 회복, 얽힘 |

이 다섯 개는 “원소 카드”가 아니라 **꿈결의 응집 매질**입니다.

## 5.2 Material Derivation

| 항목 | 역할             |
| -- | -------------- |
| 독  | 부패, 침식, 누출, 오염 |
| 강철 | 강화, 단련, 구조 고정  |
| 전기 | 위상 긴장, 방전, 도약  |
| 얼음 | 유동의 정지, 응결, 구속 |

원칙:

* 파생은 항상 **부모 흔적을 유지**
* 독립 원소처럼 보이면 안 됨

## 5.3 Ontic Aspect

| 항목 | 역할                  |
| -- | ------------------- |
| 영혼 | 비물질적 존재, 이탈된 생기     |
| 사념 | 내향 집중, 정신 반향, 재귀    |
| 신성 | 정합, 정제, 순화, 축복 질서   |
| 無  | 결락, 비움, 소거, 매질의 공백화 |

여기서 **無는 root가 아니라 overlay**입니다.
즉, ordinary element처럼 center slot에 두지 않습니다.

## 5.4 Delivery / Praxis

| 항목 | 역할                                                     |
| -- | ------------------------------------------------------ |
| 염동 | 비접촉 조작, 원격 전달                                          |
| 武  | 無의 1차 praxis 변형. 외부 매질을 비운 뒤 body/weapon axis로 환원된 실행형 |

즉,

* `無` = 외부 매질의 비움
* `武` = 그 비워진 매질을 육체/접촉축으로 되돌린 실행 방식

이 둘은 root가 아니라 **실행 방식 계층**입니다.

---

# 6. 시각 층위

이 구조는 확정입니다.

| 층                   | 역할                                        |
| ------------------- | ----------------------------------------- |
| center seed chamber | root seed                                 |
| aspect ring         | material / ontic / delivery-praxis        |
| process ring        | primitive 문법                              |
| scope / port ring   | 방향, 대상, 범위                                |
| covenant rim        | seal, checksum, 학파 accent                 |
| 3D lift layer       | stack / extrusion / orbit / tilt / bridge |

심볼의 **구체 도형 실루엣은 보류**하지만, 위 층위 구조와 역할은 고정입니다.

---

# 7. Core Primitive 문법

이 10개는 언어의 closed-class grammar입니다.

| Primitive                  | 의미                                   |
| -------------------------- | ------------------------------------ |
| 핵(nucleus)                 | source, focus, origin                |
| 경계(boundary/container)     | containment, stabilization, ward     |
| 개구(aperture)               | release, admission, leak             |
| 경로(path/ray)               | transfer, propulsion, beam           |
| 결속(link/tether)            | binding, channeling                  |
| 차단(block/cross)            | oppose, sever, cancel                |
| 축(axis/mirror)             | balance, duality, reflection         |
| 분기(branch)                 | propagation, multiplicity, diffusion |
| 반복(echo/concentric repeat) | amplification, resonance             |
| 자기유사(fractal echo)         | recursion, chained reapplication     |

원칙:

* primitive는 “주문 이름”이 아니라 **작동 방식의 재료**
* 한 atomic spell은 root를 중심으로 primitive graph를 형성
* fractal은 기본적으로 제한적으로만 사용
* 전투는 적은 primitive 수, forge는 더 큰 graph 허용

---

# 8. 관계 문법과 다중 마법진

복합 마법은 한 원 안에 우겨 넣지 않고, **개별 마법진을 먼저 seal**한 뒤 **관계 그래프**로 조합합니다.

## 8.1 기본 관계

* containment
* tangency
* intersection
* concentricity
* bridge
* phase order

## 8.2 하이퍼엣지

세 개 이상 관계는 hyperedge로 다룹니다.

## 8.3 철학

자유도는

* “아무거나 인식”이 아니라
* **“primitive와 relation 안에서 조합 가능”** 에서 나와야 합니다.

---

# 9. 3D operator

3D operator는 전부 채택합니다.

| Operator             | 의미                       |
| -------------------- | ------------------------ |
| 적층(stack)            | 우선순위, 인과 계층, 지속성         |
| 압출(extrusion)        | 용량, 지속 시간, chamber화, 구속력 |
| 공전(revolution/orbit) | 순환 유지, 와류, 지속 갱신         |
| 기울임(tilt)            | 방향성 바이어스, 특정 평면 목표화      |
| 교량(bridge/interlock) | 층간 전달, 변환, relay         |

원칙:

* 3D는 **sealed 2D spell에만 적용**
* 3D는 새 root를 만들지 않음
* 3D는 stat booster가 아니라 **coupling topology changer**
* 모든 3D 값은 양자화된 단계만 허용
* 3D는 가능하면 forge/preset 중심, combat에서는 제한적으로

---

# 10. 학파의 최종 역할

## 10.1 2D

2D에서는 학파가 바꾸는 것은 주로 아래입니다.

* 이펙트 질감
* 부가 효과 경향
* 품질 trade-off
* 오디오 운율/음색
* 외곽 accent

즉, 2D core semantics는 유지합니다.

## 10.2 3D

3D에서는 학파가 **operator interpretation profile**로 적극 개입합니다.

예:

* 길몽: 정렬, 정화, 보호 chamber, 조화로운 layer coupling
* 악몽: 압박, 오염 전이, 감금형 extrusion, 왜곡된 orbit
* 몽중몽: 중첩, 재귀, loop retention, nested stack
* 곁잠: relay, 기생적 bridge, 측면 개입, 공유 chamber

즉, **학파는 2D의 의미를 바꾸지 않고, 3D의 작동 방식을 비틀어 준다**고 정리합니다.

---

# 11. 오류 정정과 판정 구조

## 11.1 핵심 원칙

* 잘못된 성공보다 설명 가능한 실패
* 작은 실수로 다른 주문이 되지 않게 할 것
* identity와 quality를 분리할 것
* correction은 좁게, parse ambiguity는 넓게 유지할 것

## 11.2 identity / quality 분리

### identity 계열

* topology
* root family trace
* slot type consistency
* required primitive presence
* family seam
* port arity
* layer seal

### quality 계열

* closure
* symmetry
* smoothness
* order
* tempo
* overshoot
* phase
* spacing
* constraint fidelity

## 11.3 보정 규칙

1. 같은 슬롯 타입 안에서만 최근접 후보 탐색
2. 파생은 부모 흔적이 있을 때만 승급
3. 임계 이하만 보정
4. 그 외는 unstable / incomplete spell

예:

* 물 비슷하다고 얼음으로 강제 승급 금지
* 바람 비슷하다고 영혼으로 오인식 금지
* 無 overlay가 있다고 武로 오인식 금지

## 11.4 checksum

외곽 장식이면서 동시에 문법 검증 신호로 사용합니다.

예:

* family seam
* parity rule
* stabilizer count
* port signature
* layer seal

이들은 QR코드처럼 보이면 안 되고, **의식적 봉인 장식**처럼 보여야 합니다.

## 11.5 사용자 피드백 형식

오류는 “틀렸습니다”가 아니라 **syndrome** 형태로 설명해야 합니다.

예:

* “응결 축은 감지되나 유동 흔적이 충분히 봉인되지 않았습니다.”
* “정신 반향은 있으나 이탈된 존재 표지가 부족합니다.”
* “null overlay는 확인되나 체현 축이 없어 武로 확정되지 않습니다.”

---

# 12. 품질 벡터

최종 품질 평가는 분리 벡터로 관리합니다.

```text
Q =
[q_closure,
 q_sym,
 q_smooth,
 q_topo,
 q_constraint,
 q_order,
 q_tempo,
 q_overshoot,
 q_phase]
```

원칙:

* 모든 주문에 똑같은 선형 보너스로 쓰지 않음
* family마다 중요 품질이 다름

예:

* barrier/ward 계열: closure, sym, constraint
* beam/shot 계열: tempo, phase, path consistency
* growth/ritual 계열: order, stability, fractal coherence

즉, 손기술이 만능 버프가 되면 안 됩니다.

---

# 13. 컴파일 구조

최종 파이프라인은 아래로 확정합니다.

```text
digital ink
→ primitive fitting
→ candidate family detection
→ typed slot parsing
→ canonical derivation expansion
→ checksum / type validation
→ n-best parse 유지
→ seal
→ canonical SpellIR 확정
→ graph compile
→ optional 3D lift
→ ignite
→ deterministic simulation
```

핵심 원칙:

* raw stroke를 바로 class로 찍지 않음
* 하나의 z*를 draw 중에 계속 확정하지 않음
* **seal 시점**에만 canonicalize
* SpellID는 canonical representation의 hash
* environment는 identity보다 parameter에 영향
* graph compile 이후 런타임은 deterministic

---

# 14. 물리계층 최종 초안

이 부분이 창발성을 만드는 핵심입니다.

## 14.1 물리계층의 정체성

물리계층은 **공통 상태변수 위에서 작동하는 layered 2.5D 매질 시뮬레이션**으로 둡니다.

즉,

* 언어는 “무엇을 하려는가”
* 물리는 “그것이 세계에서 어떻게 흐르고 충돌하고 남는가”
  를 담당합니다.

## 14.2 공간 표현

* 완전 자유 3D voxel이 아니라
* **2D 셀/내비메시 기반 + 다층 레이어 구조**
* 활성 주문은 particle preset이 아니라 **local operator kernel**로 작동

## 14.3 지속 채널

아래 7개를 persistent channel로 둡니다.

| 기호       | 의미        |
| -------- | --------- |
| (\rho)   | 꿈결 밀도     |
| (\kappa) | 정합도 / 응집도 |
| (\phi)   | 위상        |
| (\tau)   | 긴장 / 응력   |
| (\chi)   | 오염도       |
| (\nu)    | 공백도 / 무화압 |
| (\beta)  | 생장도 / 생기  |

## 14.4 파생 채널

* (\mathbf u): 흐름/운반 벡터
* (P): 투과/차단 계수
* (g): 루프 이득 / 공명 gain

## 14.5 물리 원칙

* 전투 시간척도에서는 **반-보존적**
* 대부분은 생성보다 **모으기·이동·봉인·방출·누수·비움**
* 환경은 단순 damage tag가 아니라 상태변수에 직접 개입
* 동일한 2D spell은 동일 기본 거동
* 환경/품질/개인화/3D/다중 마법진이 같은 상태변수에 얹힘

---

# 15. 언어 요소와 물리의 연결

## 15.1 Root의 물리 역할

* 바람: 이동성, 편류, 전달
* 땅: 정합, 차단, 기반성
* 불꽃: 방출, 긴장 축적, 격발
* 물: 평형화, 순환, 누수
* 생명: 증식, 재생, 얽힘

## 15.2 파생/상태의 물리 역할

* 독: 오염 증가, 구조 침식
* 강철: 정합과 차단 강화, 대신 취성 위험
* 전기: 위상차 기반 순간 방전
* 얼음: 흐름 정지, 고정, 구속
* 영혼: 물질보다 존재/의지 채널과 강한 결합
* 사념: 위상 정렬, 내향 반향, 원격성
* 신성: 정합 강화, 오염 억제, 정제
* 無: 매질 결합 차단, null pocket
* 염동: 원격 전달 경로 재배선
* 武: 외부 field를 body-axis throughput으로 환원

특히

* **無와 武는 만능 우회키가 되면 안 됨**
* 武는 짧은 범위, 높은 자기 리스크, 낮은 외부 shaping 자유도를 가져야 함

## 15.3 primitive의 물리적 해석

* nucleus = source/sink
* boundary = 막 경계
* aperture = gate
* path = 방향 운반장
* link = conduit
* block = impedance/cancel
* axis = phase-lock / mirror
* branch = flux split
* echo = delay / reinjection
* fractal = attenuated subkernel

---

# 16. 이벤트 추출층

연속장만으로는 플레이어가 배우기 어렵기 때문에, 이산 사건을 뽑아내는 **event extraction layer**를 둡니다.

대표 이벤트:

* Seal Achieved
* Leak
* Phase Lock
* Resonance
* Rupture
* Taint Breach
* Null Collapse
* Bloom
* Relay Established

이 이벤트들은 아래에 모두 쓰입니다.

* VFX/SFX
* 리플레이
* codex 기록
* syndrome 설명
* diff UI
* AI 도우미 설명

즉, 물리계층은 계산 엔진일 뿐 아니라 **학습 가능한 사건 생산기**여야 합니다.

---

# 17. 피드백 루프 구조

창발성을 위해 명시적으로 지원해야 하는 핵심 루프는 아래 여섯 가지입니다.

1. **Containment Loop**
   경계/땅/신성 → 정합 증가 → retention 증가 → 더 강한 containment

2. **Release Loop**
   불꽃/전기 → 밀도→긴장 전환 → discharge → 큰 효과

3. **Circulation Loop**
   물/바람/path/orbit → 순환 유지 → sustain 강화

4. **Growth Loop**
   생명/echo/fractal → 생장도 증식 → 더 많은 local effect

5. **Corruption Loop**
   독/악몽 → 오염 증가 → 구조 붕괴 → 더 많은 오염

6. **Null Loop**
   無 → recruitment/coupling 차단 → 기존 loop 정지 또는 붕괴

이 루프들이 서로 교차해야 **레시피가 아니라 현상**이 생깁니다.

---

# 18. 잔재(residue)

실패는 무가치한 불발이 아니라 **연구 가능한 부산물**을 남겨야 합니다.

예:

* leak plume
* fracture shell
* phantom orbit
* null scar
* parasitic sprout
* arc remnant

단, 잔재는 강력할 수 있어도 **불안정하고 비용이 크며 맥락 의존적**이어야 합니다.
그렇지 않으면 의도적 miscast가 메타가 됩니다.

---

# 19. 개인화 구조

현재 구조에서 개인화는 강하게 필요하지만, 의미론에는 거의 들어가면 안 됩니다.

## 19.1 최상위 원칙

**개인차는 “같은 주문을 어떻게 더 자기답게 실행하는가”를 바꿔야지, “같은 그림이 무슨 주문인가”를 바꾸면 안 됩니다.**

## 19.2 적용 강도

| 층                          | 개인화 강도 |
| -------------------------- | -----: |
| SpellID / core semantics   |      0 |
| parser / candidate ranking |   매우 강 |
| 실시간 feedback               |      강 |
| runtime accent             |    약~중 |
| codex / 장기 작법 정체성          |    중~강 |

## 19.3 세 가지 개인화 프로필

### Session Motor Model

오늘 컨디션과 입력 습관

* 평균 tempo
* pause density
* slant
* oversketch tendency
* jitter

용도:

* parser prior
* today-specific feedback
* 입력 보정

### Family Habit Model

family별 작법 습관

* barrier 계열 closure 스타일
* fire/electric burst tempo
* water/air sweep regularity
* life/soul pulse rhythm

용도:

* family-specific comfort band
* family별 guidance
* 작은 style accent

### Long-term Soulprint

장기 작법 정체성

* containment 선호
* release 선호
* recursion 선호
* relay 선호
* null/body-channel 성향

용도:

* 작은 runtime trade-off
* codex lineage
* forge 추천
* 학파 공명 경향

## 19.4 속도와 각도의 처리

### 속도

속도는 두 번 쓰면 안 됩니다.

* 샘플링/점밀도 문제로서의 속도는 전처리에서 정규화
* 의식적 시전 tempo는 execution accent로 사용

즉, tempo는

* latency
* stability
* phase quality
* backlash risk
  같은 family 내부 특성으로만 작동해야 합니다.

### 각도

각도도 분리합니다.

* 전역 기울기 = normalize
* 문법 슬롯에 명시된 방향 = semantic
* 손버릇 slant = parser prior

즉,

* aperture 방향
* path 방향
* axis 방향
* orbit 회전 방향
* tilt 방향
  같은 **문법적 각도만 의미**가 됩니다.

## 19.5 금지

* 유저마다 같은 shape가 다른 주문이 되는 것
* 전역 slant를 의미로 읽는 것
* 상대평가만으로 나쁜 습관이 곧 최적화되는 것
* 실패 드로잉이 장기 프로필을 오염시키는 것
* 개인화가 武/無를 만능 메타로 만드는 것

---

# 20. 음운 / 사운드 구조

음운은 채택하되, **시각 언어의 보조 채널**로 둡니다.

역할:

* 마법 구분
* 인덱싱
* 기억 보조
* 시전 감각 강화
* 학파 분위기 차등
* 3D 확장의 청각적 체감

매핑 원칙:

* 모음 계열 = root seed
* 발단/onset = process type
* 종성/노이즈 = derivation
* 운율 = school accent
* 공간화/잔향 = 3D operator

즉, 시각 구조를 **청각적으로 중복 강화**하는 방향입니다.

---

# 21. Combat / Forge 분리

이 구조는 반드시 **combat** 와 **forge** 를 분리합니다.

## Combat

* 낮은 primitive 수
* 작은 graph
* 짧은 seal/ignite
* 제한된 3D
* 은유적 피드백 중심

## Forge

* 큰 graph
* 다중 원 배치
* 2.5D/3D 실험
* 환경 스윕
* 리플레이/비교/주석
* preset sealing
* full telemetry

중요한 점:

* **물리법칙은 동일**
* 다만 **가시성, 시간 여유, 복잡도 허용치가 다름**

---

# 22. UX 관점 최종 판정

## 22.1 현재 구조가 이미 충족한 것

* 예측 가능성
* 공유 재현성
* 2D 중심 학습 가능성
* 고유한 세계관적 문법
* 손맛과 정체성의 분리 가능성
* 다중 마법진/3D 확장의 구조적 타당성

## 22.2 아직 언어만으로는 부족한 것

지금 구조만 구현하면 **풍부한 DSL** 은 되지만, 자동으로 **고도의 창발적 경험** 이 되지는 않습니다.

진짜 창발 UX를 위해서는 아래 HCI 층이 필수입니다.

* seal 단위 자동 버전 노드
* branch / merge / compare
* replay / trace
* 환경 스윕
* lineage codex
* 가까운 유효 변형(nearby transform) 제안
* 실패도 기록되는 연구 로그

즉, 창발성은 언어만이 아니라 **실험도구 UX** 에서 완성됩니다.

## 22.3 최종 판단

현재 구조는

* “새로운 마법을 쓸 수 있는 언어”로서는 충분히 강하고
* “새로운 현상을 발견하게 하는 체계”로도 잠재력이 높습니다.

다만 후자를 실제 경험으로 만들려면
**공통 물리계층 + event trace + 실험 지원 UX** 가 반드시 같이 가야 합니다.

---

# 23. 금지/배제 원칙

다음은 현재 기준으로 core에서 배제하거나 강하게 제한합니다.

1. **freehand 3D 실시간 시전**
   기본 UX로 두지 않음

2. **global bullet time 중심 설계**
   정답 메타가 되기 쉬움

3. **룬 중심 필기 게임화**
   룬은 써도 보조 modifier로만

4. **같은 구조의 개인별 semantics 변화**
   공유 재현성 파괴

5. **3D를 단순 수치 증폭기로 쓰는 것**
   손기술 세금화 위험

6. **환경이 주문 family 자체를 바꾸는 것**
   예측 가능성 저하

---

# 24. 현재 확정 사항과 보류 사항

## 24.1 확정

* 2D 중심 비선형 표의 언어
* 5 root seed
* material / ontic / delivery-praxis 분리
* 無는 overlay, 武는 無의 1차 praxis 변형
* 시각 층위 구조
* primitive 10종
* 관계 그래프 / hyperedge
* 3D operator 5종
* 2D에서는 학파가 effect bias, 3D에서는 강한 해석 개입
* identity/quality 분리
* 오류 정정 구조
* canonicalization + graph compile + deterministic simulation
* 7개 상태변수 기반 물리계층
* 개인화는 parser 강 / semantics 0 / runtime 약
* 음운은 보조 채널
* combat/forge 분리

## 24.2 보류

* 각 심볼의 최종 실루엣
* 교육형/숙련형 표기 분리 구체안
* 세부 phoneme inventory
* 학파별 VFX palette
* 정확한 밸런스 상수
* 3D operator 단계 수치
* UI overlay의 최종 스타일
* 룬 시스템의 실제 도입 여부와 범위

---

# 25. 최종 한 줄 정의

이 마법진 체계는

**“꿈결의 기본 매질을 중심 chamber에 두고, 파생·존재 상태·실행 방식·작동 문법·범위 포트를 방사형으로 조립한 뒤, seal 시점에 canonical graph로 확정하고, 공통 상태변수 기반의 2.5D 물리계층에서 결정적으로 시뮬레이션하며, 개인차는 의미가 아니라 parser·accent·작법 정체성으로만 반영하는 비선형 마법 언어”**

로 최종 정리할 수 있습니다.

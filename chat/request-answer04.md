request


현재까지 논의된 바를 일괄 종합하여 마법진 작성 체계를 최종 정리. 심볼 도형 자체는 placeholder 형태로 보류. 나머지 권장 정규형/시각 규칙/주의점은 채택함. 시각 층위 역시 제안된 역할 그대로 적용 예정. 핵심 분류의 제안 보완 역시 그대로 가져가되, 無의 한차례 변형을 武로 가져가는 방식. 나머지는 일괄 제안 그대로 적용예정. 학파의 경우는 2d 자체에서는 이펙트/세부 효과 변화만 가져가되 3d 마법진에서 극적인 변화를 주는 방식으로. 오류 정정 구조 역시 제안구조 채택. 3d operator 역시 채택. 교육형/숙련형은 표기 분할을 추후 고민. 음운 설정 역시 제안 구조대로 채택. 컴파일러 구조 역시 제안된대로 진행할것.

----



answer



아래를 현재까지 논의된 내용을 반영한 **마법진 작성 체계 최종 설계 기준안**으로 정리합니다.
심볼의 구체 도형 실루엣은 보류하고, **의미 계층·정규형·시각 층위·문법 규칙·오류 정정·3D 확장·음운·컴파일 구조**를 우선 고정하는 형태입니다.

---

# 1. 체계의 정체성

이 마법진 언어는 **비선형 표의 중심의 세마시오그래픽(semasiographic) 공간 언어**로 둡니다.

핵심 원칙은 다음과 같습니다.

1. **2D 우선**

   * 2D가 언어의 본체입니다.
   * 3D는 2D 의미를 확장하는 상위 굴절형입니다.
   * 3D가 독립 어휘 체계가 되면 안 됩니다.

2. **중심→바깥 읽기**

   * 선형 문자처럼 좌→우로 읽지 않습니다.
   * `중심 의미 → 파생 의미 → 작동 문법 → 범위/대상 → 학파/검증 → 3D 확장` 순으로 읽습니다.

3. **의미와 솜씨의 분리**

   * 주문의 정체성은 **canonical structure**에서 결정됩니다.
   * 사용자의 손맛은 위력, 효율, 안정성, 발동속도 같은 **실행 품질**에 반영됩니다.

4. **AI는 보조, 문법/컴파일러가 권위**

   * ML은 primitive proposal, ambiguity scoring, user adaptation, beautification hint에만 사용합니다.
   * 최종 semantics는 deterministic grammar/compiler가 잡습니다.

5. **설계자가 만드는 언어**

   * 유저는 “버튼을 누른다”가 아니라, **꿈결을 응집하는 설계자**처럼 느껴야 합니다.
   * 따라서 시각적으로는 회로도보다 **의식적 도형문법**에 가까워야 합니다.

---

# 2. 세계관적 전제

* 세계는 누군가의 꿈이며, 마법은 그 **꿈결**을 모아 사용하는 기술입니다.
* 마법진은 꿈결을 응집·정렬·방출하기 위한 **응집기**입니다.
* 사용자는 정신력/체력을 대가로 지불하며, 과부하 시 자신도 꿈결로 흩어질 수 있습니다.
* 학파는 꿈을 해석하는 방식의 차이입니다.
  예: 악몽, 길몽, 몽중몽, 곁잠.
* 일부 기호/상징은 공유되지만, 학파별로 같은 구조를 다르게 확장하거나 다르게 체현합니다.

---

# 3. 최종 언어 구조

## 3.1 전체 계층

이 언어는 다음 5층으로 동작합니다.

1. **Seed Root**
   응집되는 기본 매질. 중심 chamber에 위치.

2. **Derivation / Aspect**
   기본 매질을 변형하거나 존재 방식을 바꾸는 층.

3. **Process Grammar**
   마법이 어떻게 작동하는지를 정하는 core primitive 문법층.

4. **Scope / Port**
   방향, 범위, 대상, 상호작용 포트.

5. **Covenant / Validation / School**
   외곽의 학파 accent, seal, checksum, 점화 표식.

3D는 이 5층 위에 얹히는 별도의 “lift layer”입니다.

---

## 3.2 권장 내부 표현

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

이때 중요한 제약은 다음과 같습니다.

* atomic 2D 마법진에는 **RootSeed는 정확히 1개**
* `MaterialDerivation`은 최대 1개
* `OnticAspect`는 최대 1개
* `DeliveryPraxis`는 최대 1개
* 복합 속성은 한 원 안에서 무한 병치하지 않고, **다중 원(graph)** 으로 분리하는 것을 원칙으로 함
* 3D operator는 **seal 이후에만** 적용 가능

---

# 4. 핵심 분류 체계

## 4.1 Root Seed: 중심 매질

이 다섯 개만 **center-root** 로 허용합니다.

| 분류        | 항목     | 역할              |
| --------- | ------ | --------------- |
| Root Seed | 바람     | 비물질적 이동, 편류, 전달 |
| Root Seed | 땅      | 지지, 질량, 구조, 정착  |
| Root Seed | 불꽃     | 방출, 전환, 상승, 격발  |
| Root Seed | 물      | 흐름, 순환, 적응, 누수  |
| Root Seed | 식물(생명) | 발아, 생장, 회복, 얽힘  |

이 다섯 개는 “속성”이라기보다 **꿈결이 응집되는 기본 매질**입니다.

---

## 4.2 Material Derivation: 물질/에너지 파생

| 항목 | 부모 계열      | 슬롯              | 역할             |
| -- | ---------- | --------------- | -------------- |
| 독  | 물 / 생명     | Material        | 부패, 침식, 누출, 오염 |
| 강철 | 땅          | Material        | 강화, 단련, 구조 고정  |
| 전기 | 불꽃 + 바람 계통 | Material/Energy | 위상 긴장, 방전, 도약  |
| 얼음 | 물 + 땅 계통   | Material/State  | 유동의 정지, 응결, 구속 |

원칙은 동일합니다.
**파생 요소는 반드시 부모 계열의 흔적을 남겨야 합니다.**
즉, 독이 물/생명에서 완전히 끊어진 독립 심볼처럼 보이면 안 됩니다.

---

## 4.3 Ontic Aspect: 존재 방식 / 상태 파생

| 항목 | 부모 계열      | 슬롯             | 역할                     |
| -- | ---------- | -------------- | ---------------------- |
| 영혼 | 바람 계통      | Ontic          | 비물질적 존재, 이탈된 생기        |
| 사념 | 영혼 계통      | Ontic          | 내향적 집중, 정신 반향, 재귀      |
| 신성 | 생명 계통      | Ontic/Valence  | 정합, 정제, 축복된 질서         |
| 無  | 범용 overlay | Ontic/Negation | 결락, 비움, 소거, 외부 매질의 공백화 |

여기서 **無는 ordinary element가 아닙니다.**
`無`는 어떤 root 위에 덧씌워지는 **negation / hollow overlay**입니다.

즉, “불 속성처럼 無 속성”이 아니라,
“불의 외부 매질을 비워 버린 화”, “물의 흐름을 소거한 물”, “생명의 생장을 결락시킨 생명”처럼 읽는 것이 맞습니다.

---

## 4.4 Delivery / Praxis: 실행 방식

| 항목 | 부모 계열    | 슬롯       | 역할                           |
| -- | -------- | -------- | ---------------------------- |
| 염동 | 사념/영혼 계통 | Delivery | 비접촉 조작, 원격 전달                |
| 武  | 無의 1차 파생 | Praxis   | 외부 매질을 비운 뒤 육체/무기 축으로 환원된 실행 |

여기서 `武`는 독립 원소가 아닙니다.
사용자 지정대로 **“無의 한차례 변형”** 으로 둡니다.

의미는 다음과 같습니다.

* `無`는 외부 응집 매질을 비우는 부정/공백화입니다.
* `武`는 그 공백화된 외부 매질을 **몸, 무기, 자세, 접촉축**으로 되돌린 실행형입니다.
* 따라서 `武`는 존재론적 속성이 아니라 **praxis modifier** 입니다.

정리하면,

* `無` = 외부 매질의 비움
* `武` = 비워진 매질을 육체/접촉축으로 환원한 체현형

즉, `武`는 **“무(無)가 체술/전투 양식으로 접지된 상태”** 로 정리합니다.

---

# 5. 원소별 의미 고정 규칙

심볼 도형은 아직 보류지만, 각 계열은 아래의 **의미적 정규형과 추상 불변 특징**을 만족해야 합니다.

## 5.1 Root Seed

| 항목     | 정규 의미          | 추상 불변 특징                   | 주의점                              |
| ------ | -------------- | -------------------------- | -------------------------------- |
| 바람     | 이동, 편류, 개방, 전달 | 개방성, 방향성, 비정착성             | 물과 달리 “흐름의 매끄러움”보다 “빠져나감”이 보여야 함 |
| 땅      | 지지, 무게, 고정, 구조 | 완결성, 하부 안정감, 질량감           | 강철과 혼동되지 않게 단련성보다 기반성을 우선        |
| 불꽃     | 방출, 상승, 소비, 점화 | 상향 압력, 격발성, 열적 전환          | 전기와 혼동되지 않게 연속적 상승감 유지           |
| 물      | 순환, 적응, 누수, 흐름 | 매끄러운 흐름, 반복 파문, 유동성        | 얼음과 달리 정지보다는 이동성이 우선             |
| 식물(생명) | 발아, 생장, 회복, 얽힘 | 뿌리-가지 구조, 성장 방향성, 최소 자기유사성 | 신성과 달리 완벽한 대칭보다 살아 있는 성장감 우선     |

## 5.2 Derivation / Aspect

| 항목 | 정규 의미                | 부모 흔적                | 주의점                       |
| -- | -------------------- | -------------------- | ------------------------- |
| 독  | 침식, 오염, 누출           | 물/생명의 흐름 또는 생장 흔적 유지 | 그냥 “어두운 물”처럼 보이면 실패       |
| 영혼 | 이탈된 생기, 비물질 존재       | 바람의 비정착성 유지          | 바람과 구분되도록 detachedness 필요 |
| 강철 | 단련, 경화, 정밀 구조        | 땅의 기반성 유지            | 단순히 “더 강한 땅”으로만 읽히면 약함    |
| 전기 | 불연속 방전, 위상 긴장        | 불꽃의 격발성 + 바람의 전달성    | 불꽃과 구별되는 분절감 필요           |
| 염동 | 비접촉 원격 전달            | 사념/영혼 계열의 정신성 유지     | 중심 속성처럼 읽히면 안 됨           |
| 얼음 | 유동의 정지, 응결, 구속       | 물의 흐름 흔적 유지          | “차가운 물”이 아니라 “멈춘 물”이어야 함  |
| 사념 | 내향 집중, 정신 반향, 재귀     | 영혼의 존재감 유지           | 영혼보다 더 압축되고 안쪽으로 감겨야 함    |
| 신성 | 정합, 정제, 순화, 축복 질서    | 생명의 생기 유지            | 단순 종교 아이콘처럼 보이면 위험        |
| 無  | 결락, 비움, 소거           | 적용 대상의 흔적이 비워진 형태    | 독립 원소처럼 쓰지 않음             |
| 武  | 체현, 접촉, 자세, 절단/타격 실행 | 無의 비움 + 육체/축 환원      | root slot에 두지 않음          |

---

# 6. 시각 층위와 읽기 순서

## 6.1 시각 층위

이 구조는 그대로 채택합니다.

| 층                       | 역할                 | 설명                                  |
| ----------------------- | ------------------ | ----------------------------------- |
| **center seed chamber** | 기본 매질              | root seed 1개만 허용                    |
| **aspect ring**         | 파생/존재 상태           | material, ontic, delivery/praxis 배치 |
| **process ring**        | 작동 문법              | primitive 조합으로 주문 방식 정의             |
| **scope / port ring**   | 방향/대상/범위           | 외부로의 적용 구조                          |
| **covenant rim**        | 학파, seal, checksum | 외곽 검증 및 의식적 마감                      |
| **3D lift layer**       | 상위 굴절              | seal 이후 layer operator 적용           |

## 6.2 읽기 순서

1. 중심 root를 읽는다.
2. 파생 층에서 “무엇으로 변조되었는가”를 읽는다.
3. process graph에서 “어떻게 작동하는가”를 읽는다.
4. port를 통해 “어디에 작동하는가”를 읽는다.
5. outer rim에서 “어떤 학파/검증/점화 방식인가”를 읽는다.
6. 3D가 있으면 마지막에 “이 구조가 어떻게 확장되었는가”를 읽는다.

---

# 7. Core Primitive 문법

아래 primitive는 언어의 **closed-class grammar** 입니다.

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

### 문법 원칙

* primitive는 **고수준 마법 이름**이 아니라 작동 원리의 재료입니다.
* 한 atomic spell은 root를 중심으로 primitive graph를 형성해야 합니다.
* `fractal echo`는 전투에서는 깊이 1, forge에서는 더 높은 깊이를 허용합니다.
* primitive 수가 늘어날수록 자유도는 올라가지만 직관성과 인식 강건성은 떨어지므로, 전투용 live spell은 **작은 수의 primitive**를 권장합니다.

---

# 8. 2D 작성 규칙

## 8.1 원자 마법진 규칙

```text
AtomicSpell2D =
RootSeed
+ [MaterialDerivation]
+ [OnticAspect]
+ [DeliveryPraxis]
+ ProcessGraph
+ ScopePorts
+ CheckCode
+ Seal
```

### 제약

* root 1개 필수
* material 0~1
* ontic 0~1
* delivery/praxis 0~1
* process primitive 1개 이상 필수
* 전투에서는 scope port 1~3개 권장
* 한 원 안에 root를 다수 넣지 않음

복합성은 **다중 마법진 그래프**로 해결합니다.

## 8.2 복합 마법 규칙

복합 주문은 한 원 안에 무리하게 몰아 넣지 않고, **개별 atomic spell을 seal한 뒤 관계 그래프로 조합**합니다.

허용 관계는 우선 다음으로 제한합니다.

* containment
* tangency
* intersection
* concentricity
* bridge
* phase order

세 개 이상 관계는 hyperedge로 다룹니다.

---

# 9. 3D 확장 규칙

3D operator는 그대로 채택하되, 의미는 **2D의 상위 굴절형** 으로만 사용합니다.

| Operator                   | 의미                       |
| -------------------------- | ------------------------ |
| 적층(stack)                  | 우선순위, 인과 계층, 지속성         |
| 압출(extrusion/thickness)    | 용량, 지속 시간, chamber화, 구속력 |
| 공전(revolution/orbit)       | 순환 유지, 와류, 지속 갱신         |
| 기울임(tilt/orientation bias) | 방향성 바이어스, 특정 평면 목표화      |
| 교량(bridge/interlock)       | 층간 전달, 변환, relay         |

## 9.1 3D 적용 원칙

* 3D는 **sealed 2D spell** 에만 적용
* 3D는 새로운 root를 만들지 않음
* 3D는 기존 의미를 확장/강조/변형할 뿐, 2D의 기본 정체성을 파괴하지 않음
* 각 operator는 양자화된 단계만 허용

  * 적층: 정수 레이어
  * 압출: 소수 단계의 깊이
  * 공전: quarter/half/full
  * 기울임: 정해진 각도 구간
  * 교량: 허용된 typed port 사이만 연결

---

# 10. 학파 규칙

## 10.1 2D에서의 학파

사용자 결정대로, **2D에서는 학파가 정체성 자체를 바꾸지 않습니다.**

2D에서 학파는 다음 정도만 바꿉니다.

* 이펙트 질감
* 부가 효과 경향
* 품질 trade-off 경향
* 오디오 운율/음색
* 외곽 accent

즉, 2D SpellID에는 학파가 **핵심 semantics로 들어가지 않음** 을 원칙으로 합니다.

## 10.2 3D에서의 학파

반면 **3D에서는 학파가 극적으로 개입**합니다.

따라서 3D spell의 해석은 다음처럼 정리합니다.

```text
LiftedSpellIR =
Hash(Canonical2DSpell + LayerOps + School3DProfile)
```

즉,

* 2D: 학파는 주로 style/effect bias
* 3D: 학파는 operator interpretation profile

### 학파별 3D 해석 축 예시

* **길몽**

  * stack: 조화적 계층
  * extrusion: 보호 chamber
  * orbit: 수호적 유지장
  * tilt: 인도/정렬
  * bridge: 조화 연결

* **악몽**

  * stack: 압박 계층
  * extrusion: 감금 chamber
  * orbit: 공포 와류
  * tilt: 뒤틀린 편향
  * bridge: 오염 전이

* **몽중몽**

  * stack: 중첩 재귀
  * extrusion: 기억의 깊이
  * orbit: 루프 유지
  * tilt: 관점 전도
  * bridge: 재귀 relay

* **곁잠**

  * stack: 기생적 덧층
  * extrusion: 공유 chamber
  * orbit: 측면 보조 흐름
  * tilt: 빗축介入
  * bridge: 동반 relay

이 구조를 쓰면 2D의 공유 언어는 유지하면서, 3D에서 학파별 극적인 차이를 줄 수 있습니다.

---

# 11. 오류 정정 구조

이 부분은 제안 구조 그대로 채택합니다.

## 11.1 기본 원칙

* **잘못된 성공보다 설명 가능한 실패**
* 작은 실수가 다른 계열 주문으로 바뀌지 않도록 함
* identity와 quality를 분리
* auto-correction은 제한적으로만 수행

## 11.2 identity와 quality 분리

### Identity 계열

* topology
* root family trace
* slot type consistency
* required primitive presence
* family seam
* port arity
* layer seal

### Quality 계열

* closure
* symmetry
* smoothness
* order
* tempo
* overshoot
* phase regularity
* spacing

즉, 곡선이 예쁘지 않다고 주문 계열이 바뀌면 안 됩니다.

## 11.3 보정 규칙

1. **같은 슬롯 타입 안에서만 최근접 후보 탐색**
2. **파생은 부모 흔적이 있을 때만 승급**
3. **임계 이하만 보정**
4. 그 외는 **unstable / incomplete spell** 로 처리

예를 들면,

* 물이 조금 굳어 보인다고 바로 얼음으로 바꾸지 않음
* 바람이 조금 허술하다고 영혼으로 바꾸지 않음
* 無 overlay가 있다고 바로 武로 바꾸지 않음
  → 武는 반드시 praxis 조건이 별도로 맞아야 함

## 11.4 checksum 구성

외곽 장식이면서 동시에 오류 정정 신호가 되도록 합니다.

* family seam
* parity rule
* stabilizer count
* port signature
* layer seal

이들은 QR처럼 노골적이지 않고, **의식적 장식/봉인 흔적**처럼 보여야 합니다.

## 11.5 사용자 피드백 형식

시스템은 “틀렸다”가 아니라 **syndrome** 를 보여줘야 합니다.

예:

* “응결 축은 감지되나 유동 흔적이 충분히 봉인되지 않았습니다.”
* “정신 반향은 있으나 이탈된 영혼 표지가 부족합니다.”
* “null overlay는 확인되나 체현 축이 없어 武로 확정되지 않습니다.”
* “외곽 parity가 맞지 않아 containment 계열로 봉인되지 않습니다.”

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

중요한 점은, 이 벡터를 모든 주문에 동일 선형 보너스로 적용하지 않는 것입니다.

즉,

* barrier/ward 계열은 `closure`, `sym`, `constraint` 비중이 높고
* beam/shot 계열은 `tempo`, `phase`, `path consistency` 비중이 높고
* growth/ritual 계열은 `fractal coherence`, `order`, `stability` 비중이 높아야 합니다.

이렇게 해야 손기술이 단순 만능 버프가 되지 않습니다.

---

# 13. 음운/사운드 체계

음운은 실제 문자언어의 core가 아니라, **UX 강화용 중복 채널**로 둡니다.

## 역할

* 마법 구분
* 인덱싱/기억 보조
* 시전 감각 강화
* 학파 분위기 차등
* 3D 확장의 청각적 체감

## 매핑 원칙

* **모음 계열**: root seed
* **초성/발단**: process type
* **종성/노이즈 텍스처**: derivation
* **운율**: school accent
* **공간화/잔향**: 3D operator

즉, 의미를 소리에만 숨기지 않고, **시각 구조를 청각적으로 중복 강화**하는 방향입니다.

---

# 14. 컴파일러 구조

사용자 결정대로 제안 구조를 그대로 채택합니다.

## 최종 파이프라인

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

## 핵심 원칙

* raw stroke를 바로 spell class로 찍지 않음
* n-best parse를 commit 전까지 유지
* seal 시점에만 canonicalize
* graph compile 이후 물리/이펙트는 deterministic
* 환경 태그는 identity가 아니라 parameter에 영향
* ML은 보조 계층에만 사용

---

# 15. 다중 마법진 / 그래프 컴파일

복합 주문은 다음 구조를 사용합니다.

```text
GraphSpell =
{AtomicSpell_i}
+ TypedRelations
+ Hyperedges?
+ SharedPorts?
```

## 원칙

* 원 하나에 모든 걸 밀어 넣지 않음
* 개별 마법진을 먼저 안정적으로 seal
* 이후 배치/확대/축소/이동/연결로 상호작용 설계
* ignite 시점에 graph 전체를 compile

## 관계 유형

* containment
* tangency
* intersection
* concentricity
* bridge/interlock
* phase order

## 설계 철학

* 자유도를 “아무거나 인식”에서 주지 않음
* 자유도를 “조합 가능한 graph composition”에서 줌

---

# 16. 전투와 제작의 분리

이 체계는 **combat** 와 **forge** 를 분리하는 것이 전제입니다.

## Combat

* small grammar
* 낮은 primitive 수
* 포트 수 제한
* 빠른 seal/ignite
* 3D는 제한적 또는 preset 중심

## Forge

* 복합 graph
* 다중 원 배치
* 높은 primitive 수
* 2.5D/3D 설계
* preset sealing
* 고급 파생/학파/3D operator 실험

즉, 전투는 **반응형 실행 언어**,
forge는 **설계형 연구 언어** 입니다.

---

# 17. 현재 기준으로 고정된 사항

다음은 사실상 확정입니다.

1. 이 언어는 **2D 중심의 비선형 표의 공간 언어**
2. root seed는 **바람/땅/불꽃/물/식물(생명)** 5개
3. 파생은 material / ontic / delivery-praxis로 분리
4. **無는 negation overlay**
5. **武는 無의 1차 praxis 변형**
6. 염동은 root가 아니라 delivery modifier
7. 시각 층위는 `center → aspect → process → port → covenant → 3D`
8. 3D operator는 `stack / extrusion / orbit / tilt / bridge`
9. 2D에서 학파는 semantics보다 effect bias
10. 3D에서 학파는 operator interpretation profile로 적극 개입
11. 오류 정정은 slot/type/topology/checksum 기반
12. 음운은 중복 채널
13. 컴파일러는 `ink → primitive → parse → canonicalize → graph → 3D → simulate`
14. 그래프 컴파일과 forge/combat 분리 채택

---

# 18. 의도적으로 보류된 사항

아직 확정하지 않은 것은 다음입니다.

* 각 symbol의 최종 실루엣
* 교육형/숙련형 표기 분할
* 세부 phoneme inventory
* 각 학파의 실제 VFX palette
* root/derivation별 세부 balance 값
* 3D operator의 수치 단계 수
* UI overlay의 시각 스타일

즉, **외형과 수치, 세부 UX는 열어 두되, 언어 구조와 컴파일 구조는 고정**한 상태입니다.

---

# 최종 한 줄 정리

이 마법진 체계는
**“꿈결의 기본 매질을 중심 chamber에 두고, 파생·존재 상태·실행 방식·작동 문법·범위 포트를 방사형으로 조립한 뒤, seal 시점에 canonical graph로 확정하고, 필요시 3D operator와 학파 3D profile로 상위 확장하는 비선형 마법 언어”**
로 정리됩니다.

request


지금까지 이야기한 점을 감안하여 다음과 같은 요소로 우선 정의하였다고 할 때, 현재까지의 논의를 일괄 감안하여 다음의 마법진 언어요소 추가 구체화 및 고도화 하여 제공. 핵심요소 symbol(속성) - 표의
- 바람, 땅, 불꽃, 물, 식물(생명)

파생요소 symbol - 표의 요소 및 추가 조합. 핵심요소의 일부 변조
- 독, 영혼, 강철, 전기, 염동, 얼음, 사념, 신성, 無, 武


설정상의 마법 원리
- 세상은 누군가의 꿈이며, 그 꿈결을 모아 사용하는 것이 마법. 마법진은 꿈결을 모으기 위한 응집기이며, 꿈결을 그러모아 쓰기 때문에 사용자의 정신력/체력을 소모. 견뎌내지 못하면 마법 사용자 역시 꿈결로 흩어짐.
- 이 꿈결에 대하여 각자의 해석을 더한 것이 각 마법 학파/사용자. 악몽, 길몽, 몽중몽, 곁잠 등으로 세상을 해석하며, 이에 따라 사용하는 마법의 큰 틀이 달라짐
- 세부적인 기호/상징 자체는 일부 공유함.
- 음운은 실제 마법 구분/인덱싱/유저 UX 강화용으로 우선 고려됨(마법 시전시 사운드 효과 등)



마법 의미를 위한 core primitive
	핵(nucleus): 점 또는 소핵. 의미는 source, focus, origin.
	경계(boundary/container): 닫힌 원환. 의미는 containment, stabilization, ward.
	개구(aperture): 열린 틈이 있는 경계. 의미는 release, admission, leak.
	경로(path/ray): 방향성을 가진 선·호. 의미는 transfer, propulsion, beam.
	결속(link/tether): 두 핵 또는 두 경계를 잇는 연결. 의미는 binding, channeling.
	차단(block/cross): 경로를 가로막는 bar. 의미는 oppose, sever, cancel.
	축(axis/mirror): 대칭축 또는 거울축. 의미는 balance, duality, reflection.
	분기(branch): 갈라짐. 의미는 propagation, multiplicity, diffusion.
	반복(echo/concentric repeat): 반복된 동심 또는 반향 패턴. 의미는 amplification, resonance.
	자기유사(fractal echo): 축소 반복. 의미는 recursion, self-propagation, chained reapplication.

3d operator
	적층(stack): 위/아래 레이어 관계. 의미는 우선순위, 인과 계층, 지속성.
	압출(extrusion/thickness): 깊이 두께. 의미는 용량, 지속 시간, 구속력, chamber화.
	공전(revolution/orbit): 회전 적층. 의미는 순환 유지, 와류, 지속 갱신.
	기울임(tilt/orientation bias): 경사나 면 지향. 의미는 방향성 바이어스, 특정 평면 목표화.
	교량(bridge/interlock): 레이어 간 결합. 의미는 층간 전달, 변환, relay.



추가 고려 요소
- 오류 정정요소



---



answer


현재 정의는 뼈대는 좋습니다. 다만 그대로 쓰면 한 지점에서 무너집니다. **15개를 전부 동급의 “속성”으로 두면, 물질·상태·존재론·전달 방식·전투 자세가 한 슬롯에 섞여 버립니다.** 그러면 언어학적으로는 품사 체계가 붕괴하고, HCI 관점에서는 초심자가 “이 기호가 무엇을 뜻하는지”보다 “이 기호가 어느 자리에 들어가는지”부터 헷갈리게 됩니다. 시각 언어 설계에서는 novice가 외형만 보고 의미를 유추할 수 있는 semantic transparency가 핵심이고, novice 참여 설계가 이를 실제로 끌어올린다는 결과가 있습니다. 또 gesture elicitation과 sketch-recognition front-end 연구는 사용자 합의가 낮은 명령은 억지로 기호화하지 말고 별도 UI로 분리하는 편이 낫고, 인식 시스템은 reliability·low distraction·predictability를 우선해야 한다고 보고합니다. 이 언어도 같은 원칙을 따라야 합니다. ([Geoffrey Sampson][1])

또한 구현 측면에서는 자유 손그림을 바로 의미로 찍는 방식보다, **primitive와 관계를 그래프로 읽고, 불확실성은 commit 전까지 유지한 뒤 canonical form으로 확정**하는 편이 훨씬 강합니다. uncertain input 처리 연구는 대안 해석의 확률을 유지하는 프레임워크를 제안했고, CAD 쪽 SketchGraphs는 sketch를 primitive-constraint graph로 표현합니다. 최근 PICASSO도 hand-drawn sketch를 parametric primitive로 복원하는 방향을 취했습니다. 3D 스케치는 더 보수적으로 가야 합니다. ILoveSketch, Multiplanes, VR sketch 평가 연구는 3D 자유 드로잉이 본질적으로 어렵고 plane guidance, beautification, quantization이 있어야 쓸 만해진다고 봅니다. ([Microsoft][2])

이 전제를 깔고 보면, 지금 필요한 것은 “속성 목록을 늘리는 것”이 아니라 **언어의 계층과 품사, 위치, 파생 규칙을 정리하는 것**입니다.

## 1. 먼저 수정해야 할 핵심 분류

현재 목록은 의미적으로 이질적입니다. 그래서 다음처럼 재분류하는 편이 맞습니다.

| 계층                                     | 항목                   | 성격                 | 왜 이렇게 두는가                                  |
| -------------------------------------- | -------------------- | ------------------ | ------------------------------------------ |
| **기본 뿌리(root attribute)**              | 바람, 땅, 불꽃, 물, 식물(생명) | **주요 존재론적 매질**     | 씨앗 의미. 중심 chamber에 들어갈 수 있는 기본 어휘          |
| **물질/에너지 파생(material derivative)**     | 독, 강철, 전기, 얼음        | **뿌리 속성의 변성**      | 기본 속성의 일부 변형으로 직관적 파생 가능                   |
| **존재론/상태 파생(ontic/aspect derivative)** | 영혼, 사념, 신성, 無        | **존재 방식 또는 상태 변화** | “무엇이냐”보다 “어떤 방식으로 존재하느냐”에 가까움              |
| **전달/실행 모드(delivery/praxis modifier)** | 염동, 武                | **시전 방식/몸-매개 방식**  | substance가 아니라 작동 방식. 중심 속성 슬롯에 두면 오인식이 쉬움 |

이 재분류가 중요한 이유는 다음과 같습니다.

* **`신성`은 원소가 아니라 “정합성/축복된 질서”에 가깝습니다.**
* **`無`는 물질이 아니라 “소거/결락/부재화”에 가깝습니다.**
* **`염동`은 매질이 아니라 “비접촉 전달 방식”입니다.**
* **`武`는 실체가 아니라 “체현된 공격 자세/수행 방식”입니다.**

즉, 이 네 개는 기본 원소들과 같은 자리에 놓이면 안 됩니다.
가장 크게 보면 이 언어는 다음처럼 굴러가야 합니다.

**씨앗 매질(root) + 변성(material) + 존재 상태(aspect) + 실행 방식(delivery) + 공간 문법(process)**

---

## 2. 권장 언어 골격: “몽결문” 식 radial graph grammar

이 체계는 선형 문자처럼 좌→우로 읽히는 것이 아니라, **중심에서 바깥으로 읽히는 방사형 문법**으로 두는 것이 맞습니다.

### 권장 내부 표현

```text
SpellIR =
< SeedRoot,
  MaterialDerivation?,
  OnticAspect?,
  DeliveryMode?,
  ProcessGraph,
  ScopePorts,
  SchoolAccent?,
  LayerOps*,
  Check >
```

### 시각 층위

| 층                   | 내용                                                                 | 언어학적 역할     | HCI 역할                | 권장 제한             |
| ------------------- | ------------------------------------------------------------------ | ----------- | --------------------- | ----------------- |
| **중심 seed chamber** | 기본 속성(root)                                                        | 핵심 어휘       | 관전자도 한눈에 “무슨 계열인지” 파악 | 정확히 1개            |
| **aspect ring**     | material/ontic 파생                                                  | 파생 형태론      | 가족 유사성 유지, 학습 쉬움      | 각 타입 1개 이하        |
| **process ring**    | boundary, aperture, path, link, block, axis, branch, echo, fractal | 닫힌 문법 기능어   | 마법의 작동 방식을 읽게 함       | 과도한 중첩 금지         |
| **scope/port ring** | 방향, 범위, 대상 포트                                                      | 통사-실행 인터페이스 | 전투에서 즉시 읽히는 층         | live는 1~3포트       |
| **covenant rim**    | 학파 accent, checksum, 점화 표식                                         | 화용론/문체      | 신뢰성, 세계관, 설명 가능성      | 미시 장식 수준          |
| **3D lift**         | stack/extrusion/orbit/tilt/bridge                                  | 상위 aspect   | 2D 의미의 확장             | sealed spell에만 적용 |

이 구조의 핵심은 단순합니다.

* **중심은 “무엇이 응집되었는가”**
* **중간은 “어떻게 작동하는가”**
* **바깥은 “어디로/무엇에 작동하는가”**
* **3D는 “그 작동을 어떻게 확장하는가”**

즉, 3D는 새로운 언어가 아니라 **2D 언어의 상위 굴절형(aspectual extension)**이어야 합니다.

---

## 3. 기본 5개 속성 symbol 고도화

여기서 중요한 것은 “그럴듯한 그림”이 아니라 **hard identity feature**를 갖는 것입니다.
각 속성은 최소 3개의 강한 불변 특징을 가져야 하고, 나머지는 품질 점수로 보내야 합니다.

### 3-1. 바람

| 항목            | 제안                                                                      |
| ------------- | ----------------------------------------------------------------------- |
| **기하 recipe** | `Aperture(1) + Path×3(tangential, same direction) + Nucleus(eccentric)` |
| **핵심 의미**     | 편류, 전달, 회피, 확산, 속도                                                      |
| **하드 불변 특징**  | ① 경계가 완전 폐합되지 않음 ② 3개의 흐름선이 같은 방향으로 쓸림 ③ nucleus가 정중앙이 아니라 뒤로 치우침       |
| **품질 focus**  | sweep 일관성, aperture 각도, 중심 편향의 긴장감                                      |
| **직관적 인상**    | “잡혀 있는 것”보다 “흘러 나가는 것”이 먼저 보이도록                                         |

해석적으로는 **바람 = 비물질적 이동성**입니다.
그래서 나중에 `영혼`, `전기`, `염동` 계열의 부모가 되기 좋습니다.

### 3-2. 땅

| 항목            | 제안                                                                                     |
| ------------- | -------------------------------------------------------------------------------------- |
| **기하 recipe** | `Boundary(closed, thick) + Axis(base) + Block×2(lower braces) + Nucleus(basal-center)` |
| **핵심 의미**     | 지지, 질량, 정착, 구조화, 인내                                                                    |
| **하드 불변 특징**  | ① 완전 폐합 ② 하부 지지축 존재 ③ 아래쪽이 무겁고 좌우가 받쳐 줌                                                |
| **품질 focus**  | 폐합도, 좌우 균형, 하부 안정성                                                                     |
| **직관적 인상**    | 위보다 아래가 더 무겁게 읽혀야 함                                                                    |

땅은 “돌”보다 넓고, “안정화된 꿈결의 지지면”에 가깝게 두는 편이 좋습니다.
그래서 `강철`, `얼음`, `武`의 안정축 역할을 잘 합니다.

### 3-3. 불꽃

| 항목            | 제안                                                             |
| ------------- | -------------------------------------------------------------- |
| **기하 recipe** | `Nucleus(center) + Aperture(up) + Branch×3(ascending tongues)` |
| **핵심 의미**     | 방출, 변환, 상승, 소비, 격발                                             |
| **하드 불변 특징**  | ① 위쪽으로 열린 crown ② 위로 치솟는 3갈래 ③ 아래 반구가 더 조여 있음                  |
| **품질 focus**  | 상승 템포, 혀 모양 분리도, overshoot 제어                                  |
| **직관적 인상**    | 안정보다 상승 압력과 방출감이 먼저 보이도록                                       |

불꽃은 “열”보다 **상향 방출과 전환**에 더 가깝게 두는 편이 좋습니다.
그래서 `전기`, `武`와 잘 연결됩니다.

### 3-4. 물

| 항목            | 제안                                                                              |
| ------------- | ------------------------------------------------------------------------------- |
| **기하 recipe** | `Echo×2~3(parallel flowing crescents) + Nucleus(center) + minor spill Aperture` |
| **핵심 의미**     | 흐름, 적응, 누수, 순환, 냉각                                                              |
| **하드 불변 특징**  | ① 평행한 곡선 파문 ② branch/root 없음 ③ 작은 spill gap 또는 흘러내림 방향이 존재                      |
| **품질 focus**  | 곡률의 매끄러움, 파문 간격, 누수와 폐합의 균형                                                     |
| **직관적 인상**    | “고정된 그릇”이 아니라 “머무르면서도 흐르는 것”처럼 보여야 함                                            |

물은 echo와 비슷해 보일 위험이 큽니다.
그래서 **평행 파문 + 미세한 spill 방향**을 identity로 고정해야 합니다.

### 3-5. 식물(생명)

| 항목            | 제안                                                                                     |
| ------------- | -------------------------------------------------------------------------------------- |
| **기하 recipe** | `Nucleus(low-center) + Link(root, down) + Branch(Y, up) + Fractal×1(mini-bifurcation)` |
| **핵심 의미**     | 발아, 회복, 얽힘, 증식, 생장                                                                     |
| **하드 불변 특징**  | ① 아래 root ② 위쪽 bifurcation ③ 최소 1단 자기유사 미세분기                                           |
| **품질 focus**  | root 고정력, 가지 각도, 자기유사 비율                                                               |
| **직관적 인상**    | “한 번 뻗는다”가 아니라 “살아서 이어진다”가 보이게                                                         |

생명은 단순 “힐링”이 아니라 **성장하는 매질**로 두는 편이 좋습니다.
그래야 `독`, `신성`과 서로 반대 방향의 파생이 가능합니다.

---

## 4. 파생 10개 symbol 고도화

여기서는 **독립 pictogram**을 새로 만드는 것보다, **기본 속성의 파생 형태소**로 보이게 하는 것이 훨씬 좋습니다.
즉, 각 파생 symbol은 내부적으로 반드시 다음처럼 확장 가능해야 합니다.

```text
Derived = ParentRoot + DerivationalMark(+ optional auxiliary root)
```

그리고 모든 파생은 **교육형 표기**와 **숙련형 표기** 두 가지를 두는 것이 좋습니다.

* **교육형**: 부모 흔적이 분명히 보이는 확장형
* **숙련형**: 리가처(합자)처럼 압축된 축약형

초심자에게는 교육형을 먼저 노출하고, 숙련자나 preset/forge에서만 숙련형을 강하게 쓰는 편이 맞습니다.

### 4-1. material derivative

| symbol | 권장 정규형                     | 시각 규칙                                                 | 슬롯              | 주의점                             |
| ------ | -------------------------- | ----------------------------------------------------- | --------------- | ------------------------------- |
| **독**  | `(물 or 생명) + Corrupt/Seep` | 부모의 파문/생장 뼈대를 남긴 채, **누수 흉터**와 **하강 seep**를 추가        | material        | `신성`과 같은 chamber에서 직접 공존시키지 말 것 |
| **강철** | `땅 + Temper/Brace`         | 땅의 폐합과 하부 지지를 유지하고, **강한 축**과 **측면 brace**를 추가        | material        | 단순히 “더 두꺼운 땅”처럼 보이면 실패          |
| **전기** | `불꽃 + 바람 + Discharge`      | 불꽃의 상승 crown 위에 **forked discharge**와 **이중 위상 긴장** 추가 | material/energy | 바람과 혼동되기 쉬우므로 분절감 필요            |
| **얼음** | `물 + 땅 + Arrest`           | 물의 파문을 보존하되 **spill gap을 닫고**, **고정 축**을 부여           | material/state  | “차가운 물”이 아니라 “멈춘 물”처럼 보여야 함     |

### 4-2. ontic / aspect derivative

| symbol | 권장 정규형                              | 시각 규칙                                      | 슬롯            | 주의점                                 |
| ------ | ----------------------------------- | ------------------------------------------ | ------------- | ----------------------------------- |
| **영혼** | `바람 + Halo + Detached nucleus`      | 바람의 흐름 뼈대를 유지하되 nucleus가 지지면에서 **떨어져 있음**  | ontic         | 바람과 구분하려면 halo 또는 detachedness가 필수  |
| **사념** | `영혼 + Inward Echo + Fractal(1)`     | 영혼 기반 위에 **안쪽으로 말리는 반향**과 작은 자기유사 추가       | ontic         | 영혼보다 더 내향적이고 집중된 인상이어야 함            |
| **신성** | `생명 + Perfect Axis + Halo + Purify` | 생명 구조를 **의도적으로 대칭화**하고, 정갈한 halo를 추가       | ontic/valence | “종교 상징”보다 “길몽 정합성”으로 읽혀야 함          |
| **無**  | `Negate + Hollow core`              | 기대되는 nucleus를 **의도적으로 비우고**, null scar를 남김 | negation      | ordinary root와 동급의 속성처럼 쓰지 않는 것이 안전 |

### 4-3. delivery / praxis modifier

| symbol | 권장 정규형                           | 시각 규칙                                                  | 슬롯              | 주의점                 |
| ------ | -------------------------------- | ------------------------------------------------------ | --------------- | ------------------- |
| **염동** | `Air/Thought-driven Remote Link` | seed chamber가 아니라 **link/path 위에 떠 있는 비접촉 tether**로 표기 | delivery        | 중심 root 자리에 넣지 말 것  |
| **武**  | `Earth + Fire + Embodied Strike` | 역시 seed가 아니라 **axis/path 위의 체현된 strike mark**로 표기      | delivery/praxis | substance처럼 읽히면 안 됨 |

여기서 특히 중요한 점이 두 가지 있습니다.

첫째, **`염동`과 `武`는 중심 chamber glyph가 아니라 process modifier여야 합니다.**
둘째, **`無`는 ordinary element가 아니라 negation overlay여야 합니다.**

이 셋을 같은 “원소 슬롯”에 넣는 순간, 언어가 급격히 불친절해집니다.

---

## 5. 혼동이 큰 쌍과 반드시 분리해야 할 시각 단서

이 언어에서 가장 위험한 혼동쌍은 미리 막아야 합니다.

| 혼동쌍              | 왜 헷갈리는가                    | 강제 분리 규칙                                                   |
| ---------------- | -------------------------- | ---------------------------------------------------------- |
| **바람 vs 영혼**     | 둘 다 비물질적이고 가벼움             | 영혼은 **detached nucleus + halo** 필수                         |
| **물 vs 얼음**      | 둘 다 유체 계열                  | 얼음은 **spill closure + full axis** 필수                       |
| **생명 vs 신성**     | 둘 다 회복/정화 계열로 보일 수 있음      | 신성은 **강대칭**, 생명은 **비대칭 성장** 유지                             |
| **물/생명 vs 독**    | 독이 단지 어두운 색의 물/생명처럼 보이기 쉬움 | 독은 **누수 흉터 + 침식 방향성** 필수                                   |
| **불꽃 vs 전기**     | 둘 다 밝고 공격적                 | 전기는 **fork / segmentation / 위상 긴장**이 있어야 함                 |
| **땅 vs 강철 vs 武** | 전부 단단하고 무거워 보임             | 강철은 **tempered brace**, 武는 **forward strike axis**가 보여야 함  |
| **영혼 vs 사념**     | 둘 다 정신계                    | 사념은 **inward echo + recursion**, 영혼은 **detached presence** |

이 표는 단순 미학이 아니라 **오류 정정의 최소 거리 확보 규칙**입니다.

---

## 6. 학파 표현: “같은 언어의 방언/문체”로 두어야 한다

설정상 학파가 매우 중요하므로, 이 요소는 언어에 반드시 들어가야 합니다.
다만 **학파가 core semantics를 바꾸면 안 됩니다.**
학파는 **allography + pragmatics + parameter bias** 정도로만 기능해야 합니다.

### 권장 학파 accent

* **길몽**

  * 형태: 매끄러운 폐합, 높은 대칭, 얇고 깨끗한 halo
  * 의미 편향: 안정, 회복, 정제
  * UX 효과: 관전자에게 “안전하고 정돈된 주문”으로 읽힘

* **악몽**

  * 형태: 늘어진 개구, 비틀린 echo, 불균형한 누수 흔적
  * 의미 편향: 압박, 공포, 반동, 오염
  * UX 효과: 같은 spell이라도 더 위험하고 공격적으로 보임

* **몽중몽**

  * 형태: 내부에 1단계 더 작은 echo 또는 fractal nesting
  * 의미 편향: 지연, 재귀, 반사, 기억
  * UX 효과: “한 번 더 감기는 주문”으로 읽힘

* **곁잠**

  * 형태: 측면 편위 accent, lateral bridge, 쌍핵의 그림자
  * 의미 편향: relay, 기생, 공동시전, 간섭
  * UX 효과: 관계형/보조형 마법으로 읽힘

여기서 중요한 것은 **학파 accent는 outer rim에 얹히는 층**이라는 점입니다.
즉, 같은 canonical spell은 학파가 달라도 같은 SpellID를 가져야 합니다.
달라지는 것은 이펙트의 질감, 품질 trade-off, 사운드, 부가 파라미터 정도가 맞습니다.

---

## 7. 3D operator는 “새 어휘”가 아니라 “상위 굴절형”이어야 한다

3D는 새로운 의미 체계를 따로 만들면 안 됩니다.
**이미 sealing된 2D chamber를 어떻게 들어 올리는가**로만 해석해야 합니다.

### 권장 규칙

* **Stack**

  * 의미: 상위 레이어가 제어 규칙, 하위 레이어가 payload
  * 예: `신성` 위에 `생명 ward`를 stack → 치유 장막
  * 제한: live combat에서는 최대 2층

* **Extrusion**

  * 의미: 용량, 지속시간, 구속력 증가
  * 예: `얼음 boundary` + extrusion → prison/chamber
  * 제한: 깊이는 3단계만 양자화

* **Orbit**

  * 의미: 유지, 순환, 와류, aura
  * 예: `전기 path` + orbit → 회전 방전장
  * 제한: quarter / half / full revolution 정도만 허용

* **Tilt**

  * 의미: 특정 평면/방향으로 편향
  * 예: `바람 path` + tilt → 벽면 타고 흐르는 절단풍
  * 제한: 기울임 각도는 몇 단계로 양자화

* **Bridge**

  * 의미: 층간 relay, 변환, 단계적 점화
  * 예: `독 field`와 `불꽃 trigger`를 bridge → 독연소
  * 제한: bridge는 typed port끼리만 허용

핵심은 다음입니다.

* 3D는 **lexical distinction**을 추가하지 않는다.
* 3D는 **aspectual / operational distinction**만 추가한다.
* 3D 조작은 **quantized**되어야 한다.
* 3D는 가능하면 **forge/preset 중심**, 전투에서는 제한적으로만 쓴다.

---

## 8. 오류 정정 구조: “잘못된 성공”보다 “설명 가능한 실패”

이 부분은 반드시 구조화해야 합니다.

### 8-1. identity와 quality를 분리

* **identity**

  * boundary closure/open
  * nucleus 존재/부재
  * root/branch/fork/axis 같은 topological class
  * family accent / seam
* **quality**

  * 부드러움
  * 대칭 정밀도
  * spacing
  * overshoot
  * tempo

즉, **stroke order나 필기 질감은 품질로 보내고, identity는 topology와 family mark로 고정**해야 합니다.

### 8-2. 최소 정정 방식

```text
1) 같은 슬롯 타입 안에서만 최근접 후보를 찾음
2) 부모 family 흔적이 남아 있을 때만 파생으로 승급
3) 거리 임계 이하만 보정
4) 그 밖은 unstable / incomplete spell 처리
```

예를 들면:

* `물` 비슷한 파문이 있지만 axis가 충분치 않다 → **얼음 오인식 금지**, 그냥 “불완전 응결”
* `바람` 계열 흐름은 보이나 detached nucleus가 없다 → **영혼 오인식 금지**, 그냥 바람 또는 미완성 영혼
* `武` 비슷한 strike mark가 있지만 body-axis가 없다 → **무속성으로 오인식 금지**, 그냥 불완전 체현

### 8-3. 권장 checksum 방식

체크코드를 노골적인 QR처럼 만들면 안 됩니다. 대신 **미시 장식이면서 동시에 오류 정정 신호**가 되어야 합니다.

권장 구조는 다음과 같습니다.

* **family seam**

  * 각 root family는 outer rim에 자기 고유의 seam 위치를 가짐
* **parity rule**

  * containment 계열은 짝수 stabilizer
  * propagation 계열은 홀수 branch cue
  * strike 계열은 단일 전방 port
* **layer seal**

  * 3D 승격된 spell은 layer bridge mark가 반드시 존재

즉, recognizer는 “보기에 예쁜 장식”이 아니라 “문법 일관성 검사”를 수행할 수 있습니다.

### 8-4. 사용자에게 보여 줄 오류는 syndrome 형태가 좋다

좋은 메시지:

* “물 계열 파문은 확인되지만 응결 축이 부족합니다.”
* “영혼 표식이 보이나 중심 이탈이 불충분합니다.”
* “전기 방전 fork가 위상 정렬을 잃었습니다.”
* “분기 parity가 맞지 않아 확산계 주문으로 확정되지 않습니다.”

나쁜 메시지:

* “이건 얼음이 아닙니다.”
* “잘못 그렸습니다.”
* “다른 주문으로 해석했습니다.”

---

## 9. 음운/사운드 계층은 **중복 채널**이어야 한다

이 설정에서 음운은 충분히 가치가 있습니다.
다만 **음운이 core semantics를 단독으로 구분하면 안 됩니다.**

권장 원칙은 이렇습니다.

* **시각 채널이 1차 의미**
* **음운 채널은 2차 indexing / SFX / 기억 강화**
* **학파 차이는 운율과 음색에서**
* **3D 차이는 공간화(reverb / spatialization)에서**

### 가장 안정적인 매핑 방식

* **vowel quality** → 기본 root

  * 바람: 가볍고 얇은 모음
  * 땅: 무겁고 둥근 모음
  * 불꽃: 열린 밝은 모음
  * 물: 둥글고 흐르는 모음
  * 생명: 중간 음역의 살아 있는 공명

* **onset class** → process

  * containment, emission, link, sever, branch, echo 등

* **coda / noise layer** → derivation

  * hiss = 독/얼음
  * crack = 전기
  * halo overtone = 영혼/신성
  * breath drop / silence = 無
  * body impact transient = 武

* **prosody** → school accent

  * 길몽: 균일, 정갈
  * 악몽: 흔들림, distort
  * 몽중몽: 반복/에코
  * 곁잠: 측면 딜레이, call-response

이 구조의 장점은 **관전자는 그림만 보고도 대략 읽을 수 있고**,
사용자는 **소리만 들어도 무슨 계열인지 빠르게 기억**할 수 있다는 점입니다.

---

## 10. 구현 방향: 인식기보다 “언어 컴파일러” 중심으로

구현은 다음 흐름이 가장 낫습니다.

```text
digital ink
→ primitive fitting
→ candidate family detection
→ typed slot parsing
→ derived expansion to canonical IR
→ checksum/type validation
→ seal/ignite
→ deterministic simulation
```

### 구현상 핵심 판단

1. **derived symbol을 단독 class로 학습하지 말고, parent+mark 구조로 읽게 할 것**
   예: `얼음`은 내부적으로 항상 `물 + arrest + earth-axis`로 환원

2. **n-best parse 유지**

   * commit 전까지 하나로 확정하지 않음
   * seal 시점에만 canonicalize

3. **ML은 보조**

   * primitive ranking
   * ambiguity scoring
   * user-specific fitting
   * beautification hint

4. **최종 semantics는 deterministic**

   * graph IR
   * typed port
   * family constraints
   * layer operators

이 방향은 sketch를 primitive/constraint graph로 다루는 CAD 계열과 잘 맞고, hand-drawn sketch를 parametric primitive로 복원하는 최근 접근과도 정합적입니다. 또한 uncertain input을 commit 전까지 유지하는 방식이 explainability와 신뢰성에 유리합니다. 3D 역시 raw freehand를 바로 의미로 읽기보다, sealed 2D structure에 discrete operator를 얹는 편이 훨씬 현실적입니다. ([Microsoft][2])

---

## 11. 최종 권장안

현재 정의를 가장 강하게 만드는 정리는 아래와 같습니다.

### 핵심 결정

* **기본 5개만 center-root로 허용**
* **파생 10개는 전부 typed derivation으로 재배치**
* **`염동`, `武`는 중심 속성이 아니라 delivery modifier**
* **`無`는 ordinary element가 아니라 negation overlay**
* **`신성`은 종교 일반이 아니라 “길몽 정합/정화”로 내부 gloss 고정**
* **파생 symbol은 교육형/숙련형 2계층 표기**
* **3D는 sealed 2D spell의 굴절형 확장**
* **오류 정정은 family seam + topology + slot type 기반**
* **음운은 의미를 숨기지 않는 중복 채널**

### 이 체계가 주는 효과

* 초심자는 “무슨 계열인지”를 빠르게 읽을 수 있음
* 숙련자는 파생과 조합을 통해 자기식 설계를 할 수 있음
* 학파 차이는 살아 있지만 shared lexicon은 유지됨
* recognizer는 설명 가능한 canonicalization이 가능함
* 2D 우선 원칙을 지키면서 3D 확장도 의미적으로 자연스러움

한 줄로 요약하면, 이 언어는 **“꿈결의 매질(root)을 중심 chamber에 놓고, 파생과 실행 방식은 바깥 고리로 굴절시키는 semasiographic radial spell language”**로 가는 것이 가장 강합니다.
지금 주신 요소들은 이 구조로 재배치하면 충분히 설계 가능한 수준입니다.

[1]: https://www.grsampson.net/AWsm.pdf "https://www.grsampson.net/AWsm.pdf"
[2]: https://www.microsoft.com/en-us/research/wp-content/uploads/2016/10/SchwarzUIST2010.pdf "https://www.microsoft.com/en-us/research/wp-content/uploads/2016/10/SchwarzUIST2010.pdf"

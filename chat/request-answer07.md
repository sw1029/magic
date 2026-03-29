request


현재 구성된 언어체계 및 논의 전반을 토대로 물리계층을 설계하고자 할 때, 어떤 방식으로 진행되어야 할지 엄밀히 검토 후 구성 초안을 제시. 사용자 승인 필요 항목은 별도 분리. 사용자의 창발적 UX를 최 우선시하는 관점.
---



answer



엄밀히 보면, 지금까지 고정한 언어체계를 **실제로 창발적 경험을 만드는 시스템**으로 바꾸려면, 물리계층은 “스킬별 하드코딩 효과 묶음”이 아니라 **공통 상태변수 + 국소 상호작용 + 피드백 루프 + 설명 가능한 이벤트 추출층**으로 설계되어야 합니다. emergent game design 쪽에서는 prescripted path보다 **전역 규칙과 경계**를 먼저 두고, 그 안에서 **짧은 범위의 상호작용, 비선형성, 피드백 루프**가 복합적으로 작동해야 한다고 봅니다. Sweetser는 이런 접근을 emergent game world의 핵심으로 설명했고, 환경 효과를 cellular automata로 다루는 방향도 직접 검토했습니다. Dormans는 게임의 동적 거동과 플레이 경험을 좌우하는 것이 내부 economy의 feedback structure라고 보고, 이런 구조를 디자이너가 의식적으로 다루게 하는 추상 모델이 중요하다고 정리했습니다. 또 creativity-support HCI 쪽에서는 what-if exploration, process visualization, replayable history가 있어야 사용자가 “발견”을 학습 가능한 형태로 축적할 수 있다고 봅니다. 지금 마법 언어는 이미 graph/constraint 기반 canonicalization에 잘 맞기 때문에, SketchGraphs류의 primitive-constraint graph 관점과도 구조적으로 잘 맞습니다. ([전기전자컴퓨터공학부][1])

따라서 권장 방향은 명확합니다. **언어계층은 graph compiler**, **물리계층은 layered field simulator**, **UX계층은 event/trace explainer**로 분리해야 합니다. 핵심은 “마법진이 무엇을 뜻하는가”와 “그 뜻이 세계에서 어떻게 전개되는가”를 분리하되, 둘 사이를 아주 촘촘히 연결하는 것입니다.

## 1. 먼저 고정해야 할 물리계층 원칙

이 물리계층은 아래 다섯 조건을 만족해야 합니다.

첫째, **동일한 2D canonical spell은 동일한 기본 거동**을 가져야 합니다.
둘째, **환경·품질·다중 마법진·3D 연산**이 모두 같은 공통 상태변수에 개입해야 합니다.
셋째, 플레이어가 학습할 수 있도록 **숨은 상태는 적고, 관측 가능한 단서는 많아야** 합니다.
넷째, 실패도 완전히 무의미한 fizzled fail이 아니라 **생산적 잔재(residue)** 를 남겨야 합니다.
다섯째, combat와 forge는 **동일 물리법칙**을 공유하고, 차이는 오직 가시화·시간 여유·허용 복잡도에서만 나야 합니다. 이 점이 중요합니다. 물리가 모드마다 달라지면 장인정신이 쌓이지 않습니다.

가장 피해야 할 설계는 다섯 가지입니다.

* 주문 family별 전용 코드 경로
* 3D를 단순 수치 증폭기로만 쓰는 구조
* 플레이어가 전혀 읽을 수 없는 과도한 숨은 상태
* `無`/`武`를 범용 우회키로 만드는 구조
* 환경을 단순 데미지 배율 태그로 처리하는 구조

---

## 2. 권장 아키텍처: “계층형 2.5D 공통 매질 시뮬레이션”

### 2.1 런타임 공간 표현

권장 표현은 **layered sparse field + spell hypergraph** 입니다.

* 세계는 완전한 3D voxel이 아니라, **2D 표면 기반 셀 격자/내비메시 위에 얹힌 여러 레이어**로 표현합니다.
* 각 레이어는 2D field를 가지며, `bridge`와 `stack` 같은 3D operator가 레이어 간 결합을 만듭니다.
* 활성 주문은 particle effect가 아니라, 각 레이어 위에 작용하는 **local operator kernel 집합**으로 컴파일됩니다.

이를 수식으로 쓰면,

[
\Omega = {(c,\ell)\mid c \in \mathcal C,\ \ell \in {0,\dots,L-1}}
]

여기서 (\mathcal C)는 2D 셀 집합, (\ell)은 레이어 인덱스입니다.

이 표현이 좋은 이유는 단순합니다.

* 현재 언어체계가 **2D 우선 + 2.5D 확장**이므로 구조적으로 잘 맞습니다.
* 완전 자유 3D보다 훨씬 예측 가능하고, 성능과 디버깅이 쉽습니다.
* Graph/hypergraph 기반 spell IR를 layer-local operator로 내리기 좋습니다. CAD sketch도 primitive, constraints, hyperedge까지 포함한 graph 표현이 잘 맞는다는 점이 이미 확인되어 있습니다. 

---

## 3. 권장 상태변수: 7개 지속 채널 + 3개 파생 채널

지금 언어체계에서 창발성을 만들려면, 상태변수는 너무 많아도 안 되고 너무 적어도 안 됩니다.
권장안은 **7개 persistent channel**입니다.

### 3.1 지속 채널

| 기호       | 이름        | 직관적 의미              | 주 역할                    | 플레이어가 느껴야 할 단서        |
| -------- | --------- | ------------------- | ----------------------- | --------------------- |
| (\rho)   | 꿈결 밀도     | 쓸 수 있는 매질의 양        | 모집, 유지, 고갈              | 안개/입자 농도, 흡입감         |
| (\kappa) | 정합도 / 응집도 | 구조가 얼마나 잘 봉인·정렬되었는가 | 봉인, 안정성, 경계 품질          | 선의 선명도, 구조가 ‘맞물리는’ 느낌 |
| (\phi)   | 위상        | 공명/회전/동기 상태         | resonance, pulse timing | 맥동, 회전, 리듬            |
| (\tau)   | 긴장 / 응력   | 축적된 불안정과 방출 압력      | 폭발, 반동, 파열              | 균열, 떨림, crackle       |
| (\chi)   | 오염도       | 독·악몽·부패 성분          | 부식, 전염, 불안정화            | 얼룩, 노이즈, 탁함           |
| (\nu)    | 공백도 / 무화압 | 매질 결락, 결합 단절        | 차단, 소거, 무화              | 소리의 소거, 질감의 비어 있음     |
| (\beta)  | 생장도 / 생기  | 생명적 증식성과 회복력        | 치유, 발아, 자기증식            | 맥동, 싹틈, 유기적 확장        |

범위는 다음처럼 두는 편이 좋습니다.

[
\rho,\kappa,\tau,\chi,\nu,\beta \in [0,1], \qquad \phi \in (-\pi,\pi]
]

### 3.2 파생 채널

이 셋은 저장형 state라기보다 **runtime에서 계산되는 derived field**로 두는 편이 좋습니다.

* (\mathbf u): **유량/흐름 벡터**
  path, link, orbit, tilt, air 계열이 만든 방향성 운반장
* (P): **투과/차단 계수**
  boundary, aperture, earth, steel, ice가 만든 막 성질
* (g): **루프 이득 / 공명 gain**
  echo, fractal, orbit, symmetry, school-3D profile이 만든 피드백 증폭도

핵심은 이렇습니다.
플레이어가 실제로 배우는 것은 “불은 세다”가 아니라,
**불은 (\rho)를 (\tau)와 방출로 잘 바꾸고, 땅은 (\kappa)와 (P)를 강화하며, 물은 (\phi)와 (\rho)를 평형화한다**는 식의 재료감이어야 합니다.

---

## 4. 세계/사용자/환경 상태도 분리해야 한다

물리계층은 세계 상태만 있으면 부족합니다. 최소한 아래 세 층이 있어야 합니다.

### 4.1 세계 셀 상태

[
m(c,\ell,t) = [\rho,\kappa,\phi,\tau,\chi,\nu,\beta]
]

### 4.2 환경 파라미터

[
e(c,\ell) = [p,\eta,w,b,s,r]
]

* (p): 투과성 / 다공성
* (\eta): 전도성
* (w): 습윤도
* (b): 생기 밀도 / 생물성
* (s): 구조 안정도
* (r): 학파 공명도

즉, “맵 태그”가 아니라 **공통 상태변수에 영향을 주는 수치 환경**입니다.

### 4.3 사용자 상태

[
a_u(t) = [f,h,\theta,\mu,\alpha]
]

* (f): 집중력
* (h): 체력/지구력
* (\theta): 꿈결 내성
* (\mu): body-channel capacity
* (\alpha): 학파 조율도

이 사용자 상태가 필요한 이유는 `염동`, `영혼`, `사념`, `武`, 과부하 반동이 전부 **사용자 몸/정신과의 coupling**을 필요로 하기 때문입니다.

---

## 5. 기본 동역학 초안

핵심 설계 원칙은 **완전 보존계가 아니라, 지역적으로는 거의 보존적이고 장기적으로는 열린 계(open system)** 로 두는 것입니다.

즉, 전투 시간척도에서는 꿈결이 “그 자리에서 재분배”되는 느낌이 강해야 하고, 긴 시간척도에서는 환경과 사용자가 다시 채웁니다. 그래야 환경, 배치, 중첩, 잔재가 의미를 가집니다.

가장 바깥 수식은 이렇게 둘 수 있습니다.

[
m_{c,\ell}^{t+1}=
\mathrm{clip}\Big(
m_{c,\ell}^{t}
+\Delta t\big[
F_{\text{env}}(m,e)
+\sum_j F_{\mathcal K_j}(m,e,a,q; c,\ell)
+\sum_{n\in \mathcal N(c)}T_{n\rightarrow c}(m)
\big]
\Big)
]

뜻은 단순합니다.

* (F_{\text{env}}): 환경이 원래 하려는 일
  (느린 회복, 느린 감쇠, 습윤/전도/생기 영향)
* (F_{\mathcal K_j}): 활성 주문 (j)가 끼치는 영향
* (T_{n\rightarrow c}): 이웃 셀과의 교환
  (확산, 이동, 위상 동기화, 오염 전파)

### 5.1 추천 기본 방정식

직관적으로 설명하면:

* (\rho): 모으고, 이동하고, 새고, 다시 차오른다
* (\kappa): 봉인과 축으로 올라가고, 긴장/오염/무화로 내려간다
* (\phi): 경로와 공전으로 움직이고, 물과 축으로 정렬된다
* (\tau): 불/전기/과잉 공명으로 쌓이고, 안정화로 풀린다
* (\chi): 독/악몽으로 퍼지고, 정화로 걷힌다
* (\nu): 무화로 생기고, 주변 매질이 서서히 메운다
* (\beta): 생명으로 자라고, 독/불에 깎인다

이를 초안식으로 적으면:

[
\begin{aligned}
\dot{\rho} &= r_\rho(\rho_{\text{env}}-\rho) - u_{\text{draw}} - a_\nu \nu \rho + \nabla\cdot(D_\rho \nabla \rho) \
\dot{\kappa} &= r_\kappa(\kappa_{\text{env}}-\kappa) + s_{\text{bound}} + s_{\text{axis}} + s_{\text{sacred}} - b_\tau\tau - b_\chi\chi - b_\nu\nu \
\dot{\phi} &= \omega_{\text{path}} + \omega_{\text{orbit}} + \lambda_{\text{sync}}\Delta\phi + \xi \
\dot{\tau} &= g_{\text{release}} + g_{\text{grad}} + g_{\text{loop}} - d_\tau \tau \
\dot{\chi} &= s_{\text{poison}} + D_\chi\Delta\chi - p_{\text{purify}} \
\dot{\nu} &= s_{\text{void}} - d_\nu \nu + r_{\text{refill}}(\rho,\kappa) \
\dot{\beta} &= g_{\text{life}}\beta(1-\beta/K) + r_{\text{water}} - d_{\text{poison}} - d_{\text{fire}}
\end{aligned}
]

여기서 중요한 것은 방정식의 “정확한 상수”보다 **부호와 상호작용 방향**입니다.
이 부호 체계가 바로 창발성의 근간입니다.

### 5.2 반드시 유지해야 할 반-보존 법칙

[
\frac{d}{dt}\sum_{x\in A}\rho_x
===============================

I_{\text{env}}(A)
+I_{\text{caster}}(A)
-O_{\text{release}}(A)
-O_{\text{leak}}(A)
-O_{\text{void}}(A)
]

의미는 이렇습니다.

* 꿈결은 전투 중 마구 새로 생기지 않는다.
* 대부분은 **모으고, 이동하고, 봉인하고, 방출하고, 새는 것**이다.
* 그래서 같은 주문도 장소와 배치에 따라 다르게 작동한다.

이 반-보존 구조가 없으면, 환경과 다중 마법진 배치가 거의 전부 “데미지 배율 장식”으로 추락합니다.

---

## 6. 언어 → 물리 커널 매핑

현재 언어체계는 이미 충분히 좋습니다. 중요한 것은 이것을 **공통 커널 언어**로 내리는 것입니다.

권장 수식은 아래처럼 두면 됩니다.

[
\mathcal K
==========

\mathcal M^{3D}*{\text{school}}
\circ
\mathcal L*{\text{3D}}
\circ
\mathcal M_{\text{praxis}}
\circ
\mathcal M_{\text{ontic}}
\circ
\mathcal M_{\text{material}}
\circ
\mathcal R_{\text{root}}(G,q,e,a)
]

여기서

* (\mathcal R_{\text{root}}): root seed가 만드는 기본 커널
* (\mathcal M_{\text{material}}): 독/강철/전기/얼음 같은 파생 변형
* (\mathcal M_{\text{ontic}}): 영혼/사념/신성/無
* (\mathcal M_{\text{praxis}}): 염동/武
* (\mathcal L_{\text{3D}}): stack/extrusion/orbit/tilt/bridge
* (\mathcal M^{3D}_{\text{school}}): 학파의 3D profile

즉, **root가 기저 물리편향을 만들고**, 나머지는 그 기저를 비틀고, 배치하고, 우선순위를 바꾸고, 채널을 재결선합니다.

### 6.1 Root seed의 물리 역할

| Root   | 지배적 물리작용                             | 대표 양의 피드백                | 대표 실패              |
| ------ | ------------------------------------ | ------------------------ | ------------------ |
| 바람     | (\rho,\phi,\mathbf u) 이동성 증가, 투과성 증가 | 순환/편류 유지                 | 분산, 엉뚱한 누출         |
| 땅      | (\kappa,P) 증가, 감쇠/정착                 | containment loop         | 경직, 파단             |
| 불꽃     | (\rho \rightarrow \tau) 전환, 방출/격발    | release loop             | flashback, 과열      |
| 물      | 확산/평형화, 위상 동기화, 완충                   | circulation/cooling loop | 과도한 누수             |
| 식물(생명) | (\beta) 증식, 패턴 재사용, 얽힘               | growth loop              | runaway bloom, 기생화 |

### 6.2 파생/상태/실행 방식의 물리 역할

* **독**: (\beta \rightarrow \chi) 전환, (\kappa) 침식, 느린 확산
* **강철**: (P,\kappa) 상승, capacity 상승, 대신 brittle threshold 도입
* **전기**: (\phi) 차이와 (\tau) 축적을 pulse discharge로 바꿈
* **얼음**: 확산 억제, (\kappa) 고정, arrest 상태 유도
* **영혼**: 물질 환경보다 agent/mind 채널과 강하게 결합
* **사념**: 위상 정렬과 원격 조준 성능 상승, 재귀/내향 반향 강화
* **신성**: (\chi) 억제, (\kappa) 정화, symmetry/phase-lock 가중치 상승
* **無**: (\rho) 모집과 coupling을 끊는 null pocket 생성
* **염동**: caster focus를 외부 (\mathbf u) 및 remote port 제어로 재배선
* **武**: 외부 field throughput을 body-axis/weapon-axis throughput으로 전환

여기서 `無`와 `武`는 반드시 조심해야 합니다.
이 둘이 만능 우회키가 되면 복합 field 설계의 가치가 급락합니다.
특히 `武`는 **짧은 범위·강한 자기 위험·낮은 외부 shaping 자유도**를 가져야 합니다.

---

## 7. Core primitive의 물리 해석

지금까지 확정한 primitive는 그대로 쓰되, 의미가 아니라 **국소 연산자**로 해석해야 합니다.

* **핵(nucleus)**
  source/sink locus. 모집, 집중, 점화의 기준점
* **경계(boundary)**
  막 경계. (P)와 capacity를 부여하고, (\kappa)를 올림
* **개구(aperture)**
  경계 위의 gate. 선택적 유출입
* **경로(path)**
  (\mathbf u)를 생성하는 방향성 운반장
* **결속(link)**
  두 locus 사이의 conduit. directed coupling
* **차단(block)**
  특정 채널에 대한 impedance / cancellation
* **축(axis)**
  mirror coupling, phase-lock, symmetry bonus
* **분기(branch)**
  flux splitting. 보존 가중치 기반 분배
* **반복(echo)**
  delay buffer. 일정 시간 뒤 재주입
* **자기유사(fractal)**
  축소 복사된 attenuated subkernel 생성

이 primitive들이 중요한 이유는, 이것들이 바로 **local rule set** 이기 때문입니다.
창발성은 대개 “고수준 effect name”이 아니라 “이런 local operators가 어떻게 얽히는가”에서 나옵니다.

---

## 8. 3D operator의 물리 역할

3D는 독립 언어가 아니라, 반드시 **연산 의미**만 바꿔야 합니다.

* **stack**
  계산 순서와 우선권을 바꿈. 상위 층이 control, 하위 층이 payload
* **extrusion**
  chamber volume, dwell time, capacity를 바꿈
* **orbit**
  주기적 재주입과 회전 운반장을 만듦
* **tilt**
  어떤 표면/방향으로 coupling할지 바꿈
* **bridge**
  레이어 간 directed conduit를 만듦

가장 중요한 점은 이것입니다.
3D는 **피해량 배수기**가 아니라 **coupling topology 변경자**여야 합니다.
즉, 3D를 쓰면 상태변수의 흐름 구조가 바뀌어야 합니다.

### 학파의 3D 개입 방식

사용자 결정대로, 학파는 2D core semantics에는 들어가지 않고, 3D에서만 강하게 개입하는 것이 맞습니다.
그래서 학파는 아래처럼 구현하는 편이 가장 안정적입니다.

* 길몽: (\kappa) 정렬, 정화, 안정된 layer coupling
* 악몽: (\tau,\chi) 증폭, 뒤틀린 orbit, 감금형 extrusion
* 몽중몽: echo retention, recursive delay, nested stack
* 곁잠: side-coupling, bridge 효율, relay/parasite 성질

즉, 학파는 **새 root를 만들지 않고, 3D 연산의 커널 행렬을 바꾸는 상위 프로파일**입니다.

---

## 9. 품질 벡터는 물리계층에서 이렇게 써야 한다

정체성은 compile 단계에서 확정되고, 품질은 물리계층의 계수에 들어가야 합니다.

* `q_closure` → boundary permeability 감소, leak 감소
* `q_sym` → axis lock 및 phase synchrony 강화
* `q_smooth` → transport loss 감소
* `q_order` → ignition ramp 안전성 증가
* `q_tempo` → 점화 지연/상승시간 변화
* `q_overshoot` → 불필요한 (\tau) spike 증가
* `q_phase` → resonance/orbit lock 질 향상
* `q_topo`, `q_constraint` → hard validity와 residue 형태 결정

이렇게 해야 “잘 그렸다”가 단순 전능 버프가 아니라,
**어떤 종류의 주문을 어떤 스타일로 더 안정적이거나 더 위험하게 만든다**로 작동합니다.

---

## 10. UX를 위해 반드시 필요한 “이벤트 추출층”

연속장만 있으면 플레이어는 배울 수 없습니다.
그래서 물리계층 위에 **event extraction layer**가 있어야 합니다.

권장 이벤트는 아래 정도입니다.

* **Seal Achieved**
  rim 평균 (\kappa)가 기준 이상이고 leak가 작을 때
* **Leak**
  aperture 외의 경계에서 유량이 기준을 넘을 때
* **Phase Lock**
  (\phi)의 circular variance가 작을 때
* **Resonance**
  echo/orbit loop gain이 기준을 넘을 때
* **Rupture**
  (\tau > \tau_{\text{break}}(\kappa, material))
* **Taint Breach**
  (\chi)가 경계를 넘어 외부 확산으로 전환될 때
* **Null Collapse**
  (\nu)가 높고 (\rho)가 임계 이하로 떨어질 때
* **Bloom**
  (\beta)가 자기증식 임계 이상으로 올라갈 때
* **Relay Established**
  bridge/link를 통한 안정된 전달 경로가 생겼을 때

예를 들면,

[
\text{Seal} = \mathbf 1{\bar{\kappa}*{\text{rim}}>\theta*\kappa \ \land\  \Phi_{\text{leak}}<\varepsilon}
]

[
\text{Rupture} = \mathbf 1{\tau>\tau_{\text{break}}(\kappa,s,P)}
]

이 이벤트들이 중요한 이유는,
플레이어는 “숫자”가 아니라 **이산적 학습 사건**으로 시스템을 이해하기 때문입니다.

이 층은 다음을 모두 공급해야 합니다.

* VFX/SFX cue
* 로그/리플레이 메타데이터
* codex 발견 기록
* AI/도우미의 설명 문장
* diff/비교 UI의 차이 포인트

---

## 11. 창발성을 실제로 만드는 피드백 루프 라이브러리

Dormans 관점에서 보면, 진짜 재미는 개별 효과보다 **피드백 구조**에 있습니다. 그래서 물리계층 설계 시 아래 루프를 명시적으로 지원해야 합니다. 

### 11.1 권장 루프 6종

1. **Containment Loop**
   boundary/earth/sacred → (\kappa) 증가 → (\rho) retention 증가 → 더 안정된 boundary
   리스크: rupture 시 축적분이 한 번에 터짐

2. **Release Loop**
   fire/electric → (\rho \rightarrow \tau) 전환 → discharge → 큰 효과
   리스크: flashback, self-damage, phase scatter

3. **Circulation Loop**
   water/air/path/orbit → (\rho,\phi) 순환 유지 → sustain 향상
   리스크: uncontrolled spread, leak meta

4. **Growth Loop**
   life/echo/fractal → (\beta) 증식 → 패턴 복제 → 더 많은 local effect
   리스크: runaway bloom, parasitic behavior

5. **Corruption Loop**
   poison/nightmare → (\chi) 상승 → (\kappa) 저하 → 더 많은 breach → 더 많은 (\chi)
   리스크: map denial, opaque failure

6. **Null Loop**
   void → recruitment/coupling 차단 → 기존 loop 정지
   리스크: 너무 강하면 모든 조합을 평탄화

이 여섯 루프가 서로 교차해야 “발견”이 생깁니다.
예를 들어 `water + orbit + electric`은 circulation loop 위에 release loop가 얹히고,
`life + sacred + boundary`는 growth loop 위에 containment loop가 얹힙니다.

---

## 12. 잔재(residue) 설계: 실패도 실험 자산이어야 한다

사용자 창발 UX를 최우선으로 본다면, miscast와 unstable spell은 “그냥 실패”가 아니라 **관측 가능한 잔재**를 남겨야 합니다.

권장 잔재 타입:

* **Leak plume**: (\rho + \chi) 가벼운 누수 구름
* **Fracture shell**: 깨진 (\kappa) 경계 조각
* **Phantom orbit**: 위상은 남았지만 source는 사라진 잔류 공전
* **Null scar**: (\nu)가 남긴 빈 흔적
* **Parasitic sprout**: 과도한 (\beta)의 오작동 가지
* **Arc remnant**: electric discharge 후 남은 위상 균열

단, 이 잔재는 **강력하지만 불안정**해야 합니다.
잔재가 정규 주문보다 항상 강하면, 플레이어는 일부러 오작동을 최적화하게 됩니다.

---

## 13. UX 최우선 관점에서 물리계층이 반드시 제공해야 하는 플레이어 단서

Shneiderman은 creativity-support 도구에서 what-if, process visualization, replayable history가 핵심이라고 봤습니다. 따라서 물리계층은 단순 계산 엔진이 아니라 **관측 가능한 학습 엔진**이어야 합니다. ([Creativetech][2])

권장 규칙은 아래와 같습니다.

### 13.1 각 상태변수는 반드시 2개의 표현을 가져야 함

* **전투용 은유적 단서**
* **forge용 분석 오버레이**

예:

* (\rho): 전투에서는 안개/입자 농도, forge에서는 density heatmap
* (\kappa): 전투에서는 선명도/봉인음, forge에서는 coherence ring overlay
* (\phi): 전투에서는 맥동/회전, forge에서는 phase wheel
* (\tau): 전투에서는 crackle/떨림, forge에서는 stress contour
* (\chi): 전투에서는 얼룩/탁도, forge에서는 contamination map
* (\nu): 전투에서는 소리 감쇠/공백 질감, forge에서는 null mask
* (\beta): 전투에서는 맥동 생기, forge에서는 growth potential overlay

### 13.2 forge와 combat는 같은 물리, 다른 가시성

* **forge**: 모든 채널·이벤트·리플레이·환경 스윕 제공
* **combat**: 은유적 cue와 핵심 syndrome만 제공

### 13.3 물리계층은 리플레이 가능한 trace를 내놔야 함

최소한 저장해야 할 것:

* 채널 평균/최대 변화량
* 발생 이벤트 타임라인
* dominant loop tag
* rupture/leak/phase-lock 원인
* residue 생성 경로

이것이 나중에 branch/merge/compare UI의 바탕이 됩니다.

---

## 14. 개발용 설계 렌즈도 필요하다

Dormans가 말하듯, 디자이너는 동적 구조를 모델로 봐야 tuning이 됩니다. 그래서 런타임 외에 **개발용 loop lens** 가 별도로 필요합니다. ([dl.ifip.org][3])

권장 도구:

* **kernel inspector**: root/derivation/process가 어떤 채널을 얼마나 건드리는지
* **feedback loop scanner**: positive/negative loop 탐지
* **parameter sweep harness**: 환경/품질/학파/3D operator 스윕
* **residue analyzer**: 실패가 어디서 생겼는지 구조적으로 해석
* **counterfactual runner**: 같은 spell을 10개 환경에서 자동 테스트

이런 도구가 없으면 후반 밸런싱이 거의 불가능해집니다.

---

## 15. 단계별 구현 초안

전체를 한 번에 만들면 실패 확률이 높습니다. 창발 UX를 최우선으로 보면 아래 순서가 낫습니다.

### 1단계: 2D 핵심 물리

* persistent channel 7개 도입
* root 5개 + material 4개 + ontic 일부
* primitive 중 nucleus/boundary/aperture/path/link/axis/echo 우선
* event detector 5개 정도 우선
* forge overlay와 replay 먼저 완성

목표: “같은 주문이 환경과 배치에 따라 진짜로 다르게 느껴지는가” 검증

### 2단계: productive failure와 graph 조합

* residue 추가
* multi-circle hyperedge
* school 2D bias
* ontic/praxis 확장 (`영혼`, `사념`, `無`, `염동`, `武`)

목표: “실패도 실험 자산인가” 검증

### 3단계: 3D layer ops

* stack/extrusion/orbit/tilt/bridge
* school 3D profile
* layered sparse solver 고도화

목표: “3D가 stat booster가 아니라 topology changer인가” 검증

---

## 16. 가장 위험한 실패 패턴

냉정하게 보면, 물리계층에서 특히 위험한 건 다섯 가지입니다.

첫째, **채널이 너무 많아 플레이어가 아무것도 못 배우는 경우**입니다.
그래서 persistent channel은 7개를 넘기지 않는 편이 좋습니다.

둘째, **`無`와 `武`가 모든 구조를 우회하는 만능키가 되는 경우**입니다.
이 경우 복합 graph 연구가 전부 가치 하락합니다.

셋째, **3D가 수치 버프일 뿐 구조 변화를 만들지 못하는 경우**입니다.
이러면 3D는 손기술 세금이 됩니다.

넷째, **오작동 잔재가 정규 주문보다 효율적인 경우**입니다.
그러면 유저는 의도적 miscast를 메타화합니다.

다섯째, **환경 지속성이 너무 강해 전투가 snowball 되는 경우**입니다.
환경은 의미 있어야 하지만, 판을 영구히 망가뜨리면 안 됩니다.

---

## 17. 사용자 승인 필요 항목

아래는 지금 단계에서 꼭 결정을 받아야 하는 항목입니다.

| 항목                  | 권장안                                                 | 이유                               | 다른 선택의 위험                          |
| ------------------- | --------------------------------------------------- | -------------------------------- | ---------------------------------- |
| **A. 공통 채널 수**      | **7 persistent + 3 derived**                        | `無/武/생명/독/신성`까지 무리 없이 설명 가능      | 5채널로 줄이면 의미가 뭉개지고, 8개 이상이면 학습성이 급락 |
| **B. 자원 경제 방식**     | **지역 반-보존형**                                        | 환경·배치·다중 상호작용이 살아남음              | caster 생성형이면 조합이 stat combo로 축소    |
| **C. 공간 표현**        | **layered sparse 2.5D field**                       | 현재 2D-first 철학과 성능, 결정성에 가장 잘 맞음 | full voxel은 과도하게 비싸고 UX도 불투명       |
| **D. 실패 잔재 허용 범위**  | **combat에도 1차 잔재 허용, 단 강한 비용 부여**                   | 실패가 연구 자산이 됨                     | forge 전용으로만 두면 전투 창발성이 약해짐         |
| **E. 학교 3D 개입 범위**  | **layer coupling / gain / order 변경까지만 허용**          | 2D 공유 언어 보존 가능                   | 새로운 root semantics까지 건드리면 언어가 쪼개짐  |
| **F. 武의 특권 여부**     | **특권 없음, body-channel cap 엄격 적용**                   | 복합 field 언어의 가치 보존               | 특권을 주면 메타가 체현형으로 단일 수렴             |
| **G. 환경 지속성 시간척도**  | **전투 10–30초 수준의 국소 지속, forge는 장시간**                 | 환경 의미와 전투 템포의 균형                 | 너무 짧으면 환경이 무의미, 너무 길면 snowball     |
| **H. 멀티플레이 결정성 요구** | **PvP/동기화 계획이 있으면 고정소수점 고정틱**                       | 재현성, 리플레이, 공정성 확보                | 나중에 붙이면 거의 재작성 수준                  |
| **I. 플레이어 노출 수준**   | **forge는 full telemetry, combat은 diegetic cues 중심** | 장인정신과 몰입 모두 확보                   | 둘 다 숨기면 학습 불가, 둘 다 다 보여주면 몰입 붕괴    |

---

## 18. 최종 권장 초안

현재 언어체계 위에 얹을 물리계층 초안은 다음 한 문장으로 요약할 수 있습니다.

**“2D 중심의 canonical spell graph를, 7개 공통 꿈결 상태변수 위에서 작동하는 layer-aware local operator kernel로 컴파일하고, 그 결과를 discrete event detector와 replayable trace로 다시 플레이어 경험으로 환원하는 반-보존적 공통 매질 시뮬레이션”**

더 짧게 말하면:

* 언어는 **무엇을 하려는가**
* 물리는 **그것이 세계에서 어떻게 번지는가**
* UX는 **그 차이를 어떻게 배우게 하는가**

지금 구조에서는 이 방향이 가장 강합니다.
핵심은 주문별 템플릿이 아니라, **공통 상태변수 (\rho,\kappa,\phi,\tau,\chi,\nu,\beta)** 와
그 위를 도는 **containment / release / circulation / growth / corruption / null** 루프를 먼저 고정하는 것입니다.
이것이 고정되면, 나머지 root, 파생, 학파, 3D는 그 위에서 자연스럽게 의미를 갖습니다.

[1]: https://staff.itee.uq.edu.au/janetw/papers/PhD%202006%20Sweetser.pdf "Microsoft Word - Full Thesis - final for binding.doc"
[2]: https://creativetech.mat.ucsb.edu/readings/schneiderman_creativitysupport.pdf "https://creativetech.mat.ucsb.edu/readings/schneiderman_creativitysupport.pdf"
[3]: https://dl.ifip.org/db/conf/iwec/icec2012/Dormans12.pdf "The Effectiveness and Efficiency of Model Driven Game Design."

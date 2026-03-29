request



아래의 초기 아이디어 관점에서 유저별 속도, 습관, 개인적 편향은 해당 구조 내에서 어떻게 적용되어야 할지 추가 검토. 화면에 마법진을 그리면(한번에 하나 혹은 여러개), 그리는 속도, 각도, 품질, 정확도, 맵 환경에 따라 다른 마법(효과가 달라지는 것. 모양에 따라 케이스는 고정하는 형태)
---


answer




핵심 판단부터 정리하면, 현재 구조에서 **유저별 속도·습관·개인적 편향은 “의미 결정”이 아니라 “입력 해석 보정 + 실행 accent + 장기적 작법 정체성”으로 분리해서 넣어야** 합니다. 그렇게 해야 두 가지를 동시에 지킬 수 있습니다. 첫째, 같은 모양을 따라 그리면 같은 주문이 나와야 하는 재현성입니다. 둘째, 플레이어마다 “내 손맛”, “내 작법”, “내 성향”이 살아야 한다는 체감입니다. 실제로 펜/스트로크 입력 연구에서는 **개인 내부의 일관성은 높고, 사람들 사이의 일관성은 훨씬 낮다**는 결과가 나왔습니다. Wobbrock 계열 연구는 113명, 40,305개 샘플에서 within-user consistency가 .91, between-user consistency가 .55였고, 87%의 제스처가 within-user agreement .75 이상이었지만 between-user는 32%만 .75 이상이었습니다. Sezgin과 Davis도 특정 도메인에서는 사람들이 객체를 그릴 때 **선호하는 획순**이 있고, 그 일관성을 개인 모델에 쓰면 인식에 도움이 된다고 봤습니다. 즉, **개인차는 분명히 존재하고 안정적이지만, 그것을 곧바로 의미론으로 올리면 공유 재현성이 깨집니다.** ([UW Faculty][1])

또 한 가지는 기술적 전제입니다. 온라인 잉크는 단순 이미지가 아니라 **시간 순서가 있는 스트로크 데이터**이고, 최근 연구도 이미지 OCR만 쓰는 것보다 time-ordered digital ink를 직접 쓰는 쪽이 낫다고 보고합니다. 동시에 사용자 적응은 **적은 데이터로, 빠르게, 점진적으로** 일어나야 하며, 그렇지 않으면 사용자는 매번 인식 결과를 검증하게 되어 pen interface의 장점이 무너진다고 요약됩니다. 실제 personalization 연구에서는 소량의 사용자 데이터만으로도 baseline error 10.3%를 9.1%까지 낮추고, 21명 중 18명에서 개선이 관찰됐습니다. 이 근거를 그대로 가져오면, 당신 시스템의 정답은 **개인화는 강하게 하되, 적용 위치를 엄격히 제한하는 것**입니다. ([arXiv][2])

제가 권하는 한 줄 원칙은 이것입니다.

**개인화의 강도는 `Parser > Feedback > Runtime Accent > Semantics` 순으로 급격히 낮아져야 합니다.**

조금 더 풀어 쓰면 이렇습니다.

| 층                          | 개인화 강도 | 들어가야 하는 것                                               | 들어가면 안 되는 것                  |
| -------------------------- | -----: | ------------------------------------------------------- | ---------------------------- |
| **의미론 / SpellID**          |      0 | 없음                                                      | 같은 모양인데 사람마다 다른 spell family |
| **파서 / 후보 랭킹**             |   매우 강 | 획순 습관, 속도 baseline, 회전·기울기 습관, overtrace, pause pattern | 유효하지 않은 구조를 억지로 정답 처리        |
| **실시간 피드백**                |      강 | “당신 평소 closure보다 지금 leak가 큼” 같은 개인화 syndrome            | 자동으로 선을 고쳐서 다른 주문으로 바꿔 주기    |
| **실행 accent / 스탯**         |    약~중 | tempo comfort, style fit, 작은 trade-off                  | 개인 스타일이 곧바로 상위 호환 DPS가 되는 것  |
| **장기 정체성 / codex / 학파 공명** |    중~강 | 반복되는 작법 성향, graph 선호, 3D 해석 편향                          | 2D core semantics 자체 변경      |

이제 요소별로 보면 더 선명합니다.

## 1. 속도는 두 번 쓰면 안 됩니다

속도는 가장 조심해야 합니다. 온라인 잉크에서는 사용자가 느리게 그리면 점이 더 촘촘히 찍히고, 빠르게 그리면 샘플이 듬성듬성해집니다. 그래서 전처리 단계는 이미 **상대 속도로 인해 생긴 점 밀도 불균형을 정규화**해야 합니다. 이 정규화를 안 해놓고 나서 또 “빠르게 그렸으니 다르다”고 평가하면, 같은 속도가 입력 표현에도 한 번, 게임 판정에도 한 번 들어가서 이중으로 작동합니다. 그건 설계상 오류입니다. ([NYU Computer Science][3])

따라서 속도는 두 단계로 분리해야 합니다.

첫 번째 속도는 **센서/샘플링 보정용 속도**입니다.
이건 전처리에서 지워야 합니다. resampling, smoothing, point-density normalization으로 처리합니다.

두 번째 속도는 **의식적 시전 tempo** 입니다.
이건 남겨야 합니다. 예를 들어:

* 전체 시전 시간
* local acceleration burst
* pause/hesitation density
* segment별 tempo regularity
* closure 직전 감속 여부
* orbit/echo 구간의 리듬 정합성

이 tempo는 spell family를 바꾸면 안 되고, **같은 family 내 accent**를 바꿔야 합니다.
예를 들면:

* 빠르고 결정적인 tempo: 발동 지연 감소, release sharpness 증가, 대신 (\tau) 긴장과 backlash 위험 증가
* 느리고 일정한 tempo: (\kappa) 정합도와 phase lock 증가, 대신 시전 시간 증가
* pause가 많은 tempo: 불안정, leak, null scar, incomplete seal 위험 증가

여기서 중요한 것은, **플레이어마다 편한 tempo band가 다르다**는 점입니다. Wobbrock 계열 분석에서는 faster gesture entry가 더 비정상적인 articulation과 함께 가지 않았고, 오히려 빠른 입력이 더 잘 인식된다는 prior finding도 같이 언급됩니다. 그러므로 “빠른 유저=무조건 불리/유리”로 보면 안 됩니다. 속도는 **절대 판정 + 개인 comfort band**의 혼합이어야 합니다. ([UW Faculty][1])

가장 안전한 방식은 이겁니다.

[
q_{\text{tempo-final}}
======================

(1-\alpha),q_{\text{tempo-abs}}
+
\alpha,q_{\text{tempo-comfort}}
]

여기서

* (q_{\text{tempo-abs}}): 해당 family가 요구하는 객관적 tempo band와 얼마나 맞는가
* (q_{\text{tempo-comfort}}): 이 플레이어의 안정적인 tempo 습관과 얼마나 잘 맞는가
* (\alpha): 개인화 가중치

권장값은 (\alpha \approx 0.15\sim0.25) 입니다.
이렇게 하면 개인 손맛은 살아나지만, 나쁜 습관이 곧 최적해가 되지는 않습니다.

## 2. 각도는 “의미 있는 각도”와 “손버릇 각도”를 분리해야 합니다

각도도 분해가 필요합니다. 온라인 필기 쪽 최근 preprint도 회전 변형이 spatial layout를 무너뜨려 인식 정확도를 떨어뜨린다고 적고 있고, 고전 필기 시스템도 translation/scale/rotation normalization을 front-end 문제로 다뤄 왔습니다. 즉, **전역 기울기나 손의 slant는 원래 인식기의 적**에 가깝습니다. ([arXiv][4])

그래서 각도는 세 종류로 쪼개야 합니다.

첫째, **전역 회전(global rotation)**
원 전체가 살짝 기울어진 것.
이건 대체로 normalize해야 합니다.

둘째, **문법적으로 명시된 방향(angle-as-grammar)**
path의 방사 방향, aperture의 열린 방향, port가 향하는 방향, tilt operator, orbit 회전 방향 같은 것.
이건 의미로 써도 됩니다.

셋째, **개인 손버릇의 slant / entry angle**
왼손잡이라서 시작점이 다르거나, 항상 아래쪽에서 올라가거나, 시계/반시계 편향이 있는 것.
이건 parser prior로만 써야 합니다.

따라서 규칙은 매우 간단합니다.

* **명시적 anchor/port/tilt가 없는 각도는 의미가 아니다.**
* **명시적 문법 슬롯에 들어간 각도만 의미가 된다.**

이 원칙이 없으면 “왼손잡이의 비스듬한 ward가 의도치 않게 다른 주문이 되는” 식의 UX 파탄이 생깁니다.
현재 구조에서는 특히 다음만 semantic angle로 인정하는 것이 안전합니다.

* aperture 방향
* path/beam 방사 방향
* axis 정렬 방향
* tilt operator 방향
* orbit의 시계/반시계
* world-aligned port 방향

나머지 각도 습관은 전부 **개인화 보정**으로 보내는 것이 맞습니다.

## 3. 습관은 “저수준 motor habit”와 “고수준 compositional habit”로 나눠야 합니다

“습관”을 한 덩어리로 두면 안 됩니다.

### 3-1. 저수준 motor habit

이건 직접 손의 움직임과 관련된 습관입니다.

* 시계/반시계 선호
* 시작점 선호
* closure를 어디서 닫는지
* oversketch / overtrace 빈도
* pause 위치
* 급가속 구간
* 곡선 vs 직선 선호
* symmetry를 어디서 맞추는지
* stroke order 안정성

이 계층은 Sezgin과 Davis가 말한 preferred stroke ordering, Wobbrock의 within-user consistency와 아주 잘 맞습니다. 이건 **개인화 파서**의 재료로 매우 유용합니다. 반대로 runtime semantics로 너무 세게 올리면, 플레이어는 “내 습관이 게임 규칙이 됐다”는 느낌보다 “게임이 나를 틀에 가뒀다”는 느낌을 받기 쉽습니다. ([UW Faculty][1])

그래서 저수준 습관은 아래에 강하게 넣는 것이 맞습니다.

* primitive proposal ranking
* n-best parse ordering
* error syndrome personalization
* beautification ghost alignment
* comfort band 추정

그리고 아래에는 약하게만 넣는 것이 맞습니다.

* latency/stability accent
* family-specific ideal tempo shift

### 3-2. 고수준 compositional habit

이건 “무엇을 자주 설계하는가”에 관한 습관입니다.

* concentric containment를 선호하는가
* tangency relay를 자주 쓰는가
* echo/fractal recursion을 선호하는가
* null overlay를 자주 섞는가
* bridge-heavy 3D 구조를 좋아하는가
* multi-circle을 순차 배치로 푸는가, 동시 graph로 푸는가

이건 실시간 전투 power보다 **forge / codex / lineage / recommendation**에 넣는 편이 맞습니다.
즉, 시스템은 이런 습관을 보고 “당신은 containment-dominant artisan”, “relay-heavy conductor”, “recursive dreamer” 같은 식으로 codex를 만들어 줄 수는 있지만, 전투에서 같은 canonical graph를 사람마다 다른 spell로 바꿔 버리면 안 됩니다.

정리하면,

* 저수준 습관 = 파서와 feedback에 강하게
* 고수준 습관 = codex와 forge UX에 강하게
* 의미론 = 둘 다 금지

## 4. 개인적 편향은 “숨은 soulprint”로 두되, 0-sum trade-off만 허용하는 것이 맞습니다

여기서 개인적 편향은 단순 motor habit보다 큽니다.
현재 세계관에서는 “꿈결에 대한 각자의 해석”이 있으니, 플레이어마다 미묘한 작법 정체성이 생기는 것은 오히려 자연스럽습니다. 다만 이것이 **2D core semantics**를 바꾸면 안 됩니다. 당신이 이미 정한 원칙과도 맞지 않습니다.

그래서 권장하는 방법은, long-term history에서 아주 천천히 학습되는 **caster soulprint vector**를 두는 것입니다.

예를 들면:

[
\psi_u =
[\psi_\rho,\psi_\kappa,\psi_\phi,\psi_\tau,\psi_\chi,\psi_\nu,\psi_\beta]
]

각 성분은 앞서 정의한 물리 채널에 대한 미세한 선호를 뜻합니다.

* (\psi_\tau)가 높다: 방출/긴장/격발 쪽으로 기운다
* (\psi_\kappa)가 높다: 정합/봉인/구조 안정 쪽으로 기운다
* (\psi_\phi)가 높다: 위상/공명/회전 정렬 쪽으로 기운다
* (\psi_\nu)가 높다: 무화/차단/절단 성향이 강하다
* (\psi_\beta)가 높다: 생장/재귀/증식 쪽으로 기운다

하지만 이 벡터는 반드시 **zero-sum 혹은 bounded trade-off** 로 써야 합니다.

[
\sum_i w_i \psi_i = 0,\qquad |\psi_i| \le \epsilon
]

즉, 개인 성향은 “공짜 파워”가 아니라 **강점과 약점의 재배치**여야 합니다.
예를 들면:

* 빠르고 공격적인 유저: (+\tau), (-\kappa)
* 신중하고 정렬 잘 맞추는 유저: (+\kappa,+\phi), (-release sharpness)
* 재귀/실험형 유저: (+\phi,+\beta), (-stability margin)

이렇게 해야 개인차가 살아 있으면서도 “특정 손버릇 = 상위 호환”이 되지 않습니다. 개인화 연구에서도 personalization은 성능 향상에 유효하지만, 보통 writer-independent baseline 위에 **추가 적응**으로 붙는 것이지 전체 의미 체계를 갈아엎는 방식은 아닙니다. ([Microsoft][5])

## 5. 현재 구조에는 “세 개의 개인화 프로필”이 들어가는 것이 가장 안전합니다

가장 권장하는 설계는 아래 셋입니다.

### (a) Session Motor Model

세션 단위의 빠른 보정입니다.

[
x_u^{\text{sess}}
]

담는 것:

* 오늘의 평균 tempo
* slant
* pause density
* oversketch tendency
* 손 떨림/피로로 인한 jitter
* 현재 장치의 샘플링 특성

용도:

* parser prior
* on-the-fly feedback
* point-density normalization 이후의 tempo band 추정

업데이트:

* 빠르게 업데이트
* 실패 stroke도 아주 약하게 반영 가능
* 세션 종료 시 decay

이 프로필은 “오늘 컨디션”을 잡는 용도입니다.

### (b) Family Habit Model

마법 family별로 쌓이는 작법 습관입니다.

[
x_{u,f}^{\text{fam}}
]

담는 것:

* fire/electric에서의 burst tempo
* earth/ice/sacred에서의 closure 스타일
* water/air의 sweep regularity
* life/soul/thought의 pulse rhythm

용도:

* family-specific comfort band
* 개인화된 guidance
* family 내 style accent

업데이트:

* **성공적으로 seal된 cast만** 반영
* family별로 따로 저장

이 프로필 덕분에 같은 플레이어라도 barrier는 천천히, beam은 빠르게 그리는 차이가 구조적으로 들어갑니다.

### (c) Long-term Soulprint

장기적인 작법 정체성입니다.

[
\psi_u
]

담는 것:

* containment 선호
* release 선호
* recursion 선호
* relay 선호
* null/body-channel 성향
* school resonance 경향

용도:

* 작은 runtime trade-off
* codex lineage
* forge 추천
* 3D school profile과의 결합

업데이트:

* 매우 천천히
* 반복된 성공 cast, 반복 설계 패턴, codex 사용 이력 기반
* 실패 cast는 반영하지 않음

이 셋을 분리해야 “오늘 손이 미끄러운 것”과 “원래 이 사람은 containment artisan인 것”을 혼동하지 않습니다.

## 6. 현재 컴파일 구조에 넣는 구체 위치

지금 구조에 그대로 끼워 넣으면 다음이 가장 자연스럽습니다.

### 6-1. 입력 특징 추출

[
f_{\text{style}}(S)=
[\bar v,\sigma_v,h_{\text{pause}},r_{\text{cw}},s_{\text{slant}},o_{\text{order}},o_{\text{over}},b_{\text{sym}},b_{\text{closure}}]
]

직관적으로는

* 평균 속도
* 속도 분산
* 망설임 빈도
* 시계/반시계 편향
* 기울기
* 획순 일관성
* overtrace 비율
* 대칭 성향
* 폐합 성향

입니다.

### 6-2. 개인화 파서

[
z^*=\arg\max_z P(z \mid S, x_u^{\text{sess}}, x_{u,f}^{\text{fam}})
]

하지만

[
SpellID = \mathrm{Hash}(\mathrm{Canonical}(z^*))
]

입니다.

즉, 개인화는 **파싱을 돕지만**, canonical spell 자체는 공통 규칙으로 고정됩니다.
이게 현재 전체 구조와 가장 잘 맞습니다.

### 6-3. 품질 분리

[
q_{\text{abs}} = h_{\text{abs}}(S), \qquad
q_{\text{comfort}} = h_{\text{rel}}(f_{\text{style}}(S)-\mu_{u,f})
]

* (q_{\text{abs}}): 구조적 정확도, topology, closure, constraint
* (q_{\text{comfort}}): 이 플레이어의 안정된 작법과 얼마나 잘 맞는가

최종 품질은 완전 상대평가가 아니라 혼합으로:

[
q_{\text{final}}
================

(1-\alpha)q_{\text{abs}} + \alpha q_{\text{comfort}}
]

### 6-4. 실행 accent

[
a = g_f(z^*, q_{\text{abs}}, e) + \epsilon_f,T_f(\psi_u, q_{\text{comfort}}, e)
]

* (g_f): family 공통 base effect
* (T_f): 개인 성향에 따른 작은 trade-off 변형
* (\epsilon_f): 개인화 강도

권장값은

* 경쟁성 강한 모드: (\epsilon_f \le 0.05)
* PvE / sandbox / forge 실험 중심: (\epsilon_f \le 0.10)

정도가 안전합니다.

## 7. 속도·습관·개인 편향은 family별로 이렇게 읽히는 것이 좋습니다

이건 설계 제안입니다.

* **바람 / 물**: 빠름 자체보다 **연속성과 sweep regularity**가 중요
* **불꽃 / 전기**: **burst acceleration**과 decisive tempo가 유리, 대신 (\tau) 리스크 증가
* **땅 / 얼음 / 신성**: **steady tempo**, symmetry, closure discipline이 중요
* **식물 / 영혼 / 사념**: **리듬성 있는 pulse**, 재진입 위치 일관성, phase coherence가 중요
* **無 / 武**: 외부 field shaping보다 **axis discipline**, 접촉축 일관성, 과부하 관리가 중요

즉, 같은 “빠른 손”도 family에 따라 다르게 읽혀야 합니다.
빠른 손이 모든 family에서 무조건 이득이면 메타가 무너집니다.

## 8. 맵 환경은 개인차를 증폭할 수는 있어도, 개인차를 대체하면 안 됩니다

초기 아이디어에서 맵 환경은 중요한 축이었고, 그건 맞습니다. 다만 순서가 중요합니다.

**shape → canonical spell → quality/accent → environment coupling**

의 순서가 맞습니다.
환경은 canonical spell을 다른 spell로 바꾸지 않고, **같은 spell이 어느 채널로 더 잘 결합되는가**를 바꿔야 합니다.

예를 들면:

* 습윤/전도성 높은 맵: fast electric/fire release가 강하게 coupling
* 구조 안정도 높은 석실: earth/ice/sacred containment가 유리
* 생기 밀도 높은 지형: life/growth loop가 잘 붙음
* nightmare resonance가 높은 지형: (\chi,\tau)가 증폭되어 aggressive bias가 잘 살아남지만 backlash도 큼

여기서 개인차는 “이 환경에서 누가 더 잘 맞느냐” 정도로만 들어가야 합니다.
환경이 personal bias를 완전히 덮어쓰면 손맛이 사라지고, 반대로 personal bias가 환경을 무시하면 세계가 죽습니다.

## 9. 실제 UX에서 가장 중요한 규칙

가장 중요한 규칙은 네 가지입니다.

첫째, **개인화는 인식에는 크게, 전투 파워에는 작게** 들어가야 합니다.
이게 핵심입니다.

둘째, **나쁜 습관이 최적화되면 안 됩니다.**
그래서 absolute quality를 항상 큰 비중으로 유지해야 합니다.

셋째, **개인화는 성공한 seal 중심으로 배워야 합니다.**
패닉 드로잉, 미완성 낙서, 실패 streak를 그대로 장기 프로필에 넣으면 프로필이 오염됩니다.

넷째, **플레이어에게는 “내가 어떤 성향으로 읽히는지”가 설명 가능해야 합니다.**
예를 들면:

* “당신은 fire/electric 계열에서 burst tempo가 안정권입니다.”
* “당신의 barrier 계열은 closure는 안정적이지만 aperture discipline이 약합니다.”
* “당신의 작법은 containment보다 relay 쪽 soulprint가 강합니다.”

이런 식의 피드백이 있어야 장인정신이 생깁니다.
개인화가 내부에서만 일어나면, 유저 입장에서는 그저 “게임이 왠지 나를 다르게 판정한다”로 느껴집니다.

## 10. 현재 구조에서 금지해야 할 적용

이건 분명히 금지하는 편이 좋습니다.

* **같은 canonical shape인데 유저별로 다른 spell family가 되는 것**
* **전역 slant/손기울기를 semantic angle로 읽는 것**
* **상대평가만 써서 느린/서툰 습관도 그대로 최적화되는 것**
* **개인화 프로필을 너무 빨리 업데이트해서 하루 컨디션이 정체성이 되는 것**
* **`無`/`武`에 개인 편향을 크게 얹어 체현형이 만능 메타가 되는 것**
* **graph composition 취향을 live damage bonus로 직접 환산하는 것**

특히 마지막은 위험합니다.
graph composition 취향은 **codex, preset, forge 추천, lineage** 로 표현하는 것이 맞고, 즉시 전투 화력으로 바꾸는 것은 위험합니다.

## 최종 정리

현재 구조에서 유저별 속도, 습관, 개인적 편향은 다음처럼 적용되는 것이 가장 타당합니다.

* **모양**은 canonical spell을 정한다.
* **속도/각도/품질/정확도**는 같은 spell family 안에서 execution accent를 정한다.
* **유저 습관**은 parser와 feedback를 강하게 개인화한다.
* **개인적 편향**은 long-term soulprint로 축적되어 작은 trade-off와 codex 정체성을 만든다.
* **환경**은 이 모든 것을 공통 물리계층에서 증폭/감쇠한다.

한 줄로 줄이면,

**유저별 개인차는 “같은 주문을 어떻게 더 잘, 더 위험하게, 더 자기답게 실행하느냐”를 바꿔야지, “같은 그림이 무슨 주문이냐”를 바꾸면 안 됩니다.**

현재 당신이 만든 구조는 이 원칙을 넣기에 꽤 잘 맞습니다.
정확히는 **semantic identity에는 0, parser에는 강, quality에는 혼합, runtime power에는 약, codex/forge에는 강**으로 넣는 것이 가장 안전하고도 창발적입니다.

[1]: https://faculty.washington.edu/wobbrock/pubs/gi-13.02.pdf "https://faculty.washington.edu/wobbrock/pubs/gi-13.02.pdf"
[2]: https://arxiv.org/html/2402.15307v1 "https://arxiv.org/html/2402.15307v1"
[3]: https://cs.nyu.edu/media/publications/oh_jong.pdf "https://cs.nyu.edu/media/publications/oh_jong.pdf"
[4]: https://arxiv.org/abs/2602.01533 "https://arxiv.org/abs/2602.01533"
[5]: https://www.microsoft.com/en-us/research/wp-content/uploads/2006/10/szummerbishop-iwfhr06.pdf "https://www.microsoft.com/en-us/research/wp-content/uploads/2006/10/szummerbishop-iwfhr06.pdf"

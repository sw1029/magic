request


해당 목표를 위한 HCI 관점의 인공언어 체계 신규 창조 관점에서, 고안하여야 할 방향성 및 요소, 구현방향 등의 판단 요소를 언어학적 관점을 병렬 판단하여 제시.
2D - 3D 관점에서 모두 의미적으로 기능하여야 하며, 설계자의 언어적 세계관 및 편향 관점에서 해당 요소를 최대한 감안하되 난이도가 낮은 직관적인 언어체계여야 함. 2d 관점의 인식을 우선시하며, 3d 관점 추가 언어적 장치는 가짓수를 한정하여 2d 언어 표현의 확장 표현으로 기능 가능하도록 우선 고려되어야 함.
언어의 선형적 인식은 최대한 배제하고, 기하학/의미론적 구성 및 창발적 의미 조합이 가능한 형태를 추구함.
필요시 프렉탈 구조와 같은 추가 기하학적 요소 를 배치 가능함(문법-연관요소 관점).
가능하다면 오류 정정코드와 같이 해석 강건성 관점의 고려 역시 진행되었으면 함.
해당 인공언어가 실제 언어체계로 기능 가능할 정도는 아니더라도, 마법진을 보는 유저가 해당 의미를 직관적으로 해석이 가능하여야 함. 표의/표음 관련하여 각각에 대해 어떤 방식이 게임 관점에서의 HCI를 병행 고려하였을 때 타당한지도 검토. 지나치게 회로도와 같은 형식은 지양하며, 가능한 UX 관점에서 인문학적 접근을 통해 유저가 마법을 직접 설계한다는 인상을 주어야함.


---


answer


제 판단은 분명합니다. 이 목표에 가장 잘 맞는 것은 **“비선형 표의 중심의 세마시오그래픽(semasiographic) 마법 언어”**입니다. 즉, 말소리를 적는 문자 체계가 아니라, **의미를 직접 도형·배치·관계로 표현하는 시각 언어**가 맞습니다. Sampson은 이런 체계를 glottographic writing과 구분해 semasiographic system으로 설명하고, 제한된 도메인에서는 말과 독립적으로 의미를 전달하는 체계가 성립할 수 있다고 봅니다. 동시에 Talmy와 DISL 계열 연구는 인간이 공간을 이해할 때 아주 큰 개수의 규칙이 아니라, **작은 수의 공간 schema와 그 조합**으로 의미를 구조화한다는 점을 보여줍니다. 당신이 원하는 “선형 읽기 최소화”, “기하학 기반 의미”, “직관적 해석”, “2D 우선 + 3D 확장”은 이 방향과 거의 정확히 맞물립니다. ([Geoffrey Sampson][1])

다만 중요한 단서가 있습니다. 이 언어는 **실제 언어처럼 모든 것을 표현하는 완전한 writing system**을 목표로 삼으면 안 됩니다. 그렇게 가면 기호 수가 급격히 늘고 학습 난도가 치솟습니다. 대신 **마법이라는 좁은 의미 영역에 특화된 “공간-의미 도형문법”**으로 설계해야 합니다. 말하자면 “말을 적는 문자”가 아니라 “행위를 설계하는 의식적 도면”에 더 가깝게 만드는 것이 맞습니다. 이것이 게임 HCI에서도 훨씬 강합니다. ([Geoffrey Sampson][1])

1. 언어의 본체는 “표의 + 공간문법”이어야 합니다.

HCI 관점에서는, 유저가 처음 봤을 때 어느 정도 의미를 짐작할 수 있어야 합니다. Moody 계열 연구는 semantic transparency를 “초심자가 외형만 보고 의미를 추론할 수 있는 정도”로 정의하고, 이것이 novice 이해도에 큰 영향을 준다고 봅니다. 실제로 novice가 만든 기호가 전문가가 만든 기호보다 더 semantically transparent한 경우가 많았고, symbolization/blind interpretation 절차를 거치면 이해도와 인식 정확도가 크게 올라갔습니다. 반면 의미를 너무 추상적으로 압축하면 초심자 입장에서 “멋있지만 아무 뜻도 모르겠는 문양”이 됩니다. ([homepages.uc.edu][2])

언어학 관점에서는, 핵심은 **open-class와 closed-class를 분리하는 것**입니다. Talmy 식으로 말하면 open-class는 세계관적 내용, closed-class는 개념 구조를 만듭니다. 당신의 마법 언어도 마찬가지여야 합니다. 즉, “불·얼음·기억·피·정화” 같은 세계관적 내용은 open-class 층에 두고, “안에 가둔다 / 밖으로 보낸다 / 연결한다 / 차단한다 / 분기한다 / 공명시킨다” 같은 구조는 closed-class 문법으로 두는 편이 맞습니다. 이 구조가 있어야 **세계관 편향**을 살리면서도 **직관성**을 잃지 않습니다. ([acsu.buffalo.edu][3])

그래서 권장하는 기본 모델은 이렇습니다.
**핵심 의미는 small closed inventory의 공간 schema**,
**세계관적 개성은 motif family와 mandatory distinction**,
**개인 차이는 필체와 학교별 allographic style**로 분리합니다.

2. 설계자의 세계관과 편향은 “장식”이 아니라 “무엇을 반드시 표시하게 할 것인가”에 들어가야 합니다.

이 부분이 가장 중요합니다. 세계관은 문양 스타일로만 넣으면 얕아집니다. 진짜 세계관은 **문법화된 대비축**에 들어가야 합니다. Talmy가 말한 closed-class 구조처럼, 언어가 어떤 구분을 강제하는지가 그 문명의 사고방식을 드러냅니다. 예를 들어 어떤 문명이 “오염/정화”를 핵심 가치로 본다면, 이 언어는 경계의 투과성, 내부/외부, 봉인/누출을 강하게 문법화해야 합니다. “계약/대가”가 중심이면 witness mark, paired anchor, cost slot 같은 문법이 생겨야 합니다. “순환/균형”이 중심이면 대칭, 공전, 상쇄, 환류가 핵심 문법이 됩니다. ([acsu.buffalo.edu][3])

HCI 관점에서는, 세계관 편향을 너무 깊게 넣으면 초심자가 못 읽습니다. Moody의 결과처럼 semantic transparency는 문화권과 배경지식의 영향을 받습니다. 또 emoji 연구처럼, 처음엔 직관적으로 보이던 기호도 커뮤니티 사용을 거치면 의미가 drift할 수 있습니다. 따라서 **핵심 문법은 embodied spatial schema에서**, **세계관 편향은 그 위에 얹는 층에서** 넣는 것이 가장 안전합니다. 쉽게 말해, “안에 가둔다”, “밖으로 뿜는다”, “둘을 묶는다” 같은 핵심은 인간 보편의 공간 직관에 기대고, 그 위에 “정화의 결속인지 / 피의 계약인지 / 기억의 봉인인지”를 세계관 layer가 정하게 해야 합니다. ([homepages.uc.edu][2])

실무적으로는 먼저 세계관 축을 3~5개만 고르는 것이 좋습니다. 예를 들면 다음 정도가 적당합니다.

* 내부 / 외부
* 봉인 / 방출
* 균형 / 왜곡
* 순환 / 파열
* 정화 / 오염

이 축들이 이후의 primitive 설계, modifier 설계, 3D 확장 의미, 심지어 UI 색채와 사운드까지 관통해야 합니다.

3. 2D 핵심 언어는 “작은 수의 image schema primitive”로 시작해야 합니다.

여기서는 고수준 의미를 primitive로 두면 안 됩니다. Wobbrock의 gesture elicitation 연구에서 개념 복잡도가 올라갈수록 사람들 사이의 제스처 합의도는 유의미하게 떨어졌습니다. OctoPocus 쪽에서도 16개 수준의 gesture-command set이 단기 기억 한계를 넘는 어려운 학습 과제로 쓰였습니다. 이 말은 곧, **“심판”, “복수”, “시간정지” 같은 높은 수준의 추상 개념을 하나의 기본 기호로 만들지 말라”**는 뜻입니다. 그런 것은 primitive 조합으로 나오게 해야 합니다. ([faculty.washington.edu][4])

가장 타당한 2D core primitive는 8~12개 정도입니다. 저는 다음 10개 정도를 권합니다.

* **핵(nucleus)**: 점 또는 소핵. 의미는 source, focus, origin.
* **경계(boundary/container)**: 닫힌 원환. 의미는 containment, stabilization, ward.
* **개구(aperture)**: 열린 틈이 있는 경계. 의미는 release, admission, leak.
* **경로(path/ray)**: 방향성을 가진 선·호. 의미는 transfer, propulsion, beam.
* **결속(link/tether)**: 두 핵 또는 두 경계를 잇는 연결. 의미는 binding, channeling.
* **차단(block/cross)**: 경로를 가로막는 bar. 의미는 oppose, sever, cancel.
* **축(axis/mirror)**: 대칭축 또는 거울축. 의미는 balance, duality, reflection.
* **분기(branch)**: 갈라짐. 의미는 propagation, multiplicity, diffusion.
* **반복(echo/concentric repeat)**: 반복된 동심 또는 반향 패턴. 의미는 amplification, resonance.
* **자기유사(fractal echo)**: 축소 반복. 의미는 recursion, self-propagation, chained reapplication.

이 목록은 Talmy의 spatial element 관점과 DISL의 independent / relational / attributive primitive 구분과 잘 맞습니다. 중요한 점은 이들이 “마법 이름”이 아니라 **마법 의미를 만드는 구조 재료**라는 것입니다. ([acsu.buffalo.edu][3])

이 2D 언어의 읽기 방식은 좌→우가 아니라 **중심→경계→방사 구조→외부 관계→미세 modifier**여야 합니다. 완전 무질서한 비선형은 읽기 불가능하므로, “선형성 배제”는 “읽기 순서 부정”이 아니라 **“공간적 스캔 순서로 대체”**로 이해하는 것이 맞습니다. 예를 들면 중심은 “무엇이 작동하는가”, 첫 경계는 “무엇을 감싸거나 허용하는가”, 방사 구조는 “어떻게 작용하는가”, 외부 연결은 “무엇과 상호작용하는가”, 미세 표지는 “어떤 상태로 작동하는가”를 담당하게 하는 방식입니다. 이러면 비선형이면서도 해석 가능해집니다.

이 구조에서 고수준 의미는 primitive 조합으로 나옵니다. 예를 들어 beam은 `핵 + 경로`, barrier는 `핵 + 닫힌 경계 + 축`, chain effect는 `핵 + 경로 + 분기`, prison은 `경계 + 차단 + 이중경계`, resonance field는 `핵 + 동심반복 + 대칭` 같은 식입니다. 이렇게 하면 유저는 “새로운 단어를 외운다”기보다 “새로운 조합을 설계한다”는 느낌을 받습니다.

4. 3D는 독립 언어가 아니라 “2D 언어의 형태소적 확장”이어야 합니다.

이 부분은 강하게 제한해야 합니다. VR/3D sketch 연구는 일관되게, 3차원 직접 드로잉이 2D보다 훨씬 어렵고 부정확하며, plane guidance, snapping, beautification이 있어야 쓸 만해진다고 보고합니다. ILoveSketch도 2D에서 3D로의 “judicious leap”를 강조했고, Multiplanes는 snapping plane과 trigger point, beautification을 자동으로 제공했습니다. 즉, 3D를 자유 언어로 풀어주면 언어가 아니라 motor-skill tax가 됩니다. ([Dynamic Graphics Project][5])

그래서 3D는 **소수의 닫힌 operator 집합**이어야 합니다. 저는 4~5개면 충분하다고 봅니다.

* **적층(stack)**: 위/아래 레이어 관계. 의미는 우선순위, 인과 계층, 지속성.
* **압출(extrusion/thickness)**: 깊이 두께. 의미는 용량, 지속 시간, 구속력, chamber화.
* **공전(revolution/orbit)**: 회전 적층. 의미는 순환 유지, 와류, 지속 갱신.
* **기울임(tilt/orientation bias)**: 경사나 면 지향. 의미는 방향성 바이어스, 특정 평면 목표화.
* **교량(bridge/interlock)**: 레이어 간 결합. 의미는 층간 전달, 변환, relay.

핵심은 **3D가 새로운 어휘를 만들지 않고, 2D 어휘의 aspect를 바꾸게 하는 것**입니다. 예를 들어 2D barrier를 압출하면 prison/chamber가 되고, 2D beam을 공전시키면 vortex-like maintained beam이 되며, 2D summon을 적층하면 sustained manifestation이 됩니다. 즉 3D는 새 단어가 아니라 **문법적 확장**입니다.

구현상으로도 이것이 중요합니다. 모든 3D 마법은 반드시
`canonical 2D core + small 3D operator list`
로 환원되어야 합니다. 그래야 2D 인식 우선 원칙과 재현 가능성을 동시에 지킬 수 있습니다. CAD 계열도 2D sketch primitives와 constraints를 기반으로 3D operations(extrusion, revolution 등)로 올라갑니다. 당신 시스템도 이 모델이 가장 안정적입니다. ([arXiv][6])

5. 표의/표음은 “표의 중심, 표음은 비핵심”이 맞습니다.

언어학적으로 보면, glottographic/phonographic 체계는 결국 spoken unit를 기록하는 방향으로 갑니다. 그러면 읽기 순서와 선형성의 압력이 생깁니다. Sampson도 logographic과 phonographic을 구분하고, phonographic 체계는 음성 단위와의 대응이 핵심이라고 설명합니다. 그런데 당신의 목표는 **비선형 공간 조합**입니다. 그래서 표음을 핵심 축으로 두는 순간 설계 철학이 흔들립니다. ([Geoffrey Sampson][1])

게임 HCI에서도 마찬가지입니다. 표의 기호는 처음 보는 spectator가 “대충 무슨 계열인지”를 짐작하기 쉽습니다. 반대로 표음 기호는 일단 배워야 하고, 대개 선형 순서를 타게 됩니다. IKON 연구에서도 단일 pictogram의 평균 이해율은 높았지만, 여러 기호를 이어 iconic sentence 수준으로 가면 이해율이 크게 떨어졌습니다. 또 emoji 연구는 “보기만 하면 바로 이해되는 것처럼 보이는 기호”조차 사용과 맥락에 따라 의미가 drift한다는 점을 보여줍니다. 즉, 표의는 직관성이 있지만 drift 위험이 있고, 표음은 조합력은 좋지만 직관성이 약합니다. ([ACL Anthology][7])

그래서 제 권고는 이렇습니다.

* **전투/설계용 핵심 언어는 표의적이어야 합니다.**
* **문법 표지는 소수의 비선형 부호로 둡니다.**
* **표음 요소가 필요하면, 코어 문법이 아니라 lore·chant·preset 이름·학교명 같은 주변부에 둡니다.**

조금 더 공격적으로 말하면, 당신 시스템에서 “표음”은 visible grammar보다 **운율(prosody)이나 ritual naming** 쪽이 더 어울립니다. 예를 들어 stroke cadence, 반복 횟수, 시전 방향 같은 것을 아주 제한적으로만 “준음운적 부호”처럼 쓸 수는 있습니다. 그러나 이것도 많아지면 recognizer와 사용자가 다른 것을 보게 됩니다. OctoPocus가 보여주듯, 사용자는 shape를 보는데 recognizer는 direction을 보는 순간 혼란이 생깁니다. 그래서 동적 요소를 쓴다면 clockwise/counterclockwise, single/double pass처럼 소수만 허용해야 합니다. ([lri.fr][8])

요약하면, **표의 80~90% + 비표음적 문법/운율 표지 10~20%**가 가장 타당합니다. 진짜 phonography는 코어에서 빼는 편이 좋습니다.

6. 프렉탈은 “어휘”가 아니라 “문법적 modifier”로 쓰는 것이 좋습니다.

프렉탈이나 자기유사는 매우 매력적이지만, 기본 어휘로 쓰면 난도가 급격히 올라갑니다. 기호/아이콘 연구에서도 complexity는 central usability dimension입니다. 따라서 fractal depth가 높아질수록 readability와 recognizability가 급격히 떨어질 가능성이 큽니다. ([eprints.bournemouth.ac.uk][9])

그래서 프렉탈은 다음 같은 의미에만 제한적으로 쓰는 것이 좋습니다.

* 재귀적 재시전
* 자기증폭
* 확산/전염
* 기억/잔향
* 다중 대상 반복 적용

즉, `self-similar repeat`가 보이면 “이 주문이 자기 자신을 다시 적용한다”는 식으로 읽히게 만드는 것이 좋습니다. 다만 실전에서는 depth 1~2, forge/preset에서만 더 깊은 depth를 허용하는 식이 안전합니다.

7. 해석 강건성은 오류정정코드처럼 설계하는 것이 맞습니다.

이 부분은 아주 좋은 방향입니다. 가장 중요한 원칙은 **“작은 오류가 다른 마법으로 바뀌지 않게 하는 것”**입니다. Schwarz의 uncertainty 처리 프레임워크는 uncertain input을 너무 빨리 single interpretation으로 collapse하지 말고, 여러 가능성을 유지한 채 나중에 더 많은 정보를 보고 결정하라고 제안합니다. sketch UI 연구도 사용자가 예측 가능한 오류와 낮은 변형의 피드백을 선호한다고 보고했습니다. 따라서 이 시스템도 **바로 확정하지 말고, seal/ignite 전까지 ambiguity를 유지**하는 편이 맞습니다. ([Microsoft][10])

구조적으로는 valid spell을 “codeword”처럼 보면 됩니다.
유저가 그린 추정 구조를 `Ĉ`, 유효한 canonical 구조 집합을 `V`라 하면,

`C* = argmin_{C ∈ V} d_struct(Ĉ, C)`

로 가장 가까운 구조를 찾되,

* `d_struct <= r_correct` 이면 보정
* `r_correct < d_struct <= r_detect` 이면 **불안정한 미완성 주문**
* `d_struct > r_detect` 이면 실패

로 처리하는 것이 좋습니다. 핵심은 **detectable failure > wrong success**입니다.

이를 위해 각 spell family는 최소한 3개 이상의 독립 invariant를 가져야 합니다. 예를 들면:

* topology class: 닫힌 외곽인가, 개구가 있는가
* symmetry class: 축대칭 / 회전대칭 / 비대칭
* anchor parity: 짝수/홀수 anchor 수
* family echo: 중심과 외곽에 반복되는 동일 family 표지
* relation signature: allowed containment/tangency/intersection pattern

이렇게 하면 한 획 삐끗한 것이 곧바로 다른 family로 점프하지 않습니다. 더 나아가 **parity ring**이나 **echo marker**를 outer rim에 심어 두면 aesthetically integrated checksum처럼 기능할 수 있습니다. 중요한 것은 QR코드처럼 보이지 않게, 세계관 안에서 자연스러운 micro-ornament로 흡수하는 것입니다.

UI도 여기에 맞춰야 합니다. 시스템은 “빔으로 오인식했습니다”가 아니라
“외곽 폐합 불일치”, “반대축 위상 불일치”, “anchor parity 오류”
처럼 **syndrome를 해설**해야 합니다. 그래야 유저가 “게임이 멋대로 읽었다”가 아니라 “내 구조가 어디서 어긋났는지”를 이해합니다.

8. 인터페이스는 “해석을 보여주되, 손맛을 지우지 않는 방향”이어야 합니다.

사용자는 마법을 설계하고 있다는 인상을 받아야 합니다. 따라서 backend는 canonical IR로 정리하더라도, front-end는 raw hand-drawn texture를 어느 정도 보존해야 합니다. sketch front-end 연구는 사용자가 recognition feedback이 자신의 스케치를 크게 변형하거나 clutter를 늘리는 것을 싫어한다고 보고했고, OctoPocus류는 on-screen feedforward/feedback이 학습과 수행을 돕는다고 보여줍니다. 따라서 가장 좋은 방식은 **원본 선을 대체하지 않는 ghost overlay**입니다. 예를 들어 closure gap, symmetry ghost, candidate family aura, instability glow, possible 3D lift preview를 겹쳐 보여주되, 원본 stroke는 그대로 남기는 방식입니다. ([Academia][11])

또한 모든 것을 언어로 해결하려 들면 안 됩니다. Wobbrock의 gesture elicitation 연구는 어떤 referent는 사용자 합의가 매우 낮아서 gesture보다 widget이 낫다는 점을 분명히 보여줍니다. 따라서 preset save/load, memory slot 호출, undo, camera reset, layer duplicate, 3D inspect 같은 **meta operation은 언어 밖 UI**로 빼는 것이 맞습니다. 언어는 magic semantics를 담당하고, UI는 authoring workflow를 담당해야 합니다. ([faculty.washington.edu][4])

9. 실제 구현 방향은 “primitive graph + commit-based parse + participatory symbol design”이 가장 좋습니다.

인식 엔진은 전체 그림을 end-to-end로 하나의 class로 찍지 말고, CAD와 sketch recognition에서 하듯 **primitive와 constraint graph**로 가는 편이 낫습니다. SketchGraphs는 real CAD sketch를 geometric constraint graph로 표현했고, PICASSO도 hand-drawn sketch를 parametric primitive로 복원하는 방향을 택합니다. 당신의 언어도 똑같이
`digital ink → primitive fitting → relation graph → canonical semantic graph → 3D operator parse → spell IR`
로 가는 것이 맞습니다. ([arXiv][6])

그리고 symbol set은 책상 위에서 혼자 만들면 안 됩니다. 가장 좋은 루프는 이렇습니다.

* 설계자가 의미 후보를 정의한다.
* 초심자에게 의미만 보여주고 기호를 그리게 하거나 고르게 한다.
* blind interpretation으로 어떤 기호가 실제로 읽히는지 본다.
* polysemy가 큰 기호를 버린다.
* rationale을 붙여 다시 테스트한다.

이 접근은 Wobbrock의 guessability 절차, Moody의 symbolization/blind interpretation 절차, IKON의 Iconometer-like adequacy 평가와 잘 맞습니다. 특히 기호 생성 단계에서는 가능한 한 기존 예시를 먼저 보여주지 않는 편이 좋습니다. 그래야 designer bias가 그대로 복제되지 않고, 실제 naive intuition이 올라옵니다. ([faculty.washington.edu][12])

최종 권장 청사진을 한 줄로 요약하면 이렇습니다.

**“세계관이 문법화된, image-schema 기반의 2D 세마시오그래픽 코어 언어를 만들고, 3D는 4~5개의 형태소적 operator로만 확장하며, 구조적 redundancy와 commit-based parse로 오류를 흡수하는 authoring-oriented 마법 언어”**

이 방향이면 당신이 원하는 조건들, 즉
비선형성, 기하학적 의미 조합, 2D 우선성, 3D 확장 가능성, 세계관 반영, 직관성, 오류 강건성, 그리고 “유저가 직접 마법을 설계한다”는 인문학적 감각을 동시에 살릴 수 있습니다.

[1]: https://www.grsampson.net/AWsm.pdf "https://www.grsampson.net/AWsm.pdf"
[2]: https://homepages.uc.edu/~niunn/courses/RE-refs/Prosumers-RE13.pdf "https://homepages.uc.edu/~niunn/courses/RE-refs/Prosumers-RE13.pdf"
[3]: https://www.acsu.buffalo.edu/~talmy/talmyweb/Recent/hampevi.pdf "https://www.acsu.buffalo.edu/~talmy/talmyweb/Recent/hampevi.pdf"
[4]: https://faculty.washington.edu/wobbrock/pubs/chi-09.02.pdf "https://faculty.washington.edu/wobbrock/pubs/chi-09.02.pdf"
[5]: https://www.dgp.toronto.edu/~shbae/pdfs/Bae_Bala_Sing_2008_ILoveSketch.pdf "https://www.dgp.toronto.edu/~shbae/pdfs/Bae_Bala_Sing_2008_ILoveSketch.pdf"
[6]: https://arxiv.org/abs/2007.08506 "https://arxiv.org/abs/2007.08506"
[7]: https://aclanthology.org/2022.emoji-1.2.pdf "https://aclanthology.org/2022.emoji-1.2.pdf"
[8]: https://www.lri.fr/~mbl/Stanford/CS477/papers/Octopocus-UIST2008.pdf "https://www.lri.fr/~mbl/Stanford/CS477/papers/Octopocus-UIST2008.pdf"
[9]: https://eprints.bournemouth.ac.uk/10165/ "https://eprints.bournemouth.ac.uk/10165/"
[10]: https://www.microsoft.com/en-us/research/wp-content/uploads/2016/10/SchwarzUIST2010.pdf "https://www.microsoft.com/en-us/research/wp-content/uploads/2016/10/SchwarzUIST2010.pdf"
[11]: https://www.academia.edu/28448268/Designing_a_sketch_recognition_front_end "https://www.academia.edu/28448268/Designing_a_sketch_recognition_front_end"
[12]: https://faculty.washington.edu/wobbrock/pubs/chi-05.pdf "https://faculty.washington.edu/wobbrock/pubs/chi-05.pdf"

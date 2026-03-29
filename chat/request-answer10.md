request



보류항목 작성해야 하는 심볼 실루엣/학파별 VFX palette 정리. 권장/제안안을 유사 레퍼런스를 참고하여 제시. 필요시 시각화 병행



---



answer



최종 권장안은 **“알케미 표기법의 압축된 원소어휘 + 고딕 rose window의 방사형 층위 + LADDER/PaleoSketch의 구조적 primitive 사고 + 현대 게임 VFX 가독성 규칙”**을 합성하는 방향입니다. 알케미 도상은 원소와 작업을 체계적인 시각 어휘로 기록했고, rose window 연구는 방사형 기하와 단계적 설계 과정을 procedural model로 다룰 수 있음을 보여줍니다. LADDER와 PaleoSketch는 적은 primitive로 구조를 기술하고, early beautification/feedback을 주는 방식이 유효함을 보여줍니다. 또 게임 VFX 연구는 효과가 underlying mechanics와 meaningful event를 읽게 해야 한다고 보고하며, 배경의 시각 복잡도는 플레이어 성능을 실제로 떨어뜨릴 수 있습니다. ([게티][1])

실루엣 설계의 기준은 **shape first, interior second, color third**로 두는 편이 맞습니다. 시각 기호 연구에서는 semantic transparency, 즉 “처음 보는 사람이 외형만 보고 뜻을 어느 정도 짐작할 수 있는가”가 매우 중요하고, visual variables 연구와 cartography 쪽 기호 설계는 shape, orientation, hue/value, outline, figure–ground contrast가 이해도와 검색성에 큰 영향을 준다고 봅니다. 특히 closed outline은 figure–ground를 안정화하고, frame과 pictogram을 분리하면 상위 범주와 하위 의미를 동시에 읽기 쉬워집니다. 따라서 현재 체계의 `center chamber / aspect ring / process ring / covenant rim` 구조는 그대로 유지하되, **root는 외곽 contour**, **파생은 내부 perturbation**, **학파는 2D에서 실루엣이 아니라 광휘/질감과 미세 accent**로 들어가는 편이 가장 안정적입니다. ([UC 홈페이즈][2])

직접 만든 개념 보드는 여기서 볼 수 있습니다.
[Magic circle silhouette + VFX board (PNG)](sandbox:/mnt/data/magic_language_silhouette_vfx_board.png)

## 1. 심볼 실루엣 권장안

가장 먼저 고정할 규칙은 다섯 가지입니다.

첫째, **root는 24–48px 흑백 축소에서도 구분되는 macro silhouette**를 가져야 합니다.
둘째, **파생은 부모 실루엣을 보존한 채 한 개의 진단적 변형만 추가**해야 합니다.
셋째, **negative space는 root당 1~2개까지만** 허용하는 편이 좋습니다.
넷째, **detached subform은 최대 1개**만 허용해야 합니다. detached 요소가 많아지면 영혼/사념/곁잠 계열이 전부 비슷해집니다.
다섯째, **정확한 의미가 아닌 “어떤 계열인가”를 먼저 읽히게** 해야 합니다. semantic transparency 연구의 관점에서도 초심자는 완전한 문법 해독보다, 먼저 category inference에 성공해야 학습이 붙습니다. ([UC 홈페이즈][2])

### Root Seed 실루엣

| 계열         | 권장 macro silhouette                           | 내부 진단 요소                           | 피해야 할 것                        |
| ---------- | --------------------------------------------- | ---------------------------------- | ------------------------------ |
| **바람**     | **열린 sweep/fan**. 닫히지 않은 C형 또는 부채형 개방 실루엣     | **편심 nucleus** + 같은 방향의 2~3개 sweep | 완전 폐합, 정대칭, 물처럼 평행 파문만 남는 형태   |
| **땅**      | **낮고 무거운 폐합 질량**. squat ring 또는 하중이 아래로 걸린 원환 | **weighted base** + 하부 brace       | 위로 뾰족한 crown, 지나치게 가는 선, 개구 우세 |
| **불꽃**     | **상향 crown / flame-diamond**. 위로 밀어올리는 수직 실루엣 | 내부 flame tongue, apex vent         | 완전 원형, 수평 위주, 차갑고 정적인 대칭       |
| **물**      | **평행 파문/유동 호**. 좌우로 길고 부드러운 층상 실루엣            | 작은 spill notch 또는 유출 방향            | 생명처럼 가지치기, 불꽃처럼 위로 찢어지는 crown  |
| **식물(생명)** | **rooted Y / 발아 실루엣**. 아래 뿌리, 위 분기            | 1단계 미세 자기유사 분기                     | 신성처럼 과도한 완전대칭, 물처럼 병렬 곡선       |

여기서 가장 중요한 것은 **역사적 알케미 기호를 “직접 복제하지 않는 것”**입니다. 알케미 기호의 장점은 “낮은 스트로크 수로 계열을 바로 읽히게 만든다”는 점이지, 삼각형+가로선 자체를 가져오는 데 있지 않습니다. 즉, **참조할 것은 원리이고 외형은 새로 만들어야** 합니다. Getty의 알케미 자료를 보면 실제로 원소와 작업이 간결한 시각 어휘로 체계화되어 있다는 점이 강점입니다. 당신 시스템은 그 체계성을 빌리되, 현재의 radial grammar와 root/derivation 구조에 맞게 다시 설계하는 편이 맞습니다. ([게티][1])

### 파생 요소 실루엣

파생 요소는 “새 원소 추가”가 아니라 **부모 trace를 남긴 채 interior perturbation 하나를 넣는 방식**이 가장 좋습니다. 이건 semantic transparency와 structural parsing 두 쪽 모두에 유리합니다. LADDER와 PaleoSketch가 적은 primitive와 구조적 조합을 유지한 상태에서 recognition/feedback을 다루는 이유도 결국 같은 문제, 즉 “새 뜻을 만들되 전체 기하 정체성은 무너지지 않게” 하기 위해서입니다. ([Rationale][3])

권장 overlay는 아래 정도로 정리하는 편이 좋습니다.

| 파생     | 권장 overlay 논리                            |
| ------ | ---------------------------------------- |
| **독**  | downward drip scar / 누수 흉터               |
| **강철** | brace frame / 단련된 보강틀                    |
| **전기** | forked discharge / 분절된 방전 갈래             |
| **얼음** | arrest bar / 정지축 + facet 힌트              |
| **영혼** | detached halo / 부모로부터 살짝 떨어진 핵           |
| **사념** | inward echo / 안쪽으로 감기는 반향                |
| **신성** | perfect axis + halo / 정렬된 축과 정갈한 후광      |
| **無**  | hollow omission / 의도적 결손, 비워진 core       |
| **염동** | floating tether / 접지되지 않은 부유 결속          |
| **武**  | null-derived strike axis / 無에서 이어지는 체현 축 |

특히 `無`와 `武`는 예외를 분명히 두는 편이 좋습니다. `無`는 **실루엣을 더하는 파생이 아니라 일부를 “비우는” 계열**이고, `武`는 그 비움을 **전방향 strike axis**로 다시 접지하는 형태여야 합니다. 따라서 `武`는 독립 pictogram처럼 보이기보다, **null lineage를 읽을 수 있는 전방 지향 실루엣**이 낫습니다.

## 2. 실루엣 구현 규칙

실무적으로는 아래 규칙을 권합니다.

외곽선 두께를 1.0이라고 두면, 내부 진단선은 0.65~0.75, micro accent는 0.35~0.5 정도로 두는 편이 좋습니다. root는 외곽선으로 읽히고, 파생은 내부 정보로 읽히게 해야 합니다. 또 combat에서 바탕이 복잡해질수록 실루엣 정보가 먼저 무너지므로, **하나의 glyph 안에서 대칭축은 1개, 강한 파손/절단은 1개, detached form도 1개** 정도가 상한선이라고 보는 편이 안전합니다. 배경 복잡도가 증가하면 과업 성능이 유의미하게 나빠질 수 있다는 실험 결과를 감안하면, 이 상한은 미적 선택이 아니라 UX 방어선에 가깝습니다. ([ResearchGate][4])

교육형/숙련형을 나눌 때도 같은 원칙을 그대로 씁니다. **교육형은 부모 trace를 노골적으로 남기고**, **숙련형은 ligature처럼 압축하되 macro silhouette class는 바꾸지 않는 것**이 맞습니다. 예를 들어 `물 → 얼음`은 교육형에서 파문+정지축이 둘 다 보여야 하고, 숙련형에서도 최소한 “유동 기원 + arrest”가 보이지 않으면 안 됩니다.

## 3. 학파별 VFX palette

여기서는 “색표”보다 **빛·재질·모션·잔재까지 포함한 palette**로 잡는 편이 좋습니다. in-situ VFX 연구는 플레이어가 효과를 통해 게임 세계의 상호작용을 추론하고, meaningful event와 underlying mechanic를 읽는다고 봅니다. 따라서 학파 palette는 단순히 색을 바꾸는 것이 아니라, **무슨 종류의 상호작용인가를 느끼게 하는 light logic과 motion logic**을 함께 가져야 합니다. 또 background complexity 연구를 감안하면, 한 주문 인스턴스에서 동시에 주도권을 가지는 색은 많지 않은 편이 좋습니다. combat 기준으로는 **dominant hue 1개 + support hue 1개 + anomaly hue 1개** 정도가 상한선입니다. ([arXiv][5])

또 한 가지 중요한 점은, **학파는 2D에서 root hue를 덮어쓰지 말아야** 한다는 것입니다. 당신 구조에서는 2D core semantics가 공유되므로, 2D에서는 `root identity 70 : school filter 30` 정도, 3D lift에서는 `root identity 40 : school filter 60` 정도로 가중을 뒤집는 설계를 권합니다. 이렇게 해야 2D에서는 “불인지 물인지”가 먼저 읽히고, 3D에서는 “길몽식 불인지 악몽식 불인지”가 드라마틱하게 살아납니다.

### 색채 사용의 근거

색채는 hue만 보면 부족합니다. Wilms와 Oberfeld는 **밝고 채도 높은 색이 더 높은 arousal**을 만들고, hue도 arousal에 영향을 주며 **red 쪽이 blue/green보다 arousal을 더 끌어올리는 경향**이 있다고 보고했습니다. 따라서 학파 palette는 “무슨 색이냐”보다 **value, saturation, hue 조합**으로 설계해야 합니다. 이 점은 특히 악몽/길몽 구분에서 중요합니다. 길몽을 밝고 질서 있게, 악몽을 어둡고 찢어진 것으로 만들고 싶다면, hue보다 먼저 **value range와 edge noise**를 갈라야 합니다. ([staff.uni-mainz.de][6])

### 학파 palette 제안

| 학파      | 권장 색상                                             | 빛/재질                                          | 모션 어휘                                                    | 2D 적용                                 | 3D signature                                          |
| ------- | ------------------------------------------------- | --------------------------------------------- | -------------------------------------------------------- | ------------------------------------- | ----------------------------------------------------- |
| **길몽**  | `#F6F1DA` `#DDBB59` `#A8D8C0` `#9FE6F0` `#FFFDF6` | 얇은 금빛 rim, 유리/성유리(stained-glass) 같은 깨끗한 bloom | laminar pulse, ordered expansion, clean dust             | root hue 위에 얇은 고가치 halo만 추가           | rose-window형 shell, 정렬된 광기둥, 투명한 chamber              |
| **악몽**  | `#130B1A` `#6C1D5E` `#B40F3A` `#6CFF6C` `#25131D` | 압력 그림자, 찢어진 glow, 기름진 smoke                   | asynchronous pulse, edge tearing, inward suction → burst | bruised rim, taint leak, stress crack | 뒤틀린 감금실, shadow well, 혈관형 pressure arc                |
| **몽중몽** | `#1A1537` `#6657C7` `#C2B7FF` `#62D7FF` `#F2F0FF` | afterimage plane, 위상선, 얇은 mist                | delay trail, nested orbit, phase split                   | doubled rim, faint echo offset        | recursive shell, echo tunnel, phase-shift bridge      |
| **곁잠**  | `#0F2E35` `#4FB7C5` `#C1A4F7` `#FFB7A1` `#F5F4FA` | 쌍으로 움직이는 wisp, 보조 shadow, relay glow          | lateral relay, paired sparks, call-response flash        | side-offset auxiliary ring            | side-coupled bridges, companion chamber, relay ribbon |

이 팔레트는 “정답”이라기보다 **현재 언어체계와 물리계층에 가장 잘 맞는 1차 제안안**입니다. 길몽은 (\kappa)와 정렬을, 악몽은 (\tau,\chi)를, 몽중몽은 (\phi)/echo를, 곁잠은 bridge/relay를 강조하는 쪽이므로, 색과 모션도 그 채널에 맞게 설계하는 편이 자연스럽습니다.

## 4. 학파별 VFX 운용 규칙

길몽은 **high value / low noise / thin highlights**가 핵심입니다. 2D에서는 선과 봉인의 정합도를 올려 보이게 하고, 3D에서는 rose window나 성유리처럼 **층이 맞물리는 투명 shell**을 만드는 편이 가장 좋습니다. rose window 연구가 보여주듯 이런 구조는 단순 장식보다도, “결정들이 누적되어 형성된 방사형 구조”라는 감각을 강하게 줍니다. 길몽의 3D는 바로 그 “정렬된 누적성”을 살리는 쪽이 맞습니다. ([itcon.org][7])

악몽은 반대로 **low value field 위에 high-chroma stress accent**가 올라오는 구조가 낫습니다. red·magenta 계열의 고채도 accent는 arousal을 올리고, 주변 value를 낮추면 긴장감이 커집니다. 다만 acid green은 support가 아니라 anomaly cue로만 써야 합니다. 악몽을 전부 초록/보라로 뒤덮으면 오히려 root 판독성이 떨어집니다. 2D에서는 찢어진 rim, 안쪽으로 빨아들이는 그림자, taint leak만 허용하고, 3D에서만 본격적인 pressure tunnel이나 warped chamber를 여는 편이 좋습니다. ([staff.uni-mainz.de][6])

몽중몽은 color보다 **time-offset**가 더 중요합니다. 즉, 색상은 deep indigo + violet + echo cyan 정도로 억제하고, 진짜 signature는 **지연된 복제, 약간 어긋난 afterimage, phase-split orbit**에서 나와야 합니다. VFX 연구가 “효과는 mechanic를 읽게 해야 한다”고 보듯, 몽중몽은 “같은 것이 한 번 더, 조금 다른 위상으로 온다”는 것을 시각적으로 설명해야 합니다. 그래서 3D에서는 nested shell, echo tunnel, delayed reinjection ribbon이 핵심입니다. ([arXiv][5])

곁잠은 가장 어렵습니다. 이 학파는 단순히 부드러운 보조 계열이 아니라, **옆에서 붙고, 옆에서 넘기고, 옆에서 빼앗는 relay aesthetics**를 가져야 합니다. 그래서 palette도 하나의 단일 hue보다는 **paired dual-tone**이 어울립니다. dark teal/cyan을 주축으로 두고, lilac나 pale coral을 “같은 색이 아닌 보조 신호”로 써서 side-coupling을 드러내는 편이 좋습니다. 2D에서는 중심을 바꾸지 않고 측면 offset ring이나 쌍위상 shadow만 주고, 3D에서 side bridges와 relay ribbons가 본격적으로 살아나는 구조를 권합니다.

## 5. 바로 적용 가능한 제작 규칙

실루엣은 다음 순서로 고정하는 것이 좋습니다.
`(a) root macro contour → (b) parent-preserving derivation overlay → (c) grayscale 32px test → (d) busy-background test → (e) school VFX 적용`

school VFX는 다음 순서가 좋습니다.
`(a) root base hue anchor 고정 → (b) school value/saturation transform → (c) motion vocabulary 추가 → (d) residue/rupture palette 추가 → (e) 2D와 3D 가중치 분리`

실무적으로는 아래 다섯 검증이 있으면 충분합니다.

1. **3초 블라인드 추론**: 처음 보는 사람이 root 계열을 맞히는가
2. **흑백 축소 테스트**: 32px에서도 root가 구분되는가
3. **parent trace 테스트**: 파생이 부모 없이도 읽히는 것이 아니라, 부모와의 관계로 읽히는가
4. **combat clutter 테스트**: 복잡한 배경에서 school VFX가 root 판독성을 해치지 않는가
5. **3D divergence 테스트**: 2D semantics는 유지되면서 3D에서만 학파 차이가 충분히 커지는가

이 검증 루프는 semantic transparency를 높이기 위한 notation 평가 연구와, background complexity가 실제 플레이 성능에 영향을 준다는 연구를 합쳐 보면 거의 필수에 가깝습니다. ([UC 홈페이즈][2])

## 6. 최종 정리

지금 구조에서 가장 강한 방향은 이겁니다.

* **실루엣은 알케미처럼 압축적이되, 직접 복제하지 않는다.**
* **구도는 rose window처럼 중심–고리–방사 구조를 갖되, 현재의 radial grammar에 맞게 단순화한다.**
* **파생은 부모 trace를 보존하는 overlay 방식으로 통일한다.**
* **학파는 2D에서 색/빛/질감 accent만, 3D에서 shell·bridge·orbit의 dramatic logic까지 담당한다.**
* **combat에서는 root 판독성이 최우선이고, 학파 미학은 3D lift에서 본격적으로 폭발시킨다.**

한 줄로 줄이면,
**심볼은 “shape로 읽히고”, 학파는 “light와 motion으로 느껴지게” 만드는 것이 현재 체계에 가장 잘 맞습니다.**

[1]: https://www.getty.edu/research/exhibitions_events/exhibitions/alchemy/AoA_symbols.pdf "https://www.getty.edu/research/exhibitions_events/exhibitions/alchemy/AoA_symbols.pdf"
[2]: https://homepages.uc.edu/~niunn/courses/RE-refs/Prosumers-RE13.pdf "https://homepages.uc.edu/~niunn/courses/RE-refs/Prosumers-RE13.pdf"
[3]: https://rationale.csail.mit.edu/publications/Hammond2005Ladder.pdf "https://rationale.csail.mit.edu/publications/Hammond2005Ladder.pdf"
[4]: https://www.researchgate.net/publication/258146295_Impact_of_the_motion_and_visual_complexity_of_the_background_on_players%27_performance_in_video_game-like_displays "https://www.researchgate.net/publication/258146295_Impact_of_the_motion_and_visual_complexity_of_the_background_on_players%27_performance_in_video_game-like_displays"
[5]: https://arxiv.org/pdf/2210.03800 "https://arxiv.org/pdf/2210.03800"
[6]: https://www.staff.uni-mainz.de/oberfeld/downloads/Wilms-Oberfeld2018_Article_ColorAndEmotionEffectsOfHueSat.pdf "https://www.staff.uni-mainz.de/oberfeld/downloads/Wilms-Oberfeld2018_Article_ColorAndEmotionEffectsOfHueSat.pdf"
[7]: https://www.itcon.org/papers/2006_26.content.05932.pdf "https://www.itcon.org/papers/2006_26.content.05932.pdf"

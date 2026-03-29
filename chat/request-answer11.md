request



현재 구조에서 AI가 필수적인 지의 여부를 분석 + 실제 보조 ml을 안에 넣을 경우를 고려하여 심볼 실루엣 자체는 코드/수식 등의 형태로 자연 생성 가능한 형태로 고려하여 다시 제시. 



---



판정은 명확합니다. **현재 구조에서 AI는 필수는 아닙니다.** 이유는 이미 고전적 스케치 인식 계열에서, primitive·제약·도메인 문법만으로도 도형을 구조적으로 해석하는 방식이 성립해 왔기 때문입니다. LADDER는 도메인별 스케치 언어를 기술하고 그로부터 recognizer를 생성하는 구조를 제시했고, PaleoSketch는 제약이 있는 환경에서 primitive recognition과 beautification을 높은 정확도로 수행했습니다. 반면 최근 PICASSO와 InkFM은 손그림을 parametric primitive로 바꾸거나 digital ink 전반을 이해하는 데 ML이 매우 유용하다는 점을 보여 주지만, 이들도 결국 **구조화된 표현** 쪽으로 가고 있습니다. 즉, 당신의 시스템에서는 **의미론과 시뮬레이션의 권위자**로서 AI가 필요한 것이 아니라, **노이즈 많은 입력을 구조화된 표현으로 올려 주는 보조자**로서 AI가 유용합니다. ([Rationale][1])

조금 더 실무적으로 말하면, **forge 중심·스냅 강함·seal 후 확정** 구조라면 AI 없이도 충분히 시작할 수 있습니다. 반대로 **실시간 freehand 전투, 장치 편차, 많은 유저, 낮은 오인식 허용도, 개인화**까지 요구하면 보조 ML은 사실상 강하게 권장됩니다. 사용자별 stroke ordering과 articulation은 실제로 개인 내부에서는 꽤 일관되고 사용자 간에는 더 많이 다르며, Sezgin–Davis는 preferred stroke ordering을 recognition에 활용했습니다. 그래서 현재 구조에서 AI가 필요해지는 지점은 “언어 자체를 이해하는 두뇌”가 아니라, **개인화된 parser prior와 confidence estimator**가 필요한 지점입니다. ([UW Faculty][2])

그래서 최종 권장안은 **procedural-first hybrid**입니다.
핵심 semantics는 규칙계가 잡고, ML은 아래 네 곳까지만 들어갑니다.

| 서브시스템                                 | AI 필요도 | 권장 판단      |
| ------------------------------------- | -----: | ---------- |
| root/derivation 문법 정의                 |     낮음 | 규칙/수식으로 고정 |
| freehand primitive proposal           |     높음 | ML 강권      |
| n-best candidate ranking / confidence |     높음 | ML 강권      |
| 개인화 보정(session/family habit)          |  중간~높음 | ML 유용      |
| canonical SpellID 결정                  |      0 | 규칙계로 고정    |
| graph compile / simulator             |      0 | 규칙계로 고정    |
| 디자이너용 symbol variant 탐색               |     중간 | ML 선택적     |

핵심은 이것입니다.
**AI가 없어도 언어는 성립해야 하고, AI가 들어가면 입력을 덜 짜증나게 만들어야 합니다.**
AI가 semantics를 바꾸면 현재 구조의 장점이 거의 다 사라집니다.

---

## 가장 적합한 심볼 실루엣 표현

지금 구조에서는 심볼을 비트맵 이미지가 아니라 **“생성 가능한 기하 프로그램(glyph program)”** 으로 두는 것이 가장 강합니다. 이 방향은 shape grammar와 procedural model 관점과 잘 맞습니다. shape grammar는 glyph 기반 시각화를 절차적으로 생성하는 데 쓰였고, gothic rose window 연구도 “형태”가 아니라 “생산 논리”를 절차적으로 기술할 수 있음을 보여줬습니다. 이걸 현재 마법진 언어에 옮기면, 심볼은 손으로 그린 아이콘 자산이 아니라 **해시 가능하고 렌더 가능하며 합성 가능한 코드 객체**가 됩니다. ([TU Wien Computer Graphics][3])

가장 권장하는 표현은 **SDF/CSG 기반의 절차 기하문자**입니다.
정규화 좌표계 (p=(x,y)\in[-1,1]^2) 위에서 각 glyph를 signed distance field로 표현합니다.

기본 primitive는 다음 정도면 충분합니다.

[
d_{\text{circle}}(p;c,R)=|p-c|-R
]

[
d_{\text{annulus}}(p;c,R,w)=\left||p-c|-R\right|-w
]

[
d_{\text{segment}}(p;a,b,w)=\mathrm{dist}(p,\overline{ab})-w
]

그리고 조합 연산은

[
U(a,b)=\min(a,b),\qquad
I(a,b)=\max(a,b),\qquad
D(a,b)=\max(a,-b)
]

로 둡니다.
즉, union / intersection / difference를 SDF의 `min/max`로 구성합니다.

이 위에서 각 glyph를

[
G = \Big(B_R,\ {M_i},\ {H_j},\ \Theta\Big)
]

처럼 둡니다.

* (B_R): root shell
* (M_i): additive motif
* (H_j): subtractive hollow/gap
* (\Theta): 파라미터 벡터

실제 contour는

[
d_G(p)=D\Big(U(B_R,\ \bigcup_i M_i),\ \bigcup_j H_j\Big)
]

의 0-level set으로 읽습니다.

이 표현이 좋은 이유는 네 가지입니다.
첫째, **semantic skeleton이 명시적**입니다.
둘째, **resolution-independent**입니다.
셋째, **synthetic training data를 무한히 생성**할 수 있습니다.
넷째, **canonical hash**를 바로 만들 수 있습니다.

즉, 지금 구조에서 심볼 실루엣은 “아트 파일”보다 **프로시저**로 정의하는 편이 맞습니다.

---

## 권장 Glyph Program DSL

실무 구현에서는 SDF를 직접 쓰기보다, 아래 같은 DSL로 저장하는 편이 더 다루기 쉽습니다.

```yaml
glyph:
  root: wind
  shell:
    type: open_annulus
    R: 0.62
    w: 0.08
    gap_dir_deg: 0
    gap_span_deg: 72
  motifs:
    - type: tangent_sweep
      R: 0.30
      span_deg: 150
      phase_deg: -10
    - type: tangent_sweep
      R: 0.45
      span_deg: 160
      phase_deg: 0
    - type: tangent_sweep
      R: 0.58
      span_deg: 170
      phase_deg: 10
    - type: nucleus
      offset: [0.18, 0.0]
      r: 0.05
  hollows: []
  invariants:
    - open_shell
    - eccentric_nucleus
    - sweep_count in {2,3}
```

이 표현의 장점은 ML이 들어가도 **모델이 예측해야 하는 목표가 클래스가 아니라 파라미터 집합**이라는 점입니다. PICASSO가 hand-drawn sketch를 parametric primitive로 옮기는 방향을 택한 이유와도 잘 맞습니다. ([CVF Open Access][4])

---

## Root Seed 실루엣의 “생성형 정규형”

아래는 현재 구조를 기준으로, 손으로만 그리는 pictogram이 아니라 **코드/수식으로 자연 생성 가능한 root silhouette 정규형**입니다.
여기서 모양은 placeholder지만, 생성 논리와 invariant가 핵심입니다.

### 1) 바람

```yaml
root: wind
shell: open_annulus(R=0.62, w=0.08, gap_dir=0°, gap_span=60°~80°)
motifs:
  - tangent_sweep(R=0.30, span=140°~160°, phase=-10°)
  - tangent_sweep(R=0.45, span=150°~170°, phase=0°)
  - tangent_sweep(R=0.58, span=160°~180°, phase=+10°)
  - nucleus(offset=(+0.12~+0.25, 0), r=0.04~0.06)
invariants:
  - shell must remain open
  - nucleus must remain eccentric
  - sweep direction must be coherent
```

의미적으로는 **열림, 편류, 빠져나감**이 먼저 읽혀야 합니다.
그래서 wind는 완전 폐합 금지, 정중앙 nucleus 금지, 대칭 우세 금지가 맞습니다.

### 2) 땅

```yaml
root: earth
shell: closed_annulus(R=0.55, w=0.10)
motifs:
  - base_plinth(width=0.70, height=0.18, y=-0.72)
  - side_brace(side=left,  w=0.18, h=0.28)
  - side_brace(side=right, w=0.18, h=0.28)
  - nucleus(offset=(0,-0.05), r=0.05)
invariants:
  - shell must remain closed
  - lower mass must dominate
  - support elements must touch lower half
```

땅은 **무게중심이 아래** 있어야 하고, 완결성과 기반성이 먼저 보여야 합니다.

### 3) 불꽃

```yaml
root: fire
shell: flame_loop(R=0.55, apex_gain=0.15~0.28, lower_compression=0.08~0.16)
motifs:
  - crown_tongue(x=-0.22, y0=0.20, y1=0.78)
  - crown_tongue(x= 0.00, y0=0.20, y1=0.82)
  - crown_tongue(x=+0.18, y0=0.20, y1=0.76)
  - nucleus(offset=(0,-0.10), r=0.05)
invariants:
  - upward thrust must dominate
  - apex must remain vent-like
  - no full radial symmetry
```

불꽃은 **방출과 상승 압력**이 보여야 하므로, crown과 apex bias가 핵심입니다.

### 4) 물

```yaml
root: water
shell: ripple_stack(count=3, rx=[0.60,0.55,0.48], ry_scale=0.70~0.80)
motifs:
  - spill_notch(dir=-20°~+20°, length=0.12~0.22)
  - nucleus(offset=(0,-0.05), r=0.05)
invariants:
  - parallel flow must dominate
  - no branching tree
  - no hard arrest axis
```

물은 **병렬 파문과 작은 spill**이 핵심입니다.
정지된 대칭축이 들어가면 얼음으로 가까워집니다.

### 5) 식물(생명)

```yaml
root: life
shell: rooted_stem(length=0.90, stem_w=0.06)
motifs:
  - branch(angle=+35°~45°, length=0.45)
  - branch(angle=-35°~45°, length=0.45)
  - micro_bifurcation(level=1, scale=0.55)
  - root_pair(depth=0.18~0.28)
  - nucleus(offset=(0,-0.15), r=0.07)
invariants:
  - root and crown must both exist
  - at least one self-similar split
  - perfect symmetry discouraged
```

생명은 회복 아이콘이 아니라 **발아와 생장 구조**로 읽혀야 합니다.

---

## 파생 요소는 “독립 심볼”보다 “overlay transform”이 낫습니다

이 부분이 중요합니다.
파생 요소를 별도 그림 10개로 두면 semantic transparency와 parser 안정성이 같이 떨어집니다. LADDER/PaleoSketch 계열도 저수준 primitive와 구조 조합을 유지하는 이유가 여기에 가깝습니다. 그래서 현재 구조에서는 파생을 **overlay transform** 으로 두는 것이 맞습니다. ([Rationale][1])

권장 방식은 아래입니다.

### Material overlays

```yaml
poison:
  add:
    - seep_notch(dir=-90°, depth=0.08~0.14)
    - stain_arc(span=30°~50°, phase=-90°)
  preserve_parent_contour: >= 0.70

steel:
  add:
    - brace_frame(thickness=0.05~0.08)
    - flatten_lower_curvature(gain=0.08~0.15)
  preserve_parent_contour: >= 0.75

electric:
  add:
    - fork_discharge(branch_angle=18°~30°)
    - phase_split(offset=small)
  preserve_parent_contour: >= 0.65

ice:
  add:
    - arrest_axis(theta=0° or 90°)
    - facet_quantize(level=1)
  preserve_parent_contour: >= 0.70
```

### Ontic / praxis overlays

```yaml
soul:
  add:
    - detached_halo(offset=(0.10~0.18, 0.10~0.18), r=0.08~0.12)

thought:
  add:
    - inward_echo(count=1~2, shrink=0.70)
    - mild_recursion(scale=0.55)

sacred:
  add:
    - perfect_axis(theta=0° or 90°)
    - clean_halo(thin=true)

void:
  subtract:
    - hollow_core(r=0.10~0.18)
    - null_gap(local=true)

telekinesis:
  add:
    - floating_tether(detached=true)

martial:
  require:
    - void
  add:
    - strike_axis(dir=targetward, flare=0.10~0.18)
    - body_channel_stub(length=0.15~0.25)
```

특히 `無 → 武`는 현재 구조상 매우 자연스럽습니다.
`無`는 **결손/비움**, `武`는 그 결손을 **전방 strike axis**로 되접지한 형태로 두면, 시각적으로도 계보가 보이고 코드적으로도 자연스럽습니다.

---

## 왜 “신경망이 그린 심볼”보다 “프로시저가 만든 심볼”이 맞는가

현재 구조에서는 심볼 그 자체를 end-to-end neural image generator에 맡기는 것은 권장하지 않습니다. 이유는 세 가지입니다.

첫째, **canonical SpellID를 만들어야** 합니다.
둘째, **부모-파생 관계가 보존되어야** 합니다.
셋째, **오류 정정 코드와 checksum이 구조 안에 들어가야** 합니다.

이 세 가지는 이미지 생성기보다 **절차 기하 + 명시적 파라미터**가 훨씬 잘 맞습니다. 반대로 ML은 이 프로시저 공간을 탐색하고, 사람이 그린 noisy ink를 이 공간 위로 올리는 데 유용합니다. PICASSO도 결국 hand-drawn sketch를 parametric primitive로 복원하는 방향이고, InkFM과 digital-ink representation 연구도 시간 순서가 있는 ink를 직접 쓰는 편이 낫다는 쪽입니다. ([CVF Open Access][4])

---

## 보조 ML을 넣는다면 가장 좋은 결합 방식

가장 좋은 결합은 아래와 같습니다.

[
\tilde S \xrightarrow{\text{ML}}
{(\hat G_i,\hat\Theta_i,\hat c_i)}_{i=1}^K
\xrightarrow{\text{typed parser}}
G^*
\xrightarrow{\text{canonicalize}}
\text{SpellID}
]

* (\tilde S): 유저의 noisy digital ink
* (\hat G_i,\hat\Theta_i): ML이 뽑은 glyph family와 파라미터 후보
* (\hat c_i): confidence
* (G^*): 규칙계가 확정한 canonical glyph

즉, ML은 **후보를 제안**하고, 규칙계가 **승인/거절**합니다.

여기에 synthetic data pipeline을 얹으면 더 강해집니다.

[
(G,\Theta,u,\epsilon)\ \mapsto\ \mathrm{render_ink}(G,\Theta;u,\epsilon)
]

* (G,\Theta): procedural glyph와 파라미터
* (u): user style model
* (\epsilon): jitter, slant, oversketch, pause, missing segment 등 노이즈

이렇게 하면 **실제 유저 데이터를 많이 모으기 전에** procedural generator만으로도 대량의 labeled ink를 만들 수 있습니다. shape grammar는 원래 이런 “생산 논리”를 기술하기에 적합하고, PICASSO도 rendering self-supervision으로 파라미터 주석 의존성을 줄였습니다. ([TU Wien Computer Graphics][3])

---

## AI가 “필수”가 되는 조건

현재 구조에서 AI가 사실상 필수에 가까워지는 조건은 아래 네 가지가 동시에 성립할 때입니다.

* freehand 전투 입력을 실시간으로 받는다
* 유저별 작법 편차를 꽤 허용한다
* 오인식 허용도가 낮다
* onboarding 없이도 처음부터 매끄럽게 쓰게 하고 싶다

이때는 보조 ML이 거의 필수에 가깝습니다.
반대로 아래 구조라면 AI 없이 시작해도 됩니다.

* forge 중심
* snap/guide 강함
* seal 확정형
* symbol 수 제한적
* 파생은 overlay 기반
* combat에서는 preset 또는 단순 live cast만 허용

즉, **AI는 구조를 가능하게 만드는 필수요소라기보다, freehand UX를 상업적으로 쓸 만하게 만드는 품질요소**에 가깝습니다.

---

## 최종 권장안

지금 구조에 가장 잘 맞는 답은 아래입니다.

1. **심볼 실루엣의 authoritative 표현은 procedural glyph program으로 고정**
2. **ML은 digital ink → glyph parameter 후보 추정기로만 사용**
3. **canonical SpellID는 procedural form에서만 계산**
4. **학파/VFX/style은 geometry와 분리해서 render layer에서 얹음**
5. **교육형/숙련형도 같은 glyph program의 다른 render preset으로 해결**

한 줄로 줄이면,

**AI는 이 언어의 “의미론”에 필수는 아니고, “입력 해석 품질”에는 매우 유용합니다.
그리고 심볼 실루엣은 그림 파일보다, 코드와 수식으로 정의되는 procedural glyph로 바꾸는 것이 현재 구조와 가장 잘 맞습니다.**

참고용으로 procedural placeholder root 보드와 간단한 생성 코드도 같이 남깁니다.
[Procedural root silhouettes 예시 PNG](sandbox:/mnt/data/procedural_roots.png)
[Procedural glyph prototype code](sandbox:/mnt/data/procedural_spell_glyphs.py)

[1]: https://rationale.csail.mit.edu/publications/Hammond2005Ladder.pdf?utm_source=chatgpt.com "LADDER, a sketching language for user interface developers"
[2]: https://faculty.washington.edu/wobbrock/pubs/gi-13.02.pdf?utm_source=chatgpt.com "Understanding the Consistency of Users' Pen and Finger ..."
[3]: https://www.cg.tuwien.ac.at/research/publications/2009/karnik-09-shapegrammar/karnik-09-shapegrammar-paper.pdf?utm_source=chatgpt.com "A Shape Grammar for Developing Glyph-based ..."
[4]: https://openaccess.thecvf.com/content/WACV2025/papers/Karadeniz_PICASSO_A_Feed-Forward_Framework_for_Parametric_Inference_of_CAD_Sketches_WACV_2025_paper.pdf?utm_source=chatgpt.com "A Feed-Forward Framework for Parametric Inference of CAD ..."

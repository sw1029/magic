# Art Overhaul Plan

이 문서는 `Magic Exam Hall`의 시각 자산(캐릭터·맵·오브젝트·룬·UI)을 "절차적 32×32 도형 합성" 단계에서 "고퀄리티 도트 게임" 수준으로 끌어올리기 위한 전체 계획이다. 한 명이 다 만들 필요는 없고, Phase 단위로 분담 가능하다.

관련 맥락:

- [docs/SPRITE_GUIDE.md](SPRITE_GUIDE.md)는 외부 PNG 도입 규칙과 라이선스 가이드를 이미 정의한다. 본 문서는 그 위에서 "무엇을 어떤 순서로 만들 것인가"를 다룬다.
- [docs/TEAM_DEVELOPMENT_PLAN.md](TEAM_DEVELOPMENT_PLAN.md)는 입력·인식·게임 런타임 경계를 다룬다. Art Overhaul은 그 경계 중 게임 런타임 표현 레이어에 한정해 영향을 준다.
- [PR #109](https://github.com/sw1029/magic/pull/109)에서 `PixelArtFactory`가 PNG 우선 로더로 확장됐다. 본 계획의 모든 자산 교체는 이 로더 위에서 이뤄지므로 코드 변경 없이 점진 적용이 가능하다.

## 1. 현 상태 진단

현재 모든 sprite는 [PixelArtFactory.cs](../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs)가 런타임에 32×32 텍스처에 `Fill`·`Ellipse`·`Diamond`·`Ring`·`Line` 프리미티브로 그려서 만든다. [Resources/Sprites/](../unity/MagicExamHall/Assets/MagicExamHall/Resources/Sprites/) 폴더에는 README 한 장만 있고 PNG는 0장이다.

이 상태에서 "도트 디자인이 빈약하다"는 인상의 구조적 원인은 다음 7가지다.

| # | 원인 | 영향 |
| --- | --- | --- |
| 1 | 캐릭터·타일·룬·가구가 모두 32×32 단일 해상도 | 캐릭터 안에 후드·로브·얼굴·금장식을 다 욱여넣어 형태가 안 읽힘 |
| 2 | 절차 도형 합성(Diamond+Ellipse+Fill)으로만 그림 | 클러스터 셰이딩·선택적 안티앨리어싱·디더링 같은 hand-pixel 기법 불가 |
| 3 | `Shade(c, k)`·`Mix(a, b, t)`로 색을 즉석 생성 + 층별 tint 곱 | 화면 색 수백 개. 톤이 조화되지 않음 |
| 4 | 단일 Sprite 반환, 애니메이션 프레임 개념 없음 | idle 호흡·워크 사이클·캐스팅 모션이 전혀 없어 정적 |
| 5 | 4방향 sprite 없음 | 톱다운 게임인데 캐릭터가 한 방향만 바라봄 |
| 6 | `FloorTile`·`WallTrim`이 단일 비트맵, 오토타일 없음 | 그리드에 깔면 "방안지" 패턴 |
| 7 | URP·2D Light·Pixel Perfect Camera·포스트 FX 모두 미설치 | 룬이 발광하지 않음. 카메라 이동 시 픽셀 떨림 |

이 7가지는 "픽셀을 더 잘 배치"한다고 해결되지 않는다. **파이프라인 자체를 절차 생성 → PNG 자산 + URP 2D 렌더링으로 옮겨야** 한다.

## 2. 핵심 원칙

- 자산을 만들기 전에 **스타일을 lock**한다. 해상도·팔레트·시점·라이팅 방향을 [docs/ART_STYLE_BIBLE.md](ART_STYLE_BIBLE.md)에 명시하고, 모든 PR은 이 문서를 기준으로 리뷰한다.
- **점진 교체 가능**하게 유지한다. PNG 우선 로더가 fallback 구조라 한 자산만 교체해도 게임이 동작한다. 한 PR에 전체 자산 교체를 묶지 않는다.
- **코드보다 자산이 우선**이다. `PixelArtFactory`를 더 정교하게 만드는 방향은 천장에 도달한다. 절차 그림은 폴백·placeholder로만 남긴다.
- **입력/인식 레이어는 영향받지 않는다**. Art Overhaul은 표현 레이어 한정이며 [SpellRecognitionHandoff](../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Recognition/) DTO를 건드리지 않는다.
- **외부 의존(URP)은 한 번에 도입**한다. URP는 셰이더 영향이 있으므로 Phase 1 한 번의 PR로 마이그레이션 후 전 자산이 그 위에서 동작한다.

## 3. Phase 구성

각 Phase는 독립 PR로 올리는 것을 전제로 한다. Phase 0과 Phase 1은 직렬, 나머지는 일정 부분 병렬 가능하다.

```
Phase 0 (Style lock)
  └─> Phase 1 (URP + 2D Light)
        ├─> Phase 2 (Player PNG + 애니메이션)
        ├─> Phase 3 (Autotile 시스템)
        ├─> Phase 4 (Prop 베리에이션)
        ├─> Phase 5 (Rune 손그림)
        └─> Phase 6 (Post FX)
```

### Phase 0 — Style Bible 락 (코드 0줄)

목적: 자산 만들기 전에 결정해야 할 모든 것을 한 문서에 모은다.

산출물:

- [docs/ART_STYLE_BIBLE.md](ART_STYLE_BIBLE.md)
  - 시점(top-down 3/4 oblique 45°)
  - 캐릭터 해상도(48×64), 타일 해상도(32×32), PPU(32)
  - 팔레트(AAP-64 또는 Endesga 32 중 1택)
  - 라이팅 방향(좌상단 → 우하단)
  - 외곽선 규칙(검정 단색 금지, 색별 다크 셰이드)
  - 룬 가독성 규칙(40% 축소 시에도 family 구분 가능)

영향 범위: 문서만. 코드·자산 없음.

검증: 팀 3인 리뷰 승인.

### Phase 1 — URP + 2D Light + Pixel Perfect Camera

목적: 자산 도입 전에 렌더 파이프라인을 한 번에 교체한다.

작업:

- `com.unity.render-pipelines.universal` 패키지 추가
- `2D Renderer Asset` 생성, Quality 설정 바인딩
- Main Camera에 `Pixel Perfect Camera` 컴포넌트 (Reference Resolution 384×216, PPU 32)
- Global 2D Light 1개(어두운 baseline), 양초·룬서클·플레이어 캐스팅에 Point Light 2D
- 기존 sprite는 손대지 않고 그대로 동작 확인

영향 범위:

- 렌더 파이프라인 변경 → 모든 머티리얼 재바인딩 필요
- `PixelMaterialProvider.cs` URP/2D용으로 검토
- 셰이더가 없으므로 Built-in → URP 마이그레이션 도구로 자동 처리 가능

검증:

- 기존 EditMode·PlayMode 테스트 전부 통과
- 양초 주변에 따뜻한 빛 원이 보임
- 카메라 이동 시 픽셀 떨림(서브픽셀 jitter) 없음
- Windows build 성공

롤백: 패키지 제거 + 머티리얼 원복. 1커밋으로 가능.

### Phase 2 — Player PNG + 4방향 + idle/walk/cast 애니메이션

목적: 가장 시선이 가는 단일 자산을 정적 → 살아 있는 것으로 바꾼다. 체감 변화가 가장 큰 Phase.

자산:

```
Resources/Sprites/Player/
  idle_down_0.png  idle_down_1.png       (0.5초 주기 호흡)
  walk_down_0..3.png                     (4프레임 사이클)
  walk_up_0..3.png  walk_left_0..3.png  walk_right_0..3.png
  cast_charge_0..2.png                   (시전 charge)
  cast_release_0..1.png                  (시전 release)
```

자체 제작 부담 시: itch.io의 [Mystic Woods](https://game-endeavor.itch.io/mystic-woods)(CC-BY) 또는 [Penzilla character pack](https://penzilla.itch.io/)이 48×64 마법사 캐릭터를 커버한다.

코드 변경:

- `PixelArtFactory` → `PixelSpriteLibrary`로 확장(단일 Sprite → `SpriteSet`)
- `PixelSpriteView`에 frame ticker(`Animator` 도입 또는 자체 카운터)
- `PixelSpriteKind` enum을 frame이 있는 종류와 없는 종류로 분리

영향 범위:

- [PixelArtFactory.cs](../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs), [PixelSpriteView.cs](../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelSpriteView.cs)
- 플레이어 prefab
- 입력/인식 레이어 영향 없음

검증:

- idle 시 0.5초 주기 호흡 가시
- 워크 시 4방향 모두 정확한 sprite 출력
- 캐스팅 시 charge → release 전이 가시
- Phase 1 PostFX와 함께 보면 자연스러움

### Phase 3 — Autotile 시스템

목적: 바닥·벽이 "방안지"처럼 보이는 문제 해결.

작업:

- `com.unity.2d.tilemap.extras` 추가
- 47-blob 또는 16-Wang 비트마스크 RuleTile 생성
- `FloorTile.png`·`WallTrim.png`를 단일 비트맵에서 **blob sheet**로 교체
- `WorldDrawingController`에서 `SpriteRenderer` 그리드 페인팅을 `Tilemap.SetTile()`로 전환

영향 범위:

- [WorldDrawingController.cs](../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/WorldDrawingController.cs)
- 층별 floor·wall 배치 코드
- 입력/인식·게임 로직 영향 없음

검증:

- 같은 타일 그리드 안에서 코너·모서리·이음매가 자연스럽게 연결
- 1~5층 모두에서 floor 패턴 깨짐 없음

### Phase 4 — Prop 베리에이션

목적: 반복 패턴이 안 보이게 만든다.

자산:

- 책장 3종(책 가득 / 절반 / 마법서 진열)
- 양초 3종(높낮이 다른 3종)
- 바닥 가드 4종(깨끗 / 금간 / 룬 흔적 / 그을음)
- 벽 코너 4종 + 기둥 2종

코드 변경:

- `PixelSpriteKind`에 variant 인덱스 추가 또는 별도 enum 분리
- spawn 위치마다 hash 기반 결정적 랜덤 선택(같은 자리는 항상 같은 변이)

영향 범위:

- [PixelArtFactory.cs](../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs)의 enum
- prop spawn 코드
- 입력/인식 영향 없음

검증:

- 같은 줄에 같은 prop 연속 배치되지 않음
- 5층 5회 플레이 시에도 같은 자리는 같은 변이(결정성 유지)

### Phase 5 — Rune 손그림 교체

목적: 핵심 메커닉인 룬을 가장 정성 들인 자산으로 만든다.

자산:

```
Resources/Sprites/Runes/
  FireRune.png  FireRune_charge_0..2.png
  WaterRune.png  WaterRune_charge_0..2.png
  WindRune.png  WindRune_charge_0..2.png
  EarthRune.png  EarthRune_charge_0..2.png
  LifeRune.png  LifeRune_charge_0..2.png
```

해상도: 64×64 권장(다른 자산보다 크게). 화면에서 작아져도 family 식별 가능해야 함.

영향 범위:

- 절차 룬 그림([PixelArtFactory.cs:339-471](../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs#L339))은 placeholder로만 남기고 PNG로 덮어씀
- 입력/인식 레이어가 룬을 어떻게 판정하는지에는 영향 없음(인식 결과 → 표현 매핑만 변경)

검증:

- 40% 축소 시에도 5 family 시각 구분 가능
- charge 프레임 재생 중 발광이 Phase 1 Point Light와 자연스럽게 합쳐짐

### Phase 6 — Post FX

목적: 분위기 마감.

작업:

- `UniversalRendererPipelineAsset`에 2D Volume 추가
- Bloom(intensity 0.5, threshold 0.9): 룬·양초·발광 강조
- Vignette(intensity 0.25): 시험장 압박감
- 미세 Chromatic Aberration(0.05): CRT 느낌(호불호 있어 기본 OFF, 옵션화)

영향 범위:

- 렌더 설정 한정
- 자산·코드·로직 영향 없음

검증:

- HUD·Magic Note·Ending Report 텍스트 가독성 영향 없음
- 저사양 머신에서 프레임 드랍 없음(URP 2D는 가벼움)

## 4. 합의가 필요한 결정

PR 리뷰에서 댓글로 의견을 모을 항목.

| 항목 | 권장 | 대안 | 결정자 |
| --- | --- | --- | --- |
| 캐릭터 해상도 | 48×64 | 32×32 유지(불권장), 64×96(Sea of Stars급) | 아트 담당 |
| 타일 해상도 | 32×32 | 16×16(Stardew급) | 아트 담당 |
| 팔레트 | AAP-64 | Endesga 32, DB-32, 자체 24색 | 아트 담당 |
| 시점 | Top-down 3/4 oblique 45° | 순수 top-down, side-view | 게임 디자이너 |
| 자산 소싱 | 자체 + Mystic Woods 보완 | 100% 자체, 100% 외부 팩 | 팀 합의 |
| Chromatic Aberration | OFF 기본 + 옵션 | 항상 OFF, 항상 ON | 게임 디자이너 |

## 5. 영향 범위와 비영향 보장

| 레이어 | 영향 받음 | 이유 |
| --- | --- | --- |
| Input Capture | ❌ | 표현 레이어만 변경 |
| Stroke Session | ❌ | 동일 |
| Recognition / Personalization | ❌ | `SpellRecognitionHandoff` DTO 그대로 |
| Game Runtime | ⚠️ 일부 | prop spawn·tilemap 페인팅 변경(Phase 3·4) |
| Feedback / Tutorial / Logs | ❌ | 텍스트·로그 변경 없음 |
| Render Pipeline | ⚠️ 전체 | Phase 1에서 URP 마이그레이션 |
| Tests | ⚠️ 일부 | sprite 생성 가정한 테스트는 PNG 우선 로더 mock 필요 |

입력 담당(sw1029)은 본 계획 전 Phase에 직접 영향이 없다. 인식 담당도 마찬가지.

## 6. 자산 소싱 전략

| 자산군 | 권장 | 이유 |
| --- | --- | --- |
| 플레이어 캐릭터 | 외부 팩 + 팔레트 통일 | 자체 제작 가장 어려움. Mystic Woods/Penzilla 추천 |
| 바닥·벽 타일 | 자체 또는 Kenney CC0 | 톤 통일이 중요 |
| 룬 | 자체 제작 | 게임 정체성. 외부에 없음 |
| 가구·소품 | Kenney 또는 Mystic Woods | 변이만 추가 |
| 이펙트(빛·파티클) | 자체 + URP 2D Light 조합 | 코드+자산 결합 |

외부 팩 도입 시 [docs/SPRITE_GUIDE.md](SPRITE_GUIDE.md)의 라이선스 표를 따른다. 받은 자산은 `docs/asset-licenses/` 아래 라이선스 파일 보관, `docs/CREDITS.md`에 한 줄 추가.

## 7. 타임라인 가이드

강제는 아니나 vertical slice 1~2주 기준 권장 진행.

| Phase | 예상 작업량 | 1인 담당 가능 |
| --- | --- | --- |
| 0 Style Bible | 0.5일 | ✅ |
| 1 URP + 2D Light | 1일 | ✅ |
| 2 Player + 애니 | 2~3일 | ✅ |
| 3 Autotile | 1~2일 | ✅ |
| 4 Prop 베리에이션 | 꾸준히 | ✅ |
| 5 Rune 손그림 | Family당 0.5일 × 5 = 2~3일 | ✅ |
| 6 Post FX | 0.5일 | ✅ |

Phase 0·1 끝나는 시점에서 한 번 빌드하고 팀원이 직접 플레이해서 다음 Phase 우선순위를 재조정한다.

## 8. 롤백 시나리오

각 Phase는 독립 PR이므로 단일 revert로 되돌릴 수 있다. 특히 주의할 점:

- Phase 1(URP)을 revert하면 Phase 6(Post FX)는 자동 무효화된다.
- Phase 2 캐릭터 PNG는 [Resources/Sprites/](../unity/MagicExamHall/Assets/MagicExamHall/Resources/Sprites/) 파일만 삭제하면 절차 폴백으로 자동 복귀.
- Phase 3 autotile은 RuleTile asset과 Tilemap 컴포넌트 제거가 필요(코드 revert만으로는 부족).

## 9. 진행 체크리스트

- [ ] Phase 0 — [docs/ART_STYLE_BIBLE.md](ART_STYLE_BIBLE.md) merged
- [ ] Phase 1 — URP + 2D Renderer + Pixel Perfect Camera 적용
- [ ] Phase 2 — Player 4방향 + idle/walk/cast 애니 도입
- [ ] Phase 3 — Tilemap autotile 시스템 도입
- [ ] Phase 4 — Prop 변이 3종 이상 도입
- [ ] Phase 5 — Rune 5 family PNG 교체
- [ ] Phase 6 — Bloom/Vignette 적용

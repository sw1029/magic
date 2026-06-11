- id: T30-02
- status: done
- owner: SilverSupplier
- depends_on: T30-01
- blocks: -

# Phase 2 — Player PNG + 4방향 + idle/walk/cast 애니메이션

## 목적

가장 시선이 가는 단일 자산을 정적 절차 그림 → PNG 4방향 애니메이션으로 교체한다. 체감 변화가 가장 큰 Phase.

## 자산

```
Resources/Sprites/Player/
  idle_down_0.png  idle_down_1.png            (0.5초 주기 호흡)
  idle_up_0..1.png  idle_left_0..1.png  idle_right_0..1.png
  walk_down_0..3.png                          (4프레임 사이클)
  walk_up_0..3.png  walk_left_0..3.png  walk_right_0..3.png
  cast_charge_0..2.png                        (3프레임)
  cast_release_0..1.png                       (2프레임)
```

- 해상도 48×64 (Style Bible §2)
- 팔레트 AAP-64 (Style Bible §3)
- 광원 좌상단 → 우하단 (Style Bible §4)

자체 제작 부담 시 [Mystic Woods](https://game-endeavor.itch.io/mystic-woods)(CC-BY) 또는 [Penzilla character pack](https://penzilla.itch.io/) 검토. 라이선스 [docs/SPRITE_GUIDE.md](../SPRITE_GUIDE.md) §라이선스 규칙 준수.

## 코드 작업

- [x] `PixelArtFactory` → `PixelSpriteLibrary`로 확장: 단일 `Sprite` 반환에서 `SpriteSet`(방향×상태×프레임) 반환
- [x] [PixelSpriteView.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelSpriteView.cs)에 frame ticker 추가(Animator 도입 또는 자체 카운터)
  - `PlayerSpriteAnimator`가 player 전용 frame ticker를 담당하고, 정적 `PixelSpriteView`는 fallback/static sprite 전용으로 유지한다.
- [x] `PixelSpriteKind` enum을 frame이 있는 종류와 없는 종류로 분리
  - 정적 kind enum은 유지하고, framed player는 `PlayerAnimationState`/`PlayerFacing` 조합으로 분리했다.
- [x] 플레이어 prefab에 facing direction 입력 → 자동 sprite 선택
- [x] 캐스팅 상태 전이(idle → cast_charge → cast_release → idle) 연결

## 검증

- [x] idle 시 0.5초 주기 호흡 가시
- [x] 워크 시 4방향 모두 정확한 sprite 출력
- [x] 캐스팅 시 charge → release 전이 가시
- [x] EditMode/PlayMode 테스트 전부 통과
- [x] 외부 PNG 미설치 시 절차 폴백으로 안전 동작(SPRITE_GUIDE 로더 규칙)
- [x] Phase 1 PostFX와 합쳐 자연스러움

## 완료 기록

- 2026-06-07: 자체 제작 48×64 플레이어 PNG 29프레임(`idle_*`, `walk_*`, `cast_charge_*`, `cast_release_*`) 생성.
- 2026-06-07: `PlayerSpriteLibrary`/`PlayerSpriteAnimator` 도입. 이동 입력은 4방향 facing과 walk cycle로, 주문 처리 경로는 cast charge/release로 연결.
- 2026-06-07: EditMode 61/61, PlayMode 22/22 통과. Windows player build 0 errors/0 warnings, player smoke 통과.
- 2026-06-07: floor screenshot export로 플레이어 렌더와 URP 조명 합성을 확인. 5층 라벨 과밀은 후속 UX/UI polish에서 별도 개선 필요.

## 영향 범위

- [PixelArtFactory.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs)
- [PixelSpriteView.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelSpriteView.cs)
- 플레이어 prefab
- 입력/인식 레이어 영향 없음

## 자산 소싱 결정

- [x] 자체 제작 선택
- 외부 asset을 사용하지 않았으므로 [docs/CREDITS.md](../CREDITS.md) 및 [docs/asset-licenses/](../asset-licenses/) 추가 갱신 없음.

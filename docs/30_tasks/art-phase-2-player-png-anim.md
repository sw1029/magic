- id: T30-02
- status: blocked
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

- [ ] `PixelArtFactory` → `PixelSpriteLibrary`로 확장: 단일 `Sprite` 반환에서 `SpriteSet`(방향×상태×프레임) 반환
- [ ] [PixelSpriteView.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelSpriteView.cs)에 frame ticker 추가(Animator 도입 또는 자체 카운터)
- [ ] `PixelSpriteKind` enum을 frame이 있는 종류와 없는 종류로 분리
- [ ] 플레이어 prefab에 facing direction 입력 → 자동 sprite 선택
- [ ] 캐스팅 상태 전이(idle → cast_charge → cast_release → idle) 연결

## 검증

- [ ] idle 시 0.5초 주기 호흡 가시
- [ ] 워크 시 4방향 모두 정확한 sprite 출력
- [ ] 캐스팅 시 charge → release 전이 가시
- [ ] EditMode/PlayMode 테스트 전부 통과
- [ ] 외부 PNG 미설치 시 절차 폴백으로 안전 동작(SPRITE_GUIDE 로더 규칙)
- [ ] Phase 1 PostFX와 합쳐 자연스러움

## 영향 범위

- [PixelArtFactory.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs)
- [PixelSpriteView.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelSpriteView.cs)
- 플레이어 prefab
- 입력/인식 레이어 영향 없음

## 자산 소싱 결정 필요

- [ ] 자체 제작 vs Mystic Woods vs Penzilla 선택
- 선택 후 [docs/CREDITS.md](../CREDITS.md)에 출처 추가, [docs/asset-licenses/](../asset-licenses/) 아래 라이선스 파일 보관

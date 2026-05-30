# Sprites

이 폴더에 PNG 파일을 두면 `PixelArtFactory`가 자동으로 procedural 도형 대신 사용합니다.

자세한 규칙은 `docs/SPRITE_GUIDE.md` 참조.

## 빠른 시작

1. PNG 파일 이름을 `PixelSpriteKind` enum 값과 동일하게 둡니다 (예: `Player.png`, `FireRune.png`).
2. Unity Editor에서 import 설정 확인: PPU 16, Filter Point, Wrap Clamp, Pivot Center.
3. Play 모드 실행 시 자동 교체.
4. `docs/CREDITS.md`에 출처를 한 줄 추가합니다.

## 라이선스 보관

받은 자산의 LICENSE 또는 README 파일은 `docs/asset-licenses/` 아래에 보관합니다. 이 `Resources/Sprites` 폴더에는 런타임에 불러올 PNG만 둡니다.

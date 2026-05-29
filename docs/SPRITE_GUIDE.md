# Sprite Guide

`Magic Exam Hall`의 비주얼 자산을 외부 픽셀 아트로 교체할 때 따르는 규칙입니다. 자산만 정해진 자리에 두면 코드 수정 없이 바로 게임에 반영됩니다.

## 경로와 명명 규칙

PNG 파일을 다음 위치에 둡니다.

```
unity/MagicExamHall/Assets/MagicExamHall/Resources/Sprites/
```

파일 이름은 `PixelArtFactory.cs`의 `PixelSpriteKind` enum 값과 정확히 일치해야 합니다. 대소문자도 같게 둡니다.

| Kind | 파일명 | 용도 |
| --- | --- | --- |
| `Player` | `Player.png` | 입학생 마법사 |
| `Station` | `Station.png` | 보조 설치물 |
| `Target` | `Target.png` | 일반 base 목표 (fallback) |
| `Pulse` | `Pulse.png` | 시전 펄스·위험 잔향 |
| `FloorTile` | `FloorTile.png` | 바닥 타일 (반복) |
| `WallTrim` | `WallTrim.png` | 벽 장식 (반복) |
| `Rug` | `Rug.png` | 중앙 카펫 (반복) |
| `Bookshelf` | `Bookshelf.png` | 양쪽 책장 |
| `Candle` | `Candle.png` | 모서리 촛불 |
| `RuneCircle` | `RuneCircle.png` | overlay·combo 목표 룬 |

family별 base 룬은 시각 정합성 PR에서 enum이 확장된 뒤에 사용 가능합니다.

| Kind | 파일명 | 형태 권장 |
| --- | --- | --- |
| `FireRune` | `FireRune.png` | 위로 솟은 닫힌 삼각형 |
| `WaterRune` | `WaterRune.png` | 닫힌 원형 루프 |
| `WindRune` | `WindRune.png` | 평행한 열린 세 줄 |
| `EarthRune` | `EarthRune.png` | 아래가 넓은 사다리꼴 |
| `LifeRune` | `LifeRune.png` | 줄기와 위로 갈라지는 두 가지 |

## 권장 스펙

- **해상도**: 32×32 픽셀 권장. 16×16 또는 64×64도 동작하지만 다른 sprite와 같은 단위로 그려야 화면에서 크기가 일관됨.
- **Pixels per Unit**: 16 (코드 상수와 일치). 다른 PPU로 그렸다면 Unity 임포트 설정에서 PPU를 같이 16으로 맞춰 import.
- **Filter Mode**: Point (no filter). Unity 임포트 시 자동으로 잡지만 확인.
- **Wrap Mode**: Clamp. 단 반복 타일 sprite는 Repeat로 설정 가능.
- **Pivot**: Center.
- **Transparency**: 알파 채널 포함 PNG.
- **Palette**: 일관된 16~24색 팔레트 권장. 외부 팩을 섞을 때는 [Lospec](https://lospec.com/palette-list) 같은 사이트의 팔레트로 통일 처리하면 톤이 안 깨짐.

## 로더 동작

`PixelArtFactory.CreateSprite` 호출 시:

1. `Resources/Sprites/<Kind>` 경로에서 PNG 검색
2. 있으면 그대로 반환 (원본 색 유지)
3. 없으면 기존 코드 기반 procedural 도형으로 fallback

따라서 PNG가 일부만 준비돼도 그 sprite만 교체되고 나머지는 그대로 동작합니다. 점진적 교체가 가능합니다.

캐싱이 있으므로 자산을 새로 넣거나 바꾼 직후엔 `PixelArtFactory.ResetExternalSpriteCache()`를 한 번 호출하거나 Editor 재시작.

## 색 처리

외부 sprite는 원본 색 그대로 표시됩니다. `PixelArtFactory.CreateSprite`의 `primary`·`secondary` 인자는 procedural fallback에만 적용됩니다.

층별로 같은 sprite를 다른 색조로 보이고 싶다면 호출 측에서 `SpriteRenderer.color`를 곱셈 tint로 설정. 예를 들어 1층의 따뜻한 톤과 4층의 위험한 톤은 같은 `FloorTile.png` 위에 `Color` 곱으로 처리합니다.

## 라이선스 규칙

저장소는 MIT 라이선스로 배포되므로 외부 자산도 호환되는 라이선스만 받아들입니다.

| 라이선스 | 가능 여부 | 의무 |
| --- | --- | --- |
| CC0 | 가능 | 없음 (선택적 출처 명시) |
| CC-BY 4.0 | 가능 | `docs/CREDITS.md`에 출처·작가 명시 |
| CC-BY-SA | 가능하지만 검토 필요 | 동일 조건 배포 의무. 코드 라이선스와 충돌 가능 |
| CC-BY-NC | 사용 금지 | 비상업 한정 |
| 독점·재배포 금지 | 사용 금지 | — |
| 명시 라이선스 없음 | 사용 금지 | 작가에게 확인 필요 |

자산을 받기 전 라이선스를 먼저 확인하고, 받은 후 즉시 `docs/CREDITS.md`에 한 줄 추가합니다.

## 후보 팩 탐색 방향

검증된 자유 자산 출처:

- **[Kenney](https://kenney.nl/)** — CC0. 톱다운 RPG·던전 타일·캐릭터 sprite 다수. 일관된 톤, 안전.
- **[OpenGameArt.org](https://opengameart.org/)** — CC0~CC-BY 혼재. 마법·룬·이펙트 검색이 풍부.
- **[itch.io](https://itch.io/game-assets/free/tag-pixel-art)** — 무료 픽셀 아트 태그. Mystic Woods 같은 환경 팩, Penzilla의 캐릭터 팩 등.
- **[Lospec](https://lospec.com/)** — 팔레트 라이브러리. 톤 통일에 사용.

검색 키워드 권장: `top-down pixel rpg`, `magic spell pixel`, `wizard pixel character`, `dungeon tile 16x16`.

자산을 후보로 정하면 다음을 평가:

1. 톤이 어두운 돌·마법탑 분위기와 맞는가
2. 같은 해상도(16 또는 32)에 정렬되는가
3. 라이선스가 안전한가
4. 캐릭터·환경·이펙트 중 어디까지 커버하는가

한 팩으로 다 못 채울 가능성이 큽니다. 환경은 A 팩, 캐릭터는 B 팩, 룬은 C 팩처럼 묶어 쓰되 팔레트만 같게 맞추면 인상이 통일됩니다.

## 작업 흐름

1. 후보 팩 다운로드 후 라이선스 파일 보관 (`Assets/MagicExamHall/Resources/Sprites/_LICENSES/`)
2. PNG를 위 표의 이름 규칙대로 저장
3. Unity Editor에서 import 설정 점검 (PPU 16, Point filter, Clamp)
4. Play로 실행해 자동 교체 확인
5. `docs/CREDITS.md`에 출처 추가
6. 새 sprite kind를 enum에 추가했다면 본 문서 표도 같이 갱신

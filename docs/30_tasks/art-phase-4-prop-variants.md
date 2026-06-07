- id: T30-04
- status: done
- owner: SilverSupplier
- depends_on: T30-01
- blocks: -

# Phase 4 — Prop 베리에이션 추가

## 목적

같은 prop이 줄지어 반복되는 패턴 제거. 같은 자리는 항상 같은 변이를 출력하는 결정적 hash 기반 선택.

## 자산

- [x] 책장 3종(책 가득 / 절반 / 마법서 진열)
- [x] 양초 3종(높낮이 다른 3종)
- [x] 바닥 가드 4종(깨끗 / 금간 / 룬 흔적 / 그을음)
- [x] 벽 코너 4종 + 기둥 2종

모두 Style Bible 해상도·팔레트·광원 방향 준수.

## 코드 작업

- [x] `PixelSpriteView.variantIndex`로 `kind + variant` 조합 지원
- [x] [PixelArtFactory.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs) PNG 로더가 variant 경로 처리
- [x] spawn 위치 좌표 → 결정적 hash → variant 선택 helper
- [x] prop spawn 호출 측 마이그레이션
- [x] [PropVariantSpriteGenerator.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Editor/PropVariantSpriteGenerator.cs)로 내부 제작 PNG variant 생성

## 검증

- [x] 같은 줄에 같은 prop 연속 배치되지 않음
- [x] 5층 포함 모든 층 reload 시 같은 자리는 항상 같은 변이(결정성 유지)
- [x] EditMode/PlayMode 테스트 전부 통과
- [x] 외부 PNG variant 미설치 시 base PNG로 폴백, base PNG도 없으면 절차 폴백

## 영향 범위

- [PixelArtFactory.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs) enum
- prop spawn 코드
- 입력/인식 영향 없음

## 비고

본 Phase는 자산 누적 작업이라 한 PR로 끝내지 않아도 됨. 첫 PR에 책장·양초만, 다음 PR에 바닥 가드·벽 코너처럼 분할 가능.

## 완료 로그

- `PixelSpriteKind`는 기존 Unity enum 직렬화 값을 흔들지 않도록 새 prop kind(`FloorGuard`, `WallCorner`, `Pillar`)를 enum 끝에 추가했다.
- `PixelSpriteView.variantIndex`와 `PixelArtFactory.CreateSprite(..., variantIndex)`를 추가했다. 로더는 `Sprites/<Kind>_<Variant>`, `Sprites/<Kind>/<Variant>`, base `Sprites/<Kind>` 순서로 찾고, 모두 없으면 절차 sprite를 만든다.
- 결정적 variant 선택은 floor number, 좌표, prop 이름 salt를 hash에 넣는다. 같은 줄에서 바로 옆 prop이 같은 variant가 되면 다음 variant로 한 칸 밀어 반복을 막는다.
- `PropVariantSpriteGenerator.Generate`로 내부 제작 PNG를 생성했다: 책장 3종, 양초 3종, 바닥 가드 4종, 벽 코너 4종, 기둥 2종.
- 1~5층 런타임 floor art에 책장, 양초, 벽 코너, 기둥, 바닥 가드를 variant prop으로 배치했다.
- 검증:
  - Unity EditMode: 66/66 passed
  - Unity PlayMode: 24/24 passed

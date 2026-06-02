- id: T30-04
- status: blocked
- depends_on: T30-01
- blocks: -

# Phase 4 — Prop 베리에이션 추가

## 목적

같은 prop이 줄지어 반복되는 패턴 제거. 같은 자리는 항상 같은 변이를 출력하는 결정적 hash 기반 선택.

## 자산

- [ ] 책장 3종(책 가득 / 절반 / 마법서 진열)
- [ ] 양초 3종(높낮이 다른 3종)
- [ ] 바닥 가드 4종(깨끗 / 금간 / 룬 흔적 / 그을음)
- [ ] 벽 코너 4종 + 기둥 2종

모두 Style Bible 해상도·팔레트·광원 방향 준수.

## 코드 작업

- [ ] `PixelSpriteKind`에 variant 인덱스 추가, 또는 `Bookshelf_A`/`Bookshelf_B`/`Bookshelf_C` 별도 enum 분리
- [ ] [PixelArtFactory.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs) PNG 로더가 variant 경로 처리
- [ ] spawn 위치 좌표 → 결정적 hash → variant 선택 helper
- [ ] prop spawn 호출 측 마이그레이션

## 검증

- [ ] 같은 줄에 같은 prop 연속 배치되지 않음
- [ ] 5층 5회 플레이 시 같은 자리는 항상 같은 변이(결정성 유지)
- [ ] EditMode/PlayMode 테스트 전부 통과
- [ ] 외부 PNG variant 미설치 시 base PNG로 폴백, base PNG도 없으면 절차 폴백

## 영향 범위

- [PixelArtFactory.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs) enum
- prop spawn 코드
- 입력/인식 영향 없음

## 비고

본 Phase는 자산 누적 작업이라 한 PR로 끝내지 않아도 됨. 첫 PR에 책장·양초만, 다음 PR에 바닥 가드·벽 코너처럼 분할 가능.

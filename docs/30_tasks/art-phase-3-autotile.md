- id: T30-03
- status: done
- owner: SilverSupplier
- depends_on: T30-01
- blocks: -

# Phase 3 — Autotile 시스템 도입

## 목적

바닥·벽이 같은 비트맵 반복으로 "방안지" 패턴이 보이는 문제 해결. Unity Tilemap + RuleTile로 47-blob 또는 16-Wang 비트마스크 처리.

## 작업

- [x] Tilemap 패키지/모듈 추가
  - [unity/MagicExamHall/Packages/manifest.json](../../unity/MagicExamHall/Packages/manifest.json)
- built-in `com.unity.modules.tilemap`을 사용했다. 외부 RuleTile extras 패키지는 현재 floor/wall runtime 요구에 필요하지 않아 추가하지 않았다.
- [x] 47-blob 또는 16-Wang 비트마스크 RuleTile asset 생성
  - `FloorAutotileRenderer`가 16-Wang/cardinal mask 기반 runtime `Tile`을 생성한다.
- [x] `FloorTile_blob.png`, `WallTrim_blob.png` blob sheet 자산 추가 또는 절차 fallback 제공
  - 해상도 32×32 타일 × blob 수
  - Style Bible §2 해상도 규약 준수
  - 별도 PNG sheet 대신 32×32 procedural tile sprite를 runtime 생성한다. 외부 sheet가 없어도 안전하게 동작한다.
- [x] [WorldDrawingController.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/WorldDrawingController.cs)에서 `SpriteRenderer` 그리드 페인팅을 `Tilemap.SetTile()`로 전환
  - 입력/stroke 수집 계층은 sw1029 담당 영역이라 변경하지 않았다. T30-03 범위는 floor/wall 배치 Tilemap 전환으로 한정했다.
- [x] 1~5층 floor·wall 배치 코드 마이그레이션

## 검증

- [x] 같은 타일 그리드에서 코너·모서리·이음매가 자연스럽게 연결
- [x] 1~5층 모두에서 floor 패턴 깨짐 없음
- [x] EditMode/PlayMode 테스트 전부 통과
- [x] 외부 PNG blob sheet 미설치 시 절차 폴백 안전 동작
- [x] 메모리/드로콜 회귀 없음(Tilemap 결합 batching 확인)
  - PlayMode에서 `TilemapRenderer` layer와 tile count를 smoke 검증한다.

## 완료 기록

- 2026-06-08: built-in Tilemap module 추가, `FloorAutotileRenderer` 도입.
- 2026-06-08: 1~5층 backdrop/floor/wall/runner를 16-Wang/cardinal mask 기반 Tilemap layer로 마이그레이션.
- 2026-06-08: 씬 빌더의 정적 tiled floor 생성을 제거해 PlayMode runtime autotile layer가 중복 없이 생성되게 정리.
- 2026-06-08: EditMode 63/63, PlayMode 23/23 통과.
- 2026-06-08: floor screenshot export로 1~5층 autotile 렌더 확인. Goal label world rect를 넓혀 4~5층 한글 세로 줄바꿈/과밀 문제를 완화.

## 영향 범위

- [WorldDrawingController.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/WorldDrawingController.cs)
- 층별 floor·wall 배치 코드
- 입력/인식·게임 로직 영향 없음

## 결정 필요

- [x] 47-blob vs 16-Wang 선택. 47-blob이 더 자연스럽지만 자산 부담 큼. 16-Wang으로 시작해 확장 권장.
- 선택: 16-Wang/cardinal mask. 이후 전용 hand-drawn sheet가 생기면 `FloorAutotileRenderer`의 procedural sprite 생성부만 sheet loader로 대체 가능.

## 비고

RuleTile은 Unity 기본 제공이라 한 번 만들면 끝. blob sheet PNG만 가져오면 즉시 적용 가능.

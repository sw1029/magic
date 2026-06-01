---
id: art-phase-3-autotile
status: pending
depends_on:
  - art-phase-1-urp-2dlight
blocks: []
---

# Phase 3 — Autotile 시스템 도입

## 목적

바닥·벽이 같은 비트맵 반복으로 "방안지" 패턴이 보이는 문제 해결. Unity Tilemap + RuleTile로 47-blob 또는 16-Wang 비트마스크 처리.

## 작업

- [ ] `com.unity.2d.tilemap.extras` 패키지 추가
  - [unity/MagicExamHall/Packages/manifest.json](../../unity/MagicExamHall/Packages/manifest.json)
- [ ] 47-blob 또는 16-Wang 비트마스크 RuleTile asset 생성
- [ ] `FloorTile_blob.png`, `WallTrim_blob.png` blob sheet 자산 추가
  - 해상도 32×32 타일 × blob 수
  - Style Bible §2 해상도 규약 준수
- [ ] [WorldDrawingController.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/WorldDrawingController.cs)에서 `SpriteRenderer` 그리드 페인팅을 `Tilemap.SetTile()`로 전환
- [ ] 1~5층 floor·wall 배치 코드 마이그레이션

## 검증

- [ ] 같은 타일 그리드에서 코너·모서리·이음매가 자연스럽게 연결
- [ ] 1~5층 모두에서 floor 패턴 깨짐 없음
- [ ] EditMode/PlayMode 테스트 전부 통과
- [ ] 외부 PNG blob sheet 미설치 시 절차 폴백 안전 동작
- [ ] 메모리/드로콜 회귀 없음(Tilemap 결합 batching 확인)

## 영향 범위

- [WorldDrawingController.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/WorldDrawingController.cs)
- 층별 floor·wall 배치 코드
- 입력/인식·게임 로직 영향 없음

## 결정 필요

- [ ] 47-blob vs 16-Wang 선택. 47-blob이 더 자연스럽지만 자산 부담 큼. 16-Wang으로 시작해 확장 권장.

## 비고

RuleTile은 Unity 기본 제공이라 한 번 만들면 끝. blob sheet PNG만 가져오면 즉시 적용 가능.

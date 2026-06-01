---
id: art-phase-1-urp-2dlight
status: pending
depends_on:
  - art-phase-0-style-bible
blocks:
  - art-phase-2-player-png-anim
  - art-phase-3-autotile
  - art-phase-4-prop-variants
  - art-phase-5-rune-handdrawn
  - art-phase-6-postfx
---

# Phase 1 — URP + 2D Light + Pixel Perfect Camera 도입

## 목적

자산 도입 전에 렌더 파이프라인을 Built-in RP에서 URP 2D Renderer로 한 번에 교체한다. 이후 Phase에서 모든 자산이 URP 위에서 동작한다.

## 작업

- [ ] `com.unity.render-pipelines.universal` 패키지 추가
  - [unity/MagicExamHall/Packages/manifest.json](../../unity/MagicExamHall/Packages/manifest.json)
- [ ] `2D Renderer Asset` 생성, Quality 설정에 바인딩
- [ ] Main Camera에 `Pixel Perfect Camera` 컴포넌트
  - Reference Resolution 384×216
  - PPU 32
  - Crop Frame: Pillarbox + Letterbox
  - Stretch Fill: OFF
- [ ] Global 2D Light 1개(어두운 baseline, intensity 0.3~0.5)
- [ ] 양초·룬서클·플레이어 캐스팅에 Point Light 2D 부착
- [ ] [PixelMaterialProvider.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelMaterialProvider.cs) URP/2D 머티리얼로 교체
- [ ] 기존 sprite는 손대지 않고 동작 확인

## 검증

- [ ] EditMode 테스트 전부 통과
- [ ] PlayMode 테스트 전부 통과
- [ ] Windows build 성공
- [ ] 양초 주변에 따뜻한 빛 원 가시
- [ ] 카메라 이동 시 픽셀 떨림(서브픽셀 jitter) 없음
- [ ] 룬서클 위치에서 family 색에 맞는 발광 가시

## 영향 범위

- 렌더 파이프라인 전체 교체 → 모든 머티리얼 재바인딩
- [PixelMaterialProvider.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelMaterialProvider.cs) 일부 수정
- 입력/인식 레이어 영향 없음
- 게임 로직 영향 없음

## 롤백

패키지 제거 + Quality 설정에서 Pipeline Asset 분리 + 머티리얼 원복. 1커밋으로 가능.

## 비고

URP 마이그레이션 도구가 Built-in 셰이더를 자동 변환한다. 자체 셰이더 없으므로 수동 작업 최소.

Phase 0 lock 후에만 시작. 본 Phase가 끝나기 전에는 Phase 2~6 시작 금지.

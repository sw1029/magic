- id: T30-06
- status: blocked
- owner: SilverSupplier
- depends_on: T30-01
- blocks: -

# Phase 6 — Post FX (Bloom / Vignette / 선택적 CA)

## 목적

분위기 마감. 룬·양초 발광 강조, 시험장 압박감 표현.

## 작업

- [ ] `UniversalRendererPipelineAsset`에 2D Volume 추가
- [ ] Bloom 설정
  - intensity 0.5
  - threshold 0.9
  - 룬·양초·발광 강조
- [ ] Vignette 설정
  - intensity 0.25
  - 시험장 압박감
- [ ] Chromatic Aberration (선택적)
  - 기본 OFF, 게임 옵션에서 ON 가능
  - intensity 0.05
  - CRT 느낌(호불호 있음)

## 검증

- [ ] HUD·Magic Note·Ending Report 텍스트 가독성 영향 없음 → 스크린샷 비교
- [ ] 저사양 머신에서 프레임 드랍 없음 (URP 2D Volume은 가벼움)
- [ ] EditMode/PlayMode 테스트 전부 통과
- [ ] Windows build 성공
- [ ] 룬 charge 시 발광이 자연스럽게 번짐

## 영향 범위

- 렌더 설정 한정
- 자산·코드·로직 영향 없음

## 결정 필요

- [ ] Chromatic Aberration 기본 OFF + 옵션 vs 항상 OFF vs 항상 ON 선택

## 비고

본 Phase는 다른 Phase가 끝나지 않아도 시작 가능(Phase 1 완료만 전제). 다만 자산이 적은 상태에서 효과만 켜면 빈약한 느낌이 강조될 수 있으므로 Phase 2 이상 끝난 후 적용 권장.

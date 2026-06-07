- id: T30-05
- status: blocked
- owner: SilverSupplier
- depends_on: T30-01
- blocks: -

# Phase 5 — Rune 손그림 PNG 교체

## 목적

게임 핵심 메커닉인 룬 5 family를 절차 도형 합성에서 손그림 PNG로 교체. 가장 정성 들인 자산이어야 함.

## 자산

```
Resources/Sprites/Runes/
  FireRune.png       FireRune_charge_0..2.png
  WaterRune.png      WaterRune_charge_0..2.png
  WindRune.png       WindRune_charge_0..2.png
  EarthRune.png      EarthRune_charge_0..2.png
  LifeRune.png       LifeRune_charge_0..2.png
```

- 해상도 64×64 (Style Bible §2, 다른 자산보다 큰 이유는 가독성)
- 40% 축소 시(약 26×26)에도 family 5종 식별 가능해야 함
- 각 family는 형태로 우선 구분, 색은 보조 (Style Bible §7)
- charge 프레임은 base와 같은 형태 + 발광 강도만 다르게(형태 변경 금지, Style Bible §10)

## 코드 작업

- [ ] 절차 룬 그림([PixelArtFactory.cs:339-471](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs#L339))은 placeholder로 유지
- [ ] PNG 우선 로더로 자동 교체 확인
- [ ] charge 프레임 재생 시 Phase 1 Point Light 강도와 동기화

## 검증

- [ ] 40% 축소 시(약 26×26 미리보기) 5 family 시각 구분 가능 → PR 본문에 비교 이미지 첨부
- [ ] charge 프레임 재생 중 발광이 Phase 1 Point Light와 자연스럽게 합쳐짐
- [ ] EditMode/PlayMode 테스트 전부 통과
- [ ] 인식 결과 → 표현 매핑 회귀 없음(charge 형태가 base와 다르지 않은지 검사)

## 영향 범위

- [PixelArtFactory.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/PixelArtFactory.cs) 절차 그림(placeholder로만 남김)
- 인식 레이어가 룬을 어떻게 판정하는지에는 영향 없음(표현 매핑만 변경)

## 자산 소싱

자체 제작 권장. 룬 디자인은 게임 정체성이라 외부 팩에 잘 맞는 게 거의 없다. Aseprite로 family당 0.5일 작업 기준 5 family = 2~3일.

## 비고

본 Phase는 family 단위로 분할 PR 가능. 첫 PR에 Fire/Water 2종, 다음 PR에 Wind/Earth/Life처럼.

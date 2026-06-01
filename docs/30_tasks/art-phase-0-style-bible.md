---
id: art-phase-0-style-bible
status: in_review
depends_on: []
blocks:
  - art-phase-1-urp-2dlight
  - art-phase-2-player-png-anim
  - art-phase-3-autotile
  - art-phase-4-prop-variants
  - art-phase-5-rune-handdrawn
  - art-phase-6-postfx
---

# Phase 0 — Art Style Bible Lock

## 목적

자산 제작 전에 해상도·팔레트·시점·라이팅 방향·외곽선 규칙을 한 문서에 명시하고 lock한다. 이후 모든 Art Phase PR은 이 문서를 기준으로 리뷰한다.

## 산출물

- [docs/ART_STYLE_BIBLE.md](../ART_STYLE_BIBLE.md)

## 작업

- [x] 시점(Top-down 3/4 oblique 45°) 결정
- [x] 캐릭터 해상도(48×64), 타일 해상도(32×32), PPU(32) 결정
- [x] 팔레트(AAP-64) 결정
- [x] 라이팅 방향(좌상단 → 우하단) 결정
- [x] 외곽선 규칙(검정 단색 금지, 색별 다크 셰이드) 결정
- [x] 룬 가독성 룰(40% 축소 시 family 구분 가능) 결정
- [x] 애니메이션 프레임 규약 결정
- [x] 파일명 컨벤션 결정

## 합의 필요 항목

- 캐릭터 해상도 48×64 동의 여부
- 팔레트 AAP-64 동의 여부(대안: Endesga 32, DB-32, 자체 24색)
- 시점 Top-down 3/4 oblique 동의 여부

## 검증

- 팀 3인 리뷰 승인
- 기존 자산(절차 sprite) 가운데 본 규칙과 어긋나는 항목 식별 → Phase 5 룬 손그림 작업 우선순위 산정 자료로 활용

## 영향 범위

- 문서만. 코드·자산 변경 없음.

## 비고

스타일을 자주 바꾸면 자산이 누적되지 않는다. 본 문서 변경은 별도 PR + 팀 리뷰 필수.

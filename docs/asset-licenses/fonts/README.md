# Font Asset Notes

추가일: 2026-06-13

## Galmuri (SIL Open Font License 1.1)

| 파일 | 원본 | 작가 | 출처 | 라이선스 |
| --- | --- | --- | --- | --- |
| `Resources/Fonts/Galmuri11.ttf` | Galmuri11 v2.40.3 | Lee Minseo (quiple) | <https://github.com/quiple/galmuri> | SIL OFL 1.1 |
| `Resources/Fonts/Galmuri11-Bold.ttf` | Galmuri11 Bold | Lee Minseo (quiple) | 〃 | SIL OFL 1.1 |
| `Resources/Fonts/Galmuri14.ttf` | Galmuri14 | Lee Minseo (quiple) | 〃 | SIL OFL 1.1 |

- 선정 사유: 게임 UI 전반의 한글 텍스트를 OS 폰트(Malgun Gothic) 의존 없이 모든 머신에서 동일하게 픽셀 단위로 또렷하게 렌더링하기 위함. 픽셀 아트 톤과도 맞는 비트맵 스타일.
- 로더: `ExamGameController.LoadGameFont`가 `Resources/Fonts/Galmuri11`을 우선 로드하고, 없으면 Malgun Gothic/Arial OS 폰트로 fallback.
- OFL 전문은 `Galmuri-LICENSE.txt` 참조. OFL은 폰트 번들·임베드를 허용하며, 폰트 파일 자체를 판매하지 않는 한 게임에 포함 가능.

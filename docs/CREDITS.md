# Credits

`Magic Exam Hall`이 외부 자산을 사용할 때 이 파일에 출처를 남깁니다. 자산을 추가하기 전에 반드시 `docs/SPRITE_GUIDE.md`의 라이선스 규칙을 확인합니다.

원본 LICENSE, README, attribution 파일은 `docs/asset-licenses/` 아래에 보관합니다. Unity `Resources/` 아래에는 런타임에 불러올 실제 PNG만 둡니다.

## 비주얼

| 자산 | 작가 | 출처 | 라이선스 | 사용 위치 |
| --- | --- | --- | --- | --- |
| 기본 32×32 도트 sprite pack | 프로젝트 내부 제작 | `Assets/MagicExamHall/Resources/Sprites` | 프로젝트 라이선스 | 시험장 배경, 설치물, 룬, 원소 문양 |
| Zelda-like tilesets and sprites (캐릭터 시트) | ArMM1998 | <https://opengameart.org/content/zelda-like-tilesets-and-sprites> | CC0 | 플레이어 4방향 idle/walk/cast 프레임, 멘토 NPC 15종 (`scripts/import-character-pack.py`로 슬라이스·리컬러) |
| Procedural fallback sprite | 프로젝트 내부 제작 | `PixelArtFactory.cs` | 프로젝트 라이선스 | PNG 누락 시 fallback |
| 고문서 UI fallback pack | 프로젝트 내부 제작 | `MagicExamUiTheme.cs` | 프로젝트 라이선스 | 타이틀 로고, 책/두루마리 패널, 버튼, 노트, 체크박스, 슬라이더 |
| `TitleLogo.png` | 프로젝트 내부 제작 | `Resources/Sprites/UI/TitleLogo.png` | 프로젝트 라이선스 | 타이틀 화면 마법탑 룬 문장 |

## 사운드

| 자산 | 작가 | 출처 | 라이선스 | 사용 위치 |
| --- | --- | --- | --- | --- |
| 절차 생성 SFX/BGM | 프로젝트 내부 제작 | `AudioDirector.cs` | 프로젝트 라이선스 | 시전 성공/실패, 목표 완료, 층 완료, 노트, NPC, hazard, 클라이맥스 BGM |
| Loopable Dungeon Ambience (`ambient_tower.ogg`) | JaggedStone | <https://opengameart.org/content/loopable-dungeon-ambience> | CC0 | 1~4층 탑 ambient BGM (`Resources/Bgm/ambient_tower.ogg`) |

## 폰트

| 자산 | 작가 | 출처 | 라이선스 | 사용 위치 |
| --- | --- | --- | --- | --- |
| Galmuri11 / Galmuri14 (한글 비트맵 폰트) | Lee Minseo (quiple) | <https://github.com/quiple/galmuri> | SIL Open Font License 1.1 | HUD, 룬 라벨, 노트, 멘토 대사 등 모든 UI 텍스트 (`Resources/Fonts/`) |
| Unity 기본 Arial | — | Unity 번들 | — | Galmuri 로드 실패 시 fallback |

## 자산 추가 시 작성 규칙

- **자산** 이름은 PNG 파일명 또는 자산 팩 이름.
- **작가** 이름과 가능하면 핸들 함께. 익명 자산은 "Anonymous".
- **출처**는 다운로드 페이지 URL. 팩으로 받은 경우 팩의 메인 페이지.
- **라이선스**는 CC0, CC-BY 4.0 등 정확히. 출처 페이지에서 확인.
- **사용 위치**는 어떤 `PixelSpriteKind` 또는 어떤 화면에 쓰이는지 짧게.

CC-BY 자산은 작가 표기가 의무이므로 이 표 외에 게임 내 옵션 화면 또는 엔딩 리포트 푸터에도 짧게 노출하는 것을 권장합니다.

## 도구

| 도구 | 용도 |
| --- | --- |
| Unity 6000.3.14f1 | 게임 엔진 |
| TypeScript 6.x + Vite 8.x | Web prototype |
| Vitest | Web 테스트 |
| Aseprite (권장) | sprite 작업 |

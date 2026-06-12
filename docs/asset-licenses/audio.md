# Audio Asset Notes

작성 기준일: 2026-06-10 (외부 BGM 추가: 2026-06-12)

런타임 SFX와 `climax_seal` BGM은 [AudioDirector.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/AudioDirector.cs)에서 절차적으로 생성된다. `ambient_tower` BGM은 외부 CC0 음원을 사용한다 (아래 참조).

## External BGM

| 파일 | 원제 | 작가 | 출처 | 라이선스 |
| --- | --- | --- | --- | --- |
| `Assets/MagicExamHall/Resources/Bgm/ambient_tower.ogg` | Loopable Dungeon Ambience (`dungeon_ambient_1.ogg`) | JaggedStone | <https://opengameart.org/content/loopable-dungeon-ambience> | CC0 (Public Domain) |

- 선정 사유: 저주파 바람 + 물방울 소리의 끊김 없는 루프. 탑 내부 ambient에 부합하고 CC0라 표기 의무가 없다.
- 로더: `AudioDirector.ExternalBgm`이 `Resources/Bgm/ambient_tower`를 우선 로드하고, 없으면 절차 생성 패드로 fallback.
- 청취 후 교체용 대안 후보 (미사용, 저장소에 미포함):
  - Dungeon Ambience (`dungeon002.ogg`) — yd, CC0, <https://opengameart.org/content/dungeon-ambience> (로컬 보관: `outputs/bgm-candidates/`)
  - Dark Ambient Loop 13 — Lucas Calvo (MundoSound), CC-BY 3.0 (표기 의무 발생), <https://opengameart.org/content/dark-ambient-loop-13>

## Procedural Cues

| Cue | 생성 방식 | 라이선스 |
| --- | --- | --- |
| `cast_base_success` | sine tone sweep | 프로젝트 라이선스 |
| `cast_overlay_success` | triangle tone sweep | 프로젝트 라이선스 |
| `cast_final_effect` | layered sine sweep | 프로젝트 라이선스 |
| `cast_invalid` | square low tone | 프로젝트 라이선스 |
| `cast_incomplete` | triangle descending tone | 프로젝트 라이선스 |
| `cast_dependency_missing` | locked low square tone | 프로젝트 라이선스 |
| `goal_satisfied` | rising sine chime | 프로젝트 라이선스 |
| `floor_complete` | long rising stinger | 프로젝트 라이선스 |
| `note_unlock` | short triangle chime | 프로젝트 라이선스 |
| `hazard_reset` | deterministic noise burst | 프로젝트 라이선스 |
| `npc_appear` | soft sine glow | 프로젝트 라이선스 |
| `ambient_tower` | looped sine drone | 프로젝트 라이선스 |
| `climax_seal` | looped brighter sine drone | 프로젝트 라이선스 |

외부 사운드 팩으로 교체할 경우, 원본 LICENSE/README 파일을 이 폴더에 추가하고 `docs/CREDITS.md`를 함께 갱신한다.

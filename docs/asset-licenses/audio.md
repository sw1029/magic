# Audio Asset Notes

작성 기준일: 2026-06-10

현재 `Magic Exam Hall`의 최종화 브랜치는 외부 오디오 파일을 포함하지 않는다. 런타임 사운드는 [AudioDirector.cs](../../unity/MagicExamHall/Assets/MagicExamHall/Scripts/Runtime/AudioDirector.cs)에서 절차적으로 생성된다.

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

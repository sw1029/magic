# Magic Exam Hall

Unity 6.3 LTS 2D top-down JRPG/indie pixel game implementation for the Magic Recognizer term project.

## How to Run

1. Open `unity/MagicExamHall` in Unity 6.3 LTS.
2. Open `Assets/Scenes/MagicExamHall.unity`.
3. Press Play.

The scene contains a top-down exam tower room, title/menu flow, options, pause, codex, world-space casting input, HUD, magic note toast, generated pixel sprites, procedural audio, save slots, and a five-floor progression loop. If the scene needs to be regenerated, use Unity's menu:

```text
Magic Exam Hall/Rebuild Demo Scene
```

To create a Windows player from the editor, use:

```text
Magic Exam Hall/Build Windows Player
```

The default output is `unity/MagicExamHall/Builds/MagicExamHall.exe`.

The command-line build wrapper used for release verification is:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-windows.ps1 -BuildPath 'tmp\MagicExamHallFinalize\MagicExamHall.exe' -LogPath 'unity\MagicExamHall\unity-build-finalize.log'
```

## Controls

- Move: WASD or arrow keys
- Draw spell: hold right mouse button on the map floor
- Cast: release right mouse button
- Multi-stroke input: start the next stroke within about 1 second
- Pause: Escape
- Codex: Tab or the top-right note button

Options include BGM/SFX volume, mouse sensitivity, left/right mouse swap, movement preset, text scale, color assist, and observer mode.

There is no default drawing panel, cast button, or station modal in the playable flow.

## Game Flow

The playable flow starts at a title screen, lets the player choose a save slot, enters floor 1, climbs through all five floors, and finishes with an ending report before returning to title.

1. Floor 1, Departure: experiment with the five base families.
2. Floor 2, Reaction: discover all six overlay operators.
3. Floor 3, Flow: connect broken bridge states with base + overlay combinations.
4. Floor 4, Fracture: avoid hazards and stabilize unstable rune states.
5. Floor 5, Constellation Heart: restore the final admission circle.

Progress autosaves on new game and floor checkpoints. Three save slots are available from the main menu, and the codex includes a manual save button.

## Spell Runtime

- Base families: fire, water, wind, earth, life
- Overlay operators: steel_brace, electric_fork, ice_bar, soul_dot, void_cut, martial_axis
- `martial_axis` requires `void_cut` to already be attached to the same seal.
- Overlay and combo goals only react when the attached seal or overlay stroke is near the target object.
- Failed recognition creates a weak ripple and a short magic-note hint instead of health loss or death.
- Successful strokes fade from white into family/operator colors; invalid strokes dissolve without reward color.
- First-floor silence can trigger a mentor hint and a short ghost gesture.
- The codex tracks dialogue, floor notes, discovered base families, and discovered overlay operators.

## Logs

Runtime logs are saved under:

```text
Application.persistentDataPath/MagicExamHallLogs/<sessionId>/
```

Each session writes:

- `attempts.jsonl`
- `attempts.csv`
- `survey.jsonl`
- `survey.csv`

Attempt logs include spell phase, base family, overlay stack, seal id, floor id, target object, world effect, world position, recognition quality, and assist state.

## Verification

EditMode tests cover base recognition, overlay recognition, `martial_axis` dependency, quality vectors, hint escalation, procedural audio, accessibility settings, and log schema. PlayMode smoke tests load the world-casting scene, exercise boot/menu/save/codex flows, create a base seal, attach overlays, advance floors, reset on a hazard, complete a full synthetic playthrough, and show the ending report.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.com' -batchmode -quit -projectPath 'C:\Users\silve\source\repos\magic\unity\MagicExamHall' -executeMethod MagicExamHall.Editor.MagicExamHallSceneBuilder.BuildAll
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-windows.ps1 -BuildPath 'tmp\MagicExamHallFinalize\MagicExamHall.exe' -LogPath 'unity\MagicExamHall\unity-build-finalize.log'
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke-magic-exam-hall-player.ps1 -BuildPath 'tmp\MagicExamHallFinalize\MagicExamHall.exe' -LogPath 'tmp\MagicExamHallFinalize\player-smoke.log'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.com' -batchmode -projectPath 'C:\Users\silve\source\repos\magic\unity\MagicExamHall' -runTests -testPlatform editmode -testResults 'C:\Users\silve\source\repos\magic\unity\MagicExamHall\TestResults.xml'
& 'C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.com' -batchmode -projectPath 'C:\Users\silve\source\repos\magic\unity\MagicExamHall' -runTests -testPlatform playmode -testResults 'C:\Users\silve\source\repos\magic\unity\MagicExamHall\PlayModeTestResults.xml'
```

The player smoke script starts the generated Windows build headlessly, waits for the scene startup log markers, and fails if the player exits early or logs a fatal startup pattern. Manual release QA should still confirm WASD movement and right-mouse world drawing in the visible player.

## Troubleshooting

If batchmode exits before compile/test output and the log contains `No valid Unity Editor license found`, activate the Unity Editor license first and rerun the command.

Messages like `abort_threads: Failed aborting id ... mono_thread_manage will ignore it` can appear during Unity/Mono shutdown. Treat them as secondary unless the same log also contains a real compile, exception, or test failure such as `CS#### error`, `Exception`, or `Test run failed`.

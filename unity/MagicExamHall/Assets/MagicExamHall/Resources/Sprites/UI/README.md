# Magic Exam UI Sprites

Optional pixel-art overrides for `MagicExamUiSprites`.

Place PNG files here with these names:

- `TitleLogo.png`
- `BookPanel.png`
- `ScrollPanel.png`
- `DarkPanel.png`
- `ButtonPanel.png`
- `RuneCursor.png`
- `NoteIcon.png`
- `Checkbox.png`
- `SliderTrack.png`

The runtime generates matching fallback sprites when an override is absent. Imported
panel sprites should use point filtering, no compression, and sliced borders.

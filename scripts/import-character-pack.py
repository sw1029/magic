#!/usr/bin/env python3
"""'Zelda-like tilesets and sprites' (ArMM1998, CC0) 캐릭터 시트를
Magic Exam Hall의 드롭인 규약 프레임으로 슬라이스·리컬러한다.

원본: https://opengameart.org/content/zelda-like-tilesets-and-sprites (gfx_3.zip)
시트 배치: character.png 16x32 셀, 행 0=down 1=right 2=up 3=left,
  열 0-3 = 걷기 4프레임, x=80~127 = 팔 들어올리기 3프레임(시전용).

산출:
  Resources/Sprites/Player/  — idle/walk 방향별 + cast 공용 (PlayerSpriteLibrary 규약, PPU32)
  Resources/Sprites/<Kind>   — 멘토 15종 (PixelSpriteKind 이름, 무드별 포즈 + 로브색, PPU16)

플레이어는 PlayerSpriteLibrary가 PPU32로, 멘토는 PixelArtFactory가 PPU16으로 로드하므로
같은 1x2 월드 크기를 내려면 플레이어만 2배 업스케일한다.

사용: python scripts/import-character-pack.py <character.png 경로>
"""

import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parent.parent
SPRITES = REPO / "unity/MagicExamHall/Assets/MagicExamHall/Resources/Sprites"
PLAYER_OUT = SPRITES / "Player"

CELL_W, CELL_H = 16, 32
PLAYER_SCALE = 2  # 16x32 -> 32x64, PPU32 기준 1x2 유닛
MENTOR_SCALE = 1  # 16x32 그대로, PPU16 기준 1x2 유닛
ROW_BY_FACING = {"down": 0, "right": 1, "up": 2, "left": 3}
CAST_CELLS_X = (80, 96, 112)  # 팔 들어올리기 3프레임 (down 행)

# 플레이어 로브(파랑 계열) — ExamGameController의 기존 fallback robe 색과 동일 계열
PLAYER_ROBE = (78, 148, 235)

# MentorProfile.ForFloor의 로브 색 (0-1 -> 0-255)
MENTOR_ROBES = {
    "Mentor": (133, 82, 219),          # 1층 발착층 조교 (보라)
    "MentorScholar": (46, 158, 184),   # 2층 벽화 연구원 (청록)
    "MentorGuide": (107, 179, 92),     # 3층 다리 안내원 (초록)
    "MentorWatcher": (179, 61, 71),    # 4층 균열 감시자 (적색)
    "MentorArchivist": (64, 92, 199),  # 5층 성좌 기록관 (남색)
}


def is_shirt(pixel) -> bool:
    r, g, b, a = pixel
    return a > 0 and r > g + 28 and r > b + 28


def recolor(frame: Image.Image, robe) -> Image.Image:
    """셔츠(붉은 계열)만 로브 색으로 바꾸고 명암은 원본 비율을 유지한다."""
    out = frame.copy()
    px = out.load()
    rr, rg, rb = robe
    for y in range(out.height):
        for x in range(out.width):
            p = px[x, y]
            if is_shirt(p):
                lum = max(p[0], 1) / 200.0  # 원본 빨강 밝기 -> 음영 비율
                lum = min(lum, 1.25)
                px[x, y] = (
                    min(int(rr * lum), 255),
                    min(int(rg * lum), 255),
                    min(int(rb * lum), 255),
                    p[3],
                )
    return out


def cell(sheet: Image.Image, x: int, row: int) -> Image.Image:
    return sheet.crop((x, row * CELL_H, x + CELL_W, (row + 1) * CELL_H))


def save(frame: Image.Image, path: Path, scale: int) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    frame.resize((frame.width * scale, frame.height * scale), Image.NEAREST).save(path)


def export_player(sheet: Image.Image) -> int:
    count = 0
    for facing, row in ROW_BY_FACING.items():
        walk = [recolor(cell(sheet, i * CELL_W, row), PLAYER_ROBE) for i in range(4)]
        for i, frame in enumerate(walk):
            save(frame, PLAYER_OUT / f"walk_{facing}_{i}.png", PLAYER_SCALE)
            count += 1
        for i, frame in enumerate((walk[0], walk[2])):
            save(frame, PLAYER_OUT / f"idle_{facing}_{i}.png", PLAYER_SCALE)
            count += 1

    down = ROW_BY_FACING["down"]
    cast = [recolor(cell(sheet, x, down), PLAYER_ROBE) for x in CAST_CELLS_X]
    for i, frame in enumerate(cast):
        save(frame, PLAYER_OUT / f"cast_charge_{i}.png", PLAYER_SCALE)
        count += 1
    save(cast[2], PLAYER_OUT / "cast_release_0.png", PLAYER_SCALE)
    save(recolor(cell(sheet, 0, down), PLAYER_ROBE), PLAYER_OUT / "cast_release_1.png", PLAYER_SCALE)
    return count + 2


def export_mentors(sheet: Image.Image) -> int:
    """무드별 포즈: Neutral=정면 서기, Happy=팔 들어올리기, Frown=옆으로 돌아섬."""
    down, left = ROW_BY_FACING["down"], ROW_BY_FACING["left"]
    poses = {
        "Neutral": cell(sheet, 0, down),
        "Happy": cell(sheet, CAST_CELLS_X[2], down),
        "Frown": cell(sheet, 0, left),
    }
    count = 0
    for prefix, robe in MENTOR_ROBES.items():
        for mood, frame in poses.items():
            save(recolor(frame, robe), SPRITES / f"{prefix}{mood}.png", MENTOR_SCALE)
            count += 1
    return count


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 1

    sheet = Image.open(sys.argv[1]).convert("RGBA")
    player = export_player(sheet)
    mentors = export_mentors(sheet)
    print(f"player frames: {player}, mentor overrides: {mentors}")
    print(f"-> {PLAYER_OUT}")
    print(f"-> {SPRITES} (mentors)")
    return 0


if __name__ == "__main__":
    sys.exit(main())

#!/usr/bin/env python3
"""Magic Exam Hall 타이틀 화면용 픽셀아트 생성.

- TitleTower.png : 떠 있는 마법탑 (돌탑 몸통 + 원뿔 지붕 + 빛나는 창 + 부유 암반).
- TitleSky.png   : 밤하늘 그라데이션 + 별 + 달 배경 (1280x720).

도형 나열이 아니라 음영/외곽선/디더링으로 입체감을 준 핸드드로잉 픽셀아트.
출력: Resources/Sprites/ (UI Image가 런타임에 Texture2D로 로드).
"""

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

REPO = Path(__file__).resolve().parent.parent
OUT = REPO / "unity/MagicExamHall/Assets/MagicExamHall/Resources/Sprites/UI"
PREVIEW = REPO / "outputs"


def lerp(a, b, t):
    t = max(0.0, min(1.0, t))
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


# ---------------------------------------------------------------- tower
def make_tower(scale=4):
    W, H = 150, 300
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    cx = W // 2
    # palette
    stone = (108, 112, 126)
    stone_hi = (140, 146, 162)
    stone_lo = (74, 78, 92)
    mortar = (56, 58, 70)
    outline = (28, 26, 40)
    roof = (74, 52, 110)
    roof_hi = (104, 80, 150)
    roof_lo = (50, 34, 78)
    glow = (255, 214, 132)
    glow_core = (255, 240, 200)
    rock = (60, 50, 46)
    rock_hi = (84, 70, 60)
    rock_lo = (40, 32, 30)

    # ---- body (slightly tapered) ----
    body_top, body_bot = 96, 250
    top_half, bot_half = 36, 46

    def half_at(y):
        t = (y - body_top) / (body_bot - body_top)
        return top_half + (bot_half - top_half) * t

    for y in range(body_top, body_bot):
        hw = half_at(y)
        # base shade with left-light / right-shadow gradient across the width
        for x in range(int(cx - hw), int(cx + hw) + 1):
            tx = max(0.0, (x - (cx - hw)) / (2 * hw))
            col = lerp(stone_hi, stone_lo, tx ** 1.1)
            img.putpixel((x, y), col + (255,))

    # stone courses (brick rows with offset joints)
    course_h = 18
    row = 0
    for y in range(body_top + 2, body_bot, course_h):
        hw = half_at(y)
        d.line([(cx - hw, y), (cx + hw, y)], fill=mortar + (255,), width=1)
        offset = (course_h // 2) if row % 2 else 0
        bx = int(cx - hw) + offset
        while bx < cx + hw:
            yy = min(y + course_h, body_bot)
            d.line([(bx, y), (bx, yy)], fill=mortar + (200,), width=1)
            bx += course_h
        row += 1

    # body outline
    d.line([(cx - top_half, body_top), (cx - bot_half, body_bot)], fill=outline + (255,), width=2)
    d.line([(cx + top_half, body_top), (cx + bot_half, body_bot)], fill=outline + (255,), width=2)

    # ---- glowing arched windows (3 tiers) ----
    def window(yc, w=11, h=18):
        x0, x1 = cx - w // 2, cx + w // 2
        # soft halo
        halo = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        hd = ImageDraw.Draw(halo)
        hd.ellipse([x0 - 9, yc - 14, x1 + 9, yc + h + 8], fill=glow + (70,))
        halo = halo.filter(ImageFilter.GaussianBlur(4))
        img.alpha_composite(halo)
        # frame
        d.rectangle([x0 - 2, yc - 2, x1 + 2, yc + h + 2], fill=outline + (255,))
        # arch glow
        d.rectangle([x0, yc + 4, x1, yc + h], fill=glow + (255,))
        d.pieslice([x0, yc - (x1 - x0) // 2, x1, yc + (x1 - x0)], 180, 360, fill=glow + (255,))
        d.rectangle([x0 + 2, yc + 7, x1 - 2, yc + h - 3], fill=glow_core + (255,))

    window(120)
    window(168)
    window(212, w=13, h=20)

    # ---- battlement ring under the roof ----
    ring_y = body_top
    d.rectangle([cx - top_half - 4, ring_y - 6, cx + top_half + 4, ring_y + 2], fill=stone + (255,))
    d.rectangle([cx - top_half - 4, ring_y - 6, cx + top_half + 4, ring_y - 6], fill=stone_hi + (255,))
    d.line([(cx - top_half - 4, ring_y + 2), (cx + top_half + 4, ring_y + 2)], fill=outline + (255,), width=2)

    # ---- conical roof ----
    roof_apex = 18
    roof_base_y = ring_y - 6
    roof_half = top_half + 8
    # shaded cone: draw vertical slices
    for x in range(cx - roof_half, cx + roof_half + 1):
        t = (x - (cx - roof_half)) / (2 * roof_half)
        # height of cone surface at this x
        edge_t = abs(x - cx) / roof_half
        ytop = roof_apex + (roof_base_y - roof_apex) * edge_t
        col = lerp(roof_hi, roof_lo, max(0.0, t) ** 1.2)
        d.line([(x, ytop), (x, roof_base_y)], fill=col + (255,))
    # roof outline
    d.line([(cx - roof_half, roof_base_y), (cx, roof_apex)], fill=outline + (255,), width=2)
    d.line([(cx + roof_half, roof_base_y), (cx, roof_apex)], fill=outline + (255,), width=2)
    d.line([(cx - roof_half, roof_base_y), (cx + roof_half, roof_base_y)], fill=outline + (255,), width=2)
    # roof shingle hints
    for ry in range(int(roof_apex) + 14, int(roof_base_y), 12):
        et = (ry - roof_apex) / (roof_base_y - roof_apex)
        hw = roof_half * et
        d.line([(cx - hw + 2, ry), (cx + hw - 2, ry)], fill=roof_lo + (160,), width=1)

    # ---- spire + glowing orb ----
    d.line([(cx, roof_apex), (cx, roof_apex - 14)], fill=outline + (255,), width=2)
    orb_y = roof_apex - 18
    halo = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(halo)
    hd.ellipse([cx - 18, orb_y - 18, cx + 18, orb_y + 18], fill=glow + (120,))
    halo = halo.filter(ImageFilter.GaussianBlur(5))
    img.alpha_composite(halo)
    d.ellipse([cx - 6, orb_y - 6, cx + 6, orb_y + 6], fill=glow + (255,))
    d.ellipse([cx - 3, orb_y - 4, cx + 2, orb_y + 1], fill=glow_core + (255,))

    # ---- floating rock base ----
    rb_y = body_bot
    pts = [(cx - bot_half - 2, rb_y - 2), (cx + bot_half + 2, rb_y - 2),
           (cx + bot_half - 6, rb_y + 18), (cx + 14, rb_y + 30),
           (cx - 6, rb_y + 40), (cx - 26, rb_y + 26), (cx - bot_half + 2, rb_y + 16)]
    d.polygon(pts, fill=rock + (255,))
    # rock top light / bottom shadow
    d.line([(cx - bot_half - 2, rb_y - 2), (cx + bot_half + 2, rb_y - 2)], fill=rock_hi + (255,), width=2)
    for (px, py) in [(cx - 18, rb_y + 12), (cx + 10, rb_y + 16), (cx - 2, rb_y + 26)]:
        d.line([(px, py), (px - 3, py + 8)], fill=rock_lo + (255,), width=1)
    d.polygon(pts, outline=outline + (255,))
    # a couple of drifting pebbles below
    for (px, py, s) in [(cx - 30, rb_y + 44, 3), (cx + 22, rb_y + 38, 2), (cx + 4, rb_y + 52, 2)]:
        d.ellipse([px - s, py - s, px + s, py + s], fill=rock + (255,), outline=outline + (255,))

    # base magical underglow
    halo = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(halo)
    hd.ellipse([cx - 40, rb_y + 8, cx + 40, rb_y + 48], fill=(120, 150, 255, 90))
    halo = halo.filter(ImageFilter.GaussianBlur(7))
    base = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    base.alpha_composite(halo)
    base.alpha_composite(img)
    img = base

    big = img.resize((W * scale, H * scale), Image.NEAREST)
    return big


# ---------------------------------------------------------------- sky
def make_sky():
    W, H = 1280, 720
    img = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    top = (18, 16, 38)
    bot = (6, 6, 14)
    for y in range(H):
        t = y / H
        img.paste(lerp(top, bot, t) + (255,), [0, y, W, y + 1])
    d = ImageDraw.Draw(img)

    rng = random.Random(7)
    # stars
    for _ in range(220):
        x = rng.randint(0, W - 1)
        y = rng.randint(0, int(H * 0.78))
        b = rng.randint(90, 230)
        s = rng.choice([1, 1, 1, 2])
        d.ellipse([x, y, x + s, y + s], fill=(b, b, min(255, b + 30), 255))
    # a few brighter stars with glow
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    for _ in range(14):
        x = rng.randint(40, W - 40)
        y = rng.randint(30, int(H * 0.6))
        gd.ellipse([x - 5, y - 5, x + 5, y + 5], fill=(200, 215, 255, 120))
    glow = glow.filter(ImageFilter.GaussianBlur(4))
    img.alpha_composite(glow)

    # moon upper-right with soft halo
    mx, my, mr = 1050, 150, 70
    halo = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(halo)
    hd.ellipse([mx - mr - 40, my - mr - 40, mx + mr + 40, my + mr + 40], fill=(180, 195, 240, 70))
    halo = halo.filter(ImageFilter.GaussianBlur(18))
    img.alpha_composite(halo)
    d.ellipse([mx - mr, my - mr, mx + mr, my + mr], fill=(222, 228, 244, 255))
    d.ellipse([mx - mr + 18, my - mr + 10, mx + mr + 18, my + mr + 10], fill=(18, 16, 38, 255))  # crescent cut
    # craters
    for (cxp, cyp, cs) in [(mx - 20, my - 10, 8), (mx - 4, my + 18, 5), (mx - 30, my + 14, 4)]:
        d.ellipse([cxp - cs, cyp - cs, cxp + cs, cyp + cs], fill=(200, 206, 224, 255))

    # bottom vignette
    vg = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    vd = ImageDraw.Draw(vg)
    vd.rectangle([0, H - 160, W, H], fill=(0, 0, 0, 120))
    vg = vg.filter(ImageFilter.GaussianBlur(40))
    img.alpha_composite(vg)
    return img


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.mkdir(parents=True, exist_ok=True)
    tower = make_tower()
    sky = make_sky()
    tower.save(OUT / "TitleTower.png")
    sky.save(OUT / "TitleSky.png")
    # previews on a dark bg
    bg = Image.new("RGB", (tower.width + 80, tower.height + 80), (16, 16, 32))
    bg.paste(tower, (40, 40), tower)
    bg.save(PREVIEW / "preview_tower.png")
    sky.convert("RGB").resize((640, 360)).save(PREVIEW / "preview_sky.png")
    print(f"tower {tower.size} -> {OUT/'TitleTower.png'}")
    print(f"sky {sky.size} -> {OUT/'TitleSky.png'}")


if __name__ == "__main__":
    main()

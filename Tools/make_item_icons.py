"""Round 17: the HUD's item icons (simple 2D, no Tripo) -> Assets/Resources/UI/Items/<ItemKind>.png (128 x 128, transparent).
Usage: python Tools/make_item_icons.py"""
import math
import os
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "UI", "Items")
S = 4  # drawn 4x, scaled down (smooth edges)
N = 128 * S

TINT = {
    "WrongForm": (190, 90, 255), "RainbowBridge": (255, 128, 200), "BubbleTrap": (140, 215, 255), "SwapSpot": (255, 215, 64),
    "Mirage": (180, 240, 255), "Rewind": (100, 255, 150), "PocketStorm": (115, 128, 178), "HypeHorn": (255, 150, 40),
    "BubbleShield": (128, 240, 240), "Slick": (215, 140, 255), "DinoEgg": (240, 230, 180), "FireworkRocket": (255, 76, 64),
    "HumanCannonball": (255, 100, 150), "FloodWave": (76, 150, 255), "BigFinish": (255, 205, 50),
}
INK = (30, 24, 40)
WHITE = (255, 255, 255)


def P(*xy):
    return [v * S for v in xy]


def base(tint):
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle(P(4, 4, 124, 124), radius=22 * S, fill=tint + (255,), outline=INK + (255,), width=5 * S)
    hi = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    ImageDraw.Draw(hi).rounded_rectangle(P(12, 12, 116, 52), radius=16 * S, fill=(255, 255, 255, 60))
    im = Image.alpha_composite(im, hi)
    return im, ImageDraw.Draw(im)


def star(d, cx, cy, r1, r2, n, fill):
    pts = []
    for i in range(n * 2):
        a = math.pi / n * i - math.pi / 2
        r = r1 if i % 2 == 0 else r2
        pts.append(((cx + math.cos(a) * r) * S, (cy + math.sin(a) * r) * S))
    d.polygon(pts, fill=fill, outline=INK, width=3 * S)


def draw(name, d):
    w = 4 * S
    if name == "WrongForm":
        d.polygon([P(70, 16)[0:2], P(36, 70)[0:2], P(60, 70)[0:2], P(48, 112)[0:2], P(92, 52)[0:2], P(66, 52)[0:2], P(80, 16)[0:2]], fill=WHITE, outline=INK, width=w)
    elif name == "RainbowBridge":
        cols = [(255, 64, 64), (255, 150, 40), (255, 235, 60), (80, 230, 90), (70, 150, 255), (160, 90, 255)]
        for i, c in enumerate(cols):
            r = 50 - i * 6
            d.arc(P(64 - r, 88 - r, 64 + r, 88 + r), 180, 360, fill=c, width=6 * S)
        d.rectangle(P(12, 92, 116, 100), fill=INK)
    elif name == "BubbleTrap":
        d.ellipse(P(22, 22, 106, 106), fill=(200, 240, 255, 200), outline=INK, width=w)
        d.ellipse(P(36, 34, 58, 52), fill=WHITE)
        d.ellipse(P(50, 60, 78, 88), fill=(80, 80, 110))
    elif name == "SwapSpot":
        d.arc(P(24, 24, 104, 104), 200, 340, fill=WHITE, width=10 * S)
        d.polygon([P(98, 38)[0:2], P(112, 66)[0:2], P(84, 62)[0:2]], fill=WHITE, outline=INK, width=2 * S)
        d.arc(P(24, 24, 104, 104), 20, 160, fill=INK, width=10 * S)
        d.polygon([P(30, 90)[0:2], P(16, 62)[0:2], P(44, 66)[0:2]], fill=INK)
    elif name == "Mirage":
        for i, a in enumerate((90, 150, 255)):
            x = 22 + i * 18
            d.rounded_rectangle(P(x, 40, x + 46, 96), radius=18 * S, fill=(255, 255, 255, a), outline=INK + (a,), width=3 * S)
            d.ellipse(P(x + 12, 56, x + 20, 64), fill=INK + (a,))
            d.ellipse(P(x + 26, 56, x + 34, 64), fill=INK + (a,))
    elif name == "Rewind":
        d.polygon([P(64, 30)[0:2], P(20, 64)[0:2], P(64, 98)[0:2]], fill=WHITE, outline=INK, width=w)
        d.polygon([P(108, 30)[0:2], P(64, 64)[0:2], P(108, 98)[0:2]], fill=WHITE, outline=INK, width=w)
    elif name == "PocketStorm":
        for (x, y, r) in ((44, 54, 20), (68, 46, 24), (88, 58, 18), (60, 64, 20)):
            d.ellipse(P(x - r, y - r, x + r, y + r), fill=(225, 230, 240), outline=INK, width=3 * S)
        for x in (40, 58, 76, 94):
            d.line(P(x, 86, x - 8, 110), fill=(120, 190, 255), width=5 * S)
    elif name == "HypeHorn":
        d.polygon([P(22, 52)[0:2], P(46, 52)[0:2], P(100, 22)[0:2], P(100, 106)[0:2], P(46, 76)[0:2], P(22, 76)[0:2]], fill=WHITE, outline=INK, width=w)
        d.text(P(64, 108)[0:2], "x2", fill=INK)
        for r in (12, 22):
            d.arc(P(100 - r, 64 - r * 2, 100 + r, 64 + r * 2), 300, 60, fill=WHITE, width=3 * S)
    elif name == "BubbleShield":
        d.polygon([P(64, 16)[0:2], P(104, 30)[0:2], P(98, 78)[0:2], P(64, 112)[0:2], P(30, 78)[0:2], P(24, 30)[0:2]], fill=(220, 255, 255), outline=INK, width=w)
        d.ellipse(P(44, 34, 60, 50), fill=WHITE)
    elif name == "Slick":
        d.ellipse(P(16, 70, 112, 108), fill=(160, 80, 220), outline=INK, width=w)
        d.ellipse(P(40, 78, 64, 90), fill=(235, 200, 255))
        d.polygon([P(64, 18)[0:2], P(80, 50)[0:2], P(64, 62)[0:2], P(48, 50)[0:2]], fill=(160, 80, 220), outline=INK, width=3 * S)
    elif name == "DinoEgg":
        d.ellipse(P(32, 14, 96, 114), fill=(250, 245, 220), outline=INK, width=w)
        for (x, y, r) in ((52, 44, 7), (74, 60, 9), (56, 82, 8), (80, 92, 5), (70, 32, 5)):
            d.ellipse(P(x - r, y - r, x + r, y + r), fill=(110, 180, 90))
        d.line(P(36, 70, 50, 62, 60, 72, 72, 60, 84, 70, 94, 64), fill=INK, width=3 * S)
    elif name == "FireworkRocket":
        d.polygon([P(64, 12)[0:2], P(80, 36)[0:2], P(48, 36)[0:2]], fill=(255, 230, 80), outline=INK, width=3 * S)
        d.rectangle(P(48, 36, 80, 92), fill=(230, 50, 50), outline=INK, width=w)
        d.rectangle(P(48, 56, 80, 64), fill=WHITE)
        d.polygon([P(48, 72)[0:2], P(32, 96)[0:2], P(48, 92)[0:2]], fill=WHITE, outline=INK, width=2 * S)
        d.polygon([P(80, 72)[0:2], P(96, 96)[0:2], P(80, 92)[0:2]], fill=WHITE, outline=INK, width=2 * S)
        d.polygon([P(54, 92)[0:2], P(74, 92)[0:2], P(64, 118)[0:2]], fill=(255, 170, 40))
    elif name == "HumanCannonball":
        d.polygon([P(16, 92)[0:2], P(70, 48)[0:2], P(84, 66)[0:2], P(30, 110)[0:2]], fill=(70, 70, 90), outline=INK, width=w)
        d.ellipse(P(20, 92, 44, 116), fill=(150, 90, 50), outline=INK, width=3 * S)
        d.ellipse(P(78, 18, 108, 48), fill=(255, 215, 180), outline=INK, width=3 * S)
        d.chord(P(78, 14, 108, 42), 180, 360, fill=(255, 220, 40), outline=INK, width=3 * S)
        d.ellipse(P(89, 32, 97, 40), fill=(255, 40, 40))
    elif name == "FloodWave":
        d.chord(P(14, 30, 114, 130), 180, 360, fill=(90, 160, 255), outline=INK, width=w)
        d.arc(P(52, 34, 104, 86), 180, 320, fill=WHITE, width=7 * S)
        d.rectangle(P(14, 92, 114, 104), fill=(60, 120, 230))
    elif name == "BigFinish":
        star(d, 64, 62, 46, 20, 5, (255, 240, 120))
        d.text(P(57, 54)[0:2], "B", fill=INK)


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, tint in TINT.items():
        im, d = base(tint)
        draw(name, d)
        im = im.resize((128, 128), Image.LANCZOS)
        im.save(os.path.join(OUT, name + ".png"))
    print(f"{len(TINT)} icons -> {OUT}")


if __name__ == "__main__":
    main()

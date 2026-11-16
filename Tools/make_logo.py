"""ShiftStorm title logo (Round 14 item 1): Assets/Resources/UI/Logo.png, shown on the title screen.
"SHIFT" + a lightning bolt + "STORM" in heavy italic letters, gold over a dark outline and a teal storm streak behind.
A placeholder until real key art exists. Usage: python Tools/make_logo.py"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

root = Path(__file__).resolve().parents[1]
W, H = 2048, 560
font_path = next((p for p in ["C:/Windows/Fonts/ariblk.ttf", "C:/Windows/Fonts/impact.ttf", "C:/Windows/Fonts/arialbd.ttf"] if Path(p).exists()), None)
font = ImageFont.truetype(font_path, 225) if font_path else ImageFont.load_default()

def word(text):
    l, t, r, b = font.getbbox(text)
    return text, r - l, b - t, l, t

layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(layer)
# the storm streak: a teal band with a lighter edge
d.polygon([(90, 330), (W - 60, 210), (W - 120, 330), (150, 450)], fill=(20, 150, 170, 235))
d.polygon([(90, 330), (W - 60, 210), (W - 75, 236), (100, 352)], fill=(120, 230, 235, 255))

text = Image.new("RGBA", (W, H), (0, 0, 0, 0)); td = ImageDraw.Draw(text)
a, aw, ah, al, at = word("SHIFT"); b, bw, bh, bl, bt = word("STORM")
gap = 140; total = aw + gap + bw; x0 = (W - total) // 2; y0 = 70
gold, dark = (255, 205, 52, 255), (40, 22, 6, 255)
for dx in range(-14, 15, 2):
    for dy in range(-14, 15, 2):
        if dx * dx + dy * dy <= 200:
            td.text((x0 - al + dx, y0 - at + dy), a, font=font, fill=dark)
            td.text((x0 + aw + gap - bl + dx, y0 - at + dy), b, font=font, fill=dark)
td.text((x0 - al, y0 - at), a, font=font, fill=gold)
td.text((x0 + aw + gap - bl, y0 - at), b, font=font, fill=gold)
# a lighter top half on the letters (a simple shine)
shine = Image.new("RGBA", (W, H), (0, 0, 0, 0)); sd = ImageDraw.Draw(shine)
sd.rectangle((0, 0, W, y0 + ah * .45), fill=(255, 245, 190, 120))
mask = text.split()[3].point(lambda v: 255 if v > 0 else 0)
gold_mask = Image.new("L", (W, H), 0); ImageDraw.Draw(gold_mask).bitmap((0, 0), mask)
# the bolt between the words
cx = x0 + aw + gap // 2; bolt = [(cx + 40, y0 - 30), (cx - 70, y0 + 160), (cx - 5, y0 + 160), (cx - 60, y0 + 330), (cx + 75, y0 + 120), (cx + 10, y0 + 120), (cx + 80, y0 - 30)]
td.polygon(bolt, fill=(255, 240, 120, 255), outline=dark, width=10)
# shear: italic
text = text.transform(text.size, Image.AFFINE, (1, .22, 0, 0, 1, 0), resample=Image.BICUBIC)
shadow = text.split()[3].filter(ImageFilter.GaussianBlur(14))
sh = Image.new("RGBA", (W, H), (0, 0, 0, 0)); sh.putalpha(shadow.point(lambda v: int(v * .6)))
out = Image.alpha_composite(layer, sh.transform(sh.size, Image.AFFINE, (1, 0, -12, 0, 1, -14)))
out = Image.alpha_composite(out, text)
# the shine, only on the gold
sh2 = shine.transform(shine.size, Image.AFFINE, (1, .22, 0, 0, 1, 0))
alpha = Image.eval(out.split()[3], lambda v: v)
gold_only = Image.new("L", (W, H), 0)
px_out = out.load(); gpx = gold_only.load()
for yy in range(H):
    for xx in range(0, W):
        r, g, bb, aa = px_out[xx, yy]
        if aa > 200 and r > 230 and g > 180 and bb < 140: gpx[xx, yy] = 255
sh2.putalpha(Image.composite(sh2.split()[3], Image.new("L", (W, H), 0), gold_only))
out = Image.alpha_composite(out, sh2)
dst = root / "Assets/Resources/UI"; dst.mkdir(parents=True, exist_ok=True)
out.save(dst / "Logo.png")
print("logo written", dst / "Logo.png", "font", font_path)

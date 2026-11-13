"""ShiftStorm placeholder cover and poster for the launcher and the Steam shortcut, from a player-view picture of a check run.
Usage: python Tools/make_cover.py <picture.png>  ->  SourceArt/Title/ShiftStormCover.png (square) and ShiftStormPoster.png (600 x 900)."""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
root = Path(__file__).resolve().parents[1]; src = Image.open(sys.argv[1]).convert("RGB"); out = root / "SourceArt/Title"

def font(size):
    for f in ("C:/Windows/Fonts/segoeuib.ttf", "C:/Windows/Fonts/arialbd.ttf"):
        try: return ImageFont.truetype(f, size)
        except OSError: pass
    return ImageFont.load_default()

def title(img, size, y):
    d = ImageDraw.Draw(img); f = font(size); text = "SHIFTSTORM"; w = d.textlength(text, font=f)
    x = (img.width - w) / 2
    d.text((x + 4, y + 4), text, font=f, fill=(0, 0, 0)); d.text((x, y), text, font=f, fill=(255, 214, 64))

w, h = src.size
cover = src.crop(((w - h) // 2, 0, (w + h) // 2, h)).resize((720, 720)); title(cover, 84, 40); cover.save(out / "ShiftStormCover.png")
poster = src.crop(((w - int(h * 2 / 3)) // 2, 0, (w + int(h * 2 / 3)) // 2, h)).resize((600, 900)); title(poster, 70, 60); poster.save(out / "ShiftStormPoster.png")
print("cover and poster written")

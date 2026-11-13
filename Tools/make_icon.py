"""ShiftStorm placeholder icon (SourceArt/Title/GameIcon.png + Launcher/ShiftStorm.ico): a storm-teal disc with a lightning bolt.
Round 1 placeholder until real key art exists. Usage: python Tools/make_icon.py"""
from pathlib import Path
from PIL import Image, ImageDraw
root = Path(__file__).resolve().parents[1]
S = 512
img = Image.new("RGBA", (S, S), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
d.ellipse((16, 16, S - 16, S - 16), fill=(14, 92, 120, 255), outline=(250, 200, 60, 255), width=22)
d.ellipse((70, 300, S - 70, S - 40), fill=(28, 170, 190, 255))           # the sea
bolt = [(300, 60), (170, 280), (255, 280), (200, 455), (350, 220), (262, 220), (330, 60)]
d.polygon(bolt, fill=(255, 214, 64, 255), outline=(120, 70, 0, 255))
out = root / "SourceArt/Title"; out.mkdir(parents=True, exist_ok=True)
img.save(out / "GameIcon.png")
img.save(root / "Launcher/ShiftStorm.ico", sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])
print("icon written")

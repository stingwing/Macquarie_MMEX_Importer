# Generates the MoneyManagerExMAQ app icon: an MMEX-style coin (blue instead of MMEX's green)
# with an orange "import" arrow badge. Drawn at 1024px and downsampled per icon size.
#
# Usage: python make_icon.py <out.ico> [preview.png [path/to/mmex.png]]
#   preview.png  optional side-by-side with MMEX's own icon; skipped if MMEX's icon isn't found
#                (default: newest C:/MoneyManager/mmex-*/res/mmex.png). Requires Pillow.
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import glob
import os
import sys

if len(sys.argv) < 2:
    sys.exit(__doc__ or "Usage: python make_icon.py <out.ico> [preview.png [path/to/mmex.png]]")

N = 1024
def lerp(a, b, t): return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))

def coin(size):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    # Radial-ish vertical gradient like MMEX's glossy coin.
    top, bottom = (96, 165, 250), (29, 78, 216)
    grad = Image.new("RGBA", (size, size))
    gd = ImageDraw.Draw(grad)
    for y in range(size):
        gd.line([(0, y), (size, y)], fill=lerp(top, bottom, y / size) + (255,))
    mask = Image.new("L", (size, size), 0)
    m = int(size * 0.04)
    ImageDraw.Draw(mask).ellipse([m, m, size - m, size - m], fill=255)
    img.paste(grad, (0, 0), mask)
    d = ImageDraw.Draw(img)
    # Darker rim + soft top highlight.
    d.ellipse([m, m, size - m, size - m], outline=(23, 59, 160, 255), width=int(size * 0.025))
    hl = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ImageDraw.Draw(hl).ellipse([int(size * 0.17), int(size * 0.09), int(size * 0.83), int(size * 0.52)], fill=(255, 255, 255, 60))
    hl = hl.filter(ImageFilter.GaussianBlur(size * 0.02))
    img = Image.alpha_composite(img, Image.composite(hl, Image.new("RGBA", (size, size)), mask))
    # White "$", nudged up-left so the badge doesn't cover it.
    font = ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", int(size * 0.66))
    d = ImageDraw.Draw(img)
    cx, cy = size * 0.47, size * 0.47
    d.text((cx + size * 0.012, cy + size * 0.018), "$", font=font, fill=(15, 40, 120, 110), anchor="mm")
    d.text((cx, cy), "$", font=font, fill=(255, 255, 255, 255), anchor="mm")
    return img

def badge(img):
    size = img.size[0]
    d = ImageDraw.Draw(img)
    r = size * 0.24
    bx, by = size - r - size * 0.03, size - r - size * 0.03
    ring = size * 0.035
    d.ellipse([bx - r - ring, by - r - ring, bx + r + ring, by + r + ring], fill=(255, 255, 255, 255))
    d.ellipse([bx - r, by - r, bx + r, by + r], fill=(234, 88, 12, 255))
    # Down arrow (import).
    w = r * 0.30
    d.rectangle([bx - w / 2, by - r * 0.58, bx + w / 2, by + r * 0.05], fill=(255, 255, 255, 255))
    d.polygon([(bx - r * 0.55, by - r * 0.02), (bx + r * 0.55, by - r * 0.02), (bx, by + r * 0.60)], fill=(255, 255, 255, 255))
    return img

master = badge(coin(N))
sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
frames = [master.resize((s, s), Image.LANCZOS) for s in sizes]
out_ico = sys.argv[1]
frames[-1].save(out_ico, format="ICO", sizes=[(s, s) for s in sizes], append_images=frames[:-1])
print(f"wrote {out_ico}")

preview = sys.argv[2] if len(sys.argv) > 2 else None
candidates = [sys.argv[3]] if len(sys.argv) > 3 else sorted(glob.glob("C:/MoneyManager/mmex-*/res/mmex.png"))
candidates = [c for c in candidates if os.path.isfile(c)]
if preview is None:
    sys.exit(0)
if not candidates:
    print("MMEX icon not found; skipping preview")
    sys.exit(0)

# Preview: MMEX's icon next to ours at taskbar-ish sizes, on light and dark strips.
mmex = Image.open(candidates[-1]).convert("RGBA")
pv = Image.new("RGBA", (560, 200), (243, 243, 243, 255))
ImageDraw.Draw(pv).rectangle([0, 100, 560, 200], fill=(32, 32, 32, 255))
x = 16
for s in (16, 24, 32, 48, 64):
    for row, y0 in ((0, 0), (1, 100)):
        y = y0 + (100 - s) // 2
        pv.alpha_composite(mmex.resize((s, s), Image.LANCZOS), (x, y))
        pv.alpha_composite(master.resize((s, s), Image.LANCZOS), (x + s + 8, y))
    x += 2 * s + 30
pv.alpha_composite(master.resize((96, 96), Image.LANCZOS), (450, 52))
pv.save(preview)
print(f"wrote {preview}")

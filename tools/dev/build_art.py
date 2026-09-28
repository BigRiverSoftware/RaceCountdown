"""Copy the design-system artwork into the app and derive the widget background (docs/design-system).

The icon and backgrounds carry Poppins text, so the app uses the PNG exports rather than the SVGs (MAUI's
resizetizer would render the text in a fallback font). Needs Pillow:

    python -m pip install pillow
    python tools/dev/build_art.py

Delete src/RaceCountdown/obj/<config>/<tfm>/resizetizer before the next build so the new PNGs are picked up.
Exits 1 if widget text would fall below WCAG AA on the generated widget background.
"""
import shutil
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DOCS = ROOT / "docs"
RES = ROOT / "src/RaceCountdown/Resources"

COPIES = {
    DOCS / "icons/bathurst-icon-1024.png": RES / "AppIcon/appiconfg.png",
    DOCS / "backgrounds/bathurst-background-portrait.png": RES / "Images/background_portrait.png",
    DOCS / "backgrounds/bathurst-background-landscape.png": RES / "Images/background_landscape.png",
}

# Widget: the landscape scene cropped to 2:1 from the top (so the road and wall stay in), under a dial-navy
# shade that is heaviest on the left, where the medium widget's text sits.
WIDGET = RES / "Images/widget_background.png"
WIDGET_SIZE = (800, 400)
DIAL_NAVY = (0x14, 0x3A, 0x7A)
SHADE_LEFT, SHADE_RIGHT = 0.92, 0.88

WHITE, DIAL_YELLOW = (255, 255, 255), (0xFF, 0xC2, 0x1F)
# name, colour, large text (>= 24px, or >= 18.66px bold)
WIDGET_TEXT = [
    ("Widget headline and title (white)", WHITE, False),
    ("Widget detail line (dial-yellow)", DIAL_YELLOW, False),
]


def luminance(rgb):
    def channel(c):
        c /= 255
        return c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4
    r, g, b = (channel(c) for c in rgb)
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def contrast(a, b):
    la, lb = sorted((luminance(a), luminance(b)), reverse=True)
    return (la + 0.05) / (lb + 0.05)


def build_widget():
    scene = Image.open(DOCS / "backgrounds/bathurst-background-landscape.png").convert("RGB")
    w, h = scene.size
    crop_h = w * WIDGET_SIZE[1] // WIDGET_SIZE[0]
    scene = scene.crop((0, h - crop_h, w, h)).resize(WIDGET_SIZE, Image.LANCZOS)

    shade = Image.new("RGBA", WIDGET_SIZE, DIAL_NAVY + (0,))
    px = shade.load()
    for x in range(WIDGET_SIZE[0]):
        alpha = SHADE_LEFT + (SHADE_RIGHT - SHADE_LEFT) * x / (WIDGET_SIZE[0] - 1)
        for y in range(WIDGET_SIZE[1]):
            px[x, y] = DIAL_NAVY + (round(alpha * 255),)
    widget = Image.alpha_composite(scene.convert("RGBA"), shade).convert("RGB")
    widget.save(WIDGET, optimize=True)

    pixels = widget.get_flattened_data() if hasattr(widget, "get_flattened_data") else widget.getdata()
    brightest = max(pixels, key=luminance)
    failures = 0
    for name, colour, large in WIDGET_TEXT:
        ratio = contrast(colour, brightest)
        needed = 3.0 if large else 4.5
        ok = ratio >= needed
        failures += not ok
        print(f"{name:40} {ratio:6.2f} {needed:6.1f}  {'ok' if ok else 'FAIL'}")
    return failures


def main():
    for source, target in COPIES.items():
        shutil.copyfile(source, target)
        print(f"copied {source.relative_to(ROOT)} -> {target.relative_to(ROOT)}")
    failures = build_widget()
    print(f"wrote {WIDGET.relative_to(ROOT)}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())

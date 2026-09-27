"""WCAG AA contrast check for the countdown page text over the background art (plan §9, Phase 3).

Takes every colour used in the page background SVGs, puts the page's dark overlay on top, and checks each text style
against the worst (brightest) result. This is conservative: it assumes any text could sit over any part of the art.

    python tools/dev/check_contrast.py

Keep OVERLAY_* and PANEL_* in step with CountdownPage.xaml and TEXT with its label styles. Exits 1 on any failure.
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ART = [ROOT / "src/RaceCountdown/Resources/Images" / name
       for name in ("background_portrait.svg", "background_landscape.svg")]

# Least darkening behind any text in CountdownPage.xaml: overlay (>= 25%) + content card (50%) = 62.7%,
# and the overlay alone reaches 62% under the footer.
OVERLAY_RGB, OVERLAY_ALPHA = (0x14, 0x0A, 0x2A), 0.62
PANEL_RGB, PANEL_ALPHA = (0x00, 0x00, 0x00), 0.35        # CountdownPanel (#59000000)

WHITE, GOLD = (255, 255, 255), (0xFF, 0xC2, 0x1A)
# name, colour, text alpha, large text (>= 24px, or >= 18.66px bold), drawn on a countdown panel
TEXT = [
    ("Title (30 semibold)", WHITE, 1.0, True, False),
    ("Subtitle (14, muted)", WHITE, 0.9, False, False),
    ("Banner RACE DAY (28 semibold, gold)", GOLD, 1.0, True, False),
    ("Countdown digits (48 semibold)", WHITE, 1.0, True, True),
    ("Segment captions (12, gold)", GOLD, 1.0, False, True),
    ("Detail / start time (16-18)", WHITE, 1.0, False, False),
    ("Footer and disclaimer (11-12, muted)", WHITE, 0.9, False, False),
    ("Refresh button (12)", WHITE, 1.0, False, True),
]


def blend(top, alpha, bottom):
    return tuple(t * alpha + b * (1 - alpha) for t, b in zip(top, bottom))


def luminance(rgb):
    def channel(c):
        c /= 255
        return c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4
    r, g, b = (channel(c) for c in rgb)
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def contrast(a, b):
    la, lb = sorted((luminance(a), luminance(b)), reverse=True)
    return (la + 0.05) / (lb + 0.05)


def art_colours():
    colours = set()
    for path in ART:
        for hex_ in re.findall(r'(?:fill|stop-color)="#([0-9A-Fa-f]{6})"', path.read_text(encoding="utf-8")):
            colours.add(tuple(int(hex_[i:i + 2], 16) for i in (0, 2, 4)))
    return colours


def main():
    backgrounds = [blend(OVERLAY_RGB, OVERLAY_ALPHA, c) for c in art_colours()]
    failures = 0
    print(f"{'Text':42} {'worst':>6} {'needs':>6}")
    for name, colour, alpha, large, on_panel in TEXT:
        worst = None
        for bg in backgrounds:
            if on_panel:
                bg = blend(PANEL_RGB, PANEL_ALPHA, bg)
            ratio = contrast(blend(colour, alpha, bg), bg)
            worst = ratio if worst is None else min(worst, ratio)
        needed = 3.0 if large else 4.5
        ok = worst >= needed
        failures += not ok
        print(f"{name:42} {worst:6.2f} {needed:6.1f}  {'ok' if ok else 'FAIL'}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())

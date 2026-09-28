"""WCAG AA contrast check for the countdown page text (docs/design-system).

Every piece of text on CountdownPage.xaml sits on an opaque panel: the dial-navy card, or the deeper navy of the
countdown tiles and Refresh button. The background art never shows through, so each text style is checked
against its panel colour alone. (The widget background is checked by tools/dev/build_art.py.)

    python tools/dev/check_contrast.py

Keep the colours in step with Resources/Styles/Colors.xaml and TEXT with the page's label styles. Exits 1 on any failure.
"""
import sys

DIAL_NAVY = (0x14, 0x3A, 0x7A)          # the card (Panel style)
DIAL_NAVY_DEEP = (0x0B, 0x25, 0x56)     # countdown tiles and the Refresh button

WHITE = (255, 255, 255)
STOPWATCH_ORANGE = (0xFF, 0x5F, 0x14)
DIAL_YELLOW = (0xFF, 0xC2, 0x1F)
# name, colour, text alpha, large text (>= 24px, or >= 18.66px bold), panel
TEXT = [
    ("Wordmark BATHURST (24 bold italic)", WHITE, 1.0, True, DIAL_NAVY),
    ("Wordmark COUNTDOWN (24 bold italic, orange)", STOPWATCH_ORANGE, 1.0, True, DIAL_NAVY),
    ("Title (28 bold)", WHITE, 1.0, True, DIAL_NAVY),
    ("Subtitle, footer, disclaimer (12-13, muted)", WHITE, 0.85, False, DIAL_NAVY),
    ("Banner RACE DAY (28 bold, yellow)", DIAL_YELLOW, 1.0, True, DIAL_NAVY),
    ("Detail / start time (16-18)", WHITE, 1.0, False, DIAL_NAVY),
    ("Countdown digits (32 bold)", WHITE, 1.0, True, DIAL_NAVY_DEEP),
    ("Tile labels DAYS...SEC (14 bold, yellow)", DIAL_YELLOW, 1.0, False, DIAL_NAVY_DEEP),
    ("Refresh button (13 semibold)", WHITE, 1.0, False, DIAL_NAVY_DEEP),
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


def main():
    failures = 0
    print(f"{'Text':46} {'ratio':>6} {'needs':>6}")
    for name, colour, alpha, large, panel in TEXT:
        ratio = contrast(blend(colour, alpha, panel), panel)
        needed = 3.0 if large else 4.5
        ok = ratio >= needed
        failures += not ok
        print(f"{name:46} {ratio:6.2f} {needed:6.1f}  {'ok' if ok else 'FAIL'}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())

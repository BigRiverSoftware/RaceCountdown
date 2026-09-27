# Source artwork

All artwork is original and vector (SVG). No Supercars or Bathurst 1000 logos or marks are used (plan §9).

| File | Used by |
|------|---------|
| `circuit.svg` | Master shape of the stylised Mount Panorama circuit. Its path data is copied into the files below. |
| `../src/RaceCountdown/Resources/AppIcon/appicon.svg` | Icon background: sunset gradient, sun, ridgeline. |
| `../src/RaceCountdown/Resources/AppIcon/appiconfg.svg` | Icon foreground: circuit ribbon + checkered flag, inside Android's 66% safe circle. |
| `../src/RaceCountdown/Resources/Splash/splash.svg` | Splash: circuit outline on `#2B124C`. |
| `../src/RaceCountdown/Resources/Images/background_portrait.svg`, `background_landscape.svg` | Countdown page background, two crops. |
| `../src/RaceCountdown/Resources/Images/widget_background.svg` | Home-screen widget background (Phases 4–5). |

MAUI's resizetizer turns the SVGs into PNGs for every Android density and Windows scale at build time, so there is
no separate export step.

**After editing an icon or image**, delete `src/RaceCountdown/obj/<config>/<tfm>/resizetizer` before building:
resizetizer sometimes keeps the old PNGs. Then check:

- `python tools/dev/check_contrast.py` still passes (it reads the background SVGs).
- The icon at 48 px and inside a circle mask. Opening a small HTML page that stacks `appicon.svg` and
  `appiconfg.svg` in Edge is enough.

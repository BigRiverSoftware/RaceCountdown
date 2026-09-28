# Source artwork

The look of the app comes from the design system in `docs/design-system` (colour, type, spacing and radius tokens
in `tokens.json`, usage rules in `README.md`). The masters are in `docs/icons` and `docs/backgrounds`. All artwork
is original. No Supercars or Bathurst 1000 logos or marks are used (plan §9).

| File | Used by |
|------|---------|
| `docs/icons/bathurst-icon-1024.png` | Copied to `src/RaceCountdown/Resources/AppIcon/appiconfg.png`, the icon foreground. |
| `src/RaceCountdown/Resources/AppIcon/appicon.svg` | Icon background: the icon's dark frame at radius-icon (19.5%). |
| `src/RaceCountdown/Resources/Splash/splash.svg` | Splash: the icon's circuit in white with a chequered start line, on dial-navy. |
| `docs/backgrounds/bathurst-background-*.png` | Copied to `Resources/Images/background_portrait.png` and `background_landscape.png`. |
| `src/RaceCountdown/Resources/Images/widget_background.png` | Home-screen widget background: the landscape scene under a dial-navy shade. |
| `fonts/Poppins-OFL.txt` | Licence for the Poppins fonts in `src/RaceCountdown/Resources/Fonts`. |

The icon and backgrounds carry Poppins text, so the app uses the PNG exports rather than the SVGs (the resizetizer
would draw the text in a fallback font). MAUI's resizetizer scales the PNGs for every Android density and Windows
scale at build time.

**After changing the masters**, run `python tools/dev/build_art.py` (needs Pillow) to copy them in and regenerate
the widget background, then delete `src/RaceCountdown/obj/<config>/<tfm>/resizetizer` before building:
resizetizer sometimes keeps the old PNGs. Then check:

- `python tools/dev/check_contrast.py` still passes (page text on its panels); `build_art.py` checks the widget.
- The icon at 48 px and inside a circle mask.

# Bathurst Countdown

The look of the app that counts down to the next Supercars race at Mount Panorama, Bathurst: a big stopwatch over a sunny day on the Mountain, with blue sky, green hills, a red-and-white kerb and hot orange race-timing accents.

## The app icon

A stopwatch fills the icon. Its navy face carries the outline of the Mount Panorama circuit in white, traced from the official circuit map (Pit Straight at the bottom, Mountain Straight up the right, the Cutting, Reid Park, Sulman Park, Skyline, the Esses and Dipper, Forrest's Elbow at the top left, then Conrod Straight and The Chase down the left). BATHURST / COUNTDOWN sits in the top of the circuit and the day count (07 DAYS) in the Pit Straight to Griffin's Bend section. A car runs down Conrod, and a chequered line marks the start/finish on Pit Straight. The bezel is white on the left and orange-to-yellow on the right, with orange crown buttons. Below it are hills, trees, the road with its kerb and a MOUNT PANORAMA wall.

- Master: `assets/App icon/bathurst-icon-1024.png`.
- The number in the icon is fixed artwork. The live countdown belongs in the app.

## Backgrounds

`assets/Backgrounds/` holds a portrait (1080 × 1920) and landscape (1920 × 1080) scene with the same sky, hills, road and wall, plus a large, faint circuit outline in the sky. The middle is left clear so the app can draw its live countdown on top. Use `ink` or white text on `dial-navy` panels over them.

## Colour

`sky-blue` and `dial-navy` set the ground. `stopwatch-orange` and `dial-yellow` are the race-timing accents, used for the one or two things that must be seen first. `kerb-red` and `grass-green` belong to the scene only. Text is white on `dial-navy` / `sky-blue`, or `ink` on `surface` / `dial-yellow`.

## Type

Poppins Bold (italic for the BATHURST COUNTDOWN wordmark), with the system sans as fallback. `countdown` for the big numbers, `title` for headings, `label` for unit labels (DAYS · HRS · MIN · SEC).

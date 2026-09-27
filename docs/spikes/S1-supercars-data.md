# Spike S1 — Getting the Bathurst race start time from supercars.com

**Date:** 2026-09-27
**Result:** Solved. Read the data that each page embeds for Next.js. The site has no API we are allowed to use, and no HTML parser is needed.

## Question
What is the most reliable way to get the Bathurst 1000 main-race start time from supercars.com (plan §5.2)?

## Findings

### Allowed pages
`https://www.supercars.com/robots.txt` disallows `/api/`, `/account/`, `/auth/`, `/superview/videos/` and `/barcode/`. It does not disallow `/calendar` or `/events/*`.

We must **not** call the site's `/api/` endpoints, even though the pages use them.

### How the data is delivered
- The pages are built with Next.js App Router and rendered on the server.
- All page data is embedded in the HTML as script blocks: `self.__next_f.push([1,"<JSON-encoded string>"])`.
- The data is structured, and every date is ISO-8601 with its **UTC offset**, so no guessing about time zones is needed.

### Extraction algorithm (verified on both pages; every row parsed, none failed)
1. Regex: `self\.__next_f\.push\(\[1,("(?:[^"\\]|\\.)*")\]\)`. JSON-decode each captured string literal.
2. Concatenate the decoded strings in order.
3. Split on `\n`. Each line looks like `<hex id>:<payload>`. Keep the lines whose payload starts with `[` or `{`, and parse them as JSON.
4. Walk each parsed tree recursively and pick out the objects described below.

### `GET /calendar`: event objects
Objects with a `slug` and a `startDate`:

| Field | Example |
|-------|---------|
| `slug` | `2026-bathurst-1000` |
| `title` | `2026 Repco Bathurst 1000` |
| `location` | `Bathurst, NSW` |
| `startDate` | `2026-10-08T06:00:00.000+11:00` |
| `endDate` | `2026-10-11T18:00:00.000+11:00` |

All 14 events of the 2026 season were present.

### `GET /events/{slug}`: session objects
Objects with `name`, `type`, `startDate` and `series`:

| Field | Example (Bathurst 1000 main race) |
|-------|-----------------------------------|
| `name` | `Race 30` |
| `type` | `Race` (others: `Practice`, `Qualifying`, `Shootout`, `Warm Up`, `On Track Activity`) |
| `startDate` | `2026-10-11T11:30:00.000+11:00` |
| `endDate` | `2026-10-11T18:30:00.000+11:00` |
| `durationLabel` | `161 laps` |
| `series.name` | `Repco Supercars Championship` |

- Support categories (Super2, Carrera Cup, Touring Car Masters and others) are sessions on the same page with a **different `series.name`**.
- Main-race filter (D12): `series.name == "Repco Supercars Championship"` and `type == "Race"`. In 2026 exactly one session matches.
- `estimatedDuration` = `endDate − startDate` (7 hours for 2026).

## Decision for `EventFeedBuilder`
- Fetch `/calendar`, find the events whose slug matches `*-bathurst-1000`, then fetch each matching `/events/{slug}` page. That is at most 2–3 requests per run.
- Send an honest User-Agent: `BathurstCountdownFeedBuilder/<version> (+https://bigriversoftware.au)`.
- Save both pages as **test fixtures**. If the site's markup changes, a fixture test fails instead of a bad feed being published.
- Next year's event page may not exist yet (404) or may have no Supercars `Race` session yet. Either way the event is published as `DateTba`, and the app shows "TBA" (D13).
- Wikipedia fallback: not needed for v1. `overrides.json` covers manual corrections.

# Copy-Paste Prompt — Build Sound of Sight Presentation (Video-First Plan)

> **How to use this file:** Open a new Claude conversation. Copy everything below the `─── PROMPT BEGINS ───` line. Paste it as your first message. Claude will produce a complete `.pptx` PowerPoint file with image placeholders, ready to open and edit.

---

─── PROMPT BEGINS ───

You are building a 5-minute presentation **PowerPoint deck** for a class showcase of a Unity game called **Sound of Sight**. I need an actual `.pptx` file, not markdown — produce it end-to-end using `python-pptx` and deliver the binary file as a downloadable artifact.

## What you must produce

1. A single Python script using `python-pptx` that builds the entire deck.
2. **Execute the script and produce the resulting `slides_video_first.pptx` as a downloadable file.** If you have a code-execution tool available, use it. If not, output the script in a single fenced code block and tell me clearly that I need to run it locally (`pip install python-pptx`, then `python build_deck.py`).
3. The deck must contain **5 slides** with exact content, layout, and speaker notes as specified below.
4. Every image / chart / video clip is a **labeled placeholder rectangle** (gray fill, dashed border, centered text reading `REPLACE WITH: <filename>`), so the deck is fully usable before I have the footage. I'll click each placeholder in PowerPoint and Insert → Picture to swap in the real asset later.

## Slide dimensions and theme

- **16:9 widescreen** (13.333 × 7.5 inches).
- **Background:** dark `#1a1a1f` on every slide.
- **Panel / card fill:** `#22222a` (use this for content cards if you draw any).
- **Foreground text:** `#e6e6f0`.
- **Primary accent (titles, headlines, key callouts):** warm yellow `#ffd24a` — this matches the slash light in the game.
- **Secondary accents:** light blue `#9bc1ff`, light red `#ff7c7c`, light green `#9affc8`.
- **Fonts:** Inter, Helvetica, or whatever sans-serif python-pptx defaults to. Sizes: titles ≥40pt, headlines ≥28pt, body ≥20pt, callouts ≥18pt.
- **Speaker notes:** populate `slide.notes_slide.notes_text_frame.text` for every slide with the full speaker-notes prose given below.
- **Image placeholders:** dashed-border rectangle, gray fill (`#3a3a44`), white centered text labeled `REPLACE WITH: <filename>`. Sized to fit the layout described per slide.

## Game context (so you understand what we're presenting)

**Sound of Sight** is a 2D top-down action game built in Unity. Tagline: *Every attack you make permanently escalates the threat.*

The world is pitch black. The player has no flashlight — only their combat abilities produce light. A small ambient glow lets the player see their immediate surroundings; everything else is dark. Slashing, shooting, dashing, and the Light Wave ability all emit light that **reveals enemies** but also **permanently activates them** — once an enemy sees light, it stays active forever and chases the player even in the dark afterward. The core tension: every attack you make has a permanent consequence.

The player has **3 HP**, enemies have **2 HP**. Player abilities:
- **Slash (J)** — 120° melee arc, 0.15s cooldown, no resource cost
- **Shoot (K)** — bullet with travelling light, costs 1 bullet + 1 energy
- **Dash (Shift)** — fast burst, deals 2 damage on contact, destroys enemy bullets, costs 1 dash + 3 energy
- **Light Wave (L)** — 360° room-wide light burst, costs 1 flash + 10 energy

Game modes: a hand-crafted **Story Mode** with chained levels, a procedurally-generated **Endless Mode** (rooms get bigger each floor, with HP and ammo rewards between rooms), and four hand-tuned bosses (Scarab, Umbra, Vesper, Crimson) interleaved into endless mode every 5 rooms.

We ran a closed beta from **2026-04-06 to 2026-04-27** and gathered telemetry: 3,961 events, 48 runs, 2,509 enemy kills, 1,000 damage interactions. We made design changes based on what the data showed.

## Time budget — 5:00 total

| Time | Phase | Speaker |
|---|---|---|
| 0:00 – 1:50 | **Opening video plays first** (separate file, NOT a slide) | (silent / music bed) |
| 1:50 – 2:15 | **Slide 1**: Game pitch + 4-panel evolution timeline (25s) | Speaker A |
| 2:15 – 3:00 | **Slide 2**: Issue #1 — slash dominance (45s) | Speaker B |
| 3:00 – 3:45 | **Slide 3**: Issue #2 — hit-stacked damage chains (45s) | Speaker C |
| 3:45 – 4:30 | **Slide 4**: Issue #3 — long-tail enemies / Hunt Mode (45s) | Speaker D |
| 4:30 – 5:00 | **Slide 5**: Team highlights — one favorite each (30s) | Everyone |

The opening 1:50 video is a separate file the team plays before opening the deck. **Do not create a slide for it.** The deck you produce starts at Slide 1.

## Slide specifications

For each slide below: produce a slide that matches the layout, has the headline and body text shown, has placeholder rectangles where I describe images / clips, and has the speaker notes pasted into the notes pane verbatim.

---

### Slide 1 — Game pitch + evolution (25 seconds)

**Layout:**
- **Left half (about 55% width):** large title `Sound of Sight` in accent yellow `#ffd24a`, ~60pt. Below it, the tagline `Every attack you make permanently escalates the threat.` in foreground white, ~24pt.
- **Right half:** a vertical four-panel timeline. Each panel is a small image-placeholder rectangle stacked top-to-bottom, with a label to the left of each panel: `March`, `April`, `Beta`, `Gold`. The placeholder text on each rectangle reads `REPLACE WITH: timeline_march.png`, `timeline_april.png`, `timeline_beta.png`, `timeline_gold.png`.

**Speaker notes (paste verbatim into notes pane):**

> What you just saw is Sound of Sight — a 2D top-down action game built in Unity where the world is dark and your combat abilities are the only thing that produces light. We started with the core loop in March, shipped procedural rooms and four bosses through April, and ran a closed beta. The next three slides are the three biggest things that beta data taught us — and what we shipped because of it.

---

### Slide 2 — Issue #1: 50% of runs never fire a single bullet (45 seconds)

**Layout:**
- **Top:** headline `Issue 1: 50% of runs never fire a single bullet.` in accent yellow `#ffd24a`, ~36pt, left-aligned.
- **Top-left quadrant (chart 1):** image placeholder labeled `REPLACE WITH: 05_per_run_resource_collapse.png`. Below the placeholder, small caption text: `Per-run kill-method composition + resource collapse.`
- **Top-right quadrant (chart 2):** image placeholder labeled `REPLACE WITH: 01_kill_method_mix_by_day.png`. Below the placeholder, small caption text: `Slash dominates every beta day.`
- **Bottom half:** two side-by-side video / image placeholders, each ~half-width. Left placeholder labeled `REPLACE WITH: before_slash_spam.mp4` with a red `BEFORE` tag in the top-left corner. Right placeholder labeled `REPLACE WITH: after_skitter_swap.mp4` with a green `AFTER` tag in the top-left corner.
- **Bottom strip (small text, foreground white):** the key data points as a single line, separated by `•`: `82.8% slash share • 50% zero bullets • 50% zero dash • 25% pure slash • Skitter bullet share 14% → 28%`.

**Speaker notes (paste verbatim):**

> Our beta data showed that 82.8% of all kills came from one ability: slash. We thought that was just preference. Then we looked at the per-run breakdown — half of all runs never fired a single bullet, half never used dash, and a quarter scored 100% of their kills with slash. That's not preference. That's the resource economy collapsing — players ignored the entire toolkit. The data also told us why: slash kills in a quarter of a second; bullets and dashes take 6 to 10 times longer because they have travel time. So we couldn't nerf slash globally without breaking combat. Instead, we built the Skitter — an enemy with slash resistance — plus a clone-phase that punishes slash spam. After shipping it, bullet usage against the Skitter doubled.

---

### Slide 3 — Issue #2: Fatal damage chains were hit-stacks, not long fights (45 seconds)

**Layout:**
- **Top:** headline `Issue 2: Fatal damage chains were hit-stacks, not long fights.` in accent yellow, ~36pt, left-aligned.
- **Left half (60% width):** large image placeholder labeled `REPLACE WITH: 04_damage_chain_distribution.png`. Below the placeholder, small caption: `Fatal vs survived damage histogram. Player base HP = 3.`
- **Right half (40% width):** two stacked video placeholders. Top placeholder labeled `REPLACE WITH: before_multi_hit.mp4` with red `BEFORE` tag. Bottom placeholder labeled `REPLACE WITH: after_iframes.mp4` with green `AFTER` tag.
- **Bottom strip (small text):** `1,000 chains • 894 survived (avg 1.46 dmg) • 106 fatal (avg 2.80 dmg) • Worst observed: 9 dmg in one chain • Fix: 0.5s i-frames`.

**Speaker notes (paste verbatim):**

> Of a thousand damage interactions in beta, the chains players survived averaged 1.5 damage. The chains that killed them averaged 2.8 damage — almost double. But here's the thing: the durations were the same. Fatal chains weren't longer engagements where the player took multiple hits over time. They were single-frame multi-hits — overlapping bullets and contact hitboxes dealing damage on the same physics tick. The worst case we recorded was 9 damage in one chain. The player only has 3 HP. So we added 0.5-second invincibility frames after every hit. That same 9-damage chain now lands as 1 or 2 damage, because every hit after the first lands inside the i-frame window and gets absorbed.

---

### Slide 4 — Issue #3: Half of enemies died instantly, the other half hid for 30 seconds (45 seconds)

**Layout:**
- **Top:** headline `Issue 3: Half died instantly. The other half hid for 30 seconds.` in accent yellow, ~36pt, left-aligned.
- **Left half (60% width):** large image placeholder labeled `REPLACE WITH: 03_ttk_mean_vs_median.png`. Below the placeholder, small caption: `Median vs mean TTK per enemy spawn slot — 14 of 14 slots show a 2-5× gap.`
- **Right half (40% width):** two stacked video placeholders. Top placeholder labeled `REPLACE WITH: before_stuck_enemy.mp4` with red `BEFORE` tag. Bottom placeholder labeled `REPLACE WITH: after_hunt_mode.mp4` with green `AFTER` tag.
- **Bottom strip (small text):** `Median TTK: 0.37s • Mean TTK: 1.28s • 3.5× gap • Fix: Hunt Mode (A* pathfinding when ≤3 enemies remain)`.

**Speaker notes (paste verbatim):**

> When we plotted the mean versus the median time-to-kill for every enemy spawn slot, every single one had the same shape: half of all enemies died in 0.4 seconds — that's the one-slash one-kill we designed for — but the other half survived 5 to 30 seconds. The reason is mechanical: the moment an enemy escapes the slash arc, it loses line-of-sight and chases at base speed with no pathfinding, so it gets stuck on geometry. We added Hunt Mode: when 3 or fewer enemies remain, every survivor force-activates and switches to A-star pathfinding. The stragglers come find you instead of you having to find them. The long tail of those room-clear times closes.

---

### Slide 5 — Team highlights (30 seconds)

**Layout:**
- **Top:** headline `Our favorite parts.` in accent yellow, ~40pt, centered.
- **Body:** a 2×2 grid of equal-sized panels. Each panel is a card with `#22222a` fill, a small image placeholder at the top labeled `REPLACE WITH: highlight_<n>.png`, the team-member name in white below the image (~22pt), and a one-line caption in light gray (~16pt italic).
- **Suggested captions** (use as placeholders — the team will customize):
  1. *"The first time the slash arc revealed a room of waiting enemies and the design read perfectly."* — Member 1
  2. *"Building Crimson's clone phase — when a tester picked the wrong clone and got knocked back, we knew it worked."* — Member 2
  3. *"Watching the analytics confirm slash was 10× faster than every other tool."* — Member 3
  4. *"Two and a half seconds of portal choreography that makes every cleared room feel earned."* — Member 4
- **Bottom-right small text:** `Thank you.`

**Speaker notes (paste verbatim):**

> Each team member takes 6 to 8 seconds. Read your one sentence — `My favorite part is X because Y.` Go in slide order, left-to-right, top-to-bottom. The last person ends with `Thank you.` Do not run over the 5:00 mark.

---

## Assets to drop in after the deck builds

I'll place these files in the same folder as the .pptx, then click each placeholder in PowerPoint and Insert → Picture (or Insert → Video) to swap in the real asset.

- **Charts (already exist in our repo at `docs/analytics/`):**
  - `01_kill_method_mix_by_day.png`
  - `03_ttk_mean_vs_median.png`
  - `04_damage_chain_distribution.png`
  - `05_per_run_resource_collapse.png`
- **Timeline panels for Slide 1** — small static screenshots, one per phase: `timeline_march.png`, `timeline_april.png`, `timeline_beta.png`, `timeline_gold.png`.
- **Before / after clip pairs** for Slides 2-4 — capture per the team's shot list:
  - `before_slash_spam.mp4` + `after_skitter_swap.mp4`
  - `before_multi_hit.mp4` + `after_iframes.mp4`
  - `before_stuck_enemy.mp4` + `after_hunt_mode.mp4`
- **Highlight images for Slide 5** — `highlight_1.png` through `highlight_4.png`.

If you have a code-execution tool, optionally check whether any of those files exist in the working directory and embed them directly instead of using a placeholder. If a file isn't there, fall back to the placeholder rectangle.

## Final deliverable checklist

- [ ] A `slides_video_first.pptx` file delivered as a downloadable artifact (preferred), OR a Python build script with run instructions if you cannot execute code.
- [ ] All 5 slides populated with the specified layouts, headlines, body, data callouts, and image placeholders.
- [ ] Speaker notes populated **verbatim** in the notes pane of every slide.
- [ ] Dark theme applied consistently across all slides (background, accent yellow, foreground white).
- [ ] Slide dimensions = 13.333 × 7.5 inches (16:9 widescreen).
- [ ] After producing the file, output a short numbered checklist titled "Assets to drop in" listing the filenames I need to place next to the .pptx, grouped by slide.

## Constraints

- Do **not** invent data or numbers. Only use the figures listed above.
- Do **not** add a 6th slide or pad the deck. 5 slides only.
- Do **not** output Marp markdown, Reveal.js, HTML, or any format other than `.pptx`. The deliverable is a PowerPoint file.
- Do **not** abbreviate the speaker notes — paste them verbatim from the prose blocks above.
- If a layout choice would make the slide visually crowded, prioritize clarity over density.

Build the deck now.

─── PROMPT ENDS ───

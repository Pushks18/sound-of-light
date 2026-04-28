# Copy-Paste Prompt — Build Sound of Sight Presentation (Live-Demo Plan)

> **How to use this file:** Open a new Claude conversation. Copy everything below the `─── PROMPT BEGINS ───` line. Paste it as your first message. Claude will produce a complete `.pptx` PowerPoint file with image placeholders, ready to open and edit.

---

─── PROMPT BEGINS ───

You are building a 5-minute presentation **PowerPoint deck** for a class showcase of a Unity game called **Sound of Sight**. This is the **live-demo variant**: 4 minutes of slides up front, then a 1-minute live gameplay showcase to close. I need an actual `.pptx` file, not markdown — produce it end-to-end using `python-pptx` and deliver the binary file as a downloadable artifact.

## What you must produce

1. A single Python script using `python-pptx` that builds the entire deck.
2. **Execute the script and produce the resulting `slides_live_demo.pptx` as a downloadable file.** If you have a code-execution tool available, use it. If not, output the script in a single fenced code block and tell me clearly that I need to run it locally (`pip install python-pptx`, then `python build_deck.py`).
3. The deck must contain **5 slides** with exact content, layout, and speaker notes as specified below.
4. Every image / chart / video clip is a **labeled placeholder rectangle** (gray fill, dashed border, centered text reading `REPLACE WITH: <filename>`), so the deck is fully usable before I have the footage. I'll click each placeholder in PowerPoint and Insert → Picture to swap in the real asset later.
5. After the deck, also produce a **one-page lectern reference card** listing the four live-demo beats with timing — this is for me to keep on the lectern at the keyboard. Output it as plain text in a fenced block at the end.

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
| 0:00 – 0:30 | **Slide 1**: Game pitch + 4-panel evolution timeline + embedded looping clip placeholder (covers the rubric's "footage of the final version" requirement) | Speaker A |
| 0:30 – 1:30 | **Slide 2**: Issue #1 — slash dominance (60s) | Speaker B |
| 1:30 – 2:30 | **Slide 3**: Issue #2 — hit-stacked damage chains (60s) | Speaker C |
| 2:30 – 3:30 | **Slide 4**: Issue #3 — long-tail enemies / Hunt Mode (60s) | Speaker D |
| 3:30 – 4:00 | **Slide 5**: Team highlights — last person says the live-demo hand-off line | Everyone |
| 4:00 – 5:00 | **Live demo (NOT a slide)** — cut from PowerPoint to live game window, choreographed 4-beat play | Player |

Slides 2-4 each get **60 seconds** in this variant (vs 45 in the video-first version), so speaker notes are slightly longer. Slide 5 ends with a hand-off line that's the cue to cut to the live build.

## Slide specifications

For each slide below: produce a slide that matches the layout, has the headline and body text shown, has placeholder rectangles where I describe images / clips, and has the speaker notes pasted into the notes pane verbatim.

---

### Slide 1 — Game pitch + evolution + looping clip (30 seconds)

**Layout:**
- **Left third:** large title `Sound of Sight` in accent yellow `#ffd24a`, ~52pt. Below it, the tagline `Every attack you make permanently escalates the threat.` in foreground white, ~22pt.
- **Center third:** a large image / video placeholder labeled `REPLACE WITH: hero_loop.mp4 (autoplay, loop, muted)`. This is the embedded gameplay clip that satisfies the rubric's "footage of the final version" requirement since we don't have a separate opening video.
- **Right third:** a vertical four-panel timeline. Each panel is a small image-placeholder rectangle stacked top-to-bottom, with a label to the left: `March`, `April`, `Beta`, `Gold`. Placeholder text on each rectangle reads `REPLACE WITH: timeline_march.png`, `timeline_april.png`, `timeline_beta.png`, `timeline_gold.png`.

**Speaker notes (paste verbatim into notes pane):**

> Welcome — this is Sound of Sight, a 2D top-down action game in Unity where every room is dark and your combat abilities are the only light. We started with the core loop in March, shipped procedural rooms and four bosses through April, and ran a closed beta. We're going to walk through the three biggest things that beta data taught us, hand the game off for a live demo, and that's the five minutes.

---

### Slide 2 — Issue #1: 50% of runs never fire a single bullet (60 seconds)

**Layout:**
- **Top:** headline `Issue 1: 50% of runs never fire a single bullet.` in accent yellow `#ffd24a`, ~36pt, left-aligned.
- **Top-left quadrant (chart 1):** image placeholder labeled `REPLACE WITH: 05_per_run_resource_collapse.png`. Below the placeholder, small caption text: `Per-run kill-method composition + resource collapse.`
- **Top-right quadrant (chart 2):** image placeholder labeled `REPLACE WITH: 01_kill_method_mix_by_day.png`. Below the placeholder, small caption text: `Slash dominates every beta day.`
- **Bottom half:** two side-by-side video / image placeholders, each ~half-width. Left placeholder labeled `REPLACE WITH: before_slash_spam.mp4` with a red `BEFORE` tag in the top-left corner. Right placeholder labeled `REPLACE WITH: after_skitter_swap.mp4` with a green `AFTER` tag in the top-left corner.
- **Bottom strip (small text, foreground white):** `82.8% slash share • 50% zero bullets • 50% zero dash • 25% pure slash • Skitter bullet share 14% → 28%`.

**Speaker notes (paste verbatim):**

> Our beta data showed that 82.8% of all kills came from one ability: slash. We thought that was just preference. Then we looked at the per-run breakdown — half of all runs never fired a single bullet, half never used dash, and a quarter scored 100% of their kills with slash. That's not preference. That's the resource economy collapsing — players were ignoring the entire toolkit. The data also told us why: slash kills in a quarter of a second; bullets and dashes take 6 to 10 times longer because they have travel time. So we couldn't nerf slash globally without breaking combat. Instead we built the Skitter — an enemy with slash resistance — plus a clone-phase on Crimson that punishes slash spam. After shipping, bullet usage against the Skitter doubled — players kept slash for normal enemies but reached for bullets when the kit demanded it.

---

### Slide 3 — Issue #2: Fatal damage chains were hit-stacks, not long fights (60 seconds)

**Layout:**
- **Top:** headline `Issue 2: Fatal damage chains were hit-stacks, not long fights.` in accent yellow, ~36pt, left-aligned.
- **Left half (60% width):** large image placeholder labeled `REPLACE WITH: 04_damage_chain_distribution.png`. Below the placeholder, small caption: `Fatal vs survived damage histogram. Player base HP = 3.`
- **Right half (40% width):** two stacked video placeholders. Top placeholder labeled `REPLACE WITH: before_multi_hit.mp4` with red `BEFORE` tag. Bottom placeholder labeled `REPLACE WITH: after_iframes.mp4` with green `AFTER` tag.
- **Bottom strip (small text):** `1,000 chains • 894 survived (avg 1.46 dmg) • 106 fatal (avg 2.80 dmg) • Worst observed: 9 dmg in one chain • Fix: 0.5s i-frames`.

**Speaker notes (paste verbatim):**

> Of a thousand damage interactions in beta, the chains players survived averaged 1.5 damage. The chains that killed them averaged 2.8 damage — almost double. But the durations of those two distributions are the same. Fatal chains weren't longer engagements where the player took multiple hits over time. They were single-frame multi-hits — overlapping bullets and contact hitboxes dealing damage on the same physics tick. The worst chain we recorded was 9 damage on one frame. The player only has 3 HP, so anything north of 3 is a one-shot. We added 0.5-second invincibility frames after every hit. The 9-damage worst case is now 1 or 2 damage, because every hit after the first lands inside the i-frame window and gets absorbed. The mechanic is invisible when nothing's happening, which is exactly what a defensive system should be.

---

### Slide 4 — Issue #3: Half of enemies died instantly, the other half hid for 30 seconds (60 seconds)

**Layout:**
- **Top:** headline `Issue 3: Half died instantly. The other half hid for 30 seconds.` in accent yellow, ~36pt, left-aligned.
- **Left half (60% width):** large image placeholder labeled `REPLACE WITH: 03_ttk_mean_vs_median.png`. Below the placeholder, small caption: `Median vs mean TTK per enemy spawn slot — 14 of 14 slots show a 2-5× gap.`
- **Right half (40% width):** two stacked video placeholders. Top placeholder labeled `REPLACE WITH: before_stuck_enemy.mp4` with red `BEFORE` tag. Bottom placeholder labeled `REPLACE WITH: after_hunt_mode.mp4` with green `AFTER` tag.
- **Bottom strip (small text):** `Median TTK: 0.37s • Mean TTK: 1.28s • 3.5× gap • Fix: Hunt Mode (A* pathfinding when ≤3 enemies remain)`.

**Speaker notes (paste verbatim):**

> When we plotted the mean versus the median time-to-kill for every enemy spawn slot in the game, every single one had the same shape: half of all enemies died in 0.4 seconds — that's the one-slash one-kill we designed for. But the other half survived 5 to 30 seconds. The reason is mechanical: the moment an enemy escapes the slash arc, it loses line-of-sight and falls back to a chase at base speed with no pathfinding. So it gets stuck on geometry — corners, walls, the dead zones the procedural generator naturally produces. We added Hunt Mode: when 3 or fewer enemies remain in a room, every survivor force-activates and switches to A-star pathfinding with diagonal-corner-cutting prevention. The stragglers come find you instead of you having to find them. Watching a stuck enemy turn and walk around a wall to chase the player is the most satisfying frame this game produces.

---

### Slide 5 — Team highlights + hand-off to live demo (30 seconds)

**Layout:**
- **Top:** headline `Our favorite parts.` in accent yellow, ~40pt, centered.
- **Body:** a 2×2 grid of equal-sized panels. Each panel is a card with `#22222a` fill, a small image placeholder at the top labeled `REPLACE WITH: highlight_<n>.png`, the team-member name in white below the image (~22pt), and a one-line caption in light gray (~16pt italic).
- **Bottom-right small text in accent yellow:** `Up next → live demo`.
- **Suggested captions** (use as placeholders — the team will customize):
  1. *"The first time the slash arc revealed a room of waiting enemies and the design read perfectly."* — Member 1
  2. *"Building Crimson's clone phase — when a tester picked the wrong clone and got knocked back, we knew it worked."* — Member 2
  3. *"Watching the analytics confirm slash was 10× faster than every other tool."* — Member 3
  4. *"Two and a half seconds of portal choreography that makes every cleared room feel earned."* — Member 4

**Speaker notes (paste verbatim — note the explicit hand-off line at the end):**

> Each team member takes 6 seconds. Read your one sentence — `My favorite part is X because Y.` Go in slide order, left-to-right, top-to-bottom. The last person ends with the literal line `and now we'll show you a minute of the game.` That line is the cue to cut from PowerPoint to the live game window. While the last speaker says it, the live player walks to the keyboard. Do not skip the hand-off line — it is what triggers Phase 2.

---

## After the deck, produce a lectern reference card

After you build the deck, output a fenced plain-text block containing this lectern card. It's a single page I'll print and keep at the keyboard during the live minute.

```
LIVE DEMO — 4 BEATS, 60 SECONDS TOTAL

Pre-load: Showcase scene, full HP, full ammo, mid-room, past spawn flash.
Window: live game, 1920×1080 fullscreen on projector. Pre-launched & minimized.
Cue: Speaker D's line "and now we'll show you a minute of the game" → Cmd-Tab.

Beat 1 — Reveal (0:00–0:12, 12s)
  Walk forward into dark. Slash once. Cone reveals first enemy.
  Slash again to kill it.
  → Shows: light reveals threat.

Beat 2 — Toolkit (0:12–0:25, 13s)
  Fire two bullets at second enemy across the room (yellow streak).
  Dash through an incoming enemy bullet (blue trail).
  → Shows: kit is more than slash.

Beat 3 — Light Wave (0:25–0:38, 13s)
  Hit L. 360° burst lights the entire room.
  Three more enemies revealed (now permanently active).
  → Shows: every reveal escalates the threat.

Beat 4 — Clear & portal (0:38–0:50, 12s)
  Slash + dash through remaining enemies.
  Last enemy dies → portal spawns → "+1 ♥" floats up.
  Walk into portal. Brief room-transition fade.
  → Shows: the endless-mode loop.

Wrap (0:50–1:00, 10s)
  Speaker A: "Thanks for watching." Done.

If something breaks:
  - You die: Space → continue at next beat.
  - Lost an enemy: trigger Light Wave to find it.
  - Build crashes: cut to live-demo-fallback.mp4 on desktop.
```

## Assets to drop in after the deck builds

I'll place these files in the same folder as the .pptx, then click each placeholder in PowerPoint and Insert → Picture (or Insert → Video) to swap in the real asset.

- **Charts (already exist in our repo at `docs/analytics/`):**
  - `01_kill_method_mix_by_day.png`
  - `03_ttk_mean_vs_median.png`
  - `04_damage_chain_distribution.png`
  - `05_per_run_resource_collapse.png`
- **Hero loop for Slide 1:** `hero_loop.mp4` (or `hero.png` if videos won't embed in PowerPoint).
- **Timeline panels for Slide 1:** `timeline_march.png`, `timeline_april.png`, `timeline_beta.png`, `timeline_gold.png`.
- **Before / after clip pairs** for Slides 2-4:
  - `before_slash_spam.mp4` + `after_skitter_swap.mp4`
  - `before_multi_hit.mp4` + `after_iframes.mp4`
  - `before_stuck_enemy.mp4` + `after_hunt_mode.mp4`
- **Highlight images for Slide 5** — `highlight_1.png` through `highlight_4.png`.

If you have a code-execution tool, optionally check whether any of those files exist in the working directory and embed them directly instead of using a placeholder. If a file isn't there, fall back to the placeholder rectangle.

## Final deliverable checklist

- [ ] A `slides_live_demo.pptx` file delivered as a downloadable artifact (preferred), OR a Python build script with run instructions if you cannot execute code.
- [ ] All 5 slides populated with the specified layouts, headlines, body, data callouts, and image placeholders.
- [ ] Speaker notes populated **verbatim** in the notes pane of every slide.
- [ ] The Slide 5 speaker notes contain the literal hand-off line `and now we'll show you a minute of the game.`
- [ ] Dark theme applied consistently across all slides (background, accent yellow, foreground white).
- [ ] Slide dimensions = 13.333 × 7.5 inches (16:9 widescreen).
- [ ] After the deck, the lectern reference card is output as a fenced plain-text block.
- [ ] After everything else, output a short numbered checklist titled "Assets to drop in" listing the filenames I need to place next to the .pptx, grouped by slide.

## Constraints

- Do **not** invent data or numbers. Only use the figures listed above.
- Do **not** add a 6th slide or pad the deck. 5 slides only — the live demo replaces what would have been a 6th slide.
- Do **not** output Marp markdown, Reveal.js, HTML, or any format other than `.pptx`. The deliverable is a PowerPoint file.
- Do **not** abbreviate the speaker notes — paste them verbatim from the prose blocks above.
- The hand-off line `and now we'll show you a minute of the game` MUST appear in Slide 5's speaker notes verbatim, since it is the cue that triggers Phase 2.
- If a layout choice would make the slide visually crowded, prioritize clarity over density.

Build the deck now.

─── PROMPT ENDS ───

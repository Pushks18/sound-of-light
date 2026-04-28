# Presentation Assets

Drop-in folder for the Sound of Sight presentation deck. Copy this entire folder to wherever you save `slides_video_first.pptx` / `slides_live_demo.pptx` so the placeholder filenames in the deck resolve cleanly when you Insert → Picture in PowerPoint.

The four chart PNGs below are already exported and ready to drop in. Everything else in the "needs capture" tables is footage your team still needs to record before showcase day.

## Charts — ready to use

These were generated from the closed beta's analytics data. They match the placeholder filenames referenced in the slide deck prompts.

| File | Goes on | What it shows |
|---|---|---|
| `01_kill_method_mix_by_day.png` | Slide 2 (top-right chart) | Per-day kill-method mix — slash dominates every beta day |
| `05_per_run_resource_collapse.png` | Slide 2 (top-left chart) | Per-run kill composition + resource-collapse summary (50% never bullet, 50% never dash, 25% pure-slash) |
| `04_damage_chain_distribution.png` | Slide 3 (left/center chart) | Damage chain histogram — fatal vs survived chains, with the 3 HP threshold |
| `03_ttk_mean_vs_median.png` | Slide 4 (left/center chart) | Per-enemy TTK mean vs median dumbbell — long tail visible on every spawn slot |
| `07_timeline.png` | *(optional)* Slide 1 | Beta telemetry timeline with analytics-driven commit markers — useful as an alternative to the four-panel evolution strip |

## Hero clip (Slide 1) — needs capture

For the live-demo variant only. The video-first variant doesn't need this — its opening 1:50 video plays before the deck.

| File | Spec |
|---|---|
| `hero_loop.mp4` | 12–15 seconds, autoplay, muted, looping. Player walks into a dark room, slashes once, light cone reveals an enemy mid-frame, kills it, walks back into the dark. Captured from the current Gold build at 1920×1080, 30 fps. |
| `hero.png` | Static fallback if PowerPoint export refuses the video. Single frame from the same scene at maximum drama (slash mid-swing, enemy partially revealed). |

## Evolution timeline panels (Slide 1) — needs capture

Four small static screenshots, one per development phase. Each ~480×270 (16:9 mini).

| File | What to capture |
|---|---|
| `timeline_march.png` | An early procedural room — bare walls, no bosses yet. Pulled from a March commit if possible, or recreated by stripping enemies/lights from the current build. |
| `timeline_april.png` | A boss arena — Scarab works well here because of the warning rectangle on its charge attack. |
| `timeline_beta.png` | A composite or screenshot of the analytics dashboard / a chart from `docs/analytics/` so the audience visually associates this phase with data work. |
| `timeline_gold.png` | The current Gold build mid-action — slash arc visible, enemy revealed, ambient light reading nicely. |

## Before / after clip pairs (Slides 2–4) — needs capture

Six clips total, three pairs. Each clip 8–18 seconds, 1920×1080. Use OBS or Unity Recorder.

### Slide 2 — Slash dominance

| File | Build | What to record |
|---|---|---|
| `before_slash_spam.mp4` | Pre-Skitter beta build (~2026-04-19 or earlier) | Player kills five basic enemies in a row by mashing J. No bullets, no dashes. Show the slash spam working effortlessly. ~12s. |
| `after_skitter_swap.mp4` | Current Gold build | Player encounters a Skitter, slash bounces (low damage from slash multiplier), player swaps to bullets and drops the Skitter. Then a basic enemy appears and player slashes it normally. Demonstrates the kit shifting based on enemy type. ~12s. |

### Slide 3 — Hit-stacked damage chains

| File | Build | What to record |
|---|---|---|
| `before_multi_hit.mp4` | Pre-i-frame build | Player walks into two overlapping enemy bullets and a contact hitbox simultaneously. Take 5–6 damage on one frame. Player dies instantly. ~8s. |
| `after_iframes.mp4` | Current Gold build | Same scenario, same approach. 0.5s i-frames absorb the second and third hits. Player takes 1 damage and dashes out alive. ~10s. |

### Slide 4 — Long-tail enemies / Hunt Mode

| File | Build | What to record |
|---|---|---|
| `before_stuck_enemy.mp4` | Pre-Hunt-Mode build | Player clears 4 of 5 enemies in a room. The 5th is stuck behind a wall corner. Player wanders for ~18 seconds looking for it. Show the frustration. |
| `after_hunt_mode.mp4` | Current Gold build | Same room. As the kill count drops to 3, all surviving enemies switch to A* pathfinding and start actively hunting the player around walls. The stuck enemy turns and walks around the corner toward the player. ~15s. |

## Highlight images (Slide 5) — needs capture

| File | Suggested content |
|---|---|
| `highlight_1.png` | Slash arc fully extended in a dark room, multiple enemies caught in the cone. |
| `highlight_2.png` | Crimson clone phase — all five clones in the rotating ring. |
| `highlight_3.png` | A dramatic frame from the Hunt Mode after-clip (enemy walking around a wall). |
| `highlight_4.png` | Portal suck-in mid-animation — player spinning down, ring expanding. |

The team will customize these to match the favorites each member ends up calling out.

## How to use this folder

1. Generate the `.pptx` with one of the prompt files in `docs/`.
2. Save the .pptx file inside this folder (or copy this whole folder to wherever the .pptx lives).
3. Open the .pptx in PowerPoint or Keynote. Each placeholder rectangle is labeled `REPLACE WITH: <filename>` — find that file in this folder, click the placeholder, and Insert → Picture (or Insert → Video for clips).
4. The four chart files are already here. Capture the rest from the tables above.

## Capture quick-reference

- **Encoding:** MP4, H.264, 30 fps. Audio optional — it's muted in the deck either way.
- **Resolution:** 1920×1080.
- **Tool:** OBS Studio, Unity Recorder package, or macOS QuickTime screen-record.
- **Build switching:** keep separate build folders / branches for the "before" clips. Don't overwrite the Gold build to capture old footage — git checkout an older tag, build, capture, then return.
- **Two takes per clip minimum.** Pick the cleaner one.

# Presentation Plan — Sound of Sight Gold Showcase (Live-Demo Variant)

**Alternate to `presentation.md`.** Same 5-minute slot, same content beats, but the structure is inverted: 4 minutes of slides up front, then a 1-minute live gameplay showcase to close.

The trade-off: the audience has 4 minutes of fully-controlled framing before they see the game live, so the presentation reads as "we built a thing → here is what we learned → now watch it run". The risk is that the last minute is unscripted gameplay, which costs reliability — see the **Live-demo risk plan** at the bottom for the safety net.

This document is the build sheet. It tells the team what slides to build, what to say, what to capture as fallback footage, and exactly what to do for the live minute.

## Master timing

| Time | Phase | Content | Speaker |
|---|---|---|---|
| 0:00 – 0:30 | Slide 1 | Game pitch + 4-panel evolution timeline + a short looping clip | Speaker A |
| 0:30 – 1:30 | Slide 2 | Issue #1 — Players never use anything but slash | Speaker B |
| 1:30 – 2:30 | Slide 3 | Issue #2 — A single bad frame could take 9 HP | Speaker C |
| 2:30 – 3:30 | Slide 4 | Issue #3 — Stragglers hid and wasted player time | Speaker D |
| 3:30 – 4:00 | Slide 5 | Team highlights — one favorite each | Everyone |
| 4:00 – 4:05 | Hand-off | Cut from slides to live build, transition line | Speaker A |
| 4:05 – 4:55 | Live demo | 50 seconds of choreographed live gameplay | Player (you) |
| 4:55 – 5:00 | Wrap | "Thanks for watching" + team name on screen | Speaker A |

If the team is smaller than four, collapse Speaker assignments accordingly. The live player can also narrate their own demo if there are only two of you.

## Pace and tone

- **Slides are roomier than the video-first variant.** Each issue gets 60 seconds, not 45 — that's deliberate. Use the extra 15 seconds to hold on the chart longer and make the before/after clip readable.
- **Treat 4:00 as a hard wall.** Whatever slide you're on at 4:00, click forward. The demo player will be ready.
- **The live minute is showmanship.** Don't speedrun, don't try to do everything. Hit the four beats below at a watchable pace and end on a clean moment.

---

## Phase 1 — Slides (0:00 – 4:00)

### Slide 1 — Game pitch + evolution (0:00 – 0:30, 30s) — Speaker A

**Slide layout:**
- Title block: *Sound of Sight*
- One-line tagline: *Every attack you make permanently escalates the threat.*
- Center: a 12-15 second looping gameplay clip (silent autoplay) — captured from the current Gold build, showing slash → reveal → enemy activation → kill → walk back into the dark. This is the rubric's "footage of the final version of the game" requirement, embedded so we don't need a separate opening video.
- Right strip: a four-panel mini-timeline labeled **March → April → Beta → Gold**, each panel a tiny screenshot (procedural rooms → bosses → analytics → polish).

**Speaker notes (~75 words, conversational):**

> "Welcome — this is *Sound of Sight*, a 2D top-down action game in Unity where every room is dark and your combat abilities are the only light. We started with the core loop in March, shipped procedural rooms and four bosses through April, and ran a closed beta. We're going to walk through the three biggest things that beta data taught us, hand the game off for a live demo, and that's the five minutes."

**Speaker cues:**
- The looping clip on the slide is the visual anchor — let it play behind you while you talk; don't cut to it or pause it.
- Click forward at "five minutes". Land it cleanly so Speaker B can pick up.

### Slide 2 — Issue #1: Players never use anything but slash (0:30 – 1:30, 60s) — Speaker B

**Slide layout:**
- Title: **"Issue 1: 50% of runs never fire a single bullet."**
- Top-left chart: `05_per_run_resource_collapse.png` — focus on the right panel (25% pure-slash, 50% never shoot, 50% never dash, 95% never trap).
- Top-right chart: `01_kill_method_mix_by_day.png` — slash bar dominating every day.
- Bottom: side-by-side gameplay clip pair — **Before** (early beta build, player kills 5 enemies in a row by mashing J) — **After** (current build, player meets a Skitter, slash bounces, player swaps to bullets, drops the Skitter, then back to slash for basic enemies).

**Speaker notes (~150 words):**

> "Our beta data showed that 82.8% of all kills came from one ability: slash. We thought that was just preference. Then we looked at the per-run breakdown — half of all runs never fired a single bullet, half never used dash, and a quarter scored 100% of their kills with slash. That's not preference. That's the resource economy collapsing — players were ignoring the entire toolkit. The data also told us *why*: slash kills in a quarter of a second; bullets and dashes take 6-to-10 times longer because they have travel time. So we couldn't nerf slash globally without breaking combat. Instead we built the Skitter — an enemy with slash resistance — plus a clone-phase on Crimson that punishes slash spam. After shipping, bullet usage against the Skitter doubled — players kept slash for normal enemies but reached for bullets when the kit demanded it."

**Speaker cues:**
- Pause for two seconds after "82.8% of all kills came from one ability". Let it land.
- Point at the right-panel collapse chart on the line "half of all runs never fired a single bullet". The 50% bar is the load-bearing visual.
- Hold on the after-clip until you finish "when the kit demanded it", then click forward.

### Slide 3 — Issue #2: A single bad frame could take 9 HP (1:30 – 2:30, 60s) — Speaker C

**Slide layout:**
- Title: **"Issue 2: Fatal damage chains were hit-stacks, not long fights."**
- Center chart: `04_damage_chain_distribution.png` — fatal vs survived damage histogram with the 3 HP threshold line clearly visible.
- Right: side-by-side clip pair — **Before** (player walks into two overlapping enemy bullets and a contact hitbox simultaneously, takes 6 damage on one frame, dies instantly) — **After** (same scenario, 0.5-second i-frames absorb the second and third hit, player takes 1 damage and dashes out).

**Speaker notes (~150 words):**

> "Of a thousand damage interactions in beta, the chains players survived averaged 1.5 damage. The chains that killed them averaged 2.8 damage — almost double. But the durations of those two distributions are the same. Fatal chains weren't longer engagements where the player took multiple hits over time. They were single-frame multi-hits — overlapping bullets and contact hitboxes dealing damage on the same physics tick. The worst chain we recorded was 9 damage on one frame. The player only has 3 HP, so anything north of 3 is a one-shot. We added 0.5-second invincibility frames after every hit. The 9-damage worst case is now 1 or 2 damage, because every hit after the first lands inside the i-frame window and gets absorbed. The mechanic is invisible when nothing's happening, which is exactly what a defensive system should be."

**Speaker cues:**
- The 3 HP dotted line on the chart is the visual anchor — point at it when you say "anything north of 3 is a one-shot".
- Sync the before/after clip: "before" plays during the problem statement, "after" plays during the solution.
- Click forward at "exactly what a defensive system should be".

### Slide 4 — Issue #3: Stragglers hid and wasted player time (2:30 – 3:30, 60s) — Speaker D

**Slide layout:**
- Title: **"Issue 3: Half of enemies died instantly — the other half hid for 30 seconds."**
- Center chart: `03_ttk_mean_vs_median.png` — the dumbbell chart. Every spawn slot has a 2-5× gap between median and mean.
- Right: side-by-side clip pair — **Before** (player clears 4 of 5 enemies; the 5th is stuck behind a wall corner, player wanders for 20 seconds looking for it) — **After** (same room, last 3 enemies switch to A\* pathfinding the moment the count drops to 3, they actively hunt the player around walls).

**Speaker notes (~150 words):**

> "When we plotted the mean versus the median time-to-kill for every enemy spawn slot in the game, every single one had the same shape: half of all enemies died in 0.4 seconds — that's the one-slash one-kill we designed for. But the *other* half survived 5 to 30 seconds. The reason is mechanical: the moment an enemy escapes the slash arc, it loses line-of-sight and falls back to a chase at base speed with no pathfinding. So it gets stuck on geometry — corners, walls, the dead zones the procedural generator naturally produces. We added Hunt Mode: when 3 or fewer enemies remain in a room, every survivor force-activates and switches to A-star pathfinding with diagonal-corner-cutting prevention. The stragglers come find you instead of you having to find them. Watching a stuck enemy turn and walk around a wall to chase the player is the most satisfying frame this game produces."

**Speaker cues:**
- Pause for a beat on "half" / "the *other* half" — the bimodal nature is the whole point.
- Trace the median-to-mean gap on the dumbbell chart with a cursor or pointer.
- Click forward at "the most satisfying frame this game produces". This line lets the highlights slide pick up the energy.

### Slide 5 — Team highlights (3:30 – 4:00, 30s) — Everyone

**Slide layout:**
- Title: **"Our favorite parts."**
- A 2×2 (or 2×n) grid of small panels, one per team member. Each panel: a screenshot or short looping clip, a name, and a one-line caption.

**How to run it:**
- Each member gets ~6-8 seconds. Strict.
- Practice the order. No "uh, after [name]" hand-offs — go in slide order, left to right, top to bottom.
- Each speaker says a single sentence: "My favorite part is X because Y."
- The last person ends with "and now we'll show you a minute of the game." That single line is the hand-off into Phase 2.

**Suggested highlight prompts** (each member picks one ahead of the rehearsal):
- *"The first time the slash arc revealed a room of waiting enemies and the design read perfectly."*
- *"Building Crimson's clone phase — when a tester picked the wrong clone and the pushback fired, we knew it worked."*
- *"Watching the analytics confirm that slash was 10× faster than every other tool — the data agreed with the feel."*
- *"The portal suck-in transition. Two and a half seconds of choreography that makes every cleared room feel earned."*
- *"Hunt Mode — the moment a stuck enemy turned, walked around a wall, and chased the player."*
- *"Fitting four boss fights with their own arenas and intro cinematics into one game."*

**Speaker cues:**
- Final speaker delivers the hand-off line clearly. Don't trail off. The audience needs to know the slides are done and the live game is starting.
- Cut from slides to the game window at exactly 4:00. Have the game window pre-loaded behind the slides — Cmd-Tab / Alt-Tab is faster than relaunching.

---

## Phase 2 — Live demo (4:00 – 5:00)

A choreographed minute of live gameplay in the current Gold build. The player hits four beats and lands on a clean visual moment. No improvisation past the script — improv eats time and produces dead air if something goes wrong.

### Pre-load setup

- The live build runs in **windowed mode at 1920×1080**, fullscreen on the projector, on the same machine that just played the slides. Pre-launch it before the presentation starts and minimize it.
- Pre-load a **deterministic showcase scene** — either:
  1. A duplicated `ProgressiveRoomGen` scene with a fixed `Random.seed` at room start, so the spawn layout is repeatable and rehearsable; **or**
  2. A built `BaseScene` with hand-placed enemies (3-4 standard, 1 Skitter) at known positions, no traps, no doors.
- The player is full HP, full ammo, mid-room (already past the spawn flash).
- Audio: game audio at moderate level. Mic for the player optional — the visuals carry.

### The four beats (~12 seconds each, 50 seconds of play)

The player's job is to *hit each beat at a watchable pace*. Not fast. The audience is reading the screen, not your APM.

| Beat | Time inside demo | What you do | What it shows |
|---|---|---|---|
| **1. Reveal** | 0:00 – 0:12 | Walk forward into the dark, slash once. The yellow cone reveals the first enemy mid-frame. Slash again to kill it. | The core hook: *light reveals threat*. |
| **2. Toolkit** | 0:12 – 0:25 | Fire two bullets at a second enemy across the room (yellow streak through the dark). Then dash through an incoming enemy bullet — the dash blue trail and the destroyed bullet are both visible. | Two non-slash tools doing their job. Validates that the kit is more than slash. |
| **3. Light Wave** | 0:25 – 0:38 | Hit `L`. The 360° burst lights the entire room. Reveal three more enemies. Mention out loud — or not — that they're all now permanently active. | The big-cost ability and the central tension: *every reveal escalates the threat*. |
| **4. Clear & portal** | 0:38 – 0:50 | Slash and dash through the remaining enemies. Last enemy dies → portal spawns under the player → "+1 ♥" floats up. Walk into portal. Brief room-transition fade. | The endless-mode loop that ties the whole thing together. |

### Optional narration (live player or Speaker A)

If the player narrates while they play, keep it to four lines, one per beat:

1. (during reveal) — *"Every room is pitch black until you swing."*
2. (during toolkit) — *"Bullets, dashes, and flash all light the room."*
3. (during light wave) — *"…and they all wake up everything they touch."*
4. (during portal) — *"Clear the room. Earn the heal. Next room is bigger."*

Better: silent demo. The visuals are louder than narration in a 1-minute window.

### The wrap (4:55 – 5:00)

When the portal triggers and the screen fades, Speaker A says:

> "Thanks for watching."

Cut to a closing slide (or back to the highlights slide) with the team name. Done.

---

## Live-demo risk plan

Live gameplay is the highest-variance part of the rubric. The professionalism criterion explicitly penalizes "long pauses, skipping, or rewinding". Build the safety net.

### Tier 1 — Avoid the failure modes

- **Don't engage a boss live.** Boss fights have phase transitions and cinematics that can desync your timing. Keep the demo in a basic endless room or a hand-built showcase scene.
- **Don't try a hard maneuver.** No tight slash deflects on bullets, no perfect dash dodges through three traps. The demo wants legibility, not flex.
- **Don't aim for a "wow" moment that you hit 1 in 5 times.** Pick beats that work 5 in 5 times.
- **Test on the actual presentation hardware.** Frame pacing on the projector laptop matters; if it's a different machine, rehearse on it.

### Tier 2 — Survive a problem

- **If you die during the demo:** Press space (death screen → restart), pick up at the next beat. Total cost ~2 seconds. Don't apologize, don't explain, just keep going.
- **If you can't find an enemy:** Use Light Wave. It will be wasted out of script order, but the room is now lit and you can finish.
- **If the room soft-locks** (rare but possible — a stuck enemy in pre-Hunt-Mode geometry could happen): trigger a Light Wave to hunt-activate, slash through, recover.
- **If the build crashes:** cut to the fallback video. See Tier 3.

### Tier 3 — Fallback video

Record a **clean 50-second take of the four beats** before showcase day and have it ready as a video file on the desktop, named `live-demo-fallback.mp4`. If the live build crashes or the input dies, Speaker A says "let me show you a recording" and plays the file. The audience won't know the difference.

This fallback is *also* what you'd ship if the live demo plan gets too risky for a particular venue. It costs nothing to record and 30 seconds to plan around.

---

## Production checklist

### Before recording day

- [ ] Build the **showcase scene**: duplicate `ProgressiveRoomGen` or build `BaseScene` with the deterministic enemy layout described above. Save it as `Assets/Scenes/ShowcaseLiveDemo.unity`.
- [ ] Build a separate executable / build profile that **launches directly into the showcase scene** so there's no main-menu navigation to eat time.
- [ ] Capture the looping clip for Slide 1 (12-15 seconds) — slash → reveal → kill → walk back into the dark, on loop. Embed in the slide.
- [ ] Capture all six before/after clip pairs for the issue slides (see table in `presentation.md` — same shot list applies).
- [ ] Record `live-demo-fallback.mp4` — 50 seconds of the four-beat sequence, clean. Keep it on the presentation laptop's desktop.

### Slide assets to drop in

All charts already live at `docs/analytics/`:

- `01_kill_method_mix_by_day.png` → Slide 2
- `03_ttk_mean_vs_median.png` → Slide 4
- `04_damage_chain_distribution.png` → Slide 3
- `05_per_run_resource_collapse.png` → Slide 2

### Day-of presentation

- [ ] Slides loaded and on-screen 60 seconds before start. Slide 1's clip is autoplaying on a loop.
- [ ] Live build pre-launched, minimized, on the showcase scene with full HP and ammo.
- [ ] Mouse / keyboard at the lectern. Test input *before the audience walks in*, not when you're handed the keyboard.
- [ ] Backup laptop with the same files, including `live-demo-fallback.mp4`.
- [ ] Phone with a stopwatch on the lectern, set to count up. The 4:00 mark is when slides end. Glance only.

### Rehearsal targets

Run the full 5:00 at least four times before showcase. The live minute eats more rehearsal than slides do.

- **Rehearsal 1:** Slides only. Time each speaker. Trim filler words, not content.
- **Rehearsal 2:** Slides + handoff to live demo (no actual play). Make sure the cut at 4:00 is clean.
- **Rehearsal 3:** Full run including the live minute. Don't redo if the demo runs long — just note it.
- **Rehearsal 4:** Full run with deliberate failure injection — at one point during the live minute, simulate dying or losing the enemy. Practice recovering on script.

### Speaker assignment

A common four-person split:

| Role | Slide | Why |
|---|---|---|
| Speaker A | Slide 1 + closing wrap | Anchors the start and the end |
| Speaker B | Slide 2 (Issue #1) | The longest analytics story, give it the most-prepared speaker |
| Speaker C | Slide 3 (Issue #2) | Damage / i-frames is the most concrete, easiest to land |
| Speaker D | Slide 4 (Issue #3) | Hunt Mode + A\* — give it whoever wrote the pathfinding |
| Player (you) | Live demo | The person who plays the build best |

If you are the live player and *also* a slide speaker, take Slide 5's hand-off line (final highlight + "and now we'll show you a minute of the game") so you smoothly walk to the keyboard while talking.

## What we are *not* doing

- No opening video. The intro footage is the looping clip on Slide 1.
- No reading chart numbers aloud. The speaker says the *takeaway*, the chart shows the *evidence*.
- No live boss fight. The 1-minute window can't absorb a phase-2 cinematic plus recovery if the player whiffs.
- No live menu navigation. Pre-load the showcase scene; show the game playing, not the title screen.

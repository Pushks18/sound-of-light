# Presentation Plan — Sound of Sight Gold Showcase

5-minute slot. Two phases: a 1:50 opening video that establishes the game, then a 3:10 slide walkthrough of the analytics-driven changes and team highlights. The whole presentation is built so a viewer who has never seen the game understands the pitch by 0:30 and the analytics story by 4:30.

This document is the build sheet. It tells the team what footage to capture, what slides to build, and what to say on each beat.

## Master timing

| Time | Phase | Content | Speaker |
|---|---|---|---|
| 0:00 – 1:50 | Video (silent or with subtle music bed) | Curated game footage with on-screen captions | (no speaker — let the game breathe) |
| 1:50 – 2:15 | Slide 1 | Game pitch + one-line evolution | Speaker A |
| 2:15 – 3:00 | Slide 2 | Issue #1 — Players never use anything but slash | Speaker B |
| 3:00 – 3:45 | Slide 3 | Issue #2 — A single bad frame could take 9 HP | Speaker C |
| 3:45 – 4:30 | Slide 4 | Issue #3 — Stragglers hid and wasted player time | Speaker D |
| 4:30 – 5:00 | Slide 5 | Team highlights — one favorite each | Everyone |

If the team is smaller than four, collapse Speaker assignments accordingly: one person can do A + final highlight; another can do two adjacent issue slides.

## Pace and tone

- **Slow is good.** Every analytics slide gets 45 seconds — that is the pace, not a stretch. Don't hurry through the chart.
- **Read the room, not the script.** The script below is at conversational speed (~150 words/min). If you finish a beat early, hold on the slide for two seconds before clicking forward.
- **Let the video speak first.** No voice-over for the first 1:50. The footage and on-screen captions carry it.
- **Keep slides sparse.** Each slide is one chart, one before/after pair, one headline. The speaker is the narrator; the slide is the evidence.

---

## Phase 1 — Opening video (0:00 – 1:50)

The video plays without a presenter speaking. Background audio is either ambient game audio at low volume or a soft music bed. Captions appear and dissolve to introduce each beat.

**Aspect ratio:** 16:9, 1920×1080, encoded at 30 fps. Use OBS or Unity's recorder package to capture.

### Segment 1 — Atmosphere (0:00 – 0:20, 20s)

- **Show:** Player standing still in a pitch-black endless room. Only the small ambient glow around the player is visible. Player takes a few slow steps. The minimap radar fades in at 0:08 to hint that the world extends past the screen.
- **Caption (0:00 – 0:08):** *"In Sound of Sight, every room is dark."*
- **Caption (0:10 – 0:18):** *"You have no flashlight."*
- **Capture notes:** Pick a generated room with empty space directly in front of the player. Keep movement deliberate — no panicked WASD spam.

### Segment 2 — First reveal (0:20 – 0:40, 20s)

- **Show:** Player slashes (J). The yellow light cone reveals an enemy mid-frame. Enemy activates (red mark light flashes) and starts chasing. Player slashes again, killing the enemy.
- **Caption (0:22 – 0:30):** *"Light is your weapon — but it's also what wakes up the threats."*
- **Caption (0:32 – 0:38):** *"Once an enemy sees light, it stays active forever."*
- **Capture notes:** Frame the slash so the enemy is visibly in the cone the moment it's revealed. Don't let the enemy close to body distance — keep the reveal moment readable.

### Segment 3 — Combat toolkit (0:40 – 1:00, 20s)

A quick three-beat showcase of the rest of the kit. ~6 seconds each.

- **0:40 – 0:46:** Player fires bullets (K). The warm-yellow bullet light streaks through the dark.
- **0:46 – 0:52:** Player dashes (Shift) through an incoming enemy bullet — destroyed mid-flight, blue-white shadow afterimage trail visible.
- **0:52 – 1:00:** Player triggers Light Wave (L). The 360° burst lights up the entire room and reveals four enemies waiting in the dark.
- **Caption (0:42 – 0:50):** *"Shoot. Dash. Flash."*
- **Caption (0:52 – 1:00):** *"Every attack permanently escalates the threat."*

### Segment 4 — Boss montage (1:00 – 1:30, 30s)

7-8 seconds per boss. Pick the most cinematic moment of each fight.

- **1:00 – 1:08 — Scarab.** Charge attack: warning rectangle telegraphs the lunge, Scarab dashes 22 m/s across the arena, player dodges. Show the white-flash armor deflect on a slash so the "back is the weak point" is visually obvious.
- **1:08 – 1:16 — Umbra.** Phase 2 berserk: zig-zag rush at the player, 14-bullet omniburst on a turn. The screen is pure motion.
- **1:16 – 1:24 — Vesper.** Player triggers Light Wave; Vesper takes damage; Vesper teleports to a dark spot; eyes fade in at the new location. The light-locked vulnerability sells in one beat.
- **1:24 – 1:32 — Crimson.** Clone phase: Crimson teleports to center, 5 identical clones spread into the rotating ring. Player slashes one — wrong clone — pushback fires (player gets knocked back, brief lockout). Player picks the right one on the second try; the ring detonates.
- **Caption (1:00 – 1:04):** *"Four bosses. Each demands a different read."*

### Segment 5 — Endless mode loop (1:30 – 1:50, 20s)

- **1:30 – 1:38:** Last enemy in a room dies. Portal appears, brightens the room, "+1 ♥" floats above the player.
- **1:38 – 1:46:** Player walks into portal. Suck-in animation: player shrinks and spins down. Screen fades to black.
- **1:46 – 1:50:** Fade in on a new, larger room. "Room 6" caption appears top-center, then dissolves. Spawn flash fires, enemies revealed in the safe zone.
- **Caption (1:32 – 1:40):** *"Clear the room. Earn the heal."*
- **Caption (1:42 – 1:50):** *"Then the next room is bigger."*

---

## Phase 2 — Slides (1:50 – 5:00)

Each issue slide has the same anatomy: a one-line problem headline, the analytics chart that surfaced it, a before/after gameplay clip pair, and the change shipped. Build them with the same template so the audience can pattern-match across the three.

### Slide 1 — Game pitch + evolution (1:50 – 2:15, 25s) — Speaker A

**Slide layout:**
- Title block: *Sound of Sight*
- One-line tagline: *Every attack you make permanently escalates the threat.*
- Right side: a small four-panel mini-timeline labeled **March → April → Beta → Gold**, each panel a tiny screenshot (procedural rooms → bosses → analytics → polish).

**Speaker notes (~60 words, conversational):**

> "What you just saw is *Sound of Sight* — a 2D top-down action game built in Unity where the world is dark and your combat abilities are the only thing that produces light. We started with the core loop in March, shipped procedural rooms and four bosses through April, and then ran a closed beta. The next three slides are the three biggest things that beta data taught us — and what we shipped because of it."

**Speaker cues:**
- Land on "biggest things that beta data taught us" right at 2:13 so you click forward at 2:15.
- Hand off to Speaker B with eye contact, not a verbal "now [name] will…".

### Slide 2 — Issue #1: Players never use anything but slash (2:15 – 3:00, 45s) — Speaker B

**Slide layout:**
- Title: **"Issue 1: 50% of runs never fire a single bullet."**
- Top-left chart: `05_per_run_resource_collapse.png` (the right panel — 25% pure-slash, 50% never shoot, 50% never dash, 95% never trap).
- Top-right chart: `01_kill_method_mix_by_day.png` (slash bar on every day).
- Bottom: side-by-side gameplay clip — **Before** (early beta build, player kills 5 enemies in a row by mashing J) — **After** (current build with Skitter, player swaps to bullets to kill the resistant Skitter, then back to slash for basic enemies).

**Speaker notes (~115 words):**

> "Our beta data showed that 82.8% of all kills came from one ability: slash. We thought that was just preference. Then we looked at the per-run breakdown — half of all runs never fired a single bullet, half never used dash, and a quarter scored 100% of their kills with slash. That's not preference. That's the resource economy collapsing — players ignored the entire toolkit. The data also told us why: slash kills in 0.22 seconds; bullets and dashes take 6-to-10 times longer because they have travel time. So we couldn't nerf slash globally without breaking combat. Instead, we built the Skitter — an enemy with slash resistance — plus a clone-phase that punishes slash spam. After shipping it, bullet usage against the Skitter doubled."

**Speaker cues:**
- Pause for 2 seconds after "82.8% of all kills came from one ability" — let the headline land before naming it.
- Point at the right-panel collapse chart when you say "half of all runs never fired a single bullet". The 50% bar is the load-bearing visual.
- Click forward at "bullet usage against the Skitter doubled" — this is the punchline and the natural transition.

### Slide 3 — Issue #2: A single bad frame could take 9 HP (3:00 – 3:45, 45s) — Speaker C

**Slide layout:**
- Title: **"Issue 2: Fatal damage chains were hit-stacks, not long fights."**
- Center chart: `04_damage_chain_distribution.png` — fatal vs survived damage histogram with the 3 HP threshold line.
- Right: side-by-side gameplay clip — **Before** (player walks into two overlapping enemy bullets and a contact hitbox simultaneously, takes 6 damage on one frame, dies instantly) — **After** (same scenario, 0.5s i-frames absorb the second and third hit, player takes 1 damage and dashes out).

**Speaker notes (~115 words):**

> "Of a thousand damage interactions in beta, the chains players survived averaged 1.5 damage. The chains that killed them averaged 2.8 damage — almost double. But here's the thing: the durations were the same. Fatal chains weren't longer engagements where the player took multiple hits over time. They were single-frame multi-hits — overlapping bullets and contact hitboxes dealing damage on the same physics tick. The worst case we recorded was 9 damage in one chain. The player only has 3 HP. So we added 0.5-second invincibility frames after every hit. That same 9-damage chain now lands as 1 or 2 damage, because every hit after the first lands inside the i-frame window and gets absorbed."

**Speaker cues:**
- The 3 HP dotted line on the chart is the visual anchor — point at it when you say "the player only has 3 HP".
- Sync the before/after clip: the "before" half should be playing during the problem statement, the "after" half during the solution.
- Click forward at "gets absorbed".

### Slide 4 — Issue #3: Stragglers hid and wasted player time (3:45 – 4:30, 45s) — Speaker D

**Slide layout:**
- Title: **"Issue 3: Half of enemies died instantly — the other half hid for 30 seconds."**
- Center chart: `03_ttk_mean_vs_median.png` (dumbbell chart — every enemy slot has a 2-5× gap).
- Right: side-by-side gameplay clip — **Before** (player clears 4 of 5 enemies in a room; the 5th is stuck behind a wall corner and the player wanders for 20 seconds looking for it) — **After** (same 5-enemy room, last 3 enemies switch to A* pathfinding the moment the count drops to 3, they actively hunt the player).

**Speaker notes (~115 words):**

> "When we plotted the mean versus the median time-to-kill for every enemy spawn slot, every single one had the same shape: half of all enemies died in 0.4 seconds — that's the one-slash one-kill we designed for — but the *other* half survived 5 to 30 seconds. The reason is mechanical: the moment an enemy escapes the slash arc, it loses line-of-sight and chases at base speed with no pathfinding, so it gets stuck on geometry. We added Hunt Mode: when 3 or fewer enemies remain, every survivor force-activates and switches to A-star pathfinding. The stragglers come find you instead of you having to find them. The long tail of those room-clear times closes."

**Speaker cues:**
- Pause briefly on "half" / "the *other* half" — the bimodal nature is the whole insight.
- Trace the gap on the dumbbell chart with your cursor / pointer.
- Click forward at "closes".

### Slide 5 — Team highlights (4:30 – 5:00, 30s) — Everyone

**Slide layout:**
- Title: **"Our favorite parts."**
- A 2×2 (or 2×n) grid of small panels, one per team member. Each panel: a screenshot or short looping clip, a name, and a one-line caption.

**How to run it:**
- Each team member gets ~6-8 seconds. Strict.
- Practice the order ahead. No "uh, after [name]" hand-offs — go in slide order, left to right, top to bottom.
- Speak from the panel: "My favorite part is X because Y." That's it. One sentence each.

**Suggested highlight prompts** (each member picks one before the practice run):
- *"The first time I saw the slash arc reveal a room of waiting enemies and realized the design read perfectly."*
- *"Building the Crimson clone phase — the moment a tester picked the wrong clone and got knocked back, we knew it was working."*
- *"Watching the analytics tell us our slash was 10× faster than anything else — the data confirmed what the game already felt like."*
- *"The portal suck-in transition. Two and a half seconds of choreography that makes every room feel earned."*
- *"Hunt Mode — the moment we shipped A-star and a stuck enemy turned and walked around a wall to chase the player."*
- *"Fitting four boss fights with their own arenas and intro cinematics into one game."*

**Speaker cues:**
- The last person ends on the team name or "Thank you." Land it cleanly — silence is fine.
- Don't run over. If you hit 5:00, stop. The grader is timing.

---

## Production checklist

### Before recording day

- [ ] Build a "showcase" save / scene sequence: pre-loaded endless room with deterministic enemy placement that matches the video script.
- [ ] Pick one beta build (~2026-04-19) to capture **before** clips from. Keep it on a separate branch / build folder so you don't have to revert.
- [ ] Pick the current Gold build for **after** clips.
- [ ] Confirm OBS capture settings: 1920×1080, 30 fps, NVENC or VideoToolbox, MP4 container.
- [ ] Confirm slide tool: PowerPoint / Keynote / Google Slides — pick one and stick with it. Embed video clips, don't link them.
- [ ] Decide audio: ambient game audio only, or one music track at -18 LUFS so voice-over (slides 2-5) sits on top.

### Footage to capture (in this order)

For each item, capture two takes minimum.

| # | Clip | Length | Source build |
|---|---|---|---|
| V1 | Atmosphere walk in dark | 25s | Gold |
| V2 | Slash reveals + activates an enemy | 25s | Gold |
| V3 | Shoot bullet | 8s | Gold |
| V4 | Dash through enemy bullet | 8s | Gold |
| V5 | Light Wave reveals room | 10s | Gold |
| V6 | Scarab charge attack | 10s | Gold |
| V7 | Umbra Phase 2 berserk | 10s | Gold |
| V8 | Vesper teleport on light hit | 10s | Gold |
| V9 | Crimson clone phase + pushback + correct hit | 12s | Gold |
| V10 | Endless room clear + portal + Room X transition | 22s | Gold |
| BA1 | Slash-spam clear (5 enemies, all J) | 12s | Pre-Skitter beta build |
| BA2 | Skitter forces bullet swap | 12s | Gold |
| BA3 | Multi-hit overlap kills player on one frame | 8s | Pre-i-frame build |
| BA4 | Same scenario, i-frames absorb stack | 10s | Gold |
| BA5 | Last enemy stuck behind wall, player wanders | 18s | Pre-Hunt-Mode build |
| BA6 | Last 3 enemies A* hunt the player | 15s | Gold |

### Slide assets to drop in

All charts already live at `docs/analytics/`:

- `01_kill_method_mix_by_day.png` → Slide 2
- `03_ttk_mean_vs_median.png` → Slide 4
- `04_damage_chain_distribution.png` → Slide 3
- `05_per_run_resource_collapse.png` → Slide 2

### Day-of presentation

- [ ] Slides loaded and on-screen 60 seconds before start.
- [ ] Video pre-rolled to 0:00 in a separate window. First click is "play video, full-screen".
- [ ] Speaker order finalized. Each speaker has their one slide / one beat memorized — don't read off the slide.
- [ ] One backup laptop with the same files, in case the primary fails.
- [ ] Phone with a stopwatch on the lectern, set to count up. Glance only — don't stare.

### Rehearsal targets

- Run the full 5:00 at least three times before showcase.
- The first rehearsal will likely run 5:30+. Trim filler words, not content.
- Mark a hard cut at each slide boundary. If you reach 4:30 and you haven't started the highlights slide, skip directly to it.
- Time each speaker individually. The 45-second issue slides are tight — practice them on a stopwatch until they land at 42-46 seconds reliably.

## What we are *not* doing

- No voice-over during the opening 1:50 video. The footage carries it; talking over it dilutes both signals.
- No reading the chart numbers aloud. Speaker says the *takeaway*, the chart shows the *evidence*.
- No live demo. The risk of a crash or a soft-locked enemy is too high. Pre-recorded clips only.
- No team-credit slide. The highlights slide is the credit — every member gets to be the face of one moment.

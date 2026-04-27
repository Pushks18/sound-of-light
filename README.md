# Sound of Sight

A 2D top-down action game built in Unity where darkness is your default state and light is a weapon. Every room is pitch black. Your combat abilities emit light that reveals enemies — but once an enemy sees light, it stays active forever. The core tension: every attack you make permanently escalates the threat.

## Concept

The world is dark. You have no flashlight — only your combat abilities produce light. A faint ambient glow lets you see your immediate surroundings, but the rest is black. Slashing, dashing, shooting, and light waves all emit light that reveals and activates enemies. Once activated, enemies chase you relentlessly, even in total darkness. The core loop is: explore cautiously, engage strategically, and manage the permanent consequences of every action you take.

## Game Modes

### Tutorial (FinalTutorial / NewTut)
Guided onboarding with checkpoint gates that pulse the ambient light brighter so new players can read the room while learning slash, shoot, dash, and flash. Includes scripted text triggers, an invincibility window for the first encounter, and a procedurally laid-out tutorial dungeon.

### Story Mode (GameScene + Level1–Level9)
Hand-crafted levels with keys, doors, and fixed enemy layouts. Kill all enemies or reach the exit door with the key to win. Numbered Level scenes are a chained level progression for the campaign mode.

### Endless Mode — Progressive (ProgressiveRoomGen)
Procedurally generated rooms with scaling difficulty. Rooms start small and intimate (30x22 tiles, 3 enemies) and grow larger with more enemies each floor. There are no doors — the only way forward is to clear all enemies and enter the portal.

**Boss room interleaving:** Every `bossRoomInterval` rooms (default 5), the run hands off to a dedicated boss scene drawn without replacement from a shuffled pool: `ScarabScene`, `VesperScene`, `GoblinBossScene` (Crimson), `IsshinBossScene` (Umbra). The pool reshuffles when emptied, so all four bosses are seen before any repeat. After a boss is defeated, the run resumes at the next dungeon room with HP and room index restored.

**Progressive scaling per room:**

| Room | Grid Size | Enemies | Traps |
|---|---|---|---|
| 1 | 30x22 | 3 | 1 |
| 2 | 34x25 | 4 | 1 |
| 3 | 38x28 | 5 | 2 |
| 5 | 46x34 | 7 | 3 |
| 10 | 65x45 | 12 | 5 |
| Cap | 65x45 | 20 | 12 |

**Between-floor healing:** Players heal HP between rooms. Healing decreases as rooms progress (3 HP on room 2, 2 HP on room 3, 1 HP thereafter) to maintain pressure.

**Spawn flash:** On entering each room, a flash fires at the player's position, illuminating the immediate area. No enemies spawn within the flash radius, giving the player a safe zone to orient.

**Idle auto-flash:** If no input is detected for 4 seconds, a free mini-flash fires automatically (60% radius, 50% intensity, blue-white tint, no ammo cost). Helps stuck or idle players without replacing strategic flash usage. Disabled during portal/death sequences.

**Enemy spacing:** Enemies are spawned with a minimum distance of 4 units between each other to prevent clustering. Combined with the spawn flash safe zone, this ensures fair initial distribution.

**Trap placement rules:** Traps use a scoring system that prefers strategic locations:
- Corridors (3-4 floor neighbors) score highest — hardest to dodge.
- Open areas score lowest — too easy to avoid.
- Minimum 4-unit spacing between traps, 3-unit distance from enemies, 5-unit distance from player spawn.
- All rules are configurable in the Inspector.

### Endless Mode — Preset (RoomGenScene)
Loads from pre-generated room presets with fixed enemy counts. Original endless mode before progressive scaling was added.

## Boss Fights

Four hand-tuned bosses live in their own scenes and are reachable from the endless run (or playable directly). Each has a custom arena, intro camera pan, two-phase health curve, and a unique vulnerability rule. All bosses share `BossHealthBar` for the on-screen HP bar and `BossIntroCam` / their own `*ArenaTrigger` to gate the encounter.

### Scarab (ScarabScene)
- **HP:** 30 (Phase 2 at ≤ 15).
- **Armor:** Front-facing carapace deflects all damage and bounces the player. The back is the weak point.
- **Charge attack:** 0.8s tell with a warning rectangle, then a 22 m/s sweep up to 20 units. Deals 2 damage. Phase 2 charges are shorter-range but harder-hitting (3 damage, can wall-stun for 3s).
- **Phase transition:** Cinematic shockwave knocks the player back 4 units; brief landing pause.
- **Entrance:** Fly-in cinematic — Scarab descends onto the arena from above.

### Umbra (IsshinBossScene)
- **HP:** 250.
- **Slash arcs:** Short-range arcs in Phase 1 (radius 9, 220° cone). Phase 2 widens them to a near-full arena sweep (radius 30, 280° cone) on a slower beat.
- **Bullet pressure:** Rapid bullet fire at range plus an 8-bullet idle burst every 5s.
- **Berserk:** Every 5s, Umbra dashes the player at high speed in a zig-zag, firing 14-bullet omnibursts on each turn. Contact during berserk hurts.
- **Phase 2 (≤ 40% HP):** Periodic full-arena sweep — only a well-timed dash escapes it.

### Vesper (VesperScene)
- **HP:** 15 (Phase 2 at ≤ 8).
- **Light-locked vulnerability:** Vesper only takes damage while illuminated by the player's Light Wave. Bullets count for 0.5 dmg, slashes for 2 dmg.
- **Teleport hunt:** Vesper teleports to dark spots far from the player. Phase 2 chains 3 teleports per cycle and tightens the dark-radius requirement.
- **Scatter burst:** Fires 10–14 bullets in an omni-burst every 4s (Phase 2: 3s) and on every illumination event.
- **Forced retreat:** Cumulative slash damage, repeated bullet hits, or melee proximity force a teleport — staying close is rewarded but can't pin Vesper.
- **Phase 2 cinematic:** Player input freezes, the camera pans, eyes shift red, and Vesper performs a triple-zigzag teleport before resuming.
- **Death sequence:** Five fading teleports with candle-flicker eyes before the portal spawns.

### Crimson (GoblinBossScene)
- **HP:** 75 (Phase 2 at ≤ 30).
- **No light gate — always hittable.**
- **Chase + scatter:** 3.5 m/s chase (5.5 m/s in Phase 2). 8-bullet omni-bursts every 12s (Phase 2: 14 bullets every 8s).
- **Minions:** Spawns up to 4 minions (Phase 2: 6) on a 6s timer.
- **Teleports:** Fades out and lands at a corner or a position away from the player every 8–10s (4–6s in Phase 2).
- **Clone Phase (signature mechanic):** Starting 35s into the fight and every 45s after, Crimson teleports to the arena center, becomes immune, and spawns 5 identical clones that orbit on a 4.5-unit circle at 40°/sec. The real Crimson is one of the orbiters — find and hit it to collapse the ring. All other clones detonate and the fight resumes. Slash spam during clone phase triggers a knockback lockout (7 slashes in 2s → 18-unit pushback, 0.6s movement lockout) to discourage flailing.
- **Visuals:** Red body with red eyes/arms; clones use the same silhouette so reading positions matters more than reading sprites.

## Controls

| Input | Action |
|---|---|
| WASD | Move + Aim (8 directions, last direction persists) |
| J | Slash — 120-degree melee arc, deflects enemy bullets |
| K | Shoot — ranged bullet with travelling light |
| Left Shift | Dash — fast burst, deals 2 damage, destroys bullets |
| L | Light Wave — room-wide light burst |
| Esc | Pause / resume — opens the pause menu |

## Core Mechanics

### Resource Systems

#### Light Energy
- Unified energy pool (max 500) powers most abilities.
- Regenerates at 1 energy/second after a 1.5-second cooldown from last use.
- Shooting, dashing, and flashing draw from this pool. Slash is free (cooldown-only).

#### Ammo
- Discrete ammo tracked by `PlayerAmmo`:
  - **Bullets:** 15 max (consumed by Shoot)
  - **Dashes:** 5 max (consumed by Dash)
  - **Flashes:** 5 max (consumed by Light Wave)
- Each ability requires both sufficient ammo AND energy. Both are checked before either is spent.
- **Per-ability regen:** Each resource regenerates independently after a delay from its last use:
  - **Bullets:** 5 seconds after last shot, then +1 bullet every 0.5 seconds.
  - **Dashes:** 8 seconds after last dash, then +1 dash every 2 seconds.
  - **Flashes:** 8 seconds after last flash, then +1 flash every 2 seconds.
- Using an ability resets that ability's regen timer. Regen is not tied to standing still — it happens regardless of movement.

### Slash (J)
- 120-degree melee arc in your aim direction.
- Produces a visible light cone (with shadows) and a semi-transparent mesh arc showing exact range and angle, with an edge outline.
- Damages enemies caught in the arc (2 damage per hit). Line-of-sight raycast prevents damage through walls.
- **Deflects enemy bullets** — any enemy bullet entering the slash arc during its duration is destroyed with a blue-white spark effect.
- **Always available** — cooldown-only (0.15 seconds), no energy or ammo cost. This is the player's primary defense and is never resource-gated.

### Shoot (K)
- Fires a bullet in your aim direction.
- **Bullets emit a warm yellow light** (unlit sprite, always visible) that illuminates surroundings as they travel. A child "LightSource" trigger follows each bullet to activate nearby enemies.
- Bullets pass through triggers (traps, doors, keys) and only stop on solid walls or enemies.
- On wall impact, spawns an impact echo light offset away from the wall surface using a surface-normal calculation.
- Cost: 1 energy + 1 bullet per shot.

### Dash (Left Shift)
- Fast burst of movement in your aim direction (works while stationary).
- Leaves a trail of fading light orbs that shrink and dim over time.
- Spawns shadow afterimages of the player that fade to transparent.
- **Deals 2 damage** + 0.2 second stun to enemies on contact (once per enemy per dash). Uses line-segment sweep detection to catch enemies along the entire dash path.
- **Destroys enemy bullets** on contact during the dash.
- **Dodges traps** — a 0.15-second delay on trap damage allows a full dash to pass through unharmed.
- Cost: 3 energy + 1 dash. Cooldown: 1 second.

### Light Wave (L)
- Emits a large 360-degree light burst (radius 12) centered on the player.
- Fades from intensity 2.5 to 0 over 5 seconds, activating all enemies in the area.
- Use it to reveal an entire room — at the cost of waking everything up.
- Cost: 10 energy + 1 flash. Cooldown: 20 seconds. Cooldown shown on the StatusHUD as a 10-segment progress bar with remaining seconds.

### Ambient Light
- A small, always-on dim glow around the player (radius 1.5, intensity 0.4, cool blue).
- Lets you see walls and nearby objects without activating enemies (not tagged as "LightSource").
- Does not cast shadows to avoid visual artifacts at wall seams.

### Enemies

#### Standard Enemy
- Enemies are **dormant in the dark**. They don't move or shoot until light touches them.
- **Once activated by any "LightSource"-tagged light, enemies stay active permanently** — they chase at base speed (2) even after the light is gone. While actively in a light source, they move at boosted speed (8).
- This is the central design tension: **every attack you make has permanent consequences**. Light reveals threats but also creates them.
- **Hunt mode:** When 3 or fewer enemies remain in a room, all surviving enemies activate and switch to A* pathfinding — they navigate around walls to reach the player instead of getting stuck on corners. Even dormant (unlit) enemies are force-activated. The threshold is configurable per scene.
- **Enemies shoot immediately on activation** (shoot range: 10 units, cooldown: 1.2 seconds).
- **Enemy bullets emit a faint red light** — always visible in the dark.
- Enemies **fade in opacity as they take damage** (quadratic curve: full -> faded -> transparent death).
- On death, enemies **fade out over 0.3 seconds** using unscaled time (works even during timeScale = 0).
- During the death fade, AI and shooting are disabled and the collider is removed.
- Stunned enemies can't move or shoot for the stun duration.
- Enemies have a **mark light** (small red glow) that activates briefly when hit.
- Enemies have a **proximity glow** that intensifies as the player approaches (max distance 8 units). Player reference is re-acquired if lost.
- **Floating damage numbers** appear above enemies when they take damage — red-orange text that floats upward and fades out.
- **Mini health bars** appear above enemies when hit or illuminated — green/yellow/red fill with smooth fade-in/out.

#### Skitter (fast ranged enemy)
- Red triangle that activates on **player proximity (15 units)** OR on a `LightSource` trigger entering its 5-unit sensor radius.
- **Movement:** 3 m/s while engaging, 6 m/s panic-flee from active light sources (repulsion scales with inverse distance).
- **Shooting:** Fires every 3s at 12 units. Inside `closeShootRange` (≤ 5 units) the cooldown drops to 0.8s — closing the gap doesn't make Skitters safer.
- **Slash resistance:** A `slashDamageMultiplier` on `EnemyHealth` reduces slash damage against Skitters; bullets, dashes, and traps still hit normally. The check normalizes the last damage method, so the resistance is reliable.
- **Spawning:** A configurable fraction of the per-room enemy budget (`skitterFraction`) is allocated to Skitters in Endless Mode. They spawn outside the spawn-flash safe zone like other enemies.
- **Movement collision:** Continuous collision detection + 1.4-unit wall lookahead prevents the high-speed Skitter from clipping through corners.

### Traps
- Traps are invisible until revealed by a light source.
- **Dormant:** Hidden, no damage. Light from abilities (slash, dash, light wave, bullets) reveals them. Only lights with intensity > 0 trigger reveal (filters out the idle muzzle flash).
- **Arming (2 seconds):** Once revealed, a yellow light fades in over 2 seconds. Only one reveal can trigger per trap (duplicate light sources on the same frame are guarded against).
- **Armed:** Light turns red and pulses (sinusoidal oscillation). Contact now deals 1 damage.
- Traps can be **dodged by dashing** — a 0.15-second delay on damage allows dashes to pass through.
- Traps deal repeated damage if the player stands on them (with 2-second per-entity cooldown between hits).
- Optional light burst on damage (radius 3, tagged "LightSource") that can wake nearby enemies.
- Affects both players and enemies.
- Supports a `startArmed` flag for traps that should be active from the start (used in tutorial).
- All trap coroutines use real time (`WaitForSecondsRealtime` / `Time.unscaledDeltaTime`) so they complete even during victory/death pause.

### Combat
- Player has **3 HP**, enemies have **2 HP**.
- Both player and enemies fire projectiles.
- Player bullets: warm yellow light, always visible, illuminate surroundings.
- Enemy bullets: faint red light, always visible in the dark.
- Bullets are triggers (no physics knockback). They ignore other trigger colliders (traps, doors, keys, light sources).
- Bullets produce a faint muzzle flash and an impact echo light on wall impact (offset from the wall surface along the collision normal).
- **Slash deflects enemy bullets** with a blue-white spark visual effect (spark textures and materials are cached to avoid per-deflect allocations).
- **Dashing destroys enemy bullets** on contact.
- **Floating damage numbers** appear on both player and enemy hits.
- **Player invincibility frames:** 0.5-second i-frame window after taking damage prevents multi-hit scenarios.
- **Damage flash:** Player sprite flashes red 3 times when hit (uses real time so the flash completes even during pause).
- **Victory invulnerability:** Player cannot take damage once the game has ended (prevents stray hits during the victory sequence).

### Health Pickups
- Endless mode can drop **health packs** — green pulsing orbs (1.5-unit radius, ~2 Hz pulse) visible in the dark.
- Walking onto a pack restores **+1 HP** with a brief green flash burst (3-unit radius, 0.5s) to confirm the heal.
- Packs only consume on contact when the player is below max HP, so a full-HP player can ignore them.
- Health packs register themselves so the minimap can render them alongside enemies and the portal.

### Room Clear Rewards
- When a portal spawns at room clear, **floating reward popups** rise above the player to communicate what the next room grants.
- `+X ♥` (green): the heal that will be applied on the next room load. Heal tier scales with floor (clamped 1–3) and is capped to actual missing HP — no popup if the player is already full.
- `+Max Ammo` (yellow): bullet pool refills to max on next room load. Hidden if bullets are already topped off.
- Popups pop in (1.5x → 1x) over 0.12s, drift upward at 0.85 u/s, and fade out around 1.4–2.4s.

### Keys & Doors
- Collectible keys are placed in the level. Walking over a key picks it up and it follows the player visually (smooth lerp with offset).
- The door (exit) requires the matching key (default: "RedKey"). Walking onto the door with the correct key triggers a win.
- Walking onto the door without the key shows a "You need a key to open this door" message.

### Win Conditions
- **Story mode:** Kill all enemies OR reach the door with the key.
- **Endless mode:** Kill all enemies to spawn a portal. The portal sucks the player in and loads the next room.
- **Victory sequence (story):** A global Light2D fades in from 0 to full intensity over 1 second, illuminating the entire room. Then the game pauses and the win screen appears.

### Room Clear Portal (Endless Mode)
When all enemies in a room are defeated:
1. **Appear (0.35s):** Portal scales up from zero directly under the player. Three spinning ellipse rings + pulsing point light.
2. **Brighten (0.5s):** A global Light2D illuminates the entire room so the player can see the cleared space.
3. **Suck-in (0.65s):** Player shrinks and spins down into the portal with an accelerating curve. Portal glow intensifies.
4. **Fade out (0.45s):** Screen fades to black while portal collapses.
5. **Load (0.4s):** Next room loads during black screen. Player is healed and restored.
6. **Fade in (0.4s):** Screen fades back in on the new room with the spawn flash already active.

Total transition time: ~2.75 seconds.

### Shadows
- Walls block light and cast shadows via `ShadowCaster2D`, auto-configured by `WallShadowSetup` at runtime using reflection to set the private `m_ShapePath` field from each wall's collider shape.
- Wall shadow casters have `selfShadows = false` to prevent shadow artifacts at wall junctions.
- **Impact echo lights** have shadows disabled to avoid seam artifacts when bullets hit near wall joints.
- **Player ambient light** has shadows disabled to prevent thin black lines at wall seams.
- **Bullet lights** and **dash trail lights** have shadows disabled.
- **Slash** and **Light Wave** lights cast shadows for dramatic effect.

### HUD

Three procedurally-built HUD panels — no prefabs or Inspector wiring needed:

- **StatusHUD** (top-left): HP shown as heart pips with color transitions (green/yellow/red), flash cooldown as a 10-segment progress bar, and enemies remaining counter. Shows "All clear!" when all enemies are defeated.
- **PlayerAmmo HUD** (top-right): Bullet, dash, and flash counts shown as pip indicators with keybind labels. Color changes to red when a resource hits 0.
- **KeybindHUD** (top-center): Persistent keybind reference — `[J] Slash  [K] Shoot  [Shift] Dash  [L] Flash` with yellow key labels.

#### Endless Mode HUD (auto-created)
- **Minimap Radar** (bottom-right): Circular radar showing room layout (walls as outline, floor as faint fill), enemy positions (dormant = light pink, activated = bright red), portal (pulsing green dot), and player (white center dot). Room texture scrolls with player movement. Clipped to circle with object-pooled dots.
- **Room Counter** (top-center): Shows "Room X" with fade-in, 3-second hold, and fade-out on each room transition.

### Pause Menu
- `Esc` opens a dark overlay (~85% opacity) that lists current Health, Room number, and enemies remaining, plus the keybind reference. A "Main Menu" button returns to the title scene.
- Disabled during room-clear sequences and game-end states so it can't interrupt cinematics.

### Analytics & Telemetry
- `RunKillAnalytics` tracks kill counts per damage method (slash, shoot, dash, trap, unknown), damage interactions (time + total damage windows), per-room clear times, boss-vs-normal rooms, and the run's final outcome (win/death).
- `SendToGoogle` posts these as well as death positions to a Google Sheets endpoint for play-test telemetry. Each run gets a tick-based session ID.

## Analytics-Driven Changes

We instrumented the game on 2026-04-06 (`b0f5980` "3 Analytics Added") and `2c8d2ad` "All improved analytics added" on 2026-04-19, then iterated on telemetry from a closed beta. Across the 2026-04-06 → 2026-04-27 window we collected **3,961 events** spanning **48 full runs**, **2,509 enemy kills (TTK)**, **1,000 damage interactions**, and **180 room clears**.

The six insights below each pair a plot with the design change it drove. Plots are generated from `/Users/praveen/Downloads/alter-ego beta.xlsx` by `docs/analytics/_make_plots.py`. A long-form companion report with a fully-written-out summary table and per-insight prose lives at [`docs/analytics/README.md`](docs/analytics/README.md).

![Beta telemetry timeline with analytics-driven commits marked](docs/analytics/07_timeline.png)

### 1. Slash is the run, every day, every player → situational anti-slash mechanics
![Per-day kill-method mix](docs/analytics/01_kill_method_mix_by_day.png)
- **Signal:** Slash never falls below **52%** of kills on any beta day, peaks at 97% (2026-04-06), and lands at 82% on the largest sample day (2026-04-21, n=464 kills). Aggregated TTK records confirm: slash 73.5%, bullet 13.4%, dash 12.4%, trap 0.7% across 2,509 kills.
- **Why this can't be explained away:** The pattern survives across days, players, and game states — it isn't novelty, isn't a sample artifact, isn't a single broken session.
- **Change driven:** **Skitter slash resistance** via a `slashDamageMultiplier` on `EnemyHealth` (`3389653`, `ebfee36`, 2026-04-25), and the **clone-phase slash-spam pushback** in `PlayerSlash.cs` (7 slashes in 2s during Crimson's clone phase → 18-unit knockback + 0.6s lockout). Both are surgical — slash stays free everywhere else.

### 2. Slash kills 6-10× faster than the next-fastest tool → why the nerfs are situational, not global
![TTK distribution by player kill method](docs/analytics/02_ttk_by_method.png)
- **Signal:** Median TTK by method (excluding clones): **slash 0.22s**, dash 1.33s, trap 1.97s, bullet 2.14s. Slash isn't just preferred — it's the only tool that finishes in a single attack window. Bullet kills typically take 6× longer; dash kills take 6×; trap kills take 9×.
- **Why this can't be explained away:** This is mechanical kill speed inside the same game state. Aiming isn't slower than slashing — bullets fly at high velocity. The gap exists because slash is the only ability that *connects on the frame it's used*; everything else has flight time, contact time, or terrain dependency.
- **Change driven:** This is the *justification* for keeping slash damage at 2 and slash cost at zero. A global slash nerf would slow combat 6-10× system-wide. The fix is to make slash *situationally worse* (Insight 1) not universally weaker — preserving the fast-kill loop for normal enemies while gating it where it breaks design (Skitter, Crimson clones).

### 3. Every enemy spawn slot has a 2-5× mean-median TTK gap → Hunt mode (A* pathfinding)
![Per-enemy mean vs median TTK](docs/analytics/03_ttk_mean_vs_median.png)
- **Signal:** Across all 14 enemy spawn slots with ≥15 samples, **median TTK is 0.26-0.98s** but **mean TTK is 0.89-2.30s** — a 2-5× gap that appears on every single slot, not just outliers. The aggregate is median 0.37s vs mean 1.28s (3.5× gap, n=1,412).
- **Why this can't be explained away:** This is a distribution-shape signal that holds across enemy IDs, sessions, and players. Half of all enemies die instantly on first slash contact (the 0.26s median); the other half ran out of line-of-sight, hid behind a wall, or pathed into a corner — and survived 5-30 seconds before being finished off. The shape is bimodal, not noisy.
- **Change driven:** **Hunt mode** in `GameManager.cs` / `EnemyAI.cs`: when 3 or fewer enemies remain in a room, all surviving enemies force-activate and switch to **8-directional A\* pathfinding** with diagonal-corner-cutting prevention. The change makes the long tail come find the player instead of getting stuck. The 0.4s recalc interval keeps the pathfinder cheap.

### 4. Half of all runs never fire a bullet — the resource economy collapses per-run, not just on average → idle auto-flash + per-ability regen
![Per-run kill-method composition and resource collapse](docs/analytics/05_per_run_resource_collapse.png)
- **Signal:** Across 44 runs that scored at least one kill: **50% of runs never fire a bullet, 50% never dash, 25% are 100% slash, 95% never deal trap damage to an enemy.** This is not "slash is preferred" — this is "half of players never voluntarily use the resource toolkit at all".
- **Why this can't be explained away:** These are completed runs across 3 weeks of beta. Bullets regen, dashes regen, flashes regen — the runs that didn't use them weren't constrained by ammo. Players simply mashed J and ignored everything else.
- **Change driven:** **Idle auto-flash** in `PlayerLightWave.cs` — if no input is detected for 4 seconds, a free 60%-radius mini-flash fires automatically with no ammo or energy cost. The 25% of pure-slash players still get illumination they would never produce themselves. **Per-ability independent regen timers** in `PlayerAmmo.cs` (bullets: 5s+0.5s/each; dashes: 8s+2s/each; flashes: 8s+2s/each) ensure that experimenting with bullets/dashes never penalizes future slash use — there's no "save the resource" tax that pushes players back to slash.

### 5. Skitter's slash resistance demonstrably worked — bullet share doubled against it (controlled comparison) → followed up with close-range tightening
![Skitter vs basic enemy method shift and TTK](docs/analytics/06_skitter_vs_basic.png)
- **Signal:** Same beta sessions, same players, two enemy classes:
  - Basic enemies: slash 66%, bullet 14%, dash 19%, trap 1%
  - **Skitter:** slash 67%, **bullet 28%** (Δ +13pp), **dash 3%** (Δ −16pp), trap 2%
  - Skitter mean TTK = **3.64s** vs 1.28s for basic enemies (2.8× longer)
- **Why this can't be explained away:** This is a *controlled comparison* — the same player, in the same run, kills basic enemies primarily by slash and Skitters by a mix that includes 2× more bullets. The shift is enemy-driven, not player-driven; if it were a general trend across all enemies it would show up against basic enemies too, and it doesn't.
- **Change driven:** Once the slash resistance proved out, follow-up tweaks tightened Skitter behavior so the bullet adoption wasn't passive kiting:
  - **`closeShootRange` rapid fire** (`5c97db1`): inside 5 units, Skitter fires every 0.8s instead of 3s — kiting from 12 units no longer trivializes it.
  - **Proximity activation** (`038d5f0`): Skitter activates on 15-unit player range, not only on light contact — sneaking up no longer works.
  - **Triangle size bump** (`f88d551`): hitbox visibility raised to 1.1 × 0.9 so players can read the Skitter without flashing.

### 6. Fatal damage chains take 2× the damage of survived chains, concentrated above the 3-HP threshold → 0.5s i-frames + victory invulnerability
![Damage interaction histogram, fatal vs survived](docs/analytics/04_damage_chain_distribution.png)
- **Signal:** 1,000 damage chains logged. **Survived chains:** mean 1.46 dmg, median 1, concentrated at 1-2 dmg. **Fatal chains:** mean 2.80 dmg, median 2, with a long tail at 3-9 dmg. The fatal distribution starts where survival becomes impossible (≥3 dmg vs base 3 HP) and reaches as high as 9 dmg in a single interaction.
- **Why this can't be explained away:** Survived and fatal chains have similar durations (mean 1.56s overall, median 0s — most are sub-frame events). Fatal chains aren't *longer* engagements; they're more *hit-stacked* — multiple sources damaging the player on overlapping frames before any escape mechanic can fire.
- **Change driven:** **0.5-second player invincibility frames** in `PlayerHealth` after every hit cap a multi-hit chain at one tick — the 9-damage worst-case becomes a 1-2 damage outcome. The **damage flash uses real time** (so the i-frame readout is always visible even during pause). **Victory invulnerability** prevents stray hits during the win sequence, since the data shows runaway 5-9 dmg chains exist.

### Telemetry coverage (raw)
- **Date range:** 2026-04-06 → 2026-04-27 (13 active days, 3,961 events).
- **Run outcomes:** 48 (46 death, 2 win, 0 quit logged).
- **TTK records:** 2,509 across 24 enemy IDs (Skitter first appears 2026-04-21, Clones with the boss drop on 2026-04-25).
- **Damage interactions:** 1,000 (894 timeout / 106 death).
- **Room clears:** 180 (level 1 = 47, level 2 = 37, level 3 = 31, scaling down).

### Reproducing the plots
- All seven plots are checked into `docs/analytics/` as PNGs.
- Source: `docs/analytics/_make_plots.py`. Run `python3 docs/analytics/_make_plots.py` after dropping a fresh export at `~/Downloads/alter-ego beta.xlsx` to regenerate.
- Required: Python 3, `openpyxl`, `matplotlib`, `numpy`.

### Death & Win Screens
- **Death:** A "YOU DIED" overlay with dark background appears. Press Space to restart the current scene. Player sprite and collider are disabled; all player scripts are turned off. Death screen is created at runtime if not present in the scene.
- **Victory:** After the 1-second room light-up, the game pauses (timeScale = 0) and a "YOU WON!" overlay appears. Press Space to return to the main menu.
- Both screens build themselves dynamically — no manual UI wiring required.
- All singletons (StatusHUD, PlayerAmmo, WinText, DoorMessageUI, CameraShake) have duplicate protection — extra instances destroy their entire GameObject to prevent orphaned UI.

## Procedural Generation

### RandomWalk Generator
- Multiple "walkers" start at the center and take random steps, carving circular tunnels.
- Generation stops when the target floor coverage is reached.
- Produces organic, cave-like layouts.
- Parameters: `width`, `height`, `steps`, `walkers`, `fillGoal`, `carveRadius`.

### BSP Room Generator
- Binary Space Partition: recursively splits space into rectangular leaves.
- Each leaf contains a randomly-sized interior room, connected by L-shaped corridors.
- Produces structured multi-room layouts.
- Parameters: `width`, `height`, `minLeafSize`, `maxLeafSize`, `maxLeaves`, `corridorWidth`.

### Progressive Mode (DungeonManager)
- Generates rooms live using embedded RandomWalk algorithm (no presets needed).
- All parameters scale with room index: grid size, enemy count, trap count.
- Fill goal 0.32 + carve radius 1 = tight, claustrophobic corridors where light matters.
- **Corridor widening:** A post-processing pass detects 1-tile-wide pinch points and widens them to 2 tiles, ensuring the player can always fit through.
- Caps at 65x45 grid, 20 enemies, 12 traps.

### Grid Pathfinding (A*)
- 8-directional A* on the tile grid, used by hunt-mode enemies.
- Diagonal corner-cutting prevention ensures enemies don't clip through wall corners.
- Octile distance heuristic. Max 3000 iterations safety cap.
- Enemies recalculate paths every 0.4 seconds. Falls back to direct movement if no path exists.

### TilemapRoomBuilder
- Converts `bool[,]` grids into Floor + Wall tilemap tiles.
- Perlin noise color variation for natural stone floor textures.
- Door cells carved from walls with matching collider triggers.

## Tech Stack

- **Engine:** Unity 6 (6000.3.8f1)
- **Rendering:** Universal Render Pipeline with 2D Renderer, Light2D, ShadowCaster2D
- **Language:** C#

## Project Structure

```
Assets/
  Scripts/
    # Player
    PlayerMovement.cs         # WASD movement + Shift dash (aim-direction based)
    PlayerShooting.cs         # K key ranged attack (ammo + energy cost)
    PlayerSlash.cs            # J key melee arc + light cone + wall LOS + bullet deflection (no energy cost)
    PlayerDash.cs             # Dash light trail + afterimages + 2 damage + bullet destroy
    PlayerLightWave.cs        # L key light burst + idle auto-flash (4s no input)
    PlayerAmbientLight.cs     # Always-on dim glow (doesn't activate enemies)
    PlayerHealth.cs           # Player HP + iFrames + damage flash + death handling
    PlayerInventory.cs        # Key collection (HashSet-based)
    PlayerAmmo.cs             # Discrete bullet/dash/flash counts + per-ability timed regen + HUD
    LightEnergy.cs            # Unified energy pool (500 max, 1/sec regen)
    FlashlightAim.cs          # Weapon visual rotation toward mouse cursor

    # Enemies
    EnemyAI.cs                # Light activation, permanent chase, stun, speed boost, A* hunt mode
    EnemyShooting.cs          # Enemy ranged attack (fires on activation)
    EnemyHealth.cs            # Enemy HP + opacity fade + death fade-out + slash damage multiplier
    EnemyHealthBar.cs         # Procedural mini health bar above enemies
    EnemyProximityGlow.cs     # Proximity glow when player is near
    EnemyHitGlow.cs           # Visual glow on enemy hit
    EnemyRegistry.cs          # Central registry of active enemies (avoids per-frame FindObjectsByType)
    SkitterAI.cs              # Fast triangle enemy: proximity + light activation, close-range rapid fire, light fleeing
    SkitterLightSensor.cs     # Light-source detector that drives Skitter activation/flee state

    # Bosses
    ScarabAI.cs               # Scarab boss: armored front, weak-point back, charge attacks, phase-2 shockwave
    ScarabArenaTrigger.cs     # Arena entry trigger that runs Scarab fly-in intro and starts the fight
    ScarabHitForwarder.cs     # Routes hits on Scarab armor/weak-point colliders into Scarab damage logic
    ShadowBossAI.cs           # Umbra boss: slash arcs, bullet pressure, berserk, phase-2 arena sweep
    ShadowArenaTrigger.cs     # Arena entry trigger for Umbra
    VesperAI.cs               # Vesper boss: light-locked vulnerability, dark-spot teleports, scatter bursts
    VesperArenaTrigger.cs     # Arena entry trigger for Vesper
    CrimsonAI.cs              # Crimson boss: chase + scatter + minions + clone phase ring puzzle
    CrimsonArenaTrigger.cs    # Arena entry trigger for Crimson
    BossHealthBar.cs          # Shared on-screen boss HP bar (intro fade, phase-transition shake)
    BossIntroCam.cs           # Camera pan-to-boss / pan-back-to-player intro and death sequences

    # Projectiles & Effects
    Bullet.cs                 # Projectile + light + impact offset
    DamageNumber.cs           # Floating damage text
    ImpactLightStatic.cs      # Static impact light on wall hit
    TimedDestroy.cs           # Expanding echo pulse light
    ExplosionLight.cs         # Fading explosion light effect
    LightFader.cs             # Generic light fade-out utility
    HitLightFade.cs           # Hit light fade effect
    FootprintFade.cs          # Dash footprint fade effect

    # Environment
    Trap.cs                   # Dormant->Arming->Armed state machine
    Key.cs                    # Collectible key pickup
    KeyItem.cs                # Key follow behavior
    Door.cs                   # Exit door, requires matching key
    DoorMessageUI.cs          # "You need a key" popup
    FinishLine.cs             # Zone-based win trigger
    WallShadowSetup.cs        # Auto-adds ShadowCaster2D to walls
    WallDoor.cs               # Wall-mounted door variant for crafted levels
    HealthPickup.cs           # +1 HP green pulsing pack with pickup flash
    HealthUICounter.cs        # HUD readout for HP / pickup state
    RewardPopup.cs            # Floating "+1 ♥" / "+Max Ammo" popup at room clear
    LevelExit.cs              # Marks level-done state (suppresses combat input during outros)
    RoomLightSpawner.cs       # Helper for placing static room lights
    Checkpoint.cs             # Tutorial checkpoint that boosts ambient light on touch
    TextCanvas.cs             # In-world text overlay for tutorial / story prompts
    TextTrigger.cs            # Trigger volume that shows TextCanvas messages

    # Tutorial
    TutorialManager.cs        # Tutorial scene sequencer
    TutorialEnemyActivator.cs # Activates tutorial enemies on cue
    TutorialInvincibility.cs  # Brief god-mode while learning a mechanic
    TutorialLayoutGenerator.cs# Procedurally lays out tutorial room sequence
    TutorialLightPulse.cs     # Pulsing light cue to draw the player to objectives
    TutorialRoomTrigger.cs    # Trigger that advances tutorial state on entry

    # Procedural Generation
    Procedural/
      BSPRoomGenerator.cs     # Binary Space Partition room generation
      RandomWalkGenerator.cs  # Random walk cave generation
      TilemapRoomBuilder.cs   # Grid -> Tilemap tile painter + WorldToCell/CellToWorld
      SpawnManager.cs         # Enemy/trap/key spawner with spacing rules (returns enemy count)
      GridPathfinder.cs       # 8-directional A* pathfinding on tile grids
      TrapPlacement.cs        # Scoring-based trap placement (chokepoint preference, spacing rules)
    RoomPreset.cs             # ScriptableObject for saving/loading room grids

    # Dungeon / Endless Mode
    DungeonManager.cs         # Room loading, progressive mode, live generation, healing, boss interleaving, Skitter spawn fraction
    RoomExit.cs               # Door trigger to load next room
    RoomsManager.cs           # Higher-level room state coordination
    RoomClearPortal.cs        # Portal: appear -> brighten -> suck-in -> fade -> load + reward popups
    MinimapRadar.cs           # Circular radar with room layout, enemy/Skitter/health-pack dots, portal dot
    RoomCounterHUD.cs         # "Room X" display with fade animation

    # Game Management
    GameManager.cs            # Win/death state, enemy count, victory sequence, hunt mode trigger
    DeathScreen.cs            # Death overlay UI + restart
    WinText.cs                # Win text display
    MainMenuController.cs     # Main menu (Tutorial / Game / Endless)
    PauseMenu.cs              # Esc pause overlay with stats + Main Menu button
    RunKillAnalytics.cs       # Per-run kill counts, damage windows, room-clear timings, win/death telemetry

    # HUD
    StatusHUD.cs              # Top-left: HP pips, flash cooldown, enemy count
    KeybindHUD.cs             # Top-center keybind reference
    GameUIManager.cs          # Secondary HUD (Inspector-wired)

    # Camera & Utility
    CameraFollow.cs           # Camera follow player
    CameraShake.cs            # Screen shake on impacts
    SendToGoogle.cs           # Posts analytics + death positions to a Google Sheets endpoint
    WebGLOptimizer.cs         # Frame rate config for WebGL builds

  Scenes/
    MainMenu.unity            # Main menu (Tutorial / Let's Roll / Endless)
    TutorialScene.unity       # Original guided tutorial
    NewTut.unity              # Updated tutorial draft
    FinalTutorial.unity       # Current tutorial flow with checkpoints
    GameScene.unity           # Story mode level
    Level1.unity .. Level9.unity  # Numbered campaign levels
    RoomGenScene.unity        # Endless mode (preset-based rooms)
    ProgressiveRoomGen.unity  # Endless mode (progressive live-generated rooms)
    ScarabScene.unity         # Boss arena: Scarab
    VesperScene.unity         # Boss arena: Vesper
    GoblinBossScene.unity     # Boss arena: Crimson (goblin)
    IsshinBossScene.unity     # Boss arena: Umbra (isshin)
    NewEnemyTesting.unity     # Sandbox for testing new enemy types
    BaseScene.unity           # Shared base scene template
    SampleScene.unity         # Default Unity sample scene

  RoomPresets/
    RoomPreset1.asset         # Pre-generated room layout
    RoomPreset2.asset         # Pre-generated room layout
    RoomPreset3.asset         # Pre-generated room layout

  Prefabs/
    Bullet.prefab             # Player bullet
    EnemyBullet.prefab        # Enemy bullet
    Enemy.prefab              # Enemy with AI, health, shooting
    Trap.prefab               # Placeable trap
    Key.prefab                # Collectible key
    Door.prefab               # Exit door
    HitGlow.prefab            # Enemy hit glow
    EnemyGlowParticle.prefab  # Enemy proximity glow
    ExplosionLight.prefab     # Explosion light
    FootprintLight.prefab     # Dash trail light
    ImpactEchoLight.prefab    # Bullet wall impact light
```

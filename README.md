# Sound of Sight

A 2D top-down action game built in Unity where darkness is your default state and light is a weapon. Every room is pitch black. Your combat abilities emit light that reveals enemies — but once an enemy sees light, it stays active forever. The core tension: every attack you make permanently escalates the threat.

## Concept

The world is dark. You have no flashlight — only your combat abilities produce light. A faint ambient glow lets you see your immediate surroundings, but the rest is black. Slashing, dashing, shooting, and light waves all emit light that reveals and activates enemies. Once activated, enemies chase you relentlessly, even in total darkness. The core loop is: explore cautiously, engage strategically, and manage the permanent consequences of every action you take.

## Game Modes

### Story Mode (GameScene)
Hand-crafted level with keys, doors, and a fixed enemy layout. Kill all enemies or reach the exit door with the key to win.

### Endless Mode — Progressive (ProgressiveRoomGen)
Procedurally generated rooms with scaling difficulty. Rooms start small and intimate (30x22 tiles, 3 enemies) and grow larger with more enemies each floor. There are no doors — the only way forward is to clear all enemies and enter the portal.

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

## Controls

| Input | Action |
|---|---|
| WASD | Move + Aim (8 directions, last direction persists) |
| J | Slash — 120-degree melee arc, deflects enemy bullets |
| K | Shoot — ranged bullet with travelling light |
| Left Shift | Dash — fast burst, deals 2 damage, destroys bullets |
| L | Light Wave — room-wide light burst |

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
    EnemyHealth.cs            # Enemy HP + opacity fade + death fade-out + health bar
    EnemyHealthBar.cs         # Procedural mini health bar above enemies
    EnemyProximityGlow.cs     # Proximity glow when player is near
    EnemyHitGlow.cs           # Visual glow on enemy hit

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
    DungeonManager.cs         # Room loading, progressive mode, live generation, healing, spawn flash
    RoomExit.cs               # Door trigger to load next room
    RoomClearPortal.cs        # Portal: appear -> brighten -> suck-in -> fade -> load
    MinimapRadar.cs           # Circular radar with room layout, enemy dots, portal dot
    RoomCounterHUD.cs         # "Room X" display with fade animation

    # Game Management
    GameManager.cs            # Win/death state, enemy count, victory sequence, hunt mode trigger
    DeathScreen.cs            # Death overlay UI + restart
    WinText.cs                # Win text display
    MainMenuController.cs     # Main menu (Tutorial / Game / Endless)

    # HUD
    StatusHUD.cs              # Top-left: HP pips, flash cooldown, enemy count
    KeybindHUD.cs             # Top-center keybind reference
    GameUIManager.cs          # Secondary HUD (Inspector-wired)

    # Camera & Utility
    CameraFollow.cs           # Camera follow player
    CameraShake.cs            # Screen shake on impacts
    TutorialManager.cs        # Tutorial scene sequence
    SendToGoogle.cs           # Analytics: death position reporting
    WebGLOptimizer.cs         # Frame rate config for WebGL builds

  Scenes/
    MainMenu.unity            # Main menu (Tutorial / Let's Roll / Endless)
    TutorialScene.unity       # Guided tutorial
    GameScene.unity           # Story mode level
    RoomGenScene.unity        # Endless mode (preset-based rooms)
    ProgressiveRoomGen.unity  # Endless mode (progressive live-generated rooms)

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

# Sound of Sight

A 2D top-down action game built in Unity where darkness is your default state and light is a weapon. Every room is pitch black. Your combat abilities emit light that reveals enemies — but once an enemy sees light, it stays active forever. The core tension: every attack you make permanently escalates the threat.

## Concept

The world is dark. You have no flashlight — only your combat abilities produce light. A faint ambient glow lets you see your immediate surroundings, but the rest is black. Slashing, dashing, shooting, and light waves all emit light that reveals and activates enemies. Once activated, enemies chase you relentlessly, even in total darkness. The core loop is: explore cautiously, engage strategically, and manage the permanent consequences of every action you take.

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
- Unified energy pool (max 500) powers all abilities.
- Regenerates at 1 energy/second after a 1.5-second cooldown from last use.
- Shooting, dashing, slashing, and flashing all draw from this pool.

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
- Cost: 3 energy. Cooldown: 0.15 seconds. No ammo cost.

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
- **Enemies shoot immediately on activation** (shoot range: 10 units, cooldown: 1.2 seconds).
- **Enemy bullets emit a faint red light** — always visible in the dark.
- Enemies **fade in opacity as they take damage** (quadratic curve: full → faded → transparent death).
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
- **Kill all enemies** — defeating every enemy triggers the victory sequence.
- **Reach the door with the key** — reaching the exit door with the matching key triggers the victory sequence.
- **Victory sequence:** A global Light2D fades in from 0 to full intensity over 1 second, dramatically illuminating the entire room. Then the game pauses and the win screen appears.

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

### Death & Win Screens
- **Death:** A "YOU DIED" overlay with dark background appears. Press Space to restart the current scene. Player sprite and collider are disabled; all player scripts are turned off.
- **Victory:** After the 1-second room light-up, the game pauses (timeScale = 0) and a "YOU WON!" overlay appears. Press Space to return to the main menu.
- Both screens build themselves dynamically — no manual UI wiring required.
- All singletons (StatusHUD, PlayerAmmo, WinText, DoorMessageUI, CameraShake) have duplicate protection — extra instances destroy their entire GameObject to prevent orphaned UI.

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
    PlayerSlash.cs            # J key melee arc + light cone + wall LOS + bullet deflection + cached materials/sprites
    PlayerDash.cs             # Dash light trail + afterimages + 2 damage + bullet destroy
    PlayerLightWave.cs        # L key room-wide light burst (ammo + energy, 20s cooldown)
    PlayerAmbientLight.cs     # Always-on dim glow (doesn't activate enemies, no shadows)
    PlayerHealth.cs           # Player HP + iFrames + damage flash + death handling
    PlayerInventory.cs        # Key collection (HashSet-based)
    PlayerAmmo.cs             # Discrete bullet/dash/flash counts + per-ability timed regen + top-right pip HUD
    LightEnergy.cs            # Unified energy pool for all abilities (500 max, 1/sec regen, safe division)
    FlashlightAim.cs          # Weapon visual rotation toward mouse cursor

    # Enemies
    EnemyAI.cs                # Light activation, permanent chase, stun, speed boost, mark glow
    EnemyShooting.cs          # Enemy ranged attack (fires on activation, 10 range, 1.2s cooldown)
    EnemyHealth.cs            # Enemy HP + opacity fade + death fade-out + health bar control
    EnemyHealthBar.cs         # Procedural mini health bar above enemies (green/yellow/red)
    EnemyProximityGlow.cs     # Proximity glow when player is near (SmoothStep, max 8 units)
    EnemyHitGlow.cs           # Visual glow on enemy hit (Light2D flash)

    # Projectiles & Effects
    Bullet.cs                 # Projectile + light (yellow player / red enemy) + impact offset + cached materials
    DamageNumber.cs           # Floating "-N" damage text (red-orange, fades upward)
    ImpactLightStatic.cs      # Static impact light on wall hit (shadows disabled)
    TimedDestroy.cs           # Expanding echo pulse light
    ExplosionLight.cs         # Fading explosion light effect
    LightFader.cs             # Generic light fade-out utility (keep + fade phases, unscaled time)
    HitLightFade.cs           # Hit light fade effect
    FootprintFade.cs          # Dash footprint fade effect

    # Environment
    Trap.cs                   # Dormant->Arming->Armed state machine, dodge via dash, duplicate-reveal guard
    Key.cs                    # Collectible key pickup
    KeyItem.cs                # Key follow behavior (floats near player after pickup)
    Door.cs                   # Exit door, requires matching key to win
    DoorMessageUI.cs          # "You need a key" popup message
    FinishLine.cs             # Zone-based win trigger (alternative to Door)
    WallShadowSetup.cs        # Auto-adds ShadowCaster2D to walls via reflection

    # Game Management
    GameManager.cs            # Win/death state, enemy count, victory light-up sequence
    DeathScreen.cs            # Death overlay UI + restart on Space
    WinText.cs                # Win text display

    # HUD
    StatusHUD.cs              # Top-left HUD: HP pips, flash cooldown bar, enemy count
    KeybindHUD.cs             # Top-center keybind reference panel
    GameUIManager.cs          # Secondary HUD (Inspector-wired HP, enemy, flash text)

    # Camera & Utility
    CameraShake.cs            # Screen shake on impacts (unscaled time, rest-position based)
    MainMenuController.cs     # Main menu navigation (Tutorial / Game / Quit)
    TutorialManager.cs        # Tutorial scene sequence (Move->Slash->Trap->Dash->Shoot->Flash)
    WebGLOptimizer.cs         # Auto-configures frame rate and vsync for smooth WebGL builds

  Prefabs/
    Bullet.prefab             # Player bullet (yellow light, unlit sprite)
    EnemyBullet.prefab        # Enemy bullet (red light, unlit sprite)
    Enemy.prefab              # Enemy with AI, health, shooting, opacity fade, health bar
    ImpactEchoLight.prefab    # Light pulse on bullet wall impact
    Trap.prefab               # Placeable trap with state machine
    Key.prefab                # Collectible key
    Door.prefab               # Exit door
    HitGlow.prefab            # Enemy hit glow effect
    EnemyGlowParticle.prefab  # Enemy proximity glow particles
    ExplosionLight.prefab     # Explosion light effect
    FootprintLight.prefab     # Dash trail footprint light
    LightRevealMask.prefab    # Light reveal mask
    WallHorizontal.prefab     # Horizontal wall segment
    WallVertical.prefab       # Vertical wall segment
    Particle System.prefab    # Particle system effect

  Scenes/
    MainMenu.unity            # Main menu
    TutorialScene.unity       # Tutorial level (guided sequence)
    GameScene.unity           # Main game scene
    GameScene 1.unity         # Alternate game scene
    BaseScene.unity           # Base/template scene
    SampleScene.unity         # Sample/test scene
```

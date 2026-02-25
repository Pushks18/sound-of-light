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

### Light Energy System
- Unified energy pool (max 500) powers all abilities.
- Regenerates at 1 energy/second after a 1.5-second cooldown from last use.
- Displayed on the HUD as "Energy: current / max".

### Slash (J)
- 120-degree melee arc in your aim direction.
- Produces a visible light cone and a semi-transparent arc showing exact range and angle.
- Damages enemies caught in the arc (2 damage per hit). Line-of-sight check prevents damage through walls.
- **Deflects enemy bullets** — any enemy bullet entering the slash arc during its duration is destroyed with a blue-white spark effect.
- Cost: 3 energy, cooldown: 0.15 seconds.

### Shoot (K)
- Fires a bullet in your aim direction.
- **Bullets emit a warm yellow light** (unlit sprite, always visible) that illuminates surroundings and activates enemies as they travel.
- Bullets pass through triggers (traps, doors, keys) and only stop on solid walls or enemies.
- Cost: 1 energy per shot.

### Dash (Left Shift)
- Fast burst of movement in your aim direction (works while stationary).
- Leaves a trail of fading light orbs that shrink and dim over time.
- Spawns shadow afterimages of the player that fade to transparent.
- **Deals 2 damage** + 0.2 second stun to enemies on contact (once per enemy per dash). Uses line-segment sweep detection to catch enemies along the entire dash path.
- **Destroys enemy bullets** on contact during the dash.
- **Dodges traps** if dashing fully across them.
- Cost: 3 energy, cooldown: 1 second.

### Light Wave (L)
- Emits a large 360-degree light burst (radius 12) centered on the player.
- Fades over 5 seconds, activating all enemies in the room.
- Use it to reveal an entire room — at the cost of waking everything up.
- Cost: 10 energy, cooldown: 20 seconds. Cooldown displayed on HUD as "Energy: Xs / 20s".

### Ambient Light
- A small, always-on dim glow around the player (radius 1.5, intensity 0.4).
- Lets you see walls and nearby objects without activating enemies.

### Enemies
- Enemies are **dormant in the dark**. They don't move or shoot until light touches them.
- **Once activated by any light source, enemies stay active permanently** — they chase at full speed even after the light is gone.
- Enemies move faster while actively in a light source (boosted speed).
- This is the central design tension: **every attack you make has permanent consequences**. Light reveals threats but also creates them.
- **Enemies shoot immediately on activation** (shoot range: 10 units, cooldown: 1.2 seconds).
- **Enemy bullets emit a faint red light** — always visible in the dark.
- Enemies **fade in opacity as they take damage** (quadratic curve: full → faded → transparent death).
- On death, enemies **fade out over 0.3 seconds** instead of popping out.
- Stunned enemies can't move or shoot for the stun duration.
- Enemies glow briefly when hit (mark light).
- Enemies have a proximity glow when the player comes close.

### Traps
- Traps are invisible until revealed by a light source.
- **Dormant:** Hidden, no damage. Light from abilities (slash, dash, light wave, bullets) reveals them.
- **Arming (2 seconds):** Once revealed, a yellow light fades in over 2 seconds.
- **Armed:** Light turns red and pulses. Contact now deals 1 damage.
- Traps can be **dodged by dashing** fully across them.
- Traps deal repeated damage if the player stands on them (with cooldown between hits).
- Optional light burst on damage that can wake nearby enemies.
- Affects both players and enemies.

### Combat
- Player has **3 HP**, enemies have **2 HP**.
- Both player and enemies fire projectiles.
- Player bullets: warm yellow light, always visible, illuminate surroundings.
- Enemy bullets: faint red light, always visible in the dark.
- Bullets are triggers (no physics knockback).
- Bullets produce a faint muzzle flash and a small echo pulse on wall impact.
- **Slash deflects enemy bullets** with a spark visual effect.
- **Dashing destroys enemy bullets** on contact.

### Keys & Doors
- Collectible keys are placed in the level. Walking over a key picks it up and it follows the player visually.
- The door (exit) requires the matching key. Walking onto the door with the correct key triggers a win.
- Walking onto the door without the key shows a "You need a key" message.

### Win Conditions
- **Kill all enemies** — defeating every enemy in the level triggers a win.
- **Reach the door with the key** — picking up the key and reaching the exit door triggers a win.

### Shadows
- Walls block light and cast shadows via ShadowCaster2D. Dark rooms stay dark until you bring light inside. Rooms have a gray background.

### Death & Win Screens
- When the player dies, a "YOU DIED" screen appears. Press Space to restart.
- When the player wins, a "YOU WON!" screen appears. Press Space to return to the main menu.
- Both screens build themselves dynamically — no manual UI wiring required.

## Tech Stack

- **Engine:** Unity 6 (URP 17.3.0)
- **Rendering:** Universal Render Pipeline with 2D Renderer, Light2D, ShadowCaster2D
- **Language:** C#

## Project Structure

```
Assets/
  Scripts/
    LightEnergy.cs            # Unified energy pool for all abilities
    PlayerMovement.cs         # WASD movement + Shift dash (aim-direction based)
    PlayerSlash.cs            # J key melee arc + light cone + wall LOS + bullet deflection
    PlayerDash.cs             # Dash light trail + afterimages + 2 damage + bullet destroy
    PlayerLightWave.cs        # L key room-wide light burst (20s cooldown)
    PlayerShooting.cs         # K key ranged attack with energy cost
    PlayerAmbientLight.cs     # Always-on dim glow (doesn't activate enemies)
    PlayerHealth.cs           # Player HP + death handling
    PlayerInventory.cs        # Key collection (HashSet-based)
    DeathScreen.cs            # Death overlay UI + restart on Space
    GameManager.cs            # Win/death state, enemy count, dynamic win screen
    GameUIManager.cs          # HUD for HP, energy, enemy count
    EnemyAI.cs                # Light activation, chase, stun, speed boost
    EnemyShooting.cs          # Enemy ranged attack (fires on activation, 10 range)
    EnemyHealth.cs            # Enemy HP + opacity fade + death fade-out
    EnemyProximityGlow.cs     # Proximity glow when player is near
    EnemyHitGlow.cs           # Visual glow on enemy hit
    Bullet.cs                 # Projectile + light emission (yellow player / red enemy)
    Trap.cs                   # Dormant→Arming→Armed state machine, dodge via dash
    Key.cs                    # Collectible key pickup + visual follow
    KeyItem.cs                # Key follow behavior (floats near player after pickup)
    Door.cs                   # Exit door, requires matching key to win
    DoorMessageUI.cs          # "You need a key" popup message
    FlashlightAim.cs          # Flashlight aiming (cached Camera.main)
    CameraShake.cs            # Screen shake on impacts
    WallShadowSetup.cs        # Auto-adds ShadowCaster2D to walls
    TimedDestroy.cs           # Expanding echo pulse light
    ImpactLightStatic.cs      # Static impact light
    LightFader.cs             # Generic light fade-out utility
    HitLightFade.cs           # Hit light fade effect
    FootprintFade.cs          # Dash footprint fade effect
    ExplosionLight.cs         # Explosion light effect
    WinText.cs                # Win text display
    FinishLine.cs             # Legacy finish line (superseded by Door.cs)
    MainMenuController.cs     # Main menu navigation
    TutorialManager.cs        # Tutorial scene logic
  Prefabs/
    Bullet.prefab             # Player bullet (yellow light, unlit sprite)
    EnemyBullet.prefab        # Enemy bullet (red light, unlit sprite)
    Enemy.prefab              # Enemy with AI, health, shooting, opacity fade
    ImpactEchoLight.prefab    # Light pulse on bullet wall impact
    Trap.prefab               # Placeable trap with state machine
    Key.prefab                # Collectible key
    Door.prefab               # Exit door
    HitGlow.prefab            # Enemy hit glow effect
    EnemyGlowParticle.prefab  # Enemy proximity glow particles
    ExplosionLight.prefab     # Explosion light effect
    FootprintLight.prefab     # Dash trail footprint light
    LightRevealMask.prefab    # Light reveal mask
    Particle System.prefab    # Particle system effect
  Scenes/
    GameScene.unity           # Main game scene
    MainMenu.unity            # Main menu
    TutorialScene.unity       # Tutorial level
    BaseScene.unity           # Base/template scene
```

# Sound of Sight

A 2D top-down action game built in Unity where darkness is your default state and light is a weapon. Every room is pitch black. Your combat abilities emit light that reveals enemies — but once an enemy sees light, it stays active forever. The core tension: every attack you make permanently escalates the threat.

## Concept

The world is dark. You have no flashlight — only your combat abilities produce light. A faint ambient glow lets you see your immediate surroundings, but the rest is black. Slashing, dashing, shooting, and light waves all emit light that reveals and activates enemies. Once activated, enemies chase you relentlessly, even in total darkness. The core loop is: explore cautiously, engage strategically, and manage the permanent consequences of every action you take.

## Controls

| Input | Action |
|---|---|
| WASD | Move + Aim (8 directions, last direction persists) |
| J | Slash — 120-degree melee arc with light cone |
| K | Shoot — ranged bullet, costs 1 energy |
| Left Shift | Dash — fast burst with light trail + contact damage |
| L | Light Wave — room-wide light burst for 1 second |

## Core Mechanics

### Light Energy System
- Unified energy pool (max 500) powers all abilities.
- Regenerates at 1 energy/second after a 1.5-second cooldown from last use.
- Displayed on the HUD as "Energy: current / max".

### Slash (J)
- 120-degree melee arc in your aim direction.
- Produces a visible light cone and a semi-transparent arc showing exact range and angle.
- Damages enemies caught in the arc (2 damage per hit).
- Cost: 3 energy, cooldown: 0.15 seconds.

### Shoot (K)
- Fires a bullet in your aim direction.
- Produces a brief muzzle flash.
- Cost: 1 energy per shot.

### Dash (Left Shift)
- Fast burst of movement in your aim direction (works while stationary).
- Leaves a trail of fading light orbs that shrink and dim over time.
- Spawns shadow afterimages of the player that fade to transparent.
- Deals 1 damage + 0.2 second stun to enemies on contact (once per enemy per dash).
- Cost: 3 energy, cooldown: 1 second.

### Light Wave (L)
- Emits a large 360-degree light burst (radius 12) centered on the player.
- Fades over 1 second, activating all enemies in the room.
- Use it to reveal an entire room — at the cost of waking everything up.
- Cost: 10 energy, cooldown: 3 seconds.

### Ambient Light
- A small, always-on dim glow around the player (radius 1.5, intensity 0.4).
- Lets you see walls and nearby objects without activating enemies.

### Enemies
- Enemies are **dormant in the dark**. They don't move or shoot until light touches them.
- **Once activated by any light source, enemies stay active permanently** — they chase at full speed even after the light is gone.
- This is the central design tension: **every attack you make has permanent consequences**. Light reveals threats but also creates them.
- Stunned enemies can't move or shoot for the stun duration.
- Enemies shoot back when activated, not stunned, and within range.

### Traps
- Placed throughout the level, traps damage both players and enemies on contact.
- Configurable: damage, cooldown, trigger delay, and whether they affect players/enemies.
- Optional light burst on activation that can wake nearby enemies.

### Combat
- Player has **3 HP**, enemies have **3 HP**.
- Both player and enemies fire projectiles.
- Bullets are triggers (no physics knockback).
- Bullets produce a faint muzzle flash and a small echo pulse on wall impact.

### Keys & Doors
- Collectible keys unlock corresponding locked doors to gate progression between rooms.

### Shadows
- Walls block light and cast shadows via ShadowCaster2D. Dark rooms stay dark until you bring light inside.

### Death Screen
- When the player dies, a "YOU DIED" screen appears with the option to press Space to restart.

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
    PlayerSlash.cs            # J key melee arc with light cone + visual arc
    PlayerDash.cs             # Dash light trail + shadow afterimages + contact damage
    PlayerLightWave.cs        # L key room-wide light burst
    PlayerShooting.cs         # K key ranged attack with energy cost
    PlayerAmbientLight.cs     # Always-on dim glow (doesn't activate enemies)
    PlayerHealth.cs           # Player HP + death screen trigger
    PlayerInventory.cs        # Key collection
    DeathScreen.cs            # Death overlay UI + restart on Space
    EnemyAI.cs                # Persistent light activation, chase, stun
    EnemyShooting.cs          # Enemy ranged attack (respects activation + stun)
    EnemyHealth.cs            # Enemy HP
    Bullet.cs                 # Projectile logic + impact echo
    Trap.cs                   # Trigger-based damage + optional light burst
    TimedDestroy.cs           # Expanding echo pulse light
    ImpactLightStatic.cs      # Static impact light
    WallShadowSetup.cs        # Auto-adds ShadowCaster2D to walls
    HealthUICounter.cs        # HUD for HP and energy
    Key.cs                    # Collectible key pickup
    Door.cs                   # Locked door that requires a key
  Prefabs/
    Bullet.prefab             # Player bullet (trigger collider)
    EnemyBullet.prefab        # Enemy bullet (trigger collider)
    ImpactEchoLight.prefab    # Light pulse on bullet wall impact
    Trap.prefab               # Placeable trap with optional light burst
  Scenes/
    SampleScene.unity         # Main game scene
```

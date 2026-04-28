# Demo Mode

A "GTA-style cheat code" that drops the player into endless mode at progressively harder rooms (6, 12, 18) with bosses interleaved between them, then ends in a Thank-You credits screen. Built for the showcase.

## How to trigger it

1. Boot the game and land on the **Main Menu**.
2. Type `d`, `e`, `m`, `o` on the keyboard. No input field — letters are captured globally on the menu.
3. Endless mode loads at Room 6 and the chain plays through.

The cheat is intentionally invisible — there's no button, hint, or UI. Letters typed are accumulated into a 16-character rolling buffer; if the last four are `demo`, the sequence starts. Anything that isn't a letter is ignored, so stray Tab / Enter / Esc presses don't break it.

## What plays

| Step | Scene | What happens | Player HP |
|---|---|---|---|
| 1 | `ProgressiveRoomGen` | Room **6** — endless mode at room-6 difficulty (size, enemies, traps scaled to that index) | **6** |
| 2 | `ScarabScene` | Boss 1 — Scarab | **8** |
| 3 | `ProgressiveRoomGen` | Room **12** — endless mode at room-12 difficulty | **10** |
| 4 | `VesperScene` | Boss 2 — Vesper | **12** |
| 5 | `ProgressiveRoomGen` | Room **18** — endless mode at room-18 difficulty | **14** |
| 6 | `GoblinBossScene` | Boss 3 — Crimson | **16** |
| 7 | *(credits)* | Auto fade-through "Thank You" + 5 team names | — |

After credits, the player is returned to the Main Menu.

**HP progression:** every step starts at exactly the listed value (both `currentHealth` and `maxHealth`). The progression is **+2 every step** — `6 → 8 → 10 → 12 → 14 → 16` — so the player has more headroom each round to compensate for the increasing scaling. Health is forced on every scene load, so a death-restart of any step respawns at the same HP value.

## Required Build Settings

You must add **all four** of these scenes to **File → Build Settings → Scenes In Build** (drag them in from `Assets/Scenes/`):

- `MainMenu`
- `ProgressiveRoomGen`
- `ScarabScene`
- `VesperScene`
- `GoblinBossScene`

If any are missing, the chain halts with a `Scene not found in Build Settings` error in the console (which is exactly what happened in the first test run with `Level6`). The demo doesn't need any `Level1`–`Level9` scenes since endless mode is the entire chain.

## How completion is detected

A single hook in `RoomClearPortal` covers both endless rooms and boss arenas. The portal is spawned automatically by the existing game systems whenever a "challenge cleared" event happens:

- **Endless room cleared** → `GameManager` sees `enemyCount == 0` → spawns `RoomClearPortal`.
- **Boss defeated** → boss AI calls `GameManager.BossDefeated()` → spawns `RoomClearPortal` (the demo path forces this branch even outside the normal "returning from boss" flow).

When the player walks into the portal, the portal's coroutine reaches its scene-transition phase and notices `DemoSequenceManager.IsActive`. Instead of calling `LoadNextRoom`, it calls `DemoSequenceManager.NotifyChallengeCleared()` which advances to the next step. One hook, both paths.

## Files added / modified

| File | Status | Purpose |
|---|---|---|
| `Assets/Scripts/DemoSequenceManager.cs` | new | DontDestroyOnLoad singleton; orchestrates the chain, owns step config + HP progression, hands off to credits |
| `Assets/Scripts/DemoCreditsOverlay.cs` | new | Runtime-built fullscreen "Thank You" + team names with fade-in/out |
| `Assets/Scripts/MainMenuController.cs` | modified | `Update()` cheat-code listener that calls `DemoSequenceManager.StartDemo()` when `demo` is typed |
| `Assets/Scripts/DungeonManager.cs` | modified | New `static int DemoStartRoomIndex`; `Start()` reads it to seed `currentRoomIndex` for endless steps |
| `Assets/Scripts/RoomClearPortal.cs` | modified | At scene-transition time, calls `DemoSequenceManager.NotifyChallengeCleared()` if demo is active |
| `Assets/Scripts/GameManager.cs` | modified | `BossDefeated()` always spawns the portal in demo mode (skips the "standalone scene → end-game" branch) |
| `Assets/Scripts/LevelExit.cs` | modified | Calls `DemoSequenceManager.Advance()` if demo is active — vestigial, kept in case you ever add level-style scenes to the demo sequence |

No gameplay code is touched outside of demo-active checks. Outside the demo, every system behaves exactly as before — the new code paths only run when `DemoSequenceManager.IsActive` is true.

## Tweaking the sequence

All sequence config is on the `DemoSequenceManager` component. Each step is a `DemoStep` with four fields:

| Field | Meaning |
|---|---|
| `type` | `Endless` or `Boss`. Endless steps seed `DungeonManager.DemoStartRoomIndex` before the load; Boss steps don't. |
| `sceneName` | Scene to load. Must be in Build Settings. |
| `endlessRoomIndex` | (Endless only) DungeonManager room index to start at. e.g. `6` makes the room scale like Room 6 of a normal endless run. |
| `health` | Player max HP **and** starting HP for this step. Forced on every scene load (handles death-restarts). |

Example tweaks:
- **Different rooms**: change `endlessRoomIndex` on the endless steps to 3 / 8 / 15 etc.
- **Different boss order**: swap `ScarabScene` ↔ `VesperScene` ↔ `GoblinBossScene` ↔ `IsshinBossScene`.
- **Skip a boss**: delete its `DemoStep` from the array.
- **More HP headroom**: bump every step's `health` field by 2.

## Failure modes & recovery

- **Player dies during a step.** Restart with Space (default `DeathScreen` behaviour). The static `DungeonManager.DemoStartRoomIndex` and the OnSceneLoaded re-apply make the restart land at the same room with the same HP — no need to retype the cheat.
- **Player Esc → Main Menu mid-chain.** Returns to MainMenu. The persistent `DemoSequenceManager` GameObject is still alive but inactive in the new scene. Typing `demo` again starts a fresh sequence (the singleton check destroys the old manager).
- **Boss death cinematic delays the portal.** This is fine — the demo waits for the player to walk into the portal whenever it actually spawns. There's no time pressure.
- **Boss component is destroyed without spawning a portal.** Shouldn't happen with current bosses, but if a future boss skips `GameManager.BossDefeated()` it would soft-lock the demo. Workaround: have the boss's death sequence call `GameManager.Instance.BossDefeated()` itself.

## Editing team credits

Team names live in `DemoCreditsOverlay.cs`:

```csharp
static readonly string[] TeamMembers = new string[]
{
    "Rachel Channell",
    "Yunzhe Li",
    "Jackson Xie",
    "Pushkaraj Baradkar",
    "Praveen Saravanan",
};
```

Edit the array to add, remove, or reorder members. The pacing constants at the top of the same file (`NameHold`, `NameFadeIn`, `NameFadeOut`) control per-name timing.

## About the unrelated console warnings

You may see lines like this in the editor console:

```
Can't Generate Mesh, No Font Asset has been assigned.
UnityEditor.HandleUtility:BeginHandles () (...)
```

These are **not from the demo code**. They come from a TextMeshPro UI element somewhere in your scene that has no font asset assigned, surfaced by the editor's scene-view gizmo handles. The credits overlay uses Unity's legacy `Text` (not TMP) and bundles its own `LegacyRuntime.ttf`, so it can't trigger that warning. The "BeginHandles" frame in the stack trace is the giveaway — that's editor scene-view gizmos, never runtime UI.

If you want to silence them, find the TMP UI element in your menu / level scenes and either assign a TMP font asset or replace it with `Text`.

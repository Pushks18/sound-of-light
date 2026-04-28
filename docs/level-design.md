# Level Design — Sound of Sight

Endless mode is procedural, but it doesn't *feel* procedural. That's deliberate. The whole system is a pipeline of steps where each one takes a raw, chaotic output and folds in a designer's bias — the goal at every step is "stay random enough to surprise the player, structured enough to be fair."

Here's how the pipeline works and why each layer exists.

## The starting goal

We wanted endless mode to feel **claustrophobic, organic, and threatening** — like exploring a dark cave system where every corner could be hiding something. The opposite would be procedural rooms that look like rectangles glued together — readable but boring, and worse, predictable enough that a player can figure out where enemies always are.

So the layout had to be:

- **Cave-like**, not grid-like — irregular walls, dead-ends, branches, no straight 90° corridors
- **Tight enough that the player's small ambient light actually matters** — open arenas would let the player see everything just from ambient glow
- **Always traversable** — the player can never get stuck, can always reach every enemy, and the portal is always reachable
- **Different every time** but **fair every time**

These requirements pull in different directions. Randomness gives the cave-like organic feel but breaks playability (1-tile pinch points, isolated pockets, enemies spawned in walls). Curation gives playability but kills surprise. The pipeline is what reconciles them.

## Layer 1 — The base random walk

The starting carver is a **multi-walker random-walk algorithm** in `DungeonManager.GenerateGrid` and `RandomWalkGenerator`. Two walkers start at the room's center and take random four-direction steps. At each step, they carve a circular hole around their current position with a configurable `carveRadius`.

The two parameters that define the *feel* of every room are:

- **`fillGoal` = 0.32** — the walkers stop when 32% of the grid is floor. That's deliberately low — most procedural cave generators target 40–50%. We wanted dark, narrow, threatening tunnels, not open caverns.
- **`carveRadius` = 1** — each carve only paints a 3×3 area, not a 5×5. Tight tunnels, not highways.

Together those two numbers give us the claustrophobic feel. The player walks through a room they can almost never see all of at once, even after a full Light Wave. **The bias toward tight spaces is the single most important creative decision** — it's what makes the player's small ambient light feel useful instead of useless, and it's what makes every slash light cone feel like genuine information rather than decoration.

The walkers always stay one tile inside the grid edge (`>= 1` and `< w - 1` bounds checks), which gives every room a guaranteed **1-tile outer wall border**. This is the first padding.

## Layer 2 — The corridor-widening pass (the actual "padding")

After the random walk finishes, we run **`WidenNarrowPassages`** on the grid. This is the most important curation step in the whole pipeline.

The problem: random walks naturally produce **1-tile pinch points** — single floor cells where there's a wall directly above and below, or walls to the left and right. These are visually invisible (they look like normal corridors) but mechanically catastrophic: the player's collider is wider than 1 tile, so they can get stuck. The dash and the slash have wider hitboxes than walking does, so a corridor that was passable while walking becomes impassable while dashing. Enemies pathfinding through these corridors fail. Bullets clip wall corners and disappear.

`WidenNarrowPassages` solves it deterministically:

1. Take a snapshot of the current grid (so the widening doesn't cascade).
2. For every floor cell, check the four cardinal neighbors.
3. If walls are above AND below (a horizontal pinch), open the cell directly above (or below if at the top edge).
4. If walls are left AND right (a vertical pinch), open the cell directly to the right (or left if at the right edge).
5. The pass enforces every passage is at least **2 tiles wide** while preserving the original cave shape everywhere else.

The creative aspect: we tried two other approaches first. **Approach A** was raising `carveRadius` to 2, which guarantees 2-tile passages everywhere, but loses all the variation — every corridor is the same width and the cave loses its character. **Approach B** was post-processing with a cellular-automaton smoothing pass, which softens the cave too much and produces rounded, blob-shaped rooms. The pinch-point widening keeps the *organic* feel of the random walk almost everywhere, surgically fixing only the cells that would have broken playability. Most of the cave you walk through is untouched by the padding pass — only the impossible parts get fixed.

This is the central trick: **don't over-correct randomness, find the specific failure modes and patch only those**. The result reads as designed even though it's mechanical.

## Layer 3 — Spawn safety zones

Once the geometry exists, we still have to place the player, enemies, traps, and the portal. Each of these has its own bias.

**Player spawn** uses an explicit "safe cell" filter (`minFloorNeighboursForSpawn = 4`) — a candidate cell only qualifies if at least 4 of its 8 neighbors are also floor. That keeps the player from spawning at the end of a one-tile dead-end nook with their back literally to the wall. It also implicitly biases the spawn toward room interiors rather than corridors.

**Spawn flash safe zone** — the moment the player loads a room, a one-shot light flash fires at the player's position. Enemies are then placed with the rule `distance to player ≥ spawnFlashRadius` (default ~10 units). This gives the player an unconditional 10-unit safe radius on every room load. They don't have to fight the moment they appear; they get a free orientation pass. The same radius defines where the spawn flash itself is visible, so the player visually sees their safe zone — the curated detail is also the visual cue.

**Edge buffer** — anything spawned (enemies, traps, keys) sits at least 3 tiles inside the grid edge. We added this after watching playtests where enemies in the far corners felt unfair because the player couldn't see them coming and they had nowhere to flee. The 3-tile buffer makes every fight have *room to fight in*.

**Inter-enemy spacing** (`minDistBetweenEnemies = 4`) — no two enemies spawn within 4 units of each other. Without this rule, the random scatter would occasionally produce 3-enemy clusters that one Light Wave would activate at once and kill the player instantly. The 4-unit rule guarantees the player can always activate enemies one at a time if they're careful.

These three rules together — safe spawn, flash zone, spacing — turn the random scatter into something that looks like a designer placed it. The player can't tell that the layout was generated five seconds ago.

## Layer 4 — Trap placement scoring

This is the most explicitly designerly layer. `TrapPlacement.PickTrapCells` doesn't pick traps randomly — it scores every candidate cell and picks the highest-scoring ones, with hard distance rules layered on top.

The score function is the creative core:

```
neighbors == 3   → score 1.0   (tight corridor — best)
neighbors == 4   → score 0.9   (corridor bend / junction)
neighbors == 5   → score 0.7   (corridor opening into room)
neighbors <= 2   → score 0.4   (dead-end nook — too hidden)
neighbors == 6   → score 0.4   (near a wall in open area)
default          → score 0.2   (wide open — too easy to dodge)
```

The intuition: a trap in a wide-open arena is just a small obstacle to walk around. A trap in a dead-end nook is the player's fault for going somewhere weird. The *interesting* trap is the one in a tight corridor where the player has to commit to either dashing through or finding another way around. So the scoring function literally encodes "good trap placement = chokepoint placement" as a scalar.

We then add a **0.15 random jitter** to every score before sorting. That breaks ties unpredictably — the same room layout never traps the same chokepoints twice. The randomness is the *texture*; the scoring function is the *intention*.

The hard rules layer on top:

- **`minDistFromPlayer = 5`** — no trap closer than 5 units to spawn (so the player can't immediately step on one)
- **`minDistFromEnemies = 3`** — no trap inside an enemy's spawn cluster (so the trap isn't cheesed by enemies stepping on it)
- **`minDistBetweenTraps = 4`** — no two traps within 4 units of each other (so a single mistake doesn't kill the player from overlapping damage)

This is the most explicit example of curation in the pipeline. The greedy picker takes the highest-scoring 1-12 cells (depending on room number) that satisfy the hard rules, and that's where the traps go. The whole thing runs in maybe 5 milliseconds, but the result reads as if a level designer placed every trap.

## Layer 5 — Skitter vs basic enemy distribution

A subtler curation lives in how enemies are spawned. The progressive spawner uses a **`skitterFraction`** parameter — what share of the per-room enemy budget goes to Skitters vs basic enemies. The two types are interleaved as the spawner walks the shuffled candidate cells, so Skitters and basics end up spread *across* the room rather than clustered. A room with 10 enemies and `skitterFraction = 0.3` will have 7 basics and 3 Skitters, scattered, never two Skitters next to each other.

The creative reason: a cluster of three Skitters in one corner is a death sentence (their close-range rapid fire stacks). A scattered distribution forces the player to choose which threat to engage first — the slash-resistant Skitter that wants you close, or the basic enemy whose bullets already lit the corridor. **Variety distribution is what produces tactical decisions** in a room generated by a random walk.

## Layer 6 — Progressive scaling

Every layer above takes the same parameters in every room, *except* these three, which scale with `currentRoomIndex`:

- **Grid size:** 30×22 (room 1) → 65×45 (cap at room ~10)
- **Enemy count:** 3 → 20
- **Trap count:** 1 → 12

Notice what *doesn't* scale: `fillGoal`, `carveRadius`, the trap scoring weights, the spacing rules, the spawn-flash radius. The game's *feel* stays constant — every room is still a tight cave with a safe spawn and chokepoint traps — only the *amount* of stuff scales. This was a conscious choice. Early prototypes scaled `carveRadius` upward in later rooms to make them more open; it broke the lighting design because the player could see too much, and the threat density felt diluted because enemies had room to spread out. Holding the carve parameters constant and scaling only counts kept the claustrophobic feel intact across a 20-room run.

## Why it feels curated

When a player walks into a generated room, what they perceive is:

1. They can see roughly 1.5 tiles around themselves.
2. There's nothing within 10 units of them — they have a moment to breathe.
3. Slashing reveals a corridor with two corners and probably an enemy.
4. The corridor narrows visually but never traps them — the geometry always works for the player's collider.
5. Walking through the corridor reveals a trap exactly where the corridor was tightest — designed-feeling chokepoint placement.
6. They turn the corner and find a Skitter, and the next corner has a basic enemy — variety distribution.
7. Clearing the room takes 30–60 seconds, the portal spawns under their feet with a heal popup, and the next room is *bigger* but plays the same way.

Every one of those experiences is the output of a layer in the pipeline. The randomness is the texture; the curation is the bias each layer enforces. The audience reads the bias and assumes intent — that's the trick.

## The single biggest creative lesson

The early version of endless mode generated rooms purely from the random walk with no widening, no scoring, no spacing rules. It was unplayable: pinch points trapped the player, enemy clusters one-shot them, traps spawned in dead-ends nobody went to, and rooms felt random in the bad way — chaotic without intention.

Each layer above was added in response to a specific failure we observed in playtests or in the analytics. None of them remove randomness; they remove the *bad* randomness. **The creative process for endless mode was, almost entirely, "find the specific ways procgen produces unfair experiences and write a deterministic patch for each one."** What's left is procgen that feels designed — because by the time the layout reaches the player, the unfair shapes have all been filtered out, and only the surprising shapes remain.

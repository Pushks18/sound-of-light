# GDD — Hypothesized Issues from Feedback (Gold)

Copy-paste sections below into the GDD. Each issue mirrors the example's structure: Significance → Feedback/Data points → Potential Fixes → Implemented Solution → image placeholder. Numbers are pulled directly from the closed-beta dataset (2026-04-06 → 2026-04-27, 3,961 telemetry events, 48 runs, 2,509 enemy kills, 1,000 damage interactions).

---

## Hypothesized Issue #1
Players over-rely on the slash ability and ignore the rest of the combat kit.

### Significance
Slash is meant to be one of four combat tools alongside bullets, dash, and the Light Wave, but if it dominates kills the resource economy collapses and the core "every attack escalates the threat" loop loses its tension. When players spam J without considering bullet/dash/flash trade-offs, the game flattens into a single-button experience and most of our combat design goes unused.

### Feedback/Data points
- Across 48 beta runs and 586 run-summary kills, 82.8% came from slash — bullet 8.9%, dash 7.8%, trap 0.3%. Per-enemy time-to-kill records confirm the same skew (slash 73.5%, bullet 13.4%, dash 12.4%, trap 0.7%).
- Per-run breakdown: 50% of runs (22 of 44 non-zero-kill runs) never fired a single bullet, 50% never dashed, and 25% scored 100% of their kills with slash.
- The slash share never fell below 52% on any beta day across the entire three-week window, ruling out novelty effects, single-broken-session artifacts, or one player dragging the average.
- Time-to-kill by method tells us why: slash kills in 0.22 seconds median; bullets take 2.14 seconds, dash 1.33 seconds, traps 1.97 seconds. Slash is the only ability that connects on the same frame the player presses the button — every other tool has travel time, contact time, or terrain dependency.

### Potential Fixes
- Globally nerf slash damage, raise its cooldown, or attach an energy cost
- Introduce enemies that resist slash specifically and design encounter beats that reward tool variety

### Implemented Solution
We chose the surgical fix over a global nerf because the data showed slash isn't merely *preferred*, it is mechanically 6-to-10× faster than every alternative — a global slash nerf would slow basic combat by the same factor and break the one-slash-one-kill loop the rest of the design depends on. Instead, we added the Skitter, a fast-moving enemy with a slash-damage multiplier on its health component that reduces slash damage specifically against it. We also built Crimson's clone phase, which triggers a slash-spam pushback in the player's slash code: seven slashes within a two-second window during clone phase produces an eighteen-unit knockback and a 0.6-second movement lockout. Slash stays free everywhere else, so basic combat tempo is preserved.

We can test the efficacy of this adjustment by re-running the per-method time-to-kill and per-run resource-collapse charts on post-fix telemetry. The validation already in hand: bullet share against Skitter rose from the 14% baseline against basic enemies to 28% in the same beta sessions — a controlled comparison that confirms the resistance approach forces tool variety enemy-by-enemy without nuking slash globally.

\<Add images/screenshots showing gameplay before and after implementation of the fix\>

---

## Hypothesized Issue #2
A single bad frame can kill the player before they can react.

### Significance
With only 3 HP, the player has very little headroom for any hit. When two or three damage sources land on overlapping physics frames — overlapping enemy bullets, contact hitboxes, an explosion light — the player can lose a run to a single tick of bad geometry rather than to skill. This makes deaths feel unfair, breaks the "each hit is a deliberate cost" loop, and discourages players from approaching enemies aggressively.

### Feedback/Data points
- Of 1,000 damage interactions logged across 48 beta runs, 106 (10.6%) ended in player death and 894 (89.4%) ended with the player escaping.
- Fatal chains averaged 2.80 damage; survived chains averaged 1.46 damage — fatal chains take roughly twice as much damage as the chains players walked away from.
- The two distributions have nearly identical durations (overall median 0 seconds, mean 1.56 seconds), so the fatal chains are not longer engagements with multiple discrete hits. They are sub-frame multi-hits where overlapping sources deal damage on the same physics tick.
- The worst single-chain damage we recorded was 9 damage on one frame — three times the player's base HP. That chain is mechanically unrecoverable without a safety net, no matter how skilled the player is.
- The fatal-vs-survived histogram clearly shows the fatal distribution shifting right of the 3 HP threshold line while survived chains concentrate at 1 to 2 damage, which is the exact line that needs a mechanical cap.

### Potential Fixes
- Increase player base HP to 5 or more so multi-hit chains are survivable
- Add a brief invincibility window after each hit so overlapping damage is absorbed instead of stacking

### Implemented Solution
We added 0.5-second player invincibility frames after every hit. This caps any multi-hit chain at one tick: every hit after the first lands inside the i-frame window and is absorbed, so the worst-case 9-damage chain now resolves as 1 or 2 damage. We also made the damage flash use unscaled real time so the player's red flash (and the i-frame readout) remains visible during cinematic pauses where time scale drops to zero, and we added victory invulnerability so a stray runaway chain can't grief a successful run during the win sequence. The mechanic is invisible when nothing is happening, which is exactly what a defensive system should be.

We can test the efficacy of this adjustment by re-running the same fatal-versus-survived damage-chain histogram on post-fix telemetry and observing whether the fatal distribution compresses back below the 3 HP threshold and the death share of damage interactions falls below the current 10.6%.

\<Add images/screenshots showing gameplay before and after implementation of the fix\>

---

## Hypothesized Issue #3
Half of all enemies survive the first attack and become stuck stragglers that waste the player's time.

### Significance
After clearing most of a room, the player frequently spends 20+ seconds looking for one or two enemies that escaped the initial slash and got stuck on wall corners or geometry dead-zones. The dormant chase AI can't pathfind around obstacles, so survivors end up parked behind walls in dead corners. This breaks the pacing of the endless-mode loop, makes rooms feel empty when they aren't, and adds frustration in the worst possible place — right when the player thinks they're about to clear and earn the next portal.

### Feedback/Data points
- Across the 14 enemy spawn slots with at least 15 samples each, every single slot shows a 2× to 5× gap between its median time-to-kill and its mean time-to-kill.
- Aggregate gap across 1,412 basic-enemy kills: median 0.37 seconds versus mean 1.28 seconds — a 3.5× ratio.
- The shape of the distribution is bimodal, not noisy. Half of all enemies die in 0.4 seconds (the one-slash-one-kill we designed for, since slash deals 2 damage and basic enemies have 2 HP) and the other half survive 5 to 30 seconds.
- The fact that the gap appears on every single spawn slot rules out a single positional dead-zone or one broken room — it is a structural property of the chase AI, not a level-design artifact.
- Long-tail enemies were almost always survivors of the player's first slash that escaped the cone, lost line-of-sight, then chased at base speed (2 m/s) without pathfinding and snagged on a wall corner.

### Potential Fixes
- Increase enemy chase speed to close the long tail by brute force
- Add A* pathfinding so survivors actively navigate around walls and hunt the player

### Implemented Solution
We chose the pathfinding approach because raising chase speed would have made early engagements with full enemy counts overwhelming, while the actual problem only existed in the late-room cleanup phase. We shipped Hunt Mode in the game manager and enemy AI: when three or fewer enemies remain in a room, every surviving enemy force-activates (even ones that were still dormant) and switches to 8-directional A* pathfinding with diagonal corner-cutting prevention, recalculating every 0.4 seconds with a 3000-iteration safety cap. This makes long-tail enemies come find the player rather than the player having to find them. The room-cleanup phase becomes active and aggressive instead of a hide-and-seek slog.

We can test the efficacy of this adjustment by re-running the per-enemy mean-versus-median time-to-kill dumbbell chart on post-fix telemetry. A successful fix should narrow the dumbbell visibly across all spawn slots, particularly on the high-sample slots that contributed most of the long tail before.

\<Add images/screenshots showing gameplay before and after implementation of the fix\>

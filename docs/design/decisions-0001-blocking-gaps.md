# Decisions: the four blocking gaps

Decided 2026-10-21. Resolves section 1 of [open-gaps-addendum.md](open-gaps-addendum.md). Numbers marked *(proposed)* are starting values for Phase 0 to tune; the rules are decided, the constants are not.

## 1.1 Enemy targeting - decided as recommended
Lock on at acquisition after a **0.3s acquisition delay**. Re-target only if the target dies, becomes untargetable, or is more than **8 units** away. The Sneaky trait ("enemies lock on 15% slower") scales the 0.3s delay.

## 1.2 Enemy-barrier reaction - decided: per enemy type, in data
Each enemy type defines **its own, predictable reaction** to a barrier, matched to what that enemy is and does. This replaces the single shared rule. Mites are unchanged (turn 90 degrees, hold 2s).

A reaction is a data field on the enemy type: a *kind* plus parameters. Kinds:

| Kind | Parameters | Example fit |
|---|---|---|
| `Turn` | angle, hold seconds | a crawler that veers off |
| `TurnAround` | hold seconds | a chaser that is spooked |
| `Recoil` | distance, optional stun seconds | a heavy hitter knocked back |
| `ReflectProjectile` | stun seconds on the shooter | a ranged enemy hit by its own shot |

Build only the kinds the current enemies need. `ReflectProjectile` is reserved for a future chapter; do not implement it for Chapter 1.

Chapter 1 *(proposed)*:
- **Ash Viper:** `Turn`, 90 degrees, 1.5s. (Its existing wording already says it turns 90 degrees.)
- **Spark Hound:** `Recoil`, 2 units, then it **drops its lock** and re-acquires with a fresh 0.3s acquisition delay (ties into 1.1).

The 0.6s trigger immunity applies to enemies as well as Mites unless an enemy type overrides it. Every reaction must be readable in the first second: the player sees *what it did*, not just that something happened.

## 1.3 Spawn rules - decided: edges, evenly spread, released by Spawn Portals
Mites no longer appear from nowhere. Each level has **one or more Spawn Portals** on the field edge. Portals are spaced evenly by angle around the flame with a small jitter seeded from the level seed, rejecting any position closer than 5 units to the flame or within 1.5 units of a field element. (Daily Challenge and Replay stay deterministic.)

A portal carries a **counter** (how many Mites it will release) and a **release pattern**:

| Pattern | Behaviour |
|---|---|
| `Burst` | all at once |
| `Sequence` | one every *interval* seconds |
| `Groups` | *n* Mites, a *gap*, repeat |
| `Delayed` | any of the above, starting after a start delay (later levels, staggered portals) |

The Mite count now lives on the portal, not on a level-wide `deploySlots`. The level format (GDD Appendix A) must change to a `portals[]` list; `deploySlots` is superseded.

**Consequences to handle:**
- **New art asset:** the portal (idle, releasing, depleted states). It is not in the Appendix D commission manifest and must be added **before** commissioning.
- **Teaching:** portals need a tooltip and a visible remaining-count so the player can plan.
- **Sequencing the work:** Phase 0 builds one portal with `Burst` only. `Sequence`, `Groups` and `Delayed` come in Phase 1, so the prototype's 25 hours stay about answering "does the catch feel good".

## 1.4 Breathe architecture - decided as recommended
Breathe state (active flag, remaining steps, cooldown) lives in the Sim, counted in sim time, and pressing it is a recorded input. Unity only reads that state to decide how fast to pump fixed steps (a quarter as many per real second at 0.25 scale). `BreatheManager` keeps `Time.timeScale` for presentation effects only. This corrects GDD v3.1 section 15.1.

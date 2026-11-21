# Obstacles in the field: design proposal

Status: **APPROVED 2026-11-20 and built** (Sim, View preset 5, 9 tests). Decisions: Stone first, Mites slide along it, fixed layout per level.

Originally a proposal only. Raised in the first playtest
(TC-023): "no obstacles in the field to juggle Mites with".

## The problem
The player's only tools are one barrier and Breathe. Mites walk straight
lines, so once a barrier is placed there is nothing else in the field to
combine it with. Obstacles give the player something to *use*, not just
react to.

## Proposed first obstacle: the Stone (static, solid)
- A circle in the field (radius 0.6 to 1.0). Mites cannot enter it.
- A Mite walking into a Stone slides along its edge, keeping its heading
  where possible, then continues. No new input, no new Mite state.
- The flame's Catch radius is NOT blocked by Stones (keeps the threat clear).
- The barrier works on top of a Stone as normal (a Mite behind a Stone can
  still be turned).
- Placed by the level definition only; the player cannot move them.

Why Stones first: smallest change, fully deterministic, no timers, and it
already creates "funnel" puzzles (a Stone line steers Mites into the barrier).

## Sim changes needed (when approved)
1. `LevelConfig`: add `Obstacles` (array of centre + radius), default empty
   so existing levels and the 52 tests are unchanged.
2. `Simulation.MoveMite`: after the move step, push the Mite out of any
   overlapping obstacle along the edge normal (no trig, no sqrt beyond the
   existing `Normalized`). Obstacles iterate in array order for determinism.
3. `SpawnPlanner`: reject spawn points inside or within `MiteRadius` of a
   Stone.
4. View: draw Stones as placeholder grey circles. State hash includes
   nothing new (obstacles are static config).

## Tests to add
- Mite aimed at a Stone ends up outside it and still advances.
- No Mite ever overlaps a Stone over a full seeded run.
- Level with no obstacles hashes identically to today (regression).
- Spawn never lands inside a Stone.

## Later ideas (NOT proposed now)
Moving obstacles, breakable obstacles, Gloom Moss terrain, wind. Each would
need its own design pass.

## Questions for the owner
1. Is Stone-first right, or do you picture something else (pits, mirrors)?
2. Should Mites bounce off Stones, or slide along them (recommended: slide)?
3. Fixed per-level layout (recommended), or seeded-random placement?

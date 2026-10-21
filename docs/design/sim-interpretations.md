# Sim interpretations

Where the GDD (v3.1) or the decisions doc was ambiguous while building the Sim core, this records what was chosen and why. Each is a candidate to revisit during Phase 0 tuning. The GDD is authoritative once it says something explicit.

| # | Question | Chosen | Why / what would change it |
|---|---|---|---|
| 1 | Is Breathe's "2 seconds" real time or sim time? (GDD 8.3) | **2 real seconds at 0.25 scale = 25 sim steps.** Cooldown **20 s of sim time** (1000 steps), counted from activation | Literal reading of "time scale 0.25 for 2 seconds". If the 2 s should be 2 s of *game* time (8 real seconds), set `BreatheActiveSteps` to 100. This is the biggest feel lever in Breathe: the Catch struggle is only paused for as long as Breathe lasts |
| 2 | Which way is "left" for a Leftythe? | 90 degrees **counter-clockwise from its heading toward the flame** (y up) | A Mite heading right turns up. Turns are a component swap, so exact |
| 3 | "Runs directly away" (Scattermite): away from what? | **Away from the barrier centre**; if exactly on it, away from the flame's direction | The GDD only says "directly away". Alternative is away from the flame |
| 4 | Tap while a barrier is active or cooling down | **Ignored** (no replacement, no queue) | GDD 8.4 says rescuing "costs you your barrier for three seconds", which implies no replacement |
| 5 | Even spacing "by angle" around the flame | **Even along the perimeter**, plus a small seeded jitter | No trigonometry needed, so exactly reproducible. Close to angular spacing while the flame is near the centre |
| 6 | Spawn rejection when a point is too near the flame | A burst **slides along the perimeter as a group** until every member is valid | Sliding members individually piled them on one spot (caught by a test) |
| 7 | A rescued Mite lands inside the barrier | It gets the **0.6 s trigger immunity** | Otherwise it would instantly re-trigger a reaction |
| 8 | Panic after rescue | **Set to 1.0 and recorded, but has no effect and does not decay** | The panic system is not built; the GDD's weaker reaction at 100% panic is deferred with it |
| 9 | The step a Mite is caught | Caught at the end of a step; struggle counts the **next 100 steps**, so exactly 2.0 s | Tested directly |
| 10 | Mites caught when the flame dies | Released as **Walking**, level is a **Win** | GDD 2.2 |

## Not built in the Sim core yet
Enemies and Aggro Lines, panic effects, Super Barrier, Flame Pulse, Last Gasp, terrain (Gloom Moss), wind, Mite abilities and personalities, Huddle, the Whistle, scoring, Spawn Portal patterns other than Burst, and the second barrier slot. Each is a separate piece of work.

# Meandermites — Open Specification Gaps

**Addendum to GDD v3.1** · 21 October 2026

*Companion to the master design document. Four items block the first coding sessions; five are deferred with due dates. One of them corrects an error in v3.1 §15.1.*

---

## Why this document exists

GDD v3.1 is build-ready in the sense that matters: every constant a developer would otherwise invent is pinned, and the design decisions are closed. These nine items are what remains. They were identified while assessing build-readiness and are recorded here so they do not live only in conversation.

Treat the four blockers as **about an hour of decisions total** — not a redesign.

---

## 1 · Blocking — close before writing simulation code

### 1.1 Enemy targeting

**Gap:** §7.1 says the Spark Hound "chases nearest Mite at 1.5× speed" without saying whether the target is chosen once or re-evaluated.

**Why it matters:** re-evaluating every fixed step makes an enemy oscillate between two near-equidistant Mites, visibly jittering and committing to nothing. It reads as a broken enemy rather than a hunting one.

**Recommended rule:** lock on at acquisition. Re-target only when the current target dies, becomes untargetable, or exceeds a break distance (suggest **8 units**). Add a **0.3s acquisition delay** so a newly spawned enemy does not snap instantly — the Sneaky personality's "enemies lock on 15% slower" modifies exactly this value, which gives that trait a concrete meaning it currently lacks.

### 1.2 Enemy–barrier reaction

**Gap:** §7.1 gives both Chapter 1 enemies the counter-play "barrier turns it away" with **no angle and no duration specified**. Mites have a defined 90° for 2 seconds; enemies have nothing.

**Why it matters:** this is the only counter-play either enemy has, and it is needed by Level 3 — the third playable level.

**Recommended rule:** an enemy entering a barrier trigger turns **90° away from the barrier centre** and holds that heading for **1.5s** — shorter than a Mite's 2s, so barriers steer enemies less reliably than they steer Mites. That asymmetry is desirable: barriers should feel like a tool for your creatures that merely inconveniences the hunters. Enemies take the same **0.6s trigger immunity** as Mites (§4.4). The Ash Viper's existing "turns 90°" wording already matches this; it simply needs the duration.

### 1.3 Spawn rules

**Gap:** §4.2 sets a minimum spawn distance of 5 units from the flame, but nothing says where Mites enter the playfield. Appendix A's level JSON carries `deploySlots` and no positions, so placement is procedural — and the procedure is unwritten.

**Why it matters:** needed by Level 1, and spawn geometry silently determines early difficulty. On a 16×9 field with a centred flame, a 5-unit minimum already constrains the valid area considerably.

**Recommended rule:** Mites spawn on the playfield **edges**, distributed by even angular spacing around the flame with a small seeded jitter, rejecting any position closer than 5 units or within 1.5 units of a field element. Even spacing matters — clustering every Mite on one edge turns the first barrier into a trivially correct move and removes the decision. Derive the jitter from the level seed so Daily Challenge and Replay stay deterministic.

### 1.4 Breathe architecture — **corrects v3.1 §15.1**

**The error:** v3.1 states that `BreatheManager` is a Unity component and the "sole owner of `Time.timeScale`", while §15.2 states that timeScale changes must not desync replays. **Both cannot be true.**

**Why it matters:** Breathe is a gameplay input with a cooldown. If its state lives in the Unity layer, the replay has no record that it happened, and the cooldown is measured in real time rather than simulation time — so it is not deterministic, and two players with identical inputs can get different outcomes.

**Correct architecture:**

- **Breathe state belongs in the simulation** — an active flag, remaining steps, and a cooldown counted in sim-time. Pressing Breathe is a recorded input like any other.
- **The Unity layer only reads that state** to decide how fast to pump fixed steps. At 0.25 scale it pumps a quarter as many steps per real second.
- `BreatheManager` keeps ownership of `Time.timeScale` for *presentation* — the Combo slow-motion flourish and visual effects — but owns none of Breathe's own logic.

This keeps the behaviour players experience identical while making it replayable. The §15.1 component table should read: *reads Breathe state from the sim and sets the step-pump rate accordingly; sole owner of `Time.timeScale` for presentation effects only.*

---

## 2 · Deferred — with due dates

| Gap | What is missing | Due |
|---|---|---|
| **Daily Challenge generation** | Which parameters vary — enemy count, field layout, timer, deploy slots — and within what bounds. Currently "a seeded procedural level" with no generator spec. | Phase 3 |
| **Survival escalation curve** | "Pulse interval tightens every 30s" by how much, and where it caps. An unbounded curve has no design intent. | Phase 3 |
| **Cloud save conflict policy** | Two devices, divergent Colonies — which Graveyard is true? Permadeath makes this a design question, not a technical one: a wrong merge resurrects the dead or deletes the living. | Schema in Phase 1, resolved by Phase 3 |
| **Teaching copy** | Only Level 0's text is written. Fourteen further levels each introduce one mechanic needing a tooltip. | Phase 2 |
| **Art style reference** | Appendix D lists *which* assets, not *how they look*. "Pastel lavender, bright Mites" is a palette, not a style — an artist will ask for visual references. | **Before commissioning** |

The last one is the only item here with real schedule risk. Commissioned art is the longest lead in the project and it gates Phase 2.

---

## 3 · What is deliberately not being decided yet

The Phase 0 prototype exists to answer whether the core loop feels good, and it will probably move some constants. Specifying the Daily Challenge generator eleven months before building it, against a game whose feel is unproven, would be designing on sand.

Close the four blockers. Build Phase 0. Let it tell you what else was wrong.

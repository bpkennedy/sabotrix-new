# M2 Plan — The Bar

**Goal:** The match from M1 plays inside a 3D nerd bar. You choose cards from your seat in first person, the camera goes overhead while the turn plays out, and Blackout smashes the light box and darkens the room. (GDD Sections 3.3, 4.1, 4.2, 5, 13.)

**Not in M2:** hero card art, toon models, card animations, audio (M3). Room effects for Short Circuit, Beholder Drop, and Spilled Drink (M3, see Question 3). Bot (M4). No rule changes: Core stays as it is.

**Done when:** Brian and Malcolm play a full match on the Windows PC in the bar, with both seat cameras, the overhead resolve show, and a Blackout that cuts away to the light box and darkens the room.

**Starts after:** Playtest 2 (M1). Number tuning from that playtest goes in first, so M2 isn't built on top of rule changes.

## Status (2026-10-11)

| Slice | Status | Commit |
|:--|:--|:--|
| M2.0 Bar blockout | Done. Revised after feedback: bar counter, back-bar with bottles and mirror, stools with background patrons, neon signs, pictures. Room 10 × 10 m. | `86c7515` + revision |
| M2.1 Seats and cameras | Done. Seat swaps are a hard cut (a blend flies through the table). | |
| M2.2 Seated patrons | Done. Quaternius human models (CC0, see `docs/credits.md`) with sitting animations from the Universal Animation Library. Players sit idle, bar patrons sit talking. Seat tilt 12°. | |
| M2.3 Resolve show | Done. 0.6 s blend to overhead, click or Space skips. | `61d1a72` |
| M2.3b Rounds (feedback) | Done. Both players pick, then the round resolves together; the show reveals all cards, then each phase with both players' numbers at once. Blackout blinds everyone (card types still visible). GDD 0.3. | |
| Playtest 3 | **Next** | |
| M2.4 Dress the bar | Not started | |
| M2.5 Lighting | Not started | |
| M2.6 Blackout room effect | Not started | |
| M2.7 Windows build | Not started | |
| Playtest 4 | Not started | |

---

## Slices

Each slice ends compiled, tested, and committed. Slices that are mostly visual are checked with a play-mode screenshot (Unity MCP `manage_camera` `screenshot`) and an auto-played match (`execute_code`).

### M2.0 — Bar Blockout

Build the room out of plain shapes so we can judge size and layout before spending time on art.

- Editor menu item **Sabotrix → Build Bar Blockout** (in `Scripts/Editor/`) that creates a `Bar` prefab from primitives with flat colored materials:
  - Floor, brick-red walls, ceiling with red steel trusses and pipes.
  - One table in the middle, two chairs facing each other across it.
  - Props the sabotage cards will use (GDD 3.3): pendant light box over the table, breaker box on a nearby wall, beholder sculpture on a chain, a drink on each side of the table.
  - Background: wall TVs, a game shelf, the hazard-striped step.
- Every prop gets a named anchor (`LightBox`, `BreakerBox`, `Beholder`, `Drink_P1`, `Seat_P1`, ...) so later code finds it by name, not by position.
- Place the `Bar` prefab in `Main.unity` with one temporary light so it's visible.

> **Unity concept: prefab.** A saved GameObject (with its children and components) that can be placed in scenes and edited in one place. Changes to the prefab update every copy.

**Done when:** entering play mode shows the bar from a free scene camera, and Malcolm agrees the layout reads as "the Dirty 20 table."

### M2.1 — Seats and Cameras

- Add the **Cinemachine** package. One virtual camera per view: `Seat_P1`, `Seat_P2`, `Overhead`, `Cutaway_LightBox`. A `CameraDirector` component switches between them by name.
- The match screen stops covering the whole screen: drop the solid background, keep the HUD (top and bottom rows) and hand as an overlay, and move the status/log to a smaller side panel so the bar shows through the middle.
- Choose Phase uses the active player's seat camera. The handoff screen hides the hand while the camera moves to the other seat.
- Fixed seated view, no mouse look.

> **Unity concept: Cinemachine.** Unity's camera package. Instead of moving one camera by hand, you place several "virtual cameras" and tell Cinemachine which one is live. It blends between them smoothly. Cutaways become "switch to that virtual camera for 2 seconds."

**Done when:** a full match plays in the bar, and the view swaps seats each turn.

### M2.2 — Seated Patrons

- A patron sits in each chair. Start with a gray capsule body and head, then swap in two different generic human models with a seated idle animation (see Question 4).
- The patron across from you holds face-down cards. The count matches the opponent's hand size and updates every turn. The overlay's face-down opponent row goes away.
- The patron in your own seat is hidden while you're in first person (the camera sits where their head is).
- The background patrons at the bar counter (`BarPatron_1..3`) get human models too, with a seated idle. They don't react to the game.

> **Unity concept: Animator.** A component that plays animation clips on a model. A Mixamo "sitting idle" clip on a looping Animator is all M2 needs.

**Done when:** from either seat you see the other player sitting across the table holding the right number of cards.

### M2.3 — Resolve Show

Today `EndTurn` resolves instantly and the result goes straight to the log. M2 turns that into a short show (GDD 5: "plan it, then watch it play out").

- After **End Turn**: cut to the `Overhead` camera, then play `Match.LastTurnEvents` one at a time, about 0.8 s each **[DEFAULT]**.
  - The played card appears face-up on the table as a gray-box card (world-space uGUI, same text as M1).
  - A floating number for damage, heal, and Block. Text banner for "SABOTAGE!" and "BACKFIRE!".
  - The HUD counts HP and Block down as each event plays, instead of jumping to the final numbers.
- Click or Space skips to the end of the show.
- Then the handoff screen (or results screen) as today.
- Core is unchanged. The show only reads events that already happened.

> **Unity concept: coroutine.** A method that can pause (`yield return new WaitForSeconds(0.8f)`) and continue on a later frame. M1 already uses one for the blind-pick reveal. The resolve show is one coroutine that walks the event list.

**Done when:** every card type and both sabotage outcomes (landed, backfired) show on the table in order, and skipping jumps straight to the handoff.

### M2.3b — Rounds (feedback after M2.3)

Malcolm's feedback changed the turn structure (GDD 7.2, version 0.3):

- **Rounds instead of alternating turns.** P1 picks, pass, P2 picks, then the round resolves: both players' Abilities, then both players' Attacks, then both players' Sabotage. The higher-Initiative player picks first and resolves first within each phase. Knockouts are checked per phase; both at 0 is a draw. 8 rounds instead of 16 turns.
- **The show presents the round, not one player.** All played cards are revealed on the table at once (1 s), then each phase shows both players' totals together (1.2 s per phase).
- **Blackout blinds everyone** for the next round. Blind cards still show their type and frame color, not their name, cost, or effect. A backfire blinds only the player who played it.
- **Secret picks.** The opponent's Energy shows as it was at the start of the round, so the second picker can't read the first picker's choices.

**▶ Playtest 3 (Malcolm + Brian):** play 3 matches in the editor. Check:
- Does the show make rounds easier to follow, or just slower? Is 1.2 s per phase right?
- Do rounds feel good? Is picking second (lower Initiative) a disadvantage?
- Is everyone-blind Blackout fun, and is seeing card types the right amount of info?
- Do matches still land around 2–3 minutes?
- Is the seat view comfortable? Can you read your hand?

### M2.4 — Dress the Bar

Malcolm leads this slice (art direction). Brian handles imports.

- Pick free asset packs for furniture, bar props, and wall dressing (GDD 4.5: packs first, AI placeholders when nothing fits).
- Replace blockout shapes with real models in the `Bar` prefab, keeping the named anchors from M2.0.
- Big binary assets go through Git LFS (check `.gitattributes` covers each new file type before committing).

**Done when:** Malcolm signs off that it looks like the bar in the reference photos.

### M2.5 — Lighting

- Warm bar lighting with realtime lights only (no baking), so sabotage can change lights at runtime (GDD 4.1). The PC renderer is already Forward+, which handles many realtime lights.
- Lights grouped by what can turn them off: `RoomLights` (pendants, ceiling), `TvGlow` (TV screens, emissive), `TableLight` (the light box). A `RoomLighting` component turns groups on and off.
- URP post-processing Volume: a little bloom (TV and light glow) and vignette.

> **Unity concept: Volume.** A URP object that applies post-processing effects (bloom, color grading) to the camera image. One global Volume is enough here.

**Done when:** the room looks warm and readable from both seats and overhead, and calling `RoomLighting` to turn `RoomLights` off leaves only the TVs glowing.

### M2.6 — Blackout Room Effect

- Add a **Cutaway** checkbox to `CardData` (GDD 7.6). Set it on for Blackout and off for the other three (GDD 10.4). `StarterAssetTests` checks it. Core doesn't need it: the Game layer looks the flag up by card id.
- When a Blackout event plays in the resolve show:
  1. Cut to `Cutaway_LightBox` (only if the card's Cutaway is on).
  2. The light box sparks and drops on its chain (simple particle burst + transform move).
  3. `RoomLights` and `TableLight` go off. Only the TVs glow.
  4. Cut back to `Overhead` and continue the show.
- The room stays dark through the next round's picks, and the lights come back when that round resolves (GDD 10.4).
- A backfire plays the same room effect (see Question 2).

**Done when:** in an auto-played match with a fixed seed, Blackout lands once and backfires once, and both show the cutaway, the dark room, and the lights coming back.

### M2.7 — Windows Build

- Build Windows 64-bit and play on the Windows PC.
- Check frame rate and that text is readable at the PC's resolution.

**Done when:** a full match plays on the Windows PC in the bar.

**▶ Playtest 4 (Malcolm + Brian):** play 5+ matches. Check:
- Is Blackout a "can't believe that happened" moment? Is the dark Choose Phase fun or just annoying?
- Which sabotage should get the next room effect?
- Anything about the camera that feels slow or confusing?

---

## Questions for Malcolm (Resolved)

All proposed defaults accepted on 2026-10-11. Blackout decisions added to GDD Section 10.4.

| # | Question | Decision |
|:--|:--|:--|
| 1 | After Blackout, when do the lights come back? | When the blind player's turn ends. The room is dark exactly while someone is playing blind. |
| 2 | When Blackout backfires, does the room still go dark? | Yes. Same effect, and the saboteur is the one playing blind. |
| 3 | Room effects for Short Circuit, Beholder Drop, and Spilled Drink? | Not in M2. They show as text banners in the resolve show. Pick the next one after Playtest 4. |
| 4 | What do the two patrons look like? | Generic human people: two different ordinary bar-goers from a free character source (e.g. Mixamo), not toons (GDD 4.2). |

## Decisions for M2

| Decision | Choice | Why |
|:--|:--|:--|
| Camera control | Cinemachine | Unity's standard camera package. Blends and cutaways without writing camera math. |
| Building the room | Editor script for the blockout, then dressed by hand in the editor | Fast first pass from code. Malcolm can move props himself when dressing. |
| Lighting | Realtime only, no baking | Sabotage changes lights at runtime. The RTX 4070 has plenty of headroom. |
| Hand and HUD | Stay as screen-space uGUI overlay | Cards in 3D come with card art in M3. Keeps M2 focused on the room. |
| Resolve show | One coroutine reading `LastTurnEvents` | Core stays untouched. Uses events that already exist. |
| Cutaway flag | On `CardData` only, not in Core | It's a camera setting, not a rule. |
| Asset budget | Free packs only in M2 | Revisit before M3 if something good is paid. |

## Risks

- **Match length.** The resolve show adds about 3–5 s per round. 8 rounds is under a minute extra. Playtest 3 checks this. Skip and shorter step times are the fixes.
- **Asset style mismatch.** Mixing packs can look messy. Prefer one pack family for the room.
- **Repo size.** Models and textures add up. Check LFS before each asset commit.

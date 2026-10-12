# M1 Plan — Gray-box Hot-seat

**Goal:** Two players can play a full Sabotrix match on one PC using plain boxes and text cards. All rules from GDD Section 7 and all content from GDD Section 10 work.

**Not in M1:** the 3D bar, first-person/overhead camera, room effects, card art, audio, bot. Those are M2–M4.

**Done when:** Brian and Malcolm play a full match on the Windows PC from a Windows build, start to results screen, with no rule bugs.

---

## Slices

Each slice ends compiled, tested, and committed.

### M1.0 — Project Cleanup

- Delete template sample content: `Assets/TutorialInfo/`, `Assets/Readme.asset`.
- Set Product Name to `Sabotrix`.
- Create the folder layout and assembly definitions from `CLAUDE.md`.
- Add one placeholder EditMode test.

**Done when:** the project compiles, the placeholder test passes from the CLI, and the editor opens with no errors.

### M1.1 — Core Data Model

- Core definitions: hero, card (Attack / Sabotage / Ability), deck.
- Match state: players, HP, Block, Energy, Special Energy, hand, deck order, sabotages played, turn counter, selected cards.
- Seedable RNG interface.

**Done when:** a match can be created in a test from two heroes and two decks.

### M1.2 — Turn Flow

- Match setup: shuffle once, draw 5, Initiative decides first player (tie: random).
- Start of turn: draw 1 (skip if hand is 7), Energy Regen up to Max.
- Choose phase: select with ordering rules, reserve Energy, withdraw with refund, Basic Attack only when no Attack is affordable, End Turn only when legal.
- Resolve phase: Ability → Attacks → Sabotage. Match ends the moment HP hits 0.
- End of turn: played cards go to the bottom of the deck.
- Win conditions: HP 0, forfeit, 30-turn limit (higher HP wins, tie = draw).
- Dead Sabotage cards after 3 played.

**Done when:** EditMode tests cover every rule above.

### M1.3 — Card Effects

- Damage formula with Strength and Block.
- Special Energy earned per damaging Attack hit.
- Every card in GDD Section 10: Haymaker, Pot Lid Guard, the 8 Attacks, the 4 Sabotages.
- Sabotage reversal roll and reversal effects.

**Done when:** each card has at least one test, including reversal for each Sabotage.

### M1.4 — Card Data Assets

- ScriptableObject types for heroes, cards, decks.
- Assets for Bonk, Pudge, all cards, and the Brawler and Bunker decks, matching GDD Section 10.
- Conversion to Core definitions at match start.

**Done when:** Brian and Malcolm can change a card's numbers in the Inspector and see it in play without touching code.

### M1.5 — Gray-box Match Screen

- Text-only cards: name, type, cost, damage/effect.
- HUD for both players: HP, Block, Energy, Special Energy, cards left in deck, sabotages used (x/3), turn counter.
- Opponent's hand shown as face-down cards (count only).
- Click to select, click again to withdraw. End Turn button enabled only when legal.
- Resolve log: a text list showing each step as it resolves ("Bonk plays Jab: 13 damage").
- Blackout: affected player's hand shows face-down.

**Done when:** a full match is playable in the editor.

**▶ Playtest 1 (Malcolm):** play 3 matches. Note match length, any confusing moments, and whether any card feels too strong or weak.

### M1.6 — Hot-seat Flow

- Title screen: Play, Quit.
- Match setup: each player picks a hero and a deck.
- Handoff screen between turns: "Pass to [Player]" + Ready.
- Results screen: winner, Rematch, Main menu.

**Done when:** you can go from title to results and back without restarting the game.

### M1.7 — Windows Build

- Build Windows 64-bit from the Mac.
- Copy to the Windows PC and play.

**Done when:** a full match plays on the Windows PC.

**▶ Playtest 2 (Malcolm + Brian):** play 5+ matches. Check:
- Do matches land around 6–9 turns per player and about 5 minutes?
- Do dead Sabotage cards feel bad in long matches? (GDD open question 2)
- Is Sabotage fun to gamble on? Are backfires annoying-but-funny?
- Which numbers need tuning?

---

## Rule Clarifications (Resolved)

All proposed defaults accepted on 2026-10-11 and added to GDD Section 7.8.

| # | Question | Decision |
|:--|:--|:--|
| 1 | **Short Circuit** "steals" 2 Energy. When does the thief get it? | Opponent loses 2 at the start of their next turn (after Regen). You gain 2 at the start of your next turn (up to Max). |
| 2 | **Haymaker** doubles the next Attack. Does Strength get doubled too? | Yes: (card damage + Strength) × 2. |
| 3 | Does **Block** stop Sabotage damage (Beholder Drop)? | Yes. Block absorbs all damage. |
| 4 | Does **Basic Attack** earn Special Energy? | Yes. It's an Attack that deals damage. |
| 5 | Can healing (Second Wind) go above starting HP? | No. Capped at starting HP. |
| 6 | **Blackout:** when playing blind and you pick a card you can't afford or that breaks ordering rules, what happens? | The card is revealed for a moment and returns to your hand. No penalty. |
| 7 | **Spilled Drink:** does +1 cost apply to Basic Attack (cost 0)? | No. Only Attack cards. |
| 8 | **Big Finish:** checks opponent HP before or after earlier Attacks this turn? | At the moment Big Finish resolves (after earlier Attacks). |
| 9 | Turn limit "30 total turns": each player's turn counts as 1? | Yes. 15 turns each. |

## Decisions Made for M1

| Decision | Choice | Why |
|:--|:--|:--|
| UI system | uGUI | More tutorials and examples for learning |
| Rules engine | Pure C#, no Unity references | Fast tests, reusable for bot and online play |
| Camera | Single fixed camera | First-person and overhead cameras are M2 |

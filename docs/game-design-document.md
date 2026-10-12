# Sabotrix — Game Design Document

| Field | Value |
|:--|:--|
| **Version** | 0.3 (draft) |
| **Last updated** | 2026-10-11 |
| **Status** | Pre-production. No code yet. |
| **Engine** | Unity |
| **Target platform** | Windows 11 PC (dev on macOS) |
| **Team** | Malcolm (12) and Brian (43) |

> **[DEFAULT]** marks a starting value picked to get playing. Expect to change it after playtesting. **[OPEN]** marks something still undecided (see [Section 16](#16-open-questions)).

---

## 1. Overview

### 1.1 Logline

A cartoon card battle played at a table in a nerd bar, where sabotage cards don't just hurt your opponent, they mess with the room itself: lights blow, breakers spark, and sometimes it all backfires on you.

### 1.2 Genre

Turn-based 1v1 card battler with deck building.

### 1.3 Design Pillars

1. **Sabotage is a gamble.** Stronger sabotage is more likely to backfire. Backfires are annoying and funny, never game-ending.
2. **The room is part of the game.** Sabotage changes the physical space around the table: lighting, sound, props.
3. **Plan it, then watch it play out.** Both players choose their cards in secret, then the camera pulls back and the round plays out as a show.
4. **Skill beats luck.** Deck choice, card order, and character choice decide most games. Luck spices them up.

### 1.4 Player Experience Goals

- "I can't believe that backfired!" moments that both players laugh at.
- Planning a turn feels like a puzzle: what order, how much energy, risk the sabotage or not.
- Matches are short, about 2–3 minutes.

---

## 2. Inspirations

| Reference | What we take from it |
|:--|:--|
| **Dirty 20 Nerd Bar** ([dirty20bar.com](https://www.dirty20bar.com/)) | The setting. Exposed red steel trusses and pipes, hanging pendant lights, brick walls, a giant beholder sculpture on a chain, TVs, game shelves, wood tables, hazard-striped step. |
| **The Amazing Digital Circus** (episode 5 bar) | Bar layout: long dark counter with rounded ends and a neon strip along the bottom, stools, neon signs, framed pictures. |
| **Dandy's World** (Roblox) | Hero art style: big round heads, stubby bodies, soft pastel shading, flat cartoon faces. |
| **D&D night in real life** | The feeling: friends at a table, competitive, goofy, not actual fantasy. |
| **Inscryption** | First-person view seated at a table, opponent sitting across, the room as part of the game. |
| **Hearthstone** | Tavern setting, chunky satisfying card animations, cards that "hit" with weight. |
| **Slay the Spire** | Energy per turn, readable card text, planning a turn as a puzzle. |

---

## 3. Setting & Tone

### 3.1 World

The game takes place at a table in a nerd bar modeled on Dirty 20. Players are bar regulars playing a card game against each other to climb the bar's made-up league ranks. It is real life, not a fantasy world. The "magic" is the card game, and sabotage spills out into the real room.

### 3.2 Tone

Goofy, competitive, friendly trash talk. Chaos is funny, not scary.

### 3.3 The Room as a Game Element

The bar is full of things that can be sabotaged. Each one is a chance for a lighting, VFX, sound, or animation moment. See [Section 10.3](#103-sabotage-cards) for the starter Sabotage cards that use them.

| Bar element (from Dirty 20 reference) | Used by |
|:--|:--|
| Hanging pendant lights / light box | Blackout |
| Breaker box | Short Circuit |
| Beholder sculpture on chain | Beholder Drop |
| Drinks on the table | Spilled Drink |
| Wall TVs, overhead pipes | Future sabotage ideas |

---

## 4. Art Direction

### 4.1 Environment

- 3D, modeled on the Dirty 20 reference photo: warm and slightly grungy, red steel, brick, wood, dark chairs, glowing TVs.
- Lighting matters for gameplay. Sabotage effects change the lights (blackout, flicker, sparks), so the room is built to be relit at runtime.
- The players' table sits in the middle of the room. Along the back wall is a bar counter (long, rounded ends, neon strip along the bottom) with stools in front.
- Behind the counter: shelves of drink bottles in different shapes and sizes, and mirrors.
- Wall decorations: neon signs and framed pictures.

### 4.2 Bar Patrons (the players)

- The two players seated at the table are "average humans": ordinary bar-goers, not toons.
- Your opponent sits across the table from you. Their hand is visible only as face-down cards.
- **Background patrons** sit at the bar counter for atmosphere. They are ordinary humans too, and they don't play.

### 4.3 Heroes (card art)

- Heroes appear **on cards only**.
- Style follows Dandy's World: big round heads, small stubby bodies, smooth soft pastel shading, flat cartoon faces with thick black outlines, and one simple signature accessory.
- Card art is **rendered from 3D toon models** inside Unity. A model is posed in front of a camera, and that image becomes the card art.
- Card art can be animated (idle bob, blink, face-texture swap when attacking or hit).

### 4.4 Cards

Card shapes come from Malcolm's sketch (`docs/IMG_7151.jpg`). Each card type has a distinct silhouette so you can tell them apart at a glance.

| Card type | Shape | Frame color [DEFAULT] |
|:--|:--|:--|
| **Character** | Arched top, flat bottom (like a doorway) | Class color |
| **Attack** | Rounded rectangle | Red |
| **Sabotage** | Rounded rectangle | Yellow/black hazard stripe (matches the bar's step) |
| **Ability** | Rectangle with flared, pointed corners | Gold |

**Card layout [DEFAULT]:**

| Area | Character card | Attack / Sabotage card | Ability card |
|:--|:--|:--|:--|
| Top-left badge | Class icon | Energy cost | Special Energy cost |
| Top | Name | Name | Name |
| Middle | Hero art (large) | Hero art in action | Hero art, special pose |
| Banner | Class | "ATTACK" or "SABOTAGE" | "ABILITY" |
| Text box | Ability name | Effect text | Effect text |
| Bottom-left | HP | Damage (Attack) / Reversal % (Sabotage) | — |
| Bottom-right | Energy Max / Regen | Room effect icon (Sabotage) | — |

Soft goal: card design stays printable as a physical card game later.

### 4.5 Asset Sources

1. Free and paid asset packs first (Unity Asset Store, Kenney, Synty, Mixamo for human animations, and similar).
2. AI-generated art or 3D models as a placeholder, or as a final asset when no suitable pack exists.
3. Gray-box placeholders are fine while we test the rules.

---

## 5. Camera & Presentation

| Moment | Camera |
|:--|:--|
| **Choosing cards** | First-person from the seat of the player who is picking. Your hand is in front of you, your opponent sits across with face-down cards, the bar is around you. |
| **Resolving the round** | After both players have picked, the camera moves directly overhead. Both players' cards are revealed on the table together, then each phase (Abilities, Attacks, Sabotage) plays out for both players at once, showing each player's numbers. |
| **Sabotage cutaways** | Some Sabotage cards cut to the prop they affect (Blackout cuts to the light box). Others play out at the table. Each Sabotage card has a Cutaway yes/no setting. |
| **Hot-seat handoff** | Between picks, a "Pass to [Player]" screen hides the hand so the other player can't peek. |

---

## 6. Core Gameplay Loop

```
Match start
  → Each player picks a hero + prebuilt deck
  → Shuffle, draw 5
  → Repeat rounds:
       Both draw 1 → P1 chooses cards (first-person) → pass → P2 chooses cards → Reveal and resolve (overhead) → Check win
  → Match end: winner shown
```

---

## 7. Rules

### 7.1 Match Setup

1. Each player picks one hero (Character card) and a prebuilt deck.
2. Each player has their **own** deck. Each deck is shuffled once.
3. Each player draws 5 cards.
4. Energy starts at Energy Max. Special Energy starts at 0.
5. The player whose hero has higher **Initiative** picks first every round, and their cards resolve first within each phase. Tie: random. **[DEFAULT]**

### 7.2 Round Structure

A match is played in **rounds**. Each round both players pick their cards in secret, then everything resolves together.

**1. Start of Round** (both players)
- Draw 1 card from the top of your deck. If your hand is full (7 cards), skip the draw.
- Gain Energy equal to your hero's Energy Regen (up to Energy Max).
- Apply any "at the start of the next round" Sabotage effects.
- Block left over from the last round disappears.

**2. Choose Phase** (first-person camera)

The players pick one after the other (higher Initiative first), passing the seat in between. Picks are secret: while you pick, you see your opponent's Energy as it was at the start of the round. **[DEFAULT]**

Select cards in any order. They always play out in this order:

| Step | Card | Rule |
|:--|:--|:--|
| 1 | Ability (optional) | Max 1 per round. Costs Special Energy. Must be followed by another card. |
| 2 | Attack(s) | At least 1 required. Costs Energy. |
| 3 | Sabotage (optional) | Max 1 per round. Must be last. |

- Energy is reserved as you select cards.
- **Withdraw:** you can take back any selected card before pressing End Turn. Its cost is refunded.
- **Basic Attack:** if you can't afford any Attack card in your hand (or have none), you may select the Basic Attack instead.
- **End Turn** is only enabled when your selection is legal (at least 1 Attack, ordering rules met). Pressing it locks in your cards.

**3. Resolve Phase** (overhead camera, after both players lock in)
- Both players' cards are revealed, then resolve phase by phase: both players' Abilities, then both players' Attacks, then both players' Sabotage.
- Within a phase, the higher-Initiative player's cards resolve first, in the order they were selected. **[DEFAULT]**
- Each Attack that deals damage earns Special Energy (see 7.4).
- Knockouts are checked at the end of each phase, so a hero at 0 HP still finishes that phase. If a hero is at 0 HP, the match ends. If both are, it's a draw.

**4. End of Round**
- All played cards go face-down to the **bottom** of their owner's deck. The deck is never reshuffled, so cards come back around in a predictable order.

### 7.3 Card Types

| Type | Where it lives | Cost | Limits |
|:--|:--|:--|:--|
| **Attack** | Deck / hand | Energy | At least 1 per round |
| **Sabotage** | Deck / hand | Energy (can be 0) | 1 per round, 3 per match |
| **Ability** | Separate Ability slot next to your hero (never in deck) | Special Energy | 1 per round, can't be last |
| **Basic Attack** | Always available, not a card in your deck | 0 Energy | 5 damage, no Strength bonus. Only when you can't afford any Attack card. Counts as your Attack for the round. |

**Dead Sabotage cards:** once you have played 3 Sabotage cards in a match, any Sabotage card in your hand is dead. It can't be played and stays in your hand.

### 7.4 Energy & Special Energy

| Stat | Meaning |
|:--|:--|
| **Energy** | Spent on Attack and Sabotage cards. Starts at Energy Max. |
| **Energy Max** | Energy cap. |
| **Energy Regen** | Energy gained at the start of each round. |
| **Special Energy** | Spent on Ability cards. Separate pool. Starts at 0, max 6. **[DEFAULT]** |
| **Special Energy Gain** | Special Energy earned each time one of your Attacks deals damage to your opponent. |

Some Sabotage cards cost 0 Energy, give Energy to the player, or steal Energy from the opponent.

### 7.5 Damage

```
Attack damage  = card damage + hero Strength
Damage taken   = Attack damage − Block (Block is used up first)
```

- Strength applies to Attack cards only (not Basic Attack, not Sabotage).
- **Block** absorbs damage. Unused Block disappears at the start of the next round.

### 7.6 Sabotage & Reversal

Each Sabotage card has:

- **Effect:** what happens to the opponent.
- **Room effect:** what happens in the bar (lights, sparks, props).
- **Cutaway:** whether the camera cuts to the affected prop.
- **Reversal chance:** odds it backfires. Stronger cards have a higher chance.
- **Reversal effect:** what happens instead when it backfires, usually hitting the player who played it.

Reversal rule: roll a random number from 0 to 1. If the roll ≤ the card's reversal chance, it backfires.

Reversal effects must be:

| Must be | Must NOT be |
|:--|:--|
| Annoying | Instantly game-ending |
| Funny | Unavoidably losing |
| Recoverable | Catastrophically punishing |

### 7.7 Win Conditions

| Condition | Result |
|:--|:--|
| Opponent's HP reaches 0 | You win |
| Opponent forfeits | You win |
| Both heroes reach 0 HP in the same round | Draw |
| 8 rounds played **[DEFAULT]** | Higher HP wins. Tie = draw. |

### 7.8 Rule Details

| Topic | Rule |
|:--|:--|
| **Block** | Absorbs all damage, including Sabotage damage (e.g. Beholder Drop). |
| **Healing** | Can't go above the hero's starting HP. |
| **Basic Attack** | Earns Special Energy when it deals damage, like any Attack. |
| **Haymaker** | Doubles (card damage + Strength) of your next Attack this round, including Big Finish's bonus, both Double Tap hits, or Basic Attack. |
| **Block in the Attack phase** | Block gained from an Attack card (Counter Stance) only stops Attacks that resolve after it. The higher-Initiative player's Counter Stance can block the other player's Attacks that round; the slower player's comes too late. |
| **Special Energy gain** | Earned only when an Attack removes HP. A fully blocked Attack earns none. Poke's +1 bonus follows the same rule. |
| **Backfire knockout** | If a backfire drops the saboteur to 0 HP, the opponent wins (a draw if both heroes are at 0). |
| **Big Finish** | Checks opponent HP at the moment Big Finish resolves (after your earlier Attacks this round, and the opponent's if they resolved first). |
| **Short Circuit** | Opponent loses 2 Energy at the start of the next round (after Regen). The player who played it gains 2 Energy at the start of the next round (up to Max). |
| **Spilled Drink** | The +1 cost applies next round, to Attack cards only, not Basic Attack. |
| **Blackout (blind play)** | Playing blind, you can see each card's type (Attack, Sabotage) and its frame color, but not its name, cost, or effect. If a blind pick can't be afforded or breaks ordering rules, the card is revealed briefly and returns to the hand. No penalty. |
| **Round limit** | 8 rounds. Each round both players pick once. (Was 16 alternating turns, 8 each.) |

### 7.9 Removed for MVP

- **Accuracy:** removed. Cards always hit, so the only luck is Sabotage reversal.
- **Gain Energy at 0:** removed. Basic Attack covers being out of Energy.

---

## 8. Characters

### 8.1 Stats

| Stat | Meaning |
|:--|:--|
| HP | Health. 0 = lose. |
| Strength | Added to each Attack card's damage. |
| Initiative | Higher goes first. |
| Energy Max / Energy Regen / Special Energy Gain | See 7.4 |
| Stealth | Avoid being targeted. Only matters in future team modes. |

### 8.2 Classes

| Class | Identity |
|:--|:--|
| **Fighter** | Direct damage. High Strength. |
| **Tank** | Huge HP, hard to kill. |
| **Stealth** | Avoids targeting. High Initiative. |
| **Support** | Heals, buffs, protects. Needs a 1v1-friendly version of its abilities. |
| **Minemaster** | Special Energy specialist. Gets abilities out more often. |

MVP uses Fighter and Tank. The other classes come later.

---

## 9. Decks

| Rule | Value |
|:--|:--|
| Deck contents | Attack + Sabotage cards (Ability is separate) |
| Max Attack cards | 8 |
| Max Sabotage cards | 3 |
| Deck size | 11 (8 Attack + 3 Sabotage) **[DEFAULT]** |
| Starting hand | 5 |
| Max hand size | 7 **[DEFAULT]** |
| Duplicates | Allowed |

For MVP, players choose from **prebuilt decks**. Collection and deck building come later.

---

## 10. Starter Content (MVP)

Heroes (10.1) are approved. Cards and all numbers are starting values to tune through playtesting. Numbers target a match of roughly 3–5 rounds.

### 10.1 Heroes

| | **Bonk** | **Pudge** |
|:--|:--|:--|
| **Class** | Fighter | Tank |
| **Look** | Pastel orange round head, determined grin, stubby arms with oversized boxing gloves | Mint green, extra-round body, sleepy happy face |
| **Accessory** | Red sweatband | Saucepan worn as a helmet |
| **HP** | 80 | 110 |
| **Strength** | 3 | 0 |
| **Initiative** | 5 | 2 |
| **Energy Max / Regen** | 6 / 3 | 6 / 3 |
| **Special Energy Gain** | 1 | 1 |

### 10.2 Ability Cards

| Hero | Name | Cost (SE) | Effect |
|:--|:--|:--|:--|
| Bonk | **Haymaker** | 3 | Your next Attack this round deals double damage. |
| Pudge | **Pot Lid Guard** | 3 | Gain 20 Block. |

### 10.3 Attack Cards

| Name | Cost | Damage | Extra effect |
|:--|:--|:--|:--|
| **Jab** | 1 | 10 | — |
| **Poke** | 1 | 8 | Earn +1 extra Special Energy |
| **Counter Stance** | 1 | 6 | Gain 6 Block |
| **Swing** | 2 | 18 | — |
| **Double Tap** | 2 | 8 ×2 | Hits twice (each hit earns Special Energy, Strength applies to each hit) |
| **Second Wind** | 2 | 10 | Heal 8 HP |
| **Body Slam** | 3 | 25 | — |
| **Big Finish** | 3 | 20 | +10 damage if opponent has 25 HP or less |

### 10.4 Sabotage Cards

| Name | Cost | Effect | Room effect | Cutaway | Reversal % | Reversal effect |
|:--|:--|:--|:--|:--|:--|:--|
| **Blackout** | 2 | Everyone plays blind next round: you can see card types, not what they are | Light box smashes, room goes dark, only the TVs glow. Lights come back after next round's picks. Same effect on a backfire. | Yes | 35% | Only you play blind next round |
| **Short Circuit** | 1 | Steal 2 Energy from opponent at the start of the next round | Breaker box sparks near the table | No | 20% | You lose 2 Energy at the start of the next round |
| **Beholder Drop** | 3 | 20 damage to opponent | Beholder sculpture swings on its chain and slams the table | No | 45% | It swings back: you take 10 damage |
| **Spilled Drink** | 0 | Opponent's Attack cards cost +1 Energy next round | Drink tips over and soaks the table | No | 15% | Your Attack cards cost +1 Energy next round |

### 10.5 Prebuilt Decks

| Deck | Hero | Attack cards (8) | Sabotage cards (3) |
|:--|:--|:--|:--|
| **Brawler** | Bonk | Jab ×2, Swing ×2, Body Slam ×2, Double Tap, Big Finish | Blackout, Short Circuit, Beholder Drop |
| **Bunker** | Pudge | Jab ×2, Poke, Swing, Counter Stance ×2, Second Wind ×2 | Blackout, Short Circuit, Spilled Drink |

---

## 11. Audio

| Area | Direction |
|:--|:--|
| **Music** | Chill, calm bar music (lo-fi / mellow). Gets more intense as the stakes rise: within a match when a hero drops low on HP **[DEFAULT: below 25%]**, and in future at higher league ranks. |
| **Ambience** | Crowd chatter, clinking glasses, TV noise. |
| **Cards** | Draw, select, withdraw, play, hit. |
| **Sabotage** | Power-down hum (Blackout), electrical zap (Short Circuit), chain creak and crash (Beholder Drop), splash (Spilled Drink). |

---

## 12. Screens & UI

| Screen | MVP contents |
|:--|:--|
| Title | Play, Quit |
| Match setup | Each player picks hero + prebuilt deck |
| Hot-seat handoff | "Pass to [Player]" + Ready button |
| Match HUD | HP, Block, Energy, Special Energy, cards left in deck, sabotages used (x/3), round counter, End Turn button |
| Results | Winner, Rematch, Main menu |

---

## 13. Milestones

| # | Milestone | Goal |
|:--|:--|:--|
| **M1** | Gray-box hot-seat | Full rules playable in Unity with plain boxes and text cards. Two players, one PC. |
| **M2** | The bar | 3D bar environment, lighting, camera moves, seated opponent, first room effect (Blackout). |
| **M3** | Card art & polish | Toon hero models rendered to card art, card animations, audio. |
| **M4** | Bot opponent | Simple AI that picks legal cards. |
| **Future** | Online & ranks | Teams of players on different machines, ranked league play, character progression, card collection. |

---

## 14. Team & Decisions

| Role | Person | Owns (final say) |
|:--|:--|:--|
| **Game Director** | Malcolm | How the game plays and looks: rules, cards, heroes, art direction, sabotage ideas. |
| **Tech Lead / Producer** | Brian | How the game is built: code, tools, Unity setup, and what fits in each milestone. |

**Breaking ties:**
1. Each person explains their idea and why (2 minutes each, no interrupting).
2. If it's about fun or feel, try both versions on paper or in a quick test, then decide.
3. Still stuck? The owner of that area decides.
4. Good ideas that don't fit the current milestone go in the **Parking Lot** (below), not in the trash.

### 14.1 Parking Lot

Ideas we like but aren't building yet.

- Wall TV and pipe sabotage cards
- Stealth, Support, and Minemaster heroes
- More intense music at higher league ranks

---

## 15. Technical Direction

| Area | Choice |
|:--|:--|
| Engine | Unity 6 LTS (latest patch) |
| Render pipeline | URP |
| Language | C# |
| Card and hero data | ScriptableObjects |
| Card art | 3D toon models rendered to textures in Unity |
| Dev machine | MacBook Pro, driven via Unity CLI + coding agent |
| Play machine | Windows 11, RTX 4070 Super |
| Builds | Windows build from Mac (Mono backend) **[DEFAULT]** |
| Version control | Git + Git LFS (art, audio, models) |
| Save data | Local only (MVP) |

Design the rules engine so it does not depend on UI or camera code. That keeps a bot (M4) and online play (Future) possible without rewriting the rules.

---

## 16. Open Questions

| # | Question | Owner |
|:--|:--|:--|
| 1 | Tune cards and numbers in Section 10 through playtesting | Malcolm |
| 2 | Dead Sabotage cards stay in hand and take up hand space. Revisit once M1 is playable: does it feel bad in long matches? | Malcolm |

---

## 17. Out of Scope (MVP)

- Online multiplayer, teams (2v2/3v3), ranks
- Character XP, leveling, progression
- Card collection, deck building, shop, currency
- Friends, chat, accounts
- Lore / story mode
- Accuracy stat

---

## 18. Glossary

| Term | Meaning |
|:--|:--|
| **Hero / Character** | The toon on your Character card. Has class, stats, and one Ability. |
| **Patron** | A human player seated at the bar table. |
| **Hot-seat** | Two players take turns on the same PC. |
| **Round** | Both players pick their cards in secret, then all the picked cards resolve together. |
| **Block** | Absorbs incoming damage until the start of the next round. |
| **Withdraw** | Take back a selected card before End Turn and refund its cost. |
| **Room effect** | A change to the 3D bar caused by a Sabotage card. |
| **Cutaway** | Camera cuts to the bar prop a Sabotage card affects. |
| **Reversal / Backfire** | A Sabotage card's effect hits the player who played it instead. |
| **Cycling** | Played cards go to the bottom of your deck and come back around. |
| **Dead card** | A Sabotage card that can't be played because you've used all 3 for the match. |

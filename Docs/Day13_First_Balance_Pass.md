# Day 13 — First balancing pass

Five numbers changed, on five assets. **No `.cs` in the game changed, no save field
moved, and not one `BalanceCanary` needed updating** — which is not the good news it
sounds like, and is most of what this document is about.

The day inherited a live question from §7 of `Docs/Day12_Roster_And_Parties.md`: a
ten-hour bracket between two model policies, with instructions to decide which of them
the game should encourage. It turned out not to be a question about the player.

---

## 1. The bracket was a price tag

Day 12 left two runs on unchanged assets and called them the extremes of one policy
decision:

> ranking swaps by fully-trained potential gives sixteen Legendaries and a 28-hour game,
> strict improvement gives no Legendary and 17h45m, and a real player who wants a
> Champion sits between.

The instinct to distrust that framing came from the Ledger's own standing warning —
*when a run says something is pointless, check the policy can reach it before believing
the answer*. It applies here, but pointing the other way than usual. The policy was not
failing to reach the content. **The content was making the policy look insane.**

Here is the whole thing, in one table the model could have printed at any point in the
last three days and never had a reason to:

| archetype | power at 25 | lifetime gold | **gold per point of power** |
|---|---:|---:|---:|
| Militia Recruit | 71.6 | 88,493 | **1,236** |
| Hedge Knight | 143.2 | 265,523 | **1,854** |
| Wandering Ranger | 286.4 | 796,808 | **2,782** |
| Arcane Battlemage | 572.8 | 2,391,625 | **4,175** |
| Dragonsworn Champion | 1,145.6 | 7,180,876 | **6,268** |

Lifetime gold is the hire plus every training level. Each band doubles power — that
much was authored deliberately on Days 10–11 and is pinned by a canary — but each band
**tripled** the training cost, because the bases were 20 / 60 / 180 / 540 / 1620 at a
common 34% growth. So every step up the ladder costs 1.5x more gold for the same power,
and a Legendary bed costs **81x** a Common bed to realise while returning **16x** the
power.

Rarity was strictly dominated on the gold axis. Not weak, not marginal — dominated, at
every band, for the entire game.

That is what made Day 12's greedy rule look like an arbitrage bug. It was not
arbitraging anything; it was buying the best archetype available, which is what a
player does, and being charged ten hours for it. **The behaviour was never wrong. The
price of the behaviour was.**

### Three days of looking straight at it

Worth recording, because the failure mode is more useful than the fix:

- **Days 8–9** modelled the whole arc and concluded higher rarities were pointless next
  to the Training Room's guild-wide bonus. True, and not the reason.
- **Days 10–11** found the model's hiring rule had never bought a non-Common at all,
  fixed it, and concluded the *Inn's bed ratchet* was the reason. Also true, also not
  the reason.
- **Day 12** removed the ratchet, watched the impatient player still field no Legendary,
  and concluded the structural lock had become an *economic* one. True in the narrowest
  possible sense, and still not the reason.

Every one of those three looked at power, at gates, and at the player. None looked at
the price list. The lesson generalises past this project: **a ratio that is authored in
one place and paid for in another will not be checked by anybody looking at either.**
Power lives on `_powerByLevel`, price lives on `_trainingCostToReachLevel`, and no
document, test or model run had ever divided one by the other.

---

## 2. What changed

The per-band training multiple, from ×3 to ×2, so that it matches the power multiple.

| archetype | `_trainingCostToReachLevel` BaseValue | | first training level |
|---|---|---|---|
| Militia Recruit | 20 | *unchanged* | 26.8 |
| Hedge Knight | 60 → **40** | | 53.6 |
| Wandering Ranger | 180 → **80** | | 107.2 |
| Arcane Battlemage | 540 → **160** | | 214.4 |
| Dragonsworn Champion | 1620 → **320** | | 428.8 |

`LinearPerLevel` stays 0 and `GrowthPerLevel` stays 0.34 on all five. Nothing else in
the game moved — no power curve, no recruit cost, no building tree, no tier gate, no
quest, no `maxLevel`.

The rule is now stateable in one line, which is the standard Days 10–11 set for this
ladder and the reason it is generated rather than hand-picked: **each band doubles the
archetype's power and doubles the gold it takes to train that power out.** Gold per
point of power comes out flat at 1,236 → 1,249 across all five bands, the 1% drift
being the recruit-cost ladder, which climbs 5x per band and is about 1% of what a bed
costs over its life.

### Why ×2 and not less

A bed is capped and a wallet is not. With the multiple at exactly 2, the player is
indifferent to rarity *on gold* and buys it for the only thing that is actually scarce —
the sixteenth bed. That is the design the Inn has always implied, and it is the first
time the numbers have agreed with it.

Below 2 the model stops making sense rather than getting better:

| per-band training multiple | patient (Capital / maxed) | impatient |
|---|---|---|
| ×3.00 *(shipped until today)* | 5h41m / 28h17m | 5h08m / 32h07m |
| ×2.50 | 5h37m / 22h49m | 4h09m / 27h38m |
| **×2.00** | **5h54m / 18h14m** | **6h56m / 19h33m** |
| ×1.75 | 5h42m / 15h43m | 5h05m / 18h33m |
| ×1.50 | 5h51m / 17h02m | 4h21m / 13h03m |

At ×1.75 and ×1.50 the impatient player **finishes the game before the patient one**,
which is incoherent — patience is supposed to cost nothing and save a little. ×2.00 is
the only row where the two profiles are ordered correctly and the gap between them is
small. It is also the row that needs no justification beyond the rule.

---

## 3. The bracket collapses

The interesting confirmation is not that the new numbers are better. It is that the
question Day 13 was handed stops existing.

Three swap policies — Day 12's rejected greedy rule, Day 12's shipped strict rule, and
the pragmatic one described in §4 — against the old ladder and the new one:

| training band cost | policy | patient | impatient |
|---|---|---|---|
| ×3 *(old)* | strict | 22h50m, 5C 1U 5R **4L** | 17h45m, 3U 4R 9E — **no Legendary** |
| ×3 *(old)* | greedy | 28h16m, 16 Legendary | 27h06m, 16 Legendary |
| ×3 *(old)* | pragmatic | 28h17m, 16 Legendary | 32h07m, 16 Legendary |
| **×2** | strict | 14h30m, 5C 1U 6R 4L | 14h23m, 4U 3R 9E |
| **×2** | greedy | 18h15m, 16 Legendary | 18h05m, 16 Legendary |
| **×2** | **pragmatic** | **18h14m, 16 Legendary** | **19h33m, 16 Legendary** |

Under the old ladder the three policies span **ten hours**. Under the new one they
agree inside **eighty minutes**, and greedy and pragmatic — the two that were supposed
to be opposite extremes — land one minute apart. There was never a policy fork to
choose inside; there was a ladder priced so badly that how you walked it mattered more
than where you ended up.

The strict arm is the exception and it deserves its own sentence, because it is the one
that still reports no Legendary at ×2. That is not the content talking. See below.

---

## 4. The third arm, and the 6% that hid inside it

`guild_model.py`'s swap rule required a **level-1** replacement to already beat the
incumbent. Nobody plays that way — you do not buy a Champion, field it at level 1 next
to your maxed Militia Recruit, and shrug. And the rule does not miss by a lot:

| | power at a maxed Training Room | cumulative hire + training |
|---|---:|---:|
| maxed Militia Recruit | **403.0** | — |
| Dragonsworn Champion, level 1 | 379.4 | 15,000 |
| Dragonsworn Champion, level 2 | 395.2 | 15,429 |
| Dragonsworn Champion, **level 3** | **412.5** | 16,003 |

**Six percent.** Two training levels, about a thousand gold under the new ladder, at a
point in the game where the guild earns that in seconds. The "economic lock" Day 12
reported as having replaced the structural one was a rule that never looked at level 3.

`switching_cost()` replaces it: the hire, plus training the replacement up to the first
level that beats what the incumbent has already been trained to. It names no threshold
and no magic number — the incumbent's current power *is* the bar, so sunk training is
respected the way Day 12 wanted, without the rule being blind to the fact that
replacements can be trained too. Charging the catch-up in one step is a modelling
convenience against a real game that would be a hire and a run of training taps; it is
the same gold, and it keeps the decision atomic so a half-finished swap cannot leave
the roster weaker than it started.

Both of Day 12's rules were straw players standing either side of that. The one it
rejected ignored the price of catching up; the one it shipped priced catching up at
infinity.

---

## 5. Where the game now sits

Against the four figures Day 12 published, which these supersede:

| | Town | City | Capital | everything maxed | decisions | final roster |
|---|---|---|---|---|---|---|
| Patient, Day 12 | 0h08m | 1h08m | 5h41m | 22h50m | 444 | 5 C, 1 U, 5 R, 5 Legendary |
| **Patient, today** | 0h08m | 1h08m | **5h54m** | **18h14m** | 551 | **16 Legendary** |
| Impatient, Day 12 | 0h08m | 1h08m | 4h16m | 17h45m | 450 | 3 U, 4 R, 9 Epic |
| **Impatient, today** | 0h08m | 1h14m | **6h56m** | **19h33m** | 553 | **16 Legendary** |

Four things worth reading off it.

**The fork is now a schedule rather than a destination.** Both profiles finish with the
same roster; impatience costs about eighty minutes and buys nothing that cannot be
bought later. That is the shape Day 12 was reaching for when it added the retire action,
and it is the first run in which the two arms are ordered the way the story says they
should be — the impatient player reaches Capital *later*, because they spent gold on
Battlemages they went on to retire.

**Everything-maxed lands at 18h14m**, against Days 8–9's 17h21m and Days 10–11's
corrected 19h37m. On the established line, and deliberately so: shortening the tail is
what Day 21's time-skip offers are for, and removing it now would remove the thing
monetisation is meant to sell against.

**The purchase-gap profile is the best this project has recorded** — median 1.5 min,
90th percentile 4 min, worst gap 19. Days 8–9 said the model exists to protect this
number and asked for 5–7 and 25; Days 10–11 delivered that; today's ladder improves it
again, because cheaper training at the top bands means more affordable purchases in the
stretches where only an expensive Tavern level used to be on offer.

**All four endgame parties sit at x3.54 of Dragon's Roost.** Uniform, because the roster
converges, and comfortably under `QuestResolution`'s 4x speed clamp — so the last
Training Room levels and the last Legendary levels still buy measurable speed, which is
the no-dead-levels rule applied to the top of the game. Thirteen percent of headroom is
not much and it is worth re-checking on Day 21.

---

## 6. The Training Room is a levelling mechanic pointed the wrong way

Named here as an open decision rather than acted on, because it is a design change and
not a tuning one, and because it cascades into every quest's Recommended Power.

`Adventurer.PowerWith` adds the Training Room's bonus **flat** to every adventurer. A
flat bonus is by construction worth most to the weakest person it touches: +331 on a
maxed Militia Recruit's 71.6 is +462%, on a maxed Champion's 1,145.6 it is +29%. So the
building whose stated job is "raises each adventurer's Power" is in practice an
equaliser, and the authored 16x rarity ladder is worth this much by the time the guild
is finished:

| Training Room | flat bonus | Common → Legendary |
|---|---:|---:|
| level 0 | +0.0 | **x16.0** |
| level 10 | +6.5 | x14.8 |
| level 20 | +24.1 | x12.2 |
| level 30 | +89.4 | x7.7 |
| **level 40** | **+331.4** | **x3.7** |

The same shape has a second face that is uglier: because a new hire arrives carrying the
full guild bonus for free, a **25-gold Militia Recruit is worth +331 power at a finished
guild — 0.1 gold per point** — while a training level at the same moment costs between
1,576 and 7,977 gold per point. Power is roughly four orders of magnitude cheaper by the
body than by the level. Only the Inn's sixteen-bed cap stops that being the dominant
strategy in the game.

Making the bonus multiplicative on the adventurer's own power fixes both halves
completely and is conceptually what the building already claims to do. It costs: one
line in `Adventurer.PowerWith`, a neutral base of 1.0 for `AdventurerPower` in
`GuildState`, a `ModifierKind` change on the Training Room asset, re-deriving all five
quests' Recommended Power, re-spacing the tier gates (the model puts Town at 13m rather
than 8m otherwise), and four canaries. The model says it adds roughly seven hours to the
tail before any of that re-tuning.

**Deferred to Day 21** — the second balancing pass, which already exists on the roadmap —
for two reasons. The compression bites hardest between Training Room 30 and 40, by which
point the roster has converged to Legendary anyway, so what it costs today is *feel* at
the climax rather than balance. And the cheap-body exploit is fenced by the bed cap, so
it is a latent inversion rather than a live one. Neither is worth spending the day that
also has to leave Day 14 a stable set of numbers to play against.

`GuildStat.AdventurerPower` is persisted in saves by ordinal, so this change must never
renumber the enum — only reinterpret the value. It needs no migration for the same
reason Days 10–11's Max Level change needed none, and is precisely the kind of change
the Ledger warns slips past: **the meaning of a value moving while its shape does not.**
A fixture is owed on the day it happens.

---

## 7. Verification

Every number in this document came from `Docs/tools/guild_model.py`, updated in the same
commit as the assets per the standing rule. Two things changed in it, and they are the
two halves of the same finding: the training curves, and `switching_cost()` replacing
the level-1 swap rule. It also gained the gold-per-point-of-power table under `--checks`,
so the defect this day fixed is one the model can now show rather than one it can only
be told about.

The suite went from 64 to 66 and no existing test moved.

| test | kind | covers |
|---|---|---|
| `AHigherRarityBandNeverCostsMoreGoldPerPointOfPower` | **invariant** | a band never costs more gold per point of power than the band below, within 10% slack for the recruit ladder |
| `TheTrainingLadderReadsAsWritten` | **BalanceCanary** | the five first-level training costs as figures — 26.8 / 53.6 / 107.2 / 214.4 / 428.8 |

Two tests rather than one because they fail in different directions. The invariant
catches the *shape* going wrong again and will survive any future retune that keeps
rarity honest, so Day 21 should not have to touch it. The canary catches a single
mistyped figure in a single asset — this project's most expensive and most repeated
failure mode, the one that gave a level-1 Inn fifty beds on Day 4–5 — which the
invariant would sail straight past, since a slip that scaled all five bands equally
leaves every ratio intact.

### That no canary moved is the finding, not the reassurance

The handoff for this day said in as many words that updating a canary is part of a
balance pass and updating an invariant is a warning. Neither happened. A pass that moved
the endgame by four and a half hours and collapsed a ten-hour policy spread to eighty
minutes ran without disturbing a single value-asserting test — because **there had never
been a canary on a training cost.** Five of the seven watch quest resolution, one
watches the Inn's beds, one watches the rarity power ladder. The price of that ladder
was watched by nothing.

Which is worth stating as a general shape, because it is the same one §1 ends on wearing
different clothes: **a canary set that does not watch a value is quieter than no canary
set at all, because its silence reads as a pass.** The four quiet days were not four days
of nothing changing.

### Still by hand

Nothing was added to the manual list. The twenty-five minutes carried from Days 10–11
and Day 12 stands, and most of it still wants Day 14's played-in save. One item is worth
re-reading in light of today: **step 4, "Town in about ten minutes"**, is unaffected —
the opening is untouched and the model still reports 8m — but Day 14 is now the first
chance to judge whether a Champion *feels* like the reward Capital exists to hand over,
which §6 says it currently should not.

---

## 8. Files

Five assets, one model, one test file, two documents. **No game code.**

```
Data/Adventurers/Adventurer_HedgeKnight.asset          training base 60   -> 40
Data/Adventurers/Adventurer_WanderingRanger.asset      training base 180  -> 80
Data/Adventurers/Adventurer_ArcaneBattlemage.asset     training base 540  -> 160
Data/Adventurers/Adventurer_DragonswornChampion.asset  training base 1620 -> 320
Data/Adventurers/Adventurer_MilitiaRecruit.asset       unchanged (the ladder's anchor)

Docs/tools/guild_model.py          the ladder, switching_cost(), the g/power table
Tests/Editor/AssetInvariantTests.cs  + the invariant and the canary
Docs/Tests.md                      66 tests, and why none of the old ones moved
GUILD_LEDGER.md                    status, resolved list, session log, continuation
```

The architectural bet is untouched, and this day did not test it — no code ran near
`BuildingDefinition` or `GuildState.Aggregate`. §6 is the one that would, and it is the
harder version the Ledger has been expecting: a change to what a **stat** means rather
than a change to the content that consumes it.

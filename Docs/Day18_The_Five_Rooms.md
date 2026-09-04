# Day 18 — the five rooms as assets, and the half of the revision nobody had built

The day the `.asset` files finally changed, and the day described in advance more than any
other. §8 of `Vision_Revision.md`'s third item: *"The five rooms as assets: Barracks and
Provisioner authored, effects moved, Training Room retired, tier gates re-derived."*

It did all of that. It also found that **authoring the rooms does not make the revision
true**, which is §4 and is worth more than the assets.

---

## 1. What changed

| | |
|---|---|
| **New** | `Building_FrontDesk`, `Building_Barracks`, `Building_Provisioner` |
| **Rewritten** | `Building_Tavern`, `Building_Inn` |
| **Retired** | `Building_TrainingRoom` — unlisted from `GameContent`, and awaiting deletion in the Project window |
| **Rewritten** | all four `Tier_*.asset`: market size, base service, base housing, and the gates re-derived onto the new rooms |
| **Rewritten** | `GameContent.asset` — five rooms in, the Training Room out |
| **Code** | `HallPlan.Default()` — five footprints where three stood |
| **Tools** | `Docs/tools/author_rooms.py`, `author_tiers.py`, `check_assets.py` |

**No game `.cs` outside `IdleGuild.World` changed, no save field moved, and
`SaveSchema.CurrentVersion` still has never been bumped** — on the eighth occasion it could
have been.

### The numbers were not typed

`author_rooms.py` and `author_tiers.py` read `tuned_params.json` through `tuner.build()`
and write the YAML. Nothing in this day's assets was transcribed by hand, and the two
derived curves are computed rather than pasted. That is Day 4–5's lesson taken seriously:
four transcription slips out of fourteen assets, and the one that mattered handed the Inn
its own *cost* curve as its bed curve, so a level-1 Inn granted fifty beds. **A wrong curve
looks exactly like a right one in the Inspector**, and it looks exactly like a right one in
a markdown table too.

`check_assets.py` then reads the finished YAML *back* and re-derives every claim made about
it — the ceilings, the gates, the levers, the opening, and what a Village guild actually
earns through a Python transcription of `TradeService.Allocate`. It reports **0 failures**.
It is checked in because the balance pass will move all of these numbers and will want the
same check.

---

## 2. The rule the day was authored under

> **The rooms' trade economy is ported from the tuned model exactly. The contract economy
> is not ported at all: every effect that has to move house keeps the value it has today at
> its old building's maximum, re-spaced onto its new building's tree. Quests, adventurers,
> reputation thresholds and starting gold are untouched.**

The reason is §4. The model's contract economy and the game's are **different mechanisms**,
so the model's contract numbers describe a game this build is not. Its *room* numbers
describe one it now is, exactly.

The happy consequence of re-spacing rather than re-tuning is that only the growth rate
changes and the **base is left alone** — so a level-1 Front Desk is worth precisely what a
level-1 Tavern was, and a level-1 Barracks precisely what a level-1 Training Room was.
Every contract canary in the suite is unchanged to the digit; the tests moved a room name
and not a number. That is the strongest evidence available that the move was a move.

### The four effects that changed address

| effect | from | to | how |
|---|---|---|---|
| **Reward Yield** | Tavern, 90 levels, ×1.08 | **Front Desk**, 52 levels | growth → **×1.143741**, ceiling **188.688** unchanged |
| **Adventurer Power** | Training Room, 40 levels, ×1.14 | **Barracks**, 41 levels | growth → **×1.136272**, ceiling **331.375** unchanged |
| **Recovery Speed** | Inn, 30 levels, ×1.10 | **Barracks**, 41 levels | growth → **×1.071543**, ceiling **1.5863** unchanged |
| **Housing Capacity** | Inn, 30 levels, +0.5/level | **Barracks**, 41 levels, **+ 2 from the tier** | linear → **+0.3125**, ceiling **16** unchanged |

The arithmetic, for the day somebody has to do it again: to put a base *b* on a tree of
*N* levels where it used to sit on a tree of *M*, the new growth is
`(1 + g)^((M − 1) / (N − 1)) − 1`, and the new linear step is
`(b + l(M − 1) − b_new) / (N − 1)`.

**Reward Yield had to leave the Tavern**, and not merely because §2 of the charter says so.
Days 8–9's structural finding was *only the Tavern compounds*, which is why it has the long
tree. A Tavern that both multiplied contract gold **and** generated its own room revenue
would compound twice and bury the other four rooms.

---

## 3. The authored tables

### The rooms

| | Tavern | Front Desk | Barracks | Inn | Provisioner |
|---|---|---|---|---|---|
| available from | Village | Village | **Town** | **Town** | **City** |
| levels | 90 → **57** | **52** | **41** | 30 → **53** | **48** |
| build cost | 50 → **26.28** | **52.56** | **1,385.80** | 40 → **3,118.05** | **31,180.50** |
| cost growth | 0.15 → **0.11** | **0.11** | **0.11** | 0.21 → **0.11** | **0.11** |
| Service Seats | **4, +0.9914** | — | — | **2, +0.5948** | **3, +0.5948** |
| Customer Spend | **1.5, ×1.13** | — | — | **26, ×1.13** | **45, ×1.13** |
| Service Demand | **400, flat** | — | — | **20, flat** | **222.72, flat** |
| Staff Slots | **2.1275, +2.207** | — | — | — | — |
| Recruitable Rarity | **0, +0.13** *(unchanged)* | — | — | — | — |
| Reward Yield | *(gone)* | **0.2, ×1.143741** | — | — | — |
| Housing / Power / Recovery | — | — | **all three** | *(gone)* | — |

### The tiers

| | Village | Town | City | Capital |
|---|---|---|---|---|
| market size | **1** | **6.0897** | **37.0844** | **225.8332** |
| base service / hr | **5.6062** | **5.6062** | **5.6062** | **5.6062** |
| base housing | **2** | **2** | **2** | **2** |
| gate | Tavern 4, Desk 2 | Tavern 17, Desk 11 | Tavern 36, Desk 24, Inn 22, Barracks 17 | — |
| reputation | 30 | 830 | 65,000 | — |
| stipend | 1 | 2 | 4 | 8 |

**`_contractRewardScale` was deliberately not authored** and reads its neutral 1. Nothing
consumes it. Writing a value into a field nothing reads is how a room comes to look like it
earns, which is §4's whole subject, and there is now a test asserting the absence.

### Three things this makes true for the first time

**`_baseServicePerHour` has read 0 since Day 16** — the cold-start value its own tooltip
calls the failure: *"Must be above zero, or an unstaffed guild can never start trading."*
Its guard was gated on rooms producing demand rather than on itself, so it went live today
and not the day the field was added. It is now 5.6062 on every tier, and
`EveryTierCarriesABaseServiceOnceAnyRoomAsksForCustom` stopped calling `Assert.Ignore`.

**`_baseHousingCapacity` was 0 and had to stop being.** Beds moved to a Barracks the player
cannot build until Town, so without the tier's own two beds a Village guild could recruit
nobody, earn no reputation, and never reach the tier that would sell it a bed. **That is
Day 4–5's opening deadlock for the third time**, and it is closed the same way it was the
first two — in data — with the difference that §01 now has the rule written down and
`EveryTierSleepsAtLeastOneAdventurerBeforeAnythingIsBuilt` asserts it against every tier
rather than only the first.

**§3 of `World_View_Design.md` now has a producer.** Its table — 4 seats against 400 wanting
in at Village, 60 against 90,333 at the ceiling — was a claim about a spreadsheet. Reading
the shipped Tavern back gives 4 / 19 / 38 / 59 seats and 160 / 794 / 1,548 / 2,381
customers an hour against 400 / 2,436 / 14,834 / 90,333 wanting in. Those are §3's rows,
and the one-seat differences at three of them are the document having rounded where the
view floors. **Seats bind at every tier**, so the room is always turning people away and
there is always a queue outside the door: permanent visual content, confirmed rather than
hoped for.

---

## 4. THE FINDING — the contract half of the revision has never been built

§3.2 of the charter: *"The gold from a contract arrives as the **Front Desk's
commission**."* In the tuned model that is
`gold × contractRewardScale(tier) × commission(desk)`, where the commission saturates at
0.85 so a desk can never take more than a contract is worth.

**In the game a contract pays `GoldReward × RewardYield`**, unbounded, and the same
multiplier is applied to **reputation** as well as to gold. `ContractCommission` and
`ContractRewardScale` are both declared and **neither has a consumer** — not merely no
producer, which is what Day 16 recorded.

Day 16's note reads: *"`ContractCommission` is declared with no producer, exactly as
`QuestSlots`, `MaxQuestTier` and `FailureRateReduction` have been since Day 2 — the Front
Desk is authored later."* That reads as though it were waiting for an asset. **It was
waiting for a consumer as well, and nobody looked, because while no room produced it the
two absences were indistinguishable.**

### How big it is

Rat Cellar pays **48 gold** in the shipped build. The same contract in the tuned model pays
about **1.7** at Village — nine gold, scaled by a Village contract multiplier of 1, times a
commission of about 0.19 at a level-1 desk. That is a factor of **twenty-eight**.

So on the build that exists tonight, a Village guild earns **9.5 gold an hour** from its
Tavern and a few hundred an hour from contracts. **Rooms will be single-digit percent of
income where the model says 65%.** The tuned figure was never a fact about this game; it is
a fact about a mechanism this game does not have.

### Why it is not fixed today

It is a code day, not an asset day, and it lands on an awkward seam: `QuestResolution` is
in `IdleGuild.Quests`, which is Core-only and cannot see a `GuildTierDefinition`, so the
tier's contract scale has to arrive through `IGuildStats` or the transaction has to move up
into App. That is a design decision with an architectural cost and it deserves its own day
rather than being bolted onto this one.

`TheContractCommissionHasNoProducerBecauseItStillHasNoConsumer` asserts the absence with
the reason attached, so authoring a commission curve before the mechanism exists is a red
test rather than a Front Desk that visibly earns nothing. **When somebody writes the
consumer, that test is what they delete.**

### What follows from it

- **`guild_model.py` cannot retire today.** The plan said it retires the day the rooms
  land. It does not, because the rooms are only half the divergence: `tycoon_model.py`
  still does not describe the shipped game either. Both models are now wrong in different
  places, which is worse than one being wrong, and the fix is the mechanism rather than a
  number.
- **Any pacing measured tonight is measuring the contract economy**, the same way pacing
  measured before Day 16 was measuring the mailbox. The sentence in the Ledger was right
  and had the wrong subject.
- It is **the eighth appearance of the same shape**: a failure whose only symptom is the
  absence of something. `AssetValidation` crying wolf, Day 13's canaries watching no
  training cost, Day 15's `--checks` block looking for a curve no room had, an interface
  never drawn, a tap with no demand to serve, a stipend on no screen, and now a stat with
  no reader. What is different this time is that it was found *before* the asset was
  written rather than after, which is the second time this project has managed that.

---

## 5. What went red, and what the numbers were

### The four save fixtures, and the one the whole thing was set up for

All four checked-in saves name a `training_room`. All four now restore with **exactly one
unknown building** — the fourth, which already pointed at a Quest Board no build ever had,
reports **two**. And nothing else: no adventurer dropped, no employee, no run, no order, no
adventurer sent home, no tier fallen back, and every building level around the gap exactly
as written.

`save_day14_played_in.json` was pinned at zero repairs on Day 14 *specifically* so that
this would arrive as a red test with a number in it rather than as a silence. It did, and
**the number was one.** That is the Day 6 repair path doing exactly what it was built for,
and it is the first time in the project's life that it has run against **real** saves that
genuinely needed repairing — until today only the synthesised third fixture ever exercised
it.

Two tests were renamed, because `TheLastSaveOfTheOneEconomyGameLoadsWithoutRepair` and
`ARealPlaySessionStillLoadsCleanly` had both stopped being true, and **a test whose name is
a lie is worse than no test**. And the assertions moved from `HasRepairs` to field by
field: *something was repaired* is precisely the assertion that would hide a **second**
thing being repaired, which on the day a fixture legitimately goes from zero to one is the
only failure mode left in it.

One thing is worth naming rather than left implicit, because it is a shape and not a bug: a
guild mid-run keeps its gold and its roster and **silently loses whatever it spent on the
Training Room**. Nothing has shipped, so nobody is owed a refund. The day something has
shipped, a content removal is a refund question.

### An invariant that went red for a reason that is by design

`NoBuildingEffectIsDeadAtMaxLevel` requires every effect to improve at its last level. It
was written on Days 8–9 and assumed, without ever saying so, that **every effect is a thing
the player buys more of**. `ServiceDemand` is not: demand belongs to the tier, and a room
whose own level raised its demand would collapse two of §3.1's three levers into one and
delete the rhythm the entire revision is built on — advance a tier, and everything you own
is suddenly insufficient.

So the invariant learned its first honest exception, named by **stat** in one place rather
than special-cased at the call site, the same way `GuildStatScope` names its three. The
exemption is kept narrow by `ADemandCurveIsFlatAndTheOtherTwoAreNot` asserting the curve
really *is* flat, so it cannot become a hole for a curve somebody forgot to fill in.

### `EveryBuildingIsAvailableFromTheStart` was right and had to go

Its own message read: *"a tier-gated building would be a design change, not a data one."*
It was right, the design change happened today, and **the change was still one field on
three assets.** It is replaced by two tests that assert the shape rather than the schedule
— every tier opens at least one new room, and Village opens what its own gate asks for,
because a gate naming a building the tier cannot build is a tier the player can never leave.

### The rest

`Shipped.SetLevels` gained two parameters and every caller had to be **re-read rather than
renamed**, because `inn:` had two meanings in that file: the hotel, and the beds. Named
arguments are what made that safe. `Shipped.SetBeds(world, n)` is new, because beds come
from two places now and a test that wants a roster of a given size can no longer name a
level — which is §2's assert-the-shape rule applied to the setup rather than the assertion.

`RosterRatchetTests` and `PartyFormationTests` cost two identifiers and **not one
assertion**, because they were written against the recruitment gate rather than against a
bed count. Day 13's discipline paying out five days later.

---

## 6. The crown's stipend, re-checked on the day it was told to be

The standing item: *"the crown's stipend is sized for a build where nothing else earns and
must be re-checked the day the rooms land; a **hardship line** is already designed if it
still bites."* §5 of `Day16_Followup_Solvency.md` was explicit that the twelve-and-a-half
minute recovery *"is largely an artefact of the build it was written in"*.

Measured against the shipped assets, for a guild that has built a Tavern:

| | per minute |
|---|---|
| idle room income | **0.16 g** |
| the crown's stipend | **2.00 g** |
| **tapping the queue** | **5.48 g** |

**The thumb out-earns the mailbox roughly threefold**, and recovering the cheapest
adventurer from an empty treasury falls from about twelve and a half minutes to about four.
So the stipend stops being the economy the moment a room exists, exactly as predicted, and
**the hardship line stays designed and unbuilt.** The crown stays unconditional.

Both measurements are kept as canaries. The twelve-and-a-half-minute one is now explicitly
the *worst* case — a guild that has built nothing at all — and it stays because it is the
floor under §01 and because moving it should still be a decision somebody made on purpose.
`AStrandedGuildWithARoomRecoversInMinutesRatherThanInTwelveOfThem` is the case a player
actually meets.

It also puts a number on §7 of `Day16_Staff_And_Revenue.md`'s open question. *"Tapping is
87% of room income across the first thirty modelled minutes"* was a claim about the model.
In the shipped build at Village it is 5.48 against 0.16, which is **97%** — and that is
before contracts, which currently dwarf both. Settle it before §6B's monetisation lands.

---

## 7. The hall plan

Five footprints where three stood, and the constraint checked before drawing rather than
after. Rooms are eight units wide either side of a two-unit corridor, placed **in the order
the guild unlocks them**, bottom to top, so the hall grows away from the street as the
settlement grows around it.

```
                 [ provisioner ]     City
   [   inn    ]  [  barracks   ]     Town
   [          ]
   [  tavern  ]  [ front desk  ]     Village
   ~~~~~~~~~~~~ street ~~~~~~~~~~~
```

The two Village rooms face each other across the entrance — the Tavern where the townsfolk
sit, the Front Desk where the contracts are posted — and the Barracks sits directly above
the desk, which is the door the adventurers living in it walk out of. The Tavern is twelve
units tall against nine: it is the only room whose spend compounds, the one the player
feeds for fifty-seven levels, and the one holding fifty-nine seats at the end where the Inn
holds thirty-four.

Against Day 17's zoom policy — *the screen's short edge shows fourteen world units* — one
room is **57%** of a portrait phone's width, which is the figure Day 17 settled on as
legible. A facing pair plus the corridor is eighteen and never fits at once; that is the
accepted trade rather than an oversight, because the alternative is rooms too small to hold
a legible seat. The floor derives to **20 × 35** units, which on a 1080×1920 screen is 14 ×
24.9 visible — so the hall pans about ten units vertically at Capital and hardly at all at
Village.

Still provisional. §11 lists where the rooms sit as open, and this is a working answer with
a constraint attached.

---

## 8. What Day 19 inherits

1. **The contract mechanism.** §4. Not a balance question — a missing consumer for two
   declared stats, and the reason neither model describes the shipped game. Everything else
   on this list is downstream of it.
2. **§9 steps 3 through 6 of `World_View_Design.md` are unblocked**, in order, and none of
   them needs art: seats, townsfolk, the queue, and re-homing the tap onto a waiting
   customer. The stats they read now have producers and the arithmetic has been checked
   against the shipped assets.
3. **The tab bar can go** once contracts open from the Front Desk and the roster from the
   Barracks. §7 pinned its removal to those two rooms being authored. They are.
4. **The staff ladder, still forbidden.** Nothing changed today: `tycoon_model.purchase()`
   still can only append, so the four prices are still unmeasured rather than tuned. Give
   the model the dismiss action the game has had since Day 16 *first*.
5. **The balance pass owes a re-tune with the shipped opening.** Starting gold stayed at
   150 where the model tuned 60, deliberately — porting room costs without the opening dial
   they were searched against is Day 15's dead-twenty-two-minutes in reverse, but moving it
   to 60 would take an hour of the crown's stipend to twice the opening budget and fail a
   live invariant with no re-tune available to answer it. Either pin `start_gold` at the
   shipped value and re-run the tuner, or move the asset and re-size the stipend. Not both,
   and not neither.
6. **The Training Room asset is still on disk.** Claude does not delete files inside
   `Assets/`; `EverythingUnderDataIsListedInGameContent` is red until it goes.
7. **The Barracks at Town rather than at Village** is one field, and it contradicts §6.2 of
   the charter, which leaned to Village. The tuned model's schedule was taken because it is
   the one the pacing was measured against. Reversible in a line if it plays badly.
8. **`_contractRewardScale` and `ContractCommission` are still unproduced**, and now
   deliberately so, with a test saying why.

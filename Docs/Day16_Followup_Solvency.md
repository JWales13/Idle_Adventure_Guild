# Day 16 follow-up — the crown's stipend, and a rule about dead ends

A playtest of the Day 16 build reached an **unrecoverable state on the third purchase of a
new guild.** This is the fix, the rule it produced, and the two design arguments that had
to be had along the way — one of which reversed a decision partway through.

---

## 1. The dead end, exactly

| | |
|---|---|
| starting gold | 150.00 |
| Tavern L1 + L2 (50.00 + 57.50) | −107.50 |
| Inn L1 | −40.00 |
| **left** | **2.50** |
| cheapest adventurer (Militia Recruit) | 25.00 |

In the shipped build gold comes **only** from completed contracts, a contract needs an
adventurer, and an adventurer costs 25. Income was exactly zero and stayed zero. Not slow
— finished. The only way out was deleting the save.

**It is Day 4–5's opening deadlock returning with teeth.** That one — Housing Capacity's
zero base meaning a guild with no Inn could recruit nobody and therefore never afford one
— was recorded as *"solved in data rather than in code"* by granting starting gold. The
general lesson it should have carried, and did not:

> **A data solution that depends on the player spending it correctly is a hope rather than
> a solution.**

It is also Day 15's arithmetic one step worse. There the dead twenty-two minutes was
`150 − 143.85 = 6.15`, and the player eventually *earned* their way out of it. Here the
same subtraction lands at 2.50 with nothing underneath it.

### And the tap built the day before was inert

`TakingsService` shipped on Day 16, tested, documented — and could not fire. No shipped
building produces `ServiceDemand`, and no tier carries a base service, so total demand is
0, unserved demand is 0, the queue never fills, and `TryCollect` returns false forever.

**Fifth appearance of the shape**, after `AssetValidation` crying wolf, Day 13's canaries
watching no training cost, Day 15's `--checks` block looking for a curve no room has, and
an interface nobody had drawn: *a failure whose only symptom is the absence of something
is not detectable, and will be found by accident or not at all.* The suite could not see
it because every trade test builds its own rooms through `TradeFixture` — which is exactly
what that fixture's own doc comment warns about, written the day before, and which was
still not enough to make anyone look.

---

## 2. The rule

Now in **§01 of the Ledger**, beside Clean Code and the data-driven architecture:

> **No sequence of choices may leave the player unable to make progress.**

A property rather than a balance figure, which is why it sits in Principles. There is
always an action available that improves the player's position: early that is the crown's
stipend, and late it is letting staff go, which is free and is half of why dismiss exists.

It is **not** a promise that mistakes are free — recovery is deliberately slow, see §5.
And it governs decisions not yet made: whether a room can be sold, whether wages can
bankrupt you, whether prestige can strand a run.

Asserted in `SolvencyTests` against the **shipped** catalogue, because a fixture would have
been built from the same assumptions that produced the dead end.

---

## 3. What was built

A **stipend from the crown**, collected from a mailbox on a cooldown. The player's idea,
and better than the conditional relief this started as: a mailbox with a badge on it is
*legible*, where "the guild's net income is zero" is a predicate the player cannot see.

| | |
|---|---|
| `GuildTierDefinition._stipendGold` | the amount, authored per tier |
| `GameContent._stipendCooldownSeconds` | 30 |
| `GameContent._stipendMaximumCharges` | 3 |
| `StipendService` (App) | the mailbox; accrues on the clock, collects by hand |
| `StipendCollected` (Core) | announced, not accrued silently — the player pressed something |
| `SavedStipend` | added field only; `SaveSchema.CurrentVersion` still has never moved |
| debug console | drawn directly under the treasury, which is where you look when the treasury is the problem |

Three rules keep it from becoming a fifth room:

- **Nothing the player buys improves it.** It is not a lever, it never enters a payback
  ranking, and it cannot compete with the four rooms for their gold. If it ever appears in
  a purchase decision, something has gone wrong.
- **It is not room income.** Takings are deliberately counted *inside* the gross so the
  thumb cannot move the 70/30 split the revision is tuned against. The stipend is not room
  trade and gets its own lifetime counter, or it moves that ratio quietly instead.
- **Deliveries cap at three.** Eight hours away banks three, not nine hundred and sixty —
  the same rule and reason as the takings queue. Offline earnings are `OfflineProgress`'s
  job and this must not double-dip.

It also separates the free floor from the paid layer *fictionally*: §6B makes the
**Patron** the premium surface (Boons, familiars, cosmetics), so making the safety net the
**crown** means the thing that can never let you fail and the thing you can spend money on
have different sources and cannot be confused. That matters on a submission where "nothing
bought with money makes a number go up" is the whole monetisation defence. And it gives
familiars a third automation target that grants no power: a familiar fetches your post.

---

## 4. The sizing argument, which reversed a decision

The first sizing aimed at the stated target — recover a 25-gold recruit in about a minute,
so 15 gold every 30 seconds. Then it was checked against what the rooms actually earn in
the tuned model at the moment each tier opens:

| tier | opens | room net/hr | stipend/hr if always collected | ratio |
|---|---|---|---|---|
| Village | 0h00m | 9.5 | 1,800 | **189×** |
| Town | 0h23m | 93.6 | 5,400 | **58×** |
| City | 2h24m | 2,053.8 | 16,200 | **8×** |
| Capital | 5h31m | 3,680,451.5 | 48,600 | 1% |

For the first two and a half hours the mailbox would not have been a floor. It would have
been the entire economy — and against tap-inclusive early income (about 418 g/hr) it is
still roughly 4×. That is the metronome failure Day 15 caught in its own loss function,
arriving as arithmetic: *a metronome looks identical to a game if you only count noises.*

**The tension is structural, not a tuning miss.** While the mailbox refills continuously,
**recovery speed *is* a sustained rate.** 25 gold is two and a half hours of Village room
income, so anything that rescues you quickly dwarfs the tier it rescues you in. You cannot
buy both out of one flat delivery.

Two ways out were put up. A **hardship line** — accrual stops above a per-tier threshold,
so the crown can never hold you above `line + one delivery`, funding the escape and
nothing else — which keeps the one-minute recovery. Or **shrink it and pay in time**,
keeping the mailbox unconditional and simple.

**The second was chosen**, explicitly reversing the earlier "about a minute" answer once
the numbers were on the table. Recorded that way rather than quietly re-tuned, because the
first answer was given without this table in front of it.

### Where it landed

There is no ladder that stays proportional, and that is worth knowing rather than
rediscovering: the guild's earn rate at tier openings runs **418 → 730 → 4,208 →
15,859,700 g/hr**, a ×3,768 jump into Capital. A stipend that stayed proportional there
would be a 26,000-gold delivery, which is not a safety net. **The stipend is necessarily
meaningful early and irrelevant late** — the same self-obsoleting shape the takings tap
has, reached by a different route: takings decay because staff cover the room, the stipend
decays because the economy outgrows it. Two manual actions, both of which stop mattering.

A ×2 ladder from 1 gold holds that shape best:

| tier | delivery | per hour | share of what the guild earns there |
|---|---|---|---|
| Village | 1 | 120 | 29% |
| Town | 2 | 240 | 33% |
| City | 4 | 480 | 11% |
| Capital | 8 | 960 | 0.01% |

×2 is also comfortably under the market's ×6.09 per tier, so the containment invariant
passes with room to spare.

---

## 5. What it costs, pinned rather than asserted

**Recovering the cheapest adventurer from an empty treasury takes about twelve and a half
minutes** of collecting every delivery the moment it lands. That is long, and it was the
known price of keeping the mailbox unconditional.

`RecoveringFromNothingIsSlowAndThatIsADecision` is a **`BalanceCanary`** that measures it
and pins it to a 10–15 minute band, so the cost is recorded and any change to it has to be
deliberate. A trade-off nobody can see is a trade-off nobody will revisit.

Worth knowing when reading this later: **the cost is largely an artefact of the build it
was written in.** Nothing earns gold today, so the mailbox is the only income there is.
Once the five rooms are authored, a stranded player also has room income and a working
takings tap, and twelve minutes stops describing anything a player will meet. If it still
does on the room day, the hardship line is the thing to reach for.

---

## 6. The tests

Eleven, in `SolvencyTests`, against the shipped catalogue.

| test | what it holds |
|---|---|
| `AGuildThatHasSpentEveryCoinCanAlwaysEarnAnother` | the Principle itself, in one assertion |
| `TheExactPlaytestPathIsNoLongerADeadEnd` | Tavern, Tavern, Inn through the real service at shipped prices, then recovery |
| `RecoveringFromNothingIsSlowAndThatIsADecision` | **canary** — what the mistake costs |
| `AnHourOfTheCrownsStipendIsWorthLessThanTheOpeningItBacksUp` | the guard that would have caught the first sizing, by 12× |
| `TheStipendNeverGrowsFasterThanTheMarketItBacks` | **ignored** until market size is authored with the rooms |
| `TheStipendLadderReadsAsWritten` | **canary** — the four amounts, the cooldown, the cap |
| `DeliveriesStopAtTheCapSoAnAbsenceCannotBankAnEvening` | the offline bound |
| `TheStipendIsNotCountedAsRoomIncome` | the 70/30 split stays the rooms' to move |
| `TheMailboxSurvivesASaveRoundTripAndAnOldSaveArrivesEmpty` | including the null case every checked-in fixture is |
| `StartingOverEmptiesTheMailboxAndNotJustTheFile` | the Day 6 lesson, third application |
| `EveryTierPaysAStipendAtAll` | an unauthored tier would fail silently — the mailbox simply never lights up |

Note what `AnHourOfTheCrownsStipendIsWorthLessThanTheOpeningItBacksUp` is doing, because
the first version of it was useless: it compared the stipend against the cheapest room's
build cost, which does not scale with tier, and **it would have passed the 15-gold version
it was written to catch.** Rewritten against starting gold — the authored answer to "what
does it take to get this guild going" — it fails that version by a factor of twelve. A
guard that would not have caught the bug that prompted it is not a guard.

---

## 7. And then it shipped invisible

The stipend went in working, saved, tested and documented — **and only into the debug
console.** `grep -rln "Stipend" Assets/_Project/UI/` returned nothing. The next playtest
asked "is the mailbox hidden?", and the answer was that it did not exist on any screen a
player looks at.

That is **twice in two days, and the same failure both times**: a mechanic verified from
the inside and never from the player's side. The tap shipped inert because no room feeds
it; the stipend shipped invisible because no view renders it. Set beside `AssetValidation`
crying wolf, Day 13's canaries watching no training cost, Day 15's `--checks` block looking
for a curve no room has, and fifteen days of an interface drawn into the void, this is the
sixth and seventh appearances of the same shape — and the standing note that **nobody has
judged this interface**, deferred twice now, is what would have caught both.

The stated rule is worth writing down in the form the playtest produced it:

> **A guarantee the player cannot reach is not a guarantee.**

Fixed by putting the mailbox in `TreasuryBar` — the permanent chrome, beside Gold and Rep,
which is where somebody looks when the treasury is the problem. It is **always present and
never hidden**: disabled with a countdown while empty, gold-bordered and enabled with a
count when there is post. Collapsing it while there is nothing to collect is tidier and is
the wrong trade, because an action that is absent and an action that is unavailable look
identical on screen — the Day 15 icon rule, applied to a button, having just been
re-learned the hard way. `GuildContext` gains `Stipend` so the view reaches the service
through the boundary rather than around it.

And there is now a test for the half that failed.
`TheTreasuryBarPutsTheStipendWhereThePlayerCanSeeIt` builds the bar headless — a UI Toolkit
element constructs its children with no panel attached — and asserts the control exists,
starts disabled on a fresh cooldown, and lights up once a delivery lands. It cannot prove
the thing is *legible*; that is still the hand-check. It proves it is *there*.

---

## 8. What this does not fix

- **The tap is still inert** until a room produces `ServiceDemand`. The stipend makes the
  build playable; it does not make the revenue engine live.
- **Pacing measured before the rooms land is measuring the mailbox**, not the game.
- **A Capital guild over-hired to net zero with an empty treasury** is not rescued by an
  8-gold delivery. It is rescued by `TryLetGo`, which is free. The Principle holds at every
  stage, by different means at different stages.
- **And the late-stage escape is currently unreachable**, which is §7's failure with the
  paint still wet: `StaffService` is on no screen and not on `GuildContext`. Until the
  staff panel lands on Day 23, §01's rule is only true in its early form. Named here
  rather than closed with unused plumbing.

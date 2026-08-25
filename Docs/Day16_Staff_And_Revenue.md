# Day 16 — the revenue engine, and a staff ladder nobody had ever climbed

The first day of the revision that changes the game rather than describing it. A sixth
feature assembly, `IdleGuild.Staff`, plus the trade layer that turns four rooms into gold
per hour, the wage floor, the tap, and the save fields for all of it.

**No `.asset` file changed.** Not one. The engine exists and the five rooms do not, which
is §8's order — assembly and engine first, arrivals second, rooms third — and which is
also, it turns out, the only defensible way round given §6 below.

---

## 1. What was built

| | |
|---|---|
| **`IdleGuild.Staff`** | Sixth feature assembly, references Core and nothing else. `StaffDefinition`, `StaffMember`, `StaffRoster`. |
| **`TradeService`** (App) | The revenue engine: per-room want, priority allocation, throttle, wages, net floored at zero. |
| **`StaffService`** (App) | Hire **and** let go, both on the first day. |
| **`TakingsService`** (App) | The tap, and the queue that makes it a game mechanic rather than a rate. |
| **`GuildStat` + 5** | `ServiceSeats`, `CustomerSpend`, `ServiceDemand`, `StaffSlots`, `ContractCommission`. Appended, never renumbered. |
| **`GuildStatScope`** (Core) | Which stats mean something summed across the guild, and which do not. |
| **`GuildTierDefinition` + 4** | Market size, contract reward scale, base service per hour, base housing capacity. |
| **`PlayerEconomy.Accrue`** | The silent income path `CurrencyChanged`'s own remark has been asking for since Day 2. |
| **Save** | Payroll and accrual state, as **added fields only**. `SaveSchema.CurrentVersion` does not move. |
| **Debug console** | Trade and staff sections, plus the tap button. Still the only playable surface. |

The architectural bet was re-checked and holds. Nothing was added to `BuildingDefinition`
to make revenue work: a room's seats, spend and demand are `BuildingEffect` entries like
every other effect, and adding a sixth department stays one new `.asset` and zero code.
`GuildState.Aggregate` gained no branch — it gained an early *return*, which is the
opposite thing. The five feature assemblies are still Core-only.

---

## 2. Where each of the three levers lives, and why the engine is in App

§3.1's rule is three levers from three separate sources with no overlap. In code:

```
demand    = the room's own ServiceDemand  x  the tier's MarketSize
capacity  = the room's own ServiceSeats   x  the catalogue's CustomerTurnsPerHour
throughput= the tier's BaseServicePerHour  +  everyone on the payroll
```

Demand is deliberately **flat across a room's levels**. A room whose own level raised its
demand would collapse two of the three levers into one, and the rhythm §3.1 exists to
create — advance a tier, and everything you own is suddenly insufficient — depends on the
settlement being the only thing that grows the crowd.

**The engine is in App and could not have been anywhere else.** Capacity is staff and
demand is buildings; `GuildState.Aggregate` reads buildings only. Teaching it about the
payroll would mean Guild referencing Staff, which is the cross-feature reference fifteen
days of discipline have kept out. Combining an `IGuildStats` with a roster is what App has
existed for since Day 4–5, and it is the same shape as dispatching a quest.

`TradeService` holds **no state**. Every number is derived from the guild as it stands, so
an upgrade is felt on the next call and there is nothing to keep in step with a save. The
lifetime totals sit on `SimulationClock`, which is this project's ledger of what has
happened.

### Priority allocation, which is finding #10 made structural

Staff serve the most valuable custom first. Sharing them proportionally is what produced
the worst deadlock the model ever found — opening the Provisioner diluted the staff
already serving the Tavern and Inn, about 137,000 an hour of damage to gain 4,000, so its
payback was negative and a player sitting on 276 million gold never bought a 9,000-gold
room. That is a design failure and not merely a modelling one; a player would have
experienced it as *the game got worse when I built something*.

Pinned by `OpeningARoomNeverMakesTheGuildPoorer`.

---

## 3. The stat that reads as a plausible wrong number

Seats, spend and demand belong to a **room**. Summing them across five rooms is
arithmetically fine and means nothing at all — and that is a worse failure mode than the
one this project keeps meeting. Four times now the shape has been *a failure whose only
symptom is the absence of something*: `AssetValidation` crying wolf, Day 13's canaries
that watched no training cost, Day 15's `--checks` block looking for a curve no room has,
and an interface that had never been drawn. Sixty-eight seats is not an absence. It reads
exactly like a real figure.

So `GuildStatScope` names the three per-room stats in one place, `GuildState.Aggregate`
refuses to produce them, `IGuildStats.Get` hands back their neutral zero, and
`GuildState.EffectFor(building, stat)` is the only sanctioned read. A room that quietly
reads zero seats earns zero gold, which anybody notices in ten seconds. A room that
quietly reads five rooms' seats is what ships.

`EveryStatIsEitherPerRoomOrGuildWideAndTheScopeSaysWhich` runs over the enum itself, so
appending a stat without deciding which kind it is fails in the suite rather than in a
room panel.

---

## 4. §4 of the charter names three stats that do not exist — CORRECTION

`Vision_Revision.md` §4 says:

> ```
> Revenue        (8)   gold per hour at full service
> ServiceDemand  (9)   service needed to run at full
> StaffSlots    (10)   how many staff may be employed
> ```

**There is no `revenue` curve on any room and there never was.** §4 was written on Day 14,
before the revision split demand from capacity, and revenue became `seats × spend` — which
is precisely the staleness Day 15 found in the model's own `--checks` block, one layer up
and in the charter rather than in a tool. `ServiceDemand` also changed meaning: it is not
"service needed to run at full", it is customers per hour who want in, and the service
needed follows from it.

Corrected in §4 of the charter rather than quietly amended. The shipped enum is:

```
ServiceSeats       (8)   PER-ROOM. seats at this level
CustomerSpend      (9)   PER-ROOM. gold one served customer leaves
ServiceDemand     (10)   PER-ROOM. customers/hr who want in, before market size
StaffSlots        (11)   guild-wide
ContractCommission(12)   guild-wide; the Front Desk's raw cut, saturated by the trade layer
```

`ContractCommission` is declared with no producer, exactly as `QuestSlots`,
`MaxQuestTier` and `FailureRateReduction` have been since Day 2 — the Front Desk is
authored later, and appending to a save-persisted enum twice when once will do is a cost
with no benefit.

---

## 5. §3.1 names a wage rule the tuned model does not use — CORRECTION

§3.1 says `wages/hr = Σ each staff member's wage`. `tycoon_model.wagesPerHour()` does not
do that. It computes `staffCapacity × averageSpendPerCustomer × WAGE_SHARE` and never
reads the per-employee `wage` field at all — which is therefore **dead data in the model**,
carried in `content()`, scaled by nothing, read by nothing.

The model is what produced every tuned number in this project, so the model wins and the
charter is corrected. The reasoning is also better than the charter's: a flat wage against
geometric room revenue is decoration, and the model measured it twice — 3,973 an hour
against 15,118,239 of gross, three hundredths of one percent. Staff in a grand hall are
simply paid more.

Two consequences worth stating:

- **`StaffDefinition` carries no wage field.** Adding one would create a second source of
  truth for a number the trade layer derives, and this project has watched a ratio
  authored in one place and paid for in another go unchecked for four days.
- **Wages are charged against capacity, not against customers served.** That is what keeps
  the mistake this mechanic exists to make possible: hire past what the crowd needs and
  you pay for idle hands. Charged against served customers, over-hiring would be free and
  the whole second economy would be a slider that only goes up. Pinned by
  `WagesAreChargedAgainstCapacityRatherThanAgainstCustomersServed`.

---

## 6. The finding: three quarters of the staff ladder is dead content, and the reason is the action this day was built to add

Before authoring four `StaffDefinition` assets from `tuned_params.json`, I divided one
authored number by the other — the check Day 13 established and which nobody had ever
performed on this ladder.

| kind | hire | service/hr | **gold per point of service** |
|---|---|---|---|
| Potboy | 21.88 | 46.30 | **0.47** |
| Server | 680.68 | 330.70 | **2.06** |
| Barkeep | 22,365.20 | 2,590.95 | **8.63** |
| Steward | 729,300.00 | 22,312.50 | **32.69** |

Each rung is worse value than the one below it, by a factor of about four, all the way up.
That is finding #4's exact shape — *"higher tiers cost more per unit delivered, so the
model hired 96 Potboys and never upgraded once"* — and it is Day 13's rarity ladder in a
third costume.

What the tuned configuration actually does, at every integration step:

```
step 10   staff 105/125   {'Potboy': 105}
step 30   staff 105/125   {'Potboy': 105}
step 60   staff 105/125   {'Potboy': 105}
```

**A hundred and five Potboys, and not one employee from the three tiers above, ever.**

### It is not the price

Flattening the ladder so every tier costs exactly what it delivers — Day 13's fix,
applied — barely moves it, and moves no pacing figure at all:

| | Town | Capital | maxed | rooms | payroll |
|---|---|---|---|---|---|
| as tuned | 0h23m | 5h23m | 6h49m | 65% | 105 Potboys |
| flat gold-per-service | 0h23m | 5h23m | 6h49m | 65% | 98 Potboys, 1 Server |

### It is not slot scarcity either

Starving the Tavern's staff-slot curve does not make the player climb:

| slots | payroll | Capital |
|---|---|---|
| ×1.0 | 105 / 125 — all Potboys | 5h23m |
| ×0.25 | 31 / 31 — all Potboys | 6h27m |
| ×0.10 | 12 / 12 — all Potboys | 10h10m |
| ×0.04 | 4 / 4 — all Potboys | 14h12m |

### It is the ratchet, and the ratchet is finding #3

`tycoon_model.purchase()` can only ever `w.staff.append(...)`. **The model has no way to
let anybody go.** So slots, once filled with the cheapest help, are filled forever, and
the ladder is unreachable *at any price and at any slot count*. Which means nothing has
ever measured what the upper tiers are worth — and a rung whose value is never measured is
free to be priced arbitrarily, and was.

The causal chain, written out because it is the useful part:

1. The model cannot dismiss staff, so the ladder is unreachable.
2. An unreachable rung cannot cost the loss function anything.
3. So the tuner priced the upper tiers freely, and priced them badly.
4. Day 15 then raised `slots_base` and `slots_lin` hard — correctly, to give the opening
   more cheap purchases and pull the worst silence down from six minutes — which removed
   even the slot pressure that might have forced a climb.
5. Nothing noticed, because there has never been a canary on a staff cost.

Step 4 is worth its own sentence: **a change that was right for the thing it was aimed at
silently deleted a subsystem somewhere else.** That is not a tuning error; it is what a
loss function that does not score a subsystem will always eventually do to it.

And there is a smaller finding inside the larger one. The model's own comment above the
staff table reads *"the ladder has to improve per gold as it climbs — an earlier run hired
ninety-six Potboys and never upgraded once… Same defect Day 13 found in the rarity ladder,
in different clothes."* The hand-authored numbers underneath it are 5.0, 14.7, 41.8, 107.1
gold per point of service — **ascending, then as now**. The fix was written as prose and
never as arithmetic, and the prose has been read several times since. Day 13's lesson has
a sharper edge than it looked: a ratio authored in one place and paid for in another will
not be checked by anybody looking at either — *including when one of the places is a
comment claiming it already was.*

### So no staff assets were authored today

Three options, and none of the other two survives contact:

- **Author the tuned ladder.** Puts three provably dead assets in the repo on the day the
  subsystem is built, with no test able to object.
- **Author a corrected ladder.** Changes the economy and requires a tuner re-run, which is
  a balance pass and not a build day's work. It would also be tuning against a model that
  still cannot climb the thing being priced.
- **Author nothing, and hand the balance pass the experiment.** Taken.

`AHigherStaffTierNeverCostsMoreGoldPerPointOfService` ships today and **calls
`Assert.Ignore`** with a pointer to this section. Ignored rather than absent, and ignored
rather than vacuously green, because a canary set that does not watch a value is quieter
than no canary set — its silence reads as a pass. This one says out loud that it is not
watching anything yet.

### What the balance pass has to do first, before touching a number

Give `tycoon_model` the action the build now has. Concretely: a rule in `purchase()` that
will replace the least capable employee when the replacement is better *the day it
arrives* — which is exactly the rule Day 13 landed on for adventurers after the naive
version (rank by fully-trained potential) churned the whole roster and added eight hours.
Only then are the four staff prices worth searching for, and only then does
`AHigherStaffTierNeverCostsMoreGoldPerPointOfService` have anything to guard.

Until that happens, treat every staff figure in `tuned_params.json` as unmeasured rather
than as tuned. The room curves are unaffected — the payroll's *total* service is what the
rooms see, and 105 Potboys deliver it.

---

## 7. The tap, and the queue the model does not have

The model treats tapping as a rate, which is right for a simulation and useless in a game:
unserved demand is customers **per hour**, and a player with a fast thumb would draw an
hour of custom out of it in three seconds.

So the rate fills a **queue** — people waiting at the bar, which is what unserved demand
physically is — and a tap serves one of them. The queue is capped
(`GameContent.MaxWaitingCustomers`, 40 by default, one modelled minute of thumb), so eight
hours away cannot bank eight hours of taps. Coming back to a wall of free gold would make
the tap a reason to close the game, which is the exact inversion of what it is for.

Everything else follows the model's placement, which was already the right one:

- capped by unserved demand, so it can never invent custom that is not there
- worth exactly nothing once staff cover the room, so it decays on its own — no late-game
  balance problem to tune away, and a familiar bought late is a familiar wasted
- touches neither demand nor capacity, so §3.1's three levers survive
- counted inside room income, so the thumb cannot quietly move the 70/30 split

Takings are granted through `Grant` and announce themselves, unlike idle room income which
goes through the new silent `Accrue`. A tap is something the player did and wants to see
land; four rooms ticking over is not.

**Still unsettled, and it is Day 28's:** tapping is 87% of room income across the first
thirty modelled minutes. High enough to be a decision rather than a side effect, and it
makes the takings familiar very valuable very early. Nothing today changed that number.

---

## 8. The save, and a trap that would have taken every fixture red

Two added fields — `SavedStaff[] Staff` and `SavedTrade Trade` — and
`SaveSchema.CurrentVersion` **does not move**, exactly as §4 of the charter predicted. The
compatibility rule is doing its job: fields are only ever added, and a pre-revision save
arrives with neither.

The trap: **JsonUtility leaves an absent array `null`, not empty.** All four checked-in
fixtures predate the revision and carry no `Staff` key, so an unguarded
`foreach (SavedStaff s in data.Staff)` would have thrown on every one of them. `RestoreStaff`
returns zero drops for a null array, and — the part that matters more — **a null payroll is
not counted as a repair**. A guild that genuinely had no staff must not be reported as
damaged, or `save_real_session.json` and `save_day14_played_in.json` both go red for having
been written honestly.

Pinned by `ASaveFromBeforeTheRevisionRestoresAsAGuildWithNoStaffAndNoRepairs`.

`SaveRestore.Reset` also empties the payroll, the lifetime totals and the queue — the Day 6
lesson applied to the two things added today: *a destructive action that does not also
invalidate the live state it describes will be undone by whatever writes that state next.*

**`save_day14_played_in.json` is still green today**, because no `.asset` changed. It goes
red on the day the Training Room is deleted, and that day should read its number before
updating it.

---

## 9. What the tests cover, and what they conspicuously do not

Two new files, and one honest exception to the suite's own rule.

§1 of `Tests.md` says tests load the real `.asset` files, because every content failure
this project has had was a wrong value in a shipped asset and a hand-built fixture would
have been written from the same misreading. **That argument is about asserting content.**
`TradeFixture` builds rooms in memory through `SerializedObject`, and nothing built on it
asserts a content value: it checks that allocation serves the good table first, that the
floor holds, that a tap cannot invent a customer. Mechanism is logic, and logic may supply
its own inputs.

The corollary matters more than the exception, and is written into the fixture's own
doc comment so it cannot be lost:

> **No room in the shipping catalogue produces a single one of these stats yet.** The
> value-asserting half of this subsystem — the seats curves, the spend curves, whether a
> Provisioner is worth nine thousand gold — has no coverage at all today, and it will not
> get any by accident.

Two tests are deliberately not-green-not-red:

| test | state | why |
|---|---|---|
| `EveryTierCarriesABaseServiceOnceAnyRoomAsksForCustom` | **Ignored** | Vacuous until a room produces `ServiceDemand`. Becomes live the day the rooms land. |
| `AHigherStaffTierNeverCostsMoreGoldPerPointOfService` | **Ignored** | §6. Nothing to guard until the ladder is authored against a model that can climb it. |

Both say so out loud rather than passing quietly, which is the whole point.

### And one went red, correctly

`PresentationTests.EveryGuildStatHasAPlayerFacingName` failed on the first run:

```
ServiceSeats falls through to its enum name, which is how the code talks about it
rather than how a player would.
  Expected: not equal to "ServiceSeats"
  But was:  "ServiceSeats"
```

An **invariant** moving is a warning rather than expected work, and this one earned its
keep: appending to `GuildStat` carries a display obligation that nothing else in the
project would have raised, and `Format.StatName`'s `_ => stat.ToString()` fallback means
the failure mode was a room panel reading *"ServiceSeats"* at the player — visible only to
somebody looking at the screen, which on this project is not a safe assumption.

It is the exact counterpoint to Day 13, where no canary moved and *that* was the finding.
Here an invariant moved and was right to. Names now read from the player's side of the
counter rather than the engine's — Seats, Spend per head, Custom, Staff slots, Commission
— because nobody upgrades a room to raise its "service demand"; they upgrade it because it
has run out of seats. `ContractCommission` deliberately falls through to `Bonus` in
`StatValue` rather than printing as a percentage: the stat is the desk's *raw* cut, which
the trade layer saturates, so a percentage here would be a lie the player could check.

---

## 10. What Day 17 inherits

1. **The owed interface hand-check, still owed.** Twenty-five minutes: Days 10–11's colour
   half of step 6, Day 12's four, and whether the 96px room icon reads beside a 28px title.
   It was deferred from today by decision, and it was supposed to happen *before* twenty-three
   more assets are sized to match a judgement nobody has made. It is now the first thing.
2. **Arrivals**, per §8's order — recruitment from shop to crowd.
3. **The five rooms as assets**, which is when the Training Room dies and the Day 14
   fixture goes red with a number in it.
4. **The staff ladder**, but only after the model can let somebody go. §6.
5. **The Barracks has to look like it makes money.** Untouched today and still owed: it
   earns nothing directly, so a payback-ranked player never buys it, and the number that
   would fix it — what the Barracks is worth through the Front Desk's commission — needs
   the Front Desk authored first. `ContractCommission` is declared and waiting.
6. **The tier panel must show what the gate is still missing.** Also untouched.
   `TierAdvancementService.Preview` returns `RequirementsNotMet` and names nothing, which
   is not enough for the screen §6C's finding #7 asks for. That service owes a shortfall
   description before Day 23 builds the panel — and it belongs in the service, because
   views hold no rules.

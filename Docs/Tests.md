# The test suite

An EditMode assembly at `Assets/_Project/Tests/Editor/`. Run it from
**Window → General → Test Runner → EditMode → Run All**. All green, in well under a
second. **One test is deliberately Ignored rather than green** — see §7. It was three
until Day 18 authored the five rooms, which is what two of them were waiting for.

There is no `dotnet` or `unity` on the Cowork shell, so tests are written there and run
here. That loop is the same one compiling already uses.

---

## 1. Why it exists, and what it replaced

Days 10–11 finished with a nine-step manual verification pass. Seven of those steps were
mechanical — load a save and read four numbers, advance a tier and check a list, compute
a duration and compare it to a table — and they were going to be re-run on Day 13, Day 14,
Day 21 and Day 23. The suite replaces the mechanical part and leaves the human part alone.

Two things argued for it more than coverage did:

- **The one real bug this project has found** — the debug console's delete undoing itself
  within thirty seconds, because it removed the file and left the world running — is
  exactly a round-trip assertion. It is now `StartingOverEmptiesTheGuildAndNotJustTheFile`.
- **Every content failure so far was a wrong value in a shipped asset**, not wrong logic.
  Day 4–5's worst was the Inn being handed its own *cost* curve as its bed curve, so a
  level-1 Inn granted fifty beds. That is one assertion long, and a fixture built in code
  would have been written from the same misreading that produced the asset.

Which is why **the tests load the real `.asset` files** through `AssetDatabase` rather
than constructing content. `Shipped.cs` is the seam that does it.

---

## 2. The one rule that keeps them useful

**Assert the shape, not the number.**

No dead levels, gates that only tighten, a rarity ladder that doubles, an opening that is
solvent, content that is reachable. Day 13 and Day 21 will move every figure in the game
and none of those should flicker. A failure means a curve stopped doing its job.

The exceptions are tagged:

```
[Category("BalanceCanary")]
```

Filter on that in the Test Runner and you have the list of tests that assert *values* —
the opening quest's `52s / 6% / 48 g`, the Inn's 2 / 12 / 16 beds, the ×2.00 rarity ladder,
the training ladder's `26.8 / 53.6 / 107.2 / 214.4 / 428.8`, Dragon's Roost at `720s / 40%`
against a starter party. **These are expected to be updated by a balance pass.** Updating
one is part of that work; updating an invariant is a warning that something else is wrong.

**Day 13 moved no canary, and that was the finding.** The first balance pass changed five
numbers — the per-band training bases — and not one canary so much as flickered, because
there had never been a canary on a training cost. The ladder tripled per band while power
doubled, so a Legendary bed cost 81x a Common bed for 16x the power, and Days 8–9, 10–11
and 12 each concluded "higher rarities feel pointless" while looking somewhere else for
the reason. **A canary set that watches the wrong values is quieter than no canary set,
because its silence reads as a pass.** Two tests closed it:
`AHigherRarityBandNeverCostsMoreGoldPerPointOfPower` as an invariant, and
`TheTrainingLadderReadsAsWritten` as the canary that should have existed since Days 10–11.

---

## 3. What each fixture covers

| File | Replaces | Covers |
|---|---|---|
| `AssetInvariantTests` | pass step 9 | the whole `--checks` block, plus every asset under `Data/` being listed in `GameContent` |
| `QuestResolutionTests` | steps 3, 8 (part) | the opening figures, both speed clamps, the failure curve, quest tiers getting harder |
| `TierUnlockTests` | step 7 | the exact quest list at each tier, slots seeded from the tier, post-MVP stats still unproduced |
| `RecruitmentGateTests` | step 5 | which of the three gates binds, the rarity ladder at Tavern 9/17/25/32, the Inn's bed counts |
| `PresentationTests` | step 6 (part) | every rarity has its own class, every refusal has a sentence, every stat has a player-facing name |
| `SaveRoundTripTests` | step 2 (part) | capture → JSON → probe → restore, reset semantics, garbage refused, delete removing every copy |
| `SaveFixtureTests` | step 2 (rest) | real save files this build did not write |
| `AssetInvariantTests` (Day 13) | — | a band never costs more gold per point of power than the band below; the training ladder as five figures |
| `RosterRatchetTests` | Day 12 | a full guild can always make room; both refusals, in the right order; a refused dismissal does not half-happen; no refund |
| `PartyFormationTests` | Day 12 | the run in flight survives a re-form untouched; the new party goes out next; exact party size; the refuse → re-form → release → retire route end to end |
| `TradeEngineTests` (Day 16) | — | the three levers and which of them binds; staff serving the most valuable custom first; opening a room never making the guild poorer; wages charged against capacity; the net floored at zero; an hour away paying exactly an hour watched; a per-room stat reading zero through the guild-wide seam; the tap's cap and its queue |
| `AssetInvariantTests` (Day 18) | — | the three levers all-or-none; a demand curve flat and the other two not; the market growing and the Village its unit; the Tavern's seats as the world view was designed against; the commission still having neither producer nor consumer |
| `TierUnlockTests` (Day 18) | — | every tier opens at least one new room; Village opens what its own gate asks for; every tier sleeps somebody before anything is built |
| `StaffRatchetTests` (Day 16) | — | a full payroll can always make room; the least capable is the one who goes; no refund; the refusals in the order the player can clear them; the payroll and the till through a save round trip; a pre-revision save restoring as no staff and **no repairs** |

Day 13 added two tests to `AssetInvariantTests` and changed nothing else in the suite.

Day 12's seventeen tests introduced no new `BalanceCanary`. Not one of them names a bed
count, a recruit cost or a rarity threshold — the roster tests hire against the *gate*
(`while Preview(...) == Recruited`) rather than against a number, precisely so that a
Day 13 Inn of fourteen or eighteen beds leaves them just as true.

### Still manual, and why

- **Step 4 — Town in about ten minutes.** A pacing judgement. A test can assert a band;
  only a person can tell whether the first ten minutes feel alive.
- **Step 6 — the colours.** `Format.RarityClass` returning `rarity--epic` is testable.
  USS resolving that class to purple is not.
- **Step 8 — is it *appropriately* hard.** The clamps are pinned. Whether Dragon's Roost
  reads as a fair fight at a guild that earned its way to Capital needs a played-in save,
  and belongs to Day 14.
- **Day 12's four**, all the same species as step 6. `button--destructive` resolving to
  the negative colour and a disabled Retire still looking disabled; a sixteen-row party
  picker fitting the phone; the selected row being unambiguous at a glance; and whether
  the retire confirmation reads as informative rather than as a scolding. Listed with
  their reasoning in §8 of `Docs/Day12_Roster_And_Parties.md`.

Call it twenty-five minutes of hand-checking rather than forty, and most of it wants
Day 14's played-in save anyway.

---

## 7. What is Ignored on purpose, and why that is not the same as absent

Day 16 built the revenue engine and authored no assets for it, which left three checks
with nothing yet to check. Each called `Assert.Ignore` with a reason rather than passing.
**Day 18 authored the five rooms and two of the three went live**, which is the whole
argument for the pattern: an ignored test with a stated condition is a test that turns
itself on, where a vacuously green one would have gone on being green and told nobody.

| test | was | now |
|---|---|---|
| `EveryTierCarriesABaseServiceOnceAnyRoomAsksForCustom` | Ignored: no shipped room produced `ServiceDemand`, so the cold-start guard was vacuous | **Live and green.** Every tier carries 5.6062 customers an hour of guildmaster |
| `TheStipendNeverGrowsFasterThanTheMarketItBacks` | Ignored: no tier carried a market size, so it compared a growing stipend against a flat market | **Live and green.** The stipend doubles per tier against a market growing ×6.09 |
| `AHigherStaffTierNeverCostsMoreGoldPerPointOfService` | Ignored: no staff assets, and §6 of `Day16_Staff_And_Revenue.md` says why they were deliberately not written | **Still Ignored.** The model can only ever *append* staff, so the ladder has never been climbed by anything and its four prices are unmeasured rather than tuned |

**A note on this document's own arithmetic, since that is the recurring fault here.** The
third row was added by the Day 16 follow-up and never reached this table, so this file
said *two* while the Ledger said *three* and the runner said three. Nobody could have
checked it without running the suite, which is the same shape as the 115/117 count and as
the staff ladder's own comment. The number is now one and this table lists all of them.

This is §2's rule pointed at itself. **A canary set that does not watch a value is quieter
than no canary set, because its silence reads as a pass** — and a test that would pass
vacuously is the same failure in a smaller costume. Ignored shows up in the runner as
neither green nor red, with the reason attached, which is the honest report: *this exists,
it is not doing anything yet, and here is the document that says when it will.*

### The coverage Day 16 owed, and Day 18 paid

`TradeFixture`'s doc comment carried the debt: *"no room produces seats, spend or demand
yet, so the seats curves, the spend curves and whether a Provisioner is worth nine
thousand gold have no coverage at all, and will not get any by accident. The day the rooms
are authored owes this suite its canaries."* Five tests settle it, in `AssetInvariantTests`:

| test | kind | holds |
|---|---|---|
| `AnEarningRoomProducesAllThreeLeversOrNoneOfThem` | invariant | seats without spend earns nothing, spend without seats has nobody to charge, demand without either is a queue at a door that never opens — and each failure is silent |
| `ADemandCurveIsFlatAndTheOtherTwoAreNot` | invariant | §3.1's three levers, as arithmetic: a room's level moves its seats and its spend and must not move its demand |
| `TheMarketOnlyEverGrowsAndTheVillageIsItsUnit` | invariant | every demand figure in the game is a room's number times this |
| `TheTavernsSeatsReadAsTheWorldViewWasDesignedAgainst` | **canary** | 4 / 19 / 38 / 59 seats against 400 wanting in — §3 of `World_View_Design.md`'s table, which had no producer until today |
| `TheContractCommissionHasNoProducerBecauseItStillHasNoConsumer` | invariant | the day's finding, as a guard: authoring a commission curve before the mechanism exists gives the desk a stat nothing reads |

And one invariant gained its first honest exception. `NoBuildingEffectIsDeadAtMaxLevel`
was written on Days 8–9 and assumed, without saying so, that every effect is a thing the
player buys more of. `ServiceDemand` is not — demand belongs to the tier, and a room that
grew its own would collapse two of §3.1's three levers into one. The exemption is named by
**stat** in one place rather than special-cased at the call site, the same way
`GuildStatScope` names its three, and it is kept narrow by
`ADemandCurveIsFlatAndTheOtherTwoAreNot` asserting the curve really is flat — so it cannot
quietly become a hole for a curve somebody forgot to fill in.

### One note on the fixtures being built in code

`TradeFixture` constructs rooms in memory through `SerializedObject`, which breaks §1's
rule that tests load the real `.asset` files. Deliberately, and narrowly: §1's argument is
about asserting *content*, and nothing built on `TradeFixture` asserts a content value. It
checks that allocation serves the good table first, that the wage floor holds, that a tap
cannot invent a customer. Mechanism is logic, and logic may supply its own inputs. Assets
are authored through `SerializedObject` rather than by adding public setters, because a
setter that exists only for tests is a setter the game can call.

---

## 4. The save fixtures, and why they are permanent

`Tests/Editor/Fixtures/` holds real save files. `SaveRoundTripTests` writes with today's
capture and reads with today's restore, which proves those two agree with each other and
nothing else. Compatibility needs a file this build did not write, and the only way to
have one is to keep it.

**`SaveSchema.CurrentVersion` has never been bumped**, because no field has ever changed
shape. Days 10–11 changed what a *value* means — Max Level went from 10 to 25 — which
needs no migration and is precisely the kind of change that slips past unnoticed. Hence
the second fixture.

| Fixture | What it is |
|---|---|
| `save_real_session.json` | a genuine session: 219 quests completed, a run mid-timer with its dispatch-time snapshot, two standing orders, members in three activities |
| `save_v1_adventurers_at_old_ceiling.json` | the same guild with its roster at level 10, synthesised to the shape a pre-Days-10–11 build produced |
| `save_v1_content_since_removed.json` | points at a tier, building, archetype and quest no build has ever had |

The second one's real job is the reverse of how it reads. Raising a ceiling is always safe —
`Adventurer`'s constructor clamps to the definition's maximum. **Lowering one silently
re-levels people**, so this test stands guard over the day a balance pass shortens a track.

The third is the only thing that has ever exercised the repair path against a file that
actually needed repairing. It should report one unknown building, one dropped adventurer,
one dropped run, one dropped order, one member sent home and a tier fallen back — with
building levels and balances untouched, because the guild around the damage is meant to be
left standing.

**The fixtures do run.** They were written and left unverified, and the Day 12 baseline
of 47 — 43 plus these four — is the first positive record of them executing. Worth
saying because the third fixture is still the only thing that has ever exercised the
Day 6 repair path against a file that actually needed repairing.

**Add a fixture whenever the format or the meaning of a value in it changes.** They cannot
be recreated once lost, only approximated: the original Week-1 save was overwritten by an
autosave before it could be preserved, which is how the second fixture came to be
synthesised rather than kept.

**Day 18 is what they were kept for, and the number was one.** All four fixtures name a
`training_room`, and the revision retired it, so all four now restore with **exactly one
unknown building** — the fourth, which already pointed at a Quest Board no build ever had,
reports two. Nothing else moved: no adventurer dropped, no run, no order, no tier fallen
back, and every building level around the gap exactly as written. `save_day14_played_in`
was pinned at zero repairs on Day 14 *specifically* so that this would arrive as a red test
with a number in it rather than as a silence, and it did. Two tests were renamed, because
`TheLastSaveOfTheOneEconomyGameLoadsWithoutRepair` had stopped being true and a test whose
name is a lie is worse than no test.

The assertions are now field by field rather than through `HasRepairs`. *Something was
repaired* is exactly the assertion that would have hidden a second thing being repaired,
which on the day a fixture legitimately goes from zero to one is the only failure mode
left. It is also the first time in the project's life that the Day 6 repair path has run
against **real** saves that genuinely needed repairing — until now only the synthesised
third fixture ever exercised it.

---

## 5. Where the assembly sits

`IdleGuild.Tests.Editor` references all seven assemblies and nothing references it, so it
sits above `IdleGuild.UI` exactly as `IdleGuild.UI` sits above `IdleGuild.App`. The
architectural bet is untouched: the features are still Core-only, and a test assembly that
can see everything cannot be depended upon by anything.

It is `Editor`-platform only and constrained on `UNITY_INCLUDE_TESTS`, so nothing here
reaches a player build.

---

## 6. What it found on the way in

Before a single test ran, writing them turned up two things:

- **`QuestResolution.FailureChance`'s doc comment was wrong.** It said the rate "doubles as
  the party falls to half" of recommended power. The formula is `2 − ratio`: half power
  gives 1.5×, and it only doubles at *no* power at all. Both cases are pinned now.
- **`Object.GetInstanceID()` is deprecated in Unity 6 and its `[Obsolete]` is an error,
  not a warning.** Worth carrying into Week 3, when the ad and IAP SDKs arrive with their
  own API-age problems: a deprecation in this engine version can fail a build outright.

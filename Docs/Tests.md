# The test suite

An EditMode assembly at `Assets/_Project/Tests/Editor/`. Run it from
**Window → General → Test Runner → EditMode → Run All**. It takes about 50 ms.

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
Dragon's Roost at `720s / 40%` against a starter party. **These are expected to be updated
by a balance pass.** Updating one is part of that work; updating an invariant is a warning
that something else is wrong.

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

### Still manual, and why

- **Step 4 — Town in about ten minutes.** A pacing judgement. A test can assert a band;
  only a person can tell whether the first ten minutes feel alive.
- **Step 6 — the colours.** `Format.RarityClass` returning `rarity--epic` is testable.
  USS resolving that class to purple is not.
- **Step 8 — is it *appropriately* hard.** The clamps are pinned. Whether Dragon's Roost
  reads as a fair fight at a guild that earned its way to Capital needs a played-in save,
  and belongs to Day 14.

Call it fifteen minutes of hand-checking rather than forty.

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

**Add a fixture whenever the format or the meaning of a value in it changes.** They cannot
be recreated once lost, only approximated: the original Week-1 save was overwritten by an
autosave before it could be preserved, which is how the second fixture came to be
synthesised rather than kept.

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

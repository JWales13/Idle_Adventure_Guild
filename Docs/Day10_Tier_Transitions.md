# Days 10–11 — Tier transitions

Village → Town → City → Capital, with the content that fills the top two tiers.
The tier assets themselves already existed and did not need editing: City and Capital
were already raising Max Quest Tier to 3 and 4 and quest slots to 3 and 4. What was
missing was anything for them to unlock. **Data only: no `.cs` file was changed.**

---

## 1. The architectural bet, checked

Days 10–11 is the day the modular architecture was supposed to be tested, so the test
is worth stating before the numbers.

**Result: eighteen changed paths, none of them a script.** Eight new files — four
`.asset` and their `.meta` — five existing assets edited, and five documents: the model,
this one, the Ledger, and superseded-banners on Day04 and Day08.
No field was added to any definition, no branch to `GuildState.Aggregate`, no service
method, no UI. The check to run in GitHub Desktop is simply that the changed-files
list contains no `.cs`.

Three things had to be true for that, and all three already were:

- **Content declares its own availability.** `QuestDefinition.MinimumTierOrder` and
  `AdventurerDefinition.MinimumTierOrder` mean a new tier unlocks things without the
  tier asset listing them, so `Tier_City` and `Tier_Capital` never had to be touched.
- **`QuestResolution.IsAvailable` reads `MaxQuestTier` off `IGuildStats`.** The tier
  seeds that stat and the Quest Board will add to it later; a tier-4 quest becoming
  available at Capital goes through the same path a tier-6 quest will after launch.
- **The UI was already written for five rarities.** `Format.RarityClass`,
  `Outcomes.Describe`, and `--color-rarity-epic` / `--color-rarity-legendary` in
  `Tokens.uss` all existed from Day 7 and had never been exercised. `RosterView`
  iterates `GameContent.Adventurers` and renders whatever it finds, greying out what
  is locked with the reason attached. Two new archetype assets appeared in the game
  fully styled, gated and explained, with nothing else done.

The one place discipline rather than the compiler is holding the line is the same one
Day 7 flagged: `GuildContext` says views hold no rules. Nothing here challenged it.

---

## 2. What was actually wrong with rarity

§3 of `Day08_Building_Trees.md` reported that higher-rarity archetypes were pointless —
a fully trained Wandering Ranger contributing 233 Power against a guild-wide +331 from
the Training Room at level 40. That is true, and it is not the main problem. Two
larger ones were underneath it.

### The model had never bought one

The Day 8–9 policy hired the **cheapest** available archetype. Run to completion it
bought Militia Recruits and nothing else: across a 26-hour simulated game the Hedge
Knight and the Wandering Ranger were **never purchased once**. So "rarity is pointless"
was a conclusion the model had no way to reach or refute — it had never tried one.

The policy now hires the best archetype it can afford for a slot it needs, and saves
for the best one when the bed is a pure upgrade. That is the change that made the rest
of this visible.

### Beds, not power, are the ceiling on roster quality

The Inn maxes at 16 beds. A Capital guild needs 12 to field four parties of three. And
**nothing can dismiss an adventurer** — `AdventurerRoster.Remove` exists but no service
or screen calls it. So a bed, once filled, is filled for the rest of the game.

With the Dragonsworn Champion gated to Capital, a player who spends their spare beds
during City can never hire one at all. That was confirmed rather than assumed: with the
old bed budget and greedy hiring, the model finished a full game with five Battlemages
and no Champion. **No power number fixes that**, which is why the fix is partly
elsewhere — see §6.

### And the guild bonus needed real competition

Fixed by making the archetype ladder geometric rather than hand-picked: **each band
doubles the archetype's own power and costs five times the hire.** Fully trained, and
with the Training Room's +331 included, that is ×1.18 / ×1.30 / ×1.46 / ×1.63 going up
the ladder — the multiple *widens* as the bands get stronger, because the flat guild
bonus is a shrinking fraction of the total. A maxed Champion contributes 1,146 against
that +331 instead of the Ranger's 233.

---

## 3. Adventurer Max Level, raised from 10 to 25

Max Level 10 saturated by Town — the same failure Days 8–9 found in the buildings,
one scale down. 25 was chosen the way the reputation thresholds were: by running it.

| Max Level | Capital | everything maxed | best endgame party |
|---|---|---|---|
| 20 | 4h22m | 13h22m | ×2.56 |
| **25** | **4h30m** | **15h30m** | **×3.54** |
| 30 | 4h35m | 25h58m | ×4.94 — **past the 4× clamp** |

30 puts the strongest party beyond `QuestResolution.MaximumSpeedMultiplier`, where the
last training levels buy literally nothing — a dead-levels failure, and the same one
Days 8–9 caught at the top of the building trees. 20 leaves the ladder short. 25 lands
the best party at 3.54 with headroom and still climbing.

### The five archetypes

Power and training are one rule applied five times, not five sets of numbers: `φ = 2.0`
on power, `×5` on the hire, `×3` on training. Level-1 power on the three existing
archetypes is unchanged at 3 / 6 / 12, so the opening is exactly as it was.

| | Militia Recruit | Hedge Knight | Wandering Ranger | Arcane Battlemage | Dragonsworn Champion |
|---|---|---|---|---|---|
| Rarity | Common | Uncommon | Rare | **Epic** | **Legendary** |
| Minimum Tier Order | 0 | 0 | 1 | 2 | 3 |
| Recruit cost | 25 | 120 | 600 | **3,000** | **15,000** |
| Power By Level | Base 3, Linear 0.8, Growth 0.05 | Base 6, Linear 1.6, Growth 0.05 | Base 12, Linear 3.2, Growth 0.05 | Base 24, Linear 6.4, Growth 0.05 | Base 48, Linear 12.8, Growth 0.05 |
| Max Level | 25 | 25 | 25 | 25 | 25 |
| Training Cost | Base 20, Growth 0.34 | Base 60, Growth 0.34 | Base 180, Growth 0.34 | Base 540, Growth 0.34 | Base 1,620, Growth 0.34 |
| Base Recovery | 45s | 60s | 75s | 90s | 105s |
| Power at 25 | 71.6 | 143.2 | 286.4 | 572.8 | 1,145.6 |

Training growth came down from 0.45 to 0.34 for the same reason the building trees were
re-spaced: a cost curve tuned for ten levels is unreachable across twenty-five.

### Which gate actually opens each band

Worth writing down because it is not obvious from either asset alone. The Tavern's
Recruitable Rarity curve is unchanged from Day 8–9 and opens the bands at Tavern
**9 / 17 / 25 / 32**. The Tavern level on *arriving* at each tier is 1 / 4 / 20 / 48.

| | Tavern threshold | Min Tier Order | Binds |
|---|---|---|---|
| Hedge Knight | 9 | 0 | **Tavern**, mid-Town |
| Wandering Ranger | 17 | 1 | **Tavern**, late Town |
| Arcane Battlemage | 25 | 2 | **Tavern**, early City |
| Dragonsworn Champion | 32 | 3 | **tier**, at Capital |

So the Tavern genuinely gates three of the four bands, which is what §02 of the Ledger
always claimed it did, and Minimum Tier Order gates the fourth. The two are not
redundant: Minimum Tier Order is the **anti-tunnelling backstop**, exactly parallel to
the multi-building tier gate. A player who pushes the Tavern to 32 while still in Town —
possible, since the tier gate also wants Training Room 11 and Inn 9 — is stopped from
hiring a Champion by the tier, not by the Tavern.

**Recruitable Rarity saturates at Tavern 32 and that is not a dead level.** It is a
gating effect against a five-value enum, so it has to stop somewhere; the Tavern's
scaling effect, Reward Yield, is still gaining +14 at level 90.

---

## 4. The two quests

| Field | Sunken Crypt | Dragon's Roost |
|---|---|---|
| Id | `sunken_crypt` | `dragons_roost` |
| Quest Tier / Minimum Tier Order | 3 / 2 | 4 / 3 |
| Required Adventurers | 3 | 3 |
| Recommended Power | 140 | **1,250** *(spec said 420)* |
| Base Duration | 240s | 360s |
| Base Failure Chance | 0.18 | 0.20 |
| Gold Reward | **1,800** *(spec said 1,000)* | **3,600** *(spec said 2,600)* |
| Reputation Reward | **120** *(spec said 67)* | **240** *(spec said 190)* |

### Why the tier-4 spec had to move

§3 of the Day 8–9 document pencilled Recommended Power 420 and warned that shipping
different numbers would move the City and Capital tiers. It has to move, for a reason
that only becomes visible once the archetypes are fixed:

> **At Recommended Power 420, every party a finished guild can field is past the 4×
> speed clamp.** Three adventurers at the Training Room's +331 alone clear 1,680
> without a single archetype level between them. Above that ratio, duration stops
> falling and failure chance is already zero — so the whole rarity ladder, and the last
> fifteen Training Room levels, buy nothing measurable. The ceiling was below where the
> game ends.

1,250 puts a finished guild's four parties at **×3.54 / ×2.17 / ×1.37 / ×1.02** — a real
spread, nothing clamped, and the weakest three sitting at the quest's design point with
a 17% failure rate. It also gives the Sunken Crypt a job at Capital: the model's fourth
party earns more there than it does failing one Dragon's Roost run in five, which is the
texture the per-party choice was added to see.

Reputation was set at the same ~15:1 ratio to gold the three shipped quests already use.

---

## 5. City's reputation gate, re-derived

`Tier_City.ReputationToAdvance` moves **28,000 → 65,000**. Village (30) and Town (830)
are unchanged.

The rule is unchanged from Day 8–9: each threshold is 75% of what the guild is actually
holding when the *building* half of that gate closes, so reputation confirms the player
has been questing rather than becoming the wall. 28,000 was derived when nothing paid
tier-3 reputation; with the Sunken Crypt authored, the guild arrives at that gate holding
86,000–120,000 depending on how it spent its beds, and the old threshold had stopped
confirming anything at all.

65,000 is 75% of the **lower** of the two player models in §6 — the gate has to be
satisfied before the buildings for both, not just for the faster one.

---

## 6. Two player models, and why there are two

`Docs/tools/guild_model.py` now reports the arc twice, because the roster is a one-way
ratchet and that makes patience a real fork:

| | Capital | everything maxed | decisions | longest gap | final roster |
|---|---|---|---|---|---|
| **Patient** — leaves spare beds empty through City | 5h53m | 20h31m | 420 | 27 min | 6 Common, 2 Uncommon, 4 Rare, **4 Legendary** |
| **Impatient** — spends them on Battlemages | 4h30m | 15h30m | 416 | 25 min | 4 Common, 3 Uncommon, 4 Rare, **5 Epic** |

Both finish, neither is broken, and the trade reads correctly: patience costs about
five hours and buys a guild whose best party is 63% stronger. The information is on the
screen — `RosterView` shows the Dragonsworn Champion greyed out with "the guild has not
reached the tier they appear at" — so it is a legible decision rather than a trap.

**But it is a decision the player cannot take back, and that is the one thing here worth
carrying into Day 12.** Nothing in the game can dismiss an adventurer. A player who
fills their last bed with a Battlemage in City has permanently locked themselves out of
the tier's headline unlock, and no amount of gold undoes it.

> **Day 12 should ship a dismiss/retire action on the roster screen.**
> `AdventurerRoster.Remove` already exists and is already called by save restoration;
> what is missing is a service method wrapping it and a confirm dialog. It is small, and
> it turns an irreversible mistake into a reversible one.

There is a second Day 12 requirement hiding in the same place. **A `QuestAssignment`
holds its party for the life of the order** — the class says so — which means a newly
hired Champion does nothing at all until the player cancels the standing order and
re-dispatches. The model has to simulate that re-dispatch for a late hire to matter, and
so does the assignment screen: if re-forming a party is buried, the best hire in the
game is inert.

---

## 7. A correction to the Day 8–9 numbers

`guild_model.py` chose one quest for the whole guild and judged it using the
**strongest** party's power. That is exact while every adventurer is identical, and
wrong the moment they are not — which is the situation this day creates. Each party now
picks its own work.

On **unchanged** Day 8–9 assets that correction alone moves the published figures:

| | published | corrected |
|---|---|---|
| Village → Capital | 4h07m | **4h41m** |
| Everything maxed | 17h21m | **19h37m** |
| Longest stretch with nothing to buy | 52 min | **59 min** |

So the Day 8–9 figures were roughly 13% optimistic on the tail. **The corrected column
is what the numbers in this document should be compared against**, and both player
models above straddle it. The purchase-gap profile — the number Days 8–9 said the model
exists to protect — improves substantially: the 90th percentile falls from 19 minutes to
5–7, and the worst gap from 59 minutes to 25–27, because 300-odd training purchases now
fill the stretches where only an expensive Tavern level was on offer.

---

## 8. Verification

No Editor steps were needed to author any of this: every value was written straight into
the `.asset` YAML, which is the Day 8–9 practice and the direct lesson of Day 4–5's four
transcription slips. Focus the Editor to reimport, then:

1. **The new assets import clean.** No `OnValidate` warnings in the console. In
   particular `GameContent` should not warn — it now lists five adventurers and five
   quests. Confirm `Library/ScriptAssemblies/IdleGuild.*.dll` is untouched and `Logs/`
   has no `error CS`: nothing here should have caused a recompile at all.

2. **Nothing changed for an existing save.** Load a Week-1 save. Adventurers keep their
   levels and now read `Level n / 25` with a Train button live again. Building levels,
   balances and quests in flight are untouched. Ids were not changed, so nothing is
   dropped and `SaveRestoreReport` should report zero repairs.

3. **The opening is exactly as it was.** New guild: 150 gold, Inn for 40, Militia
   Recruit for 25, dispatch the Rat Cellar. Level-1 power is still 3.

4. **Town is still about ten minutes**, and reaching it opens nothing new — the Hedge
   Knight waits for Tavern 9, a few levels in.

5. **The roster screen explains every lock.** At Village, the Battlemage and the
   Champion should both be visible, greyed, and carrying a reason. The Battlemage's
   should change from the tier reason to the Tavern reason once City is reached below
   Tavern 25 — the two gates are distinguishable to the player, which is the point of
   having both.

6. **Rarity colours land.** Epic reads purple and Legendary gold, from `Tokens.uss`. No
   asset or stylesheet was edited to make that happen; it is the Day 7 work paying off,
   and if it does not work the fault is in `Format.RarityClass`, not here.

7. **The tier-3 quest appears at City and the tier-4 at Capital**, and neither before.
   Advance with the debug console rather than playing four hours.

8. **Dragon's Roost is genuinely hard at Capital.** With a fresh Capital roster it should
   read a long duration and a visible failure chance, and the Sunken Crypt should still
   be the better-paying job for a weak party. If Dragon's Roost is trivial on arrival,
   Recommended Power is wrong and §4 needs re-running.

9. **The model still matches the assets.**

   ```
   python3 Docs/tools/guild_model.py --profile --checks
   ```

   `--checks` prints the no-dead-levels table and the rarity ladder. Every curve's last
   step must still move, and every band must still read ×2.00 archetype. If a number in
   this document and a number in that output disagree, the document is wrong — but a
   drifted model is worse than none, so fix them in the same commit either way.

# Days 8–9 — Building trees

The full upgrade trees for Tavern, Training Room and Inn, the re-spaced tier gates,
and the reasoning that produced them. Data only: no code changed.

---

## 1. What was wrong

The Day 4–5 numbers were explicitly first-pass, and modelling the whole arc showed
they failed structurally rather than numerically:

| | Before | After |
|---|---|---|
| Village → Capital | 4h 28m | 4h 07m |
| Everything maxed | **2h 15m** — two hours *before* Capital | 17h 21m |
| Purchase decisions in the whole game | **64** | **195** |
| Longest stretch with nothing to buy | hours | 52 min |

The middle row is the real failure. All three buildings hit level 10 at 2h15m while
Capital was still two hours away, so the back half of the game had **nothing to buy** —
the player sat watching reputation accrue toward a gate with no lever to pull. Gold
ended at 22 million against a cost curve that had stopped.

The cause was `Max Level 10` stretched across four tiers. Thirty building purchases
cannot carry a Village→Capital arc; roughly eight per tier, two of them forced by the
gate.

---

## 2. The shape that replaced it, and why it is asymmetric

The obvious fix — more levels, steeper costs — produced a worse game, and the failure
is worth recording because it is not obvious:

> With all three buildings on the same 40-level, 1.34-growth curve, **the top eleven
> levels were unreachable.** Not expensive: unreachable. Time-per-level diverged and
> the model ran 200 simulated hours without closing them.

Cost was growing 1.34 per level while income grew 1.12, so every level took 1.2× longer
than the last, forever. And the reason income could not keep up is a property of the
simulation the buildings live in:

**Only the Tavern compounds.** Reward Yield multiplies gold without bound, so its price
can grow geometrically and level 80 is still worth buying. The other two are bounded by
the game's own clamps — Training Room power stops shortening a quest once the party
reaches 4× the recommended power (`QuestResolution.MaximumSpeedMultiplier`), and Inn
recovery can only ever remove the *rest* half of a cycle. Charging a geometric price
for a bounded benefit is what put the tail out of reach.

So the three trees are deliberately different lengths:

| | Tavern | Training Room | Inn |
|---|---|---|---|
| Role | the compounding spine | keeps parties level with harder quests | throughput, then finished |
| Max level | **90** | **40** | **30** |
| Cost growth | 15%/level | 19%/level | 21%/level |

The Tavern is long and cheap-growing because its benefit never saturates. The other two
are short and steep because theirs do — the player finishes them and moves on, which is
a legitimate shape for a building rather than a flaw. **The tier gate is what stops the
player tunnelling into the Tavern alone**, and it is the mechanism the design has always
named for exactly this.

### The three curves

| Field | Tavern | Training Room | Inn |
|---|---|---|---|
| Max Level | 90 | 40 | 30 |
| Cost To Reach Level | Base 50, Linear 0, Growth **0.15** | Base 45, Linear 0, Growth **0.19** | Base 40, Linear 0, Growth **0.21** |
| Effect 0 | Reward Yield · Multiplicative · Base 0.20, Growth 0.08 | Adventurer Power · Additive · Base 2, Growth 0.14 | Housing Capacity · Additive · Base 2, Linear 0.5 |
| Effect 1 | Recruitable Rarity · Additive · Base 0, Linear 0.13 | — | Recovery Speed · Multiplicative · Base 0.10, Growth 0.10 |

Every effect is still improving at max level — the last step is +8% on the Tavern, +14%
on the Training Room, +10% on the Inn. **No dead levels**, which was the specific thing
worth checking after the reachability failure above: a building whose effect saturates
before its max level is the same bug wearing a different hat.

Recruitable Rarity at 0.13 per level opens the bands at **Tavern 9 / 17 / 25 / 32**
(Uncommon, Rare, Epic, Legendary). The Inn reaches 16 beds at level 30, against the 12 a
Capital guild needs for four slots of three.

### Tier gates

Gates are on each building's own scale, which is why the numbers no longer match
across the row:

| | Village | Town | City | Capital |
|---|---|---|---|---|
| Quest slots | 1 | 2 | 3 | 4 |
| Max quest tier | 1 | 2 | 3 | 4 |
| Tavern ≥ | 4 | 20 | 48 | — |
| Training Room ≥ | 3 | 11 | 26 | — |
| Inn ≥ | 3 | 9 | 21 | — |
| Reputation ≥ | 30 | 830 | 28,000 | — |

**The reputation thresholds are derived, not chosen.** Each is 75% of what the guild is
actually holding when the building half of the gate closes, measured from the model. The
intent is that reputation *confirms* the player has been questing rather than becoming
the wall itself — a player blocked on gold can spend their way forward, a player blocked
on reputation can only wait, which is a worse gate. The model verifies this holds: at
every tier the building requirement is satisfied last.

### Quests, which had to move too

Balancing a building tree against fixed quest rewards is not possible — the buildings
only matter as a multiplier on what a quest pays. These changed on a buildings day, and
that is worth knowing about:

| | Rat Cellar | Bandit Patrol | Ruined Watchtower |
|---|---|---|---|
| Required adventurers | 1 | 2 | 2 |
| Recommended power | 4 | 14 | 45 *(was 40)* |
| Duration | 45s | 90s | 150s *(was 180s)* |
| Base failure | 5% | 12% | 16% *(was 18%)* |
| Gold | **48** *(was 30)* | **145** *(was 90)* | **375** *(was 320)* |
| Reputation | **3** *(was 2)* | **10** *(was 6)* | **25** *(was 20)* |

All three also gained a description, which the upgrade and quest cards now show.

---

## 3. What Days 10–11 must deliver

The building curves were tuned against a four-tier arc, but **the tier-3 and tier-4
quests do not exist yet**. City and Capital raise Max Quest Tier to 3 and 4 with nothing
authored to fill them, so today's model stands in for them. If Day 10–11 ships different
numbers, re-run the model and expect the City and Capital tiers to move.

**Required, to keep the arc the shape it is now:**

| Field | Tier 3 quest | Tier 4 quest |
|---|---|---|
| Quest Tier / Minimum Tier Order | 3 / 2 | 4 / 3 |
| Required Adventurers | 3 | 3 |
| Recommended Power | 140 | 420 |
| Base Duration | 240s | 360s |
| Base Failure Chance | 0.18 | 0.20 |
| Gold Reward | 1,000 | 2,600 |
| Reputation Reward | 67 | 190 |

**Two problems Day 10–11 inherits, both found while modelling:**

1. **Higher-rarity archetypes are currently pointless.** At Training Room 40 the guild
   grants every adventurer +331 Power, while a fully trained Wandering Ranger's own
   contribution is 233 and a Militia Recruit's is 49. The guild bonus swamps the
   archetype, so a Legendary hire would be a cosmetic purchase. The Epic and Legendary
   archetypes that Tavern 25 and 32 unlock need base power in the same league as the
   guild bonus at the tier they appear — roughly Base 40 / Linear 25 for Epic and Base
   90 / Linear 55 for Legendary — or rarity is a badge rather than a decision.

2. **Adventurer Max Level 10 saturates early.** Training runs out long before the
   buildings do, so the per-person progression track stops mattering by Town. Raising it
   to 25–30 with a matching cost curve would keep it alive; that is a Day 10–11 call
   because it interacts with the archetypes above.

---

## 4. The model

`Docs/tools/guild_model.py` replicates the loop — `ScalingCurve.Evaluate`,
`GuildState.Aggregate`, `QuestResolution`, rest timers, quest slots, standing orders —
and takes outcomes at their expected value so a curve change is legible rather than lost
in variance.

```
python3 Docs/tools/guild_model.py --profile
```

It exists because **the shape of a curve is not visible in the Inspector.** A cost curve
and an effect curve that each look sensible alone produced a game that ran out of things
to buy two hours before it ran out of tiers, and no amount of staring at the asset would
have shown that.

**It is a model, not a source of truth.** If the assets change and it does not, its
answers are confidently wrong. Update it in the same commit as any balance change — the
values in it are a copy, and a copy that drifts is worse than no copy at all.

The number it exists to protect is the **purchase-gap profile**, not the tier times. A
long tail there means a stretch of the game with nothing to buy, which is the failure
that started this whole pass.

The simulated player is stated explicitly in the file rather than assumed: advance a
tier when possible, hire before anything else, push the short building in the gate, then
buy the cheapest thing. The hire rule is not a nicety — with gate progress first, the
model poured its starting gold into Inn levels, never bought an adventurer, and sat with
no way to earn anything at all. Which is also a real trap a real player could fall into,
and worth remembering when the Day 12 assignment UI is designed.

---

## 5. Verification

No Editor steps: the values were written straight into the `.asset` YAML rather than
retyped through the Inspector. That is deliberate — the Day 4–5 pass hand-copied its
tables and produced four transcription slips, one of which gave the Inn's Housing
Capacity effect the *cost* curve, so a level-1 Inn granted 50 beds instead of 2. A wrong
curve looks exactly like a right one in the Inspector.

Focus the Editor to reimport, then:

1. **The assets read back correctly.** Tavern Max Level 90, cost growth 0.15, Reward
   Yield Base 0.20 / Growth 0.08. Training Room 40 and Inn 30. No console warnings from
   `OnValidate`.
2. **The opening still works.** Play a fresh guild: 150 gold, build the Inn for 40, hire
   a Militia Recruit for 25, dispatch. If the Inn ever costs more than starting gold
   minus a recruit, the guild can never earn anything — the model checks this and prints
   it, because it is unrecoverable rather than merely slow.
3. **Town in about ten minutes.** The gate is Tavern 4 / Training Room 3 / Inn 3 and 30
   reputation. The model says 8 minutes; a human tapping rather than an optimal policy
   should land near 10.
4. **The upgrade overlay tells the truth.** Open the Tavern. Reward yield should read
   `+20% → +22%` at level 1, and the level counter `Lv 1 / 90`. This is the Day 7 overlay
   reading the curve off the asset, and it is now the fastest way to sanity-check a
   curve without leaving the game.
5. **Nothing is unreachable.** At Capital, all three buildings should still have levels
   left and the next one should still be visibly worth buying.

Steps 1–4 take five minutes and are worth doing before Day 10–11 builds tier content on
top of these numbers.

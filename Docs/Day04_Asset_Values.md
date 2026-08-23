# Day 4–5 — Asset authoring values

First-pass numbers for the assets that make the core loop exercisable. Balance is
deliberately rough: the shape of the curve is what matters now, and Day 13 is the
first real balancing pass.

Create in this order — tiers reference buildings, and GameContent references
everything.

All assets go under `Assets/_Project/Data/` (subfolders already created). Menu paths
are under **Create → Idle Guild**.

A `ScalingCurve` is three fields: **Base Value** (the value at level 1), **Linear Per
Level** (added per level above the first), **Growth Per Level** (compounding, as a
fraction — `0.55` is +55% per level). Leave a field at 0 if the table omits it.

---

## 1. Buildings — `Create → Idle Guild → Building Definition`

Save as `Data/Buildings/Building_Tavern.asset` etc. Max Level is 10 for all three.
Description and Icon can stay empty; they are Week 3 work.

### Building_Tavern

| Field | Value |
|---|---|
| Id | `tavern` |
| Display Name | Tavern |
| Minimum Tier Order | 0 |
| Max Level | 10 |
| Cost To Reach Level | Base 75, Linear 0, Growth 0.55 |

Effects (size 2):

| # | Stat | Kind | Value Per Level |
|---|---|---|---|
| 0 | Reward Yield | Multiplicative | Base 0.15, Linear 0.10, Growth 0 |
| 1 | Recruitable Rarity | Additive | Base 0, Linear 0.5, Growth 0 |

Reward Yield is a bonus fraction on 1.0, so L1 pays ×1.15 and L10 pays ×2.05.
Recruitable Rarity is floored, so the rarity band opens at L1 Common, L3 Uncommon,
L5 Rare, L7 Epic, L9 Legendary.

### Building_TrainingRoom

| Field | Value |
|---|---|
| Id | `training_room` |
| Display Name | Training Room |
| Minimum Tier Order | 0 |
| Max Level | 10 |
| Cost To Reach Level | Base 60, Linear 0, Growth 0.60 |

Effects (size 1):

| # | Stat | Kind | Value Per Level |
|---|---|---|---|
| 0 | Adventurer Power | Additive | Base 2, Linear 3, Growth 0.15 |

Roughly +2 power at L1, +24 at L5, +102 at L10, added to every adventurer.

### Building_Inn

| Field | Value |
|---|---|
| Id | `inn` |
| Display Name | Inn |
| Minimum Tier Order | 0 |
| Max Level | 10 |
| Cost To Reach Level | Base 50, Linear 0, Growth 0.50 |

Effects (size 2):

| # | Stat | Kind | Value Per Level |
|---|---|---|---|
| 0 | Housing Capacity | Additive | Base 2, Linear 1, Growth 0 |
| 1 | Recovery Speed | Multiplicative | Base 0.1, Linear 0.1, Growth 0 |

2 beds at L1 rising to 11 at L10; rest time ×1.1 faster at L1, ×2.0 at L10.

**The Inn is the opening move.** Housing Capacity is 0 with no Inn built, so a fresh
guild can recruit nobody until it goes up. That is intended, not a bug — starting gold
exists to buy it.

---

## 2. Guild tiers — `Create → Idle Guild → Guild Tier Definition`

Save as `Data/Tiers/Tier_Village.asset` etc. Requirements To Advance takes building
*asset references*, so create these after the buildings.

| Field | Village | Town | City | Capital |
|---|---|---|---|---|
| Id | `village` | `town` | `city` | `capital` |
| Display Name | Village | Town | City | Capital |
| Order | 0 | 1 | 2 | 3 |
| Quest Slots | 1 | 2 | 3 | 4 |
| Max Quest Tier | 1 | 2 | 3 | 4 |
| Reputation To Advance | 120 | 1200 | 9000 | 0 |

Requirements To Advance:

| Tier | Requirements (size) |
|---|---|
| Village | 3 — Tavern ≥ 3, Training Room ≥ 3, Inn ≥ 3 |
| Town | 3 — Tavern ≥ 6, Training Room ≥ 6, Inn ≥ 5 |
| City | 3 — Tavern ≥ 9, Training Room ≥ 9, Inn ≥ 8 |
| Capital | 0 — leave empty |

Capital with no requirements and 0 reputation is what marks it the final tier; the
asset will warn if exactly one building gates a tier, which is the tier-gate rule
enforcing itself.

---

## 3. Adventurers — `Create → Idle Guild → Adventurer Definition`

Save as `Data/Adventurers/Adventurer_MilitiaRecruit.asset` etc. Three is enough to
exercise the loop; the Epic and Legendary archetypes belong with the tier work on
Day 10–11.

| Field | Militia Recruit | Hedge Knight | Wandering Ranger |
|---|---|---|---|
| Id | `militia_recruit` | `hedge_knight` | `wandering_ranger` |
| Display Name | Militia Recruit | Hedge Knight | Wandering Ranger |
| Rarity | Common | Uncommon | Rare |
| Minimum Tier Order | 0 | 0 | 1 |
| Recruit Cost Gold | 25 | 120 | 600 |
| Power By Level | Base 3, Linear 2, Growth 0.10 | Base 6, Linear 4, Growth 0.10 | Base 12, Linear 8, Growth 0.12 |
| Max Level | 10 | 10 | 10 |
| Training Cost To Reach Level | Base 20, Linear 0, Growth 0.45 | Base 40, Linear 0, Growth 0.45 | Base 150, Linear 0, Growth 0.45 |
| Base Recovery Seconds | 45 | 60 | 75 |

The Hedge Knight is Uncommon, so the Tavern must reach level 3 before he can be
hired — the rarity gate in practice.

---

## 4. Quests — `Create → Idle Guild → Quest Definition`

Save as `Data/Quests/Quest_Rat_Infested_Cellar.asset` etc. Recommended Power is tuned for the
whole party, since party power is the **sum** of its members.

| Field | Rat Infested Cellar Cleanup | Bandit Patrol | Ruined Watchtower |
|---|---|---|---|
| Id | `rat_infested_cellar` | `bandit_patrol` | `ruined_watchtower` |
| Display Name | Rat Infested Cellar Cleanup | Bandit Patrol | Ruined Watchtower |
| Quest Tier | 1 | 1 | 2 |
| Minimum Tier Order | 0 | 0 | 1 |
| Required Adventurers | 1 | 2 | 2 |
| Recommended Power | 4 | 14 | 40 |
| Base Duration Seconds | 45 | 90 | 180 |
| Base Failure Chance | 0.05 | 0.12 | 0.18 |
| Gold Reward | 30 | 90 | 320 |
| Reputation Reward | 2 | 6 | 20 |

Duration scales with the square root of party power over recommended, clamped between
half and double the base. Failure doubles at half the recommended power and hits zero
at twice it.

---

## 5. GameContent — `Create → Idle Guild → Game Content`

One asset, saved as `Data/GameContent.asset`. This is the only asset that knows about
all four content types, which is why it lives in the App layer.

| Field | Value |
|---|---|
| Buildings | size 3 — Tavern, Training Room, Inn |
| Tiers | size 4 — Village, Town, City, Capital |
| Adventurers | size 3 — Militia Recruit, Hedge Knight, Wandering Ranger |
| Quests | size 3 — Rat Infested Cellar, Bandit Patrol, Ruined Watchtower |
| Starting Gold | 150 |
| Starting Reputation | 0 |
| Maximum Offline Seconds | 28800 (8 hours) |

Order within each array does not matter — tiers are sequenced by their Order field, and
content declares its own unlock tier.

---

## 6. Scene setup

1. New scene, saved as `Assets/_Project/Scenes/Guild.unity` (create the folder).
2. Empty GameObject named **Game**.
3. Add **Game Bootstrap**, assign `GameContent.asset` to its Content field.
4. Add **Debug Console Overlay** on the same object. Leave Bootstrap empty — it finds
   the one in the scene — or drag Game onto it.
5. Optionally tick **Use Fixed Random Seed** on the bootstrap to make quest rolls
   repeat identically between runs while checking behaviour.

## 7. Smoke test

Press Play and work down the console. This is the Week 1 checkpoint minus the UI:

1. **Treasury** reads 150 gold.
2. **Build Inn** — 100 gold left, Beds reads 0/2, Recovery ×1.10.
3. **Hire Militia Recruit** — 75 gold left, Beds 1/2, roster shows power 3.0.
4. **Send party on Rat Infested Cellar Cleanup** — one quest in flight, ~52s, fail ~6%.
5. **+1 min** — quest resolves, gold goes up by 30 (or not, on a failure), the recruit
   shows as resting, then the order restarts on its own.
6. **Build Tavern** — Reward yield ×1.15, and the next quest pays 34 rather than 30.
7. **Build Training Room** — the recruit's power jumps to 5.0 and the quest timer drops.
8. **Hire a second recruit, send the party on Bandit Patrol** — needs two, so this is
   what the second bed was for.
9. **+1 hour** — several quests resolve, gold and reputation climb.
10. **Offline 8h** — reports gold, reputation and a quest count in one line, and the
    numbers should be in the same ballpark as eight times the hour above.
11. Upgrade Tavern, Training Room and Inn to level 3, reach 120 reputation, and
    **Advance to Town** — quest slots go to 2 and Ruined Watchtower becomes available.

Anything that misbehaves here is a logic bug worth fixing before Day 6 builds save/load
on top of it.

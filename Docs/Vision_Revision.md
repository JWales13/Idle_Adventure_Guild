# Vision — an idle hotel tycoon that happens to be an adventurers' guild

Written Day 14. **Supersedes §02 of `GUILD_LEDGER.md`, rewrites §03's remaining weeks,
and replaces the first draft of this file entirely** — that draft had four buildings with
only one of them earning, which is a management sim's structure, not a tycoon's.

Design only. **No code and no asset has changed.** This exists to be argued with, and
then to be modelled, and only then to be built.

---

## 1. The game, in one paragraph

> **An idle hotel tycoon wearing an isekai guild hall.** Your guild is a business with
> five rooms. Four of them make money every hour — the tavern serves townsfolk, the inn
> lets rooms to travellers, the provisioner sells rope and rations, the front desk takes
> a cut of every contract. You hire staff to serve the crowd and spend the takings making
> each room better, which draws a bigger crowd, which pays for the next upgrade. The
> fifth room is the barracks, where the adventurers live — and adventurers are how you
> earn *reputation*, which is the only thing that unlocks the next tier of everything.

**Building up is the game.** Everything else is how the theme earns its place in it.

**The guild never moves.** Village → Town → City → Capital is one hall, and the
settlement grows *around* it — because a working adventurers' guild is exactly the thing
that turns a village into a town. This is not flavour: it is what makes equipment persist
without explanation, it is why the art direction is one frontage evolving rather than
four unrelated buildings, and it is a better story than climbing a ladder of postcodes.
You are the reason this place is on the map.

The most important consequence: the decision the player makes forty times an hour is
*"which room pays back fastest per gold right now."* That decision only exists because
four income curves compete. It is the loop, and nothing may be allowed to starve it.

---

## 2. The five rooms

| | room | earns from | also does |
|---|---|---|---|
| 💰 | **Tavern** | food and drink to townsfolk | sets which adventurers walk in — ceiling and odds |
| 💰 | **Inn** | rooms let to travellers and merchants | — |
| 💰 | **Front Desk** | commission on completed contracts | quest slots, contract tiers, reward yield |
| 💰 | **Provisioner** | rope, rations, torches, potions | — |
| 🛏 | **Barracks** | — | houses **and drills** the adventurers |

### What changed from the old three

**The Training Room is retired as a building.** A barracks that houses its people and
drills them is one idea, not two, so its function folds in and its slot goes to a fourth
earner.

**The old Inn split in two.** It was renting rooms to travellers *and* housing your
adventurers — two businesses under one roof. The Inn is now purely a hotel; the Barracks
purely a home.

**Reward Yield moved from the Tavern to the Front Desk.** Days 8–9's structural finding
was *only the Tavern compounds*, which is why it had 90 levels against 40 and 30. A
Tavern that both multiplies quest gold and generates its own would compound twice and
bury everything else. This also **absorbs the deferred Quest Board**, whose whole design
was "more quest slots and higher contract tiers" — that is now the Front Desk's job, and
the post-MVP building list drops from two to one.

---

## 3. The economy

### 3.1 · Departments, staff, and the one piece of tension

**Three levers, three separate sources, no overlap.** This is the core of the economy and
the most important thing in this document.

| lever | comes from | means |
|---|---|---|
| **demand** | the **tier** — how big the settlement has grown | how many want in per hour |
| **capacity** | the **room level** — seats, and spend per head | how many you can hold, what they pay |
| **throughput** | **staff** | how fast a seat turns over |

```
per room:  demand   = roomBaseDemand(level) × marketSize(tier)
           capacity = seats(level) × staffSpeed
           served   = min(demand, capacity)
           revenue  = served × spendPerCustomer(level)

guild:     gross/hr = Σ each room's revenue
           wages/hr = Σ each staff member's wage
           net/hr   = max(0, gross − wages)        ← the floor, §6.1
```

Staff are a single guild-wide pool rather than assigned per room — one number, far less
fiddle — and their effect is *speed*: more staff turn a seat over faster.

**The rhythm this creates is the point.** Advancing a tier makes everything you already
own suddenly insufficient: the town arrives, the customers arrive with it, and the tavern
you were quietly satisfied with is now turning people away. A burst of shopping
motivation lands at exactly the moment the player has just been rewarded. Then it runs the
other way — you upgrade until you are serving everyone who wants in, at which point
*demand* is the ceiling and the only way forward is the next tier.

Which gives contracts the clearest job they have had in any version of this design:
**questing raises your market ceiling.** Reputation stops being an abstract gate and
becomes the mechanism by which the settlement grows around you. The two loops stop being
neighbours and become a circuit.

**Wages are ongoing**, which is a real departure — every other cost in this game is
one-time. It means the tavern panel reports a *net* figure, and that over-hiring is a
live mistake rather than merely a slow one.

### 3.2 · Quests, and what they are for

Adventurers take contracts. Contracts pay **reputation**, and reputation is the **only**
thing that advances a tier. Tiers unlock better rooms, better staff and better contracts.

The gold from a contract arrives as the **Front Desk's commission** — so questing does
not sit beside the tycoon, it flows *through* it, and upgrading the desk is how you
monetise adventuring. Reward Yield living on the desk is exactly this: your cut.

Two currencies, two jobs, no overlap. **Rooms make gold. Adventurers make reputation.**
Neither loop can be ignored, and neither can be ground in place of the other.

### 3.3 · Who walks in

Recruitment stops being a shop and becomes **who is drinking here tonight**.

Arrivals happen over time. The Tavern's quality sets the ceiling and the odds: a rough
village tavern draws Militia Recruits and the occasional Hedge Knight; a capital-grade
hall has Dragonsworn Champions at the bar. The existing `RecruitableRarity` stat survives
unchanged in meaning — it stops being a shop filter and becomes the top of a
distribution. A change of *use*, not of mechanism.

Hiring still costs gold and still needs a bed, so the Barracks keeps a real role.

**Townsfolk are the reliable income; adventurers are the event.** The hero walking in
mid-shift is the beat the whole premise is built on, and it is worth some randomness to
get it.

---

## 4. How this stays data-driven

The part that decides whether the architecture survives, and it does.

**Revenue is a `GuildStat`, not a new field on `BuildingDefinition`.** Each earning room
carries a `Revenue` effect and a `ServiceDemand` effect, exactly like every other
building effect that already exists. A single room's figure — which the tycoon UI must
show — comes from evaluating *that building's own effect*, which `BuildingEffect` already
supports. So **adding a sixth department later is one new `.asset` file and zero code**,
which is the bet, still intact under the heaviest load it will ever take.

Three stats append to `GuildStat`. Appending is save-safe; the rule is never to
*renumber*, and nothing renumbers.

```
Revenue        (8)   gold per hour at full service
ServiceDemand  (9)   service needed to run at full
StaffSlots    (10)   how many staff may be employed
```

Every existing stat keeps a home:

| stat | now lives on |
|---|---|
| `RewardYield` | Front Desk |
| `RecruitableRarity` | Tavern |
| `AdventurerPower` | Barracks |
| `HousingCapacity` | Barracks |
| `RecoverySpeed` | Barracks |
| `QuestSlots` | Front Desk (adding to the tier's base) |
| `MaxQuestTier` | Front Desk (adding to the tier's base) |
| `FailureRateReduction` | still unproduced — the post-launch Smithy |

**Service capacity is computed in App, not `GuildState`.** Capacity is staff, demand is
buildings, and `GuildState.Aggregate` reads buildings only. Teaching it about staff would
put a cross-feature reference exactly where fourteen days of discipline have kept one
out. `TavernService` in App combines `IGuildStats` with the staff roster — which is what
App has existed for since Day 4–5. **The features stay Core-only.**

**Staff become a sixth feature assembly**, `IdleGuild.Staff`, depending on Core and
nothing else.

**Offline income comes free.** Day 4–5 made the clock the single path for online and
offline time, so an accrual living in `SimulationClock` is automatically right after
eight hours away. There is no second offline formula that can drift — that decision
paying out for the fourth time.

### Saves

New fields only: the staff roster, and department accrual state. Fields are only ever
added, so **`SaveSchema.CurrentVersion` still does not move.** A pre-revision save loads
with no staff and nothing accrued, which is correct.

`Adventurer.Level` becomes vestigial when individual training is cut (§5). Per the
compatibility rule it **stays declared and unread** rather than being removed — which is
precisely the case that rule was written for.

**Day 14's played-in save must be captured before any of this lands.** It is the last
record of the one-economy game and cannot be recreated.

---

## 5. What is cut, and why

**Individual adventurer training levels.** Day 13 measured the roster's training bill at
roughly **40 million gold** — the dominant sink in the entire economy. In a game whose
focus is *improve the room to earn more per hour*, that is forty million gold pulled away
from the buildings. Every coin spent levelling an adventurer is a coin not spent making
the hall nicer, and the two sinks would starve each other by design.

Adventurer power now comes from the **Barracks level** — one building, raising everyone,
gold flowing to buildings where the focus belongs. Adventurers still get stronger; you
make them stronger by *building*, which is the game.

The cost is honest: **this retires Day 13's training ladder**, the five numbers and the
finding behind them. The method survives — model first, assert shape not number, canary
what moves — but those figures stop existing.

**Party formation stops being required.** Contracts auto-staff with the strongest free
adventurers; the picker stays as an optional override. Day 12 built both halves, so this
costs nothing and removes the least idle interaction in the game from the required path.

**Kept, deliberately:** the five rarity bands, bed capacity, retiring, and the picker as
an override. Rarity is nearly free to understand and it is the entire anime hook — a
Champion walking into your tavern is the moment the premise exists for.

---

## 6. Open decisions

### 6.1 · The wage floor — SETTLED, floored

`net = max(0, gross − wages)`. Confirmed against the genre: the dominant pattern in idle
tycoons is **no ongoing wages at all** — Adventure Capitalist, Idle Miner, Idle Office
Tycoon all make staff a one-time purchase that automates rather than costs. The games with
wages you can go bankrupt on (RollerCoaster Tycoon, Two Point Hospital, Game Dev Tycoon)
are *active* management sims where the player is present to react. The genre's own design
literature frames offline progress as the thing that guarantees players never fall behind.

So the floor is the standard, not a compromise. **Wages come out of the till, not out of
the vault** — you can have a bad hour, you cannot have a bad night's sleep. Gross and
wages are shown as separate lines so over-hiring reads as a visible squeeze rather than a
mysterious slowdown.

The argument for the floor is offline time. **An idle game whose player returns after
eight hours with less gold than they left has punished them for closing it**, and no
amount of tycoon realism is worth teaching that. With the floor, over-hiring wastes income
you could have had — real tension, real mistake, no punishment for absence.

The argument against: it makes wages toothless once income is large. Worth watching in
the model — if the floor never binds after the first hour, wages are decoration.

### 6.2 · Unlock order

Two shapes, and the model should try both:

- **By cost**, Adventure-Capitalist style — every room exists from the start and you
  simply cannot afford the later ones. Purest tycoon, and the opening is one clear
  decision after another.
- **By tier**, as the game does today — Village opens with Tavern, Barracks and Front
  Desk (the desk must be early, since reputation gates everything and only contracts pay
  it), Town adds the Inn, City the Provisioner.

Tier-gated is more legible and gives each advancement a visible reward. Cost-gated is a
smoother curve. Leaning tier-gated.

### 6.3 · How random should arrivals be?

A player waiting for a Legendary to walk in is a player not playing. In order of
preference: a guaranteed-arrival timer that resets on a rare pull; a visible "tonight's
crowd" of a few slots refreshing on a clock; or a deterministic rotation gated by Tavern
level. The model should try the first and the third, because *frustrating* is not
something the pacing numbers will show.

### 6.4 · Rarity gating — SETTLED

**Hard ceiling, with the odds varying beneath it.** A level 2–3 Tavern draws roughly 80%
Common and 20% Uncommon; the ceiling and the distribution both improve as it grows.

### 6.5 · Prestige — IN SCOPE, see §10

**Superseded.** Prestige is being designed and built now rather than deferred, because it
changes what the balance target *is* — without it you tune for "finish in ~18 hours", with
it you tune for "first reset at two to four hours, each run faster". Tuning the first and
bolting on the second means throwing the first away, and the model is being rebuilt this
week regardless. Original note follows.

**Stub the hook, build nothing.** Keep the `Renown` currency field, and keep multipliers
separable from base rates in the economy so Branch Expansion can slot in cleanly later.
Costs nothing now; avoids the 1.1 update needing an economy rewrite.

---

## 6A. Prestige, and how anything survives it

**You are summoned to another world.** Reaching a threshold in the Capital hall triggers
it: the guild, the rooms, the staff, the gold and the roster all stay behind, and you
begin again somewhere new with nothing but what is in your head. It is the genre's own
premise used as its reset loop, and it is far better than the charter's original Branch
Expansion, which was a business decision where this is a story.

**What travels is knowledge, not objects.** This is the answer to the persistence problem
and it is not a workaround — it is the isekai premise exactly. You do not carry the
distillery across worlds; you carry *knowing how to build one*. Anything permanent the
player owns is framed as a technique, a blessing or a bound familiar, all of which live in
the player rather than in the building. Renown travels for the same reason.

An idea worth keeping warm rather than building: **a scroll that retrieves one comrade**
from a world you left. Emotionally much stronger than recovering equipment, and safe
precisely because adventurers are *earned* — gating earned things behind a new world's
progress is fair in a way that gating *bought* things is not.

---

## 6B. Monetisation

### The rule that decides everything else

**Nothing bought with money makes a number go up.**

Two pillars only, and neither touches the economy:

- **Familiars — automation.** A bound spirit that minds a room while you are away:
  collecting takings, hiring from the crowd, dispatching contracts. This removes *tedium*
  rather than granting power, so a free player can do everything a payer can and simply
  has to tap. It is the canonical idle-game premium for good reason, and framing it as a
  summoned familiar rather than a hired manager explains it diegetically instead of
  bolting a business metaphor onto a fantasy game.
- **Cosmetics.** Room themes, signage, staff uniforms, a guild crest. Almost no idle game
  can honestly monetise appearance because they are spreadsheets with a skin; this one
  made visual pride the entire point of the revision, so it monetises the exact fantasy it
  sells. Never touches balance. Costs art volume, which is the scarcest resource.

Techniques — permanent bought multipliers — were considered and **dropped**. They are
conceptually the weakest of the options because they are still fundamentally buying a
number, and dropping them means the free and paying economies are identical.

Three consequences worth stating. It is the easiest possible version to balance, because
there is nothing to balance. It reviews well and survives scrutiny. And **the model needs
no monetisation pass at all**, which quietly buys back a day of the schedule.

### The currency

**Boons**, granted by your **Patron** — the entity that summoned you. The premium surface
is framed as the Patron's favour rather than a shop, which gives an IAP screen a narrative
reason to exist and explains prestige with the same stroke: the same Patron summons you
onward. The Patron stays unnamed and slightly unknowable, which is cheaper than
characterising a god and usually more effective.

Boons are earned as well as bought — tier advancement, prestige, and rewarded ads — at a
rate that lets a patient free player reach roughly a third of the cosmetic and familiar
catalogue.

### Rewarded ads, all four opt-in

2× offline earnings on return; a timed 2× on room income; instant-complete a contract; a
daily Boon grant. Rewarded only at launch, per §05 of the charter. No interstitials.

### The boundary that keeps this clean

**Boons never touch the crowd randomness.** Adventurer arrivals are a random-reward
mechanic, which is entirely fine while nothing is sold into it. The moment Boons can
reroll or summon from the crowd it becomes a loot box, bringing odds-disclosure
requirements, regulatory attention in several jurisdictions, and a harder age-rating and
review conversation. Everything sold is **deterministic**: a familiar you choose, a theme
you pick, ads you remove. It costs nothing in revenue and keeps the submission clean.

**IAP products get their own App Store review pass**, so they want configuring early
rather than in the final week.


## 6C. What the model says so far

`Docs/tools/tycoon_model.py` models this design; `tuner.py` searches its parameter space
against explicit targets. Hand-tuning did not converge — twenty coupled dials, and Village
went 1h48m → 1h32m → 3h50m → 3h16m → 4h26m → 8h19m across six passes while every
individual change was correct in isolation. The tuner reached a better configuration in a
few hundred automated evaluations than six manual passes managed.

**Where it stands** (`tuned_params.json`):

| | modelled | target |
|---|---|---|
| Village → Town | 0h30m | 5m |
| Town → City | 2h18m | 1h30m |
| City → Capital | 10h53m | 6h |
| everything maxed | 11h59m | 20h |
| **rooms' share of lifetime income** | **68%** | **70%** ✅ |
| **purchase gap, 90th percentile** | **6 min** | **≤10 min** ✅ |
| purchase decisions | 213 | 260 |

The two hardest targets are met. The error is all in the *shape of the time curve*.

### Findings the model produced, which are worth more than the numbers

Each of these would have shipped as a bug:

1. **A payback-ranked player never buys a support room.** The Barracks earns nothing
   directly, so ranking by gold-per-hour made it invisible and the entire adventurer half
   of the game went with it. Also a **UI requirement**: if the player cannot see that the
   Barracks makes money, they will not buy it either.
2. **Cold-start trap.** With no staff, rooms earn nothing, so a room upgrade has zero
   marginal value *and* the first staff member has almost none. One run hired no staff
   across 150 hours. Solved with a base service on the tier — the same shape as Day 4–5's
   opening deadlock, solved in data rather than in code.
3. **Staff slots are a one-way ratchet.** Fill them cheaply and you can never upgrade.
   The Days 10–11 bed problem, caught before it shipped. **Staff need a dismiss action
   designed in**, not retrofitted the way adventurers needed one on Day 12.
4. **The staff ladder had Day 13's exact defect** — higher tiers cost more per unit
   delivered, so the model hired 96 Potboys and never upgraded once.
5. **Flat wages are decoration** against geometric revenue: 0.03% of gross at endgame.
   Wages have to scale with what a customer is worth.
6. **Static contract rewards become rounding error** — Dragon's Roost paying 26,000 in a
   guild earning 15 million an hour.
7. **The player never saves for a tier gate.** Reputation cleared Village in 20 minutes;
   the Front Desk the gate also required went unbuilt for three hours while nine Potboys
   were hired instead. **The tier panel must show what is still missing**, or a real
   player runs the same greedy policy.
8. **But blunt reserving is worse than none** — hoarding starves the compounding that
   would have paid for the gate, and pushed Capital from 16 hours to 86. The rule has to
   be economic: buy anything that pays for itself sooner than you could have saved.
9. **Demand, seat capacity and staff throughput must stay commensurate.** If any one is
   far from the others it stops binding, and the upgrade that controls it becomes useless
   rather than merely expensive.
10. **Every new room cannibalised the existing ones.** With staff shared proportionally,
    opening the Provisioner diluted the staff serving the Tavern and Inn — 137k/hr of
    damage to gain 4k. Its payback was *negative*, so the model sat on **276 million gold**
    and never bought a 9,000-gold room. Fixed by serving the most valuable custom first,
    which is what a real landlord does and means opening a room can never make you poorer.
11. **The opening hinged on a one-gold coin flip.** An adventurer cost 40, a Potboy 39.
    Gold crossed 39 first every time, so the guild hired staff it did not need and never
    sent anybody on a contract — no reputation, no tier, forever.

### What is still wrong, and it is not the tier boundary

The first-session trace: tavern and front desk built instantly, an adventurer in the crowd
immediately — then **nothing at all until the first staff hire at 21 minutes.**

That dead stretch is the real problem. "Village is 30 minutes" would be fine if the first
five were busy. So the next tuning pass should score **first-beat timings** — first staff,
first contract, first upgrade — rather than tier boundaries. That is a change to the loss
function, not to the design.

### One calibration point, from the only real measurement this project has

Day 14's played-in save records **17.6 minutes** of real play to reach Town, against the
old model's prediction of **8**. A 2.2× gap, and almost certainly not a bug: the model buys
the instant it can afford something, while a person reads, thinks, taps around and misses
windows. **Modelled minutes are not lived minutes.** Treat it as a single noisy data point
— that session included inspection as well as play — and re-measure on the next
playthrough before trusting it.


## 7. What survives

**Untouched:** the assembly graph and the Core-only rule; the save format and its
compatibility rule; the whole test suite's *shape* assertions; `AssetValidation`; the UI
patterns and `GuildContext`; `ScalingCurve`, `BuildingDefinition`, `BuildingEffect` and
`GuildState.Aggregate` — the entire data-driven engine, which turns out to model an
income stream as readily as a bed count.

**Must be re-derived:** every pacing figure from Days 8 through 13. All building trees,
tier gates, reputation thresholds, and Day 13's result. Not because any of them was
wrong, but because they were derived against a one-income-stream game that no longer
exists.

That is a re-run of a method that has worked four times, not a rewrite of its
conclusions. **`guild_model.py` is rebuilt before a line of game code is written** —
Days 8–9 and Day 13 both found structural failures no amount of playing would have
surfaced, and this change is larger than either.

---

## 8. Revised roadmap

Four weeks becomes about six. Submission moves from Day 26 to **Day 36, buffer through
Day 40.**

| days | work |
|---|---|
| **15–16** | This document, and `guild_model.py` rebuilt against it. Design signed off against numbers rather than vibes. |
| **17–19** | `IdleGuild.Staff`, department revenue, the throttle, clock accrual, save fields, tests. |
| **20** | Recruitment reworked from shop to arrivals. |
| **21** | The five rooms as assets: Barracks and Provisioner authored, effects moved, Training Room retired, tier gates re-derived. |
| **22** | Balance pass across two currencies and five curves. |
| **23–24** | Tycoon UI — room panels with gold per hour, staff, the crowd. |
| **25–26** | Art: hall frontage states, five room states, portraits, icons. |
| **27** | Art integration. |
| **28–29** | Ads and IAP. |
| **30** | Hardening. |
| **31** | Balance pass with monetisation. |
| **32–33** | Bug bash on device. |
| **34** | App Store Connect. |
| **35** | TestFlight. |
| **36** | **Submit.** |
| **37–40** | Buffer. |

### The cut list, decided now

| if | cut |
|---|---|
| the staff subsystem overruns | staff become a flat capacity number bought like a building level — no roster, no wages, no second economy of people |
| arrivals prove frustrating | fall back to §6.3's deterministic rotation, or to today's shop |
| art overruns | one hall frontage with lighting and palette variants rather than distinct states per room |
| behind by Day 30 | three rooms instead of five — Tavern, Barracks, Front Desk. The architecture makes this a data decision. |
| behind by Day 33 | three tiers instead of four, the original §05 cut, still right |

**Not cut under any circumstance:** the department income streams and the throttle. They
are the reason for the revision, and a version of this game without them is the game that
already exists.

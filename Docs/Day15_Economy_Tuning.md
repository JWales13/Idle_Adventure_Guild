# Day 15 — finishing the tycoon economy

Model and tuner only. **No `.asset` and no game `.cs` changed for the economy**; the four
UI files and one PNG this day also touched are the icon wire in §6, which is display code
and independent of everything else here.

The day was handed one instruction: *score first-beat timings in `tuner.py`'s loss
function rather than tier boundaries, and give the tuner a wider scatter or tighter bounds
on the dials controlling the opening.* Both were right. Neither was reachable, because
three things underneath them were wrong, and the first of them meant the tuner had been
optimising a measurement artefact for its entire existence.

---

## 1. The ruler changed length

`tuner.evaluate()` scored one run at `step=60`. `tycoon_model.report()` runs at `step=20`.
Nobody had ever run the same configuration at both.

`tuned_params.json`, the checked-in best point, at six integration steps:

| step | Town | City | Capital | maxed | rooms' share |
|---|---|---|---|---|---|
| 5 | 0h59m | 2h12m | 11h49m | 12h22m | **82%** |
| 10 | 1h00m | 2h15m | 11h57m | 12h26m | 83% |
| 20 | 1h01m | 2h21m | 12h11m | 12h39m | 84% |
| 30 | 0h28m | 2h07m | **never** | 13h21m | 98% |
| 60 | 0h30m | 2h18m | 10h53m | 11h59m | **68%** |
| 120 | 0h30m | 2h16m | 10h32m | 11h44m | 70% |

The bottom row is what §6C of `Vision_Revision.md` reports as the state of the design, and
the two figures it calls *the two hardest targets, met* — **rooms at 68% against a 70%
target**, and a **6-minute** 90th-percentile purchase gap — are both properties of the
step size rather than of the game. At any step a player would recognise as fine-grained
the rooms' share is 82–84%, which overshoots the target by as much as the reported figure
undershoots it. At step 30 the Capital is never reached at all.

**The cause is that a contract's duration rounded up to a whole step.** Rat Cellar is
31.5 modelled seconds and was taking a 60-second tick — a 1.9× throughput penalty on the
entire adventurer economy, which vanished as the step got finer. Arrivals quantised the
same way: a 77-second arrival gap landed every 120 seconds at a 60-second step.

Fixed by carrying the remainder in both places. `advanceOrders` now consumes the step
continuously and resolves as many cycles as fit in it, and the arrival clock adds its gap
rather than being reset to it.

**And the rest period is now paid.** `rest_of` was priced into every ranking the model
made — `questGoldPerHour`, `bestWork` — and never actually served in the simulation, so
the model charged the player for a recovery benefit it then handed out for free, and the
Barracks' recovery stat was inert. Contracts are now a run *and* a rest, which roughly
halves Village contract throughput and gives that stat something to do.

A second defect surfaced while looking: **a standing order's quest choice was frozen when
the order was created.** `syncQuests` picked the best available contract at creation and
nothing ever revisited it, so an order created in Village was still running Rat Cellar in
the Capital. Day 12 established that `QuestAssignment` holds its party for the life of a
*run*, not of the assignment; `bestWork` is the model catching up to its own game.

### What that leaves

| step | Town | City | Capital | maxed | rooms' share |
|---|---|---|---|---|---|
| 1 | 0h29m | 2h16m | 7h02m | 10h07m | 53% |
| 2 | 0h29m | 2h16m | 7h02m | 10h56m | 68% |
| 5 | 0h29m | 2h16m | 11h50m | 12h39m | 59% |
| 10 | 0h29m | 2h17m | 6h58m | 9h47m | 47% |
| 20 | 0h29m | 2h17m | 7h33m | 10h35m | 52% |
| 60 | 0h30m | 2h18m | 6h55m | 9h18m | 42% |

Town and City are now stable to the minute. Capital, maxed and the rooms' share still
move, and **they do not converge as the step shrinks** — which means it is not a
discretisation error. It is a greedy policy on a compounding economy: a small difference
in when gold crosses a threshold flips which purchase comes first, and the two paths
diverge from there. That is a real property of the design and not a bug to remove.

It does, however, mean **the loss cannot be a single run**, because a single run of a
chaotic system measures the run. `evaluate()` now scores the median across steps
(10, 30, 60) and adds a term for the spread between them, so the search prefers
configurations whose pacing does not sit on a knife-edge. That is a property worth having
in the shipped game anyway: a real player is a far noisier clock than any of these.

---

## 2. Finding #11 was live the whole time, and the reason is a unit

§6C's eleventh finding — *the opening hinged on a one-gold coin flip*, an adventurer at 40
gold against a Potboy at 39 — is recorded as fixed. It was not. It was hidden.

At `step=60` gold crossed both prices inside a single tick and the `needBody` override,
which sits earlier in `purchase()`, took the adventurer by luck of ordering. At `step=5`
gold crossed 39.44 first, the Potboy was bought, and Village went from 29 minutes to 59.
That single flip was most of the step-dependence in the Village time.

The reserve that was supposed to prevent it has two failures stacked on each other.

**The first is scope.** `reserveDeadline` was computed only inside `if unmet:` — the
branch for an unmet *room* requirement. Village's gate at `gate_scale = 0.5` is
`tavern 2 / front_desk 1`, and starting gold clears both in the first instant, so `unmet`
is empty from t=0 onward. The deadline stayed at infinity and the reserve had nothing
behind it. It is now derived from whatever the reserve turned out to be.

**The second is units, and it is the more interesting one.** `payback()`'s docstring says
*"Seconds of net income to earn back this purchase"*. It returned `cost / gold-per-hour`,
which is **hours**. The ranking never noticed, because a ranking only needs the order and
gets the same order in either unit. The one place the units could have been caught is the
single comparison against `reserveDeadline`, which is in seconds — so the guard read
`0.755 hours <= 21.8 seconds` as true and let every candidate walk through every reserve
the model ever held.

Worth setting beside Day 13's finding, because it is the same shape: **a ratio authored in
one place and paid for in another will not be checked by anybody looking at either.** Here
it is a quantity *produced* in one unit and *compared* in another, where the producer's own
docstring named the right one and nothing had ever read it against the consumer.

With both closed, Town is 0h29m at every step from 1 to 60.

---

## 3. The dead twenty-two minutes was a subtraction

The trace, at the checked-in parameters:

```
  0m00s  * Tavern -> L1
  0m00s  * Tavern -> L2
  0m00s  * Front Desk -> L1
  0m05s  . Militia Recruit joins the crowd
  1m20s  . Militia Recruit joins the crowd
     ... sixteen more arrivals, nothing else ...
 21m35s  * hire Potboy               <-- 22 minutes of nothing
```

It is not a curve. It is arithmetic:

| | |
|---|---|
| starting gold | 150.00 |
| Tavern L1 + L2, Front Desk L1 | −143.85 |
| **left** | **6.15** |
| next purchase (Militia Recruit) | 40.00 |
| income at Tavern 2 with no staff | 92.64/hr = **1.54 gold a minute** |
| | **21.9 minutes** |

Which is exactly the gap in the trace, to within a tick.

Two things follow. The first is that **starting gold buys the entire tutorial in the first
instant** — three purchases before the player has made a decision — and then buys nothing
for twenty-two minutes. The second is why no amount of tuning had ever moved it.

---

## 4. The tuner was not in a basin. It was searching the wrong space.

Every number in that table was hardcoded in `content()` and **absent from `SPEC`**:
`startingGold`, the Tavern's and Front Desk's cost bases, and the adventurer hire ladder.
The opening was not a dial. Widening the scatter or tightening the bounds could not have
reached it, because the search space did not contain the problem.

Five dials added:

| dial | range | what it moves |
|---|---|---|
| `start_gold` | 60 – 1500 | how much of the opening is bought for you |
| `open_cost` | 0.25 – 3.0 | cost base of the two rooms Village opens with |
| `late_cost` | 0.2 – 4.0 | cost base of Barracks, Inn, Provisioner |
| `hire_base` | 0.25 – 4.0 | the adventurer hire ladder |
| `rep_village` | 4 – 400 | what ends the tutorial |

One smaller change in the same spirit: the coordinate-refinement pass now **shuffles the
dial order each sweep**. A fixed order makes the dials at the top of `SPEC` systematically
likelier to claim an improvement that several of them could have made, which is one way a
search sits in a basin that is really an artefact of the iteration order.

---

## 5. What the loss scores now

The instruction was to score first beats rather than tier boundaries, and the model now
records them: `w.beats` holds the first of each kind, `w.pulse` holds every
player-visible moment.

**Arrivals are deliberately not pulses.** Watching an adventurer you cannot afford walk in
is the *absence* the score exists to catch — the dead twenty-two minutes had seventeen
arrivals in it. Counting them would have made that stretch look like the busiest part of
the game.

| term | target | weight | note |
|---|---|---|---|
| `beat2` | 60s | 4.0 | the first thing that happens *after* the opening burst |
| `adventurer` | 150s | 4.0 | somebody you can afford walks in |
| `contract` | 210s | 4.0 | and goes out on a job |
| `staff` | 420s | 2.5 | the first hire that is not an adventurer |
| `open_gap` | ≤120s | 5.0 | longest silence in the first twenty minutes |
| `open_burst` | ≤2 | 6.0 | purchases at t=0 |
| `town` | ≤30m | 1.0 | **a bound now, not a target** |
| `city` | 90m | 0.75 | |
| `capital` | 6h | 1.5 | |
| `maxed` | 20h | 1.5 | |
| `rooms_share` | 0.70 | 60.0 | unchanged; the design requirement |
| `gap_p90` | ≤10m | 1.5 | unchanged |
| `decisions` | ≥260 | 1.0 | unchanged |
| `spread` | 0 | 3.0 | how far the three step sizes disagreed |

Three choices in there are worth stating rather than leaving in the code.

**The beat penalties are asymmetric** — late costs four times what early does. A beat
arriving sooner than its target is a busy opening, which is the thing being asked for.

**`town` stopped being a target.** The old loss weighted it at **3.0** against a
**five-minute** target, making it the single largest term in the function — and it was
pushing hard on the one number that had never been the complaint. Village running thirty
modelled minutes is fine. Village running thirty minutes with twenty-two of them empty is
not, and a tier time cannot tell those apart. It is now a bound: no penalty until 30
minutes, then a gentle one.

**`open_burst` is weighted at 6.0**, above every beat, because it is the failure the beat
timings are least able to see. Raise starting gold and every beat lands gloriously early —
which the asymmetry above deliberately does not punish — while the player has been handed
a finished tutorial and no first decision. It is the only term that measures *the opening
is a sequence of distinct moments rather than a lump*, which is the actual requirement.

### And the search had to get cheaper to afford any of this

The robust score runs three simulations, and the slowest configurations are exactly the
ones that never finish and therefore simulate the whole horizon at the finest step. The
first run of the new loss managed **twenty-four evaluations in five minutes**, which is
not a search. Three changes, none of which touch what is being measured:

- **The scatter phase scores on `step=60` alone.** Its job is to find a basin, and a basin
  does not need three decimal places.
- **Refinement screens cheaply first** and pays for the robust score only on candidates
  within reach of the incumbent — so nothing is ever *accepted* on the cheap number.
- **A run that has not reached the Capital by three times its target stops.** It is
  already scoring terribly and does not get to spend the rest of the horizon proving it.

Together: 24 evaluations per five minutes → **66 per ninety seconds**, about 9× more search
for the same wall clock.

---

## 6. One room icon, end to end

The Ledger has carried this since Day 14: *Day 17 is carrying every line of display code in
the project, and the roadmap hides it.* `BuildingDefinition._icon` and
`AdventurerDefinition._portrait` were the only two sprite fields in the data model, neither
had ever been read, no view rendered an image, and `Ui.cs` had no image constructor. The
mitigation was to move about an hour of Day 17 into Day 15 and wire one icon end to end
before generating twenty-four assets against a mechanism nobody had observed working.

Done, in four files and one PNG:

- **`Ui.Icon(Sprite, …)`** — the project's first line of display code. The sprite goes on
  as a `background-image` rather than into an `Image` control, so size, radius and tint stay
  in USS with every other visual decision, and a room with no art yet still lays out.
- **`BuildingCard`** reads `building.Icon`. Icon and title are wrapped in a
  `card__identity` row, because the header is `justify-content: space-between` and a third
  direct child would have spread all three evenly and moved the title away from the thing
  it names.
- **`Tokens.uss`** gains `--size-icon-sm/md/lg`, since Tokens is the only file in the
  project allowed to name a measurement.
- **`GuildTheme.uss`** gains the `.icon` block, which is the whole per-content display
  mechanism in one place.
- **`Art/Rooms/room_tavern.png`** — a 256×256 placeholder drawn to §3 and §5 of
  `Day15_Art_Brief.md`. A placeholder on purpose: the point is to watch the path work
  before committing the real art to it.

**A missing sprite renders as a visible empty slot** (`icon--missing`) rather than
collapsing the element. Collapsing is tidier and is the wrong trade — an icon that is
absent and an icon that is zero-sized look identical on screen, and that is how
twenty-three assets ship with one of them missing. This project has now learned three times
that a failure whose mode is silence is not detectable; `AssetValidation`, Day 13's
canaries, and now this.

### What the wire actually found, twice

**One: the project's default texture import is Sprite Mode = Multiple.** Unity's
auto-slicer cut a single 256x256 tankard into `room_tavern_0` and `room_tavern_1`, because
the foam is disconnected from the body, and `_icon` had no whole-image `Sprite` to point
at. Section 5 of the brief already specifies Single; nobody could have known it was not
the default, because **no texture had ever been imported into this project**. On Day 17,
with twenty-four assets landing at once, this would have hit every icon carrying a
detached element -- a glint, a spark, a floating rune, most of a five-portrait ladder
whose whole job is to get more ornate as it climbs -- and each would have arrived silently
in pieces. Fixed in the `.meta`: `spriteMode: 1`, the slice table cleared, `maxTextureSize`
2048 -> 512 per the brief.

**Two, and it is the larger finding of the entire day: the interface had never been drawn.**

`GuildScreenController` calls `GetComponent<UIDocument>()`. The `UI` object carries two
panel components -- a `PanelRenderer` holding `GuildPanelSettings`, and a `UIDocument`
whose Panel Settings field is empty -- and it gets the empty one.

**A UIDocument with no Panel Settings still returns a perfectly good `rootVisualElement`.**
It is simply an orphan: attached to no panel, drawn by nothing. So the controller's own
guard (`if (_root == null)`) passes, the entire screen builds, every view constructs, the
tick runs, and no exception is ever thrown. The tree exists in memory and nothing renders
it. A blank Game view then looks exactly like a camera with nothing in front of it.

That was the state from Day 7 to Day 15. The game was played through the debug console,
which is why nobody noticed.

**This forces a correction to the Ledger rather than an addition.** Section 06 records the
Day 7 UI pass as *"run and passes"*, including its step 6 Week 1 checkpoint. Section 3 of
`Docs/Tests.md` lists Day 12's four manual checks as outstanding-but-doable --
`button--destructive` resolving to the negative colour, a sixteen-row party picker fitting
the phone, the selected row being unambiguous at a glance, the retire confirmation reading
as informative rather than scolding. **None of those can ever have been performed.** Day
14's recall bug is described in UI terms too, and now reads as derived from the code and
the console rather than observed on screen.

Two checks in `GuildScreenController` make it impossible to be silent again. An immediate
one on `_document.panelSettings == null`, placed *before* the root check because the
missing asset is the cause and the null root is only one of its symptoms; and a deferred
one, a frame later, on `_root.panel == null`, for the cases a missing asset does not cover
-- a second panel component claiming the settings, a disabled renderer. The deferral is
deliberate and is the Days 10-11 `OnValidate` lesson word for word: attachment happens
across `OnEnable`, and **a check that cannot tell a half-loaded object from a half-filled
one is not a check.**

The general shape, which this project has now met four times in four different costumes:
**a failure whose only symptom is the absence of something is not detectable, and will be
found by accident or not at all.** `AssetValidation` crying wolf, Day 13's canaries that
watched no training cost, this morning's `--checks` block looking for a curve no room has,
and now fifteen days of an interface nobody had seen.

**It is also the first binary ever committed to this repo**, so it is the first real test
of the Git LFS setup from Day 14. `git lfs install` has to have been run before the commit:
git treats an undefined filter as a **silent** no-op, so a commit made without it looks
completely successful and puts the PNG into history whole.

Outstanding: Unity must import the PNG to generate `room_tavern.png.meta`, after which the
sprite's GUID goes straight into `Building_Tavern.asset`'s YAML rather than being dragged
in through the Inspector.

---

## 7. The bound that was enforced on one path and not the other

`gate_scale`'s floor went from 0.5 to 0.9 partway through the day, because the search had
chosen 0.5 in **every winner across nine seeds** -- and at 0.5 the Village gate is
`tavern 2 / front_desk 1`, which starting gold clears before the player has done anything.
A gate that is already satisfied cannot cost the loss anything, so the tuner was quietly
deleting the mechanism §6C's finding #7 exists to serve.

Raising the floor did not work, and the reason is worth more than the fix. `search()`
clamps every *perturbation* into the bounds and leaves the *incumbent* alone, and the run
that followed resumed from a saved point that already held 0.5. So the out-of-bounds value
survived the entire search, through two more rounds, and was promoted. **A constraint
enforced on one path and not the other is not enforced** -- the same shape as §2's units
bug, one level up: the bound was checked where new values are generated and not where old
ones enter. `fill()` now clamps on resume.

Clamping it honestly costs a great deal, and that is the finding:

| | gate halved | gate real |
|---|---|---|
| worst silence | 3.0 min | 16.5 min |
| purchases at t=0 | 3 | 8 |
| Village → Town | 17 min | 48 min |
| loss | 11.44 | **273** |

**Every good number produced before this point was partly bought by a tier gate the player
never had to reach for.** The configuration below is the re-tuned one, with a gate that is
real -- `gate_scale` landed at **1.21**, tighter than the authored values rather than
looser.

---

## 8. Where it landed

`tuned_params.json`, at five integration steps:

| step | Town | City | Capital | maxed | rooms' share | gap p90 | decisions |
|---|---|---|---|---|---|---|---|
| 5 | 0h22m | 2h21m | 5h19m | 6h53m | 69% | 4 min | 289 |
| 10 | 0h23m | 2h25m | 5h20m | 6h54m | 68% | 4 min | 273 |
| 20 | 0h23m | 2h23m | 5h19m | 6h53m | 69% | 4 min | 242 |
| 30 | 0h23m | 2h25m | 5h23m | 6h49m | 65% | 4 min | 223 |
| 60 | 0h24m | 2h28m | 5h29m | 6h56m | 65% | 5 min | 185 |

**Compare that table to §1's.** Every figure now holds to within a few percent across a
twelve-fold change in the integration step; `spread` is 0.06 against 0.26 this morning.
That, rather than any single number in it, is the day's real output — the model now means
the same thing twice.

The opening, which was the day's actual brief:

```
  0m00s  * Tavern -> L1          6m30s  * Tavern -> L3
  0m00s  * Tavern -> L2         10m40s  * Tavern -> L4
  1m30s  * hire Militia Recruit  12m40s  * hire Potboy
  2m20s  * Rat Cellar pays 2g    17m30s  * Front Desk -> L1
```

Seven decisions in twenty minutes against **nought**, and the Front Desk arriving at
17m30s is the player saving for the gate — the behaviour finding #7 said neither the model
nor a real player would ever produce. Worst silence 5.0 minutes against 22.

| | before | after | target |
|---|---|---|---|
| worst opening silence | 22 min | **5.0 min** | ≤2 min |
| first contract | 8 min | **2 min** | 3.5 min |
| purchases at t=0 | 8 | **3** | ≤2 |
| Village → Town | 25 min | **23 min** | 20–30 ✅ |
| City → Capital | 2h38m | **5h23m** | 6h |
| rooms' share | 49% | **65–69%** | 70% |
| purchase gap p90 | 4 min | **4 min** ✅ | ≤10 min |
| step-to-step spread | 0.26 | **0.06** | 0 |

---

## 9. What is not fixed, and is written down instead

- **`questGoldPerHour` advances its party pointer by the smallest party size across all
  available contracts**, regardless of which contract it just costed. It is an estimate
  whose only job is to make the Barracks visible to a payback ranking, and it does that,
  but it is not the number it looks like.
- **The late game is chaotic**, per §1. The median-of-three-steps score manages it rather
  than removing it, and the honest reading of any Capital or maxed figure from here on is
  ±30%.
- **`guild_model.py` still describes the game that exists** and stays until the revision
  ships. Two models is confusing for exactly as long as two games exist.
- **The whole game is 6h54m of content against a 20-hour target.** Stable across every
  step, so it is not noise. It is a curve-length question and belongs to Day 22's balance
  pass rather than to a tuner that has already been asked for five things at once.
- **Tapping is 87% of room income across the first thirty modelled minutes.** With a real
  tier gate the guild is capital-starved early, so unserved demand is enormous and the
  thumb carries the opening. That is defensible — it is exactly the stretch where an idle
  game wants the player present — but 87% is high enough to be a design decision rather
  than a side effect, and it makes the "collect the takings" familiar very valuable very
  early. Worth deciding deliberately before §6B's monetisation goes in on Day 28.
- **The worst opening silence is 5 minutes against a 2-minute target**, and every seed
  across five rounds landed between 4.7 and 6.7. That reads as a frontier rather than a
  search failure: the guild cannot generate enough gold in twenty minutes to support a
  purchase every two. Roughly eleven lived minutes at Day 14's 2.2×, with a tap mechanic
  filling it — possibly fine, and a question for the first real playtest rather than for
  the model.

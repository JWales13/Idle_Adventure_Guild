# The world view — the guild as a place, not a menu

Written Day 16, after a playtest of the menu build. **Supersedes the presentation half of
`Day15_Art_Brief.md` entirely** and redirects `Art_Generation_Guide.md` §3's asset list.
The economy, the assemblies, the saves and the tests are untouched.

Design only. **No code and no asset has changed for it.** Same discipline as Day 14: this
exists to be argued with, then grey-boxed, and only then drawn.

---

## 1. The game, in one paragraph

> **The screen is the guild hall.** It opens on the hall as it currently stands, seen
> from a high three-quarter angle, and the player drags a finger to pan across it.
> Townsfolk walk in off the street, take a seat in the Tavern or check into the Inn, and
> a ring closes over their head counting down their stay. Staff move between rooms
> serving them. Adventurers arrive in the tavern crowd, live in the Barracks, walk out of
> the front door on contracts and come back. Rooms you have not built yet are dark, and
> the hall grows new wings as you open them. There is no home screen: **the home screen
> is the building.**

Reference for how the view and its systems behave — not for its art — is *Idle Hotel
Empire Tycoon*.

---

## 2. What this changes, and what it does not

**Untouched, which is most of the project:** `IdleGuild.Core` and all six feature
assemblies, `IdleGuild.App` and every service in it, the save format, the whole test
suite, and every tuned number in `tuned_params.json`. Sixteen days of work survives
because the simulation never knew what was drawing it — which is the assembly discipline
paying out for the fifth time.

**Demoted:** `IdleGuild.UI` stops being *the game* and becomes *the chrome around it*.
The treasury bar, the mailbox, the toasts, the confirm overlay and the room panels all
survive as overlays. `HallView` and its `BuildingCard` grid are replaced by the hall
itself. The tab bar's future is §7.

**Added:** a seventh assembly, **`IdleGuild.World`**, sitting beside `IdleGuild.UI` and
above `App`. It may reference App and the features; nothing references it. The features
stay Core-only, and the Quest Board / Armory bet is untouched.

```
Core  <-  Economy Adventurers Quests Guild Staff  <-  App  <-  UI
                                                          <-  World
                                                              <- Tests.Editor
```

---

## 3. The finding this design rests on: the economy is already seat-shaped

Nobody designed it this way and it is true anyway.

**`MAX_TURNS_PER_HOUR` tuned to 40, which means a seat is occupied for exactly 90
seconds — and that never changes at any tier.** Growth comes from more seats and more
spend per head, never from faster turnover. So **the animation never has to speed up**,
at any point in the game. That is a hard requirement for this kind of view and it is
already satisfied.

**Seats are the binding constraint at every tier**, so the room is always turning people
away and **there is always a queue outside the door** — permanent visual content, free:

| | seats | could serve | want in | binding |
|---|---|---|---|---|
| Village open | 4 | 160/hr | 400/hr | seats |
| Town gate | 20 | 794/hr | 2,436/hr | seats |
| City gate | 39 | 1,548/hr | 14,834/hr | seats |
| maxed | 60 | 2,381/hr | 90,333/hr | seats |

And the three levers of §3.1 stop being spreadsheet columns:

| lever | what the player sees |
|---|---|
| **demand** (tier) | how many people approach the building |
| **capacity** (room level) | how many seats there are |
| **throughput** (staff) | how fast they get seated |

### The opening will look empty, and that is correct

At Village with no staff the guildmaster serves **5.6 customers an hour — one every 10.7
minutes** — against four seats and 154/hr of unserved demand. The view will show four
mostly-empty seats and a crowd outside that cannot get in.

**Do not fake activity to cover this.** It is the throttle, visible for the first time,
and it is the game telling the player to hire. It is also why tapping is 87% of early
income: in this view the tap has a physical meaning — *you seat someone yourself* —
which is the clearest that mechanic has ever been.

---

## 4. The rule: the view DEPICTS, it does not CAUSE

**Settled.** A customer sprite appears because the economy served one. A customer leaving
does not grant gold; gold accrues in `SimulationClock` exactly as it does today.

This is not a compromise, for three reasons. It puts none of sixteen days of tuning at
risk. Offline stays correct with no second formula — the thing the Day 4-5 clock decision
exists to prevent, and the reason there is no offline drift today. And it is **exact
rather than decorative**, because the economy already speaks in seats and dwell times.

### The derivation, precisely

Everything the view draws comes from `TradeService.CollectRooms()`:

| drawn | derived from |
|---|---|
| seats in a room | `RoomTrade.Room` → `ServiceSeats` stat, floored |
| a customer is seated every… | `3600 / ServedPerHour` seconds |
| how long they sit | `3600 / CustomerTurnsPerHour` = **90 s** |
| the coin that pops when they leave | `SpendPerCustomer` |
| the crowd outside | density scaled from `UnservedPerHour`, sprite count capped |
| which room staff walk to | the allocation order — highest `SpendPerCustomer` first |
| adventurers in the Barracks / out of the door | `AdventurerActivity` — Idle / OnQuest / Resting |

The coin popup is worth noting: one customer is spawned per `3600/served` seconds and
each represents exactly `SpendPerCustomer`, **so the popups sum to the accrued total
over time**. The depiction is arithmetically exact, not an approximation.

**Returning from offline** seeds seat occupancy from the current rate rather than
replaying. Nothing is owed; the gold was already banked by the clock.

---

## 5. The hall

- **High-angle three-quarter**, tilted enough to see faces and silhouettes. Chosen over
  isometric because it is more forgiving of AI-generated art — looser perspective hides
  more — and over top-down because you would only ever see the tops of heads, and the
  rarity ladder three days of balance work went into making legible would be invisible.
- **Free 2D pan**, drag to move, with bounds. No snapping.
- **The hall physically expands.** New wings appear as rooms unlock. Camera bounds grow
  with it, so the plan must be composed so that every growth step still reads as one
  building.
- **A bit of outside is visible at the entrance** — the street, people approaching and
  leaving. This is where unserved demand lives, and it is the most informative square
  metre on the screen.

### Rooms show their level by redecoration at thresholds

Chosen over "furniture simply multiplies". Both happen — seats do appear as
`ServiceSeats` grows, because seats are derived from the stat and cannot drift from it —
but the room's *dressing* also changes at a handful of thresholds per room: rough boards
to polished hall to grand hall.

This is the expensive choice and it was taken deliberately. It is also what makes
cosmetics sellable later: §6B monetises room themes, and a room with no visual states has
nothing to re-theme.

**Unbuilt rooms are dark and shuttered where the wing will be**, which incidentally
satisfies §6C finding #7 — *the tier panel must show what the gate is still missing* —
diegetically rather than as a list.

---

## 6. Who moves

**Everyone.**

| agent | driven by | beats |
|---|---|---|
| **Townsfolk** | `ServedPerHour` per room | approach, queue, seat, 90 s ring, leave, coin |
| **Staff** | `StaffRoster` count; target = allocation order | walk between rooms, serve a seat |
| **Adventurers** | `AdventurerRoster` + `AdventurerActivity` | arrive in the crowd, hire, live in Barracks, leave on contract, return |

The adventurer thread is the isekai premise's best beat and §3.3 already says so — *"the
hero walking in mid-shift is the beat the whole premise is built on"*. It needs no new
state: `Idle`, `OnQuest` and `Resting` already exist and already mean exactly the three
things the view needs to draw.

**Staff walking to the highest-spend room renders the priority allocation rule** — the
one that stops a new room cannibalising the others, and the model's worst deadlock. The
player will be able to see it happen.

---

## 7. What happens to the existing screens

**Recommended, not yet settled:** the room panel becomes diegetic — tap a room, its panel
opens over the hall with upgrade cost, stats, and what it is currently earning. Contracts
open from the **Front Desk**, the roster from the **Barracks**, staff from wherever they
are hired. The tab bar then has nothing left to do and goes away.

Surviving as persistent chrome: `TreasuryBar` (including the mailbox), `ToastBar`,
`ConfirmOverlay`, `BuildingDetailOverlay` (re-homed), `PartyOverlay`.

`GuildContext` and its rule — *views read state and call services, they never compute
one* — applies to the world view unchanged, and matters more there, because a view that
knows how to compute a dwell time is a view that can disagree with the economy.

---

## 8. The art, and the one unresolved decision

**Deferred by agreement: whether frames are authored individually or baked out of a rig.**
It gets decided by taking one character end to end and comparing, not in the abstract.

The arithmetic that makes it worth deciding carefully:

| | |
|---|---|
| distinct characters | ~6 townsfolk + 4 staff + 5 adventurers = **15** |
| × facings | **4** — up, down, left, right (settled) |
| × frames per walk cycle | 6–8 |
| **walk frames alone** | **360–480** |
| plus idle, sit-down, sit-idle, stand-up | ×1.5 again |

**The trap:** image generators cannot produce consistent frames of the same character.
Ask for "frame 2 of this character walking" and you get a different person. This is not a
prompting problem.

**The resolution to test in the spike:** generate each character *once* as a clean
full-body image, slice and rig it with Unity's 2D Animation package (free, built in),
animate once, and **bake the result out to sprite sheets**. Skeletal consistency going
in, plain sprite frames coming out, no frame ever regenerated — and one walk cycle drives
all fifteen characters, so the fifteenth costs almost nothing after the first.

Cost savings available if the matrix bites, in order of least damage:
1. Townsfolk as one or two silhouettes with palette swaps — already the §05 fallback,
   wearing different clothes.
2. Two facings mirrored instead of four (−50%).
3. Fewer room redecoration thresholds.

---

## 9. What gets built first — grey-box, exactly as Days 1–13 did

**Settled.** Rooms as coloured rectangles, people as capsules, real pathing, real seats,
real 90-second dwell, wired to the real economy and panning under a finger. The whole
game was playable grey-box before a single asset existed, and it is why the Day 14
revision cost no code.

Order:

1. **`IdleGuild.World` assembly**, camera with drag-pan and bounds, y-sorted sprite
   layer. Nothing but an empty floor.
2. **Rooms as rectangles**, positioned on a plan, reading level and built-state from
   `GuildState`. Dark where unbuilt.
3. **Seats**, derived from `ServiceSeats`, drawn as slots. This is the first moment the
   economy is visible.
4. **Townsfolk**, spawned at `3600/ServedPerHour`, walking a waypoint path to a free seat,
   sitting 90 s under a radial timer, leaving with a coin popup.
5. **The queue outside**, density from `UnservedPerHour`.
6. **The tap re-homed**: tapping a waiting customer seats them. `TakingsService` already
   does the work; it stops being an abstract button.
7. **Staff**, walking to the allocation-priority room.
8. **Adventurers**, from `AdventurerActivity`.
9. Only then: the character spike, the art direction, and the asset matrix.

**Nothing in steps 1–8 needs a single piece of art**, and every one of them is a check on
whether the economy reads. If the view is not fun as rectangles, it will not be fun with
art on it — and that is far cheaper to find out now.

---

## 10. What this obsoletes

- **`Day15_Art_Brief.md` §4's asset list** — already superseded once by the revision, now
  superseded again. Building *icons* are not the game; rooms are.
- **`Art_Generation_Guide.md` §3's 31-asset list** — the icon and portrait counts survive
  for panels and the roster, but room art, character sheets and the hall plan are a much
  larger and different requirement. The guide's §1 (commercial rights), §2 (model
  licences), §6 (pipeline) and §7 (Unity traps) are all unaffected and still current.
- **`HallView` and `BuildingCard`** as the home screen.

## 11. Still open

- Whether frames are authored or baked (§8) — decided by the spike.
- Whether the tab bar survives (§7).
- Room threshold count: how many redecoration states per room.
- Where the five rooms physically sit on the plan, and how wings attach as it grows.
- Whether the Provisioner and Front Desk have "seats" in the same sense the Tavern does,
  or whether their custom is depicted differently — the economy treats them identically,
  the fiction may not want to.

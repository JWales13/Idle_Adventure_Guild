# Day 14 — Full playtest

The roadmap's line is *"Village-to-Capital start to finish, log every friction point and
bug for Week 3."* This document is the route, and it is built around one distinction
that decides whether the day produces evidence or just impressions.

**Time skips preserve the economy. Gold grants destroy it.**

`SimulationClock.Advance` steps from event to event, and Day 4–5 made offline catch-up
run through the identical path — so `+1 hour` is not a cheat, it is the same guild
living the same hour with nobody watching. Pressing `+10k g` is a different thing
entirely: it breaks every pacing figure Day 13 published and turns the rest of the run
into a look-at-it exercise.

So the day is **two passes with a hard wall between them**, and the save from the first
is the more valuable output.

---

## 0. Before you start

Run the EditMode suite — **66 green**. Day 14 changes no code, so if it moves, the
playthrough has already found something.

Two things to know about the console's Time section, because they change how you use it:

| button | what it does | safe for pass A |
|---|---|---|
| `+1 min` / `+10 min` / `+1 hour` | raw `clock.Advance`, uncapped | **yes** |
| `Offline 8h` | goes through `OfflineProgress`, capped at `MaximumOfflineSeconds` | **yes** — the cap is 28,800s, so eight hours forfeits nothing and the offline path gets exercised for free |
| `+100 g` / `+10k g` / `+100 rep` | grants | **no. Pass B only.** |
| `Advance to <tier>` | skips the gate | **no. Pass B only.** |

---

## 1. Pass A — the paced run

The point of this pass is that it is the only guild that will ever have *earned* its
way to Capital. Three of the outstanding hand-checks are specifically waiting on that
and cannot be satisfied by a granted guild.

### A1 · Village → Town, in real time. Do not skip.

**Play this segment with your hands, at wall-clock speed, and time it.** It is the one
stretch of the game where skipping destroys the thing being measured. Days 10–11 left
this open as step 4 and called it *a pacing judgement — a test can assert a band, only a
person can tell whether the first ten minutes feel alive.*

The opening is 150 gold: Inn level 1 for 40, a Militia Recruit for 25, then the Rat
Cellar. The model says **Town at 8 minutes**, and the gate is Tavern 4 / Training Room 3
/ Inn 3 with 30 reputation.

Write down, while it is happening rather than afterwards:

- How long until the **second** adventurer, and does the wait feel like anticipation or
  like nothing happening?
- Is there ever a moment with gold in hand and nothing worth buying? The model says the
  worst gap in the whole game is 19 minutes and the median is 1.5, but medians are not
  what the opening feels like.
- Does the Rat Cellar's 45-second base loop read as a rhythm or as a wait?
- The first tier advance — does the game tell you it happened in a way you would notice
  if you were half paying attention?

**Record the actual wall-clock figure.** Eight minutes is the model's answer to a
question about arithmetic. This is the answer to the question about the game.

### A2 · Town → City, skipping honestly

Now use `+10 min` and `+1 hour`. The rule is that you skip time and then **spend**, in
the order the game makes obvious, rather than banking gold across a long skip and
shopping once. The model makes 551 purchases across the run; you will make far fewer,
and that alone will move the timings, so treat these as bands rather than targets:

| | model | yours |
|---|---|---|
| Village → Town | 0h08m | |
| Town → City | 1h08m | |
| City → Capital | 5h54m | |

City's gate is Tavern 48 / Training Room 26 / Inn 21, and **65,000 reputation** — the
figure Days 10–11 re-derived. Watch specifically for whether reputation or gold is what
actually holds you, because Days 8–9 chose those thresholds so that *a player blocked on
gold can spend their way out and one blocked on reputation can only wait.* If the
reputation wall is where you sit and stare, that is a finding.

Along the way, the Tavern unlocks rarity bands at levels 9 / 17 / 25 / 32. Note whether
each unlock announces itself or whether you find out by opening the roster.

### A3 · The Champion, at the moment Capital hands it over

**This is the judgement Day 13 §6 specifically asked Day 14 to make**, and it has to
happen here, on this guild, at this moment — not on a granted roster.

Capital unlocks the Dragonsworn Champion at 15,000 gold. Day 13 argues it will
disappoint, and the argument is arithmetic: `Adventurer.PowerWith` adds the Training
Room's bonus **flat**, so at a well-levelled Training Room a level-1 Champion is **379.4
power against a maxed Militia Recruit's 403.0**. The best adventurer in the game arrives
weaker than the first one you ever hired, and only wins at level 3.

Buy one. Open the party picker. Look at what it says about power and duration.

- Does the roster screen make it *look* like an upgrade?
- Does the picker's power figure make the disappointment legible, or does the player
  have to work it out?
- Having trained it two levels — about a thousand gold, seconds of income — does it
  recover fast enough that the moment does not matter?

The answer decides whether the flat-versus-multiplicative Training Room change stays
parked at Day 21 or gets pulled forward. Write down which way it lands and why; that is
this pass's second deliverable after the save.

### A4 · Dragon's Roost, on a guild that earned it

Days 10–11 step 8, the half a test could not take: *whether Dragon's Roost reads as a
fair fight at a guild that earned its way to Capital.* Recommended Power is 1,250 —
raised from 420 on Days 10–11 because every party a finished guild fields was already
past the 4× speed clamp.

With your actual arriving roster, not a maxed one: what duration and failure chance does
it offer? Is the Sunken Crypt still the better-paying job for your weaker party? If
Dragon's Roost is trivial on arrival, Recommended Power is wrong again and §4 of
`Docs/Day10_Tier_Transitions.md` needs re-running.

### A5 · Capture the save. Before anything else.

> **This is the step that is easy to skip and impossible to redo.**

`Docs/Tests.md` §4: *a save file is the only record of what an earlier build wrote, and
it stops existing the moment the current build runs.* The Week 1 save intended as the
first fixture was overwritten by an autosave before anyone thought to copy it, which is
why the second fixture is a synthesis rather than a real file. **A played-in save is the
one thing the fixture set still lacks**, and this pass is the first and possibly only
time one will exist.

Two things will destroy it: the 30-second autosave, and pass B's "Start over".

1. **Stop play mode.** `OnApplicationQuit` writes the save, so stopping is what commits
   the guild you just played rather than a version 29 seconds stale.
2. The debug console's Save section prints the location. It is
   `~/Library/Application Support/DefaultCompany/Idle_Adventure_Guild/`.
3. Copy it out **now**, before touching the Editor again:

   ```
   cp ~/Library/Application\ Support/DefaultCompany/Idle_Adventure_Guild/guild_save.json \
      ~/Idle_Adventure_Guild/Assets/_Project/Tests/Editor/Fixtures/save_day14_played_in.json
   ```

That path is also why the **bundle ID and product name rename must wait until after this
file exists** — `DefaultCompany` / `Idle_Adventure_Guild` *is* the save directory, so
renaming either strands every save on the machine.

A fixture needs a test to be worth keeping. Once the file is in place, tell me and I
will add it to `SaveFixtureTests` — the assertions are cheap and the file is not
reproducible.

---

## 2. Pass B — the inspection run

Grant freely. This guild is disposable and exists so the remaining checks do not cost
five hours each. **Only start it once A5 is done.**

`Start over` → `+10k g` as needed → `Advance to <tier>` → buy what you need to look at.

### B1 · The colours

Days 10–11 step 6, the half a test cannot see. `Format.RarityClass` returning
`rarity--epic` is pinned by `PresentationTests`; USS resolving that class to purple is
not.

Buy one of each band. **Epic reads purple, Legendary reads gold**, from `Tokens.uss`. If
they do not, the fault is in the stylesheet rather than in anything Days 10–11 or 12
wrote — no asset or `.cs` was edited to make the rarity bands appear.

### B2 · Day 12's four

All from §8 of `Docs/Day12_Roster_And_Parties.md`, and all the same species as B1 — a
test can assert the class name, not what it looks like.

1. **The destructive button reads as destructive.** `button--destructive` resolving to
   the negative colour, and — the easier one to miss — a *disabled* Retire still looking
   disabled rather than live.
2. **A sixteen-row party picker fits the phone.** Needs a full Inn, so `Advance to
   Capital`, buy Inn to 30, fill all sixteen beds. `overlay__panel--tall` and
   `overlay__body--flexible` are there so the summary and buttons stay put while the
   roster scrolls. This is the only case that can overflow.
3. **The selected state is unambiguous.** Select three of sixteen and confirm the marked
   rows read at a glance without you having to read them.
4. **Retiring feels like a decision rather than a dare.** The confirmation names the
   recruit cost and the level-1 restart. Informative, or scolding?

### B3 · The refuse → re-form → release → retire route, by hand

There is a test that walks this end to end, so this is about wording rather than
behaviour. Put someone on a standing order, try to retire them, read the refusal. Does
it tell you what to do next, or only that you cannot?

Day 12 chose to refuse rather than cascade, and the cost of that choice is exactly this
sentence doing its job.

---

## 3. The friction log

The roadmap asks for *every friction point and bug*, and the value is in writing them
down while irritated rather than reconstructing them later. One line each is enough.

```
#   where            what happened                        felt like    bug?
--  ---------------  -----------------------------------  -----------  ----
1   Village, 3 min   nothing to buy for ~90s              dead air     no
2
```

The "felt like" column is the one that earns its keep — Week 3 will cut things, and
*dead air* and *confusing* survive triage differently from *ugly*.

Two categories worth separating as you go, because they route to different days:

- **Bugs and blockers** → Week 3, and anything that stops progress is a Day 15 problem
  rather than a Day 22 one.
- **Feel and polish** → Day 22–23's bug bash, unless it is a balance figure, in which
  case it is Day 21's.

---

## 4. What this day cannot settle

Named so nobody goes looking:

- **The 18h14m tail.** Twelve hours of it sit after Capital, and no sitting reaches it
  honestly. That is what the model is for. If the endgame feels thin, say so as a
  judgement rather than measuring it.
- **Anything about art.** Everything is grey-box until Day 17.
- **Ads and IAP.** Interfaces only until Days 18–19, so every offer surface is absent
  rather than broken.

---

## 5. Checklist

```
[ ] EditMode suite: 66 green

PASS A — no grants, no tier skips
[ ] A1  Village → Town in real time, timed and described     (Days 10–11 step 4)
[ ] A2  Town → City → Capital by time skip, three figures
[ ] A3  the Champion on arrival — the Day 13 §6 judgement
[ ] A4  Dragon's Roost fair at an earned Capital             (Days 10–11 step 8)
[ ] A5  STOP PLAY MODE, then copy the save to Fixtures/      ← irreversible if missed

PASS B — grants allowed, only after A5
[ ] B1  Epic purple, Legendary gold                          (Days 10–11 step 6)
[ ] B2  destructive button / 16-row picker / selection / tone (Day 12 §8)
[ ] B3  the refusal sentence reads as a route

[ ] friction log written up
[ ] EditMode suite again: still 66 green
```

Days 10–11 and 12 between them estimated twenty-five minutes of hand-checking. That
estimate still holds for pass B. Pass A is the playthrough itself, and its first ten
minutes are the part that cannot be hurried, because that is the measurement.

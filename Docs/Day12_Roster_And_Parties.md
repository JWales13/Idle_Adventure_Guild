# Day 12 — Recruitment and assignment UI

Two actions the game was missing, both named by Days 10–11 and both found by
modelling rather than by playing. **UI and services only: no `.asset` file was
touched, no save field was added, and `SaveSchema.CurrentVersion` did not move.**

---

## 1. What the day owed, and where it came from

Days 10–11 filled the top two tiers and, in doing so, ran the model against a
player who actually buys the higher rarities. Two problems fell out that had
nothing to do with the content being authored:

- **The roster was a one-way ratchet.** The Inn tops out at sixteen beds, a
  Capital guild fields twelve, and nothing in the game could dismiss an
  adventurer — `AdventurerRoster.Remove` existed and only save restoration ever
  called it. So a bed, once filled, was filled for the rest of the run. A player
  who spent their spare beds on Epics during City could never hire the Legendary
  that Capital exists to unlock, whatever gold they finished with.
- **A standing order held its party for life.** Hiring a Dragonsworn Champion
  changed nothing at all until the player worked out unaided that they had to
  cancel the order and dispatch again. The best adventurer in the game could sit
  on the bench indefinitely with nothing on screen admitting it.

Both are the same shape, and it is worth naming because it will recur: **a
permanent decision gated on a resource that stops growing is a decision the
player can only get wrong once.** The content was authored so that both outcomes
finish and the roster screen shows the locked archetype with its reason, so the
choice was legible. It was still a trap, because legible and reversible are
different properties.

---

## 2. Retiring

`RecruitmentService` gained `PreviewDismissal` and `TryDismiss`, beside the hire
path they invert. It is a service method around `AdventurerRoster.Remove`, which
has existed since Day 4–5, plus a confirmation.

### The two refusals

| Outcome | When | What the player does |
|---|---|---|
| `OnQuest` | out in the field | wait — the run lands on its own |
| `OnStandingOrder` | belongs to an order, at home or resting | re-form that order's party |

**Checked in that order, deliberately.** Somebody who is both out on a quest and
committed to the order that sent them is told about the quest: it is the nearer
obstacle and it clears itself, and nothing they do to the order helps until that
run lands. It is the same judgement `RecruitmentService.Preview` makes about
tier before rarity, and it is pinned by a test for the same reason — a later
reorder should be a failing test rather than a subtly unhelpful sentence.

**Refusing rather than cascading is the decision worth defending.** The
alternative was to let a dismissal drop the member from their order and cancel
the order if it fell below the quest's party size. That is one tap instead of
two, and it makes a destructive action carry a second consequence the player has
to be told about after the fact. Worse, the naive version of it — remove the
member, leave the order standing — is precisely the failure the Ledger already
records once: **a destructive action that does not also invalidate the live
state it describes will be undone, or in this case silently broken, by whatever
reads that state next.** `TryStartRun` would have returned false forever, with a
standing order on the screen that simply never went out again and nothing
anywhere saying why.

The cost of refusing is that a guild whose every bed is on an order cannot
retire anyone until it re-forms something. That is why the two halves of this
day ship together, and why there is a test that walks the whole route: refuse →
re-form → release → retire.

### Nothing is refunded

A rebate would make hiring and firing a free churn loop. What the roster was
short of was reversibility, not a refund: the player who guessed wrong needs a
way out, not a way to guess for nothing. The confirmation says the price of
changing their mind again — the recruit cost, and that the replacement starts at
level 1 — which is the information that makes it a decision rather than a
surprise.

---

## 3. Re-forming a standing order

`QuestAssignment.MemberInstanceIds` used to be documented as *fixed for the life
of the assignment*. It now says **fixed for the life of a run**, which was
always the truer statement and nobody had needed to notice.

The property that makes this safe was already there. `ActiveQuest` snapshots its
own party at dispatch, and `SimulationClock.SendPartyToRest` sends *that*
snapshot home rather than reading the assignment. So replacing an order's party
mid-run:

- leaves the run's timer, failure chance and rewards untouched — the same
  guarantee that makes a quest immune to an upgrade bought halfway through it;
- recalls nobody mid-dungeon, and loses no reward that was already computed;
- decides who goes out **next**.

`QuestDispatchService.TryReformParty` is therefore **not** gated on the order
being idle. The window between runs of a repeating order is a few seconds of
rest, and an edit a player can only make by catching that window is an edit they
will never make. The order's own members stay eligible for it whatever they are
doing; everyone else has to be idle and unassigned, exactly as for a first
dispatch. The quest slot is not re-checked, because the order already holds one.

`QuestPartyReformed` was added to `GameEvents` so the order card redraws. It is
structural rather than cosmetic: without it the card would keep listing the old
party until some unrelated event happened to rebuild the screen.

---

## 4. The picker

One overlay serves both "Send a party" on a quest offer and "Re-form party" on a
standing order, because they are the same question asked at two moments.

It shows, live, the party size against what the quest asks for, the combined
power, and what the run would take at that strength. `PartyPower` and
`PreviewDurationSeconds` have existed on the dispatch service since Day 4–5 with
no caller at all; this is the day they got one. They are what turn *swap the
Recruit for the Champion* from a guess into a comparison made before committing,
which is the whole argument for letting the player choose rather than
auto-filling.

Four smaller decisions inside it:

- **The duration is shown only once the party is the size the quest wants.** A
  figure for two of three adventurers is arithmetically real and practically a
  lie — it is how long a run would take that this quest will never accept.
- **Rows are built when the overlay opens and are not rebuilt while it is up.**
  Nothing reachable from the picker changes who is on the roster; hiring and
  retiring both live on the screen underneath. Rebuilding mid-choice would throw
  away a selection the player was halfway through making. Who is *free* does
  change while it is open, and that is re-read every refresh.
- **Somebody who cannot join is dimmed rather than hidden.** A list that
  silently omitted the busy Champion would read as a roster that had lost
  someone.
- **The tick is a filled box, not a glyph**, so selection still reads after
  Week 3 swaps in a display font that has never heard of whichever character we
  would have picked.

The order card now names its party instead of counting it. "3 adventurer(s)" was
describing a decision the player could no longer see, which is a large part of
how the bench problem stayed invisible.

---

## 5. Two changes that go past the brief

Both are narrowings, both are reversible, and both are written here because they
change behaviour that existed before today.

**A party is now exactly the size the quest asks for.** The lower bound has
always been enforced; the upper one was unreachable, because no caller could
assemble an over-size party by hand and so nothing had to say no. The picker
can. Every duration and failure figure in the game was derived against the
number on the quest asset, so sending four on a three-person job would hand the
player a speed multiplier nothing has been tuned for. Refused as
`PartyTooLarge`. **Widening this is a design decision for a later day** — it is
Quest Board territory — not something that should arrive as a side effect of
building a screen.

**"Send a party" picks the strongest free adventurers, not the first on the
roster.** `TryDispatchAvailableParty` walked the roster in order and took whoever
came first, which quietly sent the weaker adventurer whenever the player had
hired them first. It is also the choice `guild_model.py` makes on the player's
behalf, and the two disagreeing is how a modelled arc stops describing the real
one.

---

## 6. Saves

Nothing to migrate, and it is worth writing down *why*, because Days 10–11
already produced one change of this shape that would have slipped past unnoticed.

`SavedAssignment.MemberInstanceIds` is unchanged in name, type and meaning. On
disk it has always meant *who is on this order*; what changed is only that the
running game can now write a different answer into it. A save from a
pre-Day-12 build restores into today's classes with no reinterpretation, and a
save written today loads into a pre-Day-12 build as an ordinary party.

Two consequences of restoration bypassing the services, which is correct and
should stay that way:

- `SaveRestore` builds a `QuestAssignment` directly rather than through
  `TryDispatch`, so a save holding an over-size or duplicated party — which no
  build has ever written — still loads and still runs. **Restoring repairs
  rather than refuses**, and refusing to load a guild over a party-size rule
  introduced on Day 12 would be the wrong trade.
- Retiring publishes `AdventurerDismissed`. Restoration stays quiet, for the
  same reason it announces no upgrades: loading a guild that once retired
  somebody is not retiring them again.

No new fixture is owed. The rule in `Docs/Tests.md` is to add one whenever the
format or the *meaning of a value in it* changes, and neither did.

---

## 7. The model, and a correction it produced

`guild_model.py` had a dismiss-shaped hole in it. Its impatient player was
simulating a wall the game no longer has, and its comment block said in so many
words that such a player "can never hire a Champion at all" — a sentence that
stopped being true today. **A drifted model is worse than no model, because its
answers stay confident**, and Day 13 is about to lean on this one.

Before touching it, it was run against unchanged assets and reproduced the
Days 10–11 figures exactly — 5h53m / 20h31m patient, 4h30m / 15h30m impatient.
So the drift was in the *player*, not in the content.

### What it took two runs to get right

The obvious rule — retire the weakest benched member whenever a better archetype
is affordable — is wrong, and wrong in an instructive way. Every other hiring
decision in the model ranks archetypes by `potential`, what they reach fully
trained. Applied to swapping, that churns the entire roster to Legendary the
moment gold stops being scarce, throwing away every level of training bought
along the way:

> both profiles finishing with **sixteen Legendaries**, everything-maxed at
> **28h16m and 27h06m** — eight to twelve hours *longer* than before the action
> existed.

That is not a player, it is an arbitrage bug. The fix needs no threshold and no
magic number: a player swaps somebody out when the replacement is better **now**,
so the incumbent's current power has to lose to a level-1 recruit of the better
archetype. Sunk training is respected as a consequence rather than as a rule.

### The numbers Day 13 compares against

Assets are unchanged. Only the player moved.

| | Capital | everything maxed | decisions | final roster |
|---|---|---|---|---|
| **Patient**, published Days 10–11 | 5h53m | 20h31m | 420 | 6 C, 2 U, 4 R, **4 Legendary** |
| **Patient**, today | 5h41m | **22h50m** | 444 | 5 C, 1 U, 5 R, **5 Legendary** |
| **Impatient**, published Days 10–11 | 4h30m | 15h30m | 416 | 4 C, 3 U, 4 R, **5 Epic** |
| **Impatient**, today | 4h16m | **17h45m** | 450 | 3 U, 4 R, **9 Epic** |

Two findings, and the second is the uncomfortable one.

**Retiring makes the game longer.** Both arms gained a bit over two hours on the
tail. The action is a gold sink — the full recruit price again, and the
replacement restarts at level 1 — so a player who uses it is buying a stronger
endgame with time. That is a defensible trade and it is worth Day 13 knowing it
is there, because it is a two-hour swing that no asset change caused.

**The impatient player still never fields a Legendary.** The lock is no longer
*structural* — every bed frees up, and the Commons are gone from their final
roster entirely — but it has become economic. By the time they can afford
Champions, their Battlemages are trained past what a level-1 Champion is worth,
so the strict-improvement rule never fires for the top band. So Day 12 rescued
them from wasted beds and did not rescue them from the archetype ceiling.

**Read that as a bracket rather than as an answer.** The two runs above are the
two extremes of one decision the model has to make on the player's behalf:
greedy-by-potential gives sixteen Legendaries and a 28-hour game, strict
improvement gives no Legendary and 17h45m. A real player who *wants* a Champion
will accept being temporarily weaker and train them, which is somewhere between.
Which of those the game should encourage is a balance question, and it belongs to
Day 13 rather than to the day the action shipped.

---

## 8. Verification

Most of this day is a test, and the tests are shape rather than value — not one
of them names a bed count, a recruit cost or a rarity threshold, so Day 13 and
Day 21 can move every figure in the game without any of them flickering. None is
tagged `BalanceCanary`, because none asserts a number.

| File | Covers |
|---|---|
| `RosterRatchetTests` | a full guild can always make room; the Legendary is reachable after every bed is spent; both refusals, in the right order; a refused dismissal does not half-happen; retiring twice is refused rather than repeated; no refund |
| `PartyFormationTests` | the run in flight is untouched by a re-form; the new party is the one that goes out next; an order's own members stay eligible while out; nobody is borrowed from another order; exact party size; no duplicates; unknown order; strongest-first suggestion; the refuse → re-form → release → retire route end to end |
| `PresentationTests` | every dismissal refusal has a sentence; the commitment refusal names the order when it knows it |

### Still by hand

Four things, and the first three are the same kind of thing Day 10's §8 left
behind — a test can assert the class name, not what it looks like.

1. **The destructive button reads as destructive.** `button--destructive`
   resolving to the negative colour, and a disabled Retire still looking like
   every other disabled button rather than a live one.
2. **A sixteen-row picker fits the phone.** `overlay__panel--tall` and
   `overlay__body--flexible` are there so the summary and the buttons stay put
   while the roster scrolls. Worth checking at a full Inn, which is the only
   case that can overflow.
3. **The selected state is visible at a glance.** Selecting three of twelve and
   confirming the marked rows are unambiguous without reading them.
4. **Retiring feels like a decision rather than a dare.** The confirmation says
   the recruit cost and the level-1 restart; whether that reads as informative
   or as a scolding is a judgement, and it belongs with Day 14's playthrough.

Call it ten minutes, on top of the fifteen still outstanding from Days 10–11.

---

## 9. Files

Four new, ten changed, no assets.

```
new     UI/Views/ConfirmOverlay.cs        the dialog, and ConfirmRequest
new     UI/Views/PartyOverlay.cs          the picker, and PartyRequest
new     Tests/Editor/RosterRatchetTests.cs
new     Tests/Editor/PartyFormationTests.cs

Core/Events/GameEvents.cs        + AdventurerDismissed, QuestPartyReformed
App/GameWorld.cs                 IsAssigned becomes FindAssignmentFor
App/QuestAssignment.cs           + SetParty; the party is fixed per run, not per order
App/RecruitmentService.cs        + DismissOutcome, PreviewDismissal, TryDismiss
App/QuestDispatchService.cs      + PartyTooLarge, DuplicateMember, UnknownOrder,
                                   IsFreeForParty, SuggestParty, PreviewReform,
                                   TryReformParty, shared CheckParty
UI/Outcomes.cs                   sentences for all of the above
UI/Views/RosterView.cs           Retire, and the order named on the member line
UI/Views/QuestsView.cs           Re-form party; orders name their party
UI/GuildScreenController.cs      owns both overlays; two more subscriptions
UI/Styles/GuildTheme.uss         party rows, destructive buttons, tall overlays
Tests/Editor/PresentationTests.cs
Docs/tools/guild_model.py        + retire(), and the swap rule in the player policy
```

The architectural bet is untouched. The features are still Core-only; everything
added here lives in `App` and `UI`, which are the two assemblies allowed to see
across. No field was added to `BuildingDefinition` and no branch to
`GuildState.Aggregate`.

# The Guild Ledger
### Idle Adventurer's Guild — Project Charter

*Working reference document — read this in full before writing or changing any code. It is the source of truth, not whatever summary accompanies it.*

---

## 01 · Project Principles

**Clean Code discipline.** Intention-revealing names, small single-purpose functions, single-responsibility classes. No god-object `GameManager` owning economy, UI, saves, and quests at once. Depend on interfaces where systems interact, not concrete classes. No magic numbers/strings — constants or config assets. Comments explain *why*, not *what*.

**Data-driven, modular architecture.** Buildings, adventurers, quests, and guild tiers are ScriptableObject data assets, not hardcoded logic — new content means a new asset, not an edited script. Systems communicate through events/interfaces rather than direct references, so one system can change without rippling into the others. Code is organized by feature (`Guild/`, `Quests/`, `Economy/`, `Adventurers/`), not by type.

The 3→5 building scale-up (adding Quest Board and Armory post-launch) is the concrete test of this: it should mean creating two new `BuildingDefinition` assets and wiring their unlock conditions, not touching the Tavern/Training Room/Inn code that already ships.

**No sequence of choices may leave the player unable to make progress.** Added Day 16,
after a playtest reached an unrecoverable state on the *third purchase of a new guild* —
Tavern to 1, Tavern to 2, Inn to 1, which is 147.50 of 150 starting gold, leaving 2.50
against a 25-gold recruit in a build where gold comes only from contracts and a contract
needs an adventurer. Income was exactly zero and stayed zero. There is always an action
available that improves the player's position: early that is the crown's stipend, and late
it is letting staff go, which is free and is half of why dismiss exists.

This is a **property, not a balance figure**, so it belongs here rather than in a tuning
doc, and it is asserted against the shipped catalogue in `SolvencyTests` rather than
against a fixture — a fixture would have been built from the same assumptions that
produced the dead end. Two things it is not. It is not a promise that mistakes are free:
the recovery is deliberately slow, and a bad opening costs real time. And it is not
satisfied by giving the player enough gold to start with — Day 4-5 solved the original
opening deadlock exactly that way, "in data rather than in code", and **a data solution
that depends on the player spending it correctly is a hope rather than a solution.** This
is that hope failing, four hundred days of design later.

It governs decisions that have not been made yet, which is the point of writing it down:
whether a room can be sold, whether wages can bankrupt you, whether prestige can strand a
run, whether a cosmetic purchase can ever be the thing blocking progress.

**Styling in code, not the Inspector.** UI is built on Unity's UI Toolkit with USS stylesheets — CSS-like, text-based, no per-prefab Inspector tinkering. Design tokens (color, spacing, type scale) live in a shared stylesheet from day one.

**Model usage.** Sonnet 5 for planning, design discussion, and roadmap work. Opus 5 for actual script generation and implementation, where holding the whole modular architecture in mind while writing new code matters more than speed.

**Session handoff.** As conversations grow, close a phase by updating this document and appending a continuation prompt to the Status & Handoff section — so a fresh conversation can pick up with full context and minimal re-explaining.

---

## 02 · Concept Summary

> **⚠ SUPERSEDED on Day 14 by `Docs/Vision_Revision.md`.** The game is now an **idle
> hotel tycoon** — five rooms, four of which earn gold per hour, with contracts feeding
> the building rather than sitting beside it. Kept below as the record of what was built
> first, because most of it survives: the three MVP buildings each do what this section
> says they do, and the revision adds two rooms and a second economy rather than
> replacing them.


Quests replace "customers," adventurers replace "staff," guild hall buildings replace "shop upgrades." Standard idle-tycoon math wearing a fantasy coat.

- **Quests** — Dispatch adventurers on jobs that generate gold/reputation over time. The game's idle income source.
- **Adventurers** — Recruited at the Tavern, leveled at the Training Room, housed at the Inn. Assigned to quests they're suited for.
- **Guild hall buildings** — MVP ships with three: Tavern, Training Room, Inn. Quest Board and Armory are fully designed but deliberately deferred to a post-launch update — the architecture is built so adding them later means new data assets, not reworked code.
- **Prestige — Branch Expansion** — Post-Capital, the player founds a new branch guild elsewhere with a permanent Renown bonus. **Deferred to a post-launch update** — only the Renown data field is stubbed in for v1.

**Launch tier arc:** Village → Town → City → Capital → *(post-launch)* Branch Expansion

### Building effects (finalized)

Quest math runs on four stats, deliberately non-overlapping so upgrading every building matters instead of one dominating. Enforced by a **tier-gate rule**: advancing Village→Town→City→Capital requires a minimum level across *multiple* buildings, not just total gold spent — so tunneling into one building can't skip the others.

| Building | Owns | Effect |
|---|---|---|
| **Tavern** | Reward Yield | Food/drink quality raises adventurer Morale, which increases gold/loot paid out per completed quest. Also gates which adventurer rarity tiers are available to recruit as it levels. |
| **Training Room** | Speed & Success Rate | Raises each adventurer's Power stat, which shortens quest duration and reduces failure chance on harder quests. The most intuitive "line go up" building — balanced by the tier gate so it can't be the only investment. |
| **Inn** | Capacity & Throughput | Room count caps how many adventurers can be housed at all; room quality shortens the rest/recovery time between quests. Answers "how much total work can move through the guild," distinct from Training Room's per-adventurer quality. |
| **Quest Board** *(post-MVP)* | Work Availability | More simultaneous quest slots and access to higher quest tiers as it levels — the counterpart to Inn's workforce capacity. |
| **Armory** *(post-MVP)* | Risk Mitigation | Gear reduces failure/injury chance on the hardest quest tier, and can gate certain quest types (a combat quest requires weapons equipped). Deliberately not a second source of Power — that stays Training Room's job. |

Quest Board and Armory are deferred **by design, not by schedule pressure** — MVP holds fully on Tavern/Training Room/Inn alone: quest slots and difficulty tiers are static per guild tier until Quest Board is added, and the hardest quests keep a flat base fail rate until Armory is added.

---

## 03 · Roadmap — Four Weeks, Solo

> **⚠ SUPERSEDED on Day 14.** Four weeks became about six; submission moves from Day 26
> to **Day 38, buffer through Day 42**. See §8 of `Docs/Vision_Revision.md` for the
> current week plan and §9 for the revised cut list. Weeks 1 and 2 below happened
> essentially as written.


Aggressive but achievable if architecture stays disciplined in Week 1. Each week ends with a checkpoint that must be true before moving on — if a checkpoint slips, cut scope (see Risks & Cuts) rather than the schedule.

### Week 1 — Foundation & Architecture
*Goal: build the modular, data-driven core so later weeks add content instead of restructuring code.*

- **Day 1** — Project setup. New Unity project, UI Toolkit + ad/IAP SDK stubs installed, git repo, feature-folder structure (`Guild/`, `Quests/`, `Economy/`, `Adventurers/`, `Core/`). Also: start Apple Developer Program enrollment today — lead time matters (see Week 4).
- **Day 2–3** — Core data model. ScriptableObject definitions for Building, Adventurer, Quest, and GuildTier; a lightweight event bus; Model classes (GuildState, PlayerEconomy) fully decoupled from UI.
- **Day 4–5** — Core loop logic. Quest resolution/timers, idle & offline income calculation, building upgrade cost/effect resolution, adventurer leveling — logic only, exercised via debug console before any UI exists.
- **Day 6** — Save/load. GuildState serialized to a versioned JSON schema — versioned now so later content additions don't break existing saves.
- **Day 7** — UI Toolkit scaffolding. USS design tokens (color, spacing, type scale), grey-box screens for Guild Hall, upgrade panel, quest log, and roster, wired to the Model via events.

**Checkpoint:** the full loop is playable grey-box — earn gold, upgrade a building, recruit an adventurer, dispatch a quest, watch idle income tick.

### Week 2 — Core Gameplay Complete
*Goal: the full Village → Capital arc exists and is completable, still grey-box.*

- **Day 8–9** — Building set finalized. Full upgrade trees for Tavern, Training Room, and Inn at Village tier — the confirmed MVP set (Quest Board / Armory are post-launch additions).
- **Day 10–11** — Tier transitions. Village → Town → City → Capital, each unlocking new buildings/adventurers/quests purely through data assets — the real test of whether the modular architecture holds.
- **Day 12** — Recruitment & assignment UI. Adventurer roster management, quest assignment screen.
- **Day 13** — First balancing pass. Rough economy tuning across all four tiers — shape of the curve matters more than exact numbers right now.
- **Day 14** — Full playtest. Village-to-Capital start to finish, log every friction point and bug for Week 3.

**Checkpoint:** the game is completable start to finish, grey-box art, no crashes blocking progress.

### Week 3 — Content, Art & Monetization
*Goal: replace placeholders with real assets, wire up revenue systems.*

- **Day 15–16** — AI art generation. Guild hall backgrounds per tier, building icons, a representative adventurer portrait set, UI icon set, app icon. Batch and curate on a timebox — don't chase perfection per asset.
- **Day 17** — Art integration. Wire assets into USS, apply the Week 1 design tokens consistently across screens.
- **Day 18** — Ad SDK integration. Rewarded video (2× offline earnings, instant quest complete) plus a sparing interstitial placement, tested in sandbox.
- **Day 19** — IAP integration. Starter pack, soft-currency gems for time-skips, remove-ads option — wired to App Store Connect sandbox.
- **Day 20** — Hardening. Save/load edge cases, minimal analytics events (install, tier reached, purchase), settings screen (sound, restore purchases, privacy links).
- **Day 21** — Second balancing pass, incorporating monetization touchpoints so ad/IAP offers read as genuine value, not friction.

**Checkpoint:** feature-complete build with real art, ads, and IAP wired — ready for internal test builds.

### Week 4 — Polish & Submission
*Goal: ship — and leave room to survive a rejection.*

- **Day 22–23** — Bug bash. Full playthrough on a real device. Fix crashes/blockers first, then feedback polish (tap juice, sound, transitions).
- **Day 24** — App Store Connect setup. Listing, real-build screenshots, description/keywords, hosted privacy policy, privacy "nutrition label," age rating, App Tracking Transparency prompt if the ad SDK requires it.
- **Day 25** — TestFlight smoke test of the exact submission build — confirm IAP and ads work outside the editor sandbox.
- **Day 26** — Submit for App Review. Not Day 28 — see the buffer note below.
- **Day 27–28** — Buffer. Fix and resubmit if rejected (common causes: incomplete IAP testability, privacy label mismatch, launch crash, metadata gaps). If approved early, use the time for a hotfix-ready build and final QA.

**Checkpoint:** submitted by Day 26, not Day 28 — Apple review time is the one part of this schedule outside your control, so the buffer has to come from finishing early, not from hoping review is fast.

---

## 04 · App Store Submission Checklist

A few of these have lead time that doesn't compress — start them early rather than treating this as a Week 4 list.

**Accounts & setup**
- [x] **Apple Developer Program enrollment ($99/yr)** — **done and approved**, confirmed Day 14. This was the one item on this list whose delay was never yours to control, and it gates TestFlight on Day 25 and submission on Day 26. Everything else here is work rather than waiting.
- [ ] App Store Connect record created early (bundle ID + name reserved)
- [ ] Export compliance questionnaire answered

**Privacy & compliance**
- [ ] **Privacy policy hosted at a public URL**
- [ ] App Privacy "nutrition label" (data collection disclosure — matters more with ad SDKs)
- [ ] Age rating questionnaire
- [ ] App Tracking Transparency prompt, if the ad network requires tracking

**Store listing**
- [ ] App icon, all required sizes
- [ ] Screenshots at required device sizes, from the real build
- [ ] Description, keywords, preview text

**Commerce**
- [ ] **IAP products configured and submitted** — these get their own review pass, budget extra time
- [ ] TestFlight internal pass confirming purchases and ads work outside the editor

---

## 05 · Risks & Scope Cuts

> **⚠ EXTENDED on Day 14.** The table below still holds; §9 of `Docs/Vision_Revision.md`
> adds cuts for the staff subsystem, the arrivals mechanic, and dropping from five rooms
> to three — the last of which the modular architecture makes a data decision.


Decided in advance, so a schedule slip becomes a scope decision instead of a panic.

| Risk | Cut |
|---|---|
| Behind schedule by end of Week 2 | Drop to three tiers (Village → Town → City) instead of four. A shorter, complete arc beats a longer, broken one. |
| AI art generation eats more time than budgeted | Reuse building silhouettes across tiers with palette/detail swaps rather than fully unique art per tier. |
| Onboarding scope creeps into a full tutorial sequence | Three or four contextual tooltips instead — a guided-tutorial flow is a classic solo-dev time sink. |
| Ad placement tuning and App Review friction over aggressive ads | Launch with rewarded ads only; add interstitials post-launch once real retention data exists. |
| Temptation to build the Branch Expansion prestige loop now, since it's designed | Already decided — stub the Renown field only. The full loop is a post-launch update, not a launch feature. |

---

## 06 · Status & Handoff

### Handoff protocol

**Closing a session:** Update the Current Status fields below to what actually happened, not what was planned — call out any deviation from the roadmap explicitly. Append one line to the Session Log; never edit a past entry. Fill the Continuation Prompt Template with specifics and drop the result into "Most recent continuation prompt." Update this file (and the hosted Guild Ledger artifact, if reachable) before the session ends.

**Opening a session:** Read this entire document before writing or changing any code — it is the source of truth. State back, in a sentence or two, where you understand the project to be before proceeding, so a misread surfaces immediately instead of compounding. The Principles section (01) applies automatically and doesn't need to be repeated by the user.

### Continuation prompt template

The outgoing instance fills this in and posts the result in "Most recent continuation prompt" below; the user pastes that filled version to open the next conversation.

```
I'm continuing work on Idle Adventurer's Guild, a solo Unity idle-tycoon game
(fantasy adventurers' guild theme), targeting App Store submission with [N]
days left against the original 4-week deadline.

The full project plan, architecture principles, and roadmap live in this
document — read it in full before doing anything else.

Current position: Week [X], Day [Y] — "[week title]"
Last completed: [specific systems/files finished and verified working]
Next task: [the specific next item from the roadmap]
Deviations from the plan so far: [none, or specifics]
Known issues/blockers: [none, or specifics]

Follow the Principles section of the linked doc (Clean Code, data-driven
ScriptableObject architecture, event-driven decoupling, UI Toolkit/USS
styling) without needing it re-explained. Confirm your understanding of
where things stand in a sentence or two before writing any code.
```

### Current status

**Current status:** Week 3, Day 16 complete — **the revision is being built.** A sixth
feature assembly exists, four rooms can earn gold per hour, and the payroll can be let go
on the same day it can be hired.

`IdleGuild.Staff` joins the graph (Core-only, like the other five). `TradeService` in App
turns §3.1's three levers into money: demand from the tier, capacity from the room's
level, throughput from staff, with staff serving the most valuable custom first so that
opening a room can never make the guild poorer. Wages come out of the till and the net is
floored at zero. `TakingsService` is the tap, filling a capped queue so an absence cannot
bank an hour of thumb. `GuildStat` gained five entries, `GuildTierDefinition` four fields,
`PlayerEconomy` the silent `Accrue` its own event comment has been asking for since Day 2.
The save gained the payroll and the accrual state as **added fields only** — 
`SaveSchema.CurrentVersion` still has never moved.

**No `.asset` file changed.** Not one. The engine exists and the five rooms do not, which
is §8's order and — see below — the only defensible way round.

**The day's finding is that three quarters of the staff ladder is dead content, and that
the reason is the action this day was built to add.** Before authoring four
`StaffDefinition` assets from `tuned_params.json`, the two authored numbers were divided
by each other — the check Day 13 established, never once performed on this ladder. Gold
per point of service climbs **0.47 → 2.06 → 8.63 → 32.69**, and the tuned configuration
hires **105 Potboys and never buys a Server, Barkeep or Steward at any integration step.**
The price is not the cause: flattening the ladder still gives 98 Potboys and one Server,
and starving slots to four still gives four Potboys. The cause is that `purchase()` can
only ever *append* staff — **the model has no way to let anybody go** — so slots once
filled with the cheapest help are filled forever and the ladder is unreachable at any
price. An unreachable rung cannot cost the loss anything, so it was free to be priced
arbitrarily. Day 15 then raised the staff-slot curve to fix the opening silence, correctly,
and removed even the slot pressure that might have forced a climb.

So **no staff assets were authored**, and the invariant that would catch it ships calling
`Assert.Ignore` with a pointer to the write-up rather than passing vacuously green.
**Followed immediately by a playtest that reached an unrecoverable state on the third
purchase of a new guild** — Tavern to 1, Tavern to 2, Inn to 1, which is 147.50 of 150
starting gold, leaving 2.50 against a 25-gold recruit in a build where gold comes only
from contracts and a contract needs an adventurer. Income was exactly zero and stayed
zero. Fixed with **the crown's stipend**, a mailbox on a 30-second cooldown paying 1/2/4/8
gold by tier, and — the durable half — **a new rule in §01: no sequence of choices may
leave the player unable to make progress.** The sizing argument is worth reading: the
first attempt at "recover in about a minute" was **189× the entire Village room economy**,
because while the mailbox refills continuously *recovery speed is a sustained rate*. See
`Docs/Day16_Followup_Solvency.md`. It also caught that **the tap shipped the day before is
inert** — no room produces demand, so unserved demand is zero and the queue never fills.
Fifth appearance of a failure whose only symptom is an absence.

**Suite at 114 green**, up from 71, of which **three are Ignored rather than green** and say
so — one guard is vacuous until a room produces custom, and the staff-ladder invariant has
no ladder to guard. `Docs/Day16_Staff_And_Revenue.md` carries all of it, plus corrections
to §3.1, §4 and §6C of the charter, all three of which described mechanisms the tuned
model does not use.

---

**Superseded — Week 3, Day 15:**

Day 15 tuned the economy, and **the interface was seen for the first time.**

Day 15 was handed one instruction — score first beats rather than tier boundaries — and it
was right and unreachable, because four things underneath it were wrong. The model's
answers moved by about **2x with the integration step**, so §6C's celebrated "68% of
lifetime income from rooms" was an artefact of scoring at `step=60` and was 82% at step 5.
`payback()` said *seconds* in its docstring and returned **hours**, so the reserve it was
compared against had never once bound and §6C's finding #11 — the one-gold coin flip — was
live the whole time. The dead twenty-two minutes was **arithmetic rather than a curve**:
150 starting gold minus 143.85 of opening purchases leaves 6.15 against a 40-gold next
step at 1.54 gold a minute, which is 21.9 minutes exactly. And every number in that
sentence was **hardcoded outside `SPEC`** — the tuner was never stuck in a basin, it was
searching a space that did not contain the problem.

With the model made step-independent and the opening given dials, the worst silence in the
first twenty minutes went **22 minutes → 5.0**, Village landed at **23 minutes** inside its
new 20–30 band, rooms take **65–69%** of lifetime income, the purchase-gap 90th percentile
is **4 minutes** against a ≤10 target, and every one of those figures now holds across a
twelve-fold change in the integration step (`spread` 0.26 → **0.06**). That last number is
the day's real output: the model means the same thing twice.

**The interface had never been drawn, and that is the larger finding.**
`GuildScreenController` calls `GetComponent<UIDocument>()`, and the `UI` object carries two
panel components — a `PanelRenderer` holding `GuildPanelSettings` and a `UIDocument` whose
Panel Settings field is **empty**. A UIDocument in that state still returns a perfectly
good `rootVisualElement`; it is simply an orphan attached to no panel. So the controller's
own null guard passes, the whole screen builds, nothing throws, the log stays clean, and
the Game view shows the camera's clear colour — indistinguishable from an empty scene. The
game was played through the debug console from Day 7 to Day 15. **See the correction below;
several things this document records as verified cannot have been.**

`Docs/Day15_Economy_Tuning.md` carries all of it.

---

**Superseded — Week 2, Day 14:**

Day 14 ran the playtest, fixed a shipping bug it found, and then turned into the largest
design conversation the project has had. The game is now an **idle hotel tycoon**: five
rooms, four of them earning gold per hour, with contracts feeding the building through a
Front Desk commission rather than being the only source of income. `Docs/Vision_Revision.md`
is the new charter and supersedes §02, §03 and §05 above.

**Nothing was built.** No `.asset` and no game `.cs` changed for the revision — it is
design, a model and a tuner. That was deliberate: Days 8–9 and Day 13 both found
structural failures no amount of playing would have surfaced, and this change is larger
than either.

**The good news, and it is most of the news:** three of the four original buildings were
already mechanically what the revised vision describes. Tavern quality attracting better
adventurers has been in the build since Day 4. The gap was never the design — it was that
none of it is drawn. A tycoon wearing a spreadsheet.

**Suite at 71 green**, up from 64: the recall fix (three tests) and the Day 14 fixture
(two). Everything below this line describes the game as built, which the revision has not
yet touched.

Day 13 was the first balancing pass, and it answered the question it was handed by
dissolving it. The ten-hour policy bracket Day 12 left behind was not a question about
the player: **the rarity ladder tripled in training cost per band while power only
doubled**, so a Legendary bed cost 81x a Common bed to realise and returned 16x the
power — 6,268 gold per point of power against 1,236. Setting the per-band training
multiple to 2 makes gold-per-power flat across all five bands, and the three swap
policies that spanned ten hours then agree inside eighty minutes. **Five numbers on five
`.asset` files, no game `.cs` touched, no save field moved, and not one `BalanceCanary`
needed updating** — which is itself the finding, since no canary had ever watched a
training cost. The suite went 64 → 66 and no existing test moved.
`Docs/Day13_First_Balance_Pass.md` carries the reasoning, the collapse table and one
deferred design decision.

**Verified, not just written.** The suite was run and **reports 66 green**. The log
confirms the data-only claim the same way Days 10–11 confirmed theirs: the five changed
assets imported with **no `OnValidate` warning**, and the **seven game DLLs did not
recompile at all** — they are timestamped 03:47 and 03:59 against asset edits at 16:51,
and only `IdleGuild.Tests.Editor.dll` moved (16:58), which is the one file that changed.
Two things in `Editor.log` look alarming and are both historical, worth naming so the
next session does not re-investigate them: three `error CS` on `AssetValidation.cs:52`
are the `Object.GetInstanceID()` deprecation already recorded in §6 of `Docs/Tests.md`,
from before the Days 10–11 follow-up fixed it, and seven `GameContent: no guild tiers
listed` warnings carry `SerializedObjectReferenceBinding` in their stacks — somebody
dragging a reference in the Inspector, from before `AssetValidation.WhenLoaded` deferred
those checks. Nothing of ours appears in the last 6,000 lines of the log.

**Current pacing, superseding Day 12's four figures:** patient Capital **5h54m** and
everything-maxed **18h14m**, impatient **6h56m** and **19h33m**, both profiles finishing
with sixteen Legendaries. Purchase gaps are the best recorded — median 1.5 min, 90th
percentile 4, worst 19.

Day 12 gave the game its first two reversible decisions. Retiring an adventurer
(`RecruitmentService.PreviewDismissal` / `TryDismiss`) unwinds the bed ratchet, and
re-forming a standing order's party (`QuestDispatchService.TryReformParty`) unwinds the
frozen-party half of the same problem — with one party picker serving both a first
dispatch and a re-form, showing party power and the resulting duration before the player
commits. **No `.asset` file was touched, no save field was added, and
`SaveSchema.CurrentVersion` did not move.** Four new files, ten changed; eight assemblies
recompiled with zero `error CS` and zero new warnings, verified in `Logs/Editor.log`. The
suite gained seventeen tests and **reports 64 green**. `Docs/Day12_Roster_And_Parties.md` carries the reasoning, the
two behaviour narrowings, and a modelling correction that matters more than the code —
see the resolved list below.

**Week 2, Days 10–11.** Unity project at `~/Idle_Adventure_Guild` (Unity **6000.5.0f1**, Universal 2D / URP), published to a **private GitHub repo** and managed through **GitHub Desktop**. The core loop was finished on Days 4–5 and the 11-step smoke test in `Docs/Day04_Asset_Values.md` passed against the fourteen ScriptableObject assets, with the scene wired at `Assets/_Project/Scenes/Guild.unity` (a `Game` object carrying `GameBootstrap` and `DebugConsoleOverlay`).

Days 10–11 filled the top two tiers, and **the architectural bet passed its stated test: eighteen changed paths, not one of them a `.cs` file.** The tier assets already raised Max Quest Tier and quest slots; what was missing was content to unlock. Two quests (`sunken_crypt`, `dragons_roost`) and two archetypes (Arcane Battlemage, Dragonsworn Champion) were authored, adventurer Max Level went 10 → 25 with re-spaced power and training curves across all five archetypes, and City's reputation gate re-derived from 28,000 to 65,000. The Epic and Legendary bands appeared in the game fully styled and explained without a line of UI being written — `Format.RarityClass`, `Outcomes.Describe` and the rarity tokens in `Tokens.uss` had all been written on Day 7 and never exercised. `Docs/Day10_Tier_Transitions.md` carries the tables, the reasoning and a 9-step verification pass. **Step 1 has been run and passes** — all nine changed assets imported clean, no `OnValidate` warning from any of them, and `Library/ScriptAssemblies/IdleGuild.*.dll` were last written more than two hours before the first asset was, so Unity did not recompile at all. Steps 2–9 are Editor-side and still outstanding. A **separate follow-up commit** then fixed the long-standing `OnValidate` false alarms that step 1 surfaced — see the resolved list below. A third commit replaced most of that pass with tests: an eighth assembly, `IdleGuild.Tests.Editor`, holding **43 EditMode tests that run in 46 ms and all pass**, plus three checked-in save fixtures. `Docs/Tests.md` is the standing reference for it. Steps 4, the colour half of 6 and the is-it-fair half of 8 stay manual — about fifteen minutes rather than forty.

Days 8–9 rebuilt the building trees. The trees are now deliberately **asymmetric** — Tavern 90 levels, Training Room 40, Inn 30 — the gates are re-spaced onto each building's own scale, and the three MVP quests were re-costed to match. `Docs/Day08_Building_Trees.md` carries the tables, the reasoning and the spec Days 10–11 must hit; `Docs/tools/guild_model.py` is a runnable model of the loop that produced them. **All ten edited assets reimported with zero warnings**, verified in the log — worth stating because the values were written directly into the `.asset` YAML rather than through the Inspector.

Day 7 added the interface. `IdleGuild.UI` moved to the top of the assembly graph and now holds USS design tokens, a component stylesheet, and grey-box screens for Guild Hall, quests and roster behind a bottom tab bar, with the upgrade panel as an overlay raised from a building. `GuildScreenController` is the one MonoBehaviour, mirroring `GameBootstrap`: events set a flag, a 100 ms tick reads live values and rebuilds structure only when something actually changed. `Docs/Day07_UI_Setup.md` carries the two Editor steps — a Panel Settings asset and the scene wiring — plus a 10-step pass.

Day 6 added save/load. A versioned JSON file now carries the guild tier, every building level, all four balances, the roster with levels, activity and rest timers, the quest runs in flight with their dispatch-time snapshots, the standing orders and the lifetime clock counters. `ISaveStore` and `FileSaveStore` joined the ad and IAP interfaces in `Core/Services` as the persistence boundary; `App/Saves/` holds the wire format, capture, restore, the migration ladder and the `GameSaveService` policy layer. The last-seen timestamp has moved out of PlayerPrefs and into the file. The debug console gained a **Save** section, and `Docs/Day06_Save_Verification.md` carries the schema, the compatibility rules and a 10-step verification pass including corruption and content-removal cases. **Seven code assemblies build with zero CS errors and zero warnings**, verified after import — Core, the four features, App and now UI.

**All three verification passes have now been run and pass** — Day 6 save/load including the corruption and content-removal cases, Day 7 UI including the Week 1 checkpoint, and the Day 8–9 asset values. The Day 7 Editor setup is done: `GuildPanelSettings.asset` at 1080×1920 with Match 0, and a `UI` object under `Game` carrying `GuildScreenController` with both stylesheets assigned.

The passes earned their keep immediately. Step 1 of the Day 6 document found a real bug — the debug console's save-delete undid itself within thirty seconds, because it removed the file without resetting the running world, so deleting and restarting produced the *old* guild. Fixed; see the Day 6 resolved list below. Nothing else misbehaved.

The Week 1 checkpoint holds, minus the UI that Day 7 adds.

**Next action:** Day 17 — **the owed interface hand-check first**, then arrivals, then the five rooms as assets. The hand-check has now been deferred twice and it is the one that has to precede the art day, not follow it.

The tuner has the two hardest targets already: rooms at **68%** of lifetime income
against a 70% target, and a **6-minute** 90th-percentile purchase gap against a 10-minute
one. What remains is the shape of the time curve, and the specific problem is **not** that
Village runs 30 minutes — it is that the first-session trace shows the tavern and front
desk built instantly, an adventurer in the crowd immediately, and then **nothing at all
until the first staff hire at 21 minutes**. The next tuning pass should score *first-beat
timings* rather than tier boundaries. That is a change to `tuner.py`'s loss function, not
to the design.

Then §8 of `Docs/Vision_Revision.md` has the build order: the Staff assembly and the
revenue engine first, arrivals second, the five rooms as assets third.

Day 14 starts from a stable set of numbers rather than an open question, and it is owed
three things by earlier days. It is the day the **twenty-five minutes of accumulated
hand-checking** finally has a played-in save to run against — Days 10–11's step 4, the
colour half of step 6 and the is-it-fair half of step 8, plus Day 12's four. It should
also judge whether a Dragonsworn Champion **feels** like the reward Capital exists to
hand over, because §6 of `Docs/Day13_First_Balance_Pass.md` argues that it currently
should not: the Training Room's bonus is flat, so it compresses the authored 16x rarity
ladder to 3.7x by the time the guild is finished, and a level-1 Champion arrives weaker
than a maxed Militia Recruit. That is a deferred decision with a written cost, not a
mystery — see the open list.

### The central architectural bet, and how to check it still holds

`GuildStat` (Core) enumerates the eight quantities a building can influence. `BuildingDefinition` owns no logic — it holds `BuildingEffect` entries that each target one stat with a level-scaled `ScalingCurve`. `GuildState` aggregates them and exposes the result through `IGuildStats`.

`QuestSlots`, `MaxQuestTier` and `FailureRateReduction` are **already declared** even though nothing produces them yet. `GuildState.Get(QuestSlots)` currently seeds from the guild tier's static value and adds nothing. When Quest Board ships post-launch it contributes additively to that same stat, and every existing call site picks it up untouched. Armory does the same for `FailureRateReduction`.

**The test:** adding Quest Board and Armory must mean two new `.asset` files and zero edits to shipped `.cs`. If a future session finds itself adding a field to `BuildingDefinition` or a branch to `GuildState.Aggregate` to make them work, the bet has been lost — stop and reconsider rather than pushing through.

**Checked on Day 4–5 and still holding.** Writing the whole core loop required no change to `BuildingDefinition` and no branch in `GuildState.Aggregate`. Quest slots are read through `QuestLog.SlotsWith(IGuildStats)`, the hardest quest tier through `QuestResolution.IsAvailable`, and failure mitigation through `QuestResolution.FailureChance` — all three consume the stat and none of them cares where it came from, so the Quest Board and Armory contributions land in call sites that already exist.

**Checked again on Days 10–11, which was the day set aside for it, and it passed cleanly.** Two new quest tiers and two new rarity bands went in as four `.asset` files and five edits to existing ones; the changed-files list contained no `.cs` at all. Three properties did the work, and all three were already there: content declares its own availability through `MinimumTierOrder` so no tier asset had to list it; `QuestResolution.IsAvailable` reads `MaxQuestTier` off `IGuildStats` rather than off the tier, which is the same path Quest Board will use; and the UI had been written for five rarities on Day 7 and simply started rendering two of them. The honest caveat is that this test exercised *content*, not a new **stat** — Quest Board and Armory will be the harder version, because they add a producer to a stat rather than a consumer of one.

Two zero-safety conventions protect against half-filled assets reading as silent zeros rather than obvious mistakes:

- `ScalingCurve` stores growth as a **percent per level**, so an all-zero asset evaluates flat rather than collapsing through a `pow(0, n)` term.
- Multiplicative effects are **bonus fractions** accumulated onto 1.0 — `0.15` means +15%, and an unfilled field means "no effect", never "multiply everything by zero".

A third convention arrived on Day 4–5: **a quest run's duration, failure chance and rewards are snapshotted when it starts**, not recomputed when it finishes. A timer therefore never moves under the player mid-run, an upgrade pays off from the next dispatch, and offline catch-up can resolve hours of runs without reconstructing what the guild's stats were at each moment in the past.

### Why the building trees are different lengths

Days 8–9 found this by modelling the whole arc, and it is a property of the simulation
rather than a tuning choice, so it will still be true after Day 13 retunes every number.

**Only the Tavern compounds.** Reward Yield multiplies gold without bound, so its cost
can grow geometrically and level 80 is still worth buying. The other two buildings are
bounded by the game's own clamps: Training Room power stops shortening a quest once the
party reaches four times the recommended power, and Inn recovery can only ever remove
the *rest* half of a cycle. Charging a geometric price for a bounded benefit is what
made the top of a uniform 40-level tree literally unreachable — not expensive,
unreachable, with time-per-level diverging and 200 simulated hours failing to close the
last eleven levels.

Hence Tavern 90 levels at 15% cost growth, Training Room 40 at 19%, Inn 30 at 21%. The
Tavern is the spine the player keeps feeding; the other two are trees they finish. **The
tier gate is what stops them tunnelling into the Tavern alone**, which is the job the
multi-building rule was always there to do.

Two checks worth repeating whenever a curve moves, both of which caught real failures:

- **No dead levels.** Every effect must still improve measurably at max level. A
  building whose effect saturates before its ceiling is the same bug as an unreachable
  ceiling, wearing a different hat.
- **The purchase-gap profile, not the tier times.** A long tail there means a stretch of
  the game with nothing to buy. That is what the Day 4–5 numbers did — all three
  buildings maxed at 2h15m with Capital still two hours away — and tier times alone
  would never have shown it.

### The roster is a one-way ratchet, and what follows from that

Days 10–11 went looking for why higher rarities felt pointless and found the power
numbers were the smaller half of it.

**The Inn maxes at 16 beds, a Capital guild needs 12 to field four parties of three,
and nothing in the game can dismiss an adventurer.** `AdventurerRoster.Remove` exists
and is called only by save restoration. So a bed, once filled, is filled for the rest
of the run: a player who spends their spare beds on Epics during City can never hire
the Legendary that Capital unlocks, no matter how much gold they end up with. The model
confirms it — greedy hiring finishes a whole game with five Battlemages and no
Champion.

The content is authored so that both outcomes are playable, and the roster screen shows
the locked archetype with its reason so the choice is legible rather than hidden. But
**an irreversible decision made on incomplete information is a trap wearing a decision's
clothes**, which is why Day 12 owed the game a dismiss action. It is a service method
around a method that already exists, plus a confirm dialog.

**Day 12 shipped it, and the model then said something more interesting than "fixed".**
`TryDismiss` refuses while the adventurer is out on a quest or committed to a standing
order — refusing rather than cascading, because removing a member of a live order would
leave `TryStartRun` failing silently for the rest of the run, which is this document's
own destructive-action-that-undoes-itself lesson wearing yet another hat. Re-forming is
what releases them, which is why the two halves shipped together. What the model then
showed is that **the ratchet was two locks, not one, and only the structural one is
gone**: beds free up, but a player whose Battlemages are trained will not swap them for a
level-1 Champion, so the top band stays out of reach on economic grounds. See §7 of
`Docs/Day12_Roster_And_Parties.md`. The general shape survives the fix: a reversible
decision is not the same as a cheap one.

The same shape appears once more nearby: **a `QuestAssignment` holds its party for the
life of the order.** Hiring someone better changes nothing until the player cancels the
standing order and re-dispatches. That is defensible — the alternative is parties
silently reshuffling under the player — but it means the assignment screen has to make
re-forming a party easy to find, or the best hire in the game sits on the bench.

**Day 13 finished it, and the economic lock turned out to be six percent wide.** A
level-1 Dragonsworn Champion is 379.4 power against a maxed Militia Recruit's 403.0 at a
finished Training Room — it wins at **level 3**, for about a thousand gold. The model
had a swap rule that only ever compared a level-1 hire against a trained incumbent, so
it reported a wall two training levels thick as impassable. Underneath that, the
training ladder tripled per band against power that doubled, which is what made the top
band genuinely expensive rather than merely late. With both fixed, **both player
profiles finish with sixteen Legendaries** and the fork costs eighty minutes rather than
an endgame. The ratchet is now fully unwound: not structural since Day 12, not economic
since Day 13.

Worth stating as a general shape, because it will recur: **a permanent purchase gated
on a resource that stops growing is a decision the player can only get wrong once.** And
its companion, which took four days to earn: **a decision that is reversible, legible
and correctly priced is the only one a player can actually make** — Days 10–11 supplied
the legibility, Day 12 the reversibility, and Day 13 found that the price had been wrong
the whole time and was doing more damage than either.

### The save format, and the rule that keeps it readable

Saves are versioned JSON written by `JsonUtility` — no package to add, no dependency
to audit before submission, and it works under IL2CPP without the reflection tricks a
general-purpose serialiser needs. Its cost is that doubles print to roughly seven
significant figures, which spends fractions of a gold on a balance and a millisecond
on a quest timer. The one value where precision genuinely matters, the last-seen
timestamp, is a `long` tick count and comes back exact.

**The compatibility rule is about the schema file, not the migration code:** fields
are only ever *added*, never removed and never renamed. A field that stops being used
stays declared and unread; a field whose meaning changes gets a new name beside the old
one. That is what lets a save two versions old deserialise into today's classes at all —
everything it wrote has somewhere to land, everything it did not arrives at a neutral
default. So adding a field with a sensible default needs no version bump; bump
`SaveSchema.CurrentVersion` only when a load must *do* something to an older save, and
add the matching case to `SaveMigrations.Upgrade`. A save from a newer build is refused
rather than guessed at.

Two consequences that reach beyond the save code. **Never renumber the persisted
enums** — `GuildStat`, `Rarity`, `CurrencyType`, `ModifierKind`, `AdventurerActivity`,
each of which says so in its own comment. And **never change a definition asset's `Id`
once a build has shipped**: renaming the asset file is harmless, changing the Id field
orphans every save that references it.

**Restoring repairs rather than refuses.** A save is not trusted to match today's
catalogue, because a quest renamed in Week 2 or an archetype cut in Week 3 leaves saves
in the wild pointing at nothing. Anything unresolvable is dropped, the guild around it
is left standing, and the count comes back in a `SaveRestoreReport` that the console
shows and the log warns about. The table of what is repaired is in
`Docs/Day06_Save_Verification.md`.

**Restoration is quiet, which Day 7 has to know about.** `GuildState.RestoreState`
publishes `GuildStatsRecalculated` but not `BuildingUpgraded` or `GuildTierAdvanced` —
loading a level-4 Tavern is not four upgrades and loading a City guild is not reaching
City again. A screen therefore cannot build its first frame out of change events. It
waits for `GameLoaded`, reads the current state directly, and treats every other event
as a delta from there. `GameLoaded` is published from `GameBootstrap.Start`, not
`Awake`, so a screen subscribing in `OnEnable` still receives it.

### Repository layout

```
Assets/_Project/
  Core/          IdleGuild.Core — no dependencies
    GuildStat.cs         the thirteen stats buildings can influence (incl. post-MVP)
    GuildStatScope.cs    which of them mean anything summed across the guild
    Rarity.cs            ordered; the Tavern gates recruitment by raising the max
    ModifierKind.cs      Additive | Multiplicative
    CurrencyType.cs      Gold, Reputation, Gems, Renown (Renown stubbed for v1)
    ScalingCurve.cs      level-indexed growth, shared by costs and effects
    IGuildStats.cs       the seam letting features read stats without touching Guild
    IRandomSource.cs     injectable randomness; offline catch-up has to be replayable
    SystemRandomSource.cs  seeded or not, but its own stream, not global state
    Events/
      EventBus.cs        typed pub/sub; generic static channels, no boxing
      GameEvents.cs      the shared cross-assembly event vocabulary
    Services/
      IAdService.cs / NullAdService.cs
      IPurchaseService.cs / NullPurchaseService.cs
      ISaveStore.cs           the persistence boundary; atomic write, one recoverable copy behind it
      FileSaveStore.cs        one UTF-8 file per key under persistentDataPath
  Economy/       IdleGuild.Economy — depends on Core only
    PlayerEconomy.cs     all balances; the only place they change
  Adventurers/   IdleGuild.Adventurers — depends on Core only
    AdventurerDefinition.cs   archetype data; declares its own unlock tier
    Adventurer.cs             roster member; derived stats take IGuildStats
    AdventurerActivity.cs     Idle | OnQuest | Resting
    AdventurerRoster.cs       the roster, capped by the Inn's Housing Capacity
  Quests/        IdleGuild.Quests — depends on Core only
    QuestDefinition.cs        job data; declares its own unlock tier
    QuestResolution.cs        duration, failure and rewards as pure functions
    ActiveQuest.cs            one run; its outcome snapshotted at dispatch
    QuestOutcome.cs           what a finished run paid
    QuestLog.cs               runs in flight, capped by Quest Slots
  Staff/         IdleGuild.Staff — depends on Core only (Day 16)
    StaffDefinition.cs        one kind of employee; no wage field, deliberately
    StaffMember.cs            one employee; no level, no activity, no rest timer
    StaffRoster.cs            the payroll, capped by the StaffSlots stat
  Guild/         IdleGuild.Guild — depends on Core only
    BuildingDefinition.cs     level 0 = not built; CostToReach(1) = build cost
    BuildingEffect.cs         one building's contribution to one stat
    BuildingLevelRequirement.cs
    GuildTierDefinition.cs    the multi-building tier gate
    GuildState.cs             aggregates effects; implements IGuildStats
  App/           IdleGuild.App — the only assembly referencing more than Core
    GameContent.cs            the catalogue asset; the one place all four types meet
    GameWorld.cs              composition root: state and lookups, no transactions
    SimulationClock.cs        event-stepped time; identical path online and offline
    QuestAssignment.cs        a standing order; what actually makes the game idle
    QuestDispatchService.cs   dispatch, and restarting a repeating order
    BuildingUpgradeService.cs gold into building levels
    RecruitmentService.cs     the tier, Tavern-rarity and Inn-capacity gates
    TrainingService.cs        gold into adventurer levels
    TierAdvancementService.cs Village through Capital
    StaffService.cs           hiring, and letting go, shipped on the same day
    TradeService.cs           the revenue engine; the one place staff meet building stats
    TakingsService.cs         the tap, and the capped queue that makes it a mechanic
    OfflineProgress.cs        capped catch-up; owns no maths of its own
    GameBootstrap.cs          the one MonoBehaviour: builds the world, loads the save, drives the clock
    DebugConsoleOverlay.cs    throwaway IMGUI panel; delete once real UI lands
    Saves/
      SaveGameData.cs         the versioned wire format, and the rules for changing it
      SaveCapture.cs          running guild to data; transcription, no decisions
      SaveRestore.cs          data to running guild, repairing whatever content no longer has
      SaveMigrations.cs       the version ladder; deliberately empty until a bump is needed
      GameSaveService.cs      save/load/delete policy, and what happens when JSON does not parse
  UI/            IdleGuild.UI — the top of the graph; references Core, the four features and App
    Styles/
      Tokens.uss             the only file in the project naming a raw colour or measurement
      GuildTheme.uss         component styles, every value pulled from a token via var()
    Format.cs                amounts, durations, multipliers, stat names, rarity classes
    Outcomes.cs              the service outcome enums as sentences a player can act on
    GuildContext.cs          what a screen may touch, and the rule it exists to make visible
    Ui.cs                    element constructors, so intent survives the boilerplate
    SafeArea.cs              notch and home-indicator insets
    GuildScreen.cs           the three tab destinations
    GuildScreenController.cs the one MonoBehaviour: builds the shell, subscribes, ticks
    Views/
      TreasuryBar.cs / TabBar.cs / ToastBar.cs        the permanent chrome
      HallView.cs / BuildingCard.cs                   home: tier gate, stats, buildings
      BuildingDetailOverlay.cs                        the upgrade panel, and Week 3's overlay pattern
      QuestsView.cs / RosterView.cs                   work, and people
      ConfirmOverlay.cs                               one question, two answers; ConfirmRequest
      PartyOverlay.cs                                 who goes, for a new order and an existing one
  Data/          ScriptableObject asset instances
    Buildings/ Tiers/ Adventurers/ Quests/   and GameContent.asset alongside them
```

**ScriptableObject values now live in three documents, and the newest wins.** `Docs/Day10_Tier_Transitions.md` is current for **adventurers, quests and the City tier's reputation gate**. `Docs/Day08_Building_Trees.md` is current for **the three building trees and the tier gates' building requirements**; its quest table and its §3 tier-3/tier-4 spec are superseded. `Docs/Day04_Asset_Values.md` is current only for **GameContent, the scene setup and the 11-step smoke test** — its building, tier, quest *and* adventurer tables are all superseded now, and it carries a banner about the first three. Content declares its own availability via `MinimumTierOrder` rather than being listed on a tier asset, so adding content never edits an existing file — the exception being `GameContent`, which lists everything by design, since something has to.

Three structural decisions, the first two from Day 1 and the third from Day 4–5, all flagged as reversible:

- **Assembly definitions per feature.** Principle 01 says systems talk through events and interfaces rather than direct references. An `.asmdef` per feature turns that from a convention into a compile error — a feature that reaches into another feature's internals will not build. This is the mechanism that makes "add Quest Board and Armory without touching shipped code" checkable rather than aspirational.
- **Features depend on `Core` and on nothing else.** No cross-feature references at all; anything shared travels through `Core` events and interfaces. Deliberately the strictest arrangement, because loosening it later is a one-line asmdef edit and tightening it later is a refactor. If it proves too strict during Day 4–5 loop work, add the reference and note it here.
- **`IdleGuild.UI` moved above `App` and may reference everything (Day 7).** A screen has to render a `BuildingDefinition` and call `BuildingUpgradeService.TryUpgrade` — the same cross-assembly pressure that created App, one layer up. The alternatives were a presenter layer in App keeping the views Core-only, which is purer and costs roughly 40% more classes on the three heaviest UI days, or routing commands through the event bus, which is the arrangement Day 4–5 already rejected because "did the spend succeed?" has no good answer through a fire-and-forget publish. **The features stayed Core-only**, so the compile-time wall and the Quest Board / Armory bet are untouched; what changed is that one more assembly above them can see across. The cost is real and worth restating: the UI assembly can now see the whole game, so *views hold no rules* is kept by discipline rather than by the compiler. `GuildContext` is where that discipline is written down — **views read state and call services, they never compute one**. A cost, a gate, an unlock or a failure chance belongs to a definition asset or a service, and a screen that works one out for itself has put a rule where Day 13's balance pass will never look.

- **A seventh assembly, `IdleGuild.App`, sitting above the features.** Day 4–5 forced the question the note above anticipated: upgrading a building spends gold and raises a level, and dispatching a quest takes people off the roster — transactions spanning three feature assemblies, none of which may reference another. The alternatives were loosening the Core-only rule, or routing request-and-response traffic through the event bus, where "did the spend succeed?" becomes genuinely hard to answer. Instead App references all five feature assemblies, and nothing references App. The features stayed Core-only exactly as Day 1 hoped; the compile-time wall between them is untouched. The cost is one more layer to keep thin — App holds composition and transactions, never rules.

### Working arrangement

Implementation runs in **Claude Cowork**, not a terminal session. Its shell is a Linux VM with the project folder mounted, which has two consequences that hold until decided otherwise:

- **Claude runs NO git commands at all. The developer commits through GitHub Desktop.** The bridge blocks file deletion, and git unlinks constantly, so anything from Claude's side leaves stale `.git/*.lock` files that then block the developer's own git. This includes commands that merely *look* read-only: **`git status` refreshes the index, which is a write, and it leaves a lock behind.** Learned twice on Day 1 and again on Day 3. Genuinely safe for inspection: `git log`, `git show`, `git ls-tree`, `git ls-files`. Claude writes files and states the commit message it would use; the developer commits.
- **Unity Editor and Xcode steps are manual.** `unity` and `dotnet` are not on Claude's PATH. Claude also cannot make Unity import new files — the developer must focus the Editor window, after which Claude can verify compilation by checking for `Library/ScriptAssemblies/IdleGuild.*.dll` and grepping `Logs/` for `error CS`. Fine through Day 7, which is entirely file-level. **Week 4 (Days 22–26: device builds, TestFlight, App Store Connect) is not solvable from Cowork** and needs either a terminal session or the developer driving Xcode directly. Decide before Day 22, not on it.

Recovery command if a git operation from Claude's side ever leaves debris:

```
cd ~/Idle_Adventure_Guild && rm -f .git/HEAD.lock .git/index.lock .git/objects/maintenance.lock && find .git/objects -name 'tmp_obj_*' -delete
```

**Open decisions carried forward:**

- **The revision is now half built.** Day 16 landed the Staff assembly and the revenue engine; arrivals and the five rooms as assets are still outstanding, so **the game that runs is still the one-economy version** — three buildings, gold only from contracts, individual training. `Docs/Vision_Revision.md` is the charter, corrected in §3.1, §4 and §6C by Day 16; `Docs/tools/tycoon_model.py` models it and `tuner.py` searches its parameters. **`guild_model.py` still describes the game that actually exists** and stays until the revision ships — then it retires. Two models is confusing for exactly as long as two games exist.
- **~~Staff need a dismiss action designed in from the start~~ — done on Day 16, on the same day as hiring.** `StaffService.TryLetGo` and `TryLetGoLeastCapable` ship beside `TryHire`, with no gate on either: an employee has no run in flight and no standing order to belong to, so there is nothing they can be in the middle of. Nothing is refunded, the same rule adventurers follow. **What that immediately exposed is the item below**, and it is the more interesting half.

- **The staff ladder has never been climbed by anything, so its four prices are guesses.** Day 16's finding: gold per point of service climbs 0.47 → 2.06 → 8.63 → 32.69 across the ladder, and the tuned configuration hires **105 Potboys and no employee from the three tiers above, at every integration step**. Not a pricing problem — flattening the ladder still gives 98 Potboys and one Server, and starving slots to four still gives four Potboys. `tycoon_model.purchase()` can only ever *append* staff, so the ladder is unreachable at any price and nothing has ever measured what the upper rungs are worth. **The balance pass must give the model the dismiss action the game now has before it searches for those four numbers** — replace the least capable when the replacement is better the day it arrives, which is the rule Day 13 landed on for adventurers after the naive version added eight hours. Until then treat every staff figure in `tuned_params.json` as unmeasured rather than tuned; the room curves are unaffected, since what the rooms see is the payroll's total service. `AHigherStaffTierNeverCostsMoreGoldPerPointOfService` ships **ignored**, with a pointer, rather than vacuously green. **No staff `.asset` was authored on Day 16 for this reason.**
- **The crown's stipend is sized for a build where nothing else earns, and should be re-checked the day the rooms land.** 1/2/4/8 gold every 30 seconds, capped at three deliveries. It holds ~30% of what the guild earns at Village and Town, 11% at City and 0.01% at Capital — necessarily, because the earn rate at tier openings runs 418 → 730 → 4,208 → 15,859,700 g/hr and no ladder stays proportional across a ×3,768 jump. **Recovering from an empty treasury takes about twelve and a half minutes**, pinned as a `BalanceCanary` so the cost is visible rather than merely true. That figure is mostly an artefact of today's build having no other income; if it still bites once rooms and the tap are live, the fix already designed is a **hardship line** — accrual stops above a per-tier threshold, so the crown can never hold you above `line + one delivery`, which buys back fast recovery without making the mailbox farmable. `Docs/Day16_Followup_Solvency.md` §4.

- **The tap is inert until a room produces `ServiceDemand`.** `TakingsService` shipped tested and documented on Day 16 and cannot fire: no shipped building carries the stat, no tier carries a base service, so total demand is zero and the queue never fills. It goes live with the room assets and nothing before then. **Any pacing measured before that is measuring the mailbox, not the game.**

- **The tap mechanic — collecting takings exists as of Day 16; the other two do not.**
  `TakingsService.TryCollect` serves one waiting customer at the best-paying room still
  going unserved, out of a queue that fills at the unserved-demand rate and is **capped**,
  so an absence cannot bank an hour of thumb. It is worth exactly nothing once staff cover
  the room, which is what makes it safe to sell a familiar against and why a familiar
  bought late is a familiar wasted. §6B's other two automations — **hiring from the crowd**
  and **dispatching contracts** — still need their manual versions, and both arrive with
  arrivals and the room assets.
- **The tier panel has to show what the gate is still missing** — untouched on Day 16 and now concrete: `TierAdvancementService.Preview` returns `RequirementsNotMet` and names nothing, which is not enough for the screen finding #7 asks for. It owes a shortfall description — which building, how many levels, how much reputation — and it belongs in the **service** rather than the view, because views hold no rules. Before Day 23. The model's greedy player never saved for a gate — reputation cleared Village in twenty minutes while the Front Desk it also required went unbuilt for three hours. A real player runs the same policy unless the interface tells them what to save for.
- **The Barracks has to look like it makes money** — untouched on Day 16, and it cannot be closed until the Front Desk is authored: the number that fixes it is what the Barracks is worth *through the commission*, and `ContractCommission` is declared with no producer. Declared deliberately, so the save-persisted enum is appended once rather than twice. It earns nothing directly, so a payback-ranked player never buys it; the model went dark on the entire adventurer half of the game until the calculation could see through to the commission it enables. The player needs that connection made visible.
- **~~Day 17 carries every line of display code~~ — the wire is in, and it paid twice.**
  `Ui.Icon` exists, `BuildingCard` reads `_icon`, `Tokens.uss` has icon sizes and
  `GuildTheme.uss` has the `.icon` block. It found the Sprite Mode default *and* the
  panel-settings bug. **What Day 17 still carries: the adventurer portrait slot, the tier
  background mechanism, and the import pass** — smaller than it was, and now landing on an
  interface somebody has actually looked at.
- **Nobody has judged the interface yet, and it has now been deferred twice.** Day 16 was file-level and touched no UI, so nothing was lost — but Day 17 is where it stops being free, because it is the day that sizes twenty-three assets against a judgement nobody has made. **It is the first item of Day 17, ahead of arrivals.** Original note follows.
  Now that it renders, the twenty-five minutes of accumulated hand-checking is finally
  possible and is genuinely owed: Days 10–11's colour half of step 6, Day 12's four, and
  whether the 96px room icon reads correctly beside a 28px title. Do this before Day 17
  generates twenty-three more assets sized to match a judgement nobody has made.
- **The whole game is 6h54m of content against a 20-hour target**, stable across every
  integration step so it is not noise. A curve-length question for Day 22's balance pass.
- **Tapping is 87% of room income across the first thirty modelled minutes.** With a real
  tier gate the guild is capital-starved early, so the thumb carries the opening. Arguably
  right — that is the stretch an idle game wants the player present for — but it is high
  enough to be a decision rather than a side effect, and it makes the "collect the takings"
  familiar very valuable very early. Settle it before §6B's monetisation lands on Day 28.
- **The worst opening silence is 5 modelled minutes against a 2-minute target**, and every
  seed across five rounds landed between 4.7 and 6.7 — a frontier rather than a search
  failure. About eleven lived minutes at Day 14's 2.2x, with tapping filling it. A question
  for the first real playtest, not for the model.
- **The Training Room's flat power bonus** — deferred from Day 13 to Day 21, and the revision may resolve it for free, since power moves to the Barracks. Re-check rather than assume.

- **Git LFS — the `.gitattributes` half is done, the `git lfs install` half is yours.** Written on Day 14, one day ahead of its deadline and while the window was still clean: **no binary has ever been committed to this repo**, verified with `git ls-files`, so there is no history to rewrite. Twenty-five `filter=lfs diff=lfs merge=lfs -text` patterns now cover raster art, audio, video, fonts, models and binary libraries. **`.meta` is deliberately not among them** — it is small, it is text, Unity needs to merge it, and sending sidecars to LFS makes every asset's metadata a pointer file and breaks diffing on the one thing you most need to diff. The `binary` block was kept below the LFS block rather than deleted, because it now covers anything dropped from the list above. **Still outstanding: `git lfs install` on the machine, which Claude cannot run.** Until that is done the filter is declared and not wired, so run it *before* committing `.gitattributes`, and confirm the first art commit with `git lfs ls-files`.
- **Day 17 is carrying every line of display code in the project, and the roadmap hides it.** Found on Day 14 while writing `Docs/Day15_Art_Brief.md`. `BuildingDefinition._icon` and `AdventurerDefinition._portrait` are the **only** two sprite fields in the data model, both declared on Days 2–3 and **neither ever read by anything**; `QuestDefinition` and `GuildTierDefinition` have none, so the roadmap's "guild hall backgrounds per tier" has nowhere to land; **no view renders an image at all**; and `Ui.cs` has no image constructor. So "Day 15–16 art generation, Day 17 integration" reads as a big day followed by a small one and is the reverse — Day 17 needs an image helper, slots in three views, a tier-background mechanism and the import pass, against a one-day budget. The brief resolves the data-versus-USS question so no new sprite field is needed on any asset (per-content art uses the two fields that exist; per-screen art hangs off USS classes), which keeps Days 15–17 data-and-style only and leaves the Quest Board / Armory bet untouched. **The mitigation is to move about an hour of Day 17 into Day 15** — wire one building icon end to end before generating the other twenty-three assets, which is this project's own verification habit wearing art clothes. Decide on Day 15, not Day 17.

- **Week 4 execution surface undecided** — see Working arrangement. Hard deadline Day 22.
- **Ad network not chosen** — Day 18 needs one (Unity LevelPlay / AdMob / Unity Ads). The `IAdService` boundary lets the choice wait, but not past Week 3.
- **IAP provider not chosen** — same shape; `IPurchaseService` defers it to Day 19. Unity IAP is the path of least resistance given the project is already Unity.
- **Bundle ID and product name still template defaults** — `DefaultCompany` / `Idle_Adventure_Guild` in ProjectSettings. Needed for the App Store Connect record and worth reserving early, per the §04 lead-time note. **They are also the save directory**, confirmed in the log as `~/Library/Application Support/DefaultCompany/Idle_Adventure_Guild/`, so changing either strands every existing save — including any worth keeping as a test fixture. Capture what you want to keep *before* renaming, since §4 of `Docs/Tests.md` is right that a save file cannot be recreated once lost.
- **The save file is plain text and trivially editable**, timestamp included, which means free offline earnings for anyone who opens it. A Week 3 hardening item with a soft deadline of Day 20, and the fix is a checksum rather than encryption — the goal is to make casual editing visible, not to defeat a determined player. Quarantined `guild_save.json.corrupt-*` files are also never cleaned up automatically; decide by Day 20 whether to cap them.
- **The debug console must be deleted or excluded before submission.** `DebugConsoleOverlay` disables itself outside development builds, so it is not a shipping risk today, but leaving dev UI in a store binary is the kind of thing that reads badly in review. Hard deadline Day 22.
- Building count (3 for MVP, architected to scale to 5) and building effects remain finalized.
- **`Docs/tools/guild_model.py` is a copy of the balance numbers and will drift.** It replicates the loop well enough to have found the Day 4–5 structural failure, but it is a model, not a source of truth: update it in the same commit as any asset change. A drifted model is worse than no model, because its answers stay confident. **It is also a copy of a *player*, and that half drifts too** — Days 10–11 found its hiring rule had never once bought a non-Common adventurer, so a question about rarity it appeared to answer, it had never actually asked. When a run says content is pointless, check the policy can reach that content before believing it. **Day 13 is the case that points the other way and is worth holding beside it:** the policy was blamed for four days for what the *content* was doing, and the rule that had never bought a Champion was correct about the game as priced. So the check runs both directions — a model that says something is pointless is claiming a fact about the content *and* about the player, and either half can be the one that is lying.
- **The Training Room's power bonus is flat, and that is a levelling mechanic pointed the wrong way.** `Adventurer.PowerWith` **adds** `AdventurerPower` to every adventurer, and a flat bonus is by construction worth most to the weakest person it touches: +331 on a maxed Militia Recruit's 71.6 is +462%, on a maxed Champion's 1,145.6 it is +29%. So the building whose stated job is raising each adventurer's Power is in practice an **equaliser**, and the authored 16x rarity ladder is worth **x3.7** by the time the guild is finished (x16.0 / x14.8 / x12.2 / x7.7 / x3.7 at Training Room 0 / 10 / 20 / 30 / 40). The same shape has an uglier second face: a new hire arrives carrying the full guild bonus for free, so a **25-gold Militia Recruit is worth +331 power at a finished guild — 0.1 gold per point** — against 1,576 to 7,977 gold per point for a training level. Only the Inn's sixteen-bed cap stops that being the dominant strategy. Making the bonus multiplicative fixes both halves and is what the building already claims to do; it costs one line in `Adventurer.PowerWith`, a neutral base of 1.0 in `GuildState`, a `ModifierKind` on the asset, re-derived Recommended Power on all five quests, re-spaced tier gates, four canaries, and about seven hours of tail before any re-tuning. **Deferred to Day 21** — the compression bites hardest at Training Room 30–40, by which point the roster has converged to Legendary anyway, so what it costs today is feel at the climax rather than balance. §6 of `Docs/Day13_First_Balance_Pass.md` has the numbers. Note that it changes what a **stat** means rather than what content consumes it, which is the harder version of the architectural bet, and that `GuildStat` is persisted by ordinal — reinterpret the value, never renumber the enum, and add a save fixture on the day it happens.
- **`AssetValidation` is the eighth thing in Core and the first that exists only for the editor.** It is small, it is `[Conditional("UNITY_EDITOR")]` so it compiles out of a player build, and every feature already depends on Core — but it is worth naming here rather than letting it arrive unannounced, because Core is the assembly the whole architecture leans on and the bar for adding to it should stay high. If a second editor-only utility ever wants to join it, that is the moment to give them their own editor assembly instead.

**CORRECTION, issued Day 15 — the interface has never been drawn, so several things
this document calls verified were not.**

`GuildScreenController` reads `GetComponent<UIDocument>()`, and that component's Panel
Settings field is empty; the `PanelRenderer` beside it is the one holding
`GuildPanelSettings`. A UIDocument with no Panel Settings still hands back a valid
`rootVisualElement` — an orphan, attached to no panel and drawn by nothing — so the
screen built perfectly and silently into the void for fifteen days.

What that invalidates, stated plainly rather than quietly amended:

- **The Day 7 UI pass and its step 6 Week 1 checkpoint** are recorded above as *"run and
  passes"*. The parts that read files and assert state did pass. **Anything that required
  looking at the screen did not happen.**
- **Day 12's four manual checks** in §3 of `Docs/Tests.md` — `button--destructive`
  resolving to the negative colour, a sixteen-row party picker fitting the phone, the
  selected row being unambiguous, the retire confirmation reading as informative — are
  listed as outstanding-but-doable. **They were not doable.** They still are not done.
- **The colour half of Days 10–11's step 6** is the same case.
- **Day 14's recall bug** is described in UI terms ("the order card kept rendering
  *Repeating*"). The fix is correct and the reasoning holds, but it was derived from the
  code and the debug console, not observed on screen.

Fixed by assigning the asset, and made unable to recur by two checks in
`GuildScreenController`: an immediate one on `_document.panelSettings == null`, placed
*before* the root check because the missing asset is the cause and the null root is only
one of its symptoms; and a deferred one a frame later on `_root.panel == null`, for the
cases a missing asset does not cover. The deferral is the Days 10–11 `OnValidate` lesson
verbatim — attachment happens across `OnEnable`, and **a check that cannot tell a
half-loaded object from a half-filled one is not a check.**

**The general shape, now met four times in four costumes:** `AssetValidation` crying wolf,
Day 13's canaries that watched no training cost, Day 15's `--checks` block looking for a
curve no room has, and this. **A failure whose only symptom is the absence of something is
not detectable, and will be found by accident or not at all.**

**Resolved on Day 16:**

- **The revenue engine is real, and the architectural bet took its heaviest load yet without moving.** Four rooms earning gold per hour, one guild-wide payroll shared between them, wages priced against what a customer is worth, net floored at zero — and **nothing was added to `BuildingDefinition` to make any of it work.** A room's seats, spend and demand are `BuildingEffect` entries like every other effect, so adding a sixth department is still one new `.asset` and zero code. `GuildState.Aggregate` gained no branch; it gained an early *return*, which is the opposite thing. The five feature assemblies are still Core-only, and `IdleGuild.Staff` is the sixth on the same terms. The engine sits in App because capacity is staff and demand is buildings, and teaching `GuildState` about a roster would put Guild → Staff exactly where fifteen days of discipline have kept a cross-feature reference out.

- **A per-room stat summed across the guild is a *plausible* wrong answer, which is worse than the failure this project keeps meeting.** Seats, spend and demand belong to a room; adding five rooms' seats together is arithmetically fine and means nothing, and sixty-eight seats reads exactly like a real figure. Four times now the shape has been *a failure whose only symptom is the absence of something* — `AssetValidation` crying wolf, Day 13's canaries watching no training cost, Day 15's `--checks` block looking for a curve no room has, and an interface never drawn. This is the sibling case and it needs the opposite treatment: `GuildStatScope` names the three per-room stats in Core, `GuildState.Aggregate` **refuses** to produce them so they read a loud zero, and `EffectFor(building, stat)` is the only sanctioned read. A room reading zero seats earns zero gold and is noticed in ten seconds; a room reading five rooms' seats is what ships. An invariant over the enum itself makes appending a stat without deciding its scope a red test rather than a room panel bug.

- **Two of the charter's own mechanisms had gone stale, in exactly the way Day 15's `--checks` block had.** §4 names `Revenue` and `ServiceDemand` as the stats to append; **there is no `revenue` curve on any room and there never was** — revenue became `seats × spend` when §3.1 split demand from capacity, and §4 did not follow. And §3.1 says `wages/hr = Σ each staff member's wage`, which `wagesPerHour()` has never done: it prices wages against `capacity × averageSpend × WAGE_SHARE` and never reads the per-employee `wage` field at all, making it dead data carried in `content()` and read by nothing. The model produced every tuned number in the project, so the model won both and the charter carries corrections rather than quiet amendments. The consequence worth keeping: **`StaffDefinition` has no wage field**, because a second source of truth for a derived number is precisely how a ratio authored in one place and paid for in another goes unchecked for four days.

- **The tap needed a queue, which is the one place the model could not be copied.** The model treats tapping as a *rate*, which is right for a simulation and useless in a game — unserved demand is customers **per hour**, and a fast thumb would draw an hour of custom out of it in three seconds. So the rate fills a queue (people waiting at the bar, which is what unserved demand physically is) and a tap serves one. The queue is **capped**, because coming back from eight hours away to a wall of free gold would make the tap a reason to close the game, which is the exact inversion of what it is for. Everything else follows the model's placement, which was already right: capped by unserved demand so it cannot invent custom, worth nothing once staff cover the room, touching neither of the other two levers, and counted inside room income so the thumb cannot quietly move the 70/30 split.

- **`JsonUtility` leaves an absent array null, and that would have taken all four save fixtures red.** Every checked-in fixture predates the revision and carries no `Staff` key, so an unguarded `foreach` over `data.Staff` throws on each one. Guarded — and the half that matters more: **a null payroll is not counted as a repair.** A guild that genuinely had no staff must not be reported as damaged, or `save_real_session.json` and `save_day14_played_in.json` both go red for having been written honestly. `SaveSchema.CurrentVersion` still has never moved, on the fifth occasion it could have.

- **Idle income needed a mutation that announces nothing, and `CurrencyChanged` had asked for one since Day 2.** Its own remark reads *"Idle income accrues continuously; publishing per frame would flood the bus for no benefit."* Before the revision nothing accrued continuously, so no caller had ever needed it. `PlayerEconomy.Accrue` is that path, with the rule written on it: only the clock may call it, and only for income the player did not ask for. A tap goes through `Grant` and announces itself, because a tap is a decision and wants to land visibly.

**Resolved on Day 15:**

- **The model was measuring its own clock.** Contract durations rounded *up* to a whole
  integration step — Rat Cellar is 31.5s and took a 60s tick, a 1.9x throughput penalty on
  the entire adventurer economy that vanished as the step got finer. So `tuned_params.json`
  gave Village 30 minutes and rooms 68% at step 60, and 59 minutes and 82% at step 5, with
  the Capital **never reached** at step 30. Both figures §6C called "the two hardest
  targets, met" were properties of the step size. Fixed by carrying the remainder in the
  contract cycle and the arrival clock, and the loss is now the **median across steps
  (10, 30, 60) plus a penalty on the spread** — because a single run of a chaotic system
  measures the run. The late game genuinely is chaotic: Capital and maxed do not converge
  as the step shrinks, because a greedy policy on a compounding economy flips purchase
  order on tiny timing differences. Treat any Capital or maxed figure as ±30%.

- **`payback()` said seconds and returned hours, and that is why no reserve ever bound.**
  The ranking never noticed, because a ranking only needs the *order* and gets the same
  order in either unit — so the one place the units could have been caught was the single
  comparison against `reserveDeadline`, which is in seconds. The guard read
  `0.755 hours <= 21.8 seconds` as true and let every candidate walk through every reserve
  the model held. **§6C finding #11 was never fixed; a 60-second tick had been hiding it**
  by carrying gold past the 39.44-gold Potboy and the 40-gold adventurer inside one step,
  which put the right branch first by luck of ordering rather than by rule. The reserve's
  deadline was also computed only when a *room* requirement was unmet, which at
  `gate_scale 0.5` was never. Day 13's shape, one level down: **a quantity produced in one
  unit and compared in another, where the producer's own docstring named the right one and
  nothing ever read it against the consumer.**

- **The dead twenty-two minutes was a subtraction, and the tuner could not reach it.**
  150 starting gold, minus 143.85 for Tavern L1+L2 and Front Desk L1, leaves **6.15 gold**
  against a 40-gold next purchase at **1.54 gold a minute** — 21.9 minutes, exactly the gap
  in the trace. Every one of those numbers was hardcoded in `content()` and **absent from
  `SPEC`**. Five dials added (`start_gold`, `open_cost`, `late_cost`, `hire_base`,
  `rep_village`), plus `slots_base` / `slots_lin` for the Tavern's staff-slot curve, which
  was fixed at base 2 / +1.4 and capped the opening at two cheap purchases no matter what
  else moved — that is why the worst silence would not fall below six minutes for four
  rounds. **Worth remembering: a search that keeps returning to one answer may not be stuck
  in a basin; check that the problem is inside the space at all.**

- **A bound enforced on one path and not the other is not enforced.** `gate_scale`'s floor
  was raised 0.5 → 0.9 because the search chose 0.5 in every winner across nine seeds, and
  at 0.5 the Village gate is `tavern 2 / front_desk 1` — cleared by starting gold before
  the player acts. But `search()` clamps *perturbations* and leaves the *incumbent* alone,
  so resuming from a saved point carrying 0.5 smuggled it through two more rounds and into
  a promoted configuration. `fill()` now clamps on resume. Clamping it honestly cost the
  loss 11.4 → 273, which is the finding: **every good number produced before that point was
  partly bought by a tier gate the player never had to reach for.** The re-tuned
  configuration has `gate_scale` at **1.21** — tighter than the authored values.

- **The model priced a rest it never charged, and froze a contract choice it should have
  revisited.** `rest_of` fed every ranking and was never served in the simulation, so the
  Barracks' recovery stat was inert; and `syncQuests` picked a standing order's quest at
  creation and nothing ever re-asked, so an order created in Village was still running Rat
  Cellar in the Capital. Both fixed. Day 12 had already established that a party is fixed
  for the life of a *run*, not of the assignment — the model was behind its own game.

- **The `--checks` block was looking for a curve that does not exist.** It tested for
  `revenue`, which the revision replaced with `seats` x `spend` when it split demand from
  capacity, and never checked `seats`, `spend`, `recovery` or `maxTier` — so the two curves
  that *are* the revenue engine went unwatched, and it printed only the curves that still
  moved, meaning a dead level read as a blank line. Day 13's lesson inside the model's own
  check block.

- **Tapping is not new scope; §6B already sold it.** *"A bound spirit that minds a room
  while you are away: collecting takings... a free player can do everything a payer can and
  simply has to tap."* For a familiar that collects takings to be worth a Boon, the takings
  must otherwise need collecting — so the monetisation pillar was **load-bearing on a
  mechanic neither the code nor the model had**. Modelled as **throughput**, which is the
  lever that already had a home for it (`baseService` is the guildmaster working the bar).
  That placement makes it capped by unserved demand, worth exactly zero once staff cover
  the room, and harmless to §3.1's three-levers rule. It decays on its own — no late-game
  balance problem to tune away.

- **The icon wire found the Sprite Mode default.** This project imports textures as
  **Multiple**, so Unity's auto-slicer cut one tankard into two sprites and `_icon` had no
  whole image to point at. §5 of the art brief already specifies Single; nobody could have
  known it was not the default, because **no texture had ever been imported into this
  project**. On Day 17 that would have hit every icon with a detached element — including
  most of a five-portrait ladder whose job is to grow more ornate as it climbs. Exactly
  what moving an hour of Day 17 into Day 15 was for.

**Resolved on Day 14:**

- **The recall button was a shipping bug, not a debug-console one.** `QuestsView` and the debug console both call `QuestDispatchService.Cancel`, which deliberately lets the run in flight finish — but **published nothing**, so `GuildScreenController` never rebuilt, the order card kept rendering "Repeating" and kept offering the button that had just been pressed, and the quest visibly carried on. Press, nothing changes, indistinguishable from a dead button. The state did eventually correct itself when `QuestCompleted` fired, which for Dragon's Roost is six minutes later. **Day 12 wrote down this exact argument** for `QuestPartyReformed` — *"without it the card would keep listing the old party until some unrelated event happened to redraw it"* — and added the event for the action it was building, not for the one already beside it in the same file. `SetRepeat` had the same hole. Fixed with `QuestOrderChanged`, a "Standing down" badge state (a recalled order was rendering as *One-off*, a word the player did not cause), a disabled Recall, and a `badge--pending` token, because `badge--locked` is red and would have made an acknowledged instruction look like a failure. The general shape: **an action whose effect is deferred has to say so on the thing it acted on, not only in a toast that scrolls away** — the Day 6 destructive-action lesson with the sign flipped.

- **The playtest produced the project's first measurement of modelled time against lived time.** The Day 14 save records **17.6 minutes** of real play to reach Town; the model predicted **8**. A 2.2× gap, and almost certainly not a bug — the model buys the instant it can afford something, a person reads and thinks and misses windows. **Modelled minutes are not lived minutes**, which matters for every pacing target the project sets. One noisy data point from a session that included inspection; re-measure before trusting it.

- **The one-economy game has a permanent record.** `save_day14_played_in.json` — a genuine 17-minute session that reached Town, seven contracts, six successes and **one honest failure** that nobody hand-building a fixture would have included. Two tests give it a job: one pins **zero repairs** so that when the revision deletes the Training Room the repair count changing is a red test with a number in it, and one pins the lifetime counters separately because they live on the clock rather than the world, where a restore that rebuilt the guild perfectly while zeroing them would pass every other assertion in the file.

- **Git LFS is set up, a day early and while the window was clean** — `git ls-files` confirmed no binary had ever been committed, so there was no history to rewrite. 25 patterns; **`.meta` deliberately excluded**, because routing sidecars through LFS makes every asset's metadata a pointer file and breaks diffing on the thing you most need to diff. Worth remembering: **git treats an undefined filter as a silent no-op**, so a commit made before `git lfs install` looks completely successful and puts every PNG into history whole.

- **Apple Developer Program enrollment is approved.** The one item on §04 whose delay was never yours to control. Everything remaining on that list is work rather than waiting.

**Resolved on Day 13:**

- **The ten-hour bracket was a price tag, not a policy question.** Day 12 handed over two model runs as the extremes of one decision and asked Day 13 to choose between them. Neither was choosable, because the ladder underneath both was priced wrong: **each rarity band doubled power and tripled training cost** — bases 20 / 60 / 180 / 540 / 1620 at a common 34% growth — so gold per point of power climbed 1,236 → 1,854 → 2,782 → 4,175 → **6,268** across the five archetypes, and a Legendary bed cost **81x** a Common bed for **16x** the power. Rarity was strictly dominated on the gold axis for the entire game. That is what made the greedy rule look like an arbitrage bug: it was buying the best archetype available, which is what a player does, and being charged ten hours for it. **The behaviour was never wrong; the price of the behaviour was.** With the bases at 20 / 40 / 80 / 160 / 320 the multiple matches — each band doubles power and doubles the gold to train it out — g/power is flat at 1,236 → 1,249, and strict, greedy and pragmatic policies land inside eighty minutes of each other with greedy and pragmatic one minute apart. **The general shape is worth more than the fix: a ratio authored in one place and paid for in another will not be checked by anybody looking at either.** Power lives on `_powerByLevel`, price on `_trainingCostToReachLevel`, and in four days of hunting this exact symptom nothing had ever divided one by the other.

- **Three days had each blamed something else, and each was true and none was the reason.** Days 8–9 blamed the Training Room's guild-wide bonus, Days 10–11 blamed the Inn's bed ratchet, Day 12 blamed an economic ceiling it had itself created by removing the structural one. All three looked at power, at gates and at the player. None looked at the price list. Worth remembering the next time a symptom survives three fixes: **a diagnosis that keeps being nearly right is a sign the search is in the wrong file, not that the fix was too small.**

- **The "economic lock" on the top rarity band was six percent wide.** `guild_model.py`'s swap rule required a **level-1** replacement to already beat the incumbent. At a maxed Training Room a level-1 Dragonsworn Champion is 379.4 against a maxed Militia Recruit's 403.0 — it wins at **level 3**, two training levels and about a thousand gold at a point in the game where the guild earns that in seconds. So Day 12's report that the structural lock had become an economic one described a rule that never looked past level 1. `switching_cost()` replaces it with the hire plus training the replacement to the first level that beats what the incumbent has already been trained to — no threshold, no magic number, and sunk training respected without the rule being blind to the fact that replacements can be trained too. **Both of Day 12's rules were straw players standing either side of the real one**: the rejected one ignored the price of catching up, the shipped one priced it at infinity.

- **That no canary moved is the finding, not the reassurance.** A pass that moved everything-maxed by four and a half hours and collapsed a ten-hour spread to eighty minutes disturbed no value-asserting test, because **no canary had ever watched a training cost** — five of the seven watch quest resolution, one the Inn's beds, one the rarity *power* ladder. Two tests close it, deliberately of different kinds: `AHigherRarityBandNeverCostsMoreGoldPerPointOfPower` is an **invariant** that any honest future retune passes untouched, and `TheTrainingLadderReadsAsWritten` is the **canary** that catches one mistyped figure in one asset — which the invariant would sail past, since a slip scaling all five bands equally leaves every ratio intact. **A canary set that does not watch a value is quieter than no canary set, because its silence reads as a pass.**

- **The patient/impatient fork is now a schedule rather than a destination.** Both profiles finish with sixteen Legendaries; impatience costs about eighty minutes and buys nothing that cannot be bought later, and — for the first time — the impatient player reaches Capital **later** (6h56m against 5h54m) because they spent gold on Battlemages they went on to retire. That ordering is what Day 12's retire action was reaching for and never showed. Below a ×2 band multiple it inverts again: at ×1.75 and ×1.50 the impatient player finishes the game *first*, which is why ×2 is the value and not merely a value.

**Resolved on Day 12:**

- **The four `SaveFixtureTests` do run.** Carried into this session as "written and compile clean, but no record of them ever having been run — confirm the suite reports 47 rather than 43 before trusting them." The Day 12 baseline reported 47, and the post-change run reports 64 against seventeen added tests. So the fixtures are live, including the one pointing at content no build has ever had — still the only thing that has ever exercised the Day 6 repair path against a file that actually needed repairing.

- **The roster is no longer a one-way ratchet, and the model says the fix is narrower than it looks.** `TryDismiss` and `TryReformParty` are both service methods over machinery that already existed, and the interesting part was never the code. Retiring **refuses** while somebody is out on a quest or belongs to a standing order rather than cascading through their order, because the cascade's naive form — remove the member, leave the order standing — is this project's own recurring failure: `TryStartRun` would have returned false for the rest of the run with a standing order on screen that simply never went out again. Re-forming is what releases them, so the two halves are one route and there is a test that walks it end to end. Nothing is refunded; reversibility was what the roster lacked, not a rebate. **What the model then found is worth more than the feature**: adding the action moved everything-maxed from 20h31m to 22h50m and 15h30m to 17h45m *on unchanged assets*, because retiring is a gold sink, and the impatient player **still** never fields a Legendary — beds free up, but trained Battlemages out-earn a level-1 Champion, so the structural lock became an economic one. **A reversible decision is not the same as a cheap one**, and Day 13 inherits the question of which way that should lean.

- **Re-forming a party mid-run was free, and the reason is a Day 4–5 decision paying out a second time.** `ActiveQuest` snapshots its own party at dispatch and `SimulationClock.SendPartyToRest` sends *that* snapshot home rather than reading the assignment — so replacing an order's party leaves the run in flight untouched: no recall mid-dungeon, no timer moving under the player, no reward recomputed. `QuestAssignment.MemberInstanceIds` was documented as fixed for the life of the *assignment*; it was always really fixed for the life of a *run*, and nobody had needed to notice. Deliberately **not** gated on the order being idle, because the window between runs of a repeating order is a few seconds of rest and an edit a player can only make by catching that window is an edit they will never make.

- **Two behaviour narrowings that went past the brief, both reversible and both written down.** A party is now **exactly** the size the quest asks for rather than at least — previously unreachable, because no caller could assemble an over-size party by hand and so nothing had to say no; the picker can, and every duration and failure figure in the game was derived against the number on the asset. Widening it is Quest Board territory and a design decision for a later day, not a side effect of building a screen. And **"Send a party" now takes the strongest free adventurers rather than the first on the roster**, which is both better and what `guild_model.py` already assumed — the two disagreeing is how a modelled arc stops describing the real one.

- **Nothing to migrate, and the reason is worth stating.** `SavedAssignment.MemberInstanceIds` is unchanged in name, type and meaning: on disk it has always meant *who is on this order*, and all that changed is that the running game can now write a different answer into it. So no version bump and no new fixture — §4 of `Docs/Tests.md` asks for one when the format or the meaning of a value changes, and neither did. `SaveRestore` still builds assignments directly rather than through `TryDispatch`, so a save holding an over-size party would still load and still run; **restoring repairs rather than refuses**, and refusing a guild over a party-size rule invented on Day 12 would be the wrong trade.

**Resolved on Days 10–11:**

- **Most of the verification pass is now a test suite, and it went green first run.** 43 EditMode tests in 46 ms, against the **shipped `.asset` files** rather than fixtures built in code — which is the point, since every content failure this project has had was a wrong value in an asset and a hand-built fixture would have been written from the same misreading. The rule that keeps it useful: **assert the shape, not the number.** No dead levels, gates that only tighten, a ladder that doubles, an opening that is solvent — those survive Day 13 and Day 21 moving every figure in the game. The handful that assert *values* are tagged `[Category("BalanceCanary")]` so a balance pass can find them in one filter; updating one of those is part of the work, updating an invariant is a warning. `Docs/Tests.md` carries the rest.
- **Save compatibility now has real files behind it.** `Tests/Editor/Fixtures/` holds three: a genuine play session, a roster sitting at the old Max Level 10, and one pointing at content no build has ever had. Round-tripping today's capture into today's restore proves only that those two agree with each other; compatibility needs a file this build did not write. Worth knowing for every future change: **`SaveSchema.CurrentVersion` has never been bumped**, because no field has ever changed shape — Days 10–11 changed what a *value* means, which needs no migration and is exactly the kind of change that slips past. Add a fixture whenever the format or the meaning of a value in it changes; they cannot be recreated once lost, only approximated, which is how the second one came to be synthesised after an autosave overwrote the original.

- **The `OnValidate` warnings had been crying wolf since Day 4–5, and are now quiet.** Every definition asset was reporting nonsense about itself — an empty Id on `Tier_City`, no tiers listed on a fully populated `GameContent`, a single-building gate on a `Tier_Village` that requires three. The cause: **`OnValidate` fires while Unity is still deserialising the object**, during an import-worker pass and on every domain reload, and in that window every serialised field reads as its type default. Day 4–5 met half of this and switched `GameContent` from dereferencing `StartingTier` to counting `Tiers.Length` — true as far as it went, but the array reads empty in the same window, so the warning came straight back and had been firing ever since. **The distinction was never *what* the check looks at; it is *when* it runs.** `Core/AssetValidation.WhenLoaded` now defers each self-check by one editor tick through `EditorApplication.delayCall`, de-duplicated per asset because `OnValidate` fires several times per import and each firing logged the line again. Clamps stayed inline — clamping a field that currently reads zero to zero is harmless, and a clamp has to apply the moment a value is typed. Worth remembering in the general form, because it is not really a Unity quirk: **a check that cannot tell a half-loaded object from a half-filled one is not a check** — and the cost is never the noise itself, it is that crying wolf on every reload teaches you to scroll past the console, which is where the real warning will be sitting on the day something actually breaks.

- **The Day 8–9 pacing figures were about 13% optimistic on the tail, and the corrected ones are the baseline now.** `guild_model.py` chose one quest for the whole guild and judged it using the *strongest* party's power — exact while every adventurer is identical, wrong the moment they are not, which is the situation rarity creates. Each party now picks its own work. On **unchanged** Day 8–9 assets that alone moves Village→Capital from 4h07m to **4h41m** and everything-maxed from 17h21m to **19h37m**. Compare future runs against those, not against the published numbers.
- **The §3 tier-4 quest spec was wrong and had to move.** At Recommended Power 420, three adventurers carrying only the Training Room's maxed +331 already clear the 4× speed clamp with no archetype levels between them — so above it, duration stops falling, failure is already zero, and the entire rarity ladder plus the last fifteen Training Room levels buy nothing measurable. Recommended Power is 1,250, with gold and reputation raised to match. The general lesson is the Day 8–9 one pointed the other way: **a quest cannot be specced against adventurers that are about to change**, just as a building tree could not be balanced against fixed quest rewards.

**Resolved on Day 4–5:**

- **`[Min]` on `double` fields, removed.** Unity's Min drawer renders whatever it decorates as a *float* field, so a double-backed cost or reward was being truncated to float precision on every Inspector draw — roughly 7 significant figures, which four tiers of idle-game numbers would have crossed. Found while writing the authoring tables, before any asset had been created; the four affected fields now clamp in `OnValidate` instead, with a comment saying why. Worth remembering as a general rule: attributes that ship with the editor are not all `double`-aware.
- **The opening deadlock, avoided in data rather than code.** Housing Capacity has a neutral base of zero, so a guild with no Inn has no beds and can recruit nobody — and without adventurers there is no gold to build the Inn. Rather than special-case a starting bed in `GuildState`, `GameContent` grants starting gold (150) and the Inn is simply the first thing the player buys. No code branch, and the tutorial writes itself.

**Resolved during the Day 10 verification sweep:**

- **`Guild.unity` is in Build Settings** at index 0, enabled. Closed well ahead of its Day 25 deadline; an empty scene list would have shipped a black build.
- **Use Fixed Random Seed is off.** It was correct for verification and wrong for balance judgement, which matters from Day 13 onward. The seed field stays on the component for when a reproducible run is wanted again.

**Resolved on Day 6:**

- **The last-seen timestamp is out of PlayerPrefs** and inside the save, which means the stamp and the world it describes are written in one atomic step and cannot disagree. The failure that removes: a save that succeeded beside a stamp that did not, paying offline income for time the guild had already lived through. The old key is **deleted rather than migrated** — the builds that wrote it never persisted a guild, so its value describes an absence nothing was there for, and honouring it would hand out earnings for a guild that did not exist.
- **Saving is driven from three places, and it takes all three.** Pause is the reliable hook on iOS; `OnApplicationQuit` is not called when a player swipes the app out of the switcher, and a crash calls neither. A 30-second autosave bounds the loss when none of them fire.
- **Wiping progress means wiping the guild, not the save file.** Found later, while walking through this document's own verification pass: the debug console's delete button removed the file and left the world running, so the next autosave — or simply quitting — wrote the same guild back over the gap and the deletion undid itself within thirty seconds. `GameBootstrap.StartNewGuild()` now resets the world through `SaveRestore.Reset` and re-saves immediately, and first launch shares that path so "new guild" means one thing. The Week 3 settings screen's reset-progress option should call it rather than `GameSaveService.Delete`, which stays the raw primitive it always was. Worth remembering as a general shape: **a destructive action that does not also invalidate the live state it describes will be undone by whatever writes that state next.**
- **A corrupt save is quarantined, not deleted.** The unreadable payload is kept under a timestamped key and the copy behind it — `FileSaveStore` keeps one through every atomic write — gets its turn. A player who loses a guild should be able to see it was not thrown away, and during Weeks 2 and 3 the file that failed to parse is the only evidence of why.

**Most recent continuation prompt:**

```
I'm continuing work on Idle Adventurer's Guild, a solo Unity mobile game. The design was
revised on Day 14 from a fantasy management sim into an IDLE HOTEL TYCOON with an
isekai/anime guild-hall theme. Submission target Day 38, buffer through Day 42.

The project lives at ~/Idle_Adventure_Guild. Read GUILD_LEDGER.md in the repo root in
full first - it is the source of truth for what EXISTS, and section 06 carries a
CORRECTION that invalidates several things earlier entries call verified. Then read
Docs/Vision_Revision.md in full - the source of truth for what we are BUILDING, which
supersedes sections 02, 03 and 05 of the Ledger, and which now carries three CORRECTION
boxes of its own in 3.1, 4 and 6C where it described mechanisms the tuned model does not
use. Then Docs/Day16_Staff_And_Revenue.md (what was built yesterday and the one finding
that changes what you can trust), Docs/Day16_Followup_Solvency.md (a playtest walked into
an unrecoverable state; this is the fix, and the new Principle it produced), and
Docs/Tests.md (short, changes how you verify things - note section 7 on the tests that
are Ignored on purpose).

Current position: Week 3, Day 17 - continuing the revision.
Last completed: Week 1 in full; Days 8-9 building trees; Days 10-11 tier transitions plus
the test suite; Day 12 recruitment and assignment UI; Day 13 first balance pass; Day 14
playtest and the design revision; Day 15 economy tuning, the icon wire, and the discovery
that the interface had never been rendered; Day 16 the Staff assembly, the revenue engine,
and a follow-up adding the crown's stipend after a playtest found a dead end. Unity 6000.5.0f1 / URP 2D, private GitHub repo via GitHub Desktop, Git LFS live.
NINE assemblies now: six feature assemblies depending on Core and nothing else
(Economy, Adventurers, Quests, Guild, and as of Day 16 Staff), IdleGuild.App above them
holding composition and cross-feature transactions, IdleGuild.UI above that,
IdleGuild.Tests.Editor above everything.

WHAT EXISTS AND WHAT DOES NOT, precisely, because this is the confusing part:
the revenue ENGINE is built and NO ROOM USES IT. Day 16 added IdleGuild.Staff, the
TradeService revenue engine, StaffService with hire AND dismiss, TakingsService (the
tap), five appended GuildStats, four new tier fields, and the save fields - and touched
NOT ONE .asset file. So the game that actually runs is still the one-economy version:
three buildings, gold only from contracts, individual adventurer training, plus the
crown's stipend. Day 17 is where the assets start to change.

AND NOTE WHAT THAT MEANS FOR THE TAP: TakingsService shipped tested and documented and is
INERT. No room produces ServiceDemand and no tier carries a base service, so total demand
is zero, unserved demand is zero, the queue never fills and TryCollect returns false
forever. It goes live with the room assets and not before. ANY PACING MEASURED BEFORE THEN
IS MEASURING THE MAILBOX, NOT THE GAME.

Next task: Day 17, in this order.
  1. THE OWED INTERFACE HAND-CHECK, FIRST. It has now been deferred twice and Day 17 is
     the day it stops being free, because this is the day that sizes assets against a
     judgement nobody has made. About 25 minutes in the Editor: Days 10-11's colour half
     of step 6, Day 12's four visual checks, and whether the 96px room icon reads
     correctly beside a 28px title. Claude cannot do this; it needs your eyes.
  2. Arrivals - recruitment from shop to "who is drinking here tonight". Section 3.3 and
     6.3 of Vision_Revision.md. Two of section 6B's three familiar-automations still have
     no manual version: hiring from the crowd, and dispatching contracts.
  3. The five rooms as assets - and this is the day save_day14_played_in.json goes red,
     because deleting the Training Room is exactly what its zero-repairs pin was put
     there to catch. UNDERSTAND ITS NUMBER BEFORE UPDATING IT.

Three requirements on the BUILD carried forward, all still open:
* the tier panel must show what the gate is still MISSING - and this is now concrete:
  TierAdvancementService.Preview returns RequirementsNotMet and names nothing. It owes a
  shortfall description (which building, how many levels, how much reputation), and it
  belongs in the SERVICE not the view, because views hold no rules.
* the Barracks must visibly look like it makes money - cannot be closed until the Front
  Desk is authored, because the number that fixes it is what the Barracks is worth
  through the commission. ContractCommission is declared with no producer, deliberately.
* the TAP must exist - HALF DONE AND CURRENTLY INERT. Collecting takings ships
  (TakingsService.TryCollect, serving one customer from a capped queue) but cannot fire
  until a room produces demand. Hiring from the crowd and dispatching contracts still need
  their manual versions.

A FOURTH, added by the Day 16 playtest and now written into section 01 of the Ledger as a
Principle: NO SEQUENCE OF CHOICES MAY LEAVE THE PLAYER UNABLE TO MAKE PROGRESS. There is
always an action that improves the player's position - early that is the crown's stipend,
late it is letting staff go, which is free and is half of why dismiss exists. It is a
property rather than a balance figure and is asserted in SolvencyTests against the shipped
catalogue. It governs decisions not yet made: whether a room can be sold, whether wages
can bankrupt you, whether prestige can strand a run.

THE ONE THING FROM DAY 16 THAT CHANGES WHAT YOU CAN TRUST:
the staff ladder in tuned_params.json is UNMEASURED, not tuned. Gold per point of service
climbs 0.47 -> 2.06 -> 8.63 -> 32.69 across the four tiers, and the tuned configuration
hires 105 Potboys and never buys a Server, Barkeep or Steward at ANY integration step.
It is not a pricing problem - flattening the ladder still gives 98 Potboys and one
Server, and starving slots to four still gives four Potboys. tycoon_model.purchase() can
only ever APPEND staff, so slots once filled with the cheapest help are filled forever
and the ladder is unreachable at any price. Nothing has ever measured what the upper
rungs are worth. DO NOT AUTHOR STAFF ASSETS until the model has the dismiss action the
game now has - replace the least capable when the replacement is better the day it
arrives, which is the rule Day 13 landed on for adventurers. Section 6 of
Docs/Day16_Staff_And_Revenue.md has the tables. The ROOM curves are unaffected: what the
rooms see is the payroll's total service, and 105 Potboys deliver it.

The economy is otherwise tuned and Docs/tools/tuned_params.json holds the result. Verify
before you trust it:

    python3 Docs/tools/tycoon_model.py --profile --checks
    python3 Docs/tools/tuner.py 0 --resume --report

The second prints the opening trace and a per-step table. THE PER-STEP TABLE IS THE
POINT: the model used to give different answers at different integration steps - about
2x - and every headline figure in section 6C was an artefact of scoring at step=60. It is
now stable to a few percent across step 5 to 60 (Town 0h22-24m, Capital 5h19-29m, rooms
65-69%). If a future change makes those rows disagree again, that is the first thing to
fix and nothing else is trustworthy until it is.

Note there are TWO models on purpose. guild_model.py describes the game that actually
exists and stays until the rooms are authored. tycoon_model.py describes the game being
built. Retire the first when the second becomes true.

MODELLED MINUTES ARE NOT LIVED MINUTES - roughly 2.2x on the one noisy sample this
project has (Day 14 took 17.6 real minutes to reach Town against a predicted 8). Village
is 23 MODELLED minutes. Re-measure on the next playthrough; do not tune to modelled
numbers as though a player experiences them.

Testing: EditMode suite at Assets/_Project/Tests/Editor/ - 114 tests, all green, well
under a second, of which THREE are deliberately Ignored rather than green and say why
(section 7 of Docs/Tests.md). Run it (Window > General > Test Runner > EditMode > Run
All) before you start and before you commit. It asserts SHAPE rather than NUMBERS on
purpose; the value-asserting ones are tagged [Category("BalanceCanary")] and are expected
to move on a balance pass, while an INVARIANT moving is a warning - though Day 16 is the
case that shows a moving invariant can also be the system working, since
EveryGuildStatHasAPlayerFacingName caught five appended stats with no display name. It
loads the real .asset files through AssetDatabase rather than building fixtures - EXCEPT
TradeFixture, which builds rooms in memory because it tests mechanism rather than
content, and which carries the warning that NO shipped room produces seats, spend or
demand yet, so the value half of the revenue engine has no coverage at all. Four save
fixtures, including save_day14_played_in.json - pinned at zero repairs so that the
revision deleting the Training Room shows up as a red test with a number in it rather
than a silence. EXPECT THAT TEST TO GO RED WHEN THE ROOMS LAND AND UNDERSTAND ITS NUMBER
BEFORE UPDATING IT.

Known issues/blockers: the crown's stipend is sized for a build where nothing else earns
and should be re-checked the day the rooms land - recovering from an empty treasury takes
about twelve and a half minutes, pinned as a BalanceCanary; if that still bites once rooms
and the tap are live, the fix already designed is a HARDSHIP LINE that stops accrual above
a per-tier threshold. The whole game is 6h54m of modelled content against a 20-hour
target - a curve-length question for Day 22, stable across every step so it is not noise.
Tapping is 87% of room income across the first thirty modelled minutes, high enough to be
a design decision rather than a side effect - settle it before monetisation on Day 28.
The worst opening silence is 5 modelled minutes against a 2-minute target and looks like
a real frontier. Bundle ID and product name are still template defaults and are also the
save directory, so changing either strands every save (the Day 14 fixture has been
captured). Save files are plain text and trivially editable; the guild_save.json.corrupt-*
quarantine files are never capped. The debug console must be deleted or excluded before
submission - and note it was the ONLY way this game was playable for fifteen days and is
still the only place the trade layer, the payroll and the tap can be seen at all, so do
not remove it before the real UI has been exercised. Week 4 execution surface (device
builds, TestFlight, App Store Connect) is not solvable from Cowork and needs deciding by
Day 22. Ad network and IAP provider unchosen. Apple Developer enrollment IS approved.

Working arrangement (see section 06): this runs in Claude Cowork, whose shell is a Linux
VM with the project folder mounted - git exists but `unity` and `dotnet` do not. You
write and edit files and never run git, not even `git status`, which leaves index locks
that break my GitHub Desktop; tell me the commit message and I commit through the GUI.
When you add scripts, ask me to focus the Unity Editor so it imports them, then verify by
checking for Library/ScriptAssemblies/IdleGuild.*.dll and grepping Logs/ for "error CS" -
note that transient errors appear mid-write and what matters is whether any error line
comes AFTER the last "Finished compiling graph". Tests are the same loop: you write them,
I run them and paste failures. The Python models run fine on the Cowork shell.
ScriptableObject values can be written directly into the .asset YAML rather than retyped
through the Inspector, and a texture's .meta can be too - but note this project imports
textures as Sprite Mode MULTIPLE by default, which auto-slices any image with a detached
element into pieces. Set spriteMode: 1.

Follow the Principles section of the Ledger (Clean Code, data-driven ScriptableObject
architecture, event-driven decoupling, UI Toolkit/USS styling) without needing it
re-explained. Confirm your understanding of where things stand in a sentence or two
before writing any code.
```

### Session log

*Append-only history of what each session actually accomplished — a trail future instances (and you) can scan without re-reading full transcripts. Never edit a past entry, only add new ones below it.*

1. **PLANNING** — Concept locked (fantasy adventurers' guild, tycoon-style), architecture principles set (Clean Code, data-driven/modular, UI Toolkit + USS, Sonnet-plan/Opus-code), 4-week roadmap defined, Branch Expansion prestige loop scoped for post-launch. No code written.
2. **HANDOFF 1** — Building effects finalized (Tavern/Reward, Training Room/Speed+Success, Inn/Capacity; Quest Board/Armory designed, deferred post-MVP) and MVP building count locked at 3. Handed off from the planning conversation to a local Claude Code (Opus 5) session on the developer's Mac, where Unity Hub/Editor/Xcode are already installed, to begin Week 1 implementation.
3. **W1D1 — Foundation** — Unity project confirmed at `~/Idle_Adventure_Guild` (6000.5.0f1, URP 2D). Git initialised on `main` with Unity `.gitignore`/`.gitattributes`; `Library/`, `Temp/`, `Logs/`, `UserSettings/` verified ignored and signing material (`*.p8`, `*.p12`, `*.mobileprovision`, `GoogleService-Info.plist`) pre-excluded. Feature structure created under `Assets/_Project/` with an `.asmdef` per feature, every feature depending on `Core` alone. `IAdService`/`IPurchaseService` plus `NullAdService`/`NullPurchaseService` written into `Core/Services`. UI Toolkit needed no package — it ships in the Unity 6 editor. This Ledger moved into the repo as the working copy. Session ran in Claude Cowork, not a terminal: the shell was a Linux VM with the folder mounted, so all Editor and Xcode steps stayed manual.
4. **W1D2–3 — Core data model** — Written and verified compiling (five assemblies build, zero CS errors; UI has no scripts yet so emits no DLL). Core gained the shared vocabulary — `GuildStat`, `Rarity`, `ModifierKind`, `CurrencyType`, `ScalingCurve`, `IGuildStats` — plus a typed `EventBus` using generic static channels, and the cross-assembly event structs in `GameEvents`. Guild gained `BuildingDefinition` / `BuildingEffect` / `BuildingLevelRequirement` / `GuildTierDefinition` / `GuildState`; Adventurers gained `AdventurerDefinition` / `Adventurer`; Quests gained `QuestDefinition`; Economy gained `PlayerEconomy`. The Core-only dependency rule held throughout — `GuildState.CanAdvance` takes reputation as a `double` argument rather than referencing Economy, and Adventurers reads Training Room and Inn effects through `IGuildStats`. Post-MVP stats (`QuestSlots`, `MaxQuestTier`, `FailureRateReduction`) were declared now so Quest Board and Armory stay data-only later. Project published to a private GitHub repo and moved onto GitHub Desktop for commits. Learned that `git status` from Claude's side also leaves index locks, so Claude now runs no git at all. No ScriptableObject assets authored yet, so nothing is playable — Day 4–5 must create at least the three MVP buildings to have anything to exercise.
5. **W1D4–5 — Core loop logic** — The loop is written and compiles clean (six assemblies, zero CS errors, zero warnings). A seventh assembly, `IdleGuild.App`, was added above the features to hold the transactions that cross them — the features themselves stayed Core-only, so the compile-time wall and the Quest Board/Armory bet are intact, and the bet was explicitly re-checked: no field was added to `BuildingDefinition` and no branch to `GuildState.Aggregate`. Quests gained `QuestResolution` (duration scaling with the square root of party power, failure doubling at half the recommended power and vanishing at twice it, rewards scaled by Tavern yield), `ActiveQuest` with its outcome snapshotted at dispatch, and `QuestLog`. Adventurers gained an activity state, rest timers driven by the Inn, `AdventurerRoster` capped by Housing Capacity, and a per-adventurer training cost curve. App gained `GameContent`, `GameWorld`, `SimulationClock`, `QuestAssignment` plus the five services, `OfflineProgress`, `GameBootstrap` and `DebugConsoleOverlay`. The design decision worth remembering: the clock steps from event to event rather than in fixed slices, so eight hours of offline catch-up runs through exactly the same code as a single frame — there is no second offline formula that can drift from what the game pays while the player watches. Two things were found rather than built: `[Min]` on `double` fields was silently truncating costs to float precision through Unity's editor drawer, fixed before any asset existed; and Housing Capacity's zero base means a guild with no Inn can recruit nobody, resolved with starting gold rather than a code branch. Asset values, scene setup and an 11-step smoke test written to `Docs/Day04_Asset_Values.md`; the developer authored the fourteen assets in the Editor and the smoke test passed. Three things surfaced during that authoring pass and are worth knowing about. Reading the finished `.asset` YAML against the tables caught four transcription slips, one of them consequential: the Inn's Housing Capacity effect had received the *cost* curve, so a level-1 Inn granted 50 beds instead of 2 — a reminder that hand-copied curve fields are worth diffing rather than eyeballing, since a wrong effect looks exactly like a right one in the Inspector. Town and City had both kept Village's 3/3/3 gate, which would have collapsed the tier gate into a single check and let a player walk to Capital on Village-level buildings. And `GameContent.OnValidate` warned "no guild tiers listed" against a perfectly populated asset: it resolved `StartingTier`, which dereferences the tier references, and Unity leaves those reading null for a moment while it reimports a referenced asset — validation in `OnValidate` must count array entries, never follow references. Fixed by checking `Tiers.Length`.
6. **W1D6 — Save/load** — Written and compiling clean: six assemblies, zero CS errors, zero warnings, verified after import; the 10-step runtime pass in `Docs/Day06_Save_Verification.md` was still outstanding when this entry was written. A versioned JSON save now carries the guild tier, every building level, all four balances, the roster with levels, activity and rest timers, the quest runs in flight with their dispatch-time snapshots, the standing orders and the lifetime clock counters. `ISaveStore` and `FileSaveStore` joined the ad and IAP interfaces in `Core/Services`; `App/Saves/` gained the wire format, capture, restore, the migration ladder and the `GameSaveService` policy layer. Existing classes needed four small seams and nothing more, which the Day 4–5 read-only-state discipline had already paid for: `GuildState.RestoreState`, `SimulationClock.RestoreCounters`, `GameContent.FindTier` and a `GameLoaded` event. **The architectural bet was re-checked and still holds** — save/load spans all five feature assemblies, which is exactly the kind of pressure that would have forced a cross-feature reference, and it did not: capture and restore live in App, the features stayed Core-only, and nothing was added to `BuildingDefinition` or branched in `GuildState.Aggregate`. Four decisions worth carrying forward. **`JsonUtility`, not Newtonsoft** — no package to add before submission and no IL2CPP reflection worries, at the price of roughly seven significant figures on doubles, which the one precision-critical value dodges by being a `long` tick count. **The compatibility rule is about the schema file rather than the migration code**: fields are only added, never removed or renamed, which is what lets an old save deserialise into today's classes at all and makes most future changes need no version bump. **Restore repairs rather than refuses** — a save is not trusted to match today's catalogue, since a quest renamed in Week 2 orphans every save in the wild, so anything unresolvable is dropped and counted in a report instead of throwing. And **restoration is quiet**, which is a Day 7 constraint as much as a Day 6 one: loading a level-4 Tavern must not announce four upgrades, so a screen cannot build its first frame from change events and has to read state directly on `GameLoaded`. Two smaller things: the PlayerPrefs stamp was deleted rather than migrated, because the builds that wrote it never persisted a guild and honouring it would pay offline earnings for a guild that was never there; and saving runs off pause, quit *and* a 30-second autosave, because iOS calls `OnApplicationQuit` only sometimes and a crash calls neither. `Docs/Day06_Save_Verification.md` carries the schema, the compatibility rules and a 10-step pass whose last three steps deliberately corrupt the save and delete content out from under it — the Day 20 hardening item, brought forward to the day the code was written.
7. **W1D7 — UI Toolkit scaffolding** — Written and compiling clean: seven assemblies now, zero CS errors, zero warnings, verified after import; the 10-step pass and the two Editor setup steps were still outstanding when this entry was written. The grey-box interface exists: USS design tokens, a component stylesheet built entirely on them, and Guild Hall / Quests / Roster behind a bottom tab bar with the upgrade panel as an overlay raised from a building card. The structural decision of the day was **moving `IdleGuild.UI` above `App` and letting it reference everything** — a screen has to render a `BuildingDefinition` and call a service, which is the same cross-assembly pressure that created App, one layer up. The features stayed Core-only, so the bet is intact; the honest cost is that *views hold no rules* is now discipline rather than a compile error, and `GuildContext` exists to write that rule down where it will be read. Three things worth carrying forward. **Events set a flag and a 100 ms tick acts on it** — an idle game's numbers change continuously while its structure changes rarely, so polling values and rebuilding on demand beats either alone, and a handler that only sets a bool cannot take another subscriber down with it when `EventBus` abandons a publish on an exception. **The overlay reads effects off the asset** and evaluates the curve at the current and next level, so it explains a Tavern without knowing what a Tavern is — the data-driven architecture doing for the interface what it already does for the simulation, and the reason a badly shaped curve will now be visible in the game rather than only in the Inspector. And **every refusal says why**: the services' outcome enums become sentences through `Outcomes.Describe` and land in a toast, which is the payoff for those returns not being bools and the reason no disabled button in this game is silent. Two smaller decisions: the hierarchy is built in C# rather than UXML, on the reading that the styling principle is about USS versus the Inspector and is equally satisfied by code adding class names — with the side benefit that a mis-wired button fails at compile time instead of at runtime; and safe-area insets went in on the day the first screen was built rather than on Day 22, since the Editor reports the whole screen as safe and a tab bar under the home indicator is otherwise found during the bug bash with three weeks of screens stacked on it. Day 7 needs two Editor steps that cannot be done from a text editor — a Panel Settings asset and the scene wiring — both written up in `Docs/Day07_UI_Setup.md` along with a 10-step pass whose step 6 is the Week 1 checkpoint itself.
8. **W2D8–9 — Building trees** — Data only; no code changed, and all ten edited assets reimported clean with no `OnValidate` warnings. The first-pass curves were modelled end to end and failed structurally rather than numerically: **all three buildings hit level 10 at 2h15m while Capital was still two hours away**, so the back half of the game had nothing to buy and gold ended at 22 million against a cost curve that had stopped. Sixty-four purchase decisions in the whole game. The cause was Max Level 10 stretched across four tiers. The obvious fix made it worse in an instructive way — a uniform 40-level tree at 34% cost growth left **the top eleven levels unreachable**, time-per-level diverging, 200 simulated hours failing to close them — which exposed the real finding: **only the Tavern compounds.** Reward Yield multiplies gold without bound; Training Room power stops shortening a quest at four times recommended power and Inn recovery can only remove the rest half of a cycle, so charging geometric prices for bounded benefits is what put the tail out of reach. The trees are therefore deliberately asymmetric — Tavern 90 levels at 15% growth as the compounding spine, Training Room 40 at 19%, Inn 30 at 21% — with the tier gate doing the job it was always designed for, stopping the player tunnelling into the Tavern alone. Result: 195 purchase decisions, Capital at 4h07m, everything maxed at 17h21m, longest stretch with nothing to buy down from hours to 52 minutes, and every effect still improving at max level. **The reputation thresholds are derived rather than chosen** — 75% of what the guild actually holds when the building half of each gate closes, so reputation confirms the player has been questing instead of becoming the wall, since a player blocked on gold can spend their way out and one blocked on reputation can only wait. Three things worth carrying forward. The three MVP quests had to be re-costed on a buildings day, because a building tree cannot be balanced against fixed quest rewards — buildings only matter as a multiplier on what a quest pays. **The values were written straight into the `.asset` YAML rather than retyped through the Inspector**, which is the direct lesson of Day 4–5's four transcription slips, one of which handed the Inn's Housing Capacity effect the cost curve and gave a level-1 Inn 50 beds. And the model is now in the repo at `Docs/tools/guild_model.py`, because the shape of a curve is not visible in the Inspector and no amount of staring at the asset would have shown any of this. Days 10–11 inherit a written spec for the tier-3 and tier-4 quests the curves were tuned against, plus two problems the modelling surfaced: higher-rarity archetypes are currently pointless next to the Training Room's guild-wide bonus, and adventurer Max Level 10 saturates long before the buildings do.
9. **Verification sweep** — All three outstanding passes run in one sitting: Day 6 save/load, Day 7 UI, Day 8–9 asset values. The Day 7 Editor setup was completed alongside them, and two long-standing open items were closed while the scene was open — `Guild.unity` added to Build Settings at index 0, and Use Fixed Random Seed unticked. **One real bug found, on step 1 of the pass most likely to be skipped.** The debug console's "Delete save" removed the file and left the world running, so `TickAutosave` wrote it back within thirty seconds and `OnApplicationQuit` wrote it back immediately on stopping — meaning delete-then-restart returned the old guild, and the delete was never durable. `GameBootstrap.StartNewGuild()` now resets the running world through a new `SaveRestore.Reset` and re-saves at once, with first launch sharing that path so "new guild" means one thing in both places; `GameSaveService.Delete` stays the raw primitive. The general shape is worth remembering because it will recur: **a destructive action that does not also invalidate the live state it describes will be undone by whatever writes that state next.** Nothing else misbehaved — the loop, the offline path, the corruption recovery and the content-removal repair all behaved as written.
10. **W2D10–11 — Tier transitions** — Data only, and that is the headline: **eighteen changed paths and not one `.cs` among them**, which is the test Day 1 set for this day and the strongest evidence yet that the modular architecture is real rather than aspirational. Two quest assets (`sunken_crypt`, `dragons_roost`) and two archetypes (Arcane Battlemage, Dragonsworn Champion) filled the City and Capital tiers that had been raising Max Quest Tier to 3 and 4 with nothing to unlock. Three properties made it free, all of them already present: content declares its own availability through `MinimumTierOrder`, `QuestResolution.IsAvailable` reads the hardest tier off `IGuildStats` rather than off the tier asset, and the Day 7 UI had been written for five rarities and had simply never been shown two of them — the Epic and Legendary bands arrived styled, gated and explained with no interface work at all. The day's real finding was that **"higher-rarity archetypes are pointless" was a conclusion the model had never earned**: its hiring rule bought the cheapest available archetype, so across a 26-hour simulated game it purchased Militia Recruits and *nothing else* — the Hedge Knight and Wandering Ranger were never bought once. Fixing the policy exposed the larger problem underneath the power numbers: **the Inn's 16 beds against the 12 a Capital guild fields, with no way to dismiss anyone, make the roster a one-way ratchet**, so a player who spends spare beds during City can never hire the Legendary that Capital unlocks. The content is authored so both outcomes finish and the roster screen shows the lock with its reason, but the fix is a dismiss action and it is now owed to Day 12, alongside the second half of the same problem — `QuestAssignment` holds its party for life, so a late hire is inert until the player re-dispatches. Adventurer Max Level went 10 → 25, chosen by running 20/25/30 rather than picked: 30 pushes the strongest endgame party past the 4× speed clamp, which is the dead-levels failure Days 8–9 caught in the buildings wearing different clothes. The five archetypes are now one rule applied five times — each band doubles the archetype's power and costs five times the hire — which holds rarity visible against the Training Room's flat +331 and *widens* as the bands climb. Two spec deviations, both forced and both authorised by §3: the tier-4 quest's Recommended Power went 420 → 1,250, because at 420 every party a finished guild can field is already past the clamp and rarity buys nothing measurable; and City's reputation gate re-derived 28,000 → 65,000 under the unchanged 75% rule, because the old figure predated any asset paying tier-3 reputation. A modelling correction came out of the same work and is worth more than the content: **the model judged every party by the strongest party's power**, which was exact only while all adventurers were identical, and correcting it moves the *published Day 8–9* figures to 4h41m and 19h37m. The purchase-gap profile — the number Days 8–9 said the model exists to protect — improved sharply, 90th percentile from 19 minutes to 5–7 and worst gap from 59 to 25, because 300-odd training purchases now fill the stretches where only an expensive Tavern level was on offer. `Docs/Day10_Tier_Transitions.md` carries the tables and a 9-step pass; `guild_model.py` was updated in the same commit and now reports a patient and an impatient player, because the bed ratchet makes patience a genuine fork.
11. **W2D10–11 follow-up — `OnValidate` deferred** — Running step 1 of the Days 10–11 pass confirmed the data-only claim about as hard as it can be confirmed: nine assets imported clean, no warnings from any of them, and the seven `IdleGuild.*.dll` untouched — Unity found no reason to compile, rather than compiling and finding nothing wrong. What the step *did* turn up was ~3,800 lines back in `Editor.log`: every definition asset warning about itself on assets that are plainly correct, because **`OnValidate` runs while Unity is still deserialising the object** and every serialised field reads as its default in that window. Day 4–5 had diagnosed half of it and moved `GameContent` off `StartingTier` onto `Tiers.Length`; the length reads zero too, so the fix never took and nobody noticed because the console was already noisy. Fixed in a separate commit — six files, one of them new — so the Days 10–11 commit stays honestly data-only: `Core/AssetValidation.WhenLoaded` queues each self-check for the next editor tick via `EditorApplication.delayCall`, de-duplicated per asset, `[Conditional("UNITY_EDITOR")]` so nothing reaches a player build, with clamps left inline where they belong. The lesson is worth more than the fix: **a check that cannot tell a half-loaded object from a half-filled one is not a check**, and its real cost is teaching you to ignore the console before the day you need it.
12. **W2D10–11 follow-up — a test suite** — Prompted by the obvious question after writing a nine-step manual pass: can't we build tests? Mostly yes, and the part that resists is informative. Seven of the nine steps were mechanical and were going to be re-run on Days 13, 14, 21 and 23; they are now an eighth assembly, `IdleGuild.Tests.Editor`, with **43 tests that run in 46 ms and passed on the first run**. It references all seven assemblies and nothing references it, so it sits above UI the way UI sits above App and the architectural bet is untouched. Two arguments carried it over the schedule cost: this project's one real bug — the debug console's delete undoing itself — is exactly a round-trip assertion, and every content failure so far was a wrong *value* in a shipped asset, which is why the tests load the real `.asset` files through `AssetDatabase` rather than constructing content a fixture would have got wrong in the same way. **The rule that makes them survive a balance pass is to assert the shape rather than the number** — no dead levels, gates that only tighten, a ladder that doubles, an opening that is solvent — with the value-asserting handful tagged `BalanceCanary` so Day 13 can find them in one filter. Save compatibility got its own answer: round-tripping today's writer into today's reader proves only that they agree with each other, so `Tests/Editor/Fixtures/` now holds three real files, including one pointing at content no build has ever had — the first thing ever to exercise the Day 6 repair path against a file that actually needed repairing. The Week-1 save intended as the first fixture had already been overwritten by an autosave, which is the lesson in miniature: **a save file is the only record of what an earlier build wrote, and it stops existing the moment the current build runs.** Writing the tests found two things before any of them ran — `QuestResolution.FailureChance`'s comment claimed the rate doubles at half power when the formula gives 1.5x and only doubles at zero, and `Object.GetInstanceID()` is deprecated in Unity 6 with its `[Obsolete]` marked as an *error*, which is worth carrying into Week 3 when the ad and IAP SDKs arrive. `Docs/Tests.md` is the standing reference; `Docs/Day10_Tier_Transitions.md` §8 now says which of its steps survive by hand.

13. **W2D12 — Recruitment and assignment UI** — The game's first two reversible decisions, both named by Days 10–11 and neither invented. Written and compiling clean: eight assemblies, zero `error CS`, zero new warnings, verified in `Logs/Editor.log`; four new files, ten changed, **no `.asset` touched and no save field added**. The suite reports **64 green**, up from 47 — which incidentally closes an open question from the previous handoff, since 47 is only reachable if the four `SaveFixtureTests` are running, and nobody had a record of them ever having run. Retiring is `RecruitmentService.TryDismiss` over the `AdventurerRoster.Remove` that had existed since Day 4–5 with only save restoration calling it, and the design question was never the code but what it should do to a member of a live standing order. It **refuses**, naming the order, because the cascading version's naive form — drop the member, leave the order — is this project's own recurring failure wearing another hat: `TryStartRun` would have returned false for the rest of the run with an order on screen that simply never went out again. Re-forming a party is what releases them, so the two halves are one route and a test walks it end to end. Re-forming turned out to cost nothing structurally, because a Day 4–5 decision paid out a second time: `ActiveQuest` snapshots its own party and the clock sends *that* snapshot home, so replacing an order's party never disturbs the run in flight — `QuestAssignment` was documented as holding its party for the life of the *assignment* when it had always really been for the life of a *run*. One party picker serves both a first dispatch and a re-form, and finally gives `PartyPower` and `PreviewDurationSeconds` the callers they have lacked since Day 4–5; they are what turn *swap the Recruit for the Champion* from a guess into a comparison made before committing. Two narrowings went past the brief and are recorded as such: a party is now **exactly** the size the quest asks for, which was unreachable before a screen could build one by hand and which every duration figure in the game was derived against; and "send a party" takes the strongest free adventurers rather than the first on the roster, which `guild_model.py` already assumed. **The day's real finding came from the model rather than the game.** Its comment block still asserted that an impatient player "can never hire a Champion at all", so it was simulating a wall that no longer existed. Adding a retire rule took two attempts and the failed one is the useful half: ranking swaps by fully-trained potential — how every *other* hiring decision in the model is made — churns the entire roster to sixteen Legendaries once gold stops being scarce, throwing away every level of training bought along the way and putting everything-maxed at **28h16m**, eight hours longer than before the action existed. That is an arbitrage bug, not a player. Requiring the replacement to be better *the day it arrives* fixes it with no threshold and no magic number, and on **unchanged assets** moves the published figures to 5h41m / **22h50m** patient and 4h16m / **17h45m** impatient. Two things follow, and Day 13 inherits both. **Retiring makes the game about two hours longer** — it is a gold sink, full price again and a level-1 replacement — and **the impatient player still never fields a Legendary**: beds free up and the Commons vanish from their roster, but trained Battlemages out-earn a fresh Champion, so the structural lock simply became an economic one. Deliberately not tuned away; the two runs are a bracket on one policy decision and choosing inside it is a balance question. The general shape worth carrying: **a reversible decision is not the same as a cheap one.**
14. **W2D13 — First balancing pass** — Data only, and smaller than any day so far: **five numbers on five `.asset` files, no game `.cs`, no save field, and not one `BalanceCanary` updated.** The day was handed a ten-hour policy bracket by Day 12 and asked to choose inside it, and the answer was that it was not a policy question. **Each rarity band doubled power and tripled training cost** — bases 20 / 60 / 180 / 540 / 1620 at a common 34% growth — so gold per point of power climbed 1,236 → 1,854 → 2,782 → 4,175 → **6,268** and a Legendary bed cost **81x** a Common bed to realise while returning **16x** the power. Rarity was strictly dominated on the gold axis for the whole game, which is what made Day 12's greedy rule look like an arbitrage bug: it was buying the best archetype available, which is what a player does, and being charged ten hours for it. **The behaviour was never wrong; the price of the behaviour was.** Bases are now 20 / 40 / 80 / 160 / 320 — each band doubles power and doubles the gold to train it out — g/power comes out flat at 1,236 → 1,249, and the three swap policies that spanned ten hours land inside **eighty minutes**, with greedy and pragmatic one minute apart. Pacing: patient Capital **5h54m** / maxed **18h14m**, impatient **6h56m** / **19h33m**, both finishing with sixteen Legendaries, purchase gaps the best recorded at median 1.5 min / 90th pct 4 / worst 19 against the 5–7 and 25 Days 8–9 asked for. **Three earlier days had each blamed something else and each was true and none was the reason** — Days 8–9 the Training Room's guild-wide bonus, Days 10–11 the Inn's bed ratchet, Day 12 an economic ceiling it created by removing the structural one. All three looked at power, gates and the player; none looked at the price list, because **a ratio authored in one place and paid for in another will not be checked by anybody looking at either.** The model's half of it was six percent wide: its swap rule required a *level-1* replacement to beat the incumbent, and a level-1 Champion is 379.4 against a maxed Recruit's 403.0 — it wins at **level 3** for about a thousand gold, so a wall two training levels thick had been reported as impassable. `switching_cost()` now prices the catch-up, with no threshold and no magic number. The suite went **64 → 66** with no existing test moved, deliberately in two kinds: `AHigherRarityBandNeverCostsMoreGoldPerPointOfPower` as an invariant any honest retune passes untouched, and `TheTrainingLadderReadsAsWritten` as the canary for one mistyped figure in one asset, which the invariant would sail past. **That no canary moved is the finding rather than the reassurance** — no canary had ever watched a training cost, and **a canary set that does not watch a value is quieter than no canary set, because its silence reads as a pass.** One thing was found and deliberately not fixed: `Adventurer.PowerWith` adds the Training Room's bonus **flat**, so it is worth +462% to a maxed Militia Recruit and +29% to a maxed Champion — an equaliser wearing a levelling mechanic's clothes, compressing the authored 16x ladder to **x3.7** at a finished guild, and making a 25-gold recruit worth +331 power at 0.1 gold per point against 1,576–7,977 for a training level. Making it multiplicative is one line plus a re-derivation of every quest's Recommended Power and the tier gates; **deferred to Day 21** with the numbers written down, because the compression bites at Training Room 30–40 where the roster has converged anyway, so today it costs feel rather than balance. `Docs/Day13_First_Balance_Pass.md` carries all of it.

15. **W2D14 — Playtest, one shipping bug, and a design revision** — The day started as a playthrough and became the largest design conversation the project has had. **The playtest found a real bug in shipped code**: `QuestDispatchService.Cancel` deliberately lets the run in flight finish but **published no event**, so the order card kept rendering "Repeating", kept offering the button just pressed, and the quest visibly continued — press, nothing changes, indistinguishable from a dead button. Day 12 had written down this exact argument when it added `QuestPartyReformed`, and applied it only to the action it happened to be building. Fixed with `QuestOrderChanged`, a "Standing down" badge state, a disabled Recall and a `badge--pending` token; the shape worth carrying is that **an action whose effect is deferred has to say so on the thing it acted on, not only in a toast that scrolls away.** Suite 66 → 69, then 71 with the Day 14 save fixture — a genuine 17-minute session that reached Town with one honest contract failure in it, pinned at **zero repairs** so the revision deleting the Training Room shows up as a red test rather than a silence. **Git LFS went in a day early while the window was clean** (no binary had ever been committed), with `.meta` deliberately excluded, and Apple Developer enrollment came back approved — closing the one §04 item whose delay was never ours. **Then the design changed.** The game is now an **idle hotel tycoon**: five rooms, four earning gold per hour, contracts feeding the building through a Front Desk commission, staff on ongoing wages with the net floored at zero, adventurers arriving in the tavern crowd rather than bought from a shop, prestige as an isekai re-summoning, and a monetisation layer of **automation and cosmetics only — nothing bought with money makes a number go up.** The reassuring half: **three of the four original buildings were already mechanically what the new vision describes**; Tavern-quality-attracts-better-adventurers has been in the build since Day 4. The gap was never the design, it was that none of it is drawn. **Nothing was built** — no `.asset`, no game `.cs` — because Days 8–9 and Day 13 both found structural failures no playthrough would have surfaced and this change is larger than either. `Docs/Vision_Revision.md` is the new charter. The model was rebuilt from scratch (`tycoon_model.py`) and, when hand-tuning failed to converge across twenty coupled dials — Village went 1h48m, 1h32m, 3h50m, 3h16m, 4h26m, 8h19m while every individual change was correct in isolation — an **auto-tuner** (`tuner.py`) was written to search the space against explicit targets. It reached rooms at **68%** of lifetime income (target 70) and a **6-minute** 90th-percentile purchase gap (target 10) in a few hundred evaluations. Eleven structural findings came out of the modelling, each of which would have shipped as a bug; the two best are that **every new room cannibalised the existing ones** — opening the Provisioner diluted the staff serving the Tavern and Inn, so its payback was negative and the model sat on 276 million gold refusing to buy a 9,000-gold room — and that **the opening hinged on a one-gold coin flip**, an adventurer costing 40 against a potboy costing 39, so the guild hired staff it did not need and never sent anybody on a contract. What is still wrong is not the tier boundary but the **dead first twenty minutes**: tavern and desk built instantly, an adventurer in the crowd immediately, then nothing until the first staff hire at 21 minutes. And the day produced the project's first calibration of **modelled time against lived time** — 17.6 real minutes to Town against a predicted 8, a 2.2× gap, because a model buys the instant it can afford something and a person does not.


16. **W3D15 — the economy tuned, and an interface nobody had seen** — Handed one instruction (score first beats rather than tier boundaries) which was right and unreachable, because four things underneath it were wrong. **The model was measuring its own clock**: contract durations rounded *up* to a whole integration step, so Rat Cellar's 31.5s took a 60s tick — a 1.9x throughput penalty on the entire adventurer economy that vanished as the step got finer. §6C's "68% of lifetime income from rooms" and "6-minute purchase gap", the two figures it called the hardest targets met, were **artefacts of scoring at `step=60`**; at step 5 the share was 82%, and at step 30 the Capital was never reached at all. **`payback()` said seconds in its docstring and returned hours**, and because a ranking only needs the order and gets it in either unit, the one place the units could have been caught was the single comparison against `reserveDeadline` — which read `0.755 hours <= 21.8 seconds` as true and let every candidate walk through every reserve the model ever held. So **finding #11's one-gold coin flip was never fixed**, only hidden by a tick coarse enough to carry gold past both prices at once. **The dead twenty-two minutes was arithmetic, not a curve**: 150 gold minus 143.85 of opening purchases leaves 6.15 against a 40-gold next step at 1.54/min = 21.9 minutes, and **every number in that sentence was hardcoded outside `SPEC`** — the tuner was never in a basin, it was searching a space that did not contain the problem. Seven dials added for the opening. The loss now scores **first beats and the longest stretch with nothing to buy**, as the median across three integration steps with a penalty on their disagreement, because a single run of a chaotic system measures the run. **Two of my own metrics were wrong and both were caught by reading traces rather than losses**: counting arrivals as beats made a guild full of adventurers it could not afford look busy, and then counting contract payouts as beats made an opening of "Rat Cellar pays 2g" fourteen times and *no purchases* score the best silence in the search. Result: worst opening silence **22 min → 5.0**, Village **23 min** inside a new 20–30 band, rooms **65–69%**, purchase gap p90 **4 min**, and — the number that matters most — **`spread` 0.26 → 0.06**, so every figure now holds across a twelve-fold change in the step. **A bound enforced on one path is not enforced**: `gate_scale`'s floor was raised to stop the tuner halving every tier gate, and resuming from a saved point smuggled the old value through two more rounds into a promoted config, because `search()` clamps perturbations and not incumbents. Clamping it honestly cost 11.4 → 273, which is itself the finding — **every good number before that point was partly bought by a gate the player never had to reach for.** **Tapping turned out to be an obligation rather than an addition**: §6B sells familiars on "a free player can do everything a payer can and simply has to tap", so the monetisation pillar was load-bearing on a mechanic neither the code nor the model had. Modelled as throughput, capped by unserved demand, worth zero once staff cover the room. **And the icon wire, moved forward from Day 17, paid twice.** It found that this project imports textures as Sprite Mode **Multiple**, so the auto-slicer cut one tankard into two sprites — which on Day 17 would have hit every icon with a detached element, silently. Then it found the real one: **the interface had never been drawn.** `GuildScreenController` takes `GetComponent<UIDocument>()` and that component's Panel Settings is empty, and **a UIDocument with no Panel Settings still returns a perfectly good `rootVisualElement`** — an orphan attached to no panel. So the screen built into the void with no error, no exception and a clean log, for fifteen days, while the game was played through the debug console. Several things this Ledger records as verified could not have been; the correction is written into §06 rather than quietly amended. Two checks now make it loud, the second deferred by a frame for the Days 10–11 `OnValidate` reason. **The shape has now appeared four times in four costumes: a failure whose only symptom is the absence of something is not detectable, and will be found by accident or not at all.**


17. **W3D16 — the revenue engine, and a staff ladder nobody had ever climbed** — The first day of the revision that changes the game rather than describing it, and **not one `.asset` file was touched**. `IdleGuild.Staff` is the sixth feature assembly on the same terms as the other five, Core-only. `TradeService` in App turns §3.1's three levers into gold per hour — demand from the tier, capacity from the room's level, throughput from the payroll — with **staff serving the most valuable custom first**, which is finding #10 made structural: proportional sharing is what made opening the Provisioner *reduce* income and left a model sitting on 276 million gold refusing to buy a 9,000-gold room. Wages come out of the till and the net is floored at zero. `TakingsService` is the tap, and it needed the one thing the model could not supply: the model treats tapping as a *rate*, so a fast thumb would draw an hour of custom out of it in three seconds — the rate now fills a **capped queue** instead, because coming back from eight hours away to a wall of free gold would make the tap a reason to close the game. **The architectural bet took its heaviest load yet and did not move**: nothing was added to `BuildingDefinition`, and `GuildState.Aggregate` gained no branch — it gained an early *return*, which is the opposite thing. **Staff can be let go on the same day they can be hired**, which was the standing instruction from §6C's third finding and from Day 12 having to retrofit the adventurer version. **And that is what exposed the day's finding.** Before authoring four staff assets from `tuned_params.json`, the two authored numbers were divided by each other — the check Day 13 established and which had never once been performed on this ladder. Gold per point of service climbs **0.47 → 2.06 → 8.63 → 32.69**, and the tuned configuration hires **105 Potboys and never buys a Server, Barkeep or Steward at any integration step**. The price is not the cause: flattening the ladder still gives 98 Potboys and one Server, and starving slots to four still gives four Potboys. `purchase()` can only ever *append* staff — **the model has no way to let anybody go** — so slots once filled with the cheapest help are filled forever, the ladder is unreachable **at any price**, an unreachable rung cannot cost the loss anything, and it was therefore free to be priced arbitrarily. Day 15 then raised the staff-slot curve to cure the opening silence, which was right for the thing it was aimed at and removed even the slot pressure that might have forced a climb: **a change correct for its own target silently deleted a subsystem elsewhere, because the loss function did not score that subsystem.** Inside it, a smaller one with a sharper edge: the model's own comment above the staff table has claimed since Day 14 that "the ladder has to improve per gold as it climbs", and the numbers underneath it have always been 5.0, 14.7, 41.8, 107.1 — **the fix was written as prose and never as arithmetic**, and the prose has been read several times since. So no staff assets were authored, and `AHigherStaffTierNeverCostsMoreGoldPerPointOfService` ships calling **`Assert.Ignore`** with a pointer, because a test that would pass vacuously is Day 13's silent canary in a smaller costume. Three other things are worth carrying. **A per-room stat summed across the guild is a *plausible* wrong answer**, which is the sibling of the absence-failure this project has met four times and needs the opposite treatment — sixty-eight seats reads exactly like a real figure, so `GuildStatScope` names the three per-room stats, `Aggregate` refuses to produce them, and they read a loud zero. **Two of the charter's own mechanisms had gone stale in exactly the way Day 15's `--checks` block had** — §4 names a `Revenue` stat no room has, because revenue became `seats × spend` when §3.1 split demand from capacity and §4 did not follow; and §3.1's `Σ each staff member's wage` is a rule `wagesPerHour()` has never used, making the per-employee wage field dead data read by nothing. Both corrected in the charter rather than quietly amended, and `StaffDefinition` ships with **no wage field**. And **`JsonUtility` leaves an absent array null**, which would have thrown on all four save fixtures — guarded, with the half that matters more being that a null payroll is **not counted as a repair**, or two fixtures go red for having been written honestly. `SaveSchema.CurrentVersion` still has never moved. Suite **71 → 103**, two Ignored. One red on the first run and it was the right one: `EveryGuildStatHasAPlayerFacingName` caught all five appended stats falling through to `stat.ToString()`, so a room panel would have read *"ServiceSeats"* at the player — **an invariant moving, and right to**, which is the exact counterpoint to Day 13's finding that no canary moved.


18. **W3D16 follow-up — the crown's stipend, and a rule about dead ends** — A playtest of the Day 16 build reached an **unrecoverable state on the third purchase of a new guild**: Tavern to 1, Tavern to 2, Inn to 1 is 147.50 of 150 starting gold, leaving **2.50 against a 25-gold recruit** in a build where gold comes only from contracts and a contract needs an adventurer. Income exactly zero, permanently; the only way out was deleting the save. **It is Day 4-5's opening deadlock returning with teeth** — that one was recorded as "solved in data rather than in code" by granting starting gold, and the lesson it should have carried is that **a data solution which depends on the player spending it correctly is a hope rather than a solution.** It also found that **the tap shipped the previous day was inert**: no room produces `ServiceDemand`, so unserved demand is zero, the queue never fills and `TryCollect` returns false forever — the fifth appearance of *a failure whose only symptom is the absence of something*, and the suite could not see it because every trade test builds its own rooms, which is exactly what `TradeFixture`'s own doc comment warns about and was still not enough to make anybody look. The fix is **the crown's stipend**: a mailbox on a 30-second cooldown, three deliveries bankable, the amount authored per tier, granted through `Grant` so it announces itself, counted on its own lifetime line rather than inside room income so the thumb cannot move the 70/30 split, and improvable by no purchase whatsoever so it can never enter a payback ranking. Fictionally it is the **crown**, deliberately separate from §6B's **Patron**, so the thing that can never let you fail and the thing you can spend money on have different sources — which also hands familiars a third automation target that grants no power. **The sizing argument reversed a decision and is the interesting part.** The stated target was "recover in about a minute", which is 15 gold every 30 seconds — and against the tuned model that is **1,800 g/hr versus rooms earning 9.5 g/hr at Village, or 189x the entire economy the stipend is meant to sit underneath**, with 58x at Town and 8x at City. The tension is structural rather than a tuning miss: **while the mailbox refills continuously, recovery speed IS a sustained rate**, and 25 gold is two and a half hours of Village room income, so anything that rescues you quickly dwarfs the tier it rescues you in. Two branches were put up — a **hardship line** that stops accrual above a threshold (keeping the one-minute recovery, and capping what the crown can ever hold you at to `line + one delivery`), or shrink it and pay in time. The second was chosen, explicitly reversing the earlier answer now that the table existed. Landed at **1/2/4/8 gold**, holding ~30% of what the guild earns at Village and Town, 11% at City and 0.01% at Capital — and **no ladder can stay proportional**, because the earn rate at tier openings runs 418 → 730 → 4,208 → **15,859,700** g/hr and a proportional Capital stipend would be a 26,000-gold delivery. So the mailbox is necessarily meaningful early and irrelevant late, which is the takings tap's self-obsoleting shape reached by a different route. **The cost is twelve and a half minutes to dig out of an empty treasury**, pinned as a `BalanceCanary` rather than left as a fact nobody wrote down, because a trade-off nobody can see is a trade-off nobody will revisit. The durable half is a new entry in **§01 Principles: no sequence of choices may leave the player unable to make progress** — a property rather than a balance figure, asserted against the shipped catalogue because a fixture would have been built from the same assumptions that produced the dead end, and one that now governs whether a room can be sold, whether wages can bankrupt you, and whether prestige can strand a run. Suite **103 → 114**, three Ignored. One test earned its keep before it ever ran: the first version of `AnHourOfTheCrownsStipendIsWorthLessThanTheOpeningItBacksUp` compared the stipend against the cheapest room's build cost, which does not scale with tier, and **would have passed the 15-gold version it was written to catch**; rewritten against starting gold it fails that version by twelvefold. **A guard that would not have caught the bug that prompted it is not a guard.**

---

*This file is the working copy of the Guild Ledger and lives in the project repo. Update it directly per the handoff protocol above. The hosted artifact version is historical and is no longer kept in sync.*

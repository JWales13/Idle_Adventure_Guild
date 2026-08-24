# The Guild Ledger
### Idle Adventurer's Guild — Project Charter

*Working reference document — read this in full before writing or changing any code. It is the source of truth, not whatever summary accompanies it.*

---

## 01 · Project Principles

**Clean Code discipline.** Intention-revealing names, small single-purpose functions, single-responsibility classes. No god-object `GameManager` owning economy, UI, saves, and quests at once. Depend on interfaces where systems interact, not concrete classes. No magic numbers/strings — constants or config assets. Comments explain *why*, not *what*.

**Data-driven, modular architecture.** Buildings, adventurers, quests, and guild tiers are ScriptableObject data assets, not hardcoded logic — new content means a new asset, not an edited script. Systems communicate through events/interfaces rather than direct references, so one system can change without rippling into the others. Code is organized by feature (`Guild/`, `Quests/`, `Economy/`, `Adventurers/`), not by type.

The 3→5 building scale-up (adding Quest Board and Armory post-launch) is the concrete test of this: it should mean creating two new `BuildingDefinition` assets and wiring their unlock conditions, not touching the Tavern/Training Room/Inn code that already ships.

**Styling in code, not the Inspector.** UI is built on Unity's UI Toolkit with USS stylesheets — CSS-like, text-based, no per-prefab Inspector tinkering. Design tokens (color, spacing, type scale) live in a shared stylesheet from day one.

**Model usage.** Sonnet 5 for planning, design discussion, and roadmap work. Opus 5 for actual script generation and implementation, where holding the whole modular architecture in mind while writing new code matters more than speed.

**Session handoff.** As conversations grow, close a phase by updating this document and appending a continuation prompt to the Status & Handoff section — so a fresh conversation can pick up with full context and minimal re-explaining.

---

## 02 · Concept Summary

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
- [ ] **Apple Developer Program enrollment ($99/yr)** — start Week 1, Day 1; verification can lag
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

**Current status:** Week 2, Day 13 complete.

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

**Next action:** Week 2, Day 14 — full playtest, Village to Capital, logging every
friction point and bug for Week 3. Run the EditMode suite first as a baseline and again
before committing, as always.

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
    GuildStat.cs         the eight stats buildings can influence (incl. post-MVP)
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

- **Git LFS before the first art commit (Week 3, Day 15–16).** `.gitattributes` marks images as `binary` but does not route them through LFS. GitHub hard-rejects files over 100 MB, and binary art bloats history permanently because every revision is stored whole. Setting LFS up *before* the first art commit is trivial; after it means rewriting history. This has a hard deadline of Day 15.
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
I'm continuing work on Idle Adventurer's Guild, a solo Unity idle-tycoon game
(fantasy adventurers' guild theme), targeting App Store submission with 16
days left against the original 4-week deadline (target submission by Day 26,
buffer through Day 28).

The project lives at ~/Idle_Adventure_Guild. Read GUILD_LEDGER.md in the repo
root in full before doing anything else - it is the source of truth. Pay
particular attention to "The central architectural bet", "The roster is a
one-way ratchet" and "Working arrangement" in section 06, then read
Docs/Tests.md, which is short and changes how you verify things.

Current position: Week 2, Day 14 - "Core Gameplay Complete"
Last completed: Week 1 in full, Days 8-9 (building trees), Days 10-11 (tier
transitions), an unplanned follow-up that turned most of that verification pass
into tests, Day 12 (recruitment and assignment UI) and Day 13 (first balancing
pass). Unity 6000.5.0f1 / URP 2D, private GitHub repo via GitHub Desktop. Eight
assemblies: five feature assemblies depending on Core and nothing else,
IdleGuild.App above them holding composition and cross-feature transactions,
IdleGuild.UI above that, and IdleGuild.Tests.Editor above everything with
nothing referencing it. Day 13 was data only - five numbers on five .asset
files, no game .cs, no save field, and no BalanceCanary updated. It found that
each rarity band doubled power but TRIPLED training cost, so a Legendary bed
cost 81x a Common bed for 16x the power; the bases are now 20/40/80/160/320 and
gold-per-power is flat across all five archetypes.
Docs/Day13_First_Balance_Pass.md has the reasoning and one deferred decision;
Docs/Day12_Roster_And_Parties.md for retiring and party re-forming;
Docs/Day10_Tier_Transitions.md is still current for adventurers, quests and the
City reputation gate; Docs/Day08_Building_Trees.md for the building trees and
the tier gates' building requirements.

Next task: Day 14 - full playtest, Village to Capital, logging every friction
point and bug for Week 3. This is the day the accumulated hand-checking finally
has a played-in save to run against: three steps from Days 10-11 (step 4, Town
in about ten minutes; the colour half of step 6, whether USS actually paints
Epic purple; the is-it-fair half of step 8, Dragon's Roost at a guild that
earned its way to Capital) and four from Day 12 (the destructive button reading
as destructive, a sixteen-row party picker fitting the phone, the selected state
being unambiguous, and whether the retire confirmation reads as informative
rather than as a scolding). Call it twenty-five minutes inside a longer session.
Current modelled pacing to compare the played arc against: patient Capital 5h54m
and everything-maxed 18h14m, impatient 6h56m and 19h33m, both finishing with
sixteen Legendaries; Town at 8m and City at 1h08m. Purchase gaps are median 1.5
min, 90th percentile 4, worst 19. A real playthrough will not reach Capital in
one sitting, so the debug console's grants are the tool - but note that anything
granted invalidates the pacing comparison, so log which is which.

One judgement Day 14 is specifically asked to make, from section 6 of
Docs/Day13_First_Balance_Pass.md: does a Dragonsworn Champion FEEL like the
reward Capital exists to hand over? The document argues it currently should not.
Adventurer.PowerWith adds the Training Room's bonus FLAT, which is worth +462%
to a maxed Militia Recruit and +29% to a maxed Champion - an equaliser wearing a
levelling mechanic's clothes. It compresses the authored 16x rarity ladder to
x3.7 by the time the guild is finished, and a level-1 Champion arrives WEAKER
than a maxed Militia Recruit (379.4 against 403.0), winning only at level 3.
Making the bonus multiplicative is one line in Adventurer.PowerWith plus a
neutral base of 1.0 in GuildState, a ModifierKind on the Training Room asset,
re-derived Recommended Power on all five quests, re-spaced tier gates and four
canaries - deliberately deferred to Day 21, with the numbers written down. Day
14's job is not to do it but to say whether the deferral still looks right after
playing it.

Testing: there is an EditMode suite at Assets/_Project/Tests/Editor/ - 66 tests,
all green, running in well under a second. Run it (Window > General > Test Runner >
EditMode > Run All) before you start and before you commit. It asserts SHAPE
rather than NUMBERS on purpose; the eight that assert values are tagged
[Category("BalanceCanary")] and are expected to be updated deliberately by a
balance pass, while updating an invariant is a warning that something else is
wrong. It loads the real .asset files through AssetDatabase rather than building
fixtures in code, because every content failure this project has had was a wrong
value in a shipped asset. Docs/Tests.md explains the rest, including the three
save fixtures and why they are permanent. Day 14 is a playtest rather than a
code day, so the suite should not move at all - if it does, the playthrough
found something.

Deviations from the plan so far: none material. Day 1's "ad/IAP SDK package
stubs" became interface stubs, with the real SDK arriving Week 3 behind those
interfaces. Day 4-5 added IdleGuild.App above the features; Day 7 added
IdleGuild.UI above App; Days 10-11 added IdleGuild.Tests.Editor above
everything. In every case the features stayed Core-only. Days 8-9 touched quest
assets on a buildings day. Days 10-11 deviated from the written tier-4 quest
spec in Day08_Building_Trees.md section 3 - Recommended Power 420 became 1,250
with gold and reputation raised to match - because at 420 every party a finished
guild can field is already past QuestResolution's 4x speed clamp. That document
authorised the change. The test suite itself was not on the roadmap. Day 12
narrowed two behaviours beyond its brief, both recorded in section 5 of its doc:
a quest party must now be EXACTLY the size the quest asks for rather than at
least, and "send a party" now picks the strongest free adventurers rather than
the first on the roster. Day 13 stayed inside its brief and deferred the one
change that would have left it - see above.

Known issues/blockers: Git LFS must be set up before the first art commit on
Day 15 - its deadline is the commit, not the day, and it is now the nearest
hard deadline in the project. Ad network and IAP provider unchosen. Bundle ID
and product name are still template defaults, and they are also the save
directory (~/Library/Application Support/DefaultCompany/Idle_Adventure_Guild/),
so changing either strands every existing save - capture anything worth keeping
as a fixture BEFORE renaming, which now specifically includes whatever Day 14's
playthrough produces, since a played-in save is the one thing the fixture set
still lacks. Save files are plain text and trivially editable, a Day 20
hardening item, along with capping the guild_save.json.corrupt-* quarantine
files. The debug console must be deleted or excluded before submission, hard
deadline Day 22. Week 4 execution surface (device builds, TestFlight, App Store
Connect) is not solvable from Cowork and needs deciding before Day 22.

Two documentation hazards worth knowing. Asset values live in three documents
and the newest wins: Day13_First_Balance_Pass.md is current for the five
adventurer training curves and nothing else; Day10_Tier_Transitions.md for
everything else about adventurers, quests and the City reputation gate;
Day08_Building_Trees.md for the building trees and the tier gates' building
requirements; Day04 only for GameContent, the scene setup and the smoke test.
Day12_Roster_And_Parties.md contains no asset values at all. Pacing figures
supersede in publication order and Day 13's are current. And guild_model.py is a
copy of both the balance numbers and the player, and both halves drift: Days 8-9
found it judging every party by the strongest party's power, Days 10-11 found
its hiring rule had never once bought a non-Common adventurer, Day 12 found it
simulating a bed ratchet the game no longer has, and Day 13 found that its swap
rule had been blamed for four days for what the CONTENT was doing. When a run
says something is pointless, check both halves - the policy might not reach the
content, or the content might deserve it.

Working arrangement (see section 06): this runs in Claude Cowork, whose shell
is a Linux VM with the project folder mounted - git exists but `unity` and
`dotnet` do not. You write and edit files and never run git, not even
`git status`, which leaves index locks that break my GitHub Desktop; tell me
the commit message and I commit through the GUI. When you add scripts, ask me
to focus the Unity Editor so it imports them, then verify by checking for
Library/ScriptAssemblies/IdleGuild.*.dll and grepping Logs/ for "error CS".
Tests are the same loop: you write them, I run them and paste failures.
guild_model.py runs fine on the Cowork shell (python3, no dependencies, about a
third of a second), so model runs do not need me. ScriptableObject values can be
written directly into the .asset YAML rather than retyped through the Inspector,
which is how Days 8-9, 10-11 and 13 avoided a repeat of Day 4-5's transcription
slips.

Follow the Principles section of this doc (Clean Code, data-driven
ScriptableObject architecture, event-driven decoupling, UI Toolkit/USS
styling) without needing it re-explained. Confirm your understanding of
where things stand in a sentence or two before writing any code.
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


---

*This file is the working copy of the Guild Ledger and lives in the project repo. Update it directly per the handoff protocol above. The hosted artifact version is historical and is no longer kept in sync.*

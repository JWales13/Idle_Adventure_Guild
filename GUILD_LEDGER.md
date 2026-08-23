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

**Current status:** Week 1, Day 1 complete. Unity project lives at `~/Idle_Adventure_Guild` (Unity **6000.5.0f1**, Universal 2D / URP template). Git repo initialised on `main` with a Unity `.gitignore` and `.gitattributes`. Feature-folder structure created under `Assets/_Project/`, one assembly definition per feature. Ad and IAP boundaries stubbed as interfaces in `Core/Services`. No gameplay logic yet — that starts Day 2.

**Next action:** Week 1, Day 2–3 — core data model. ScriptableObject definitions for Building, Adventurer, Quest and GuildTier; a lightweight event bus in `Core`; `GuildState` and `PlayerEconomy` model classes fully decoupled from UI.

### Repository layout

```
Assets/_Project/
  Core/          IdleGuild.Core         — no dependencies
    Services/    ad + IAP interfaces and no-op implementations
  Economy/       IdleGuild.Economy      — depends on Core only
  Adventurers/   IdleGuild.Adventurers  — depends on Core only
  Quests/        IdleGuild.Quests       — depends on Core only
  Guild/         IdleGuild.Guild        — depends on Core only
  UI/            IdleGuild.UI           — depends on Core only
    Styles/      USS design tokens (Day 7)
  Data/          ScriptableObject asset instances
```

Two structural decisions made on Day 1, both flagged as reversible:

- **Assembly definitions per feature.** Principle 01 says systems talk through events and interfaces rather than direct references. An `.asmdef` per feature turns that from a convention into a compile error — a feature that reaches into another feature's internals will not build. This is the mechanism that makes "add Quest Board and Armory without touching shipped code" checkable rather than aspirational.
- **Features depend on `Core` and on nothing else.** No cross-feature references at all; anything shared travels through `Core` events and interfaces. Deliberately the strictest arrangement, because loosening it later is a one-line asmdef edit and tightening it later is a refactor. If it proves too strict during Day 4–5 loop work, add the reference and note it here.

**Open decisions carried forward:**

- **Ad network not chosen** — Day 18 needs one (Unity LevelPlay / AdMob / Unity Ads). The `IAdService` boundary lets the choice wait, but not past Week 3.
- **IAP provider not chosen** — same shape; `IPurchaseService` defers it to Day 19. Unity IAP is the path of least resistance given the project is already Unity.
- **Bundle ID and product name still template defaults** — `DefaultCompany` / `Idle_Adventure_Guild` in ProjectSettings. Needed for the App Store Connect record and worth reserving early, per the §04 lead-time note.
- Building count (3 for MVP, architected to scale to 5) and building effects remain finalized.

**Most recent continuation prompt:**

```
I'm continuing work on Idle Adventurer's Guild, a solo Unity idle-tycoon game
(fantasy adventurers' guild theme), targeting App Store submission with 27
days left against the original 4-week deadline (target submission by Day 26,
buffer through Day 28).

The project lives at ~/Idle_Adventure_Guild. Read GUILD_LEDGER.md in the repo
root in full before doing anything else — it is the source of truth.

Current position: Week 1, Day 2 — "Foundation & Architecture"
Last completed: Day 1. Git repo on `main` with Unity .gitignore/.gitattributes;
feature-folder structure under Assets/_Project/ with one .asmdef per feature
(Core, Economy, Adventurers, Quests, Guild, UI) where every feature depends on
Core and nothing else; ad/IAP boundaries stubbed as IAdService and
IPurchaseService in Core/Services with no-op implementations. Unity
6000.5.0f1, URP 2D template. No gameplay logic exists yet.
Next task: Day 2–3 — core data model. ScriptableObject definitions for
Building, Adventurer, Quest and GuildTier; a lightweight event bus in Core;
GuildState and PlayerEconomy model classes fully decoupled from UI.
Deviations from the plan so far: none material. Day 1's "ad/IAP SDK package
stubs" was implemented as interface stubs rather than installed SDK packages —
the real SDK arrives Week 3 behind those interfaces. See Repository layout in
§06 for the two structural decisions taken and how to reverse them.
Known issues/blockers: ad network and IAP provider still unchosen (needed by
Week 3); bundle ID and product name still template defaults.

Note on environment: if this session runs in Claude Cowork rather than a
terminal, its shell is a Linux VM with the project folder mounted — git works,
but `unity` and `dotnet` do not, so Editor and Xcode steps have to be driven
manually. Say so early rather than assuming a macOS shell.

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

---

*This file is the working copy of the Guild Ledger and lives in the project repo. Update it directly per the handoff protocol above. The hosted artifact version is historical and is no longer kept in sync.*

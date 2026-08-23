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

**Current status:** Week 1, Days 1–6 complete. Unity project at `~/Idle_Adventure_Guild` (Unity **6000.5.0f1**, Universal 2D / URP), published to a **private GitHub repo** and managed through **GitHub Desktop**. The core loop was finished on Days 4–5 and the 11-step smoke test in `Docs/Day04_Asset_Values.md` passed against the fourteen ScriptableObject assets, with the scene wired at `Assets/_Project/Scenes/Guild.unity` (a `Game` object carrying `GameBootstrap` and `DebugConsoleOverlay`).

Day 6 added save/load. A versioned JSON file now carries the guild tier, every building level, all four balances, the roster with levels, activity and rest timers, the quest runs in flight with their dispatch-time snapshots, the standing orders and the lifetime clock counters. `ISaveStore` and `FileSaveStore` joined the ad and IAP interfaces in `Core/Services` as the persistence boundary; `App/Saves/` holds the wire format, capture, restore, the migration ladder and the `GameSaveService` policy layer. The last-seen timestamp has moved out of PlayerPrefs and into the file. The debug console gained a **Save** section, and `Docs/Day06_Save_Verification.md` carries the schema, the compatibility rules and a 10-step verification pass including corruption and content-removal cases.

The Week 1 checkpoint holds, minus the UI that Day 7 adds.

**Next action:** Week 1, Day 7 — UI Toolkit scaffolding. USS design tokens (color, spacing, type scale) and grey-box screens for Guild Hall, upgrade panel, quest log and roster, wired to the Model via events. `IdleGuild.UI` is still empty and still Core-only, so the screens read the world through the events in `GameEvents` and whatever seam the App layer exposes — note the `GameLoaded` rule in the save-format section below before deciding how a screen builds its first frame.

### The central architectural bet, and how to check it still holds

`GuildStat` (Core) enumerates the eight quantities a building can influence. `BuildingDefinition` owns no logic — it holds `BuildingEffect` entries that each target one stat with a level-scaled `ScalingCurve`. `GuildState` aggregates them and exposes the result through `IGuildStats`.

`QuestSlots`, `MaxQuestTier` and `FailureRateReduction` are **already declared** even though nothing produces them yet. `GuildState.Get(QuestSlots)` currently seeds from the guild tier's static value and adds nothing. When Quest Board ships post-launch it contributes additively to that same stat, and every existing call site picks it up untouched. Armory does the same for `FailureRateReduction`.

**The test:** adding Quest Board and Armory must mean two new `.asset` files and zero edits to shipped `.cs`. If a future session finds itself adding a field to `BuildingDefinition` or a branch to `GuildState.Aggregate` to make them work, the bet has been lost — stop and reconsider rather than pushing through.

**Checked on Day 4–5 and still holding.** Writing the whole core loop required no change to `BuildingDefinition` and no branch in `GuildState.Aggregate`. Quest slots are read through `QuestLog.SlotsWith(IGuildStats)`, the hardest quest tier through `QuestResolution.IsAvailable`, and failure mitigation through `QuestResolution.FailureChance` — all three consume the stat and none of them cares where it came from, so the Quest Board and Armory contributions land in call sites that already exist.

Two zero-safety conventions protect against half-filled assets reading as silent zeros rather than obvious mistakes:

- `ScalingCurve` stores growth as a **percent per level**, so an all-zero asset evaluates flat rather than collapsing through a `pow(0, n)` term.
- Multiplicative effects are **bonus fractions** accumulated onto 1.0 — `0.15` means +15%, and an unfilled field means "no effect", never "multiply everything by zero".

A third convention arrived on Day 4–5: **a quest run's duration, failure chance and rewards are snapshotted when it starts**, not recomputed when it finishes. A timer therefore never moves under the player mid-run, an upgrade pays off from the next dispatch, and offline catch-up can resolve hours of runs without reconstructing what the guild's stats were at each moment in the past.

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
  UI/            IdleGuild.UI — depends on Core only (empty until Day 7)
    Styles/      USS design tokens (Day 7)
  Data/          ScriptableObject asset instances
    Buildings/ Tiers/ Adventurers/ Quests/   and GameContent.asset alongside them
```

**ScriptableObject assets are authored from `Docs/Day04_Asset_Values.md`**, which carries first-pass values for three buildings, four tiers, three adventurers, three quests and the GameContent catalogue, plus scene setup and a smoke test. Content declares its own availability via `MinimumTierOrder` rather than being listed on a tier asset, so adding content never edits an existing file — the exception being `GameContent`, which lists everything by design, since something has to.

Three structural decisions, the first two from Day 1 and the third from Day 4–5, all flagged as reversible:

- **Assembly definitions per feature.** Principle 01 says systems talk through events and interfaces rather than direct references. An `.asmdef` per feature turns that from a convention into a compile error — a feature that reaches into another feature's internals will not build. This is the mechanism that makes "add Quest Board and Armory without touching shipped code" checkable rather than aspirational.
- **Features depend on `Core` and on nothing else.** No cross-feature references at all; anything shared travels through `Core` events and interfaces. Deliberately the strictest arrangement, because loosening it later is a one-line asmdef edit and tightening it later is a refactor. If it proves too strict during Day 4–5 loop work, add the reference and note it here.
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
- **Bundle ID and product name still template defaults** — `DefaultCompany` / `Idle_Adventure_Guild` in ProjectSettings. Needed for the App Store Connect record and worth reserving early, per the §04 lead-time note.
- **The save file is plain text and trivially editable**, timestamp included, which means free offline earnings for anyone who opens it. A Week 3 hardening item with a soft deadline of Day 20, and the fix is a checksum rather than encryption — the goal is to make casual editing visible, not to defeat a determined player. Quarantined `guild_save.json.corrupt-*` files are also never cleaned up automatically; decide by Day 20 whether to cap them.
- **`Guild.unity` is not in Build Settings.** An empty scene list ships a black build, and it is the sort of thing found at the worst possible moment. Add it at index 0 well before the Day 25 TestFlight pass.
- **The debug console must be deleted or excluded before submission.** `DebugConsoleOverlay` disables itself outside development builds, so it is not a shipping risk today, but leaving dev UI in a store binary is the kind of thing that reads badly in review. Hard deadline Day 22.
- Building count (3 for MVP, architected to scale to 5) and building effects remain finalized.

**Resolved on Day 4–5:**

- **`[Min]` on `double` fields, removed.** Unity's Min drawer renders whatever it decorates as a *float* field, so a double-backed cost or reward was being truncated to float precision on every Inspector draw — roughly 7 significant figures, which four tiers of idle-game numbers would have crossed. Found while writing the authoring tables, before any asset had been created; the four affected fields now clamp in `OnValidate` instead, with a comment saying why. Worth remembering as a general rule: attributes that ship with the editor are not all `double`-aware.
- **The opening deadlock, avoided in data rather than code.** Housing Capacity has a neutral base of zero, so a guild with no Inn has no beds and can recruit nobody — and without adventurers there is no gold to build the Inn. Rather than special-case a starting bed in `GuildState`, `GameContent` grants starting gold (150) and the Inn is simply the first thing the player buys. No code branch, and the tutorial writes itself.

**Resolved on Day 6:**

- **The last-seen timestamp is out of PlayerPrefs** and inside the save, which means the stamp and the world it describes are written in one atomic step and cannot disagree. The failure that removes: a save that succeeded beside a stamp that did not, paying offline income for time the guild had already lived through. The old key is **deleted rather than migrated** — the builds that wrote it never persisted a guild, so its value describes an absence nothing was there for, and honouring it would hand out earnings for a guild that did not exist.
- **Saving is driven from three places, and it takes all three.** Pause is the reliable hook on iOS; `OnApplicationQuit` is not called when a player swipes the app out of the switcher, and a crash calls neither. A 30-second autosave bounds the loss when none of them fire.
- **A corrupt save is quarantined, not deleted.** The unreadable payload is kept under a timestamped key and the copy behind it — `FileSaveStore` keeps one through every atomic write — gets its turn. A player who loses a guild should be able to see it was not thrown away, and during Weeks 2 and 3 the file that failed to parse is the only evidence of why.

**Most recent continuation prompt:**

```
I'm continuing work on Idle Adventurer's Guild, a solo Unity idle-tycoon game
(fantasy adventurers' guild theme), targeting App Store submission with 23
days left against the original 4-week deadline (target submission by Day 26,
buffer through Day 28).

The project lives at ~/Idle_Adventure_Guild. Read GUILD_LEDGER.md in the repo
root in full before doing anything else — it is the source of truth. Pay
particular attention to "The central architectural bet", "The save format, and
the rule that keeps it readable" and "Working arrangement" in §06.

Current position: Week 1, Day 7 — "Foundation & Architecture", final day
Last completed: Days 1–6. Unity 6000.5.0f1 / URP 2D, private GitHub repo via
GitHub Desktop. Six code assemblies: five feature assemblies depending on Core
and nothing else, plus IdleGuild.App above them, the only assembly allowed to
reference more than one feature. The full core loop is written, compiling and
smoke-tested (Docs/Day04_Asset_Values.md, 11 steps, passed), running on
fourteen ScriptableObject assets with the scene wired at Assets/_Project/
Scenes/Guild.unity. Day 6 added save/load: a versioned JSON file carrying the
guild tier, building levels, all four balances, the roster with activity and
rest timers, quest runs with their dispatch-time snapshots, standing orders and
the clock counters. ISaveStore/FileSaveStore sit in Core/Services as the
persistence boundary; App/Saves/ holds SaveGameData, SaveCapture, SaveRestore,
SaveMigrations and GameSaveService. GameBootstrap loads on wake, pays the
absence through the existing OfflineProgress path, and saves on pause, on quit
and every 30 seconds. Docs/Day06_Save_Verification.md has the schema and a
10-step verification pass.
Next task: Day 7 — UI Toolkit scaffolding. USS design tokens (color, spacing,
type scale) in a shared stylesheet, then grey-box screens for Guild Hall,
upgrade panel, quest log and roster, wired to the Model via events. IdleGuild.UI
is empty and Core-only. Read the "restoration is quiet" note in §06 before
deciding how a screen builds its first frame: it must read state directly on
GameLoaded rather than accumulating change events, because loading a save
deliberately does not announce the upgrades and tier advances it replays.
Deviations from the plan so far: none material. Day 1's "ad/IAP SDK package
stubs" became interface stubs, with the real SDK arriving Week 3 behind those
interfaces. Day 4–5 added the IdleGuild.App assembly above the features — see
the structural decisions in §06 for why, and note that the features stayed
Core-only, including through Day 6's save/load, which spans all five of them.
Known issues/blockers: Git LFS must be set up before the first art commit on
Day 15. Ad network and IAP provider still unchosen. Bundle ID and product name
still template defaults — note these also set the save directory, so changing
them moves existing saves. Guild.unity is not in Build Settings, which ships a
black build if it is still missing at the Day 25 TestFlight pass. GameBootstrap
has Use Fixed Random Seed available and, if ticked, makes every session and
every reload roll identical quest outcomes — fine for verification, wrong for
any balance judgement. Save files are plain text and trivially editable,
including the timestamp; that is a Day 20 hardening item, not a launch blocker.
Week 4 execution surface (device builds, TestFlight, App Store Connect) is not
solvable from Cowork and needs deciding before Day 22.

Working arrangement (see §06): this runs in Claude Cowork, whose shell is a
Linux VM with the project folder mounted — git exists but `unity` and `dotnet`
do not. You write and edit files and never run git, not even `git status`,
which leaves index locks that break my GitHub Desktop; tell me the commit
message and I commit through the GUI. When you add scripts, ask me to focus
the Unity Editor so it imports them, then verify by checking for
Library/ScriptAssemblies/IdleGuild.*.dll and grepping Logs/ for "error CS".

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
6. **W1D6 — Save/load** — A versioned JSON save now carries the guild tier, every building level, all four balances, the roster with levels, activity and rest timers, the quest runs in flight with their dispatch-time snapshots, the standing orders and the lifetime clock counters. `ISaveStore` and `FileSaveStore` joined the ad and IAP interfaces in `Core/Services`; `App/Saves/` gained the wire format, capture, restore, the migration ladder and the `GameSaveService` policy layer. Existing classes needed four small seams and nothing more, which the Day 4–5 read-only-state discipline had already paid for: `GuildState.RestoreState`, `SimulationClock.RestoreCounters`, `GameContent.FindTier` and a `GameLoaded` event. **The architectural bet was re-checked and still holds** — save/load spans all five feature assemblies, which is exactly the kind of pressure that would have forced a cross-feature reference, and it did not: capture and restore live in App, the features stayed Core-only, and nothing was added to `BuildingDefinition` or branched in `GuildState.Aggregate`. Four decisions worth carrying forward. **`JsonUtility`, not Newtonsoft** — no package to add before submission and no IL2CPP reflection worries, at the price of roughly seven significant figures on doubles, which the one precision-critical value dodges by being a `long` tick count. **The compatibility rule is about the schema file rather than the migration code**: fields are only added, never removed or renamed, which is what lets an old save deserialise into today's classes at all and makes most future changes need no version bump. **Restore repairs rather than refuses** — a save is not trusted to match today's catalogue, since a quest renamed in Week 2 orphans every save in the wild, so anything unresolvable is dropped and counted in a report instead of throwing. And **restoration is quiet**, which is a Day 7 constraint as much as a Day 6 one: loading a level-4 Tavern must not announce four upgrades, so a screen cannot build its first frame from change events and has to read state directly on `GameLoaded`. Two smaller things: the PlayerPrefs stamp was deleted rather than migrated, because the builds that wrote it never persisted a guild and honouring it would pay offline earnings for a guild that was never there; and saving runs off pause, quit *and* a 30-second autosave, because iOS calls `OnApplicationQuit` only sometimes and a crash calls neither. `Docs/Day06_Save_Verification.md` carries the schema, the compatibility rules and a 10-step pass whose last three steps deliberately corrupt the save and delete content out from under it — the Day 20 hardening item, brought forward to the day the code was written.

---

*This file is the working copy of the Guild Ledger and lives in the project repo. Update it directly per the handoff protocol above. The hosted artifact version is historical and is no longer kept in sync.*

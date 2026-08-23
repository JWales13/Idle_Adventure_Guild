# Day 7 — UI Toolkit scaffolding

The grey-box interface, the design tokens under it, and the two Editor steps that
cannot be done from a text editor.

---

## 1. What was added

**Stylesheets — `UI/Styles/`**

| File | Role |
|---|---|
| `Tokens.uss` | The only file in the project naming a raw colour or measurement. Surfaces, text, currencies, rarity, spacing, radii, type scale. |
| `GuildTheme.uss` | Component styles, every value pulled from a token through `var()`. |

**Shared — `UI/`**

| File | Role |
|---|---|
| `Format.cs` | Amounts (`1.25K`), durations (`1h 04m`), multipliers, percentages, stat names, rarity classes. |
| `Outcomes.cs` | The service outcome enums as sentences a player can act on. |
| `GuildContext.cs` | What a screen may touch: the world, the six services, and a way to report back. |
| `Ui.cs` | Element constructors, so intent survives the boilerplate. |
| `SafeArea.cs` | Notch and home-indicator insets. |
| `GuildScreen.cs` | The three tab destinations. |
| `GuildScreenController.cs` | The one MonoBehaviour: builds the shell, subscribes, ticks. |

**Screens — `UI/Views/`**

`TreasuryBar`, `TabBar`, `ToastBar`, `HallView`, `BuildingCard`,
`BuildingDetailOverlay`, `QuestsView`, `RosterView`.

**Edited:** `IdleGuild.UI.asmdef` now references Core, the four feature assemblies and
App — see §3.

---

## 2. Editor steps

Two things have to be made in the Editor. Everything else is already on disk.

### 2.1 Panel Settings

1. **Create → UI Toolkit → Panel Settings Asset**, saved as
   `Assets/_Project/UI/GuildPanelSettings.asset`.
2. Set these fields:

| Field | Value | Why |
|---|---|---|
| Scale Mode | **Scale With Screen Size** | Constant-pixel UI is unreadable across phone densities. |
| Reference Resolution | **1080 × 1920** | The type scale in `Tokens.uss` is written in these units. |
| Screen Match Mode | **Match Width Or Height** | |
| Match | **0** (width) | Matching width keeps text the same size relative to the screen's width, so a taller phone simply shows more list. Matching height would squeeze the layout horizontally on tall devices. |

Leave **Theme Style Sheet** at the default runtime theme Unity assigns — the project
stylesheets layer on top of it rather than replacing it.

### 2.2 The scene

1. Open `Assets/_Project/Scenes/Guild.unity`.
2. Add a child GameObject under **Game**, named **UI**. A separate object rather than
   more components on `Game`, so the whole interface can be switched off in one click
   when testing against the debug console alone.
3. Add **Guild Screen Controller** to it. `[RequireComponent]` brings the **UIDocument**
   along automatically.
4. On the **UIDocument**: assign `GuildPanelSettings`. Leave Source Asset empty — the
   hierarchy is built in C#, not from UXML.
5. On the **Guild Screen Controller**:
   - **Bootstrap** — drag the `Game` object in. It falls back to finding the one in the
     scene, but an explicit reference is one less thing to wonder about later.
   - **Tokens** — `UI/Styles/Tokens.uss`
   - **Theme** — `UI/Styles/GuildTheme.uss`
6. Save the scene.

> If the screen comes up in Unity's default grey with the wrong fonts, one of the two
> stylesheets is unassigned — the controller warns about exactly this in the console.
> If nothing appears at all, the UIDocument has no Panel Settings, which it also warns
> about by name rather than failing as a null reference three frames later.

The debug console stays on the `Game` object and still works. Keep it: it owns the
time controls (`+1 hour`, `Offline 8h`) and the save controls, none of which the real
UI has any business exposing to a player.

---

## 3. Where the UI sits, and the rule that replaces the compiler

`IdleGuild.UI` now references Core, Guild, Quests, Adventurers, Economy and App. It is
the top of the graph and nothing references it.

This is the Day 4–5 decision applied one layer up. A screen has to render a
`BuildingDefinition` and call `BuildingUpgradeService.TryUpgrade`, which span three
feature assemblies and the App layer — the same pressure that created App in the first
place. **The features stayed Core-only**, so the compile-time wall between them and the
Quest Board / Armory bet are both untouched; what changed is that one more assembly
above them is allowed to see across.

The cost is honest: the UI assembly can now see the whole game, so "views hold no
rules" is a rule kept by discipline rather than by the compiler. `GuildContext` is
where that discipline is written down, and it is worth restating:

> **Views read state and call services. They never compute one.**

A cost, a gate, an unlock, a failure chance or a duration belongs to a definition asset
or a service. A screen that works one out for itself has put a rule somewhere the
balance pass on Day 13 and the tests will never look — and it will disagree with the
simulation the first time either changes.

Two consequences already visible in the code, worth copying rather than reinventing:

- **`BuildingDetailOverlay` reads effects off the asset** and evaluates the curve at the
  current and next level. It does not know what a Tavern does. Add an effect to an
  asset and the overlay explains it with no code change — the data-driven architecture
  doing the same work for the interface it already does for the simulation.
- **Every button that can refuse says why.** The services return an outcome enum naming
  the gate that stopped them; `Outcomes.Describe` turns it into a sentence and the toast
  shows it. This is the payoff for those enums not being bools, and it is why a disabled
  button in this game is never silent.

### How the screen keeps up with the simulation

Events never rebuild anything. They set a flag, and a 100 ms tick acts on it:

- **Live values** — balances, countdowns, progress bars, affordability — are read every
  tick. This is what `CurrencyChanged`'s own documentation asks for: idle income accrues
  continuously, so a display bound to the event would either flood the bus or sit still
  between quests.
- **Structure** — a new roster member, a run starting or finishing, a tier advancing —
  rebuilds only when one of those events fires.

The second reason for the flag is defensive: `EventBus` abandons the remaining handlers
for a publish if one of them throws, so a handler that does nothing but set a bool
cannot take another subscriber's delivery down with it.

**`GameLoaded` is how a screen gets its first frame.** Restoring a save is deliberately
quiet — loading a level-4 Tavern publishes no upgrade events — so a screen that built
its state by accumulating change events would come back from a load empty. It waits for
`GameLoaded`, reads current state directly, and treats every other event as a delta.

---

## 4. Verification

1. **It appears.** Press Play. Treasury reads 150 gold, the Guild Hall tab is active,
   three building cards are listed, the tier card shows Village with its requirements in
   red.
2. **The overlay explains before it charges.** Tap the **Inn** card. The overlay shows
   Beds as `— → 2` and Recovery speed as `— → +10%`, with a Build button at 100 gold.
   The em dash is deliberate: an unbuilt building has no current effect, and a zero
   would imply it has one that does nothing.
3. **Buy it.** Tap Build. A toast says the Inn was upgraded, gold drops to 50, the
   treasury sub-line reads `0/2 beds`, and the card reads `Lv 1 / 10`. Tap the scrim to
   dismiss.
4. **Hire.** Roster tab → **Hire** on the Militia Recruit. Toast confirms, they appear
   under Adventurers as Idle at level 1.
5. **Send them out.** Quests tab → **Send a party** on the Rat Infested Cellar. A run
   appears under *Out on quests* with a countdown and a moving progress bar, and a
   repeating standing order appears below it.
6. **Watch it pay.** Leave it running. The bar fills, gold jumps, the adventurer shows
   as Resting with a countdown, and the order restarts on its own once they are home.
   **That is the Week 1 checkpoint.**
7. **A refusal explains itself.** With both beds full, tap Hire again. The toast reads
   "No free bed — upgrade the Inn first" rather than nothing happening.
8. **The tier gate is legible.** Upgrade Tavern, Training Room and Inn and watch the
   requirement values turn green one at a time. Advance stays disabled until every
   clause and the reputation threshold are met.
9. **A load rebuilds the screen.** Open the debug console, press **Save now**, then
   **Reload**. Every screen should come back correct — this is the `GameLoaded` path,
   and if the UI comes back empty it means something is building state from change
   events instead of reading it.
10. **Offline still reads.** Save, stop Play, wait two minutes, Play again. A toast
    reports what the guild earned while away.

### Known gaps, deliberately left for later

- **Dispatch takes whoever is free and always repeats.** Choosing a specific party is
  Day 12's recruitment-and-assignment screen; today's button exists to prove the loop.
- **No art.** Every card is a rectangle. Icons, portraits and backgrounds are Days
  15–17, and the tokens are where their palette will land.
- **The safe area is invisible in the Editor**, which reports the whole screen as safe.
  It is applied and correct; the first device build on Day 22 is where it shows.

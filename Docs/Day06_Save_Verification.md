# Day 6 — Save/load

What was built, where the file lives, the rules that keep it readable as content
changes, and how to check it works. Written to be usable again in Week 3, when
"save/load edge cases" comes back around on Day 20.

---

## 1. What was added

**New — `Core/Services/`**

| File | Role |
|---|---|
| `ISaveStore.cs` | Persistence boundary: read/write/discard/delete a string by key. Sits alongside `IAdService` and `IPurchaseService` for the same reason — a cloud save later is an adapter, not a rewrite. |
| `FileSaveStore.cs` | The shipping implementation. One UTF-8 file per key under `Application.persistentDataPath`, written atomically, with the previous save kept as a backup. |

**New — `App/Saves/`**

| File | Role |
|---|---|
| `SaveGameData.cs` | The wire format: `SaveGameData` plus one small `[Serializable]` class per collection, and `SaveSchema` holding the version constants and the rules for changing them. |
| `SaveCapture.cs` | Running guild → data. Pure transcription, no decisions. |
| `SaveRestore.cs` | Data → running guild, with integrity repair. This is where the interesting rules live. |
| `SaveMigrations.cs` | The version ladder and null normalisation. No steps yet — version 1 is the first version. |
| `GameSaveService.cs` | The policy joining the three: which key, JSON in and out, what happens when it does not parse. |

**Edited**

| File | Change |
|---|---|
| `Guild/GuildState.cs` | `RestoreState(tier, levels)` — sets both together, recalculates once, and stays quiet: it publishes `GuildStatsRecalculated` but not `BuildingUpgraded` or `GuildTierAdvanced`. |
| `App/SimulationClock.cs` | `RestoreCounters(...)` for the lifetime quest counters. |
| `App/GameContent.cs` | `FindTier(id)`, matching the three lookups already there. |
| `Core/Events/GameEvents.cs` | `GameLoaded` — the one "the world is readable, go read it" signal. |
| `App/GameBootstrap.cs` | Loads on wake, pays the absence, autosaves, saves on pause and quit, and drops the PlayerPrefs stamp. |
| `App/DebugConsoleOverlay.cs` | A **Save** section: save, reload, delete, plus the file path and last-save age. |

---

## 2. Where the file lives

The debug console prints the exact path. In general:

| Platform | Path |
|---|---|
| macOS Editor | `~/Library/Application Support/DefaultCompany/Idle_Adventure_Guild/guild_save.json` |
| iOS device | the app sandbox's `Documents/guild_save.json` |

Alongside it you may see:

- `guild_save.json.bak` — the previous save, kept through each atomic write.
- `guild_save.json.tmp` — only ever present if the app died mid-write. Ignored on read.
- `guild_save.json.corrupt-<ticks>` — an unreadable payload, kept rather than deleted.
  These are never cleaned up automatically; delete them by hand once the cause is found.

> The folder name comes from the Company Name and Product Name in Player Settings,
> which are still the template defaults. Changing them (a §04 checklist item) moves
> the save directory, so do it before there is a tester whose progress matters.

---

## 3. The schema

Version 1. Pretty-printed, so it opens readably in any text editor — worth keeping
while testers are sending files back.

```jsonc
{
  "SchemaVersion": 1,
  "SavedAtUtcTicks": 638912345678901234,   // the last-seen stamp; offline income measures from here
  "GameVersion": "0.1",
  "GuildTierId": "village",
  "Buildings":   [ { "Id": "inn", "Level": 1 } ],
  "Currencies":  [ { "Currency": 0, "Amount": 75.0 } ],   // Currency is a CurrencyType value
  "Adventurers": [ {
      "InstanceId": "9f2c…", "DefinitionId": "militia_recruit", "Level": 1,
      "Activity": 1,                                      // AdventurerActivity: 0 Idle, 1 OnQuest, 2 Resting
      "ActiveQuestInstanceId": "4a71…", "RestRemainingSeconds": 0.0 } ],
  "QuestRuns":   [ {
      "InstanceId": "4a71…", "DefinitionId": "rat_cellar",
      "PartyInstanceIds": [ "9f2c…" ],
      "TotalSeconds": 52.0, "RemainingSeconds": 31.4,
      "FailureChance": 0.06, "GoldOnSuccess": 30.0, "ReputationOnSuccess": 3.0 } ],
  "Assignments": [ {
      "Id": "b0e8…", "QuestId": "rat_cellar", "MemberInstanceIds": [ "9f2c…" ],
      "Repeat": true, "ActiveQuestInstanceId": "4a71…" } ],
  "Clock": { "TotalSecondsSimulated": 612.0, "QuestsCompleted": 11, "QuestsSucceeded": 10, "QuestsFailed": 1 }
}
```

Three things about this worth remembering rather than rediscovering:

**A run's numbers are stored, not recomputed.** Duration, failure chance and payout
were fixed when the party was dispatched — that is the Day 4–5 snapshot rule — so a
save and load must carry them across unchanged. Recomputing on load would quietly
re-price every quest in flight, in the player's favour or against it depending on
what they upgraded in between.

**The timestamp is inside the file.** It was in PlayerPrefs as a Day 4 placeholder.
Moving it here means the stamp and the world it describes are written in one atomic
step and cannot disagree — the failure mode it removes is a save that succeeded next
to a stamp that did not, paying offline income for time the guild had already lived
through. The old PlayerPrefs key is deleted on first launch and *not* honoured: the
builds that wrote it never persisted a guild, so the value describes an absence that
nothing was there for.

**Enum-backed values are stored as ints and validated on the way in.** Casting an
arbitrary int to a C# enum succeeds and yields a member that equals nothing — an
adventurer in that state would be neither dispatchable nor recoverable.

### The versioning rule

The rule is about the schema file, not the migration code:

> **Fields are only ever added, never removed and never renamed.** A field that stops
> being used stays declared and unread. A field whose meaning changes gets a new name
> beside the old one.

That is what lets a save two versions old deserialise into today's classes at all —
everything it wrote has somewhere to land, and everything it did not write arrives at
a neutral default. Which in turn means:

- Adding a field with a sensible default needs **no version bump**. This is the common
  case and deliberately the cheap one.
- Bump `SaveSchema.CurrentVersion` only when a load must *do* something to an older
  save — recompute a value, split a field, drop a stale reference — and add the matching
  case to `SaveMigrations.Upgrade`.
- Never renumber the `GuildStat`, `Rarity`, `CurrencyType`, `ModifierKind` or
  `AdventurerActivity` enums. Each one says so in its own comment.
- Never change a `BuildingDefinition`, `QuestDefinition`, `AdventurerDefinition` or
  `GuildTierDefinition` **Id** once a build has shipped. Renaming the asset file is
  harmless; changing the Id field orphans every save that references it.

A save from a *newer* build is refused rather than guessed at — a TestFlight tester
who installs a build and then rolls back will produce one within a week.

### What restoring repairs

A save is never trusted to match today's catalogue. Anything unresolvable is dropped,
the guild around it is left standing, and the count comes back in a
`SaveRestoreReport` that the console shows and the log warns about:

| Situation | What happens |
|---|---|
| Saved tier id not in the catalogue | Falls back to the starting tier; levels and balances kept |
| Saved building id not in the catalogue | That level is lost; other buildings unaffected |
| Adventurer archetype gone | That roster member is dropped, and any order using them |
| Quest asset gone | Runs of it are dropped; the party is sent home |
| Member marked "on quest" for a run that is missing, or that does not list them | Sent home idle — otherwise nothing would ever bring them back |
| Standing order with a partial party | Dropped whole, not trimmed. A trimmed order looks active and never runs again |
| Roster larger than the Inn now houses | Allowed. A rebalanced Inn must not delete people the player trained |

---

## 4. Verification

Do these in order. Steps 1–7 are the day's work; 8–10 are the edge cases, and are
worth the extra ten minutes now rather than on Day 20.

**Setup.** Open `Guild.unity`. On the **Game** object, the bootstrap now has a
**Saving** section: leave *Autosave Interval Seconds* at 30 and *Load Save On Start*
ticked. Leave *Use Fixed Random Seed* as you had it — but see the note at the end.

1. **Clean slate.** Press Play, open the debug console, and in the **Save** section
   press **Delete save**. Stop, then Play again. The section should read
   `saved 0s ago · file present · schema 1 · session new`, and the treasury 150 gold —
   a new guild saves itself immediately so there is a stamp from the first frame.

2. **Make a guild worth saving.** Build the Inn, hire a Militia Recruit, and send them
   on the Rat Infested Cellar. You should have one quest in flight and one standing
   order.

3. **Save and inspect.** Press **Save now**, then open the path printed at the bottom
   of the Save section. Check against §3: `SchemaVersion` 1, the Inn at level 1, one
   adventurer with `Activity: 1`, one quest run whose `RemainingSeconds` is below its
   `TotalSeconds`, and one assignment whose `ActiveQuestInstanceId` matches the run's
   `InstanceId`.

4. **Reload in place.** Note the quest's remaining seconds, then press **Reload**.
   The message reads `Reload: Loaded. No offline time was paid.` and everything comes
   back as the file had it — same gold, same roster, the same quest **continuing** from
   where it was rather than restarting at full duration. That last part is the whole
   point of storing the snapshot.

5. **Restart the session.** Stop Play. Press Play again. The Save section now reads
   `session loaded`. Gold, roster, buildings and the quest are as they were, plus
   whatever the guild earned for the seconds you spent between the two Plays — the
   offline path runs on a restart exactly as it does on a cold launch.

6. **A real absence.** Press **+1 hour**, then **Save now**, then stop Play. Wait two
   or three minutes by the clock. Press Play. The guild should have earned the gap,
   and `LastOfflineReport` is visible as the usual offline line if you press
   **Offline 8h**'s neighbour — or just watch the treasury jump on load.

7. **Quest slots and standing orders survive.** With a repeating order running, save,
   stop, and restart. The order should still be listed and still repeating — if it
   were lost, the guild would finish its current runs and then earn nothing forever,
   which is the failure this whole file exists to prevent.

8. **Corrupt the save.** Stop Play. Open `guild_save.json` and replace the whole
   contents with `{ "nonsense": true }`. Press Play. Expect: a warning naming the
   reason ("it carries no schema version"), a new `guild_save.json.corrupt-<ticks>`
   file next to it, and the guild coming back from `guild_save.json.bak` — which is
   the save from before your last write, so a little older but intact. Nothing should
   throw, and the game should never be left on a black screen.

9. **Corrupt both.** Repeat step 8, but scramble `guild_save.json.bak` as well. Expect
   a new guild with 150 gold, two `.corrupt-` files kept on disk, and a warning saying
   so. Delete the `.corrupt-` files afterwards.

10. **Remove content out from under a save.** With a save that has a Militia Recruit
    on the roster, stop Play, and on `GameContent.asset` temporarily shorten the
    **Adventurers** array so that archetype is gone. Press Play. Expect warnings about
    a dropped roster member and a dropped standing order, the Save section showing
    `Last load repaired: …`, and the guild otherwise intact — buildings, gold and tier
    all still there. **Put the array back** and confirm the next load is clean.

### Two things that are not bugs

**Reloading replays identical quest outcomes** while *Use Fixed Random Seed* is
ticked. The seed is not saved and could not usefully be — `System.Random` has no
recoverable stream position — so a loaded session starts the sequence again from the
top. Untick the seed for anything resembling a balance judgement.

**The save is plain text and trivially editable.** So is the timestamp in it, which
means free offline earnings for anyone who cares to try. That is a Week 3 hardening
item (Day 20), not a launch blocker, and the fix is a checksum rather than encryption —
the goal is to make casual editing visible, not to defeat a determined player.

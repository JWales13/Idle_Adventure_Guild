using System;
using IdleGuild.Adventurers;
using IdleGuild.App.Saves;
using IdleGuild.Core;
using IdleGuild.Guild;
using IdleGuild.Quests;
using UnityEngine;

namespace IdleGuild.App
{
    /// <summary>
    /// A throwaway IMGUI panel for exercising the core loop before any real UI exists.
    ///
    /// IMGUI on purpose: it needs no UXML, no USS and no scene wiring, so it cannot
    /// influence the UI Toolkit work on Day 7, and it runs on a device as well as in the
    /// Editor — which matters when the first build behaves differently from the Editor.
    /// It disables itself outside development builds and is expected to be deleted once
    /// the real screens land.
    ///
    /// It reads the simulation through exactly the public services the UI will use. If
    /// something is awkward to do here, it will be awkward to do in the game.
    ///
    /// Every button queues its work rather than doing it inline. IMGUI lays out and
    /// repaints in separate passes over the same code, so recruiting someone or
    /// cancelling an order in the middle of a pass changes the control count between
    /// them and produces a screenful of layout mismatch errors. Queued actions run at
    /// the start of the next layout pass, when the panel is between frames.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugConsoleOverlay : MonoBehaviour
    {
        [SerializeField] private GameBootstrap _bootstrap;

        [SerializeField, Range(0.25f, 1f)]
        [Tooltip("Fraction of the screen width the panel occupies.")]
        private float _panelWidthFraction = 0.45f;

        private Action _queuedAction;
        private Vector2 _scrollPosition;
        private string _message = "Ready.";
        private bool _isOpen = true;

        private void Awake()
        {
            if (_bootstrap == null)
            {
                // FindAnyObjectByType rather than FindFirstObjectByType: the "first" variant
                // is deprecated for depending on instance ID ordering, and there is only ever
                // one bootstrap in the scene, so any of them is the one we want.
                _bootstrap = FindAnyObjectByType<GameBootstrap>();
            }

            if (!Debug.isDebugBuild && !Application.isEditor)
            {
                enabled = false;
            }
        }

        private void OnGUI()
        {
            if (_bootstrap == null || !_bootstrap.IsReady)
            {
                return;
            }

            if (Event.current.type == EventType.Layout && _queuedAction != null)
            {
                Action action = _queuedAction;
                _queuedAction = null;
                action.Invoke();
            }

            float scale = Mathf.Max(1f, Screen.width / 900f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            float virtualWidth = Screen.width / scale;
            float virtualHeight = Screen.height / scale;
            float panelWidth = _isOpen ? Mathf.Max(300f, virtualWidth * _panelWidthFraction) : 120f;

            GUILayout.BeginArea(new Rect(8f, 8f, panelWidth, virtualHeight - 16f), GUI.skin.box);

            if (GUILayout.Button(_isOpen ? "Hide debug console" : "Debug"))
            {
                _isOpen = !_isOpen;
            }

            if (_isOpen)
            {
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
                DrawTreasury();
                DrawGuild();
                DrawTimeControls();
                DrawSaves();
                DrawBuildings();
                DrawRecruitment();
                DrawRoster();
                DrawQuests();
                GUILayout.Space(6f);
                GUILayout.Label(_message);
                GUILayout.EndScrollView();
            }

            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }

        private void DrawTreasury()
        {
            GameWorld world = _bootstrap.World;
            Section("Treasury");

            GUILayout.Label(
                $"Gold {Amount(world.Economy.Get(CurrencyType.Gold))}    " +
                $"Reputation {Amount(world.Economy.Get(CurrencyType.Reputation))}    " +
                $"Gems {Amount(world.Economy.Get(CurrencyType.Gems))}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+100 g"))
            {
                Queue(() => world.Economy.Grant(CurrencyType.Gold, 100d));
            }

            if (GUILayout.Button("+10k g"))
            {
                Queue(() => world.Economy.Grant(CurrencyType.Gold, 10000d));
            }

            if (GUILayout.Button("+100 rep"))
            {
                Queue(() => world.Economy.Grant(CurrencyType.Reputation, 100d));
            }

            GUILayout.EndHorizontal();
        }

        private void DrawGuild()
        {
            GameWorld world = _bootstrap.World;
            IGuildStats stats = world.Stats;
            Section($"Guild — {world.GuildState.CurrentTier.DisplayName} (order {world.GuildState.CurrentTier.Order})");

            GUILayout.Label(
                $"Reward yield x{stats.Get(GuildStat.RewardYield):F2}   " +
                $"Power +{stats.Get(GuildStat.AdventurerPower):F1}   " +
                $"Recovery x{stats.Get(GuildStat.RecoverySpeed):F2}");

            GUILayout.Label(
                $"Beds {world.Roster.Count}/{world.Roster.CapacityWith(stats)}   " +
                $"Quest slots {world.QuestLog.ActiveCount}/{world.QuestLog.SlotsWith(stats)}   " +
                $"Max quest tier {Mathf.FloorToInt(stats.Get(GuildStat.MaxQuestTier))}   " +
                $"Recruits to {RarityFrom(stats)}");

            TierAdvanceOutcome tierState = _bootstrap.Tiers.Preview();
            GuildTierDefinition nextTier = _bootstrap.Tiers.NextTier();
            string nextLabel = nextTier != null ? nextTier.DisplayName : "nothing";

            GUI.enabled = tierState == TierAdvanceOutcome.Advanced;
            if (GUILayout.Button($"Advance to {nextLabel}  [{tierState}]"))
            {
                Queue(() => _message = $"Tier advance: {_bootstrap.Tiers.TryAdvance()}");
            }

            GUI.enabled = true;
        }

        private void DrawTimeControls()
        {
            Section("Time");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 min"))
            {
                Queue(() => AdvanceAndReport(60d, "1 minute"));
            }

            if (GUILayout.Button("+10 min"))
            {
                Queue(() => AdvanceAndReport(600d, "10 minutes"));
            }

            if (GUILayout.Button("+1 hour"))
            {
                Queue(() => AdvanceAndReport(3600d, "1 hour"));
            }

            if (GUILayout.Button("Offline 8h"))
            {
                Queue(ReportEightHoursOffline);
            }

            GUILayout.EndHorizontal();

            SimulationClock clock = _bootstrap.Clock;
            GUILayout.Label(
                $"Simulated {clock.TotalSecondsSimulated / 60d:F1} min   " +
                $"Quests {clock.QuestsSucceeded} ok / {clock.QuestsFailed} failed");
        }

        private void DrawSaves()
        {
            GameSaveService saves = _bootstrap.Saves;
            Section("Save");

            string age = saves.LastSaveUtc == DateTime.MinValue
                ? "never saved this session"
                : $"saved {(DateTime.UtcNow - saves.LastSaveUtc).TotalSeconds:F0}s ago";

            GUILayout.Label(
                $"{age}   file {(saves.HasSave ? "present" : "absent")}   " +
                $"schema {SaveSchema.CurrentVersion}   " +
                $"session {(_bootstrap.LoadedFromSave ? "loaded" : "new")}");

            if (saves.LastRestoreReport.HasRepairs)
            {
                GUILayout.Label($"Last load repaired: {saves.LastRestoreReport}");
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save now"))
            {
                Queue(() => _message = _bootstrap.Save("debug console")
                    ? "Saved."
                    : "Save failed — see the console for why.");
            }

            if (GUILayout.Button("Reload"))
            {
                Queue(() => _message = $"Reload: {_bootstrap.ReloadFromSave()}. No offline time was paid.");
            }

            if (GUILayout.Button("Start over"))
            {
                // Deliberately not a bare Saves.Delete(): that removes the file and
                // leaves the guild running, so the next autosave puts it straight back.
                Queue(() =>
                {
                    _bootstrap.StartNewGuild();
                    _message = "Started a new guild. The old save is gone.";
                });
            }

            GUILayout.EndHorizontal();

            // Worth showing rather than hiding behind a log line: on a device this is the
            // only way to find out where the file a tester is describing actually lives.
            GUILayout.Label(saves.Location);
        }

        private void DrawBuildings()
        {
            Section("Buildings");
            foreach (BuildingDefinition building in _bootstrap.World.Content.Buildings)
            {
                if (building == null)
                {
                    continue;
                }

                BuildingDefinition target = building;
                int level = _bootstrap.World.GuildState.GetLevel(target.Id);
                UpgradeOutcome state = _bootstrap.Buildings.Preview(target);
                string action = level == 0 ? "Build" : "Upgrade";

                GUI.enabled = state == UpgradeOutcome.Upgraded;
                if (GUILayout.Button(
                        $"{action} {target.DisplayName}  L{level}/{target.MaxLevel}  " +
                        $"{Amount(_bootstrap.Buildings.CostOfNextLevel(target))} g  [{state}]"))
                {
                    Queue(() => _message = $"{target.DisplayName}: {_bootstrap.Buildings.TryUpgrade(target)}");
                }

                GUI.enabled = true;
            }
        }

        private void DrawRecruitment()
        {
            Section("Recruit");
            foreach (AdventurerDefinition definition in _bootstrap.World.Content.Adventurers)
            {
                if (definition == null)
                {
                    continue;
                }

                AdventurerDefinition target = definition;
                RecruitOutcome state = _bootstrap.Recruitment.Preview(target);

                GUI.enabled = state == RecruitOutcome.Recruited;
                if (GUILayout.Button(
                        $"Hire {target.DisplayName} ({target.Rarity})  {Amount(target.RecruitCostGold)} g  [{state}]"))
                {
                    Queue(() =>
                        _message = $"Recruit {target.DisplayName}: {_bootstrap.Recruitment.TryRecruit(target, out Adventurer _)}");
                }

                GUI.enabled = true;
            }
        }

        private void DrawRoster()
        {
            Section($"Roster ({_bootstrap.World.Roster.Count})");
            foreach (Adventurer member in _bootstrap.World.Roster.Members)
            {
                Adventurer target = member;
                string activity = target.Activity == AdventurerActivity.Resting
                    ? $"resting {target.RestRemainingSeconds:F0}s"
                    : target.Activity.ToString().ToLowerInvariant();

                GUILayout.Label(
                    $"{target.Definition.DisplayName}  L{target.Level}  " +
                    $"power {target.PowerWith(_bootstrap.World.Stats):F1}  {activity}");

                TrainingOutcome state = _bootstrap.Training.Preview(target);
                GUI.enabled = state == TrainingOutcome.Trained;
                if (GUILayout.Button(
                        $"Train to L{target.Level + 1}  {Amount(_bootstrap.Training.CostOfNextLevel(target))} g  [{state}]"))
                {
                    Queue(() => _message = $"Train {target.Definition.DisplayName}: {_bootstrap.Training.TryLevelUp(target)}");
                }

                GUI.enabled = true;
            }
        }

        private void DrawQuests()
        {
            Section("Quests");
            foreach (QuestDefinition quest in _bootstrap.World.Content.Quests)
            {
                if (quest == null)
                {
                    continue;
                }

                QuestDefinition target = quest;
                GUI.enabled = _bootstrap.Dispatch.IsAvailable(target);
                if (GUILayout.Button(
                        $"Send party on {target.DisplayName}  (tier {target.QuestTier}, needs {target.RequiredAdventurers})"))
                {
                    Queue(() => DispatchAvailableParty(target));
                }

                GUI.enabled = true;
            }

            GUILayout.Space(4f);
            GUILayout.Label($"In flight ({_bootstrap.World.QuestLog.ActiveCount}):");
            foreach (ActiveQuest run in _bootstrap.World.QuestLog.Active)
            {
                GUILayout.Label(
                    $"   {run.Definition.DisplayName}  {run.RemainingSeconds:F0}s left  " +
                    $"{run.Progress01 * 100f:F0}%  fail {run.FailureChance * 100f:F0}%  " +
                    $"pays {Amount(run.GoldOnSuccess)} g");
            }

            GUILayout.Label($"Standing orders ({_bootstrap.World.Assignments.Count}):");
            foreach (QuestAssignment assignment in _bootstrap.World.Assignments)
            {
                QuestAssignment target = assignment;
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    $"   {target.Quest.DisplayName}  {target.MemberInstanceIds.Count} member(s)  " +
                    $"{(target.Repeat ? "repeating" : "one-off")}  {(target.IsRunning ? "out" : "home")}");

                if (GUILayout.Button("Recall", GUILayout.Width(70f)))
                {
                    Queue(() =>
                    {
                        // Say which of the two things happened. Claiming a recall while
                        // the run visibly continues is what sent the Day 14 playtest
                        // hunting for a bug in the simulation.
                        bool wasRunning = target.IsRunning;
                        _bootstrap.Dispatch.Cancel(target.Id);
                        _message = wasRunning
                            ? $"{target.Quest.DisplayName}: standing down when this run lands."
                            : $"{target.Quest.DisplayName}: order closed, party free.";
                    });
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DispatchAvailableParty(QuestDefinition quest)
        {
            DispatchOutcome outcome = _bootstrap.Dispatch.TryDispatchAvailableParty(quest, true, out QuestAssignment _);
            _message = $"Dispatch {quest.DisplayName}: {outcome}";
        }

        private void AdvanceAndReport(double seconds, string label)
        {
            _bootstrap.Clock.Advance(seconds);
            _message = $"Advanced {label}.";
        }

        private void ReportEightHoursOffline()
        {
            OfflineReport report = OfflineProgress.CatchUp(_bootstrap.World, _bootstrap.Clock, 8d * 3600d);
            _message =
                $"Offline {report.SecondsSimulated / 3600d:F1}h: {Amount(report.GoldEarned)} g, " +
                $"{Amount(report.ReputationEarned)} rep, {report.QuestsCompleted} quests " +
                $"({report.SecondsForfeited / 3600d:F1}h forfeited).";
        }

        private void Queue(Action action)
        {
            _queuedAction = action;
        }

        private static Rarity RarityFrom(IGuildStats stats)
        {
            int raw = Mathf.FloorToInt(stats.Get(GuildStat.RecruitableRarity));
            return (Rarity)Mathf.Clamp(raw, (int)Rarity.Common, (int)Rarity.Legendary);
        }

        private static void Section(string title)
        {
            GUILayout.Space(8f);
            GUILayout.Label($"— {title} —");
        }

        /// <summary>Whole numbers for readability; the real UI gets proper abbreviation on Day 7.</summary>
        private static string Amount(double value)
        {
            return value.ToString("N0");
        }
    }
}

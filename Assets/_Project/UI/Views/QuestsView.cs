using System.Collections.Generic;
using IdleGuild.App;
using IdleGuild.Quests;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// Work: what the guild can take on, what is out right now, and what it has
    /// standing orders to keep doing.
    ///
    /// The three sections are deliberately distinct things rather than one list. A quest
    /// is a job that exists; a run is one attempt at it; a standing order is the
    /// instruction that keeps making attempts. Collapsing them would be tidier on screen
    /// and would hide the only mechanism in the game that earns money while the app is
    /// closed.
    ///
    /// Dispatching creates a repeating order by default. A one-off would be a smaller
    /// promise and a worse first experience: the player taps once, sees a party leave,
    /// comes back later to gold. Recall is how they stop it, and it lets the current run
    /// finish rather than abandoning it.
    /// </summary>
    public sealed class QuestsView : ScrollView
    {
        private readonly List<OfferCard> _offers = new List<OfferCard>();
        private readonly List<RunRow> _runs = new List<RunRow>();

        private GuildContext _context;
        private VisualElement _runsContainer;
        private VisualElement _ordersContainer;

        public QuestsView()
            : base(ScrollViewMode.Vertical)
        {
            AddToClassList("guild-screen");
        }

        public void Rebuild(GuildContext context)
        {
            _context = context;
            Clear();
            _offers.Clear();
            _runs.Clear();

            Add(Ui.Text("Available work", "section-title", "section-title--first"));
            foreach (QuestDefinition quest in context.World.Content.Quests)
            {
                if (quest == null)
                {
                    continue;
                }

                OfferCard offer = new OfferCard(quest, this);
                _offers.Add(offer);
                Add(offer);
            }

            Add(Ui.Text("Out on quests", "section-title"));
            _runsContainer = Ui.Box();
            Add(_runsContainer);
            RebuildRuns(context);

            Add(Ui.Text("Standing orders", "section-title"));
            _ordersContainer = Ui.Box();
            Add(_ordersContainer);
            RebuildOrders(context);

            Refresh(context);
        }

        public void Refresh(GuildContext context)
        {
            _context = context;

            foreach (OfferCard offer in _offers)
            {
                offer.Refresh(context);
            }

            // A run that finished between rebuilds leaves its row pointing at nothing.
            // Rather than guess, each row re-finds its run and hides itself when it is
            // gone; the QuestCompleted event has already asked for a rebuild by then.
            foreach (RunRow row in _runs)
            {
                row.Refresh(context);
            }
        }

        private void RebuildRuns(GuildContext context)
        {
            _runsContainer.Clear();
            _runs.Clear();

            if (context.World.QuestLog.ActiveCount == 0)
            {
                _runsContainer.Add(Ui.Text("Nobody is out at the moment.", "empty"));
                return;
            }

            foreach (ActiveQuest run in context.World.QuestLog.Active)
            {
                RunRow row = new RunRow(run.InstanceId);
                _runs.Add(row);
                _runsContainer.Add(row);
            }
        }

        private void RebuildOrders(GuildContext context)
        {
            _ordersContainer.Clear();

            if (context.World.Assignments.Count == 0)
            {
                _ordersContainer.Add(Ui.Text("No standing orders. Send a party to create one.", "empty"));
                return;
            }

            foreach (QuestAssignment assignment in context.World.Assignments)
            {
                _ordersContainer.Add(BuildOrderCard(assignment));
            }
        }

        private VisualElement BuildOrderCard(QuestAssignment assignment)
        {
            VisualElement card = Ui.Box("card");

            VisualElement header = Ui.Box("card__header");
            header.Add(Ui.Text(assignment.Quest.DisplayName, "card__title"));
            header.Add(Ui.Text(assignment.Repeat ? "Repeating" : "One-off", "badge"));
            card.Add(header);

            card.Add(Ui.Text(
                $"{assignment.MemberInstanceIds.Count} adventurer(s) · " +
                $"{(assignment.IsRunning ? "out now" : "resting between runs")}",
                "card__meta"));

            VisualElement actions = Ui.Box("card__row");
            actions.Add(Ui.Action("Recall", () => Recall(assignment), "button--wide"));
            card.Add(actions);

            return card;
        }

        private void Recall(QuestAssignment assignment)
        {
            if (_context == null)
            {
                return;
            }

            _context.Dispatch.Cancel(assignment.Id);
            _context.Report(
                $"The {assignment.Quest.DisplayName} party will stand down after this run.",
                true);
        }

        private void Dispatch(QuestDefinition quest)
        {
            if (_context == null)
            {
                return;
            }

            DispatchOutcome outcome = _context.Dispatch.TryDispatchAvailableParty(quest, true, out QuestAssignment _);
            _context.Report(Outcomes.Describe(outcome, quest.DisplayName), Outcomes.Succeeded(outcome));
        }

        /// <summary>A job the guild can take on, and the button that takes it on.</summary>
        private sealed class OfferCard : VisualElement
        {
            private readonly QuestDefinition _quest;
            private readonly Label _detail;
            private readonly Button _send;

            internal OfferCard(QuestDefinition quest, QuestsView owner)
            {
                _quest = quest;
                AddToClassList("card");

                VisualElement header = Ui.Box("card__header");
                header.Add(Ui.Text(quest.DisplayName, "card__title"));
                header.Add(Ui.Text($"Tier {quest.QuestTier}", "badge"));
                Add(header);

                _detail = Ui.Text(string.Empty, "card__meta");
                Add(_detail);

                VisualElement actions = Ui.Box("card__row");
                _send = Ui.Action("Send a party", () => owner.Dispatch(quest), "button--primary", "button--wide");
                actions.Add(_send);
                Add(actions);
            }

            internal void Refresh(GuildContext context)
            {
                bool available = context.Dispatch.IsAvailable(_quest);
                EnableInClassList("card--locked", !available);

                _detail.text = available
                    ? $"{_quest.RequiredAdventurers} adventurer(s) · " +
                      $"{Format.Duration(_quest.BaseDurationSeconds)} at matched power · " +
                      $"pays {Format.Amount(_quest.GoldReward)} gold, {Format.Amount(_quest.ReputationReward)} rep"
                    : "Beyond the guild's reach for now.";

                _send.SetEnabled(available);
            }
        }

        /// <summary>One run in flight, with the numbers it was dispatched under.</summary>
        private sealed class RunRow : VisualElement
        {
            private readonly string _instanceId;
            private readonly Label _title;
            private readonly Label _detail;
            private readonly VisualElement _fill;

            internal RunRow(string instanceId)
            {
                _instanceId = instanceId;
                AddToClassList("card");

                VisualElement header = Ui.Box("card__header");
                _title = Ui.Text(string.Empty, "card__title");
                header.Add(_title);
                Add(header);

                _detail = Ui.Text(string.Empty, "card__meta");
                Add(_detail);
                Add(Ui.Progress(out _fill));
            }

            internal void Refresh(GuildContext context)
            {
                ActiveQuest run = context.World.QuestLog.Find(_instanceId);
                if (run == null)
                {
                    style.display = DisplayStyle.None;
                    return;
                }

                style.display = DisplayStyle.Flex;
                _title.text = run.Definition.DisplayName;
                _detail.text =
                    $"{Format.Duration(run.RemainingSeconds)} left · " +
                    $"{Format.Percent(run.FailureChance)} risk · " +
                    $"pays {Format.Amount(run.GoldOnSuccess)} gold";

                Ui.SetProgress(_fill, run.Progress01);
            }
        }
    }
}

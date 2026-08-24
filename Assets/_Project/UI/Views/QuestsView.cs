using System;
using System.Collections.Generic;
using System.Text;
using IdleGuild.Adventurers;
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
    ///
    /// Day 12 gave the order card the other action it was missing. An order used to hold
    /// its party for life, so a card that said "3 adventurer(s)" was describing a
    /// decision the player could no longer see or change — which is how the best hire in
    /// the game ended up on the bench. It now names them and offers to re-form them.
    /// </summary>
    public sealed class QuestsView : ScrollView
    {
        private readonly List<OfferCard> _offers = new List<OfferCard>();
        private readonly List<RunRow> _runs = new List<RunRow>();
        private readonly Action<PartyRequest> _chooseParty;

        private GuildContext _context;
        private VisualElement _runsContainer;
        private VisualElement _ordersContainer;

        /// <param name="chooseParty">
        /// Raises the party picker over whatever is on screen. Passed in for the same
        /// reason the roster's confirmation is: a view does not reach for the chrome
        /// around it.
        /// </param>
        public QuestsView(Action<PartyRequest> chooseParty)
            : base(ScrollViewMode.Vertical)
        {
            _chooseParty = chooseParty;
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

        /// <summary>
        /// Order cards are rebuilt rather than refreshed, because the only things about
        /// them that change — the party and whether the order still exists — are both
        /// announced as events. <c>QuestPartyReformed</c> exists precisely so that a
        /// re-formed party is not left listing its old members until something unrelated
        /// happens to redraw this screen.
        /// </summary>
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
                _ordersContainer.Add(BuildOrderCard(context, assignment));
            }
        }

        private VisualElement BuildOrderCard(GuildContext context, QuestAssignment assignment)
        {
            VisualElement card = Ui.Box("card");

            VisualElement header = Ui.Box("card__header");
            header.Add(Ui.Text(assignment.Quest.DisplayName, "card__title"));
            header.Add(Ui.Text(assignment.Repeat ? "Repeating" : "One-off", "badge"));
            card.Add(header);

            card.Add(Ui.Text(PartyNames(context, assignment), "card__meta"));
            card.Add(Ui.Text(assignment.IsRunning ? "Out now." : "Resting between runs.", "card__meta"));

            VisualElement actions = Ui.Box("card__row");
            actions.Add(Ui.Action("Re-form party", () => ReformParty(assignment.Id), "button--wide"));
            actions.Add(Ui.Action("Recall", () => Recall(assignment.Id), "button--small", "button--spaced"));
            card.Add(actions);

            return card;
        }

        /// <summary>
        /// The party, by name. An order whose party could not be resolved should not
        /// exist — save restoration drops an assignment whose members it cannot find
        /// rather than leaving a partial one — so the fallback here is a guard against a
        /// future bug rather than a case that happens today.
        /// </summary>
        private static string PartyNames(GuildContext context, QuestAssignment assignment)
        {
            if (assignment.MemberInstanceIds.Count == 0)
            {
                return "Nobody assigned.";
            }

            StringBuilder names = new StringBuilder();
            foreach (string memberId in assignment.MemberInstanceIds)
            {
                if (names.Length > 0)
                {
                    names.Append(", ");
                }

                Adventurer member = context.World.Roster.Find(memberId);
                names.Append(member != null ? member.Definition.DisplayName : "someone no longer on the roster");
            }

            return names.ToString();
        }

        /// <summary>
        /// Orders are addressed by id rather than by reference for the same reason roster
        /// cards are: a save loaded between building this card and tapping it rebuilds
        /// every assignment object in the world.
        /// </summary>
        private void ReformParty(string assignmentId)
        {
            QuestAssignment assignment = _context?.World.FindAssignment(assignmentId);
            if (assignment == null)
            {
                _context?.Report(Outcomes.Describe(DispatchOutcome.UnknownOrder, string.Empty), false);
                return;
            }

            _chooseParty?.Invoke(PartyRequest.ForExistingOrder(assignment));
        }

        private void Recall(string assignmentId)
        {
            QuestAssignment assignment = _context?.World.FindAssignment(assignmentId);
            if (assignment == null)
            {
                return;
            }

            string questName = assignment.Quest.DisplayName;
            _context.Dispatch.Cancel(assignmentId);
            _context.Report($"The {questName} party will stand down after this run.", true);
        }

        private void Dispatch(QuestDefinition quest)
        {
            _chooseParty?.Invoke(PartyRequest.ForNewOrder(quest));
        }

        /// <summary>A job the guild can take on, and the button that opens the party picker for it.</summary>
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

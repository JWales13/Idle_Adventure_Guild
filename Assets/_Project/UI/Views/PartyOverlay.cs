using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.App;
using IdleGuild.Quests;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// Which party the player is being asked to assemble: a new one for a quest, or the
    /// replacement for a standing order that already exists.
    ///
    /// The order is carried as an id rather than as the object, because a non-repeating
    /// order can finish and be removed from the world while the picker is still on
    /// screen. An id that no longer resolves is a sentence the service already knows how
    /// to produce; a stale reference is a party silently re-formed onto nothing.
    /// </summary>
    public readonly struct PartyRequest
    {
        private PartyRequest(QuestDefinition quest, string orderId)
        {
            Quest = quest;
            OrderId = orderId;
        }

        public QuestDefinition Quest { get; }

        /// <summary>The standing order being re-formed, or null when this is a first dispatch.</summary>
        public string OrderId { get; }

        public bool IsReform => !string.IsNullOrEmpty(OrderId);

        public static PartyRequest ForNewOrder(QuestDefinition quest)
        {
            return new PartyRequest(quest, null);
        }

        public static PartyRequest ForExistingOrder(QuestAssignment order)
        {
            return order == null
                ? default
                : new PartyRequest(order.Quest, order.Id);
        }
    }

    /// <summary>
    /// Choosing who goes, for a quest about to be taken on and for an order already
    /// running.
    ///
    /// One screen serves both on purpose. They are the same question asked at two
    /// moments, and the second one is the one the game was missing: a standing order used
    /// to hold its party for life, so hiring a Dragonsworn Champion changed nothing at
    /// all until the player worked out unaided that they had to recall the order and
    /// dispatch again. The best adventurer in the game could sit on the bench for the
    /// rest of the run with nothing on screen admitting it.
    ///
    /// The numbers along the top are why this is a picker rather than a list of
    /// checkboxes. <c>PartyPower</c> and <c>PreviewDurationSeconds</c> have existed since
    /// Day 4–5 with no caller; they turn "swap the Recruit for the Champion" from a guess
    /// into a comparison the player makes before committing, which is the whole argument
    /// for letting them choose at all.
    ///
    /// Rows are built when the overlay opens and are not rebuilt while it is up. Nothing
    /// the player can reach from here changes who is on the roster — hiring and retiring
    /// both live on the screen underneath — and rebuilding mid-choice would throw away a
    /// selection they were halfway through making. What does change while it is open is
    /// who is *free*, and that is re-read on every refresh.
    /// </summary>
    public sealed class PartyOverlay : VisualElement
    {
        private readonly List<string> _selected = new List<string>();
        private readonly List<CandidateRow> _rows = new List<CandidateRow>();

        private readonly Label _title;
        private readonly Label _subtitle;
        private readonly VisualElement _summary;
        private readonly VisualElement _candidates;
        private readonly Label _explanation;
        private readonly Button _commit;

        private GuildContext _context;
        private QuestDefinition _quest;
        private string _orderId;

        public PartyOverlay()
        {
            AddToClassList("overlay");
            AddToClassList("overlay--hidden");

            // A full Inn is sixteen rows, which is taller than the phone. The panel is
            // capped at the height of the screen and the list inside it is the part that
            // gives, so the summary and the buttons stay put while the roster scrolls.
            VisualElement panel = Ui.Box("overlay__panel", "overlay__panel--tall");
            _title = Ui.Text(string.Empty, "overlay__title");
            _subtitle = Ui.Text(string.Empty, "card__subtitle");
            _summary = Ui.Box("stat-row");

            ScrollView body = Ui.Scroll("overlay__body", "overlay__body--flexible");
            _candidates = Ui.Box();
            body.Add(_candidates);

            _explanation = Ui.Text(string.Empty, "card__meta");

            VisualElement actions = Ui.Box("overlay__actions");
            _commit = Ui.Action(string.Empty, Commit, "button--primary", "button--wide");
            Button autoFill = Ui.Action("Best available", AutoFill, "button--spaced");
            Button close = Ui.Action("Cancel", Close, "button--spaced");
            actions.Add(_commit);
            actions.Add(autoFill);
            actions.Add(close);

            panel.Add(_title);
            panel.Add(_subtitle);
            panel.Add(_summary);
            panel.Add(body);
            panel.Add(_explanation);
            panel.Add(actions);
            Add(panel);

            RegisterCallback<ClickEvent>(_ => Close());
            panel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        }

        public bool IsOpen => !ClassListContains("overlay--hidden");

        public void Open(GuildContext context, PartyRequest request)
        {
            if (context == null || request.Quest == null)
            {
                return;
            }

            _context = context;
            _quest = request.Quest;
            _orderId = request.OrderId;

            _title.text = _quest.DisplayName;
            _subtitle.text = request.IsReform
                ? "Whoever you choose goes out on the next run. The party already in the field finishes as it stands."
                : $"Choose {_quest.RequiredAdventurers} to send. They keep taking this work until you recall them.";

            _commit.text = request.IsReform ? "Re-form party" : "Send party";

            BuildCandidates(context);
            SelectStartingParty(context, request);

            RemoveFromClassList("overlay--hidden");
            Refresh(context);
        }

        public void Close()
        {
            AddToClassList("overlay--hidden");
            _quest = null;
            _orderId = null;
            _selected.Clear();
        }

        public void Refresh(GuildContext context)
        {
            if (!IsOpen || _quest == null)
            {
                return;
            }

            _context = context;
            QuestAssignment order = CurrentOrder(context);

            PruneDeparted(context);

            bool partyIsFull = _selected.Count >= _quest.RequiredAdventurers;
            foreach (CandidateRow row in _rows)
            {
                row.Refresh(context, order, _selected.Contains(row.InstanceId), partyIsFull);
            }

            RefreshSummary(context);

            DispatchOutcome state = CurrentState(context);
            _explanation.text = state == DispatchOutcome.Dispatched
                ? string.Empty
                : Outcomes.Describe(state, _quest.DisplayName);
            _commit.SetEnabled(state == DispatchOutcome.Dispatched);
        }

        /// <summary>
        /// What the service says about the selection as it stands. Asked of the service
        /// rather than worked out here, so the sentence under the button and the rule
        /// behind it can never disagree.
        /// </summary>
        private DispatchOutcome CurrentState(GuildContext context)
        {
            return string.IsNullOrEmpty(_orderId)
                ? context.Dispatch.Preview(_quest, _selected)
                : context.Dispatch.PreviewReform(_orderId, _selected);
        }

        private QuestAssignment CurrentOrder(GuildContext context)
        {
            return string.IsNullOrEmpty(_orderId) ? null : context.World.FindAssignment(_orderId);
        }

        private void BuildCandidates(GuildContext context)
        {
            _candidates.Clear();
            _rows.Clear();

            if (context.World.Roster.Count == 0)
            {
                _candidates.Add(Ui.Text("Nobody works here yet. Hire someone on the Roster screen.", "empty"));
                return;
            }

            foreach (Adventurer member in context.World.Roster.Members)
            {
                CandidateRow row = new CandidateRow(member.InstanceId, this);
                _rows.Add(row);
                _candidates.Add(row);
            }
        }

        /// <summary>
        /// What the picker opens with. Re-forming starts from the party that is already
        /// on the order, so the player edits rather than rebuilds; a first dispatch starts
        /// from the service's own suggestion, which is the same party the "Send a party"
        /// button used to pick without asking.
        /// </summary>
        private void SelectStartingParty(GuildContext context, PartyRequest request)
        {
            _selected.Clear();

            QuestAssignment order = CurrentOrder(context);
            if (request.IsReform && order != null)
            {
                foreach (string memberId in order.MemberInstanceIds)
                {
                    _selected.Add(memberId);
                }

                return;
            }

            context.Dispatch.SuggestParty(_quest, _selected, order);
        }

        private void AutoFill()
        {
            if (_context == null || _quest == null)
            {
                return;
            }

            _context.Dispatch.SuggestParty(_quest, _selected, CurrentOrder(_context));
            Refresh(_context);
        }

        private void Toggle(string instanceId)
        {
            if (_context == null || _quest == null)
            {
                return;
            }

            if (!_selected.Remove(instanceId))
            {
                _selected.Add(instanceId);
            }

            Refresh(_context);
        }

        /// <summary>
        /// Drop anyone who has left the roster since the picker opened. Retiring happens
        /// on the screen behind this one so it cannot happen mid-choice today, but a
        /// selection holding an id that no longer resolves would count as zero power and
        /// fail with a sentence about availability rather than about absence.
        /// </summary>
        private void PruneDeparted(GuildContext context)
        {
            for (int index = _selected.Count - 1; index >= 0; index--)
            {
                if (context.World.Roster.Find(_selected[index]) == null)
                {
                    _selected.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Party size, combined power, and what the quest would take at that strength.
        ///
        /// The duration is shown only once the party is the size the quest asks for. A
        /// figure for two of three adventurers is arithmetically real and practically a
        /// lie — it is how long a run would take that this quest will never accept.
        /// </summary>
        private void RefreshSummary(GuildContext context)
        {
            _summary.Clear();
            _summary.Add(Ui.Stat("Party", $"{_selected.Count}/{_quest.RequiredAdventurers}"));
            _summary.Add(Ui.Stat("Power", $"{context.Dispatch.PartyPower(_selected):0.#}"));

            string duration = _selected.Count == _quest.RequiredAdventurers
                ? Format.Duration(context.Dispatch.PreviewDurationSeconds(_quest, _selected))
                : "—";
            _summary.Add(Ui.Stat("Takes", duration));
        }

        private void Commit()
        {
            if (_context == null || _quest == null)
            {
                return;
            }

            string questName = _quest.DisplayName;
            DispatchOutcome outcome;
            string message;

            if (string.IsNullOrEmpty(_orderId))
            {
                outcome = _context.Dispatch.TryDispatch(_quest, _selected, true, out QuestAssignment _);
                message = Outcomes.Describe(outcome, questName);
            }
            else
            {
                outcome = _context.Dispatch.TryReformParty(_orderId, _selected);

                // Dispatched is the service's word for "it worked", and its stock sentence
                // says a party set out — which is exactly what a re-form does not do.
                message = outcome == DispatchOutcome.Dispatched
                    ? $"The {questName} party changes from the next run."
                    : Outcomes.Describe(outcome, questName);
            }

            _context.Report(message, Outcomes.Succeeded(outcome));

            if (Outcomes.Succeeded(outcome))
            {
                Close();
            }
            else
            {
                Refresh(_context);
            }
        }

        /// <summary>
        /// One adventurer the player can put in or take out of the party.
        ///
        /// Built as a plain element with a click handler rather than as a Button or a
        /// Toggle: both of those carry a text element and a nest of theme styles that
        /// would have to be undone before the row matched anything else on screen, which
        /// is the same reason <c>Ui.Progress</c> does not use ProgressBar.
        /// </summary>
        private sealed class CandidateRow : VisualElement
        {
            private readonly VisualElement _mark;
            private readonly Label _name;
            private readonly Label _meta;

            internal CandidateRow(string instanceId, PartyOverlay owner)
            {
                InstanceId = instanceId;
                AddToClassList("party-row");

                _mark = Ui.Box("party-row__mark");

                VisualElement text = Ui.Box("party-row__text");
                _name = Ui.Text(string.Empty, "party-row__name");
                _meta = Ui.Text(string.Empty, "party-row__meta");
                text.Add(_name);
                text.Add(_meta);

                Add(_mark);
                Add(text);

                RegisterCallback<ClickEvent>(_ => owner.Toggle(instanceId));
            }

            internal string InstanceId { get; }

            internal void Refresh(GuildContext context, QuestAssignment order, bool selected, bool partyIsFull)
            {
                Adventurer member = context.World.Roster.Find(InstanceId);
                if (member == null)
                {
                    style.display = DisplayStyle.None;
                    return;
                }

                style.display = DisplayStyle.Flex;

                _name.text = member.Definition.DisplayName;
                _name.EnableInClassList(Format.RarityClass(member.Definition.Rarity), true);
                _meta.text =
                    $"Level {member.Level} · power {member.PowerWith(context.Stats):0.#} · " +
                    Commitment(context, member, order);

                bool free = context.Dispatch.IsFreeForParty(InstanceId, order);
                EnableInClassList("party-row--selected", selected);
                _mark.EnableInClassList("party-row__mark--checked", selected);

                // Someone already chosen stays tappable so they can be taken back out,
                // even once the party is full — otherwise a full party could only be
                // changed by cancelling out of the picker entirely.
                SetEnabled(selected || (free && !partyIsFull));
            }

            /// <summary>
            /// What this adventurer is doing, in the terms that matter to the choice being
            /// made. Every branch is a state read; none of them decides anything.
            /// </summary>
            private static string Commitment(GuildContext context, Adventurer member, QuestAssignment order)
            {
                QuestAssignment committedTo = context.World.FindAssignmentFor(member.InstanceId);

                if (order != null && committedTo == order)
                {
                    return member.Activity == AdventurerActivity.OnQuest ? "out on this order" : "on this order";
                }

                if (committedTo != null)
                {
                    return $"on the {committedTo.Quest.DisplayName} order";
                }

                return member.Activity switch
                {
                    AdventurerActivity.OnQuest => "out on a quest",
                    AdventurerActivity.Resting => $"resting {Format.Duration(member.RestRemainingSeconds)}",
                    _ => "free"
                };
            }
        }
    }
}

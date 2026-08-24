using System;
using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.App;
using IdleGuild.Core;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// People: who works here, what they can do, who else could be persuaded to join,
    /// and — since Day 12 — who can be let go.
    ///
    /// Hiring and the roster share a screen because the three gates in front of
    /// recruitment — the guild's tier, the Tavern's pull, the Inn's beds — are only
    /// legible next to the roster they constrain. A separate hiring screen would show a
    /// greyed-out button with no visible reason beside it.
    ///
    /// Retiring belongs on the same screen for the same reason, and it is here because
    /// the Inn tops out at sixteen beds while a Capital guild fields twelve. Without a
    /// way out, a bed spent on the wrong archetype during City was spent for the rest of
    /// the run, and the player who filled their spare beds with Epics could never hire
    /// the Legendary that Capital exists to unlock. The bed count at the top of this
    /// screen is the number that decision is made against, which is why the two sit
    /// together.
    /// </summary>
    public sealed class RosterView : ScrollView
    {
        private readonly List<MemberCard> _members = new List<MemberCard>();
        private readonly List<HireCard> _hires = new List<HireCard>();
        private readonly Action<ConfirmRequest> _ask;

        private GuildContext _context;
        private VisualElement _summary;
        private VisualElement _membersContainer;

        /// <param name="ask">
        /// Raises a confirmation over whatever is on screen. Passed in rather than owned
        /// so this view never holds a reference to the chrome around it, which is the
        /// same arrangement <c>GuildContext.Report</c> uses for the toast.
        /// </param>
        public RosterView(Action<ConfirmRequest> ask)
            : base(ScrollViewMode.Vertical)
        {
            _ask = ask;
            AddToClassList("guild-screen");
        }

        public void Rebuild(GuildContext context)
        {
            _context = context;
            Clear();
            _members.Clear();
            _hires.Clear();

            Add(Ui.Text("The guild", "section-title", "section-title--first"));
            _summary = Ui.Box("stat-row");
            Add(_summary);

            Add(Ui.Text("Adventurers", "section-title"));
            _membersContainer = Ui.Box();
            Add(_membersContainer);
            RebuildMembers(context);

            Add(Ui.Text("Looking for work", "section-title"));
            foreach (AdventurerDefinition definition in context.World.Content.Adventurers)
            {
                if (definition == null)
                {
                    continue;
                }

                HireCard hire = new HireCard(definition, this);
                _hires.Add(hire);
                Add(hire);
            }

            Refresh(context);
        }

        public void Refresh(GuildContext context)
        {
            _context = context;

            if (_summary != null)
            {
                _summary.Clear();
                _summary.Add(Ui.Stat("Beds", $"{context.Recruitment.UsedHousing}/{context.Recruitment.TotalHousing}"));
                _summary.Add(Ui.Stat("Attracts", context.Recruitment.MaximumRecruitableRarity().ToString()));
                _summary.Add(Ui.Stat("Training", Format.Bonus(context.Stats.Get(GuildStat.AdventurerPower))));
            }

            foreach (MemberCard member in _members)
            {
                member.Refresh(context);
            }

            foreach (HireCard hire in _hires)
            {
                hire.Refresh(context);
            }
        }

        private void RebuildMembers(GuildContext context)
        {
            _membersContainer.Clear();
            _members.Clear();

            if (context.World.Roster.Count == 0)
            {
                _membersContainer.Add(Ui.Text(
                    "Nobody has joined yet. Build the Inn for beds, then hire below.",
                    "empty"));
                return;
            }

            foreach (Adventurer member in context.World.Roster.Members)
            {
                MemberCard card = new MemberCard(member.InstanceId, this);
                _members.Add(card);
                _membersContainer.Add(card);
            }
        }

        /// <summary>
        /// Train whoever holds this instance id right now. Resolved at the moment of the
        /// tap rather than captured when the card was built, because a save loaded in
        /// between replaces every Adventurer object on the roster.
        /// </summary>
        private void TrainById(string instanceId)
        {
            Adventurer member = _context?.World.Roster.Find(instanceId);
            if (member == null)
            {
                return;
            }

            TrainingOutcome outcome = _context.Training.TryLevelUp(member);
            _context.Report(Outcomes.Describe(outcome, member.Definition.DisplayName), Outcomes.Succeeded(outcome));
        }

        /// <summary>
        /// Ask before retiring, or explain why it cannot happen yet.
        ///
        /// The button stays enabled when the answer is no, which is the one place this
        /// screen departs from the disabled-button-with-a-reason-beside-it pattern used
        /// everywhere else. Printing "wait for them to come home" under every adventurer
        /// currently out on a quest would put a refusal on most of the roster most of the
        /// time; a player who wants to know taps and is told exactly, and the answer is
        /// still the service's rather than this view's.
        /// </summary>
        private void RetireById(string instanceId)
        {
            Adventurer member = _context?.World.Roster.Find(instanceId);
            if (member == null)
            {
                return;
            }

            DismissOutcome state = _context.Recruitment.PreviewDismissal(member);
            if (state != DismissOutcome.Dismissed)
            {
                _context.Report(
                    Outcomes.Describe(state, member.Definition.DisplayName, OrderNameFor(_context, instanceId)),
                    false);
                return;
            }

            AdventurerDefinition archetype = member.Definition;
            _ask?.Invoke(new ConfirmRequest(
                $"Retire {archetype.DisplayName}?",
                "They leave the guild for good and their bed at the Inn opens up. Hiring another " +
                $"{archetype.DisplayName} costs {Format.Amount(archetype.RecruitCostGold)} gold, and they would " +
                "start again at level 1.",
                "Retire",
                () => ConfirmRetire(instanceId),
                true));
        }

        /// <summary>
        /// Resolved by instance id a second time, at the moment the player confirms rather
        /// than when the dialog was raised. A quest can complete and a save can load
        /// between the two, and the roster this card was built against is not guaranteed
        /// to be the roster the confirmation lands on.
        /// </summary>
        private void ConfirmRetire(string instanceId)
        {
            Adventurer member = _context?.World.Roster.Find(instanceId);
            if (member == null)
            {
                return;
            }

            string name = member.Definition.DisplayName;
            string orderName = OrderNameFor(_context, instanceId);

            DismissOutcome outcome = _context.Recruitment.TryDismiss(member);
            _context.Report(Outcomes.Describe(outcome, name, orderName), Outcomes.Succeeded(outcome));
        }

        private static string OrderNameFor(GuildContext context, string instanceId)
        {
            QuestAssignment order = context.World.FindAssignmentFor(instanceId);
            return order?.Quest != null ? order.Quest.DisplayName : null;
        }

        private void Hire(AdventurerDefinition definition)
        {
            if (_context == null)
            {
                return;
            }

            RecruitOutcome outcome = _context.Recruitment.TryRecruit(definition, out Adventurer _);
            _context.Report(Outcomes.Describe(outcome, definition.DisplayName), Outcomes.Succeeded(outcome));
        }

        /// <summary>
        /// One roster member. Held by instance id rather than by reference so that a
        /// loaded save, which rebuilds every Adventurer object from scratch, cannot leave
        /// this card pointing at someone who no longer exists.
        /// </summary>
        private sealed class MemberCard : VisualElement
        {
            private readonly string _instanceId;
            private readonly Label _title;
            private readonly Label _activity;
            private readonly Label _detail;
            private readonly Button _train;

            internal MemberCard(string instanceId, RosterView owner)
            {
                _instanceId = instanceId;
                AddToClassList("card");

                VisualElement header = Ui.Box("card__header");
                _title = Ui.Text(string.Empty, "card__title");
                _activity = Ui.Text(string.Empty, "badge");
                header.Add(_title);
                header.Add(_activity);
                Add(header);

                _detail = Ui.Text(string.Empty, "card__meta");
                Add(_detail);

                VisualElement actions = Ui.Box("card__row");
                _train = Ui.Action(string.Empty, () => owner.TrainById(instanceId), "button--wide");
                actions.Add(_train);

                // Not held as a field: its label and its enabled state never change, and a
                // private field assigned once and never read is a warning in a project
                // that has none.
                actions.Add(Ui.Action(
                    "Retire",
                    () => owner.RetireById(instanceId),
                    "button--small",
                    "button--spaced",
                    "button--destructive"));

                Add(actions);
            }

            internal void Refresh(GuildContext context)
            {
                Adventurer member = context.World.Roster.Find(_instanceId);
                if (member == null)
                {
                    style.display = DisplayStyle.None;
                    return;
                }

                style.display = DisplayStyle.Flex;

                _title.text = member.Definition.DisplayName;
                _title.EnableInClassList(Format.RarityClass(member.Definition.Rarity), true);

                _activity.text = member.Activity switch
                {
                    AdventurerActivity.OnQuest => "On a quest",
                    AdventurerActivity.Resting => $"Resting {Format.Duration(member.RestRemainingSeconds)}",
                    _ => "Idle"
                };
                _activity.EnableInClassList("badge--active", member.Activity == AdventurerActivity.Idle);

                // The standing order is named on the line rather than left to the badge:
                // somebody resting between runs of a repeating order reads as "Idle", and
                // idle is the one word that does not explain why they cannot be sent
                // anywhere else or retired.
                string order = OrderNameFor(context, _instanceId);
                _detail.text =
                    $"Level {member.Level} / {member.Definition.MaxLevel} · " +
                    $"power {member.PowerWith(context.Stats):0.#}" +
                    (order == null ? string.Empty : $" · {order} order");

                TrainingOutcome state = context.Training.Preview(member);
                _train.text = state == TrainingOutcome.MaxLevel
                    ? "Fully trained"
                    : $"Train to Lv {member.Level + 1} · {Format.Amount(context.Training.CostOfNextLevel(member))} gold";
                _train.SetEnabled(state == TrainingOutcome.Trained);
            }
        }

        /// <summary>An archetype the guild might hire, and the gate currently in the way.</summary>
        private sealed class HireCard : VisualElement
        {
            private readonly AdventurerDefinition _definition;
            private readonly Label _title;
            private readonly Label _detail;
            private readonly Label _explanation;
            private readonly Button _hire;

            internal HireCard(AdventurerDefinition definition, RosterView owner)
            {
                _definition = definition;
                AddToClassList("card");

                VisualElement header = Ui.Box("card__header");
                _title = Ui.Text(definition.DisplayName, "card__title", Format.RarityClass(definition.Rarity));
                header.Add(_title);
                header.Add(Ui.Text(definition.Rarity.ToString(), "badge"));
                Add(header);

                _detail = Ui.Text(string.Empty, "card__meta");
                Add(_detail);

                _explanation = Ui.Text(string.Empty, "card__meta");
                Add(_explanation);

                VisualElement actions = Ui.Box("card__row");
                _hire = Ui.Action(string.Empty, () => owner.Hire(definition), "button--primary", "button--wide");
                actions.Add(_hire);
                Add(actions);
            }

            internal void Refresh(GuildContext context)
            {
                RecruitOutcome state = context.Recruitment.Preview(_definition);

                _detail.text =
                    $"Power {_definition.BasePowerAt(1):0.#} at level 1 · " +
                    $"rests {Format.Duration(_definition.BaseRecoverySeconds)}";

                _explanation.text = state == RecruitOutcome.Recruited
                    ? string.Empty
                    : Outcomes.Describe(state, _definition.DisplayName);

                EnableInClassList("card--locked", state == RecruitOutcome.TierLocked || state == RecruitOutcome.RarityLocked);

                _hire.text = $"Hire · {Format.Amount(_definition.RecruitCostGold)} gold";
                _hire.SetEnabled(state == RecruitOutcome.Recruited);
            }
        }
    }
}

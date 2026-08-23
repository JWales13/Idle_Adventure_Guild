using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.App;
using IdleGuild.Core;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// People: who works here, what they can do, and who else could be persuaded to
    /// join.
    ///
    /// Hiring and the roster share a screen because the three gates in front of
    /// recruitment — the guild's tier, the Tavern's pull, the Inn's beds — are only
    /// legible next to the roster they constrain. A separate hiring screen would show a
    /// greyed-out button with no visible reason beside it.
    /// </summary>
    public sealed class RosterView : ScrollView
    {
        private readonly List<MemberCard> _members = new List<MemberCard>();
        private readonly List<HireCard> _hires = new List<HireCard>();

        private GuildContext _context;
        private VisualElement _summary;
        private VisualElement _membersContainer;

        public RosterView()
            : base(ScrollViewMode.Vertical)
        {
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

                _detail.text =
                    $"Level {member.Level} / {member.Definition.MaxLevel} · " +
                    $"power {member.PowerWith(context.Stats):0.#}";

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

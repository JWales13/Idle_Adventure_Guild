using IdleGuild.App;
using IdleGuild.Core;
using IdleGuild.Guild;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// What the guild is worth and what it still owes the next tier.
    ///
    /// **It used to be the home screen and is not any more.** Section 7 of
    /// Docs/World_View_Design.md demotes the interface to chrome around the hall, and
    /// section 10 obsoletes this view's building grid outright: rooms are the game, and a
    /// room is now a rectangle on the floor that opens its own panel when tapped. So the
    /// grid is gone, <c>BuildingCard</c> has no caller left, and what remains is the one
    /// thing that has nowhere diegetic to live yet -- the tier gate and the button that
    /// passes through it.
    ///
    /// It keeps its name for now, because <c>GuildScreen.Hall</c>, the tab bar and the USS
    /// all speak it, and the rename belongs to the day the tab bar goes rather than to a
    /// step that only empties this out.
    ///
    /// **Why the tier card survived the cull.** Deleting the whole view would have been
    /// tidier and would have left advancing a tier reachable from nowhere -- a sequence of
    /// choices that leaves the player unable to make progress, which §01 forbids in almost
    /// those words. Unbuilt rooms showing dark on the floor covers the *diagnosis* half of
    /// finding #7 diegetically; it does not cover the action. That waits for the room
    /// panels to be re-homed properly.
    ///
    /// It no longer fills the screen either -- see <c>guild-screen--panel</c>. Everything
    /// below it is hall.
    /// </summary>
    public sealed class HallView : ScrollView
    {
        private GuildContext _context;
        private Label _tierTitle;
        private Label _tierSummary;
        private VisualElement _detail;
        private bool _expanded;
        private VisualElement _requirements;
        private VisualElement _stats;
        private Button _advance;

        public HallView()
            : base(ScrollViewMode.Vertical)
        {
            AddToClassList("guild-screen");
            AddToClassList("guild-screen--panel");
        }

        /// <summary>
        /// Build the parts whose existence depends on the world. Called when something
        /// structural changes, not every frame.
        /// </summary>
        public void Rebuild(GuildContext context)
        {
            _context = context;
            Clear();

            Add(Ui.Text("Guild", "section-title", "section-title--first"));
            Add(BuildTierCard());

            Refresh(context);
        }

        /// <summary>Update the values on what is already there. Cheap enough to run every tick.</summary>
        public void Refresh(GuildContext context)
        {
            if (_tierTitle == null)
            {
                return;
            }

            _context = context;

            GuildTierDefinition tier = context.World.GuildState.CurrentTier;
            GuildTierDefinition next = context.Tiers.NextTier();

            _tierTitle.text = next != null
                ? $"{tier.DisplayName} — next: {next.DisplayName}"
                : $"{tier.DisplayName} — the top of the arc";

            RefreshRequirements(context, tier);
            RefreshStats(context);
            _tierSummary.text = SummaryFor(context, tier, next);

            _advance.text = next != null ? $"Advance to {next.DisplayName}" : "Fully grown";
            _advance.SetEnabled(context.Tiers.Preview() == TierAdvanceOutcome.Advanced);
        }

        /// <summary>
        /// A one-line summary that opens into the full gate when tapped, and starts closed.
        ///
        /// The card is reference material -- what the guild is worth and what the next tier
        /// still wants -- and reference material read once a session should not hold the
        /// middle of the screen while the hall is behind it. Closed, it is a row; open, it
        /// is exactly what it was before.
        ///
        /// Advancing is two presses rather than one, which is the cost and is worth naming.
        /// It is paid a handful of times in a whole run, against a hall that is on screen
        /// for all of it.
        /// </summary>
        private VisualElement BuildTierCard()
        {
            VisualElement card = Ui.Box("card");

            _tierTitle = Ui.Text(string.Empty, "card__title");
            _tierSummary = Ui.Text(string.Empty, "card__meta");

            VisualElement summary = Ui.Box("card__summary");
            summary.Add(_tierTitle);
            summary.Add(_tierSummary);
            summary.RegisterCallback<ClickEvent>(_ => ToggleDetail());

            _requirements = Ui.Box("stat-row");
            _stats = Ui.Box("stat-row");
            _advance = Ui.Action(string.Empty, OnAdvance, "button--primary", "button--wide");

            VisualElement actions = Ui.Box("card__row");
            actions.Add(_advance);

            _detail = Ui.Box("card__detail");
            _detail.Add(_requirements);
            _detail.Add(_stats);
            _detail.Add(actions);

            card.Add(summary);
            card.Add(_detail);

            ApplyExpansion();
            return card;
        }

        private void ToggleDetail()
        {
            _expanded = !_expanded;
            ApplyExpansion();
        }

        private void ApplyExpansion()
        {
            _detail.EnableInClassList("card__detail--collapsed", !_expanded);
        }

        /// <summary>
        /// The tier gate, spelled out clause by clause rather than as a single pass or
        /// fail. That is the whole point of the multi-building rule: a player who can see
        /// two of three buildings are ready knows exactly what to spend on next, and the
        /// gate stops being an arbitrary wall.
        /// </summary>
        private void RefreshRequirements(GuildContext context, GuildTierDefinition tier)
        {
            _requirements.Clear();

            if (tier.IsFinalTier)
            {
                _requirements.Add(Ui.Text("Nothing further to reach.", "card__meta"));
                return;
            }

            foreach (BuildingLevelRequirement requirement in tier.RequirementsToAdvance)
            {
                if (requirement.Building == null)
                {
                    continue;
                }

                int level = context.World.GuildState.GetLevel(requirement.Building.Id);
                _requirements.Add(Requirement(
                    requirement.Building.DisplayName,
                    $"{level}/{requirement.MinimumLevel}",
                    level >= requirement.MinimumLevel));
            }

            double reputation = context.World.Economy.Get(CurrencyType.Reputation);
            _requirements.Add(Requirement(
                "Reputation",
                $"{Format.Amount(reputation)}/{Format.Amount(tier.ReputationToAdvance)}",
                reputation >= tier.ReputationToAdvance));
        }

        /// <summary>
        /// What the closed row has to say on its own. It reports the shortfall as a count
        /// rather than saying nothing, because a collapsed gate that reads "Village" tells
        /// the player less than the screen it replaced -- and finding #7 is already owed a
        /// gate that names what it is missing.
        /// </summary>
        private static string SummaryFor(
            GuildContext context, GuildTierDefinition tier, GuildTierDefinition next)
        {
            if (next == null)
            {
                return "Nothing further to reach";
            }

            int met = 0;
            int total = 1;

            foreach (BuildingLevelRequirement requirement in tier.RequirementsToAdvance)
            {
                if (requirement.Building == null)
                {
                    continue;
                }

                total++;

                if (context.World.GuildState.GetLevel(requirement.Building.Id) >= requirement.MinimumLevel)
                {
                    met++;
                }
            }

            if (context.World.Economy.Get(CurrencyType.Reputation) >= tier.ReputationToAdvance)
            {
                met++;
            }

            return met >= total
                ? $"Ready for {next.DisplayName}"
                : $"{met} of {total} met for {next.DisplayName}";
        }

        private static VisualElement Requirement(string label, string value, bool met)
        {
            VisualElement stat = Ui.Stat(label, value, out Label valueLabel);
            valueLabel.EnableInClassList("stat__value--met", met);
            valueLabel.EnableInClassList("stat__value--unmet", !met);
            return stat;
        }

        private void RefreshStats(GuildContext context)
        {
            _stats.Clear();
            _stats.Add(Ui.Stat("Reward", Format.Multiplier(context.Stats.Get(GuildStat.RewardYield))));
            _stats.Add(Ui.Stat("Power", Format.Bonus(context.Stats.Get(GuildStat.AdventurerPower))));
            _stats.Add(Ui.Stat("Recovery", Format.Multiplier(context.Stats.Get(GuildStat.RecoverySpeed))));
        }

        private void OnAdvance()
        {
            if (_context == null)
            {
                return;
            }

            GuildTierDefinition next = _context.Tiers.NextTier();
            string name = next != null ? next.DisplayName : "the next tier";

            TierAdvanceOutcome outcome = _context.Tiers.TryAdvance();
            _context.Report(Outcomes.Describe(outcome, name), Outcomes.Succeeded(outcome));
        }
    }
}

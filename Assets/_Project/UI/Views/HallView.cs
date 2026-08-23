using System;
using System.Collections.Generic;
using IdleGuild.App;
using IdleGuild.Core;
using IdleGuild.Guild;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// Home: what the guild is worth, what it is capable of, and the three buildings
    /// that change both.
    ///
    /// The building list is built from <c>GameContent.Buildings</c> and nothing else, so
    /// the post-launch Quest Board and Armory appear here by being added to that
    /// catalogue — no case, no layout branch, no new card type. That is the same bet the
    /// simulation makes, checked from the other end.
    /// </summary>
    public sealed class HallView : ScrollView
    {
        private readonly Action<BuildingDefinition> _onSelectBuilding;
        private readonly List<BuildingCard> _cards = new List<BuildingCard>();

        private GuildContext _context;
        private Label _tierTitle;
        private VisualElement _requirements;
        private VisualElement _stats;
        private Button _advance;

        public HallView(Action<BuildingDefinition> onSelectBuilding)
            : base(ScrollViewMode.Vertical)
        {
            _onSelectBuilding = onSelectBuilding;
            AddToClassList("guild-screen");
        }

        /// <summary>
        /// Build the parts whose existence depends on the world: the tier card and one
        /// card per building. Called when something structural changes, not every frame.
        /// </summary>
        public void Rebuild(GuildContext context)
        {
            _context = context;
            Clear();
            _cards.Clear();

            Add(Ui.Text("Guild", "section-title", "section-title--first"));
            Add(BuildTierCard());

            Add(Ui.Text("Buildings", "section-title"));
            foreach (BuildingDefinition building in context.World.Content.Buildings)
            {
                if (building == null)
                {
                    continue;
                }

                BuildingCard card = new BuildingCard(building, _onSelectBuilding);
                _cards.Add(card);
                Add(card);
            }

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

            _advance.text = next != null ? $"Advance to {next.DisplayName}" : "Fully grown";
            _advance.SetEnabled(context.Tiers.Preview() == TierAdvanceOutcome.Advanced);

            foreach (BuildingCard card in _cards)
            {
                card.Refresh(context);
            }
        }

        private VisualElement BuildTierCard()
        {
            VisualElement card = Ui.Box("card");
            _tierTitle = Ui.Text(string.Empty, "card__title");
            _requirements = Ui.Box("stat-row");
            _stats = Ui.Box("stat-row");
            _advance = Ui.Action(string.Empty, OnAdvance, "button--primary", "button--wide");

            VisualElement actions = Ui.Box("card__row");
            actions.Add(_advance);

            card.Add(_tierTitle);
            card.Add(_requirements);
            card.Add(_stats);
            card.Add(actions);
            return card;
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

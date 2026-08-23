using IdleGuild.App;
using IdleGuild.Guild;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// The upgrade panel, raised over the Guild Hall by tapping a building.
    ///
    /// It answers the one question a card cannot fit: what does the next level actually
    /// do. Effects are read straight off the <c>BuildingDefinition</c> and evaluated at
    /// the current and next level, so a building that gains a new effect asset-side
    /// starts explaining itself here without a line of code changing — the data-driven
    /// architecture doing the same work for the interface that it does for the
    /// simulation.
    ///
    /// This is also the overlay pattern Week 3's rewarded-ad and IAP prompts will reuse,
    /// which is why the scrim and the panel are separated: the scrim is the reusable
    /// part, the panel is what changes.
    /// </summary>
    public sealed class BuildingDetailOverlay : VisualElement
    {
        private readonly Label _title;
        private readonly Label _description;
        private readonly VisualElement _effects;
        private readonly Label _explanation;
        private readonly Button _upgrade;

        private GuildContext _context;
        private BuildingDefinition _building;

        public BuildingDetailOverlay()
        {
            AddToClassList("overlay");
            AddToClassList("overlay--hidden");

            VisualElement panel = Ui.Box("overlay__panel");
            _title = Ui.Text(string.Empty, "overlay__title");
            _description = Ui.Text(string.Empty, "card__subtitle");

            ScrollView body = Ui.Scroll("overlay__body");
            _effects = Ui.Box();
            body.Add(_effects);

            _explanation = Ui.Text(string.Empty, "card__meta");

            VisualElement actions = Ui.Box("overlay__actions");
            _upgrade = Ui.Action(string.Empty, OnUpgrade, "button--primary", "button--wide");
            Button close = Ui.Action("Close", Close, "button--spaced");
            actions.Add(_upgrade);
            actions.Add(close);

            panel.Add(_title);
            panel.Add(_description);
            panel.Add(body);
            panel.Add(_explanation);
            panel.Add(actions);
            Add(panel);

            // Tapping the scrim dismisses, tapping the panel does not. Without the second
            // handler every tap inside the panel would bubble out and close it.
            RegisterCallback<ClickEvent>(_ => Close());
            panel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        }

        public bool IsOpen => !ClassListContains("overlay--hidden");

        /// <summary>The building currently on show, or null when closed.</summary>
        public BuildingDefinition Building => IsOpen ? _building : null;

        public void Open(GuildContext context, BuildingDefinition building)
        {
            if (building == null)
            {
                return;
            }

            _context = context;
            _building = building;
            RemoveFromClassList("overlay--hidden");
            Refresh(context);
        }

        public void Close()
        {
            AddToClassList("overlay--hidden");
            _building = null;
        }

        public void Refresh(GuildContext context)
        {
            if (!IsOpen || _building == null)
            {
                return;
            }

            _context = context;

            int level = context.World.GuildState.GetLevel(_building.Id);
            int nextLevel = context.Buildings.NextLevel(_building);
            UpgradeOutcome state = context.Buildings.Preview(_building);

            _title.text = _building.DisplayName;
            _description.text = string.IsNullOrWhiteSpace(_building.Description)
                ? $"Level {level} of {_building.MaxLevel}."
                : _building.Description;

            RefreshEffects(level, nextLevel);

            _upgrade.text = state == UpgradeOutcome.MaxLevel
                ? "Fully upgraded"
                : $"{(level == 0 ? "Build" : $"Upgrade to Lv {nextLevel}")} · " +
                  $"{Format.Amount(context.Buildings.CostOfNextLevel(_building))} gold";
            _upgrade.SetEnabled(state == UpgradeOutcome.Upgraded);

            _explanation.text = state == UpgradeOutcome.Upgraded
                ? string.Empty
                : Outcomes.Describe(state, _building.DisplayName);
        }

        /// <summary>
        /// Each effect at the level it is at now and the level it would reach. A building
        /// that has not been built yet has no "now", which reads as an em dash rather
        /// than as a zero — zero would imply the effect exists and does nothing.
        /// </summary>
        private void RefreshEffects(int level, int nextLevel)
        {
            _effects.Clear();

            BuildingEffect[] effects = _building.Effects;
            if (effects.Length == 0)
            {
                _effects.Add(Ui.Text("This building has no effects yet.", "card__meta"));
                return;
            }

            bool hasNextLevel = _building.CanReach(nextLevel);

            foreach (BuildingEffect effect in effects)
            {
                string now = level >= 1
                    ? Format.EffectValue(effect.Kind, effect.ValuePerLevel.Evaluate(level))
                    : "—";
                string next = hasNextLevel
                    ? Format.EffectValue(effect.Kind, effect.ValuePerLevel.Evaluate(nextLevel))
                    : "—";

                VisualElement row = Ui.Box("card__row");
                row.Add(Ui.Text(Format.StatName(effect.Stat), "stat__label"));
                row.Add(Ui.Text($"{now}  →  {next}", "stat__value"));
                _effects.Add(row);
            }
        }

        private void OnUpgrade()
        {
            if (_context == null || _building == null)
            {
                return;
            }

            UpgradeOutcome outcome = _context.Buildings.TryUpgrade(_building);
            _context.Report(Outcomes.Describe(outcome, _building.DisplayName), Outcomes.Succeeded(outcome));
            Refresh(_context);
        }
    }
}

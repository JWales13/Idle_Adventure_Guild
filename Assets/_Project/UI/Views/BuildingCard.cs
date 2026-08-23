using System;
using IdleGuild.App;
using IdleGuild.Guild;
using UnityEngine.UIElements;

namespace IdleGuild.UI.Views
{
    /// <summary>
    /// One building on the Guild Hall: what it is, what level it has reached, and what
    /// the next level costs.
    ///
    /// The card itself is the tap target — the upgrade happens in the overlay it opens,
    /// not here. That costs one extra tap on every purchase, which is a real price, and
    /// it buys the room to show what the next level actually does before the player pays
    /// for it. An idle game that hides the effect behind a number is asking players to
    /// spend on faith.
    /// </summary>
    public sealed class BuildingCard : VisualElement
    {
        private readonly BuildingDefinition _building;
        private readonly Label _title;
        private readonly Label _level;
        private readonly Label _cost;

        public BuildingCard(BuildingDefinition building, Action<BuildingDefinition> onSelect)
        {
            _building = building;

            AddToClassList("card");
            AddToClassList("card--interactive");

            VisualElement header = Ui.Box("card__header");
            _title = Ui.Text(building.DisplayName, "card__title");
            _level = Ui.Text(string.Empty, "badge");
            header.Add(_title);
            header.Add(_level);

            _cost = Ui.Text(string.Empty, "card__meta");

            Add(header);
            Add(_cost);

            RegisterCallback<ClickEvent>(_ => onSelect?.Invoke(building));
        }

        public void Refresh(GuildContext context)
        {
            int level = context.World.GuildState.GetLevel(_building.Id);
            UpgradeOutcome state = context.Buildings.Preview(_building);

            _title.text = _building.DisplayName;
            _level.text = level == 0 ? "Not built" : $"Lv {level} / {_building.MaxLevel}";

            EnableInClassList("card--locked", state == UpgradeOutcome.TierLocked);

            _cost.text = state switch
            {
                UpgradeOutcome.TierLocked => "Unlocks at a later guild tier.",
                UpgradeOutcome.MaxLevel => "At its highest level.",
                _ => $"{(level == 0 ? "Build" : "Upgrade")} for " +
                     $"{Format.Amount(context.Buildings.CostOfNextLevel(_building))} gold"
            };
        }
    }
}

using IdleGuild.Core;
using IdleGuild.Guild;

namespace IdleGuild.App
{
    /// <summary>Why an upgrade did or did not happen.</summary>
    public enum UpgradeOutcome
    {
        Upgraded,

        /// <summary>No such building in the catalogue.</summary>
        UnknownBuilding,

        /// <summary>The guild has not reached the tier this building appears at.</summary>
        TierLocked,

        /// <summary>Already at the top of its track.</summary>
        MaxLevel,

        /// <summary>Not enough gold.</summary>
        Unaffordable
    }

    /// <summary>
    /// Buying a building level: the one place gold turns into guild stats.
    ///
    /// Building the thing and upgrading it are the same transaction, because level 0
    /// means "not built" and level 1 is the constructed state — so the first purchase
    /// is construction and every later one is an upgrade, with no separate code path
    /// and no separate UI rule.
    /// </summary>
    public sealed class BuildingUpgradeService
    {
        private readonly GameWorld _world;

        public BuildingUpgradeService(GameWorld world)
        {
            _world = world ?? throw new System.ArgumentNullException(nameof(world));
        }

        /// <summary>The level a purchase would take this building to.</summary>
        public int NextLevel(BuildingDefinition building)
        {
            return building == null ? 0 : _world.GuildState.GetLevel(building.Id) + 1;
        }

        /// <summary>
        /// Gold for the next level, or zero when there is no next level. Check
        /// <see cref="Preview"/> before showing this — zero means "not purchasable",
        /// never "free".
        /// </summary>
        public double CostOfNextLevel(BuildingDefinition building)
        {
            return building == null ? 0d : building.CostToReach(NextLevel(building));
        }

        /// <summary>
        /// What <see cref="TryUpgrade"/> would return, without changing anything. UI uses
        /// this to decide whether a button is enabled and what to say when it is not.
        /// </summary>
        public UpgradeOutcome Preview(BuildingDefinition building)
        {
            if (building == null || _world.Content.FindBuilding(building.Id) == null)
            {
                return UpgradeOutcome.UnknownBuilding;
            }

            if (!_world.GuildState.IsAvailable(building))
            {
                return UpgradeOutcome.TierLocked;
            }

            int nextLevel = NextLevel(building);
            if (!building.CanReach(nextLevel))
            {
                return UpgradeOutcome.MaxLevel;
            }

            if (!_world.Economy.CanAfford(CurrencyType.Gold, building.CostToReach(nextLevel)))
            {
                return UpgradeOutcome.Unaffordable;
            }

            return UpgradeOutcome.Upgraded;
        }

        /// <summary>
        /// Buy the next level. Nothing changes unless the whole transaction succeeds:
        /// gold is only spent once every gate has passed, and the level is only raised
        /// once the gold is gone.
        /// </summary>
        public UpgradeOutcome TryUpgrade(BuildingDefinition building)
        {
            UpgradeOutcome preview = Preview(building);
            if (preview != UpgradeOutcome.Upgraded)
            {
                return preview;
            }

            int nextLevel = NextLevel(building);
            if (!_world.Economy.TrySpend(CurrencyType.Gold, building.CostToReach(nextLevel)))
            {
                return UpgradeOutcome.Unaffordable;
            }

            // GuildState publishes BuildingUpgraded and recalculates stats itself.
            _world.GuildState.SetLevel(building.Id, nextLevel);
            return UpgradeOutcome.Upgraded;
        }
    }
}

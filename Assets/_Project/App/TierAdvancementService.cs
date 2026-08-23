using System;
using IdleGuild.Core;
using IdleGuild.Guild;

namespace IdleGuild.App
{
    /// <summary>Why a tier advance did or did not happen.</summary>
    public enum TierAdvanceOutcome
    {
        Advanced,

        /// <summary>Nothing above the current tier — the end of the launch arc.</summary>
        FinalTier,

        /// <summary>Building levels or reputation still short.</summary>
        RequirementsNotMet
    }

    /// <summary>
    /// Moving the guild up the Village to Capital arc.
    ///
    /// Reputation is a threshold, not a price: reaching the next tier requires holding
    /// enough, and holding onto it afterwards. Spending it would punish the player for
    /// advancing and make the next gate harder than the last for the wrong reason.
    /// </summary>
    public sealed class TierAdvancementService
    {
        private readonly GameWorld _world;

        public TierAdvancementService(GameWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>The tier above the current one, or null at the end of the arc.</summary>
        public GuildTierDefinition NextTier()
        {
            return _world.Content.TierAfter(_world.GuildState.CurrentTier.Order);
        }

        /// <summary>What <see cref="TryAdvance"/> would return, without changing anything.</summary>
        public TierAdvanceOutcome Preview()
        {
            if (NextTier() == null)
            {
                return TierAdvanceOutcome.FinalTier;
            }

            double reputation = _world.Economy.Get(CurrencyType.Reputation);
            return _world.GuildState.CanAdvance(reputation)
                ? TierAdvanceOutcome.Advanced
                : TierAdvanceOutcome.RequirementsNotMet;
        }

        /// <summary>
        /// Move up a tier. GuildState publishes the change and recalculates stats, which
        /// is what raises quest slots and unlocks the next band of content — all of it
        /// through data the new tier asset already carries.
        /// </summary>
        public TierAdvanceOutcome TryAdvance()
        {
            TierAdvanceOutcome preview = Preview();
            if (preview != TierAdvanceOutcome.Advanced)
            {
                return preview;
            }

            _world.GuildState.AdvanceTo(NextTier());
            return TierAdvanceOutcome.Advanced;
        }
    }
}

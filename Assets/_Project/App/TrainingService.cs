using System;
using IdleGuild.Adventurers;
using IdleGuild.Core;

namespace IdleGuild.App
{
    /// <summary>Why a training session did or did not happen.</summary>
    public enum TrainingOutcome
    {
        Trained,

        /// <summary>Nobody on the roster with that instance id.</summary>
        UnknownAdventurer,

        /// <summary>Already at the top of their track.</summary>
        MaxLevel,

        /// <summary>Not enough gold.</summary>
        Unaffordable
    }

    /// <summary>
    /// Levelling an individual adventurer, paid for in gold.
    ///
    /// Distinct from the Training Room, which raises everyone's Power at once. This is
    /// the per-person track; the building is the guild-wide one. Both feed the same
    /// Power number that quest duration and success read, so the player can invest in
    /// either and see the same curve move.
    ///
    /// Training is allowed while a member is out on a quest or resting. The run's
    /// numbers were snapshotted at dispatch, so a mid-quest level-up pays off from the
    /// next run rather than retroactively — consistent with how building upgrades behave.
    /// </summary>
    public sealed class TrainingService
    {
        private readonly GameWorld _world;

        public TrainingService(GameWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>
        /// Gold for the next level, or zero when there is no next level. Check
        /// <see cref="Preview"/> first — zero means "not trainable", never "free".
        /// </summary>
        public double CostOfNextLevel(Adventurer adventurer)
        {
            return adventurer == null ? 0d : adventurer.Definition.TrainingCostToReach(adventurer.Level + 1);
        }

        /// <summary>What <see cref="TryLevelUp"/> would return, without changing anything.</summary>
        public TrainingOutcome Preview(Adventurer adventurer)
        {
            if (adventurer == null || _world.Roster.Find(adventurer.InstanceId) == null)
            {
                return TrainingOutcome.UnknownAdventurer;
            }

            int nextLevel = adventurer.Level + 1;
            if (!adventurer.Definition.HasLevel(nextLevel))
            {
                return TrainingOutcome.MaxLevel;
            }

            if (!_world.Economy.CanAfford(CurrencyType.Gold, adventurer.Definition.TrainingCostToReach(nextLevel)))
            {
                return TrainingOutcome.Unaffordable;
            }

            return TrainingOutcome.Trained;
        }

        /// <summary>Buy one level for this adventurer.</summary>
        public TrainingOutcome TryLevelUp(Adventurer adventurer)
        {
            TrainingOutcome preview = Preview(adventurer);
            if (preview != TrainingOutcome.Trained)
            {
                return preview;
            }

            int nextLevel = adventurer.Level + 1;
            if (!_world.Economy.TrySpend(CurrencyType.Gold, adventurer.Definition.TrainingCostToReach(nextLevel)))
            {
                return TrainingOutcome.Unaffordable;
            }

            adventurer.SetLevel(nextLevel);
            return TrainingOutcome.Trained;
        }
    }
}

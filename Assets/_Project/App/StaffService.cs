using System;
using IdleGuild.Core;
using IdleGuild.Core.Events;
using IdleGuild.Staff;

namespace IdleGuild.App
{
    /// <summary>Why an employee was or was not taken on.</summary>
    public enum HireOutcome
    {
        Hired,

        /// <summary>No such employee in the catalogue.</summary>
        UnknownStaff,

        /// <summary>The guild has not reached the tier this kind of employee appears at.</summary>
        TierLocked,

        /// <summary>Every staff slot is filled. Upgrade the Tavern, or let somebody go.</summary>
        NoFreeSlot,

        /// <summary>Not enough gold.</summary>
        Unaffordable
    }

    /// <summary>Why an employee did or did not leave the payroll.</summary>
    public enum LetGoOutcome
    {
        LetGo,

        /// <summary>Nobody with that instance id is employed.</summary>
        UnknownStaff
    }

    /// <summary>
    /// Taking staff on, and letting them go.
    ///
    /// <b>Both halves ship on the same day, and that is the point of the class.</b>
    /// §6C's third finding is that staff slots are a one-way ratchet: fill them cheaply
    /// and you can never upgrade. That is the Days 10-11 bed problem exactly — the Inn
    /// capped at sixteen beds, a Capital guild needing twelve, and nothing in the game
    /// able to dismiss anybody, so a bed spent during City was spent for the rest of the
    /// run. Day 12 had to retrofit the fix for adventurers, and the retrofit was where
    /// the real finding came from: adding the action moved everything-maxed by over two
    /// hours <i>on unchanged assets</i>, because the economy had been priced against a
    /// wall that the fix removed. The instruction carried into this day was: do not
    /// repeat that.
    ///
    /// So there is no gate on letting somebody go. An employee has no run in flight and
    /// no standing order to belong to — the two things that make
    /// <c>RecruitmentService.TryDismiss</c> refuse for adventurers — so there is nothing
    /// they can be in the middle of. Wages simply stop.
    ///
    /// <b>Nothing is refunded</b>, the same rule adventurers follow. A rebate would make
    /// hire-and-fire a free churn loop; what the payroll was missing was reversibility,
    /// not a discount. The player who filled every slot with potboys needs a way back,
    /// not a way to have guessed for nothing.
    ///
    /// One thing this class cannot fix and the Day 16 document records instead: the
    /// model has no equivalent of <see cref="TryLetGo"/>, so it can only ever append
    /// staff. That is why its winning configuration hires a hundred and five Potboys and
    /// never buys a single employee from the three tiers above — the ladder is
    /// unreachable at any price, and was therefore free to be priced badly. Nothing has
    /// ever measured what the upper tiers are worth. See §6.
    /// </summary>
    public sealed class StaffService
    {
        private readonly GameWorld _world;

        public StaffService(GameWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>Slots filled.</summary>
        public int Employed => _world.Staff.Count;

        /// <summary>Slots available, from the Tavern.</summary>
        public int Slots => _world.Staff.SlotsWith(_world.Stats);

        /// <summary>What <see cref="TryHire"/> would return, without changing anything.</summary>
        public HireOutcome Preview(StaffDefinition definition)
        {
            if (definition == null || _world.Content.FindStaff(definition.Id) == null)
            {
                return HireOutcome.UnknownStaff;
            }

            if (definition.MinimumTierOrder > _world.GuildState.CurrentTier.Order)
            {
                return HireOutcome.TierLocked;
            }

            if (!_world.Staff.HasSlotWith(_world.Stats))
            {
                return HireOutcome.NoFreeSlot;
            }

            if (!_world.Economy.CanAfford(CurrencyType.Gold, definition.HireCostGold))
            {
                return HireOutcome.Unaffordable;
            }

            return HireOutcome.Hired;
        }

        /// <summary>
        /// Take one employee on. They get a fresh instance id, because the archetype is
        /// shared and the individual is not — two Potboys are two people, and a save
        /// references the person.
        /// </summary>
        public HireOutcome TryHire(StaffDefinition definition, out StaffMember hired)
        {
            hired = null;

            HireOutcome preview = Preview(definition);
            if (preview != HireOutcome.Hired)
            {
                return preview;
            }

            if (!_world.Economy.TrySpend(CurrencyType.Gold, definition.HireCostGold))
            {
                return HireOutcome.Unaffordable;
            }

            string instanceId = Guid.NewGuid().ToString("N");
            StaffMember employee = new StaffMember(instanceId, definition);
            _world.Staff.Add(employee);
            hired = employee;

            EventBus.Publish(new StaffHired(definition.Id, instanceId, Employed, Slots));
            return HireOutcome.Hired;
        }

        /// <summary>What <see cref="TryLetGo"/> would return, without changing anything.</summary>
        public LetGoOutcome PreviewLetGo(StaffMember employee)
        {
            if (employee == null || _world.Staff.Find(employee.InstanceId) == null)
            {
                return LetGoOutcome.UnknownStaff;
            }

            return LetGoOutcome.LetGo;
        }

        /// <summary>
        /// Let one employee go, freeing their slot and stopping their share of the wage
        /// bill from the next moment the clock turns.
        /// </summary>
        public LetGoOutcome TryLetGo(StaffMember employee)
        {
            LetGoOutcome preview = PreviewLetGo(employee);
            if (preview != LetGoOutcome.LetGo)
            {
                return preview;
            }

            if (!_world.Staff.Remove(employee.InstanceId))
            {
                return LetGoOutcome.UnknownStaff;
            }

            EventBus.Publish(new StaffDismissed(employee.Definition.Id, employee.InstanceId, Employed, Slots));
            return LetGoOutcome.LetGo;
        }

        /// <summary>
        /// Let the least capable employee go. What a "make room for someone better"
        /// button calls, and the reason the ratchet is reversible in one press rather
        /// than in a scroll through ninety-nine identical rows.
        /// </summary>
        public LetGoOutcome TryLetGoLeastCapable(out StaffMember released)
        {
            released = _world.Staff.LeastCapable();
            return released == null ? LetGoOutcome.UnknownStaff : TryLetGo(released);
        }
    }
}

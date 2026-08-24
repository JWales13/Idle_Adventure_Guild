using IdleGuild.App;

namespace IdleGuild.UI
{
    /// <summary>
    /// The service outcome enums as sentences a player can act on.
    ///
    /// This is the payoff for the services returning an outcome enum rather than a bool.
    /// Every gate in the game — tier, Tavern rarity, Inn capacity, gold, quest slots,
    /// party availability — names itself on the way out, so a disabled button always has
    /// something to say when it is tapped. A UI that can only report "no" teaches the
    /// player nothing about what to do next, and in an idle game "what do I do next" is
    /// the entire experience.
    ///
    /// Wording rule: say what is missing, not what failed. "You need a free bed at the
    /// Inn" rather than "Housing full".
    /// </summary>
    public static class Outcomes
    {
        public static string Describe(UpgradeOutcome outcome, string buildingName)
        {
            return outcome switch
            {
                UpgradeOutcome.Upgraded => $"{buildingName} upgraded.",
                UpgradeOutcome.Unaffordable => "Not enough gold for that yet.",
                UpgradeOutcome.MaxLevel => $"{buildingName} is already at its highest level.",
                UpgradeOutcome.TierLocked => $"{buildingName} unlocks at a later guild tier.",
                UpgradeOutcome.UnknownBuilding => "That building is not part of the guild.",
                _ => string.Empty
            };
        }

        public static string Describe(RecruitOutcome outcome, string adventurerName)
        {
            return outcome switch
            {
                RecruitOutcome.Recruited => $"{adventurerName} joined the guild.",
                RecruitOutcome.Unaffordable => "Not enough gold to hire them.",
                RecruitOutcome.HousingFull => "No free bed — upgrade the Inn first.",
                RecruitOutcome.RarityLocked => "The Tavern is not yet good enough to attract them.",
                RecruitOutcome.TierLocked => "They only take work from a larger guild.",
                RecruitOutcome.UnknownAdventurer => "Nobody by that name is looking for work.",
                _ => string.Empty
            };
        }

        public static string Describe(TrainingOutcome outcome, string adventurerName)
        {
            return outcome switch
            {
                TrainingOutcome.Trained => $"{adventurerName} trained up a level.",
                TrainingOutcome.Unaffordable => "Not enough gold for that training.",
                TrainingOutcome.MaxLevel => $"{adventurerName} has nothing left to learn.",
                TrainingOutcome.UnknownAdventurer => "They are not on the roster.",
                _ => string.Empty
            };
        }

        public static string Describe(DispatchOutcome outcome, string questName)
        {
            return outcome switch
            {
                DispatchOutcome.Dispatched => $"A party set out on {questName}.",
                DispatchOutcome.PartyTooSmall => "Not enough free adventurers for that quest.",
                DispatchOutcome.MemberUnavailable => "Everyone free is already on another order.",
                DispatchOutcome.NoFreeSlot => "Every quest slot is busy — advance a tier for more.",
                DispatchOutcome.QuestLocked => "That quest is beyond the guild's reach for now.",
                DispatchOutcome.UnknownQuest => "No such quest is posted.",
                DispatchOutcome.PartyTooLarge => "That is more adventurers than the quest has room for.",
                DispatchOutcome.DuplicateMember => "Somebody is in that party twice.",
                DispatchOutcome.UnknownOrder => "That standing order is no longer running.",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Retiring, and the two commitments that stand in the way of it.
        ///
        /// <paramref name="standingOrderName"/> is optional and is worth passing: "committed
        /// to the Sunken Crypt order" tells the player exactly which card to go and edit,
        /// where "committed to a standing order" leaves them hunting. The screen reads that
        /// name off the world rather than working out for itself who is committed to what.
        /// </summary>
        public static string Describe(DismissOutcome outcome, string adventurerName, string standingOrderName = null)
        {
            return outcome switch
            {
                DismissOutcome.Dismissed => $"{adventurerName} has retired from the guild.",
                DismissOutcome.OnQuest => $"{adventurerName} is out in the field — wait for them to come home.",
                DismissOutcome.OnStandingOrder => string.IsNullOrEmpty(standingOrderName)
                    ? $"{adventurerName} is committed to a standing order. Re-form that party first."
                    : $"{adventurerName} is committed to the {standingOrderName} order. Re-form that party first.",
                DismissOutcome.UnknownAdventurer => "They are not on the roster.",
                _ => string.Empty
            };
        }

        public static string Describe(TierAdvanceOutcome outcome, string tierName)
        {
            return outcome switch
            {
                TierAdvanceOutcome.Advanced => $"The guild is now a {tierName}.",
                TierAdvanceOutcome.RequirementsNotMet => "Building levels or reputation are still short.",
                TierAdvanceOutcome.FinalTier => "The guild has reached the top of the arc.",
                _ => string.Empty
            };
        }

        /// <summary>
        /// True when an outcome represents the action actually happening. Used to pick a
        /// toast colour, so the four success members do not have to be listed at every
        /// call site.
        /// </summary>
        public static bool Succeeded(UpgradeOutcome outcome) => outcome == UpgradeOutcome.Upgraded;

        public static bool Succeeded(RecruitOutcome outcome) => outcome == RecruitOutcome.Recruited;

        public static bool Succeeded(TrainingOutcome outcome) => outcome == TrainingOutcome.Trained;

        public static bool Succeeded(DispatchOutcome outcome) => outcome == DispatchOutcome.Dispatched;

        public static bool Succeeded(DismissOutcome outcome) => outcome == DismissOutcome.Dismissed;

        public static bool Succeeded(TierAdvanceOutcome outcome) => outcome == TierAdvanceOutcome.Advanced;
    }
}

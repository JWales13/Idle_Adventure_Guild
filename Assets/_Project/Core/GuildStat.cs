namespace IdleGuild.Core
{
    /// <summary>
    /// Every quantity a building can influence. Buildings own non-overlapping stats
    /// by design, so upgrading each one matters instead of a single building
    /// dominating the curve.
    ///
    /// The post-MVP entries are declared now on purpose: Quest Board and Armory ship
    /// later as new BuildingDefinition assets targeting stats that already exist,
    /// which is what keeps that expansion a data change rather than a code change.
    /// The Day 16 additions are declared for the same reason a day early — the rooms
    /// that produce them are authored later in the revision, and appending once is
    /// cheaper than appending twice.
    ///
    /// Values are explicit and must never be renumbered — they are persisted in saves.
    /// Appending is always safe; renumbering silently reinterprets every save in the
    /// wild.
    ///
    /// Not every stat here aggregates guild-wide. See <see cref="GuildStatScope"/>,
    /// which is the one place that distinction is written down.
    /// </summary>
    public enum GuildStat
    {
        /// <summary>Front Desk (was Tavern). Morale multiplier on gold and loot paid per completed quest.</summary>
        RewardYield = 0,

        /// <summary>Tavern. Highest <see cref="Rarity"/> that walks in through the door.</summary>
        RecruitableRarity = 1,

        /// <summary>Barracks (was Training Room). Added to every adventurer's Power, which shortens contracts and cuts failure chance.</summary>
        AdventurerPower = 2,

        /// <summary>Barracks (was Inn). Hard cap on how many adventurers can be housed, above the tier's own beds.</summary>
        HousingCapacity = 3,

        /// <summary>Barracks (was Inn). Multiplier on rest and recovery time between contracts. Higher is faster.</summary>
        RecoverySpeed = 4,

        /// <summary>Front Desk (was Quest Board). Simultaneous contract slots, added to the tier's base.</summary>
        QuestSlots = 5,

        /// <summary>Front Desk (was Quest Board). Hardest contract tier offered, added to the tier's base.</summary>
        MaxQuestTier = 6,

        /// <summary>Armory (post-launch). Flat reduction to failure chance. Zero until it ships, leaving a flat base rate.</summary>
        FailureRateReduction = 7,

        // ---- Day 16: the revenue engine -------------------------------------------
        //
        // Vision_Revision.md §4 named these Revenue and ServiceDemand, which was written
        // before the revision split demand from capacity and is stale in exactly the way
        // Day 15's --checks block was: there is no `revenue` curve on any room. Revenue
        // is seats x spend, and the two halves have to be separate stats because they
        // come from different levers and are read at different moments. See §4 of
        // Docs/Day16_Staff_And_Revenue.md, and the correction now carried in §4 of the
        // charter.

        /// <summary>
        /// PER-ROOM. Seats this room has at its current level. Multiplied by the
        /// catalogue's customer turns per hour to give the room's capacity — the
        /// "capacity" lever of §3.1, owned by the room's own level.
        /// </summary>
        ServiceSeats = 8,

        /// <summary>
        /// PER-ROOM. Gold one served customer leaves behind at this room's current
        /// level. The other half of capacity, and what makes a room worth upgrading
        /// once its seats are full.
        /// </summary>
        CustomerSpend = 9,

        /// <summary>
        /// PER-ROOM. Customers per hour who want in at this room, before the tier's
        /// market size multiplies them. Deliberately flat across levels — the "demand"
        /// lever of §3.1 belongs to the tier, not to the room, and a room whose own
        /// level raised its demand would collapse two of the three levers into one.
        /// </summary>
        ServiceDemand = 10,

        /// <summary>
        /// Guild-wide. How many staff may be employed at once. The Tavern produces it
        /// today; any later room may add to it without a line of code changing.
        /// </summary>
        StaffSlots = 11,

        /// <summary>
        /// Guild-wide. The Front Desk's raw commission, which the trade layer saturates
        /// into a fraction of a contract's gold. Raw rather than a fraction because a
        /// curve that must never exceed 1.0 is a curve a balance pass cannot move
        /// freely, and because an unfilled asset reading zero must mean "takes no cut"
        /// rather than "takes everything".
        /// </summary>
        ContractCommission = 12
    }
}

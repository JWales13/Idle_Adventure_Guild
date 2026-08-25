namespace IdleGuild.Core.Events
{
    /// <summary>Raised whenever a balance changes, not on every idle tick.</summary>
    /// <remarks>
    /// Idle income accrues continuously; publishing per frame would flood the bus for
    /// no benefit. Continuously-ticking displays should read the balance directly and
    /// treat this event as a correction signal.
    /// </remarks>
    public readonly struct CurrencyChanged
    {
        public CurrencyChanged(CurrencyType currency, double newBalance, double delta)
        {
            Currency = currency;
            NewBalance = newBalance;
            Delta = delta;
        }

        public CurrencyType Currency { get; }
        public double NewBalance { get; }

        /// <summary>Signed change. Negative for spends.</summary>
        public double Delta { get; }
    }

    /// <summary>Raised after a building's level increases and stats have been recalculated.</summary>
    public readonly struct BuildingUpgraded
    {
        public BuildingUpgraded(string buildingId, int newLevel)
        {
            BuildingId = buildingId;
            NewLevel = newLevel;
        }

        public string BuildingId { get; }
        public int NewLevel { get; }
    }

    /// <summary>
    /// Raised when aggregated guild stats change for any reason. Consumers should
    /// re-read <see cref="IGuildStats"/> rather than tracking causes themselves.
    /// </summary>
    public readonly struct GuildStatsRecalculated
    {
    }

    /// <summary>Raised when the guild advances a tier, e.g. Village to Town.</summary>
    public readonly struct GuildTierAdvanced
    {
        public GuildTierAdvanced(string tierId, int order)
        {
            TierId = tierId;
            Order = order;
        }

        public string TierId { get; }

        /// <summary>Zero-based position in the tier arc. Content unlocks compare against this.</summary>
        public int Order { get; }
    }

    /// <summary>
    /// Raised once the world is built and ready to be read, whether it was restored from
    /// a save or started fresh.
    /// </summary>
    /// <remarks>
    /// The single "read everything now" signal. Restoring a save writes a great many
    /// values without announcing each one as a gameplay event, precisely so that loading
    /// a level-4 Tavern does not look like four upgrades — which means a UI cannot build
    /// its initial picture out of the change events alone. It waits for this, reads the
    /// current state directly, and treats every other event as a delta from there.
    ///
    /// Published after every OnEnable has run, so a screen that subscribes there still
    /// receives it.
    /// </remarks>
    public readonly struct GameLoaded
    {
        public GameLoaded(bool restoredFromSave, double secondsSinceSave)
        {
            RestoredFromSave = restoredFromSave;
            SecondsSinceSave = secondsSinceSave;
        }

        /// <summary>False for a new guild, which has nothing to restore and no absence to pay for.</summary>
        public bool RestoredFromSave { get; }

        /// <summary>How long the player was away. Zero on a new guild.</summary>
        public double SecondsSinceSave { get; }
    }

    /// <summary>Raised when a new adventurer joins the roster.</summary>
    public readonly struct AdventurerRecruited
    {
        public AdventurerRecruited(string definitionId, string instanceId)
        {
            DefinitionId = definitionId;
            InstanceId = instanceId;
        }

        /// <summary>Identifies which AdventurerDefinition asset this came from.</summary>
        public string DefinitionId { get; }

        /// <summary>Identifies this specific roster member, which is what saves reference.</summary>
        public string InstanceId { get; }
    }

    /// <summary>Raised when an adventurer leaves the roster at the player's request.</summary>
    /// <remarks>
    /// Deliberately distinct from save restoration dropping a member it could not resolve.
    /// This one means a bed was freed by a decision, which is the thing a screen wants to
    /// redraw for and the thing Week 3's analytics pass will want to count. Restoration
    /// stays quiet for the same reason it announces no upgrades: loading a guild that once
    /// retired somebody is not retiring them again.
    /// </remarks>
    public readonly struct AdventurerDismissed
    {
        public AdventurerDismissed(string definitionId, string instanceId)
        {
            DefinitionId = definitionId;
            InstanceId = instanceId;
        }

        /// <summary>Identifies which AdventurerDefinition asset they came from.</summary>
        public string DefinitionId { get; }

        /// <summary>Identifies the member who left. No longer resolvable on the roster.</summary>
        public string InstanceId { get; }
    }

    /// <summary>Raised when a standing order's party is replaced, from its next run onwards.</summary>
    /// <remarks>
    /// Structural rather than cosmetic: the order's card names its members, and a re-form
    /// that did not announce itself would leave that card listing the old party until some
    /// unrelated event happened to redraw it.
    /// </remarks>
    public readonly struct QuestPartyReformed
    {
        public QuestPartyReformed(string assignmentId, string questDefinitionId)
        {
            AssignmentId = assignmentId;
            QuestDefinitionId = questDefinitionId;
        }

        public string AssignmentId { get; }

        public string QuestDefinitionId { get; }
    }

    /// <summary>Raised when a standing order's terms change without its party changing.</summary>
    /// <remarks>
    /// Today that means the repeat flag: recalling a running order clears it, and the
    /// order then survives until its last run resolves. Exactly the same argument as
    /// <see cref="QuestPartyReformed"/>, and it was missed for the same reason it was
    /// caught there — the order's card renders the flag, so a change that did not
    /// announce itself left the card claiming the order was still repeating and still
    /// offering the button that had just been pressed. Day 12 added the event for the
    /// action it was building and not for the one already beside it.
    ///
    /// Deliberately not raised when the order is *removed*: an idle order cancelled
    /// outright disappears, and a running one is removed by the clock, which already
    /// publishes <see cref="QuestCompleted"/>.
    /// </remarks>
    public readonly struct QuestOrderChanged
    {
        public QuestOrderChanged(string assignmentId, string questDefinitionId, bool repeat)
        {
            AssignmentId = assignmentId;
            QuestDefinitionId = questDefinitionId;
            Repeat = repeat;
        }

        public string AssignmentId { get; }

        public string QuestDefinitionId { get; }

        /// <summary>The order's repeat flag as it now stands.</summary>
        public bool Repeat { get; }
    }

    /// <summary>Raised when adventurers are dispatched on a quest.</summary>
    public readonly struct QuestStarted
    {
        public QuestStarted(string definitionId, string instanceId)
        {
            DefinitionId = definitionId;
            InstanceId = instanceId;
        }

        public string DefinitionId { get; }
        public string InstanceId { get; }
    }

    /// <summary>Raised when a quest resolves, whether it succeeded or not.</summary>
    public readonly struct QuestCompleted
    {
        public QuestCompleted(
            string definitionId,
            string instanceId,
            bool succeeded,
            double goldAwarded,
            double reputationAwarded)
        {
            DefinitionId = definitionId;
            InstanceId = instanceId;
            Succeeded = succeeded;
            GoldAwarded = goldAwarded;
            ReputationAwarded = reputationAwarded;
        }

        public string DefinitionId { get; }
        public string InstanceId { get; }
        public bool Succeeded { get; }

        /// <summary>Already scaled by Reward Yield. Zero on failure.</summary>
        public double GoldAwarded { get; }

        /// <summary>Already scaled by Reward Yield. Zero on failure.</summary>
        public double ReputationAwarded { get; }
    }

    /// <summary>Raised when a staff member is taken on.</summary>
    public readonly struct StaffHired
    {
        public StaffHired(string definitionId, string instanceId, int employed, int slots)
        {
            DefinitionId = definitionId;
            InstanceId = instanceId;
            Employed = employed;
            Slots = slots;
        }

        /// <summary>Identifies which StaffDefinition asset this came from.</summary>
        public string DefinitionId { get; }

        /// <summary>Identifies this specific employee, which is what saves reference.</summary>
        public string InstanceId { get; }

        /// <summary>Staff employed after the hire, and the slots available. Carried so a screen can render the squeeze without re-reading.</summary>
        public int Employed { get; }

        public int Slots { get; }
    }

    /// <summary>Raised when a staff member is let go at the player's request.</summary>
    /// <remarks>
    /// Exists on the same day the hire does, which is the whole point of it. §6C's third
    /// finding is that staff slots are a one-way ratchet — fill them cheaply and you can
    /// never upgrade — and that this is the Days 10-11 bed problem, which Day 12 had to
    /// retrofit for adventurers. Retrofitting a reversal is how you discover the economy
    /// was priced against a wall that no longer exists.
    ///
    /// Distinct from save restoration dropping an employee it could not resolve, for the
    /// same reason <see cref="AdventurerDismissed"/> is: this one means a slot was freed
    /// by a decision.
    /// </remarks>
    public readonly struct StaffDismissed
    {
        public StaffDismissed(string definitionId, string instanceId, int employed, int slots)
        {
            DefinitionId = definitionId;
            InstanceId = instanceId;
            Employed = employed;
            Slots = slots;
        }

        public string DefinitionId { get; }

        /// <summary>Identifies who left. No longer resolvable on the staff roster.</summary>
        public string InstanceId { get; }

        public int Employed { get; }

        public int Slots { get; }
    }

    /// <summary>Raised when the player serves a waiting customer by hand.</summary>
    /// <remarks>
    /// The tap, and it is an obligation rather than an addition. §6B sells familiars on
    /// "a free player can do everything a payer can and simply has to tap" — so for a
    /// familiar that collects the takings to be worth a Boon, the takings have to
    /// otherwise need collecting. The premium pillar was load-bearing on a mechanic
    /// neither the code nor the model had.
    ///
    /// Published per tap rather than accrued silently, unlike idle room income, because
    /// a tap is something the player did and wants to see land.
    /// </remarks>
    public readonly struct TakingsCollected
    {
        public TakingsCollected(string buildingId, double gold, double waitingCustomers)
        {
            BuildingId = buildingId;
            Gold = gold;
            WaitingCustomers = waitingCustomers;
        }

        /// <summary>The room the served customer was sitting in — the best-paying one still going unserved.</summary>
        public string BuildingId { get; }

        public double Gold { get; }

        /// <summary>How many are still waiting afterwards. Zero means the staff have the room covered.</summary>
        public double WaitingCustomers { get; }
    }
}

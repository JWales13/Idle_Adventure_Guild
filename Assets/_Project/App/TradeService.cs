using System;
using System.Collections.Generic;
using IdleGuild.Core;
using IdleGuild.Guild;

namespace IdleGuild.App
{
    /// <summary>
    /// What one room is doing right now: who wants in, how many it can hold, how many
    /// actually got served, and what that is worth an hour.
    ///
    /// A struct because the trade layer produces five of these several times a second
    /// and none of them outlive the call. Everything is per hour except
    /// <see cref="SpendPerCustomer"/>, which is per head — mixing the two units in one
    /// type is exactly the mistake that cost Day 15 most of a day, so every field says
    /// which it is.
    /// </summary>
    public readonly struct RoomTrade
    {
        public RoomTrade(
            BuildingDefinition room,
            double demandPerHour,
            double seatCapacityPerHour,
            double servedPerHour,
            double spendPerCustomer)
        {
            Room = room;
            DemandPerHour = demandPerHour;
            SeatCapacityPerHour = seatCapacityPerHour;
            ServedPerHour = servedPerHour;
            SpendPerCustomer = spendPerCustomer;
        }

        public BuildingDefinition Room { get; }

        /// <summary>Customers an hour who want in: the room's own demand times the settlement's size.</summary>
        public double DemandPerHour { get; }

        /// <summary>Customers an hour the room has seats for: seats times the catalogue's turns per hour.</summary>
        public double SeatCapacityPerHour { get; }

        /// <summary>Customers an hour actually served, after staff are shared out.</summary>
        public double ServedPerHour { get; }

        /// <summary>Gold one served customer leaves behind.</summary>
        public double SpendPerCustomer { get; }

        /// <summary>
        /// What the room could serve with unlimited staff: whichever of the crowd that
        /// wants in and the seats to hold them is smaller. Demand is the tier's lever,
        /// seats are the room's, and which of the two binds is the whole question the
        /// player is answering when they choose what to upgrade.
        /// </summary>
        public double WantPerHour => Math.Min(DemandPerHour, SeatCapacityPerHour);

        /// <summary>Customers an hour who want in and have nobody to serve them.</summary>
        public double UnservedPerHour => Math.Max(0d, WantPerHour - ServedPerHour);

        public double RevenuePerHour => ServedPerHour * SpendPerCustomer;

        /// <summary>True when this room trades at all. The Barracks does not.</summary>
        public bool IsEarning => DemandPerHour > 0d;

        /// <summary>
        /// True when seats are the binding constraint rather than the crowd — the room
        /// is turning people away and a level would be felt immediately. The single most
        /// useful thing a room panel can tell the player, and the reason this is on the
        /// struct rather than worked out by a view: §"views hold no rules" in
        /// GuildContext, and this is a rule.
        /// </summary>
        public bool IsTurningPeopleAway => IsEarning && SeatCapacityPerHour < DemandPerHour;
    }

    /// <summary>
    /// The revenue engine: four rooms earning gold per hour, one guild-wide pool of
    /// staff shared between them, and wages coming out of the till.
    ///
    /// <b>This lives in App and not in GuildState, deliberately.</b> Capacity is staff
    /// and demand is buildings, and <c>GuildState.Aggregate</c> reads buildings only.
    /// Teaching it about the staff roster would put a cross-feature reference exactly
    /// where fifteen days of discipline have kept one out — Guild would have to see
    /// Staff. Combining an <see cref="IGuildStats"/> with a roster is what App has
    /// existed for since Day 4-5, and it is the same shape as dispatching a quest.
    /// The features stay Core-only.
    ///
    /// It holds no state of its own. Every number here is derived from the guild as it
    /// currently stands, so an upgrade is felt on the next call and there is nothing to
    /// keep in step with a save. The lifetime totals live on the clock, which is this
    /// project's ledger of what has happened.
    ///
    /// §3.1's three levers, and the rule that none of them may overlap:
    ///
    ///   demand    = the room's own Service Demand x the tier's market size
    ///   capacity  = the room's seats x the catalogue's turns per hour
    ///   throughput= the tier's base service + everyone on the payroll
    /// </summary>
    public sealed class TradeService
    {
        private readonly GameWorld _world;
        private readonly List<RoomTrade> _scratch = new List<RoomTrade>(8);

        public TradeService(GameWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>
        /// Customers an hour the guild can get through: the guildmaster working the bar
        /// themselves, plus everyone employed.
        ///
        /// The tier's share of this is the cold-start fix and is not decoration. Without
        /// it an unstaffed guild serves nobody, so a room upgrade has no marginal value
        /// and neither does the first employee — each needs the other to already exist.
        /// The model run that found this hired no staff across a hundred and fifty hours.
        /// </summary>
        public double ServiceCapacityPerHour()
        {
            return _world.GuildState.CurrentTier.BaseServicePerHour + _world.Staff.ServicePerHour();
        }

        /// <summary>Customers an hour the payroll alone can get through. What wages are charged against.</summary>
        public double StaffCapacityPerHour()
        {
            return _world.Staff.ServicePerHour();
        }

        /// <summary>
        /// Fill <paramref name="destination"/> with every built room and what it is
        /// currently doing, clearing it first. Rooms that earn nothing — the Barracks —
        /// are included with zero demand, because a panel still has to draw them.
        ///
        /// <b>Staff serve the most valuable custom first.</b> Sharing them out
        /// proportionally instead produced the worst deadlock the model ever found:
        /// opening the Provisioner added its demand to the shared pool and diluted the
        /// staff already serving the Tavern and Inn — about 137,000 an hour of damage to
        /// gain 4,000 — so its payback was negative and a player sitting on 276 million
        /// gold never bought a 9,000-gold room. Every new room cannibalising the
        /// existing ones is a design failure and not merely a modelling one; a player
        /// would have felt it too, as "the game got worse when I built something".
        /// Priority allocation is what an actual landlord does, it reads correctly in
        /// the game — a new room does little until you hire for it — and it guarantees
        /// that opening a room can never make you poorer.
        /// </summary>
        public void CollectRooms(List<RoomTrade> destination)
        {
            Allocate(destination);
        }

        /// <summary>What one named room is doing. Convenience over <see cref="CollectRooms"/> for a panel showing a single card.</summary>
        public RoomTrade TradeFor(BuildingDefinition room)
        {
            Allocate(_scratch);
            foreach (RoomTrade trade in _scratch)
            {
                if (trade.Room == room)
                {
                    return trade;
                }
            }

            return new RoomTrade(room, 0d, 0d, 0d, 0d);
        }

        /// <summary>Customers an hour who want in across the whole guild, staff permitting or not.</summary>
        public double TotalWantPerHour()
        {
            Allocate(_scratch);
            double total = 0d;
            foreach (RoomTrade trade in _scratch)
            {
                total += trade.WantPerHour;
            }

            return total;
        }

        /// <summary>
        /// Customers an hour wanting in with nobody free to serve them. The cap on what
        /// tapping is worth, and the number that makes a familiar worth buying early and
        /// worthless later.
        /// </summary>
        public double UnservedWantPerHour()
        {
            return Math.Max(0d, TotalWantPerHour() - ServiceCapacityPerHour());
        }

        /// <summary>
        /// The fraction of the crowd currently being served, 0 to 1. What a staff panel
        /// shows, and what makes over-hiring legible: it sticks at 1 while the wage bill
        /// keeps climbing.
        /// </summary>
        public double Throttle()
        {
            double want = TotalWantPerHour();
            return want <= 0d ? 1d : Math.Min(1d, ServiceCapacityPerHour() / want);
        }

        /// <summary>
        /// Gold the average customer leaves, across the rooms currently trading.
        ///
        /// Weighted by what each room *wants* rather than by what it served, so the
        /// figure does not lurch when staff are reallocated. It is what wages are priced
        /// against, and a wage bill that moved every time the throttle did would be
        /// impossible to reason about.
        /// </summary>
        public double AverageSpendPerCustomer()
        {
            Allocate(_scratch);
            double want = 0d;
            double weighted = 0d;
            foreach (RoomTrade trade in _scratch)
            {
                want += trade.WantPerHour;
                weighted += trade.WantPerHour * trade.SpendPerCustomer;
            }

            return want <= 0d ? 0d : weighted / want;
        }

        /// <summary>Gold an hour across every room, before wages.</summary>
        public double GrossPerHour()
        {
            Allocate(_scratch);
            double total = 0d;
            foreach (RoomTrade trade in _scratch)
            {
                total += trade.RevenuePerHour;
            }

            return total;
        }

        /// <summary>
        /// The wage bill, per hour.
        ///
        /// Priced against what the house is worth rather than as a flat rate per head,
        /// and that is not a simplification — a flat wage against geometric room revenue
        /// is decoration, which the model showed twice: 3,973 an hour of wages against
        /// 15,118,239 of gross, three hundredths of one percent. Staff in a grand hall
        /// are simply paid more, which is also true of hotels.
        ///
        /// Charged against <b>capacity</b> and not against customers actually served,
        /// which is what keeps the mistake this mechanic exists to make possible: hire
        /// past what the crowd needs and you pay for idle hands. If it were charged
        /// against served customers, over-hiring would be free and the whole second
        /// economy would be a slider that only goes up.
        /// </summary>
        public double WagesPerHour()
        {
            return StaffCapacityPerHour() * AverageSpendPerCustomer() * _world.Content.WageShareOfSpend;
        }

        /// <summary>
        /// What the guild actually banks per hour from its rooms.
        ///
        /// <b>The floor is the whole design decision.</b> Wages come out of the till,
        /// not out of the vault: you can have a bad hour, you cannot have a bad night's
        /// sleep. An idle game whose player returns after eight hours with less gold
        /// than they left has punished them for closing it, and no amount of tycoon
        /// realism is worth teaching that. With the floor, over-hiring wastes income you
        /// could have had — a real mistake, with no punishment for absence.
        ///
        /// Gross and wages stay separate readings above precisely so that the squeeze is
        /// visible rather than mysterious. A player whose net has gone flat should be
        /// able to see two numbers converging, not wonder why the game stopped.
        /// </summary>
        public double NetPerHour()
        {
            return Math.Max(0d, GrossPerHour() - WagesPerHour());
        }

        /// <summary>
        /// The room a hand-served customer should come from: whichever still has custom
        /// going unserved and pays the most for it. Null when the staff have everything
        /// covered, which is the tap's cap.
        /// </summary>
        public BuildingDefinition BestUnservedRoom(out double spendPerCustomer)
        {
            Allocate(_scratch);
            BuildingDefinition best = null;
            spendPerCustomer = 0d;

            foreach (RoomTrade trade in _scratch)
            {
                if (trade.UnservedPerHour <= 0d)
                {
                    continue;
                }

                if (best == null || trade.SpendPerCustomer > spendPerCustomer)
                {
                    best = trade.Room;
                    spendPerCustomer = trade.SpendPerCustomer;
                }
            }

            return best;
        }

        /// <summary>
        /// The allocation pass. Walks every built room once to work out what it wants,
        /// orders them by what a customer there is worth, and pours the available
        /// service into them from the top down.
        ///
        /// Deliberately allocation-free apart from growing the destination list, and an
        /// insertion sort rather than a comparison sort because there are five rooms and
        /// this runs on every simulation step. An idle game spends its life in this loop.
        /// </summary>
        private void Allocate(List<RoomTrade> into)
        {
            into.Clear();

            GuildState guild = _world.GuildState;
            double marketSize = guild.CurrentTier.MarketSize;
            double turnsPerHour = _world.Content.CustomerTurnsPerHour;

            foreach (BuildingDefinition room in guild.Buildings)
            {
                if (room == null || guild.GetLevel(room.Id) < 1)
                {
                    continue;
                }

                double demand = guild.EffectFor(room, GuildStat.ServiceDemand) * marketSize;
                double seatCapacity = guild.EffectFor(room, GuildStat.ServiceSeats) * turnsPerHour;
                double spend = guild.EffectFor(room, GuildStat.CustomerSpend);

                RoomTrade trade = new RoomTrade(room, demand, seatCapacity, 0d, spend);

                // Highest spend first, so the pour below serves the good table first.
                int position = into.Count;
                while (position > 0 && into[position - 1].SpendPerCustomer < spend)
                {
                    position--;
                }

                into.Insert(position, trade);
            }

            double remaining = ServiceCapacityPerHour();
            for (int index = 0; index < into.Count; index++)
            {
                RoomTrade trade = into[index];
                double served = Math.Min(trade.WantPerHour, Math.Max(0d, remaining));
                remaining -= served;
                into[index] = new RoomTrade(
                    trade.Room,
                    trade.DemandPerHour,
                    trade.SeatCapacityPerHour,
                    served,
                    trade.SpendPerCustomer);
            }
        }
    }
}

#!/usr/bin/env python3
"""
A model of the REVISED Idle Adventurer's Guild -- an idle hotel tycoon.

Supersedes guild_model.py, which models the one-income-stream game that exists in the
build today. Keep both until the revision ships, then retire the old one.

The shape being modelled:
  * five rooms, four of which earn gold per hour
  * one guild-wide staff pool; each room has a service demand, and the fraction of
    total demand your staff can cover throttles EVERY room at once
  * wages are ongoing and net is floored at zero -- wages come out of the till, not
    out of the vault
  * adventurers arrive on their own clock (not as a share of trade), take contracts,
    and pay REPUTATION; the gold from a contract arrives as the Front Desk's commission
  * reputation is the only thing that advances a tier; tiers unlock rooms

    python3 tycoon_model.py             pacing
    python3 tycoon_model.py --profile   + purchase-gap profile
    python3 tycoon_model.py --checks    + no-dead-levels and structural checks
"""
import math, statistics, sys

# --------------------------------------------------------------------- curves --
def curve(base, linear=0.0, growth=0.0):
    return {"b": base, "l": linear, "g": growth}

def ev(c, level):
    """ScalingCurve.Evaluate -- growth is percent per level so an all-zero curve is flat."""
    if level < 1: return 0.0
    steps = level - 1
    linear = c["b"] + c["l"] * steps
    return linear if abs(c["g"]) < 1e-9 else linear * ((1.0 + c["g"]) ** steps)

# -------------------------------------------------------------------- content --
def content():
    return {
        "startingGold": 150,
        # ---- the five rooms ------------------------------------------------
        # revenue  : gold/hr at FULL service
        # demand   : service points needed to run at full
        # unlockTier: guild tier order at which the room may be built
        # Each earning room now has THREE inputs from three sources, which is the whole
        # economy: demand comes from the tier (how big the settlement has grown), seats
        # and spend come from the room's own level, and turnover comes from staff.
        #   demand   = baseDemand x marketSize(tier)
        #   seatCap  = seats(level) x MAX_TURNS_PER_HOUR
        #   want     = min(demand, seatCap)          what this room could serve
        #   served   = want x (staffCapacity / totalWant, capped at 1)
        #   revenue  = served x spend(level)
        # The rhythm that falls out: advancing a tier multiplies demand, so everything
        # you own is suddenly insufficient and you go shopping; upgrade until you serve
        # everyone who wants in, and demand is the ceiling again until the next tier.
        "rooms": {
            "tavern": dict(name="Tavern", unlockTier=0, maxLevel=60,
                           cost=curve(35, 0, 0.155),
                           baseDemand=60.0,
                           seats=curve(4, 1.1, 0.0),
                           spend=curve(1.5, 0, 0.075),
                           rarity=curve(0, 0.11, 0.0),
                           staffSlots=curve(2, 1.4, 0.0)),
            "front_desk": dict(name="Front Desk", unlockTier=0, maxLevel=50,
                               cost=curve(70, 0, 0.17),
                               baseDemand=0.0,                    # earns via commission
                               seats=curve(0, 0, 0.0),
                               spend=curve(0, 0, 0.0),
                               commission=curve(0.25, 0.055, 0.0),
                               questSlots=curve(0, 0.11, 0.0),
                               maxTier=curve(0, 0.055, 0.0)),
            "barracks": dict(name="Barracks", unlockTier=1, maxLevel=40,
                             cost=curve(400, 0, 0.185),
                             baseDemand=0.0, seats=curve(0,0,0.0), spend=curve(0,0,0.0),
                             beds=curve(2, 0.4, 0.0),
                             power=curve(4, 0, 0.145),
                             recovery=curve(0.10, 0, 0.10)),
            "inn": dict(name="Inn", unlockTier=1, maxLevel=55,
                        cost=curve(900, 0, 0.16),
                        baseDemand=11.0,
                        seats=curve(2, 0.6, 0.0),
                        spend=curve(26, 0, 0.08)),
            "provisioner": dict(name="Provisioner", unlockTier=2, maxLevel=50,
                                cost=curve(9000, 0, 0.165),
                                baseDemand=24.0,
                                seats=curve(3, 0.8, 0.0),
                                spend=curve(45, 0, 0.085)),
        },
        # ---- staff ---------------------------------------------------------
        # one pool, guild-wide. service covers demand; wage is ongoing.
        # Staff are the game's small, frequent purchase, and that job matters more than
        # it looks. Days 10-11 recorded that ~300 individual training purchases were what
        # filled the stretches where only an expensive room level was on offer; cutting
        # individual training took that filler away with it, and the first run's purchase
        # gaps went to a 90th percentile of 189 minutes. Staff have to replace it: many
        # slots, hired one at a time, cheap relative to a room level.
        #
        # Wages are geometric per tier for the same reason room revenue is: a flat wage
        # against geometric income is decoration, which is exactly what the first run
        # showed -- 420/hr of wages against 463,022/hr of gross, or 0.09%.
        "staff": [
            # Service is now plainly "customers served per hour", and the ladder has to
            # improve per gold as it climbs -- an earlier run hired ninety-six Potboys
            # and never upgraded once, because the tiers above were strictly worse value.
            # Same defect Day 13 found in the rarity ladder, in different clothes.
            # Wage is a fixed multiple of service so the wage bill scales with the
            # capacity it buys rather than falling behind geometric room revenue.
            dict(name="Potboy",  tier=0, hire=45,       service=9.0,     wage=18.0 * 0.30,     minTier=0),
            dict(name="Server",  tier=1, hire=1400,     service=95.0,    wage=800.0 * 0.30,    minTier=1),
            dict(name="Barkeep", tier=2, hire=46000,    service=1100.0,  wage=32000.0 * 0.30,  minTier=2),
            dict(name="Steward", tier=3, hire=1500000,  service=14000.0, wage=1250000.0 * 0.30, minTier=3),
        ],
        # ---- tiers ---------------------------------------------------------
        # baseBeds: the tier's own housing, so Village can host adventurers with no Barracks
        "tiers": [
            dict(baseService=14.0, contractScale=1.0, marketSize=1.0, id="village", name="Village", order=0, slots=1, maxTier=1, rep=12,
                 baseBeds=2, req={"tavern": 3, "front_desk": 2}),
            dict(baseService=30.0, contractScale=5.0, marketSize=9.0, id="town", name="Town", order=1, slots=2, maxTier=2, rep=700,
                 baseBeds=2, req={"tavern": 14, "front_desk": 9}),
            dict(baseService=30.0, contractScale=22.0, marketSize=70.0, id="city", name="City", order=2, slots=3, maxTier=3, rep=42000,
                 baseBeds=2, req={"tavern": 30, "front_desk": 20, "inn": 18, "barracks": 14}),
            dict(baseService=30.0, contractScale=95.0, marketSize=520.0, id="capital", name="Capital", order=3, slots=4, maxTier=4, rep=0,
                 baseBeds=2, req={}),
        ],
        # ---- adventurers ---------------------------------------------------
        # no individual levels: power comes from the Barracks. rarity is a flat multiple.
        "adventurers": {
            "militia_recruit":      dict(name="Militia Recruit",      rarity=0, hire=40,     mult=1.0),
            "hedge_knight":         dict(name="Hedge Knight",         rarity=1, hire=260,    mult=2.0),
            "wandering_ranger":     dict(name="Wandering Ranger",     rarity=2, hire=1800,   mult=4.0),
            "arcane_battlemage":    dict(name="Arcane Battlemage",    rarity=3, hire=15000,  mult=8.0),
            "dragonsworn_champion": dict(name="Dragonsworn Champion", rarity=4, hire=130000, mult=16.0),
        },
        # arrivals are their own clock, NOT a share of trade
        "arrivalSeconds": curve(240, -3.2, 0.0),   # tavern level shortens the gap
        "crowdSlots": 4,
        "quests": {
            "rat_cellar":     dict(name="Rat Cellar",     tier=1, minTier=0, need=1, rec=6,    dur=45,  fail=0.05, gold=9,    rep=4),
            "bandit_patrol":  dict(name="Bandit Patrol",  tier=1, minTier=0, need=2, rec=20,   dur=90,  fail=0.12, gold=26,   rep=14),
            "watchtower":     dict(name="Ruined Watchtower", tier=2, minTier=1, need=2, rec=70, dur=150, fail=0.16, gold=110,  rep=110),
            "sunken_crypt":   dict(name="Sunken Crypt",   tier=3, minTier=2, need=3, rec=260,  dur=240, fail=0.18, gold=760,  rep=900),
            "dragons_roost":  dict(name="Dragon's Roost", tier=4, minTier=3, need=3, rec=1100, dur=360, fail=0.20, gold=3200, rep=3000),
        },
    }


# ---------------------------------------------------------------------- world --
class World:
    def __init__(self, c):
        self.c = c
        self.levels = {k: 0 for k in c["rooms"]}
        self.tier = 0
        self.gold = c["startingGold"]
        self.rep = 0.0
        self.staff = []          # indices into c["staff"]
        self.roster = []         # {defId}
        self.crowd = []          # adventurers waiting to be hired
        self.runs, self.orders = [], []
        self.t = 0.0
        self.nextArrival = 0.0
        self.grossEarned = self.wagesPaid = self.questGold = 0.0

    def td(self):
        return self.c["tiers"][self.tier]

    def built(self, rid):
        return self.levels[rid] >= 1

    def unlocked(self, rid):
        return self.c["rooms"][rid]["unlockTier"] <= self.td()["order"]

    def _sum(self, key):
        total = 0.0
        for rid, lvl in self.levels.items():
            r = self.c["rooms"][rid]
            if lvl >= 1 and key in r:
                total += ev(r[key], lvl)
        return total

    MAX_TURNS_PER_HOUR = 12.0      # five minutes a head: eat, drink, pay, leave

    # ---- the three levers --------------------------------------------------
    def service(self):
        """Customers per hour the staff can actually get through, guildmaster included."""
        return self.td()["baseService"] + sum(self.c["staff"][i]["service"] for i in self.staff)

    def roomWant(self, rid):
        """What this room could serve if staff were unlimited: whichever of the crowd
        that wants in (tier) and the seats to hold them (room level) is smaller."""
        r = self.c["rooms"][rid]
        lvl = self.levels[rid]
        if lvl < 1 or r.get("baseDemand", 0.0) <= 0.0:
            return 0.0
        demand = r["baseDemand"] * self.td()["marketSize"]
        seatCap = ev(r["seats"], lvl) * self.MAX_TURNS_PER_HOUR
        return min(demand, seatCap)

    def totalWant(self):
        return sum(self.roomWant(rid) for rid in self.levels)

    def _allocation(self):
        """Staff serve the most valuable custom first, and everything else gets what is
        left. Allocating PROPORTIONALLY instead produced a deadlock the model found with
        276 million gold in the bank: opening the Provisioner added its demand to the
        shared pool, diluting the staff already serving the Tavern and Inn -- about
        137,000/hr of damage to gain 4,000. So its payback was negative and it was never
        bought, at any price, ever.

        Every new room cannibalising the existing ones is a real design failure and not
        merely a modelling one: a player would have felt it too. Priority allocation
        fixes it and is what an actual landlord does -- the good custom gets served, a
        new counter takes whatever capacity is spare, and opening one can never make you
        poorer. It also reads correctly in the game: a new room does nothing much until
        you hire for it."""
        rooms = [(ev(self.c["rooms"][r]["spend"], self.levels[r]), r)
                 for r in self.levels if self.roomWant(r) > 0.0]
        rooms.sort(reverse=True)
        left = self.service()
        served = {}
        for spend, rid in rooms:
            take = min(self.roomWant(rid), left)
            served[rid] = take
            left -= take
        return served

    def roomRevenue(self, rid):
        served = self._allocation().get(rid, 0.0)
        if served <= 0.0:
            return 0.0
        return served * ev(self.c["rooms"][rid]["spend"], self.levels[rid])

    def demand(self):
        return self.totalWant()

    def throttle(self):
        # baseService is the guildmaster working the bar themselves, and it exists to
        # break a cold start the model found the hard way. With throttle = service /
        # demand and no staff at all, every room earns nothing, so a room upgrade has
        # zero marginal value AND the first staff member has almost none either --
        # each needs the other to already exist. The run that found this hired no staff
        # for a hundred and fifty hours and ran the entire guild on contract commission.
        #
        # Same shape as Day 4-5's opening deadlock, where Housing Capacity's zero base
        # meant a guild with no Inn could recruit nobody and therefore never afford one.
        # That was solved in data with starting gold rather than a branch in code, and
        # this is solved the same way.
        d = self.totalWant()
        return 1.0 if d <= 1e-9 else min(1.0, self.service() / d)

    def staffSlots(self):
        return int(self._sum("staffSlots"))

    WAGE_SHARE = 0.22

    def avgSpend(self):
        """Gold per customer across the rooms currently trading."""
        want = self.totalWant()
        if want <= 1e-9:
            return 0.0
        return sum(self.roomWant(r) * ev(self.c["rooms"][r]["spend"], self.levels[r])
                   for r in self.levels) / want

    def wagesPerHour(self):
        """Wages priced against what the house is worth, not as a flat rate.

        A flat wage is decoration and the model has shown it twice now: spend per
        customer compounds geometrically with room level while a fixed wage does not,
        so by the endgame the bill was 3,973/hr against 15,118,239/hr of gross -- three
        hundredths of one percent. Staff in a grand hall are simply paid more.

        Priced against CAPACITY rather than customers actually served, which is what
        keeps the tension alive: hire past what the crowd needs and you pay for idle
        hands, which is the mistake this mechanic exists to make visible."""
        capacity = sum(self.c["staff"][i]["service"] for i in self.staff)
        return capacity * self.avgSpend() * self.WAGE_SHARE

    def grossPerHour(self):
        return sum(self.roomRevenue(rid) for rid in self.levels)

    def netPerHour(self):
        # THE FLOOR: wages come out of the till, not out of the vault.
        return max(0.0, self.grossPerHour() - self.wagesPerHour())

    def questGoldPerHour(self):
        """What the contracts are worth per hour, at the commission the desk currently
        takes. The first run left this out of the payback ranking, and the consequence
        was severe: the Barracks earns nothing directly, so a payback-ranked player
        never bought one except where a tier gate forced it -- which quietly made the
        entire adventurer half of the game invisible to the decision loop. A support
        room does pay back; it just pays back through somebody else's till."""
        if not self.roster:
            return 0.0
        cut = self.commission()
        total = 0.0
        slots = self.questSlots()
        ranked = sorted(range(len(self.roster)), key=lambda i: -self.power_of(self.roster[i]))
        avail = [q for q in self.c["quests"].values() if self.questAvailable(q)]
        if not avail:
            return 0.0
        used = 0
        for _ in range(slots):
            best = 0.0
            for q in avail:
                party = ranked[used:used + q["need"]]
                if len(party) < q["need"]:
                    continue
                p = sum(self.power_of(self.roster[i]) for i in party)
                cycle = self.duration(q, p) + self.rest_of(self.roster[party[0]])
                best = max(best, q["gold"] * self.td()["contractScale"] * cut
                           * (1 - self.failure(q, p)) * 3600.0 / cycle)
            if best <= 0.0:
                break
            total += best
            used += max(1, min(q["need"] for q in avail))
        return total

    def goldPerHour(self):
        """Everything the guild earns. This is what a payback ranking has to see."""
        return self.netPerHour() + self.questGoldPerHour()

    # ---- adventurers -------------------------------------------------------
    def beds(self):
        return int(self.td()["baseBeds"] + self._sum("beds"))

    def power_of(self, m):
        return self.c["adventurers"][m["defId"]]["mult"] * max(1.0, self._sum("power"))

    def rest_of(self, m):
        return 60.0 / max(1e-6, 1.0 + self._sum("recovery"))

    def maxRarity(self):
        return int(self._sum("rarity"))

    def questSlots(self):
        return int(self.td()["slots"] + self._sum("questSlots"))

    def maxQuestTier(self):
        return int(self.td()["maxTier"] + self._sum("maxTier"))

    COMMISSION_CEILING = 0.85

    def commission(self):
        # A desk taking more than the whole contract is nonsense, and the first run
        # produced exactly that -- 123% at Front Desk 50. Saturating rather than linear.
        r = self.c["rooms"]["front_desk"]
        if not self.built("front_desk"):
            return 0.20
        raw = ev(r["commission"], self.levels["front_desk"])
        return self.COMMISSION_CEILING * (1.0 - math.exp(-raw))

    # ---- quests ------------------------------------------------------------
    def duration(self, q, p):
        return q["dur"] / min(2.0, max(0.5, math.sqrt(max(0.0, p) / max(1e-4, q["rec"]))))

    def failure(self, q, p):
        return min(0.9, max(0.0, q["fail"] * min(2.0, max(0.0, 2.0 - p / max(1e-4, q["rec"])))))

    def questAvailable(self, q):
        return q["minTier"] <= self.td()["order"] and q["tier"] <= self.maxQuestTier()


# --------------------------------------------------------------------- player --
#
# The tycoon decision, stated: buy whatever pays back fastest per gold, with three
# overrides that a real player applies and a payback calculation would not.
#   1. advance a tier the moment it is possible -- free and always good
#   2. a tier gate that is short gets bought regardless of payback; it unlocks rooms
#   3. hire an adventurer whenever a bed and the gold are free, because reputation is
#      the only thing that advances a tier and only contracts pay it
# Everything else is ranked by payback: cost divided by the gold-per-hour it adds.

def payback(w, kind, key):
    """Seconds of net income to earn back this purchase. Lower is better. None = no payback."""
    before = w.goldPerHour()
    if kind == "room":
        lvl = w.levels[key] + 1
        cost = ev(w.c["rooms"][key]["cost"], lvl)
        w.levels[key] += 1; after = w.goldPerHour(); w.levels[key] -= 1
    else:
        s = w.c["staff"][key]
        cost = s["hire"]
        w.staff.append(key); after = w.goldPerHour(); w.staff.pop()
    gain = after - before
    return None if gain <= 1e-9 else cost / gain

def purchase(w):
    while True:
        td = w.td()
        if (td["req"] or td["rep"] > 0) and w.rep >= td["rep"] and \
                all(w.levels[b] >= l for b, l in td["req"].items()):
            w.tier += 1
            continue

        # SAVE FOR THE GATE. Without this the player nickel-and-dimes itself forever:
        # a payback ranking always has something cheap and profitable to buy, so gold
        # never accumulates to the one purchase that actually unlocks the tier. The
        # trace was unambiguous -- reputation cleared Village's gate in twenty minutes
        # and the Front Desk it also required went unbuilt for over three hours, while
        # nine Potboys were hired instead.
        #
        # Real players do save toward a visible goal, which is the other half of this:
        # the tier panel has to show what is still missing, or the player is left
        # running the same greedy policy the model just failed at.
        # Reserving bluntly is worse than not reserving at all -- the first attempt
        # pushed Capital from 16 hours to 86, because hoarding for a distant gate
        # starves the compounding that would have paid for it. The rule has to be
        # economic rather than absolute: buy anything that pays for itself SOONER than
        # you could have saved the gate cost, because that purchase gets you to the
        # gate faster. Threshold-free, and it is what a sensible player does.
        # Reputation is a tier gate too, and only contracts pay it -- so an empty roster
        # when the rep gate is short is exactly as blocking as an unbuilt room. Without
        # this the model stalled in Village forever on a forty-gold coin flip: an
        # adventurer cost 40, a potboy cost 39, gold crossed 39 first every single time,
        # and the guild hired staff it did not need for a crowd it could not grow while
        # never once sending anybody on a contract.
        needBody = (w.rep < td["rep"] and not w.roster and w.crowd
                    and len(w.roster) < w.beds())
        if needBody:
            cheapest = min(w.c["adventurers"][a]["hire"] for a in w.crowd)
            if w.gold >= cheapest:
                aid = min(w.crowd, key=lambda a: w.c["adventurers"][a]["hire"])
                w.gold -= w.c["adventurers"][aid]["hire"]
                w.roster.append(dict(defId=aid))
                w.crowd.remove(aid)
                continue

        reserve = 0.0
        reserveDeadline = float("inf")
        unmet = [b for b, l in td["req"].items() if w.levels[b] < l]
        if needBody:
            reserve = min(w.c["adventurers"][a]["hire"] for a in w.crowd)
        if unmet:
            reserve = max(reserve, min(ev(w.c["rooms"][b]["cost"], w.levels[b] + 1) for b in unmet))
            rate = max(1e-6, w.goldPerHour())
            reserveDeadline = max(0.0, reserve - w.gold) / rate * 3600.0

        # rule 3: bodies for contracts
        if w.crowd and len(w.roster) < w.beds():
            best = max(range(len(w.crowd)), key=lambda i: w.c["adventurers"][w.crowd[i]]["mult"])
            aid = w.crowd[best]
            if w.c["adventurers"][aid]["hire"] <= w.gold - reserve:
                w.gold -= w.c["adventurers"][aid]["hire"]
                w.roster.append(dict(defId=aid)); w.crowd.pop(best); continue

        options = []   # (payback, cost, kind, key)
        for rid, r in w.c["rooms"].items():
            if not w.unlocked(rid) or w.levels[rid] + 1 > r["maxLevel"]:
                continue
            cost = ev(r["cost"], w.levels[rid] + 1)
            short = w.levels[rid] < td["req"].get(rid, 0)
            if cost > w.gold:
                continue
            if not short and w.gold - cost < reserve:
                pb = payback(w, "room", rid)
                if pb is None or pb > reserveDeadline:
                    continue
            pb = payback(w, "room", rid)
            if short:
                options.append((-1.0, cost, "room", rid))      # rule 2: gates jump the queue
            elif pb is not None:
                options.append((pb, cost, "room", rid))
        if len(w.staff) < w.staffSlots():
            for i, s in enumerate(w.c["staff"]):
                if s["minTier"] <= td["order"] and s["hire"] <= w.gold and (
                        w.gold - s["hire"] >= reserve
                        or (payback(w, "staff", i) or float("inf")) <= reserveDeadline):
                    pb = payback(w, "staff", i)
                    if pb is not None:
                        options.append((pb, s["hire"], "staff", i))
        if not options:
            # Nothing affordable. Report the cheapest thing that would be, so the
            # simulation can jump straight to the moment it becomes buyable rather
            # than re-ranking every option at every tick -- which at a five-second
            # step over two hundred hours is 144,000 evaluations of a payback that
            # cannot have changed.
            wall = []
            for rid, r in w.c["rooms"].items():
                if w.unlocked(rid) and w.levels[rid] + 1 <= r["maxLevel"]:
                    wall.append(ev(r["cost"], w.levels[rid] + 1))
            if len(w.staff) < w.staffSlots():
                wall += [s["hire"] for s in w.c["staff"] if s["minTier"] <= td["order"]]
            for aid in w.crowd:
                if len(w.roster) < w.beds():
                    wall.append(w.c["adventurers"][aid]["hire"])
            return min(wall) if wall else None
        options.sort(key=lambda o: (o[0], o[1]))
        pb, cost, kind, key = options[0]
        w.gold -= cost
        if kind == "room":
            w.levels[key] += 1
        else:
            w.staff.append(key)

# -------------------------------------------------------------------- arrivals --
def arrivalGap(w):
    lvl = max(1, w.levels["tavern"])
    return max(25.0, ev(w.c["arrivalSeconds"], lvl))

def rarityRoll(w, u):
    """Deterministic stand-in for the distribution: the ceiling the Tavern allows,
    stepped down by a rotating offset so the crowd is mixed rather than uniform."""
    ceiling = min(4, w.maxRarity())
    return max(0, ceiling - (u % 3))

def admitArrival(w, n):
    if len(w.crowd) >= w.c["crowdSlots"]:
        w.crowd.pop(0)
    band = rarityRoll(w, n)
    for aid, a in sorted(w.c["adventurers"].items(), key=lambda kv: -kv[1]["rarity"]):
        if a["rarity"] <= band:
            w.crowd.append(aid); return

# --------------------------------------------------------------------- quests --
def syncQuests(w):
    slots = w.questSlots()
    avail = [q for q in w.c["quests"].values() if w.questAvailable(w.c["quests"][k]) ] if False else \
            [k for k, q in w.c["quests"].items() if w.questAvailable(q)]
    if not avail:
        return
    busy = {i for o in w.orders for i in o["party"]}
    while len(w.orders) < slots:
        free = [i for i in range(len(w.roster)) if i not in busy]
        if not free:
            break
        ranked = sorted(free, key=lambda i: -w.power_of(w.roster[i]))
        best, bestRate = None, -1.0
        for k in avail:
            q = w.c["quests"][k]
            if len(ranked) < q["need"]:
                continue
            party = ranked[:q["need"]]
            p = sum(w.power_of(w.roster[i]) for i in party)
            cycle = w.duration(q, p) + w.rest_of(w.roster[party[0]])
            rate = (q["rep"] * (1 - w.failure(q, p))) / max(1e-6, cycle)
            if rate > bestRate:
                best, bestRate = (k, party), rate
        if best is None:
            break
        k, party = best
        w.orders.append(dict(quest=k, party=party, running=False))
        busy |= set(party)

    for o in w.orders:
        if o["running"] or len(w.runs) >= slots:
            continue
        q = w.c["quests"][o["quest"]]
        p = sum(w.power_of(w.roster[i]) for i in o["party"])
        w.runs.append(dict(order=o, remaining=w.duration(q, p),
                           gold=q["gold"], rep=q["rep"], fail=w.failure(q, p)))
        o["running"] = True


# ------------------------------------------------------------------ simulation --
def simulate(c, horizon=150 * 3600, step=20.0):
    """Fixed small steps: income here is a continuous rate, not an event, so the
    event-stepped approach the old model used no longer fits."""
    w = World(c)
    marks, events, prev = {}, [], None
    arrivals = 0

    def allMaxed():
        return all(w.levels[r] >= c["rooms"][r]["maxLevel"] for r in w.levels)

    wall = 0.0
    while w.t < horizon:
        if w.gold >= wall:
            wall = purchase(w) or float("inf")
        syncQuests(w)

        cur = (tuple(sorted(w.levels.items())), len(w.staff), len(w.roster))
        if cur != prev:
            events.append(w.t); prev = cur
        marks.setdefault(w.td()["id"], w.t)
        if allMaxed():
            marks.setdefault("maxed", w.t); break

        # ---- advance ----
        w.t += step
        gross = w.grossPerHour() * step / 3600.0
        wages = w.wagesPerHour() * step / 3600.0
        net = max(0.0, gross - wages)
        w.gold += net
        w.grossEarned += gross; w.wagesPaid += min(wages, gross)

        if w.built("tavern"):
            w.nextArrival -= step
            if w.nextArrival <= 0:
                admitArrival(w, arrivals); arrivals += 1
                w.nextArrival = arrivalGap(w)
                if len(w.roster) < w.beds() and w.crowd and \
                        min(c["adventurers"][a]["hire"] for a in w.crowd) <= w.gold:
                    wall = 0.0

        for r in list(w.runs):
            r["remaining"] -= step
            if r["remaining"] <= 0:
                cut = w.commission()
                # Contracts pay what the region can afford. Static rewards against
                # geometric room revenue made Dragon's Roost worth 26,000 in a guild
                # earning fifteen million an hour -- the adventurer half of the game
                # became rounding error exactly where it was meant to matter most.
                r["gold"] *= w.td()["contractScale"]
                w.gold += r["gold"] * cut * (1 - r["fail"])
                w.questGold += r["gold"] * cut * (1 - r["fail"])
                w.rep += r["rep"] * (1 - r["fail"])
                r["order"]["running"] = False
                w.runs.remove(r)
                if w.td()["rep"] > 0 and w.rep >= w.td()["rep"]:
                    wall = 0.0    # reputation may have opened a tier
    return w, marks, events, arrivals

def hms(s):
    if s is None: return "never"
    s = int(s); return f"{s//3600}h{s%3600//60:02}m"

def report(c):
    w, marks, events, arrivals = simulate(c)
    print("Idle Adventurer's Guild - TYCOON model\n")
    print(f"  Village -> Town {hms(marks.get('town')):>8}    Town -> City {hms(marks.get('city')):>8}"
          f"    City -> Capital {hms(marks.get('capital')):>8}")
    print(f"  everything maxed {hms(marks.get('maxed')):>7}    purchase decisions {len(events)}"
          f"    adventurer arrivals {arrivals}")
    print()
    print(f"  final rooms      " + "  ".join(f"{c['rooms'][r]['name']} {w.levels[r]}" for r in w.levels))
    print(f"  staff {len(w.staff)}/{w.staffSlots()}   service {w.service():.0f}/{w.demand():.0f} "
          f"(throttle {w.throttle()*100:.0f}%)   roster {len(w.roster)}/{w.beds()}")
    print(f"  gross/hr {w.grossPerHour():>14,.0f}   wages/hr {w.wagesPerHour():>13,.0f}"
          f"   net/hr {w.netPerHour():>14,.0f}")
    tot = w.grossEarned + w.questGold
    print(f"  lifetime: rooms {w.grossEarned:,.0f} ({100*w.grossEarned/max(1,tot):.0f}%)   "
          f"commission {w.questGold:,.0f} ({100*w.questGold/max(1,tot):.0f}%)   "
          f"wages {w.wagesPaid:,.0f}")
    return w, marks, events

if __name__ == "__main__":
    c = content()
    w, marks, events = report(c)
    if "--profile" in sys.argv:
        gaps = sorted(events[i+1]-events[i] for i in range(len(events)-1)) or [0]
        print(f"\n  gap between purchases: median {statistics.median(gaps)/60:.1f} min, "
              f"90th pct {gaps[int(len(gaps)*0.9)]/60:.0f} min, max {gaps[-1]/60:.0f} min")
    if "--checks" in sys.argv:
        print("\n  no dead levels - last step of every curve still moves:")
        for rid, r in c["rooms"].items():
            for k in ("revenue", "beds", "power", "commission", "rarity", "staffSlots", "questSlots"):
                if k in r and (ev(r[k], r["maxLevel"]) - ev(r[k], r["maxLevel"]-1)) != 0:
                    L = r["maxLevel"]
                    print(f"    {r['name']:12} {k:11} L{L-1}->{L}: {ev(r[k],L-1):12,.2f} -> {ev(r[k],L):12,.2f}")
        print("\n  rarity ladder (flat multiples, no individual levels):")
        for aid, a in sorted(c["adventurers"].items(), key=lambda kv: kv[1]["rarity"]):
            print(f"    {a['name']:22} x{a['mult']:5.1f} power   hire {a['hire']:>9,}")
        print("\n  opening: Tavern L1 costs "
              f"{ev(c['rooms']['tavern']['cost'],1):.0f} of {c['startingGold']} starting gold "
              f"{'OK' if ev(c['rooms']['tavern']['cost'],1) <= c['startingGold'] else '-- DEADLOCK'}")

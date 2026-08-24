#!/usr/bin/env python3
"""
A model of the Idle Adventurer's Guild core loop, for balancing.

Why this exists: the shape of a curve is not visible in the Inspector. A cost curve
and an effect curve that each look reasonable on their own can produce a game that
runs out of things to buy two hours before it runs out of tiers -- which is exactly
what the Day 4-5 first-pass numbers did, and it took a simulation rather than a
playthrough to see it.

It replicates the C# exactly: ScalingCurve.Evaluate, GuildState.Aggregate,
QuestResolution's duration / failure / reward, roster rest timers, quest slots and
standing orders. Outcomes are taken at their expected value rather than rolled, so
the same inputs always give the same pacing and a curve change is legible against it.

    python3 guild_model.py            report the current shipping values
    python3 guild_model.py --profile  add the purchase-gap profile
    python3 guild_model.py --checks   add the no-dead-levels and rarity checks

KEEP THIS IN STEP WITH THE ASSETS. It is a model, not a source of truth: if the
.asset files change and this does not, its answers are confidently wrong. The values
below are the ones written on Day 13; everything except the five training-cost bases
dates from Days 10-11.

Two things about it changed on Days 10-11, both forced by content that finally has
more than one kind of adventurer in it:

  * The old policy hired the *cheapest* archetype available. Across a whole simulated
    game that bought Militia Recruits and nothing else -- the Hedge Knight and the
    Wandering Ranger were never purchased once. "Higher-rarity archetypes are
    pointless" was true, but the model had no way to find that out; it had never
    tried one. It now hires the best archetype it can afford for a slot it needs, and
    saves for the best one when the bed is a pure upgrade.

  * The old model chose one quest for the whole guild and judged it with the
    *strongest* party's power. That is exact while every adventurer is identical and
    wrong the moment they are not. Each party now picks its own work. On unchanged
    Day 8-9 assets this alone moves Capital from 4h07m to 4h41m and everything-maxed
    from 17h21m to 19h37m: the published Day 8-9 figures were about 13% optimistic on
    the tail, and those are the numbers to compare against, not the old ones.

Day 13 changed one thing in each half, and the two are the same finding seen twice:

  * The player's swap rule required a *level-1* replacement to already beat the
    incumbent. Nobody plays that way, and it misses by 6% -- at a maxed Training Room
    a level-1 Champion is 379 against a maxed Recruit's 403, which is two training
    levels and about 1,000 gold away. That 6% is the whole reason the impatient player
    appeared to be locked out of the top rarity band. It now prices the catch-up (see
    switching_cost) and buys the swap when it can pay for it.

  * The training cost ladder tripled per rarity band while power only doubled, so a
    Legendary bed cost 81x a Common bed to realise and delivered 16x the power. That
    is what made Day 12's greedy rule look like an arbitrage bug: the arbitrage was
    real, and it was priced at ten hours. With the ladder at 2x per band -- each band
    doubles power and doubles the gold to reach it -- strict, greedy and pragmatic
    policies all land inside eighty minutes of each other. The bracket was never a
    question about the player.
"""
import math
import statistics
import sys

# ---------------------------------------------------------------- primitives --

REWARD_YIELD, RECRUIT_RARITY, POWER, BEDS, RECOVERY, SLOTS, MAXTIER, FAILRED = range(8)
ADD, MUL = 0, 1
NEUTRAL = {REWARD_YIELD: 1.0, RECOVERY: 1.0}
RARITY_NAMES = ["Common", "Uncommon", "Rare", "Epic", "Legendary"]


def curve(base, linear, growth):
    return {"b": base, "l": linear, "g": growth}


def ev(c, level):
    """ScalingCurve.Evaluate. Growth is percent per level, so an all-zero curve
    evaluates flat rather than collapsing through a pow(0, n) term."""
    steps = max(0, level - 1)
    linear = c["b"] + c["l"] * steps
    if abs(c["g"]) < 1e-9:
        return linear
    return linear * ((1.0 + c["g"]) ** steps)


# ------------------------------------------------------------------- content --

def content():
    """The values in Assets/_Project/Data. Everything here is authored; nothing is
    a placeholder any more -- the tier-3 and tier-4 quests the building curves were
    tuned against exist as assets from Days 10-11."""
    return {
        "startingGold": 150,
        "buildings": {
            "tavern": dict(name="Tavern", maxLevel=90, cost=curve(50, 0, 0.15), effects=[
                (REWARD_YIELD, MUL, curve(0.20, 0, 0.08)),
                (RECRUIT_RARITY, ADD, curve(0, 0.13, 0))]),
            "training_room": dict(name="Training Room", maxLevel=40, cost=curve(45, 0, 0.19), effects=[
                (POWER, ADD, curve(2, 0, 0.14))]),
            "inn": dict(name="Inn", maxLevel=30, cost=curve(40, 0, 0.21), effects=[
                (BEDS, ADD, curve(2, 0.5, 0)),
                (RECOVERY, MUL, curve(0.10, 0, 0.10))]),
        },
        "tiers": [
            dict(id="village", name="Village", order=0, slots=1, maxTier=1, rep=30,
                 req={"tavern": 4, "training_room": 3, "inn": 3}),
            dict(id="town", name="Town", order=1, slots=2, maxTier=2, rep=830,
                 req={"tavern": 20, "training_room": 11, "inn": 9}),
            # Re-derived on Days 10-11: the tier-3 quest pays reputation the Day 8-9
            # model had no asset for, so the guild arrives at this gate holding far
            # more than 28,000 and the threshold stopped confirming anything.
            dict(id="city", name="City", order=2, slots=3, maxTier=3, rep=65000,
                 req={"tavern": 48, "training_room": 26, "inn": 21}),
            dict(id="capital", name="Capital", order=3, slots=4, maxTier=4, rep=0, req={}),
        ],
        # One ladder, generated by a rule rather than five hand-picked sets: each band
        # doubles the archetype's own power, doubles the gold it takes to train that
        # power out, and costs five times the hire. The doubling is what keeps rarity
        # visible next to the Training Room's guild-wide +331 at level 40; the 5x hire
        # is what stops it being a free upgrade.
        #
        # The training bases were 20/60/180/540/1620 until Day 13 -- tripling per band
        # against power that doubled, so a Legendary bed cost 81x a Common bed and
        # returned 16x the power. Rarity was strictly dominated on the gold axis, which
        # is the real reason three separate days concluded that "higher rarities feel
        # pointless". They now scale with what they deliver; see the g/power table under
        # --checks, which is flat by construction and is the thing to look at first if
        # this ladder is ever retuned again.
        "adventurers": {
            "militia_recruit": dict(name="Militia Recruit", rarity=0, minTier=0, cost=25,
                                    power=curve(3, 0.8, 0.05), maxLevel=25,
                                    train=curve(20, 0, 0.34), rest=45),
            "hedge_knight": dict(name="Hedge Knight", rarity=1, minTier=0, cost=120,
                                 power=curve(6, 1.6, 0.05), maxLevel=25,
                                 train=curve(40, 0, 0.34), rest=60),
            "wandering_ranger": dict(name="Wandering Ranger", rarity=2, minTier=1, cost=600,
                                     power=curve(12, 3.2, 0.05), maxLevel=25,
                                     train=curve(80, 0, 0.34), rest=75),
            "arcane_battlemage": dict(name="Arcane Battlemage", rarity=3, minTier=2, cost=3000,
                                      power=curve(24, 6.4, 0.05), maxLevel=25,
                                      train=curve(160, 0, 0.34), rest=90),
            "dragonsworn_champion": dict(name="Dragonsworn Champion", rarity=4, minTier=3, cost=15000,
                                         power=curve(48, 12.8, 0.05), maxLevel=25,
                                         train=curve(320, 0, 0.34), rest=105),
        },
        "quests": {
            "rat_infested_cellar": dict(name="Rat Cellar", tier=1, minTier=0, need=1, rec=4,
                                        dur=45, fail=0.05, gold=48, rep=3),
            "bandit_patrol": dict(name="Bandit Patrol", tier=1, minTier=0, need=2, rec=14,
                                  dur=90, fail=0.12, gold=145, rep=10),
            "ruined_watchtower": dict(name="Ruined Watchtower", tier=2, minTier=1, need=2, rec=45,
                                      dur=150, fail=0.16, gold=375, rep=25),
            "sunken_crypt": dict(name="Sunken Crypt", tier=3, minTier=2, need=3, rec=140,
                                 dur=240, fail=0.18, gold=1800, rep=120),
            # Recommended power is 1250 rather than the 420 the Day 8-9 spec pencilled
            # in. At 420 every party a finished guild can field is past the 4x speed
            # clamp, so the last fifteen Training Room levels and the whole rarity
            # ladder buy nothing measurable: the ceiling sat below where the game ends.
            "dragons_roost": dict(name="Dragon's Roost", tier=4, minTier=3, need=3, rec=1250,
                                  dur=360, fail=0.20, gold=3600, rep=240),
        },
    }


# --------------------------------------------------------------------- world --

class World:
    def __init__(self, c):
        self.c = c
        self.levels = {k: 0 for k in c["buildings"]}
        self.tier = 0
        self.gold = c["startingGold"]
        self.rep = 0.0
        self.roster, self.runs, self.orders = [], [], []
        self.t = 0.0

    def tierdef(self):
        return self.c["tiers"][self.tier]

    def stat(self, s):
        base = NEUTRAL.get(s, 0.0)
        if s == SLOTS:
            base = self.tierdef()["slots"]
        if s == MAXTIER:
            base = self.tierdef()["maxTier"]
        add, mul = base, 0.0
        for bid, lvl in self.levels.items():
            if lvl < 1:
                continue
            for (st, kind, cv) in self.c["buildings"][bid]["effects"]:
                if st != s:
                    continue
                v = ev(cv, lvl)
                if kind == ADD:
                    add += v
                else:
                    mul += v
        return add * (1.0 + mul)

    def power_of(self, m):
        return ev(self.c["adventurers"][m["defId"]]["power"], m["level"]) + self.stat(POWER)

    def rest_of(self, m):
        return max(0.0, self.c["adventurers"][m["defId"]]["rest"] / (self.stat(RECOVERY) or 1.0))

    def duration(self, q, p):
        ratio = max(0.0, p) / max(0.0001, q["rec"])
        return q["dur"] / min(2.0, max(0.5, math.sqrt(ratio)))

    def failure(self, q, p):
        ratio = max(0.0, p) / max(0.0001, q["rec"])
        return min(0.9, max(0.0, q["fail"] * min(2.0, max(0.0, 2.0 - ratio)) - self.stat(FAILRED)))

    def quest_available(self, q):
        return q["minTier"] <= self.tierdef()["order"] and q["tier"] <= math.floor(self.stat(MAXTIER))

    def adv_available(self, a):
        return a["minTier"] <= self.tierdef()["order"] and a["rarity"] <= math.floor(self.stat(RECRUIT_RARITY))

    def potential(self, aid):
        """Power the archetype reaches fully trained. What a hire is actually worth:
        comparing level-1 power says a fresh Champion is weaker than a trained
        Recruit, which is true and beside the point."""
        a = self.c["adventurers"][aid]
        return ev(a["power"], a["maxLevel"])


# -------------------------------------------------------------------- player --
#
# Pacing depends entirely on what the player buys, so the policy is stated rather
# than assumed. This is a sensible-but-not-optimal player:
#   1. advance a tier the moment it is possible; it is free and always good
#   2. staff every quest slot - income before everything else - taking the best
#      archetype affordable right now rather than waiting for a better one
#   3. spend a spare bed only on an upgrade, and wait for the right one
#   4. push whichever tier-gate building is still short
#   5. once beds run out, retire the weakest benched adventurer for a better archetype
#      - paying up front to train the replacement to the first level that beats the
#        incumbent, so the guild is never weaker for having made the swap
#   6. otherwise buy the cheapest thing available
#
# Rule 2's "income before everything else" is not a nicety. With gates first, the
# model poured its 150 starting gold into Inn levels, never bought an adventurer, and
# sat with no way to earn anything. Rule 2's "best affordable" and rule 3 are the
# Days 10-11 replacements for a cheapest-first rule that never once bought a
# non-Common adventurer in a 26-hour game.
#
# PATIENT is the one genuine fork in player behaviour and it is worth reporting both
# ways. A patient player leaves spare beds empty through City because the roster
# screen shows the Dragonsworn Champion greyed out with the reason, and ends the game
# with four of them. An impatient one spends those beds on Battlemages instead.
#
# Day 12 changed what that costs and Day 13 finished the job. Before Day 12 the
# impatient player could never hire a Champion at all - a bed, once filled, was filled
# for the rest of the run. Day 12 freed the bed and the model still reported no
# Legendary, which read as an economic lock replacing a structural one; it was really
# the swap rule refusing to look past level 1, on top of a training ladder that made
# the top band cost 81x a Common for 16x the power.
#
# With both fixed, both profiles finish with the same roster and the fork is a
# schedule rather than a destination: patience costs about eighty minutes and buys
# nothing you cannot buy later. That is what the two arms below now measure - how
# expensive was the detour - and it is the shape Day 12 was reaching for.

PATIENT = True


def desired_roster(w):
    """Staff every slot for the largest quest currently on offer - not the largest we
    can already staff, which deadlocks: with one adventurer only the one-person quest
    counts, so the second is never hired and the better quest never unlocks."""
    needs = [q["need"] for q in w.c["quests"].values() if w.quest_available(q)]
    return int(math.floor(w.stat(SLOTS))) * max(needs) if needs else 1


def purchase(w):
    while True:
        td = w.tierdef()
        if (td["req"] or td["rep"] > 0) and w.rep >= td["rep"] and \
                all(w.levels[b] >= l for b, l in td["req"].items()):
            w.tier += 1
            continue

        beds = math.floor(w.stat(BEDS))
        working = desired_roster(w)
        pool = [aid for aid, a in w.c["adventurers"].items() if w.adv_available(a)]
        reserve = 0.0

        if pool and len(w.roster) < beds:
            if len(w.roster) < working:
                # An unstaffed slot earns nothing, so take the best body affordable
                # now rather than saving for a better one.
                afford = [aid for aid in pool if w.c["adventurers"][aid]["cost"] <= w.gold]
                if afford:
                    aid = max(afford, key=w.potential)
                    w.gold -= w.c["adventurers"][aid]["cost"]
                    w.roster.append(dict(defId=aid, level=1, activity="idle", timer=0.0))
                    continue
            else:
                aid = max(pool, key=w.potential)
                if PATIENT and any(w.potential(k) > w.potential(aid) for k in w.c["adventurers"]):
                    aid = None  # a better archetype exists and is merely locked; wait
                if aid is not None and w.potential(aid) > min(w.potential(m["defId"]) for m in w.roster):
                    if w.gold >= w.c["adventurers"][aid]["cost"]:
                        w.gold -= w.c["adventurers"][aid]["cost"]
                        w.roster.append(dict(defId=aid, level=1, activity="idle", timer=0.0))
                        continue
                    reserve = w.c["adventurers"][aid]["cost"]

        if pool and w.roster and len(w.roster) >= beds:
            # Rule 5, added on Day 12: a full Inn is no longer a wall. The weakest
            # member of the bench can be retired to make room for a better archetype.
            #
            # Only somebody idle and free of standing orders can go, because that is
            # what the service allows, and nothing is refunded.
            #
            # What the swap costs is the question, and Day 12 answered it twice and got
            # it wrong twice in opposite directions. Ranking swaps by fully-trained
            # potential ignores the price of catching up and churns whenever gold is
            # abundant. Requiring a *level-1* replacement to already win compares a
            # fresh hire against a maxed incumbent, which nobody does. Day 13 prices the
            # catch-up instead: see switching_cost.
            best = max(pool, key=w.potential)
            committed = {i for o in w.orders for i in o["party"]}
            benched = [i for i, m in enumerate(w.roster)
                       if i not in committed and m["activity"] == "idle"]
            if benched:
                weakest = min(benched, key=lambda i: w.potential(w.roster[i]["defId"]))
                incumbent = w.roster[weakest]
                if w.potential(best) > w.potential(incumbent["defId"]):
                    switch = switching_cost(w, best, incumbent)
                    if switch is not None and w.gold >= switch[1]:
                        level, price = switch
                        retire(w, weakest)
                        w.gold -= price
                        w.roster.append(dict(defId=best, level=level,
                                             activity="idle", timer=0.0))
                        continue

        spendable = w.gold - reserve
        options = []
        for bid, b in w.c["buildings"].items():
            nxt = w.levels[bid] + 1
            if nxt <= b["maxLevel"]:
                short = w.levels[bid] < td["req"].get(bid, 0)
                options.append((ev(b["cost"], nxt), "gate" if short else "build", bid))
        for i, m in enumerate(w.roster):
            d = w.c["adventurers"][m["defId"]]
            if m["level"] + 1 <= d["maxLevel"]:
                options.append((ev(d["train"], m["level"] + 1), "train", i))

        affordable = [o for o in options if o[0] <= spendable]
        if not affordable:
            return
        gates = [o for o in affordable if o[1] == "gate"]
        cost, kind, key = min(gates or affordable, key=lambda o: o[0])
        w.gold -= cost
        if kind in ("build", "gate"):
            w.levels[key] += 1
        else:
            w.roster[key]["level"] += 1


def switching_cost(w, aid, incumbent):
    """(level, gold) for replacing `incumbent` with a fresh `aid` who is no weaker on
    the day of the swap - the hire, plus training the replacement up to the first level
    that beats what the incumbent has already been trained to. None when no level does.

    This is the third arm of the bracket Day 12 handed to Day 13, and it is the one a
    person actually plays: you do not buy a Champion and field it at level 1 next to
    your maxed Recruit, and you do not throw away a trained roster the instant a better
    archetype unlocks. You buy the Champion and spend the afternoon's gold catching it
    up. Both of Day 12's rules were straw players standing either side of that.

    The level-1 rule missed by 6%, which is the detail worth keeping: at a maxed
    Training Room a level-1 Champion is 379 against a maxed Militia Recruit's 403. Two
    training levels. The "economic lock" that Day 12 said had replaced the structural
    one was a rule that never looked at level 3.

    Charging the catch-up up front rather than level by level is a modelling
    convenience - the real game is a hire and then a run of training taps - but it is
    the same gold, and it keeps the decision atomic so a half-finished swap cannot
    leave the roster weaker than it started.
    """
    a = w.c["adventurers"][aid]
    bar = w.power_of(incumbent)
    gold = a["cost"]
    for level in range(1, a["maxLevel"] + 1):
        if level > 1:
            gold += ev(a["train"], level)
        if w.power_of(dict(defId=aid, level=level)) > bar:
            return level, gold
    return None


def retire(w, index):
    """Let one roster member go, and repair the indices the standing orders hold.

    Orders address their party by roster position, so removing anybody shifts everyone
    after them. The game has no such problem - it addresses adventurers by instance id,
    which is why a card can survive a save being loaded underneath it - and this is the
    model paying for a shortcut taken back when nobody could ever leave the roster."""
    del w.roster[index]
    for o in w.orders:
        o["party"] = [i - 1 if i > index else i for i in o["party"]]


def reform(w):
    """Drop standing orders whose party is beaten by someone on the bench.

    A QuestAssignment holds its party for the life of the order, so in the real game a
    newly hired Champion does nothing at all until the player cancels and re-dispatches.
    That is a genuine requirement on the Day 12 assignment screen, not a modelling
    convenience: without the re-dispatch, the best hire in the game is inert."""
    if not w.orders:
        return
    ranked = sorted(range(len(w.roster)), key=lambda i: -w.power_of(w.roster[i]))
    slots = int(math.floor(w.stat(SLOTS)))
    need = max((w.c["quests"][o["quest"]]["need"] for o in w.orders), default=0)
    wanted = set(ranked[:slots * need])
    for o in list(w.orders):
        if not o["running"] and any(i not in wanted for i in o["party"]):
            w.orders.remove(o)


def best_quest_for(w, party):
    """The best-paying quest this particular party can do. Judged per party, not per
    guild: the weakest three at Capital earn more on the Sunken Crypt than they do
    failing one run of Dragon's Roost in five."""
    best, best_rate = None, -1.0
    power = sum(w.power_of(w.roster[i]) for i in party)
    for qid, q in w.c["quests"].items():
        if not w.quest_available(q) or len(party) < q["need"]:
            continue
        cycle = w.duration(q, power) + max(w.rest_of(w.roster[i]) for i in party)
        rate = (q["gold"] * w.stat(REWARD_YIELD) * (1 - w.failure(q, power))) / max(1e-6, cycle)
        if rate > best_rate:
            best, best_rate = qid, rate
    return best


def sync_and_start(w):
    slots = int(math.floor(w.stat(SLOTS)))
    need = max((q["need"] for q in w.c["quests"].values() if w.quest_available(q)), default=1)
    while len(w.orders) < slots:
        taken = {i for o in w.orders for i in o["party"]}
        free = [i for i, m in enumerate(w.roster) if i not in taken and m["activity"] == "idle"]
        if len(free) < need:
            smaller = [q["need"] for q in w.c["quests"].values()
                       if w.quest_available(q) and q["need"] <= len(free)]
            if not smaller:
                break
            here = max(smaller)
        else:
            here = need
        free.sort(key=lambda i: -w.power_of(w.roster[i]))
        party = free[:here]
        qid = best_quest_for(w, party)
        if qid is None:
            break
        w.orders.append(dict(quest=qid, party=party[:w.c["quests"][qid]["need"]], running=False))

    for o in w.orders:
        if o["running"] or len(w.runs) >= slots:
            continue
        if any(w.roster[i]["activity"] != "idle" for i in o["party"]):
            continue
        qid = best_quest_for(w, o["party"]) or o["quest"]
        if w.c["quests"][qid]["need"] > len(o["party"]):
            qid = o["quest"]
        o["quest"] = qid
        q = w.c["quests"][qid]
        p = sum(w.power_of(w.roster[i]) for i in o["party"])
        y = w.stat(REWARD_YIELD)
        w.runs.append(dict(order=o, remaining=w.duration(q, p), gold=q["gold"] * y,
                           rep=q["rep"] * y, fail=w.failure(q, p)))
        o["running"] = True
        for i in o["party"]:
            w.roster[i]["activity"] = "quest"


def simulate(c, horizon=400 * 3600):
    """Returns (world, {tierId | 'buildings' | 'maxed': seconds}, [purchase timestamps])."""
    w = World(c)
    marks, events, prev = {}, [], None

    def buildings_done():
        return all(w.levels[b] >= c["buildings"][b]["maxLevel"] for b in w.levels)

    def roster_done():
        return bool(w.roster) and all(
            m["level"] >= c["adventurers"][m["defId"]]["maxLevel"] for m in w.roster)

    while w.t < horizon:
        purchase(w)
        reform(w)
        sync_and_start(w)

        cur = (tuple(sorted(w.levels.items())),
               tuple(sorted((m["defId"], m["level"]) for m in w.roster)))
        if cur != prev:
            events.append(w.t)
            prev = cur

        marks.setdefault(w.tierdef()["id"], w.t)
        if buildings_done():
            marks.setdefault("buildings", w.t)
            if roster_done():
                marks.setdefault("maxed", w.t)
                break

        nxt = min([r["remaining"] for r in w.runs] +
                  [m["timer"] for m in w.roster if m["activity"] == "resting"] + [float("inf")])
        if math.isinf(nxt):
            break
        w.t += nxt
        for r in w.runs:
            r["remaining"] -= nxt
        for m in w.roster:
            if m["activity"] == "resting":
                m["timer"] -= nxt
                if m["timer"] <= 0:
                    m["timer"], m["activity"] = 0.0, "idle"
        for r in [r for r in w.runs if r["remaining"] <= 1e-9]:
            w.gold += r["gold"] * (1 - r["fail"])
            w.rep += r["rep"] * (1 - r["fail"])
            for i in r["order"]["party"]:
                w.roster[i]["activity"] = "resting"
                w.roster[i]["timer"] = w.rest_of(w.roster[i])
            r["order"]["running"] = False
            w.runs.remove(r)
    return w, marks, events


def parties(w):
    """Power of each party a finished guild fields, strongest first."""
    ranked = sorted((w.power_of(m) for m in w.roster), reverse=True)
    slots = int(math.floor(w.stat(SLOTS)))
    need = max((q["need"] for q in w.c["quests"].values() if w.quest_available(q)), default=1)
    return [sum(ranked[i * need:(i + 1) * need]) for i in range(slots) if len(ranked) >= (i + 1) * need]


def hms(s):
    if s is None:
        return "never"
    s = int(s)
    return f"{s // 3600}h{s % 3600 // 60:02}m"


def run(c, label):
    w, marks, events = simulate(c)
    gaps = sorted(events[i + 1] - events[i] for i in range(len(events) - 1)) or [0]
    counts = {}
    for m in w.roster:
        counts[RARITY_NAMES[c["adventurers"][m["defId"]]["rarity"]]] = \
            counts.get(RARITY_NAMES[c["adventurers"][m["defId"]]["rarity"]], 0) + 1
    print(f"  {label}")
    print(f"    Village -> Town {hms(marks.get('town')):>7}   Town -> City {hms(marks.get('city')):>7}   "
          f"City -> Capital {hms(marks.get('capital')):>7}")
    print(f"    buildings maxed {hms(marks.get('buildings')):>7}   everything {hms(marks.get('maxed')):>7}   "
          f"purchase decisions {len(events)}")
    print(f"    final roster    {counts}")
    q4 = c["quests"]["dragons_roost"]
    print(f"    parties on Dragon's Roost  " +
          "  ".join(f"{p:,.0f} (x{p / q4['rec']:.2f})" for p in parties(w)))
    return w, marks, events, gaps


if __name__ == "__main__":
    c = content()
    print("Idle Adventurer's Guild - modelled pacing\n")

    profiles = []
    for patient, label in ((True, "patient player: keeps spare beds for the Dragonsworn Champion"),
                           (False, "impatient player: spends them on Battlemages in City, then buys back in")):
        PATIENT = patient
        globals()["PATIENT"] = patient
        profiles.append(run(c, label))
        print()

    inn1 = ev(c["buildings"]["inn"]["cost"], 1)
    hire = c["adventurers"]["militia_recruit"]["cost"]
    print(f"  opening move       Inn L1 {inn1:.0f} + recruit {hire:.0f} = {inn1 + hire:.0f} "
          f"of {c['startingGold']} starting gold "
          f"{'OK' if inn1 + hire <= c['startingGold'] else '-- DEADLOCK, the guild can never earn'}")

    if "--profile" in sys.argv:
        print()
        for (w, marks, events, gaps), name in zip(profiles, ("patient  ", "impatient")):
            print(f"  {name} gap between purchases: median {statistics.median(gaps) / 60:.1f} min, "
                  f"90th pct {gaps[int(len(gaps) * 0.9)] / 60:.0f} min, max {gaps[-1] / 60:.0f} min")
        print("  (a long tail here is the warning sign: it means a stretch with nothing to buy)")

    if "--checks" in sys.argv:
        print("\n  no dead levels - the last step of every curve still moves:")
        for bid, b in c["buildings"].items():
            for (st, kind, cv) in b["effects"]:
                L = b["maxLevel"]
                print(f"    {b['name']:14} L{L - 1}->{L}: {ev(cv, L - 1):9.3f} -> {ev(cv, L):9.3f}")
        for aid, a in c["adventurers"].items():
            L = a["maxLevel"]
            print(f"    {a['name']:22} L{L - 1}->{L}: {ev(a['power'], L - 1):9.1f} -> {ev(a['power'], L):9.1f}")

        print("\n  rarity is a decision - each band against the one below, fully trained:")
        order = sorted(c["adventurers"].items(), key=lambda kv: kv[1]["rarity"])
        guild_bonus = ev(c["buildings"]["training_room"]["effects"][0][2],
                         c["buildings"]["training_room"]["maxLevel"])
        for (lk, lo), (hk, hi) in zip(order, order[1:]):
            lm, hm = ev(lo["power"], lo["maxLevel"]), ev(hi["power"], hi["maxLevel"])
            print(f"    {hi['name']:22} x{hm / lm:.2f} archetype, "
                  f"x{(hm + guild_bonus) / (lm + guild_bonus):.2f} once the Training Room's "
                  f"+{guild_bonus:.0f} is added, for {hi['cost'] / lo['cost']:.0f}x the hire")

        print("\n  a band costs what it delivers - lifetime gold per point of power, per bed:")
        for aid, a in order:
            lifetime = a["cost"] + sum(ev(a["train"], L) for L in range(2, a["maxLevel"] + 1))
            top = ev(a["power"], a["maxLevel"])
            print(f"    {a['name']:22} {top:8.1f} power for {lifetime:>12,.0f} g "
                  f"= {lifetime / top:>7,.0f} g/power")
        print("    (flat by construction: a bed is capped, so rarity is bought for the bed rather")
        print("     than for the gold. A ladder that slopes upward here is rarity taxed twice.)")

        print("\n  rarity unlocks, and which gate actually binds:")
        tav = [e for e in c["buildings"]["tavern"]["effects"] if e[0] == RECRUIT_RARITY][0][2]
        entry = {0: 1, 1: 4, 2: 20, 3: 48}  # Tavern level on arriving at each tier
        for aid, a in order[1:]:
            L = next(l for l in range(1, c["buildings"]["tavern"]["maxLevel"] + 1)
                     if int(ev(tav, l)) >= a["rarity"])
            print(f"    {a['name']:22} Tavern {L:>2} / min tier order {a['minTier']} -> "
                  f"{'Tavern' if L > entry[a['minTier']] else 'tier'} gate binds")

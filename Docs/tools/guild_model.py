#!/usr/bin/env python3
"""
A model of the Idle Adventurer's Guild core loop, for balancing.

Why this exists: the shape of a curve is not visible in the Inspector. A cost curve
and an effect curve that each look reasonable on their own can produce a game that
runs out of things to buy two hours before it runs out of tiers — which is exactly
what the Day 4-5 first-pass numbers did, and it took a simulation rather than a
playthrough to see it.

It replicates the C# exactly: ScalingCurve.Evaluate, GuildState.Aggregate,
QuestResolution's duration / failure / reward, roster rest timers, quest slots and
standing orders. Outcomes are taken at their expected value rather than rolled, so
the same inputs always give the same pacing and a curve change is legible against it.

    python3 guild_model.py            report the current shipping values
    python3 guild_model.py --profile  add the purchase-gap profile

KEEP THIS IN STEP WITH THE ASSETS. It is a model, not a source of truth: if the
.asset files change and this does not, its answers are confidently wrong. The values
below are the ones written on Day 8-9.
"""
import math
import statistics
import sys

# ---------------------------------------------------------------- primitives --

REWARD_YIELD, RECRUIT_RARITY, POWER, BEDS, RECOVERY, SLOTS, MAXTIER, FAILRED = range(8)
ADD, MUL = 0, 1
NEUTRAL = {REWARD_YIELD: 1.0, RECOVERY: 1.0}


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
    """The values in Assets/_Project/Data, plus the two quests Day 10-11 must author."""
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
            dict(id="city", name="City", order=2, slots=3, maxTier=3, rep=28000,
                 req={"tavern": 48, "training_room": 26, "inn": 21}),
            dict(id="capital", name="Capital", order=3, slots=4, maxTier=4, rep=0, req={}),
        ],
        "adventurers": {
            "militia_recruit": dict(name="Militia Recruit", rarity=0, minTier=0, cost=25,
                                    power=curve(3, 2, 0.10), maxLevel=10,
                                    train=curve(20, 0, 0.45), rest=45),
            "hedge_knight": dict(name="Hedge Knight", rarity=1, minTier=0, cost=120,
                                 power=curve(6, 4, 0.10), maxLevel=10,
                                 train=curve(40, 0, 0.45), rest=60),
            "wandering_ranger": dict(name="Wandering Ranger", rarity=2, minTier=1, cost=600,
                                     power=curve(12, 8, 0.12), maxLevel=10,
                                     train=curve(150, 0, 0.45), rest=75),
        },
        "quests": {
            "rat_infested_cellar": dict(name="Rat Cellar", tier=1, minTier=0, need=1, rec=4,
                                        dur=45, fail=0.05, gold=48, rep=3),
            "bandit_patrol": dict(name="Bandit Patrol", tier=1, minTier=0, need=2, rec=14,
                                  dur=90, fail=0.12, gold=145, rep=10),
            "ruined_watchtower": dict(name="Ruined Watchtower", tier=2, minTier=1, need=2, rec=45,
                                      dur=150, fail=0.16, gold=375, rep=25),
            # Not yet authored. The building curves were tuned against these, so if
            # Day 10-11 ships different numbers, re-run this and expect the City and
            # Capital tiers to move.
            "TIER3_SPEC": dict(name="[spec] tier 3", tier=3, minTier=2, need=3, rec=140,
                               dur=240, fail=0.18, gold=1000, rep=67),
            "TIER4_SPEC": dict(name="[spec] tier 4", tier=4, minTier=3, need=3, rec=420,
                               dur=360, fail=0.20, gold=2600, rep=190),
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


# -------------------------------------------------------------------- player --
#
# Pacing depends entirely on what the player buys, so the policy is stated rather
# than assumed. This is a sensible-but-not-optimal player:
#   1. advance a tier the moment it is possible; it is free and always good
#   2. hire when a slot is understaffed - income before everything else
#   3. push whichever tier-gate building is still short
#   4. otherwise buy the cheapest thing available
#
# Rule 2 is not a nicety. With gates first, the model poured its 150 starting gold
# into Inn levels, never bought an adventurer, and sat with no way to earn anything.

def best_quest(w):
    best, best_rate = None, -1.0
    for qid, q in w.c["quests"].items():
        if not w.quest_available(q) or len(w.roster) < q["need"]:
            continue
        power = sum(sorted((w.power_of(m) for m in w.roster), reverse=True)[:q["need"]])
        cycle = w.duration(q, power) + (max(w.rest_of(m) for m in w.roster) if w.roster else 0.0)
        rate = (q["gold"] * w.stat(REWARD_YIELD) * (1 - w.failure(q, power))) / max(1e-6, cycle)
        if rate > best_rate:
            best, best_rate = qid, rate
    return best


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

        options = []
        for bid, b in w.c["buildings"].items():
            nxt = w.levels[bid] + 1
            if nxt <= b["maxLevel"]:
                short = w.levels[bid] < td["req"].get(bid, 0)
                options.append((ev(b["cost"], nxt), "gate" if short else "build", bid))
        if len(w.roster) < min(math.floor(w.stat(BEDS)), desired_roster(w)):
            for aid, a in w.c["adventurers"].items():
                if w.adv_available(a):
                    options.append((a["cost"], "hire", aid))
        for i, m in enumerate(w.roster):
            d = w.c["adventurers"][m["defId"]]
            if m["level"] + 1 <= d["maxLevel"]:
                options.append((ev(d["train"], m["level"] + 1), "train", i))

        affordable = [o for o in options if o[0] <= w.gold]
        if not affordable:
            return
        hires = [o for o in affordable if o[1] == "hire"]
        gates = [o for o in affordable if o[1] == "gate"]
        cost, kind, key = min(hires or gates or affordable, key=lambda o: o[0])
        w.gold -= cost
        if kind in ("build", "gate"):
            w.levels[key] += 1
        elif kind == "hire":
            w.roster.append(dict(defId=key, level=1, activity="idle", timer=0.0))
        else:
            w.roster[key]["level"] += 1


def sync_and_start(w):
    slots = int(math.floor(w.stat(SLOTS)))
    while len(w.orders) < slots:
        qid = best_quest(w)
        if qid is None:
            break
        need = w.c["quests"][qid]["need"]
        taken = {i for o in w.orders for i in o["party"]}
        free = [i for i, m in enumerate(w.roster) if i not in taken and m["activity"] == "idle"]
        if len(free) < need:
            break
        free.sort(key=lambda i: -w.power_of(w.roster[i]))
        w.orders.append(dict(quest=qid, party=free[:need], running=False))

    for o in w.orders:
        if o["running"] or len(w.runs) >= slots:
            continue
        if any(w.roster[i]["activity"] != "idle" for i in o["party"]):
            continue
        q = w.c["quests"][o["quest"]]
        p = sum(w.power_of(w.roster[i]) for i in o["party"])
        y = w.stat(REWARD_YIELD)
        w.runs.append(dict(order=o, remaining=w.duration(q, p), gold=q["gold"] * y,
                           rep=q["rep"] * y, fail=w.failure(q, p)))
        o["running"] = True
        for i in o["party"]:
            w.roster[i]["activity"] = "quest"


def simulate(c, horizon=300 * 3600):
    """Returns (world, {tierId or 'maxed': seconds}, [purchase timestamps])."""
    w = World(c)
    marks, events = {}, []
    prev = None
    while w.t < horizon:
        purchase(w)
        sync_and_start(w)
        cur = (tuple(sorted(w.levels.items())), len(w.roster),
               tuple(m["level"] for m in w.roster))
        if cur != prev:
            events.append(w.t)
            prev = cur
        marks.setdefault(w.tierdef()["id"], w.t)
        if all(w.levels[b] >= c["buildings"][b]["maxLevel"] for b in w.levels):
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


def hms(s):
    if s is None:
        return "never"
    s = int(s)
    return f"{s // 3600}h{s % 3600 // 60:02}m"


if __name__ == "__main__":
    c = content()
    w, marks, events = simulate(c)
    print("Idle Adventurer's Guild - modelled pacing\n")
    print(f"  Village -> Town    {hms(marks.get('town'))}")
    print(f"  Town -> City       {hms(marks.get('city'))}")
    print(f"  City -> Capital    {hms(marks.get('capital'))}")
    print(f"  everything maxed   {hms(marks.get('maxed'))}")
    print(f"\n  purchase decisions {len(events)}")
    print(f"  final levels       {dict(w.levels)}")
    inn1 = ev(c["buildings"]["inn"]["cost"], 1)
    hire = c["adventurers"]["militia_recruit"]["cost"]
    print(f"\n  opening move       Inn L1 {inn1:.0f} + recruit {hire:.0f} = {inn1 + hire:.0f} "
          f"of {c['startingGold']} starting gold "
          f"{'OK' if inn1 + hire <= c['startingGold'] else '-- DEADLOCK, the guild can never earn'}")
    if "--profile" in sys.argv:
        gaps = [events[i + 1] - events[i] for i in range(len(events) - 1)]
        gaps.sort()
        print(f"\n  gap between purchases: median {statistics.median(gaps) / 60:.1f} min, "
              f"90th pct {gaps[int(len(gaps) * 0.9)] / 60:.0f} min, max {gaps[-1] / 60:.0f} min")
        print("  (a long tail here is the warning sign: it means a stretch with nothing to buy)")

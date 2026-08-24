#!/usr/bin/env python3
"""
Auto-tuner for tycoon_model.

Hand-tuning did not converge: about twenty coupled dials, and every fix moved three
other things. Village -> Town went 1h48m, 1h32m, 3h50m, 3h16m, 4h26m, 8h19m across six
passes while each individual change was correct in isolation. That is coordinate descent
by hand at a dimensionality where it does not work.

This scores a run against explicit targets and searches instead. The targets are the
design decisions; the curves are just whatever satisfies them.
"""
import sys, math, random, statistics, time
sys.path.insert(0, '/root/work/tycoon')
import tycoon_model as M

TARGETS = dict(town=5*60, city=90*60, capital=6*3600, maxed=20*3600,
               rooms_share=0.70, gap_p90=600.0, decisions=260)

SPEC = {                       # name: (lo, hi)
    "turns":        (6.0, 40.0),
    "tav_demand":   (20.0, 400.0),
    "inn_frac":     (0.05, 0.6),
    "prov_frac":    (0.1, 1.2),
    "seats_lin":    (0.3, 3.0),
    "spend_growth": (0.035, 0.13),
    "cost_growth":  (0.11, 0.24),
    "market_step":  (3.0, 12.0),
    "contract_step":(1.6, 7.0),
    "staff_scale":  (0.25, 6.0),
    "staff_cost":   (0.2, 6.0),
    "rep_town":     (150.0, 4000.0),
    "rep_city":     (8000.0, 150000.0),
    "base_service": (5.0, 60.0),
    "wage_share":   (0.08, 0.40),
    "gate_scale":   (0.5, 1.8),
    # Tree length was fixed by hand at 60/50/40/55/50 and never questioned, and "maxed:
    # never" has been in every single run since. Days 8-9 hit exactly this -- a uniform
    # 40-level tree at 34% growth put the top eleven levels out of reach, not expensive
    # but unreachable. The lengths are a dial like any other.
    "tree_scale":   (0.35, 1.15),
    "earner_tree":  (0.8, 2.2),
    # The first adventurer cannot arrive sooner than this, and Village cannot end before
    # the first adventurer -- so it is a hard floor under the tutorial length and was
    # sitting at four minutes without ever being questioned.
    "arrival_base": (25.0, 240.0),
    "quest_dur":    (0.35, 1.4),
}

def build(p):
    c = M.content()
    M.World.MAX_TURNS_PER_HOUR = p["turns"]
    M.World.WAGE_SHARE = p["wage_share"]

    r = c["rooms"]
    r["tavern"]["baseDemand"] = p["tav_demand"]
    r["inn"]["baseDemand"] = p["tav_demand"] * p["inn_frac"]
    r["provisioner"]["baseDemand"] = p["tav_demand"] * p["prov_frac"]
    for rid in ("tavern", "inn", "provisioner"):
        r[rid]["seats"]["l"] = p["seats_lin"] * (1.0 if rid == "tavern" else 0.6)
        r[rid]["spend"]["g"] = p["spend_growth"]
    for rid in r:
        r[rid]["cost"]["g"] = p["cost_growth"]
        # Only the rooms whose SPEND compounds can carry a long tree; the support rooms
        # buy bounded benefits and a geometric price for those is what puts a ceiling
        # out of reach. Days 8-9 in one line.
        earner = r[rid].get("baseDemand", 0.0) > 0.0
        stretch = p["tree_scale"] * (p["earner_tree"] if earner else 1.0)
        r[rid]["maxLevel"] = max(8, int(round(r[rid]["maxLevel"] * stretch)))

    m, k = p["market_step"], p["contract_step"]
    for i, tier in enumerate(c["tiers"]):
        tier["marketSize"] = m ** i
        tier["contractScale"] = k ** i
        tier["baseService"] = p["base_service"]
        tier["req"] = {b: max(1, int(round(l * p["gate_scale"]))) for b, l in tier["req"].items()}
    c["arrivalSeconds"]["b"] = p["arrival_base"]
    c["arrivalSeconds"]["l"] = -p["arrival_base"] / 75.0
    for q in c["quests"].values():
        q["dur"] = q["dur"] * p["quest_dur"]
    c["tiers"][1]["rep"] = p["rep_town"]
    c["tiers"][2]["rep"] = p["rep_city"]

    for i, s in enumerate(c["staff"]):
        s["service"] = s["service"] * p["staff_scale"] * (m ** i) / (9.0 ** i)
        s["hire"] = s["hire"] * p["staff_cost"]
    return c

def evaluate(p, step=60.0, horizon=45*3600):
    try:
        c = build(p)
        w, marks, events, arr = M.simulate(c, horizon, step)
    except Exception:
        return None
    tot = w.grossEarned + w.questGold
    gaps = sorted(events[i+1]-events[i] for i in range(len(events)-1)) or [1e9]
    return dict(town=marks.get("town"), city=marks.get("city"), capital=marks.get("capital"),
                maxed=marks.get("maxed"), rooms_share=w.grossEarned/max(1.0, tot),
                gap_p90=gaps[int(len(gaps)*0.9)], decisions=len(events), arrivals=arr)

def loss(mt):
    if mt is None: return 1e9
    L = 0.0
    for k, wgt in (("town", 3.0), ("city", 1.0), ("capital", 2.0), ("maxed", 2.5)):
        v = mt[k]
        if v is None:
            L += wgt * 25.0                      # never reached: heavy, not infinite
        else:
            L += wgt * abs(math.log(max(1.0, v) / TARGETS[k])) ** 2
    # The 70/30 split is a DESIGN REQUIREMENT, not a nice-to-have: the game is about
    # building rooms, and the first search happily returned configurations where rooms
    # earned 1% of lifetime income because the time targets outweighed it. Heavily
    # weighted, with a cliff below half so those solutions are never competitive.
    share = mt["rooms_share"]
    L += 60.0 * (share - TARGETS["rooms_share"]) ** 2
    if share < 0.5:
        L += 40.0 * (0.5 - share)
    if mt["gap_p90"] > TARGETS["gap_p90"]:
        L += 1.5 * abs(math.log(mt["gap_p90"] / TARGETS["gap_p90"])) ** 2
    if mt["decisions"] < TARGETS["decisions"]:
        L += 1.0 * (1.0 - mt["decisions"] / TARGETS["decisions"]) ** 2 * 4
    return L

def rand_p(rng):
    return {k: math.exp(rng.uniform(math.log(lo), math.log(hi))) for k, (lo, hi) in SPEC.items()}

def report(p, mt, tag=""):
    print(f"  {tag}Town {M.hms(mt['town']):>7} City {M.hms(mt['city']):>7} "
          f"Cap {M.hms(mt['capital']):>7} maxed {M.hms(mt['maxed']):>7} "
          f"rooms {100*mt['rooms_share']:>3.0f}% gapP90 {mt['gap_p90']/60:>4.0f}m "
          f"dec {mt['decisions']:>3} loss {loss(mt):.2f}")


def search(seconds=100, seed=0, start=None):
    rng = random.Random(seed)
    t0 = time.time()
    best_p = start or rand_p(rng)
    best_mt = evaluate(best_p); best_L = loss(best_mt)
    n = 0
    # phase 1: random scatter to find a basin
    scatter = 0.0 if start else 0.45
    while time.time() - t0 < seconds * scatter:
        p = rand_p(rng); mt = evaluate(p); L = loss(mt); n += 1
        if L < best_L:
            best_p, best_mt, best_L = p, mt, L
    # phase 2: coordinate refinement with shrinking steps
    scale = 0.55
    while time.time() - t0 < seconds:
        improved = False
        for k, (lo, hi) in SPEC.items():
            for direction in (1.0, -1.0):
                q = dict(best_p)
                q[k] = min(hi, max(lo, best_p[k] * math.exp(direction * scale)))
                mt = evaluate(q); L = loss(mt); n += 1
                if L < best_L:
                    best_p, best_mt, best_L = q, mt, L
                    improved = True
            if time.time() - t0 > seconds:
                break
        if not improved:
            scale *= 0.6
            if scale < 0.02:
                break
    return best_p, best_mt, best_L, n


if __name__ == "__main__":
    import json, pathlib as _pl
    budget = float(sys.argv[1]) if len(sys.argv) > 1 else 100
    seeds = [int(s) for s in (sys.argv[2].split(",") if len(sys.argv) > 2 else ["1"])]
    resume = _pl.Path("best_params.json")
    start = json.loads(resume.read_text()) if (resume.exists() and "--resume" in sys.argv) else None
    if start:   # a saved point predates any dial added since; fill the gaps at midpoint
        for k, (lo, hi) in SPEC.items():
            start.setdefault(k, math.exp((math.log(lo) + math.log(hi)) / 2))
    overall = None
    if start:
        mt0 = evaluate(start); overall = (start, mt0, loss(mt0))
        report(start, mt0, tag="resumed  ")
    for s in seeds:
        p, mt, L, n = search(budget / len(seeds), seed=s, start=start)
        print(f"seed {s}: {n} evals")
        report(p, mt, tag=f"seed {s}  ")
        if overall is None or L < overall[2]:
            overall = (p, mt, L)
    print("\nBEST:")
    report(overall[0], overall[1])
    print("\nparams:")
    for k in SPEC:
        print(f"  {k:14} {overall[0][k]:.4f}")
    import json, pathlib
    pathlib.Path("best_params.json").write_text(json.dumps(overall[0], indent=1))

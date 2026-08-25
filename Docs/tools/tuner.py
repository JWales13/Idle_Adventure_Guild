#!/usr/bin/env python3
"""
Auto-tuner for tycoon_model.

Hand-tuning did not converge: about twenty coupled dials, and every fix moved three
other things. Village -> Town went 1h48m, 1h32m, 3h50m, 3h16m, 4h26m, 8h19m across six
passes while each individual change was correct in isolation. That is coordinate descent
by hand at a dimensionality where it does not work.

This scores a run against explicit targets and searches instead. The targets are the
design decisions; the curves are just whatever satisfies them.

    python3 tuner.py 300 1,2,3            search from scratch, 300s across three seeds
    python3 tuner.py 300 1,2,3 --resume   start from tuned_params.json
    python3 tuner.py 0 --report           just score and trace what is checked in
    python3 tuner.py 300 1 --promote      write the winner to tuned_params.json

Two things changed on Day 15, and both were forced by what the model turned out to be
doing rather than by a wish for a better search.

FIRST, THE LOSS SCORES THE OPENING RATHER THAN THE TIER BOUNDARY. Village running 30
modelled minutes was never the problem. The first-session trace was: tavern and front
desk built instantly, an adventurer in the crowd immediately, and then nothing at all
for twenty-two minutes -- eleven more adventurers walking past a guild that could not
afford any of them. A tier time cannot see that, and the old loss weighted `town` at 3.0
against a five-minute target, so it was pushing hard on the one number that was already
fine. `w.beats` and `w.pulse` in the model record the beats; §"the opening" below is now
the heaviest block in the loss.

SECOND, EVERY SCORE IS THE MEDIAN OF THREE INTEGRATION STEPS. The old loss scored one
run at step=60, and the model's answers moved by about 2x with the step: the celebrated
"68% of lifetime income from rooms" was 82% at step 5, and at step 30 the Capital was
never reached at all. The tuner was therefore optimising a measurement artefact, and a
configuration that only holds at one step size is not a configuration. Scoring the
median across (10, 30, 60) and penalising the spread makes the search prefer designs
whose pacing does not sit on a knife-edge -- which is a property worth having in the
shipped game too, since a real player is a much noisier clock than any of these.
"""
import sys, os, math, random, statistics, time, json, pathlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import tycoon_model as M

HERE = pathlib.Path(__file__).resolve().parent
TUNED = HERE / "tuned_params.json"      # the checked-in record; only --promote writes it
WORKING = HERE / "best_params.json"     # scratch, written every run

# ---------------------------------------------------------------------- targets --
# Modelled seconds. Day 14's playthrough took 17.6 real minutes to reach Town against a
# predicted 8, so a lived minute is roughly 2.2 modelled ones on the single noisy sample
# this project has. The opening targets below are set in MODELLED time with that ratio
# in mind -- "first contract at 210s" is aiming at about four lived minutes.
TARGETS = dict(
    # the opening, in order of the beats a first session should actually have
    beat2=60.0,          # the first thing that happens AFTER the opening burst
    adventurer=150.0,    # somebody you can afford walks in
    contract=210.0,      # and goes out on a job
    staff=420.0,         # the first hire that is not an adventurer
    open_gap=120.0,      # longest stretch with nothing to BUY, first 20 minutes
    open_burst=2,        # purchases at t=0; starting gold should not buy the tutorial
    # the long arc
    town=(20 * 60.0, 30 * 60.0),   # a BAND -- see loss()
    city=90 * 60,
    capital=6 * 3600,
    maxed=20 * 3600,
    rooms_share=0.70,
    gap_p90=600.0,
    decisions=260,
)

OPEN_WINDOW = 20 * 60    # the stretch the opening score looks at
STEPS = (10.0, 30.0, 60.0)
QUICK = (60.0,)          # the cheap screen; see `score()`
HORIZON = 30 * 3600

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
    # Floor raised from 0.5 on Day 15. At 0.5 the Village gate is tavern 2 / front_desk 1,
    # which starting gold clears in the first instant -- and the search chose exactly that
    # in every winner across nine seeds, because a gate that is already satisfied cannot
    # cost the loss anything. That is the tuner making a design decision it does not get
    # to make: §6C's finding #7 is that the player never saves for a gate and the tier
    # panel exists to fix it, which presupposes there is a gate to save for.
    "gate_scale":   (0.9, 1.8),
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

    # ---- the opening, added Day 15 -------------------------------------------------
    # These four are the whole reason the search kept returning to the same basin. The
    # dead twenty-two minutes was never a curve, it was a subtraction: starting gold 150,
    # Tavern L1+L2 and Front Desk L1 costing 143.85 of it, leaving 6.15 gold against a
    # 40-gold next purchase at 1.54 gold a minute. That is 21.9 minutes and it is exactly
    # what the trace showed. Every number in that sentence was hardcoded in `content()`
    # and absent from SPEC, so no amount of scatter or shrinking step size could reach
    # it -- the tuner was not stuck in a basin, it was searching a space that did not
    # contain the problem.
    # Staff are meant to be "the game's small, frequent purchase" -- the thing that
    # replaces the ~300 individual training purchases the revision cut, which Days 10-11
    # recorded as what filled every stretch where only an expensive room level was on
    # offer. How many of them exist early is set by the Tavern's staffSlots curve, and
    # that curve was hardcoded at base 2 / +1.4 per level and never questioned. So the
    # opening had at most two cheap purchases available in it no matter what else moved,
    # which is why the worst silence sat at six minutes across every seed and would not
    # come down: the search had nothing left to trade.
    "slots_base":   (1.0, 8.0),
    "slots_lin":    (0.3, 4.0),

    "start_gold":   (60.0, 1500.0),
    "open_cost":    (0.25, 3.0),    # cost base of the two rooms Village opens with
    "late_cost":    (0.2, 4.0),     # cost base of Barracks, Inn, Provisioner
    "hire_base":    (0.25, 4.0),    # the adventurer hire ladder
    "rep_village":  (4.0, 400.0),   # what ends the tutorial
}


def build(p):
    c = M.content()
    M.World.MAX_TURNS_PER_HOUR = p["turns"]
    M.World.WAGE_SHARE = p["wage_share"]
    c["startingGold"] = p["start_gold"]

    r = c["rooms"]
    r["tavern"]["baseDemand"] = p["tav_demand"]
    r["tavern"]["staffSlots"]["b"] = p["slots_base"]
    r["tavern"]["staffSlots"]["l"] = p["slots_lin"]
    r["inn"]["baseDemand"] = p["tav_demand"] * p["inn_frac"]
    r["provisioner"]["baseDemand"] = p["tav_demand"] * p["prov_frac"]
    for rid in ("tavern", "inn", "provisioner"):
        r[rid]["seats"]["l"] = p["seats_lin"] * (1.0 if rid == "tavern" else 0.6)
        r[rid]["spend"]["g"] = p["spend_growth"]
    for rid in r:
        r[rid]["cost"]["g"] = p["cost_growth"]
        r[rid]["cost"]["b"] *= p["open_cost"] if rid in ("tavern", "front_desk") else p["late_cost"]
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
    c["tiers"][0]["rep"] = p["rep_village"]
    c["tiers"][1]["rep"] = p["rep_town"]
    c["tiers"][2]["rep"] = p["rep_city"]

    for a in c["adventurers"].values():
        a["hire"] = a["hire"] * p["hire_base"]
    for i, s in enumerate(c["staff"]):
        s["service"] = s["service"] * p["staff_scale"] * (m ** i) / (9.0 ** i)
        s["hire"] = s["hire"] * p["staff_cost"]
    return c


# --------------------------------------------------------------------- measuring --
# A run that has not reached the Capital by three times its target is already scoring
# terribly and does not get to spend the rest of the horizon proving it. Most of the
# search budget was going into exactly these: a configuration that never finishes is the
# one that simulates every second of the horizon at the finest step.
GIVE_UP = 3.0 * TARGETS["capital"]

def measure(c, step, horizon=HORIZON):
    """One run, reduced to the numbers the loss cares about."""
    w, marks, events, arr = M.simulate(c, horizon, step, abortAfter=GIVE_UP)
    tot = w.grossEarned + w.questGold
    gaps = sorted(events[i + 1] - events[i] for i in range(len(events) - 1)) or [1e9]

    moments = sorted(set(w.pulse))    # decisions only, and the opening burst is one moment
    early = [t for t in moments if t <= OPEN_WINDOW]
    open_gap = max((early[i + 1] - early[i] for i in range(len(early) - 1)),
                   default=OPEN_WINDOW)
    if len(early) > 1 and early[-1] < OPEN_WINDOW:
        open_gap = max(open_gap, OPEN_WINDOW - early[-1])   # a quiet tail still counts

    return dict(town=marks.get("town"), city=marks.get("city"),
                capital=marks.get("capital"), maxed=marks.get("maxed"),
                rooms_share=w.grossEarned / max(1.0, tot),
                gap_p90=gaps[int(len(gaps) * 0.9)], decisions=len(events), arrivals=arr,
                beat2=moments[1] if len(moments) > 1 else None,
                adventurer=w.beats["adventurer"], contract=w.beats["contract"],
                staff=w.beats["staff"], open_gap=open_gap,
                open_burst=sum(1 for t in w.pulse if t <= 0.0))


def evaluate(p, steps=STEPS, horizon=HORIZON):
    """Median across integration steps, plus how far apart they were.

    A configuration whose pacing depends on how finely the clock is sliced is not a
    configuration, it is a coincidence. Scoring the median makes the search prefer the
    ones that survive; `spread` gives it a reason to care."""
    try:
        c_by_step = [measure(build(p), s, horizon) for s in steps]
    except Exception:
        return None

    def med(k, missing):
        vals = [(m[k] if m[k] is not None else missing) for m in c_by_step]
        return statistics.median(vals)

    out = {}
    for k in ("town", "city", "capital", "maxed", "beat2", "adventurer", "contract", "staff"):
        out[k] = med(k, horizon * 2.0)
        out[k + "_never"] = sum(1 for m in c_by_step if m[k] is None)
    for k in ("rooms_share", "gap_p90", "decisions", "arrivals", "open_gap", "open_burst"):
        out[k] = med(k, 0.0)
    caps = [m["capital"] if m["capital"] is not None else horizon * 2.0 for m in c_by_step]
    shares = [m["rooms_share"] for m in c_by_step]
    out["spread"] = max(
        (max(caps) - min(caps)) / max(1.0, statistics.median(caps)),
        (max(shares) - min(shares)) / max(0.05, statistics.median(shares)))
    return out


# ------------------------------------------------------------------------- loss --
def loss(mt):
    if mt is None:
        return 1e9
    L = 0.0

    # ---- the opening ------------------------------------------------------------
    # The heaviest block, deliberately. This is what Day 15 was handed: not "Village is
    # too long" but "the first twenty-two minutes are empty". Four beats and a silence.
    for k, wgt in (("beat2", 4.0), ("adventurer", 4.0), ("contract", 4.0), ("staff", 2.5)):
        v = mt[k]
        if mt[k + "_never"]:
            L += wgt * 30.0
        else:
            # Late is much worse than early: a beat arriving sooner than the target is a
            # busy opening, which is the thing being asked for. Asymmetric on purpose.
            e = math.log(max(1.0, v) / TARGETS[k])
            L += wgt * (e ** 2 if e > 0 else 0.25 * e ** 2)
    # The silence itself, scored directly rather than inferred from the beats.
    L += 7.0 * max(0.0, math.log(max(1.0, mt["open_gap"]) / TARGETS["open_gap"])) ** 2
    # The lump. Starting gold that buys seven purchases in the first instant hands the
    # player a finished tutorial and no first decision, and it is the failure the beat
    # timings are least able to see: every beat then reads as gloriously early, which
    # the asymmetry above deliberately does not punish. Weighted heavily because "the
    # opening is a sequence of distinct moments, not a lump" is the actual design
    # requirement and this is the only term that measures it.
    L += 6.0 * max(0.0, mt["open_burst"] - TARGETS["open_burst"]) ** 2

    # ---- the long arc -----------------------------------------------------------
    # `town` is a BAND, not a point. The old loss weighted it at 3.0 against a
    # five-minute target -- the single largest term in the function, pushing hard on a
    # number that had never been the complaint, and aiming at a figure the design could
    # not reach anyway. Twenty to thirty modelled minutes is roughly forty-five to
    # seventy LIVED minutes at Day 14's 2.2x, which is a first tier a person finishes in
    # one or two sittings. Anywhere inside the band is free; outside it costs.
    lo, hi = TARGETS["town"]
    if mt["town_never"]:
        L += 20.0
    elif mt["town"] > hi:
        L += 2.5 * math.log(mt["town"] / hi) ** 2
    elif mt["town"] < lo:
        L += 2.5 * math.log(lo / max(1.0, mt["town"])) ** 2
    for k, wgt in (("city", 0.75), ("capital", 1.5), ("maxed", 1.5)):
        if mt[k + "_never"]:
            L += wgt * 25.0
        else:
            L += wgt * abs(math.log(max(1.0, mt[k]) / TARGETS[k])) ** 2

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

    # ---- and it has to mean the same thing twice --------------------------------
    L += 3.0 * mt["spread"] ** 2
    return L


# ----------------------------------------------------------------------- search --
def rand_p(rng):
    return {k: math.exp(rng.uniform(math.log(lo), math.log(hi))) for k, (lo, hi) in SPEC.items()}

def report(mt, tag=""):
    print(f"  {tag}beats: 2nd {M.hms(mt['beat2']):>6} adv {M.hms(mt['adventurer']):>6} "
          f"contract {M.hms(mt['contract']):>6} staff {M.hms(mt['staff']):>6} "
          f"| worst silence {mt['open_gap']/60:4.1f}m  burst {mt['open_burst']:.0f}")
    print(f"  {' ' * len(tag)}arc:   Town {M.hms(mt['town']):>6} City {M.hms(mt['city']):>6} "
          f"Cap {M.hms(mt['capital']):>6} maxed {M.hms(mt['maxed']):>6} "
          f"| rooms {100*mt['rooms_share']:3.0f}% gapP90 {mt['gap_p90']/60:3.0f}m "
          f"dec {mt['decisions']:.0f} spread {mt['spread']:.2f} loss {loss(mt):.2f}")

def score(p, best_L):
    """Screen cheaply, then pay for the real answer only if it might win.

    The robust loss runs three simulations, and the slowest configurations are precisely
    the ones that never finish and so run the whole horizon at the finest step. The first
    Day 15 search managed twenty-four evaluations in five minutes, which is not a search.
    A single step-60 run costs about a fifth of the full score and orders candidates
    well enough to reject the obvious losers; anything within reach of the incumbent is
    then re-scored properly, so nothing is ever ACCEPTED on the cheap number."""
    quick = evaluate(p, steps=QUICK)
    if quick is None:
        return None, 1e9
    if loss(quick) > best_L * 1.4 + 3.0:
        return quick, 1e9          # not close; do not pay for the fine steps
    mt = evaluate(p)
    return mt, loss(mt)

def search(seconds=100, seed=0, start=None):
    rng = random.Random(seed)
    t0 = time.time()
    n = 0
    best_p = start
    if best_p is None:
        # phase 1: random scatter, on the CHEAP score only. Its job is to find a basin,
        # and a basin does not need three decimal places. Paying the robust price here
        # spent the whole budget confirming that random configurations are bad -- the
        # first run of this loss managed twenty-four evaluations in five minutes.
        bestQ = float("inf")
        while time.time() - t0 < seconds * 0.40:
            p = rand_p(rng)
            q = evaluate(p, steps=QUICK)
            L = loss(q) if q is not None else 1e9
            n += 1
            if L < bestQ:
                best_p, bestQ = p, L
        if best_p is None:
            best_p = rand_p(rng)
    best_mt = evaluate(best_p); best_L = loss(best_mt)
    # phase 2: coordinate refinement with shrinking steps. The dial order is shuffled
    # per pass -- a fixed order makes the first dials in SPEC systematically more likely
    # to claim an improvement that several of them could have made, which is one way a
    # search sits in a basin that is really an artefact of the iteration order.
    scale = 0.55
    keys = list(SPEC)
    while time.time() - t0 < seconds:
        improved = False
        rng.shuffle(keys)
        for k in keys:
            lo, hi = SPEC[k]
            for direction in (1.0, -1.0):
                q = dict(best_p)
                q[k] = min(hi, max(lo, best_p[k] * math.exp(direction * scale)))
                if q[k] == best_p[k]:
                    continue
                mt, L = score(q, best_L); n += 1
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


def fill(p):
    """A saved point predates any dial added since; fill the gaps at the midpoint.

    It is also CLAMPED into the current bounds, which is not housekeeping. Tightening a
    bound is how a design decision gets taken away from the search -- gate_scale's floor
    went 0.5 -> 0.9 on Day 15 precisely so the tuner could not keep halving every tier
    gate. Resuming without clamping smuggles the old value straight back in: coordinate
    refinement only replaces a dial when a perturbation scores better, and a perturbation
    is clamped while the incumbent is not, so an out-of-bounds start survives the entire
    search and ships. The first promoted configuration of the day had gate_scale 0.5 in it
    for exactly this reason, hours after the floor was raised to forbid it."""
    for k, (lo, hi) in SPEC.items():
        p[k] = min(hi, max(lo, p.get(k, math.exp((math.log(lo) + math.log(hi)) / 2))))
    return p


def trace(p, minutes=25.0, step=10.0):
    """The opening as a player would live it. This is the artefact the loss is a
    summary of, and it is worth reading rather than trusting the number."""
    log = []
    w, marks, events, arr = M.simulate(build(p), minutes * 60, step, log=log)
    last = 0.0
    for t, kind, label in log:
        quiet = ""
        if kind != "arrival":
            if t - last > TARGETS["open_gap"]:
                quiet = f"      <-- {(t - last)/60:.0f} min of nothing"
            last = t
        print(f"  {int(t)//60:3}m{int(t)%60:02}s  {'.' if kind == 'arrival' else '*'} {label}{quiet}")


if __name__ == "__main__":
    budget = float(sys.argv[1]) if len(sys.argv) > 1 else 100
    seeds = [int(s) for s in (sys.argv[2].split(",")
                              if len(sys.argv) > 2 and not sys.argv[2].startswith("-")
                              else ["1"])]
    src = TUNED if TUNED.exists() else WORKING
    start = fill(json.loads(src.read_text())) if ("--resume" in sys.argv and src.exists()) else None
    if start is not None:
        print("start clamped into current bounds")

    overall = None
    if start:
        mt0 = evaluate(start); overall = (start, mt0, loss(mt0))
        print(f"resumed from {src.name}:")
        report(mt0)
    for s in seeds:
        if budget <= 0:
            break
        p, mt, L, n = search(budget / len(seeds), seed=s, start=start)
        print(f"\nseed {s}: {n} evals")
        report(mt)
        if overall is None or L < overall[2]:
            overall = (p, mt, L)

    if overall is None:
        sys.exit("nothing to report -- pass a budget, or --resume with a params file")

    print("\nBEST:")
    report(overall[1])
    print("\nparams:")
    for k in SPEC:
        print(f"  {k:14} {overall[0][k]:.4f}")

    WORKING.write_text(json.dumps(overall[0], indent=1))
    if "--promote" in sys.argv:
        TUNED.write_text(json.dumps(overall[0], indent=1))
        print(f"\npromoted to {TUNED.name}")
    else:
        print(f"\nwritten to {WORKING.name} -- pass --promote to replace {TUNED.name}")

    if "--report" in sys.argv:
        print("\nthe opening, at step 10:")
        trace(overall[0])
        print("\nper-step, to show the answer means the same thing at any clock:")
        print("  step    Town    City     Cap   maxed  rooms%")
        for st in (5.0, 10.0, 20.0, 30.0, 60.0):
            m = measure(build(overall[0]), st)
            print(f"  {st:5.0f} {M.hms(m['town']):>7} {M.hms(m['city']):>7} "
                  f"{M.hms(m['capital']):>7} {M.hms(m['maxed']):>7} {100*m['rooms_share']:6.0f}")

"""Read the shipped .asset YAML back and re-derive every claim made about it.

Days 4-5 shipped four transcription slips out of fourteen assets and the one that
mattered handed the Inn its own cost curve as its bed curve. Reading the YAML back
rather than the table it was written from is the only check that could have caught it.
"""
import glob, os, sys, math, yaml

DATA = os.path.expanduser("~/mnt/Idle_Adventure_Guild/Assets/_Project/Data")
STAT = {0:"RewardYield",1:"RecruitableRarity",2:"AdventurerPower",3:"HousingCapacity",
        4:"RecoverySpeed",5:"QuestSlots",6:"MaxQuestTier",7:"FailureRateReduction",
        8:"ServiceSeats",9:"CustomerSpend",10:"ServiceDemand",11:"StaffSlots",12:"ContractCommission"}
PER_ROOM = {"ServiceSeats","CustomerSpend","ServiceDemand"}

def load(path):
    txt = open(path).read().split("---", 1)[1].split("\n", 1)[1]
    return yaml.safe_load(txt)["MonoBehaviour"]

def ev(c, level):
    if level < 1: return 0.0
    s = level - 1
    lin = c["BaseValue"] + c["LinearPerLevel"] * s
    g = c["GrowthPerLevel"]
    return lin if abs(g) < 1e-9 else lin * ((1 + g) ** s)

def effect_at(room, stat, level):
    """BuildingDefinition.EffectAt: additive terms scaled by the multiplicative bonus.

    NOTE, and it caught this script out: a room carrying ONLY a multiplicative effect
    reads ZERO here, because there is no additive term for the bonus to scale. That is
    correct — a bonus is a bonus ON something — and it is why Reward Yield and Recovery
    Speed have to be read through the guild-wide seam, which starts from a neutral 1.0.
    """
    if level < 1: return 0.0
    add = 0.0; mul = 0.0
    for e in room["_effects"]:
        if STAT[e["Stat"]] != stat: continue
        v = ev(e["ValuePerLevel"], level)
        if e["Kind"] == 0: add += v
        else: mul += v
    return add * (1.0 + mul)

def bonus_at(room, stat, level):
    """The multiplicative bonus fraction a room contributes, as GuildState reads it."""
    total = 0.0
    for e in room["_effects"]:
        if STAT[e["Stat"]] == stat and e["Kind"] == 1:
            total += ev(e["ValuePerLevel"], level)
    return total

def produces(room, stat):
    return any(STAT[e["Stat"]] == stat for e in room["_effects"])

rooms = {}
for f in sorted(glob.glob(os.path.join(DATA, "Buildings", "*.asset"))):
    d = load(f)
    if d["_id"] == "training_room":      # retired; awaiting deletion in the Project window
        continue
    rooms[d["_id"]] = d
tiers = sorted((load(f) for f in glob.glob(os.path.join(DATA, "Tiers", "*.asset"))),
               key=lambda t: t["_order"])
content = load(os.path.join(DATA, "GameContent.asset"))
TURNS, START_GOLD = 40.0, content["_startingGold"]

fails = []
def check(ok, msg):
    print(("  ok   " if ok else "  FAIL ") + msg)
    if not ok: fails.append(msg)

print("\n== every effect improves at max level, except the one that is flat by design")
for rid, r in rooms.items():
    for e in r["_effects"]:
        stat = STAT[e["Stat"]]
        if stat == "ServiceDemand": continue
        last, prev = ev(e["ValuePerLevel"], r["_maxLevel"]), ev(e["ValuePerLevel"], r["_maxLevel"]-1)
        check(last > prev, f"{rid} {stat} L{r['_maxLevel']-1}->{r['_maxLevel']}: {prev:.4f} -> {last:.4f}")

print("\n== costs rise every level")
for rid, r in rooms.items():
    rising = all(ev(r["_costToReachLevel"], l) > ev(r["_costToReachLevel"], l-1)
                 for l in range(2, r["_maxLevel"]+1))
    check(rising, f"{rid} cost L1 {ev(r['_costToReachLevel'],1):,.2f} -> L{r['_maxLevel']} {ev(r['_costToReachLevel'],r['_maxLevel']):,.0f}")

print("\n== the three levers: all or none, seats and spend rise, demand does not")
for rid, r in rooms.items():
    has = [produces(r, s) for s in ("ServiceSeats","CustomerSpend","ServiceDemand")]
    if not any(has): 
        print(f"  --   {rid} does not trade"); continue
    check(all(has), f"{rid} carries all three levers")
    check(abs(effect_at(r,"ServiceDemand",r["_maxLevel"]) - effect_at(r,"ServiceDemand",1)) < 1e-4,
          f"{rid} demand flat at {effect_at(r,'ServiceDemand',1):,.2f}/hr")
    check(effect_at(r,"ServiceSeats",r["_maxLevel"]) > effect_at(r,"ServiceSeats",1), f"{rid} seats rise")
    check(effect_at(r,"CustomerSpend",r["_maxLevel"]) > effect_at(r,"CustomerSpend",1), f"{rid} spend rises")

print("\n== per-room stats are produced by rooms only, and nothing produces a commission")
for rid, r in rooms.items():
    check(not produces(r,"ContractCommission"), f"{rid} produces no commission")
for t in tiers:
    check(abs(t.get("_contractRewardScale",1.0)-1.0) < 1e-6, f"{t['_id']} contract reward scale is neutral")

print("\n== the settlement")
check(abs(tiers[0]["_marketSize"]-1.0) < 1e-6, "Village market size is the unit (1)")
for i in range(1, len(tiers)):
    check(tiers[i]["_marketSize"] > tiers[i-1]["_marketSize"],
          f"{tiers[i]['_id']} market {tiers[i]['_marketSize']:,.3f} > {tiers[i-1]['_id']} {tiers[i-1]['_marketSize']:,.3f}")
for t in tiers:
    check(t["_baseServicePerHour"] > 0, f"{t['_id']} base service {t['_baseServicePerHour']}/hr")
    check(t["_baseHousingCapacity"] >= 1, f"{t['_id']} sleeps {t['_baseHousingCapacity']} before anything is built")

print("\n== the tier gates")
by_guid = {}
for f in glob.glob(os.path.join(DATA, "Buildings", "*.asset.meta")):
    g = [l.split()[1] for l in open(f) if l.startswith("guid: ")][0]
    by_guid[g] = load(f[:-5])["_id"]
prev = {}
for t in tiers:
    reqs = {by_guid[r["_building"]["guid"]]: r["_minimumLevel"] for r in (t["_requirementsToAdvance"] or [])}
    if reqs:
        check(len(reqs) >= 2, f"{t['_id']} gate spans {len(reqs)} buildings: {reqs}")
        for b, lvl in reqs.items():
            check(lvl > prev.get(b, 0), f"{t['_id']} asks {b} {lvl} (was {prev.get(b,0)})")
            check(lvl <= rooms[b]["_maxLevel"], f"{t['_id']}'s {b} {lvl} is within its {rooms[b]['_maxLevel']} levels")
            check(rooms[b]["_minimumTierOrder"] <= t["_order"], f"{t['_id']} can build {b}")
        prev = reqs
    else:
        print(f"  --   {t['_id']} is the final tier")

print("\n== the opening")
village_rooms = [r for r in rooms.values() if r["_minimumTierOrder"] == 0]
earners = [r for r in village_rooms if produces(r, "ServiceDemand")]
check(bool(earners), "a Village room draws a crowd")
cheapest = min(ev(r["_costToReachLevel"],1) for r in earners)
check(START_GOLD >= cheapest, f"starting gold {START_GOLD} covers the cheapest earner at {cheapest:,.2f}")
check(tiers[0]["_baseHousingCapacity"] >= 1, "a Village guild can house somebody")

tav = rooms["tavern"]
print("\n== the World_View_Design section 3 table, now with a producer")
for lvl, tier in ((1,0),(17,1),(36,2),(tav["_maxLevel"],3)):
    seats = effect_at(tav,"ServiceSeats",lvl)
    print(f"  Tavern L{lvl:2d}: seats {math.floor(seats):3d} ({seats:6.3f})  could serve {seats*TURNS:9,.1f}/hr"
          f"   want in {effect_at(tav,'ServiceDemand',lvl)*tiers[tier]['_marketSize']:11,.1f}/hr"
          f"   spend {effect_at(tav,'CustomerSpend',lvl):10,.2f}"
          f"   staff slots {effect_at(tav,'StaffSlots',lvl):7.1f}")
    check(seats*TURNS < effect_at(tav,'ServiceDemand',lvl)*tiers[tier]['_marketSize'],
          f"    seats bind at {tiers[tier]['_id']} — the queue outside the door is permanent")

print("\n== beds, and the ceiling that was preserved")
bar = rooms["barracks"]
for lvl in (0,1,27,bar["_maxLevel"]):
    beds = tiers[0]["_baseHousingCapacity"] + effect_at(bar,"HousingCapacity",lvl)
    print(f"  Barracks L{lvl:2d}: {math.floor(beds):3d} beds ({beds:.4f})")
check(math.floor(tiers[0]["_baseHousingCapacity"] + effect_at(bar,"HousingCapacity",bar["_maxLevel"])) == 16,
      "a maxed Barracks sleeps sixteen, as a maxed Inn used to")
check(abs(effect_at(bar,"AdventurerPower",bar["_maxLevel"]) - 331.374586) < 0.01, "power ceiling preserved at 331.37")
check(abs(bonus_at(bar,"RecoverySpeed",bar["_maxLevel"]) - 1.586309) < 0.01, "recovery ceiling preserved at 1.586")
fdk = rooms["front_desk"]
check(abs(bonus_at(fdk,"RewardYield",fdk["_maxLevel"]) - 188.687979) < 0.05,
      "reward yield ceiling preserved at 188.69")
check(abs(bonus_at(fdk,"RewardYield",1) - 0.2) < 1e-6, "and its level-1 value is untouched at 0.20 (guild-wide x1.20)")
check(abs(effect_at(bar,"AdventurerPower",1) - 2.0) < 1e-6, "and a level-1 Barracks is +2 power, as a level-1 Training Room was")

print("\n== rarity still reaches Legendary")
for lvl in (9,17,25,32,tav["_maxLevel"]):
    print(f"  Tavern L{lvl:2d}: rarity {effect_at(tav,'RecruitableRarity',lvl):.3f} -> band {math.floor(effect_at(tav,'RecruitableRarity',lvl))}")
check(math.floor(effect_at(tav,"RecruitableRarity",tav["_maxLevel"])) >= 4, "a maxed Tavern attracts Legendaries")
check(math.floor(effect_at(tav,"RecruitableRarity",32)) == 4, "and does so from L32, as the canary says")
check(math.floor(effect_at(tav,"RecruitableRarity",9)) == 1, "Uncommon opens at L9")


# ---------------------------------------------------------------- the trade ----
print("\n== what the guild actually earns, through the shipped trade layer")

def trade(levels, tier):
    """TradeService.Allocate against the shipped assets: priority allocation, top down."""
    market = tier["_marketSize"]
    table = []
    for rid, lvl in levels.items():
        if lvl < 1: continue
        r = rooms[rid]
        table.append(dict(
            room=rid,
            demand=effect_at(r, "ServiceDemand", lvl) * market,
            seatCap=effect_at(r, "ServiceSeats", lvl) * TURNS,
            spend=effect_at(r, "CustomerSpend", lvl)))
    table.sort(key=lambda t: -t["spend"])
    remaining = tier["_baseServicePerHour"]          # no staff assets exist yet
    for t in table:
        t["want"] = min(t["demand"], t["seatCap"])
        t["served"] = min(t["want"], max(0.0, remaining))
        remaining -= t["served"]
        t["revenue"] = t["served"] * t["spend"]
        t["unserved"] = max(0.0, t["want"] - t["served"])
    return table

opening = trade({"tavern": 2, "front_desk": 1}, tiers[0])
gross = sum(t["revenue"] for t in opening)
unserved = sum(t["unserved"] for t in opening)
for t in opening:
    print(f"  {t['room']:12s} want {t['want']:9,.1f}/hr  served {t['served']:7,.2f}  spend {t['spend']:8,.2f}"
          f"  revenue {t['revenue']:9,.2f}/hr  unserved {t['unserved']:9,.1f}/hr")
check(gross > 0, f"a Village guild with Tavern 2 and Front Desk 1 earns {gross:,.2f} gold an hour unstaffed")
check(unserved > 0, f"and turns away {unserved:,.1f} an hour, which is what a thumb is for")

tap_per_min = min(content["_maxWaitingCustomers"] if "_maxWaitingCustomers" in content else 40.0,
                  unserved / 60.0) * opening[0]["spend"]
stipend_per_min = tiers[0]["_stipendGold"] * 60.0 / 30.0
print(f"\n  tapping is worth about {tap_per_min:,.2f} g/min against the crown's {stipend_per_min:,.2f} g/min "
      f"and {gross/60.0:,.2f} g/min of idle room income")
check(tap_per_min > stipend_per_min,
      "the thumb out-earns the mailbox once a room is built — which is the crown's stipend being "
      "re-checked on the day the rooms landed, and the answer being that the hardship line is not needed")

print()
print("FAILURES:", len(fails))
for f in fails: print("  -", f)

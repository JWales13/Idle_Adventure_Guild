"""Day 18 - the five rooms as assets.

Every curve figure below is either transcribed from the tuned configuration
(Docs/tools/tuned_params.json through tuner.build) or DERIVED here, never
retyped by hand. Day 4-5 shipped four transcription slips out of fourteen
assets, one of which handed the Inn its own cost curve as its bed curve.
"""
import os, sys, uuid, math, json

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DATA = os.path.join(ROOT, "Assets/_Project/Data")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import tycoon_model as M, tuner as T

TUNED = T.build(json.load(open(os.path.join(ROOT, "Docs/tools/tuned_params.json"))))
R = TUNED["rooms"]

def ev(b, l, g, level):
    s = max(0, level - 1)
    lin = b + l * s
    return lin if abs(g) < 1e-9 else lin * ((1 + g) ** s)

def respace_growth(base, old_growth, old_max, new_max):
    """Growth that puts `base` at the same value on a tree of new_max levels
    as (base, old_growth) reaches on a tree of old_max levels."""
    return (1.0 + old_growth) ** ((old_max - 1) / (new_max - 1)) - 1.0

def respace_linear(base, old_linear, old_max, new_base, new_max):
    """Linear-per-level with the same ceiling on the new tree."""
    return (base + old_linear * (old_max - 1) - new_base) / (new_max - 1)

BUILDING_SCRIPT = "98564af1443fa40259307c657fb1dda0"
TIER_SCRIPT     = "cee0f28fd24c14a29ad546d46f224f67"

STAT = dict(RewardYield=0, RecruitableRarity=1, AdventurerPower=2, HousingCapacity=3,
            RecoverySpeed=4, QuestSlots=5, MaxQuestTier=6, FailureRateReduction=7,
            ServiceSeats=8, CustomerSpend=9, ServiceDemand=10, StaffSlots=11,
            ContractCommission=12)
ADDITIVE, MULTIPLICATIVE = 0, 1

def num(x):
    """Unity writes floats plainly; keep enough digits that a growth rate round-trips."""
    if abs(x - round(x)) < 1e-9:
        return str(int(round(x)))
    return f"{x:.6f}".rstrip("0")

def curve(b, l=0.0, g=0.0, indent=6):
    pad = " " * indent
    return (f"{pad}BaseValue: {num(b)}\n"
            f"{pad}LinearPerLevel: {num(l)}\n"
            f"{pad}GrowthPerLevel: {num(g)}\n")


def folded(text, key_indent=2):
    """A single-quoted YAML scalar wrapped the way Unity wraps one."""
    import textwrap as _tw
    words = " ".join(text.split()).replace("'", "''")
    lines = _tw.wrap(words, width=74)
    pad = " " * (key_indent + 2)
    return ("'" + lines[0] + "\n" +
            "\n".join(pad + line for line in lines[1:]) + "'")

def effect(stat, kind, b, l=0.0, g=0.0):
    return (f"  - Stat: {STAT[stat]}\n"
            f"    Kind: {kind}\n"
            f"    ValuePerLevel:\n" + curve(b, l, g))

def building(name, ident, display, description, icon, tier_order, max_level, cost, effects):
    body = (f"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n"
            "  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
            "  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            f"  m_Script: {{fileID: 11500000, guid: {BUILDING_SCRIPT}, type: 3}}\n"
            f"  m_Name: {name}\n"
            "  m_EditorClassIdentifier: IdleGuild.Guild::IdleGuild.Guild.BuildingDefinition\n"
            f"  _id: {ident}\n"
            f"  _displayName: {display}\n"
            f"  _description: {folded(description)}\n"
            f"  _icon: {icon}\n"
            f"  _minimumTierOrder: {tier_order}\n"
            f"  _maxLevel: {max_level}\n"
            "  _costToReachLevel:\n" + curve(*cost, indent=4) +
            "  _effects:\n" + "".join(effects))
    return body

def meta(guid):
    return (f"fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n"
            "  externalObjects: {}\n  mainObjectFileID: 11400000\n"
            "  userData: \n  assetBundleName: \n  assetBundleVariant: \n")

# ---------------------------------------------------------------- derived ----
# Reward Yield leaves the Tavern's 90-level tree for the Front Desk's 52.
RY_G   = respace_growth(0.2, 0.08, 90, R["front_desk"]["maxLevel"])
# Adventurer Power leaves the Training Room's 40 for the Barracks' 41.
POW_G  = respace_growth(2.0, 0.14, 40, R["barracks"]["maxLevel"])
# Recovery Speed leaves the Inn's 30 for the Barracks' 41.
REC_G  = respace_growth(0.1, 0.10, 30, R["barracks"]["maxLevel"])
# Housing leaves the Inn's 30 for the Barracks' 41, on top of the tier's two beds.
BED_L  = respace_linear(2.0, 0.5, 30, 2.0 + 2.0, R["barracks"]["maxLevel"])

CHECK = {
 "RewardYield  ceiling": (ev(0.2, 0, 0.08, 90),          ev(0.2, 0, RY_G, R["front_desk"]["maxLevel"])),
 "AdvPower     ceiling": (ev(2.0, 0, 0.14, 40),          ev(2.0, 0, POW_G, R["barracks"]["maxLevel"])),
 "Recovery     ceiling": (ev(0.1, 0, 0.10, 30),          ev(0.1, 0, REC_G, R["barracks"]["maxLevel"])),
 "Housing      ceiling": (ev(2.0, 0.5, 0, 30),           2.0 + ev(2.0, BED_L, 0, R["barracks"]["maxLevel"])),
}
for label, (old, new) in CHECK.items():
    assert abs(old - new) < 1e-6, f"{label}: {old} != {new}"
    print(f"  {label}: {old:.6f} preserved")

print(f"\n  RewardYield growth  {RY_G:.6f}")
print(f"  AdvPower    growth  {POW_G:.6f}")
print(f"  Recovery    growth  {REC_G:.6f}")
print(f"  Housing     linear  {BED_L:.6f}")

# ------------------------------------------------------------------ rooms ----
tav, fd, bar, inn, prov = (R["tavern"], R["front_desk"], R["barracks"], R["inn"], R["provisioner"])

ROOMS = {
"Building_Tavern": dict(
    guid="3290872f038bc430f9dea3e256e800bf",     # existing
    ident="tavern", display="Tavern",
    description=(
        "Food, drink and a warm fire. Townsfolk fill its seats every ninety seconds and "
        "leave what a table is worth, and word of a good house travels: better adventurers\n"
        "    come looking for work, and there is always somewhere to put another pair of hands."),
    icon="{fileID: 21300000, guid: 7ee42ee8d9e334b5d98e137679dd64d5, type: 3}",
    tier_order=0, max_level=tav["maxLevel"],
    cost=(tav["cost"]["b"], tav["cost"]["l"], tav["cost"]["g"]),
    effects=[
        effect("ServiceSeats",      ADDITIVE, tav["seats"]["b"], tav["seats"]["l"], tav["seats"]["g"]),
        effect("CustomerSpend",     ADDITIVE, tav["spend"]["b"], tav["spend"]["l"], tav["spend"]["g"]),
        effect("ServiceDemand",     ADDITIVE, tav["baseDemand"]),
        effect("StaffSlots",        ADDITIVE, tav["staffSlots"]["b"], tav["staffSlots"]["l"], tav["staffSlots"]["g"]),
        effect("RecruitableRarity", ADDITIVE, 0, 0.13, 0),      # unchanged since Day 8-9
    ]),

"Building_FrontDesk": dict(
    guid=None, ident="front_desk", display="Front Desk",
    description=(
        "Where contracts are posted, argued over and paid out. The guild's cut of every "
        "completed job comes across this counter, so a better desk is worth more than\n"
        "    the adventurers who walk away from it."),
    icon="{fileID: 0}",
    tier_order=0, max_level=fd["maxLevel"],
    cost=(fd["cost"]["b"], fd["cost"]["l"], fd["cost"]["g"]),
    effects=[
        effect("RewardYield", MULTIPLICATIVE, 0.2, 0, RY_G),
    ]),

"Building_Barracks": dict(
    guid=None, ident="barracks", display="Barracks",
    description=(
        "Bunks, a drill yard and a rack of practice weapons. It houses the roster and "
        "trains it in the same breath, which is one idea rather than two: room count caps\n"
        "    who can live here, and the yard raises the Power of everyone who does."),
    icon="{fileID: 0}",
    tier_order=bar["unlockTier"], max_level=bar["maxLevel"],
    cost=(bar["cost"]["b"], bar["cost"]["l"], bar["cost"]["g"]),
    effects=[
        effect("HousingCapacity", ADDITIVE,       2.0, BED_L, 0),
        effect("AdventurerPower", ADDITIVE,       2.0, 0, POW_G),
        effect("RecoverySpeed",   MULTIPLICATIVE, 0.1, 0, REC_G),
    ]),

"Building_Inn": dict(
    guid="82c1ec95ef7c54fafb2abeb57b879109",     # existing
    ident="inn", display="Inn",
    description=(
        "Rooms let to travellers and merchants passing through. Purely a hotel now that "
        "the roster sleeps in the Barracks: fewer beds than the Tavern has seats, and\n"
        "    each one worth a great deal more per night."),
    icon="{fileID: 0}",
    tier_order=inn["unlockTier"], max_level=inn["maxLevel"],
    cost=(inn["cost"]["b"], inn["cost"]["l"], inn["cost"]["g"]),
    effects=[
        effect("ServiceSeats",  ADDITIVE, inn["seats"]["b"], inn["seats"]["l"], inn["seats"]["g"]),
        effect("CustomerSpend", ADDITIVE, inn["spend"]["b"], inn["spend"]["l"], inn["spend"]["g"]),
        effect("ServiceDemand", ADDITIVE, inn["baseDemand"]),
    ]),

"Building_Provisioner": dict(
    guid=None, ident="provisioner", display="Provisioner",
    description=(
        "Rope, rations, torches and potions, sold to anyone about to need them. The "
        "smallest crowd of the three trading rooms and by far the richest, because\n"
        "    nobody haggles over rope on the morning they leave."),
    icon="{fileID: 0}",
    tier_order=prov["unlockTier"], max_level=prov["maxLevel"],
    cost=(prov["cost"]["b"], prov["cost"]["l"], prov["cost"]["g"]),
    effects=[
        effect("ServiceSeats",  ADDITIVE, prov["seats"]["b"], prov["seats"]["l"], prov["seats"]["g"]),
        effect("CustomerSpend", ADDITIVE, prov["spend"]["b"], prov["spend"]["l"], prov["spend"]["g"]),
        effect("ServiceDemand", ADDITIVE, prov["baseDemand"]),
    ]),
}

existing = set()
for dirpath, _, files in os.walk(os.path.join(ROOT, "Assets")):
    for f in files:
        if f.endswith(".meta"):
            for line in open(os.path.join(dirpath, f), errors="ignore"):
                if line.startswith("guid: "):
                    existing.add(line.split()[1]); break

print()
for name, spec in ROOMS.items():
    guid = spec["guid"]
    meta_path = os.path.join(DATA, "Buildings", name + ".asset.meta")
    if guid is None and os.path.exists(meta_path):
        for line in open(meta_path):
            if line.startswith("guid: "):
                guid = line.split()[1]
                break
    if guid is None:
        while True:
            guid = uuid.uuid4().hex
            if guid not in existing:
                existing.add(guid); break
        with open(os.path.join(DATA, "Buildings", name + ".asset.meta"), "w") as fh:
            fh.write(meta(guid))
    path = os.path.join(DATA, "Buildings", name + ".asset")
    with open(path, "w") as fh:
        fh.write(building(name, spec["ident"], spec["display"], spec["description"],
                          spec["icon"], spec["tier_order"], spec["max_level"],
                          spec["cost"], spec["effects"]))
    print(f"  {name:24s} guid {guid}  tier {spec['tier_order']}  {spec['max_level']} levels")
    ROOMS[name]["guid"] = guid



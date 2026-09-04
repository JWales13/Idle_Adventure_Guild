"""Day 18 - the four tier fields, and the gates re-derived onto the new rooms."""
import os, sys, json, glob

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DATA = os.path.join(ROOT, "Assets/_Project/Data")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import tuner as T

TUNED = T.build(json.load(open(os.path.join(ROOT, "Docs/tools/tuned_params.json"))))
def building_guids():
    """Room id -> asset guid, read from the .meta files rather than kept in a side file.

    A second copy of a guid is a second thing to keep in step, and the .meta is where
    Unity already keeps the only copy that matters."""
    found = {}
    for meta in glob.glob(os.path.join(DATA, "Buildings", "*.asset.meta")):
        guid = next(l.split()[1] for l in open(meta) if l.startswith("guid: "))
        for line in open(meta[:-5]):
            if line.startswith("  _id: "):
                found[line.split(": ", 1)[1].strip()] = guid
                break
    return found

GUIDS = building_guids()
TIER_SCRIPT = "cee0f28fd24c14a29ad546d46f224f67"

# Reputation and the stipend are the CONTRACT economy and are deliberately not
# ported: the game pays a contract its full authored gold through RewardYield
# where the model pays a commission on it, so the model's thresholds describe a
# mechanism this build does not have. See Docs/Day18_The_Five_Rooms.md §4.
KEEP = {
    "village": dict(name="Tier_Village", display="Village", slots=1, maxTier=1, rep=30,   stipend=1),
    "town":    dict(name="Tier_Town",    display="Town",    slots=2, maxTier=2, rep=830,  stipend=2),
    "city":    dict(name="Tier_City",    display="City",    slots=3, maxTier=3, rep=65000, stipend=4),
    "capital": dict(name="Tier_Capital", display="Capital", slots=4, maxTier=4, rep=0,    stipend=8),
}

def num(x):
    return str(int(round(x))) if abs(x - round(x)) < 1e-9 else f"{x:.6f}".rstrip("0")

def tier_yaml(t):
    k = KEEP[t["id"]]
    reqs = t["req"]
    if reqs:
        # Ordered the way the player meets them: the Tavern first, then the desk,
        # then whatever the later tiers add. Stable order keeps the diff readable.
        order = ["tavern", "front_desk", "inn", "barracks", "provisioner"]
        lines = "".join(
            f"  - _building: {{fileID: 11400000, guid: {GUIDS[b]}, type: 2}}\n"
            f"    _minimumLevel: {reqs[b]}\n"
            for b in order if b in reqs)
        req_block = "  _requirementsToAdvance:\n" + lines
    else:
        req_block = "  _requirementsToAdvance: []\n"

    return ("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n"
            "  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
            "  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
            f"  m_Script: {{fileID: 11500000, guid: {TIER_SCRIPT}, type: 3}}\n"
            f"  m_Name: {k['name']}\n"
            "  m_EditorClassIdentifier: IdleGuild.Guild::IdleGuild.Guild.GuildTierDefinition\n"
            f"  _id: {t['id']}\n"
            f"  _displayName: {k['display']}\n"
            f"  _order: {t['order']}\n"
            f"  _questSlots: {k['slots']}\n"
            f"  _maxQuestTier: {k['maxTier']}\n"
            f"  _marketSize: {num(t['marketSize'])}\n"
            f"  _baseServicePerHour: {num(t['baseService'])}\n"
            f"  _baseHousingCapacity: {t['baseBeds']}\n"
            f"  _stipendGold: {k['stipend']}\n"
            + req_block +
            f"  _reputationToAdvance: {k['rep']}\n")

for t in TUNED["tiers"]:
    path = os.path.join(DATA, "Tiers", KEEP[t["id"]]["name"] + ".asset")
    open(path, "w").write(tier_yaml(t))
    print(f"  {KEEP[t['id']]['name']:14s} market {t['marketSize']:>12.6f}  baseService {t['baseService']:.4f} "
          f" beds {t['baseBeds']}  gate {t['req']}  rep {KEEP[t['id']]['rep']}")

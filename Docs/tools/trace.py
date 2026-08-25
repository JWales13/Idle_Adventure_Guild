#!/usr/bin/env python3
"""First-session trace: every beat in the opening, and every arrival that is not one.

    python3 trace.py [minutes] [step] [params.json]
"""
import json, sys, pathlib
import tycoon_model as M

def load(path):
    import tuner as T
    p = json.load(open(path))
    for k, (lo, hi) in T.SPEC.items():
        p.setdefault(k, (lo * hi) ** 0.5)
    return T.build(p)

if __name__ == "__main__":
    mins = float(sys.argv[1]) if len(sys.argv) > 1 else 30.0
    step = float(sys.argv[2]) if len(sys.argv) > 2 else 5.0
    src  = sys.argv[3] if len(sys.argv) > 3 else "tuned_params.json"
    c = load(src) if pathlib.Path(src).exists() else M.content()
    log = []
    w, marks, events, arrivals = M.simulate(c, horizon=mins * 60, step=step, log=log)
    last = 0.0
    for t, kind, label in log:
        gap = t - last
        if kind != "arrival":
            last = t
        mark = f"   ({gap/60:.0f} min of nothing)" if kind != "arrival" and gap > 120 else ""
        print(f"  {int(t)//60:3}m{int(t)%60:02}s  {'·' if kind=='arrival' else '●'} {label}{mark}")
    print(f"\n  first beats: " + "  ".join(
        f"{k} {M.hms(v)}" for k, v in w.beats.items()))
    pulses = [p for p in w.pulse if p <= mins * 60]
    gaps = [pulses[i+1] - pulses[i] for i in range(len(pulses) - 1)]
    if gaps:
        print(f"  longest dead stretch in the first {mins:.0f} min: {max(gaps)/60:.1f} min")

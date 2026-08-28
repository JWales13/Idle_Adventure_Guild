# Generating the art — a standalone walkthrough

Written Day 16 so that the art days can run without further design conversation. Every
option below is covered, free and free-trial first. **Read §1 and §2 before opening any
generator** — they are the two things that make art unusable after it is made, and both
are cheap to get right and expensive to discover late.

`Docs/Day15_Art_Brief.md` is still current for the **palette, the display mechanism and
the import settings**. Its *asset list* is superseded twice over — see §3.
`Docs/Day15_Portrait_Prompts.md` is current and its constant block is reusable for
everything, not just portraits.

---

## 1. The rule that disqualifies most free tiers

**This game is going on the App Store with IAP. That makes every asset a commercial
asset, and most free tiers do not grant commercial rights.**

It is the single most common way an indie art batch has to be thrown away, because
nothing warns you: the image generates, it looks fine, and the licence only matters if
somebody asks. Check it *before* generating, not after.

As of August 2026 — **verify against the live terms before you rely on it, these change
without notice:**

| service | free tier | commercial use on the free tier |
|---|---|---|
| **Self-hosted** (Draw Things / ComfyUI) | unlimited | ✅ **but it depends on the MODEL, see §2** |
| **Ideogram** | ~10 credits/day | ✅ explicitly granted on free |
| **Microsoft Copilot / Image Creator** | effectively unlimited | ✅ under Microsoft's standard terms |
| **Google Gemini** (free) | limited daily | ✅ under Google's terms |
| **Adobe Firefly** | monthly free credits | ✅ and it is the only one that indemnifies you — trained on licensed stock |
| **Leonardo AI** | 150 credits/day | ❌ **no** — the most generous free tier in the list and it cannot be used |
| **Krea** | small monthly quota | ❌ no |

The Leonardo line is the trap worth naming: it is the free tier everyone recommends,
150 credits a day would cover this whole project in a week, and the output cannot ship.

---

## 2. The second licence, which nobody mentions

If you self-host, the *app* is free but **the model carries its own licence**, and they
are not all the same:

| model | licence | can you ship a paid game with it |
|---|---|---|
| **FLUX.1 [schnell]** | Apache 2.0 | ✅ yes |
| **FLUX.1 [dev]** | non-commercial research licence | ❌ **no** — and it is the better-looking one, which is exactly why this trap catches people |
| **Stable Diffusion XL** | CreativeML Open RAIL++-M | ✅ yes |
| **Stable Diffusion 3.5** | Stability Community Licence | ✅ free under a revenue threshold — read it, it is short |

**Use FLUX.1 [schnell] or SDXL.** If a tutorial tells you to download `flux1-dev`,
that tutorial is not written for someone selling the result.

---

## 3. What actually needs generating — the list has grown twice

The brief says 24 assets. That was written on Day 14, before the tycoon revision and
before the Staff assembly existed. The real list:

### Changed by the revision (Day 14)
- **Five rooms, not three.** Training Room is retired; Front Desk, Provisioner and
  Barracks are new. The Inn split from housing into a hotel.
- **Room art shows *states*, not icons** — a room at level 1 and level 40 should look
  different, because "make the room nicer" is the entire game.
- **One hall frontage evolving across four tiers**, not four separate buildings. The
  guild never relocates; the settlement grows around it.
- **Direction is modern gacha-style anime**, not the muted register §3 of the brief
  describes. The palette in §3 still holds; the *register* does not.

### Added by Day 16, and listed nowhere else
- **Four staff portraits** — Potboy, Server, Barkeep, Steward. `StaffDefinition._icon`
  exists and is unread, exactly as `BuildingDefinition._icon` was.
- **A mailbox glyph** for the crown's stipend, which now sits in the treasury bar.

### The corrected list

| what | count | generate at | mechanism |
|---|---|---|---|
| App icon | 1 | **1024×1024**, no alpha | `PlayerSettings` |
| Adventurer portraits | 5 | 256×256 | `AdventurerDefinition._portrait` |
| Staff portraits | 4 | 256×256 | `StaffDefinition._icon` |
| Room art | 5 | 256×256 (states: ×2–3 later) | `BuildingDefinition._icon` |
| Quest icons | 5 | 128×128 | USS class |
| Tab icons | 3–4 | 128×128 | USS class |
| Currency glyphs | 3 | 64×64 | USS class |
| Mailbox glyph | 1 | 64×64 | USS class |
| Hall frontage per tier | 4 | 1080×1920 | USS class — **the designated cut** |

**31 assets**, up from 24. Generate at 2× display size and let Unity scale down; never up.

---

## 4. The options, ranked for this project

### Option A — Draw Things on your Mac ★ recommended

Free forever, unlimited, offline, commercially clean with the right model, and — the
reason it wins for a 31-asset set — **it can train a LoRA on your own first few images**,
which is the only reliable way to make thirty-one assets look like one artist made them.

- Apple Silicon native (Metal FlashAttention), roughly 20–40% faster than ComfyUI on the
  same Mac.
- **16 GB RAM** is the practical floor for FLUX.1 [schnell]; 24 GB is comfortable. Under
  16 GB, use a quantised SDXL (≈10 GB) instead.
- A 1024×1024 image lands somewhere between a few seconds and about a minute.

**Walkthrough**

1. Mac App Store → search **Draw Things** → install. No terminal, no Python.
2. First launch, open the model manager and download **FLUX.1 [schnell]** (or **SDXL
   base 1.0** if you are under 16 GB). Do not download `flux1-dev` — see §2.
3. Set: size **1024×1024**, steps 4–8 for schnell (it is a fast model, more steps do
   nothing), CFG ~1.0 for schnell / ~7 for SDXL.
4. Paste the constant block from `Day15_Portrait_Prompts.md` §2 with one `[CHARACTER]`
   line. Generate 4–6 candidates.
5. **Pick one, then note its seed.** Fix that seed and change only the `[CHARACTER]`
   line for the rest of the set. Same seed plus same constant block is most of your
   consistency for free.
6. If the five portraits still drift: take the two or three you like best, use Draw
   Things' **LoRA training**, and regenerate the whole set through the trained LoRA. An
   hour of training buys consistency no amount of prompt-wrangling will.
7. Export PNG. Then §6.

### Option B — Ideogram free tier

Best zero-install option that you can legally ship. ~10 credits a day, so this project
is roughly a **three to four week** trickle unless you upgrade — plan around it or use
it for the assets that matter most (app icon, portraits).

1. Sign up at ideogram.ai.
2. Paste the constant block, set aspect **1:1**.
3. Use the **"remix"** control on your first good result for the rest of the set — it is
   Ideogram's consistency mechanism and works like a style reference.
4. Download PNG.

### Option C — Microsoft Copilot / Google Gemini

Effectively unlimited (Copilot) or generously limited (Gemini), commercially usable, no
install. Weaker on style consistency and neither gives you a seed, so expect to lean on
"same style, lighting and framing as the previous image" inside one conversation.

Good for: quest icons, tab icons, currency glyphs — small, near-monochrome, forgiving.
Poor for: the five-portrait ladder, where drift is immediately visible.

### Option D — Adobe Firefly

Monthly free credits, and the **only option that indemnifies you** — trained on Adobe
Stock and licensed content, so there is no provenance question at all. If App Review or a
future publisher ever asks where the art came from, this is the cleanest possible answer.
Set Content Type = Art, aspect 1:1.

### Option E — paid, if the free routes stall

- **Midjourney** (~$10/mo) — still the strongest *consistency* toolkit: `--sref` locks a
  style across a whole batch and `--cref` locks a character. If the ladder of five
  portraits is fighting you, one month here solves it and you cancel.
- **ChatGPT Plus** — its real advantage is that it can see what it just made, so
  "same style as the previous image" genuinely works within one conversation.

### What to avoid

- **Leonardo, Krea** on free — no commercial rights (§1).
- **`flux1-dev`** — non-commercial (§2).
- Naming living artists or studios in prompts. Increasingly filtered, and for something
  you are selling it is cleaner to describe a style than borrow a name. The prompts doc
  is already written this way on purpose.

---

## 5. Consistency — the part that is actually hard

Thirty-one assets that look like one artist made them is harder than thirty-one good
assets. Five mechanisms, strongest first:

1. **Train a LoRA** on 3–5 of your own approved images (Draw Things does this locally and
   free). Everything after that is on-model by construction.
2. **Fix the seed** and vary only the subject line.
3. **Style reference** — Midjourney `--sref`, Ideogram remix.
4. **One conversation, one sitting** — ChatGPT/Gemini can see the previous image.
5. **Never shorten the constant block.** Each generation starts cold; the repetition *is*
   the mechanism. This is already §1 of the prompts doc and it is the most-ignored rule
   in AI art.

And the rule that overrides all of them: **generate a whole set in one sitting.** Models
get updated, styles drift, and a sixth portrait made a week later will not match the
first five.

### Two colour rules from the brief that art must not break

- **A Legendary portrait must not be gold-dominant, an Epic must not be
  purple-dominant.** The rarity colour is carried by the *frame and the text*. Art that
  also carries it makes the rarity signal unreadable — the one signal three days of
  balance work went into making legible.
- **Silhouette first.** Nothing may rely on colour alone; an icon must be identifiable in
  one flat colour before it is worth rendering.

---

## 6. After generating — the pipeline

macOS has everything you need built in. No installs.

**Resize** (`sips` ships with macOS):

```bash
sips -Z 256 input.png --out building_tavern.png      # fit within 256, keeps aspect
sips -z 256 256 input.png --out building_tavern.png  # force exactly 256x256
```

**Batch a whole folder:**

```bash
cd ~/Idle_Adventure_Guild/Assets/_Project/Art/Rooms
for f in *.png; do sips -z 256 256 "$f" --out "$f"; done
```

**Transparent backgrounds.** Most generators cannot produce true alpha. Options, free
first: `rembg` (`pip install rembg`, runs locally, one command per file), or any of the
browser background removers for a handful of files. Icons generated *white on a flat
dark background* are often easier to key out than ones generated "on transparent".

**The app icon is the exception and it has hard rules:**
- **No alpha channel** — Apple rejects icons with transparency.
- **No rounded corners, no drop shadow** — the mask is applied for you; baking one in
  reads as amateurish at every size.
- Must survive being 60px on a home screen. One shape, one accent, no text.

Flatten alpha for it:

```bash
sips -s format jpeg app_icon.png --out /tmp/flat.jpg
sips -s format png /tmp/flat.jpg --out app_icon_1024.png
```

---

## 7. Getting it into Unity without the two traps

### Trap 1 — Sprite Mode defaults to Multiple

**This project imports textures as Sprite Mode = Multiple**, so Unity's auto-slicer cuts
any image with a detached element — a glint, a spark, a floating rune — into pieces, and
the sprite field then has no whole image to point at. Day 15 found this on a single
tankard whose foam was disconnected. On a 31-asset batch it would hit most of the
portrait ladder silently.

Fix in the Inspector (select all → set once), or write it straight into the `.meta`:

```
spriteMode: 1
```

Full Inspector settings, from §5 of the brief:

```
Texture Type          Sprite (2D and UI)
Sprite Mode           Single          ← the trap
Generate Mip Maps     OFF             ← on by default; silently softens every icon
Wrap Mode             Clamp
Filter Mode           Bilinear
Max Size              512 icons/portraits, 2048 backgrounds
Compression           Normal Quality
sRGB (Color)          ON
Alpha Is Transparency ON              (all except the app icon)
```

**Mip maps off is the one to check by hand.** It is on by default, it blurs everything,
and it is invisible in the Inspector preview.

### Trap 2 — Git LFS is declared but may not be wired

`.gitattributes` has 25 LFS patterns from Day 14. **`git lfs install` on your machine is
still outstanding**, and **git treats an undefined filter as a silent no-op** — a commit
made before it looks completely successful and puts every PNG into history whole, where
it stays forever.

Before the first art commit:

```bash
git lfs install
git config --get filter.lfs.process     # must print: git-lfs filter-process
```

After the first art commit:

```bash
git lfs ls-files                        # must list your PNGs
```

Note `.meta` is deliberately **not** in LFS — it is small, it is text, Unity needs to
merge it, and routing sidecars through LFS breaks diffing on the thing you most need to
diff.

---

## 8. Order of work, and what to cut

Generate in this order, so that stopping early still leaves a coherent game:

1. **App icon** — external deadline, gates the App Store Connect record.
2. **Five adventurer portraits** — the rarity ladder, the most-looked-at screen, and the
   thing three days of balance work went into making legible.
3. **Five room images** — the home screen and the whole tycoon loop.
4. **Four staff portraits** — new, and cheap once the portrait style is locked.
5. **Interface icons** — tabs, currency, mailbox. Cheap, high impact, and they make the
   grey-box read as a product.
6. **Five quest icons.**
7. **Four hall frontages** — last, largest, least visible, and **the designated cut.**
   One frontage plus three palette-and-detail variants is the fallback and is a good
   outcome rather than a compromise: the four are meant to read as one place growing.

**Timebox per set, not per asset.** The risk table's warning is about chasing perfection
on individual assets; the defence is deciding in advance that a merely-good portrait
ships.

---

## 9. Before you generate anything — do the owed check first

The interface hand-check has now been deferred twice and it exists precisely to precede
this work: **whether the 96px room icon reads correctly beside a 28px title** is a
question about art sizing, and answering it after generating thirty-one assets is the
expensive order. Twenty-five minutes in the Editor, against a batch that is otherwise
sized to a judgement nobody has made.

That is not a process nicety. Two mechanics have already shipped unusable this week —
the takings tap inert, the stipend invisible — for the same reason: verified from the
inside, never from the player's side.

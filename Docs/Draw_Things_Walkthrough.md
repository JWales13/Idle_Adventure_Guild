# Draw Things + FLUX.1 [schnell] — a walkthrough from zero

Written for someone who has never opened Draw Things, generating art for
**Idle Adventurer's Guild** under the world-view design.

Draw Things updates often and control *labels* move between versions. Everything below is
described by **what the control does** as well as what it is called, so you can find it
even if your build words it slightly differently.

---

## Contents

- [Part 0 — Read this before you generate anything](#part-0)
- [Part 1 — What you are looking at](#part-1)
- [Part 2 — Every control, and what it actually does](#part-2)
- [Part 3 — Configuring for FLUX.1 [schnell]](#part-3)
- [Part 4 — Your first image, click by click](#part-4)
- [Part 5 — Seeds: the single most important concept](#part-5)
- [Part 6 — The four facings (image-to-image)](#part-6)
- [Part 7 — Fixing a broken hand (inpainting)](#part-7)
- [Part 8 — The spike, and the decision it settles](#part-8)
- [Part 9 — Locking the style permanently (LoRA)](#part-9)
- [Part 10 — Exporting](#part-10)
- [Part 11 — Cutting out the background](#part-11)
- [Part 12 — Getting it into Unity](#part-12)
- [Part 13 — The prompt library](#part-13)
- [Part 14 — Troubleshooting](#part-14)
- [Part 15 — What not to make yet](#part-15)

---

<a name="part-0"></a>
## Part 0 — Read this before you generate anything

**You do not need an art batch right now. You need one character.**

`World_View_Design.md` §9 sets the build order, and **eight of its nine steps need no art
at all** — the World assembly, the camera, rooms as rectangles, seats as slots, townsfolk
as capsules, the queue outside, the re-homed tap, staff, adventurers. All of that runs on
coloured shapes wired to the real economy.

The art worth making today is **one townsperson taken end to end**, because it settles the
one decision we deliberately left open: whether animation frames get *authored
individually* or *baked out of a rig*. That decision changes the size of the whole art
job by roughly ten times, and it cannot be settled by arguing about it.

So: **Part 8 is the goal.** Parts 1–7 are how to get there.

Everything else — rooms, redecoration states, the hall plan, fifteen characters — is
premature until the grey-box proves that panning around a guild hall is fun. That is not
process for its own sake: it is what Days 1–13 did, and it is why the Day 14 design
revision cost zero code.

---

<a name="part-1"></a>
## Part 1 — What you are looking at

Open Draw Things. You will see three regions.

**The canvas (centre/left).** A big empty area, usually with a dashed rectangle showing
the size of the image you are about to make. Generated images appear here. You can drag
images *into* it from Finder — that is how image-to-image starts.

**The configuration panel (right side).** Everything that controls generation. If you
cannot see it, look for a toggle — a chevron, a sliders icon, or a panel button in the
toolbar. On a narrow window it may be collapsed by default.

**The prompt box (bottom).** Usually two fields stacked: a large **Prompt** field and a
smaller **Negative Prompt** field beneath it. On some layouts these sit at the bottom of
the right-hand panel instead.

There is also a **history / files browser** — a thumbnail strip or a folder icon. Every
image you generate is saved automatically. You will use this to get back to an image whose
seed you want.

> **First thing to do:** widen the window. Draw Things hides controls aggressively when
> the window is narrow, and half of "I can't find that setting" is a window-size problem.

---

<a name="part-2"></a>
## Part 2 — Every control, and what it actually does

You do not need all of these, but you need to know what they are so the panel stops
looking like noise.

### Model
The brain doing the drawing. You have downloaded **FLUX.1 [schnell]**. Selecting a
different model changes everything about how the other settings should be set.

> **Licence, and it matters because you are selling this game.** `schnell` is Apache 2.0 —
> you may ship commercial work made with it. **`FLUX.1 [dev]` is a non-commercial research
> licence.** It looks better and it is the one most tutorials tell you to download. Do not
> use it here.

### Prompt
What you want. Flux responds to **descriptive sentences**, not comma-separated tags. This
is a real difference from Stable Diffusion — `masterpiece, best quality, 1girl, cel
shaded` is SD-speak and Flux handles it poorly.

### Negative Prompt
What you do not want. **On FLUX schnell this field does nothing at all.** See Part 3 for
why. Leave it empty and stop thinking about it.

### Steps
How many passes the model makes turning noise into an image. More steps normally means
more detail — **but schnell is a *distilled* model trained to finish in about four.**
Setting it to 30 is slower and often *worse*. This surprises everyone.

### Text Guidance (also called CFG or Guidance Scale)
How hard the model is pushed to obey your prompt. Low = loose and natural, high = rigid
and often burnt-looking. **schnell is guidance-distilled and wants 1.0.** Above roughly
1.5 the image visibly degrades.

### Sampler
The mathematics used to remove noise each step. For Flux, **Euler** (or "Euler A
Trailing" if your build lists it) is correct. Do not experiment here until everything else
works.

### Image Size (Width × Height)
Flux is trained at about one megapixel. **1024×1024** for square, **1024×1536** for a
standing full-body character. Always use multiples of 64. Bigger is not better — beyond
about 1.5 megapixels Flux starts duplicating limbs and faces.

### Seed
The starting pattern of random noise. **This is the most important control in the app**
and Part 5 is entirely about it.

### Strength
Only appears once there is an image on the canvas. It controls how much of that image gets
destroyed before regenerating: **0.0** leaves it untouched, **1.0** ignores it completely.
This is the workhorse of Part 6.

### Batch Size / Batch Count
How many images per run. Generating four to eight at once and picking is far more efficient
than generating one and rerolling. Batch *size* runs them together (more memory), batch
*count* runs them one after another (slower, safer). If memory is tight, use count.

### LoRA
A small add-on file that biases the model toward a specific style or character. You can
**train your own** locally and free. This is Part 9 and it is the real answer to making
fifteen characters look like one artist drew them.

### Control / ControlNet
Forces structure — a pose, a depth layout, an outline — from a reference image. If your
build has a Flux-compatible ControlNet (depth, canny or pose), it is the strongest possible
tool for locking a character's pose across facings. Treat it as optional and advanced; get
Parts 4–6 working first.

### Refiner / Upscaler
Ignore both for now. Upscaling is a post-step you will not need at these sizes.

---

<a name="part-3"></a>
## Part 3 — Configuring for FLUX.1 [schnell]

Set these once. Many builds let you save a **preset** or **configuration** — do that and
name it `flux-schnell-game-art`, so you can return to a known-good state after fiddling.

| Control | Value |
|---|---|
| Model | **FLUX.1 [schnell]** |
| Steps | **4** (8 is acceptable; more is waste) |
| Text Guidance / CFG | **1.0** |
| Sampler | **Euler** (or Euler A Trailing) |
| Width × Height | **1024 × 1536** for characters, **1024 × 1024** for square assets |
| Seed | leave random for exploration, fix it later — Part 5 |
| Batch Count | **4** to start |
| Negative Prompt | **empty** |
| LoRA | none yet |
| Control | none yet |

### The three things that will waste your afternoon if nobody tells you

**1. Negative prompts are inert.** A model is guided toward the prompt and away from the
negative prompt by the guidance mechanism. schnell has that mechanism *distilled out* —
it runs at guidance 1.0 by design. With no guidance there is nothing to push away from, so
the negative field is ignored entirely.

> **What to do instead:** describe what you *do* want. Not "no background clutter" but
> "standing alone on a completely flat, even, solid background". Not "no shadows" but
> "evenly lit with no cast shadow". Every exclusion has a positive form, and the positive
> form is the one that works.
>
> This matters for your existing `Day15_Portrait_Prompts.md`, which ends with a long list
> of "no text, no watermark, no border, no vignette". That was written for DALL·E and
> Midjourney. Under Flux it is close to decorative — rewrite those as descriptions.

**2. More steps does not mean more quality.** If an image is wrong, change the prompt or
the seed. Reaching for the step slider is the reflex from other models and it is wrong here.

**3. Flux wants prose.** Write like you are describing a photograph to someone on the
phone. Long, specific, ordinary sentences. Flux is unusually good at following them.

---

<a name="part-4"></a>
## Part 4 — Your first image, click by click

We are making the base townsperson. This one image becomes the reference for the entire
game's art style, so it is worth spending an hour on.

**1.** Set everything in the Part 3 table. Size **1024 × 1536**. Batch Count **4**.

**2.** Click into the **Prompt** field and paste this whole thing. Do not shorten it — the
repetition across generations is the entire consistency mechanism, and each generation
starts cold with no memory of the last.

```
A single character for a mobile game, drawn in a modern anime-influenced mobile game art
style: clean confident linework, soft cel shading with smooth gradient transitions, and
gentle rim lighting. Colours are rich but restrained, as though lit by warm interior
lamplight, with deep shadows tending violet-black rather than grey and warm off-white
highlights. Antique gold is the only bright accent.

The character is seen from a high three-quarter angle, the camera looking down at roughly
forty-five degrees, full body, standing upright and facing the viewer, arms relaxed at
their sides. The whole figure is inside the frame with even margins above the head and
below the feet.

A plain village townsperson, a middle-aged man in a simple brown wool tunic, patched
trousers and worn boots, with a friendly unremarkable face and short dark hair.

The character stands alone on a completely flat, even, solid chroma-green background with
no scenery, no floor, no cast shadow and no gradient. Clean silhouette, nothing crossing
the outline of the body.
```

**3.** Leave **Negative Prompt** empty.

**4.** Press **Generate**. First run downloads and compiles some shaders — it can take a
couple of minutes and look frozen. Later runs are seconds.

**5.** Four images appear. Judge them on **exactly three things** and nothing else:

- Is the **camera angle** right — looking down at roughly forty-five degrees, not straight
  on and not overhead?
- Is the **whole body** in frame with margins, feet not cropped?
- Is the **background flat and even**, with no floor, gradient or shadow?

Ignore the face, the hands, the boots. Those get fixed later. Angle, framing, background.

**6.** If none of the four are right, press Generate again — you are getting four new seeds
each time. Three or four rounds is normal. If ten rounds all fail on the same point, the
prompt is fighting you: see Part 14.

**7.** When one is right, **click it to select it, and find its seed.** There is usually an
info/inspector panel, an "i" button, or a right-click → *Copy Configuration*. **Write that
number down somewhere outside the app.** It is the anchor for everything that follows.

### Two notes on the prompt

**Why chroma green?** Flux cannot generate transparency. A flat, saturated colour that
appears nowhere in your palette keys out in about ten seconds (Part 11). Asking for a
"transparent background" produces a checkerboard *painted into the image*, which is worse
than useless.

**Why no cast shadow?** Unity draws the contact shadow under the sprite. A shadow baked
into the image will fight the floor at every position the character stands in.

---

<a name="part-5"></a>
## Part 5 — Seeds: the single most important concept

A generation starts from a field of random noise. The **seed** is the number that produces
that noise. Everything else — model, prompt, steps, sampler, size — is deterministic.

**Which means: same seed + identical settings = byte-identical image, every time.** Not
similar. Identical.

Three consequences you will use constantly:

**Reproducibility.** Fixed seed, tweak one clause of the prompt, regenerate → you get the
*same image with that one thing changed*. Fixed seed with a randomly changing prompt is how
you develop a character rather than gamble on one.

**Exploration vs refinement are different modes.** Random seed = exploring. Fixed seed =
refining. Do not mix them; you will not be able to tell whether a change came from your
edit or from new noise.

**A seed is worthless without its settings.** Seed 8842190 at 4 steps is a different image
from seed 8842190 at 8 steps. Record the whole configuration, not just the number. *Copy
Configuration* (if your build has it) copies all of it at once — paste it into a notes file.

> **Do this now:** make a plain text file next to your art folder called `seeds.txt`. One
> line per approved asset: what it is, the seed, the size, the steps. When you come back in
> three weeks to make a sixteenth character, this file is the difference between matching
> the set and starting over.

---

<a name="part-6"></a>
## Part 6 — The four facings (image-to-image)

You need each character facing **up, down, left and right**. Generating each from scratch
gives you four different people. Image-to-image gives you one person turning round.

**The idea:** put your approved image on the canvas, tell Draw Things to partly destroy it
and rebuild it with a changed prompt. **Strength** controls how much gets destroyed.

| Strength | Result |
|---|---|
| 0.0 – 0.2 | Almost nothing changes |
| **0.25 – 0.45** | **Same person, same clothes, new pose** ← what you want |
| 0.5 – 0.6 | Same style and palette, drifting features |
| 0.7 – 1.0 | A different person |

### Step by step

**1.** Get the approved front-facing image onto the canvas — from the history strip, or
drag the exported PNG in from Finder.

**2.** A **Strength** control appears. Set it to **0.35**.

**3.** Keep the *entire* prompt identical, and change only the camera clause. Replace:

> *…full body, standing upright and facing the viewer, arms relaxed at their sides.*

with, for the back view:

> *…full body, standing upright and seen from directly behind, facing away from the viewer,
> arms relaxed at their sides.*

and for the left view:

> *…full body, standing upright in profile facing to the viewer's left, arms relaxed at
> their sides.*

**4.** Generate a batch of four. Pick the one where the clothes and colours still match.

**5.** **Do not generate the right-facing view.** It is a horizontal flip of the left. Unity
flips sprites with a negative X scale at no cost. That is a free 25% saving on every
character in the game.

**6.** If the character drifts, drop Strength to 0.28 and try again. If it barely changes,
raise it to 0.42. This is the one number you will develop a feel for.

> **If your build has a Flux ControlNet** (depth, canny or pose), it is stronger than
> strength-tuning: it locks structure absolutely while the prompt changes appearance. Worth
> exploring *after* the above works, not instead of it.

---

<a name="part-7"></a>
## Part 7 — Fixing a broken hand (inpainting)

You will get an otherwise perfect character with six fingers. Do not reroll the whole image.

**1.** Put the image on the canvas.

**2.** Find the **mask / brush / inpaint** tool in the toolbar. Paint over just the bad
region — the hand, plus a little of the surrounding sleeve so the model has context.

**3.** Keep the same prompt and the same seed. Set Strength around **0.6–0.75** — higher
than Part 6, because inside the mask you *want* a real change.

**4.** Generate. Only the masked region is redrawn; the rest is untouched.

**5.** Repeat on other problems separately. Several small masked fixes beat one large one.

Hands and faces are where every image model is weakest. Budget a couple of inpaint passes
per character and it stops being frustrating.

---

<a name="part-8"></a>
## Part 8 — The spike, and the decision it settles

This is the point of the whole document.

**You now have:** one townsperson, four facings, consistent. **What you do not know:**
whether you can produce *walk frames* the same way.

### The test

Take your front-facing base. Image-to-image, Strength **0.35**, same seed, same prompt,
and add to the pose clause:

> *…mid-stride, left leg forward, right arm swinging forward.*

Generate four. Now put frame 1 and frame 2 side by side and ask one question:

**Is that the same man?**

Check the tunic colour, the patch on the trousers, the boots, the hairline, the face.

### What the answer means

**If they match** — frame-by-frame authoring is viable. Repeat for 6–8 frames per cycle,
per facing, per character, and budget accordingly (roughly 360–480 walk frames for the full
cast, before idle and sitting poses).

**If they drift — and I expect they will** — that is not a failure, it is the result. It is
what image models do: each generation is an independent act of invention, and "the same man
one step later" is not a concept the model holds. Move to baking:

1. Take **one** clean full-body image into a free layer editor (Krita or GIMP).
2. Cut it into parts on separate layers: head, torso, upper arm ×2, forearm ×2, thigh ×2,
   shin ×2. Rough cuts are fine; overlap at the joints.
3. Export the parts and import into Unity.
4. Rig with the **2D Animation** package — free, built into Unity, no purchase.
5. Animate **one** walk cycle.
6. Bake the result out to sprite frames.

The payoff: consistency is guaranteed by construction because no frame is ever
regenerated — **and that single walk cycle then drives all fifteen characters**, so the
fifteenth costs almost nothing after the first.

### Finish the spike

Drop the sprite into the grey-box world view, replacing one capsule. Watch it walk to a
seat. That is the spike complete, the decision made with evidence rather than argument, and
the art direction proven before a batch exists.

---

<a name="part-9"></a>
## Part 9 — Locking the style permanently (LoRA)

Once you have ten to fifteen approved images that agree with each other, train a **style
LoRA** on them. Draw Things does this locally, free, in an hour or two.

Roughly: find **Train / LoRA Training**, point it at your approved folder, give the style a
trigger word (`guildhallstyle`), accept the defaults, and let it run. Afterwards, enable the
LoRA and add the trigger word to your prompt — and everything you generate is on-model by
construction.

**This is the real answer to fifteen consistent characters**, and it is the reason Draw
Things beats the web services for this project. Do it *after* the spike, when the style is
settled — a LoRA trained on a style you are about to change is an hour spent twice.

---

<a name="part-10"></a>
## Part 10 — Exporting

Right-click an image → **Export** / **Save**, or drag it out of the canvas into Finder.
Always **PNG**, never JPEG — JPEG artefacts around a chroma-key edge make cutting out
noticeably harder.

Suggested layout, mirroring the repo:

```
~/Idle_Adventure_Guild/Art_Source/          ← raw generations, NOT in the Unity project
    characters/townsperson/base_front.png
    characters/townsperson/base_back.png
    rooms/tavern_state1.png
    seeds.txt
```

Keep raw generations **outside `Assets/`**. Unity imports everything under `Assets/` and
you do not want three hundred rejected takes in your project or your git history.

---

<a name="part-11"></a>
## Part 11 — Cutting out the background

The chroma green now pays off.

**Free and automatic** (best for characters):

```bash
pip install rembg
rembg i input.png output.png
```

**Free and manual** (best when you want control): open in GIMP or Krita → Select By
Colour → click the green → grow the selection 1–2 px → Delete → export PNG with alpha.

**Batch resize afterwards** — `sips` ships with macOS, nothing to install:

```bash
# one file, exact size
sips -z 256 256 in.png --out out.png

# whole folder
for f in *.png; do sips -z 256 256 "$f" --out "$f"; done
```

**The app icon is the exception.** Apple **rejects icons containing transparency**, and
bakes its own rounded corners — so no alpha, no rounded corners, no drop shadow:

```bash
sips -s format jpeg icon.png --out /tmp/flat.jpg
sips -s format png /tmp/flat.jpg --out app_icon_1024.png
```

---

<a name="part-12"></a>
## Part 12 — Getting it into Unity

### Sprite Mode — and this trap now has two correct answers

This project imports textures as **Sprite Mode = Multiple** by default, and Unity's
auto-slicer then cuts any image with a detached element into pieces. That is how a single
tankard became two sprites on Day 15.

Now that character sheets exist, the rule has two halves:

| asset | Sprite Mode |
|---|---|
| portraits, room art, glyphs, icons, app icon | **Single** (`spriteMode: 1` in the `.meta`) |
| character walk-cycle sheets | **Multiple** → Sprite Editor → Slice → **Grid By Cell Size** |

Getting this backwards is silent in both directions: a shredded portrait and an unsliced
sheet both look like "the sprite field is empty".

### The rest of the import settings

```
Texture Type          Sprite (2D and UI)
Generate Mip Maps     OFF     ← ON by default; silently blurs every sprite in the game,
                                and is invisible in the Inspector preview
Wrap Mode             Clamp
Filter Mode           Bilinear
Max Size              512 for characters and icons, 2048 for room and hall art
Compression           Normal Quality
sRGB (Color)          ON
Alpha Is Transparency ON      (all except the app icon)
```

### Git LFS — before the first PNG is committed

```bash
git lfs install
git config --get filter.lfs.process     # must print: git-lfs filter-process
```

`.gitattributes` has had 25 LFS patterns since Day 14, but **git treats an undefined filter
as a silent no-op** — a commit made before `git lfs install` looks completely successful
and writes every PNG into history whole, permanently. Verify afterwards with
`git lfs ls-files`.

---

<a name="part-13"></a>
## Part 13 — The prompt library

Every prompt is the **style block** with one slot swapped. Keep the block byte-identical.

### The character block

Parts 1, 2 and 4 of the Part 4 prompt, with paragraph 3 replaced by:

| who | slot text |
|---|---|
| Townsperson A | *A plain village townsperson, a middle-aged man in a simple brown wool tunic, patched trousers and worn boots, with a friendly unremarkable face and short dark hair.* |
| Townsperson B | *A plain village townswoman in a plain grey-green dress with a linen apron and a headscarf, carrying a small basket, with a round kind face.* |
| Potboy | *A skinny teenage tavern potboy in a stained apron over a rough linen shirt, sleeves rolled up, mop of untidy hair.* |
| Server | *A composed young tavern server in a neat dark waistcoat over a clean white shirt, hair tied back.* |
| Barkeep | *A broad-shouldered barkeep in a heavy leather apron with rolled sleeves and forearm wraps, greying beard.* |
| Steward | *A dignified elderly steward in a long dark coat with antique gold buttons and a chain of office, upright bearing.* |
| Militia Recruit *(Common)* | *A young militia recruit in plain boiled leather over a homespun tunic, a simple short sword at the hip, nervous posture, plain features.* |
| Hedge Knight *(Uncommon)* | *A weathered hedge knight in mismatched scavenged mail and a faded green surcoat, a dented kite shield on the back.* |
| Wandering Ranger *(Rare)* | *A lean wandering ranger in a hooded travelling cloak of muted blue-grey over quilted leather, a longbow across the back.* |
| Arcane Battlemage *(Epic)* | *An arcane battlemage in layered dark robes with faintly glowing runic embroidery at the cuffs, a short staff held at the side, calm and precise.* |
| Dragonsworn Champion *(Legendary)* | *A regal dragon-touched champion in ornate dark plate armour edged with antique gold filigree, a heavy cloak, faint horn ridges at the temples, composed and confident.* |

> **The rarity colour rule, and it is not cosmetic.** A **Legendary must not be
> gold-dominant** and an **Epic must not be purple-dominant**. The rarity colour is carried
> by the frame and the text in the UI. Art that also carries it makes the two
> indistinguishable and destroys the rarity signal — the one signal three days of balance
> work went into making legible.

### The room block

Same first paragraph (style), then:

```
The interior of a [ROOM] in a fantasy adventurers' guild hall, seen from a high
three-quarter angle looking down at roughly forty-five degrees, drawn as a cutaway with no
ceiling and no fourth wall.

The room is completely empty of people and empty of chairs and tables — only the fixed
architecture, the walls, the floor and the built-in fittings. Even lighting with no strong
cast shadows.
```

| room | slot |
|---|---|
| Tavern | *rough village tavern with a plank bar and a large stone hearth* |
| Inn | *modest inn landing with panelled doorways leading to guest rooms* |
| Front Desk | *guild front desk with a contract board, ledgers and a brass bell* |
| Provisioner | *provisioner's shop with heavy shelving, sacks, coils of rope and crates* |
| Barracks | *spartan barracks with stone walls, bunk frames and a weapon rack* |

> **Generate rooms empty of furniture, and this is a rule rather than a preference.** Seats
> are drawn by the game from the `ServiceSeats` stat — 4 at Tavern level 1, 20 by Town, 60
> at max. Furniture painted into the background contradicts the economy the moment the
> player upgrades. This is the depict-not-cause rule from the design doc, applied to art.

**Redecoration states:** generate the *poorest* state first, then image-to-image at
Strength **0.5–0.6** with the same seed and an upgraded description — *the same room, now
finely appointed with polished dark wood, brass fittings and antique gold detailing*. Same
footprint, richer dressing, so the states read as one room improving rather than three
different rooms.

---

<a name="part-14"></a>
## Part 14 — Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Images look washed out, melted or burnt | Text Guidance too high | Set it to **1.0**. This is the most common schnell mistake. |
| Very slow, or fans at full speed | Steps too high | Set Steps to **4**. |
| Out of memory / crash | Model too large for available RAM | Reduce image size; use Batch **Count** not Batch Size; close other apps; use a quantised model build if offered. |
| Negative prompt has no effect | Working as designed | schnell ignores it. Rewrite exclusions as positive description. Part 3. |
| Character keeps changing between generations | Random seed | Fix the seed. Part 5. |
| Two heads, three arms, duplicated figures | Resolution too far above ~1 megapixel | Drop to 1024×1536 or smaller. |
| Background has a floor or gradient | Prompt not explicit enough | Use the exact background sentence in Part 4 — "completely flat, even, solid… no floor… no gradient". |
| Style drifts across a set | Generated across sessions | Generate a whole set in one sitting; then train a LoRA (Part 9). |
| Hands are wrong | Universal to image models | Inpaint just the hand. Part 7. |
| First generation seems frozen | Shader compilation on first run | Wait a couple of minutes once. Later runs are seconds. |

---

<a name="part-15"></a>
## Part 15 — What not to make yet

Until the grey-box world view exists and proves the view is fun:

- the other fourteen characters
- any facing other than the spike character's four
- any redecoration state above the first
- the assembled hall plan
- quest icons and interface glyphs

Every one of those is **cheap to regenerate** once a LoRA locks the style, and **expensive
to regenerate** because you made them before you knew what the style was.

The one exception worth doing early is the **app icon**, because it gates the App Store
Connect record and that record has lead time you do not control.

> ## ⚠ MOSTLY CURRENT — one caveat
>
> The five archetypes, the framing, the constant block and the anime direction all still
> hold. The one thing that changed on Day 14: **adventurers no longer have individual
> levels.** Rarity is now a flat power multiple set by the Barracks, so the ladder is
> about *identity* rather than progression — which if anything makes the escalating
> ornateness below matter more, not less. See §5 of `Vision_Revision.md`.

# Portrait prompts — five archetypes

**Direction:** modern gacha splash art (Genshin / Epic Seven / Honkai register). Square
bust crops, plain backgrounds, mildly restrained saturation. The frame is authored in
vector separately and is what beds the art into the dark UI — so the generator's only
job is a face.

**Do not ask the generator for a frame, a border, a background scene, or text.** Every
one of those is either my job or a thing that has to be removed later.

---

## 1. How to use these

Paste the **whole block** each time. The first four paragraphs are identical across all
five prompts and that repetition is the entire consistency mechanism — resist the urge to
shorten them once you've "already said it", because each generation starts cold.

Three rules that matter more than the wording:

1. **Generate all five in one sitting.** Style drifts between sessions, models get
   updated, and a portrait made a week later will not match.
2. **Lock the style after the first good result.** In Midjourney, take the image's
   `--sref` value (or any fixed number, e.g. `--sref 1234567`) and reuse it on all five.
   In ChatGPT, keep the same conversation and say *"same art style, lighting and framing
   as the previous image"* — it can see what it just made, which is its real advantage.
3. **Don't name studios or living artists.** Beyond the ethics, prompts naming them are
   increasingly filtered, and for something you're selling it's cleaner to describe the
   style than to borrow a name. Everything below is descriptive on purpose.

### Platform notes

| | what to add |
|---|---|
| **Midjourney** | append `--ar 1:1 --style raw --sref <your locked value>` |
| **ChatGPT / DALL·E** | paste as-is; add *"square image"*; for 2–5 say *"same style, lighting and framing as the previous image"* first |
| **Firefly** | paste as-is, set aspect to 1:1, Content Type = Art |

---

## 2. The constant block

This is the part that never changes. `[CHARACTER]` is the only slot.

> Anime-style character portrait in a modern mobile-game splash art register: clean
> confident linework, soft cel shading with smooth gradient transitions, subtle rim
> lighting from behind, high-quality digital illustration.
>
> Square 1:1 composition. Head-and-shoulders bust, centred, facing three-quarters
> toward the viewer, head and both shoulders fully inside the frame with even margins.
> Plain flat dark background, single colour, no scenery, no pattern.
>
> Slightly restrained saturation — rich but not neon, colours a little muted as though
> lit by warm interior lamplight. Deep shadows tending violet-black rather than grey.
> Warm off-white highlights.
>
> **[CHARACTER]**
>
> No text, no watermark, no signature, no logo. No border, frame, vignette or card
> edge. No background scenery. Weapons and props must not cross the face or the
> silhouette edge.

---

## 3. The five

Each `[CHARACTER]` line below drops into the block above. They escalate deliberately —
plainer gear and warier posture at the bottom, more ornate and more composed at the top —
because that ladder is what makes rarity legible at a glance, and it's the same ladder
the balance work spent three days making true in the numbers.

**A colour rule runs through all five, and it is not cosmetic.** Each archetype's frame
carries its rarity colour, so the *portrait* must not be dominated by that same colour or
the two signals collapse into one and neither reads. The steer is written into each line.

---

### 1 · Militia Recruit — Common (grey frame)

> A young village militia recruit, late teens, nervous but willing. Short practical
> brown hair, plain undyed linen shirt under a scuffed leather jerkin, a simple iron
> gorget. No insignia, no ornament. Earth tones — brown, tan, dull iron. Slightly
> uncertain expression, chin a little lowered.

*Grey frame, so keep the costume warm and brown rather than grey or steel.*

---

### 2 · Hedge Knight — Uncommon (green frame)

> A wandering hedge knight in their thirties, weathered and self-reliant, sworn to no
> lord. Dark shoulder-length hair tied back, a plain travel-worn cuirass over a faded
> red gambeson, one pauldron replaced with a mismatched spare. A short scar across the
> jaw. Steady, unimpressed expression. Iron, oxblood and dust.

*Green frame, so no green in the costume — the faded red gambeson is doing that work.*

---

### 3 · Wandering Ranger — Rare (blue frame)

> A wandering ranger, lean and watchful, at home far from roads. A deep forest-green
> hooded cloak thrown back off the head, layered leather harness, a quiver strap
> crossing the chest, fair braided hair. Amber eyes, alert sidelong glance. Moss green,
> russet, worn brass.

*Blue frame, so keep it green and russet — no blue cloak, no cool tones.*

---

### 4 · Arcane Battlemage — Epic (purple frame)

> An arcane battlemage who fights in the line rather than behind it, poised and
> disciplined. Dark red high-collared coat with gold filigree worn over light plate at
> the shoulders, a rune-etched focus at the throat glowing faint cyan. Silver-white
> hair, sharp confident expression. Crimson, gold, pale cyan light.

*Purple frame, so steer the arcane glow to cyan and the robes to crimson — a purple mage
against a purple frame is the single easiest mistake to make here.*

---

### 5 · Dragonsworn Champion — Legendary (gold frame)

> A dragonsworn champion at the peak of their power, bound by oath to a dragon. Ornate
> dark scaled armour with deep crimson underlayers, a heavy mantle clasped at one
> shoulder, faint ember-orange light tracing the armour's seams. Black hair, one eye
> slit-pupilled and amber. Calm, absolute, unhurried expression.

*Gold frame, so the armour must read dark and crimson with gold only as a thin accent.
Gold armour against a gold frame turns your best adventurer into a blur.*

---

## 4. The app icon — a different job

Not a portrait, and worth treating separately: it has to work at 60 pixels on a home
screen, it's the asset gating the App Store Connect record, and it's the one where the
copyright gap actually stings, so plan to modify whatever comes back.

> A single bold fantasy guild emblem, centred, filling the frame. A stylised dragon
> curled around a tankard, rendered as a clean heraldic sigil. Antique gold on a deep
> violet-black field. Thick confident shapes, minimal internal detail, high contrast,
> readable at very small size. Flat vector-like illustration, subtle metallic sheen.
> Square. No text, no letters, no border, no rounded corners, no drop shadow.

Then hand it to me — I'll rebuild it as vector so it's crisp at every required size,
which also converts it from raw generation into something with real human authorship
behind it.

---

## 5. Before you generate all five

**Do one and stop.** Run the Militia Recruit, send me the result, and I'll frame it and
drop it into a mock roster row and party picker at true size so you can see it in the
actual interface rather than as a big image on a monitor. Two things only show up there:
whether the face survives being 68 pixels tall, and whether the muting is enough against
the dark UI.

If it works, generate the other four in the same session. If it doesn't, we've spent one
generation finding out instead of five.

That's the same habit the rest of this project runs on — wire one thing end to end before
committing to twenty-three more.

## 6. What to send me

Whatever the generator gives you, raw. Don't crop, don't remove backgrounds, don't
resize — all three are lossy by hand and automatic in code. I'll handle background
removal, square-cropping to consistent framing, the rarity frames, downscaling to 256×256,
and the Unity import settings.

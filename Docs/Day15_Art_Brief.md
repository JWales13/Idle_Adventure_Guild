> ## ⚠ SUPERSEDED IN PART — read `Vision_Revision.md` first
>
> Written on Day 14, hours before the design was revised into an idle tycoon. What
> survives: the display-mechanism decision (per-content art in sprite fields, per-screen
> art on USS classes), the palette pulled from `Tokens.uss`, the import settings, and the
> finding that **Day 17 carries every line of display code in the project**.
>
> What does **not**: the asset list. It assumes three buildings, four tier backgrounds
> and a Guild Hall that is only a screen. The game now has **five rooms**, the settlement
> grows around one hall rather than the guild relocating, and the art direction is
> **modern gacha-style anime** rather than the muted register described below. The room
> art also has to show *states*, not icons.
>
> Redo the asset list against §2 and §5 of `Vision_Revision.md` before generating anything.

# Days 15–16 — Art brief

Written on Day 14, ahead of the batch, because generating art is the cheap half and
**the game currently has nowhere to put any of it.**

---

## 1. The finding this brief exists for

`BuildingDefinition._icon` and `AdventurerDefinition._portrait` are the only two sprite
fields in the entire data model. Both were declared on Days 2–3 and **neither has ever
been read by anything.** Beyond them:

- `QuestDefinition` has no sprite field.
- `GuildTierDefinition` has no sprite field — so the roadmap's *"guild hall backgrounds
  per tier"* has nowhere to land.
- **No view renders an image at all.** `BuildingCard` builds a header of title plus
  level badge and a cost line, and that is the closest anything comes.
- `Ui.cs` offers `Box`, `Text`, `Action`, `Scroll`, `Stat` and `Progress`. There is no
  image constructor.
- The two `background-image: none` rules in `GuildTheme.uss` are Unity resets on `.tab`
  and a button class, not art slots.

So the roadmap's shape is misleading. Day 15–16 is *"AI art generation"* and Day 17 is
*"art integration — wire assets into USS"*, which reads like a small day following a big
one. In fact **Day 17 is carrying every line of display code in the project**: an image
helper, slots in three views, a tier background mechanism, and the decision below about
sprite fields. That is a one-day budget against work nobody has started.

Two consequences, and they are why this document is worth reading before opening an
image generator:

1. **Decide the display mechanism before generating**, because it fixes dimensions,
   transparency and naming. Art generated against the wrong mechanism is regenerated,
   and §05's risk table already flags art as the thing most likely to overrun.
2. **Consider moving an hour of Day 17 into Day 15.** Wiring one building icon end to
   end before generating twenty-three more assets converts an assumption into a fact,
   and this project's whole verification habit says the same thing in other clothes.

---

## 2. Two mechanisms, and the rule for which is which

Principle 01 says *styling in code, not the Inspector*, and Principle 01 also says
content is data. Those pull in different directions here, and the split that satisfies
both is:

| art that is… | mechanism | why |
|---|---|---|
| **per content instance** — this Tavern, this Champion | **`Sprite` field on the definition asset** | it varies with content, so it belongs to content. Adding an archetype must stay "one new asset", and a portrait is part of that asset. |
| **per interface element** — a tab, a currency glyph, a tier's backdrop | **USS `background-image`** | it varies with the *screen*, not with content. It needs no data field, it lives in the stylesheet with every other visual decision, and Week 3 can restyle it without touching an asset. |

Which settles the two open cases:

- **Quest icons → USS, keyed on a class**, not a new `QuestDefinition` field. Five
  quests is a closed set for the MVP, and the alternative adds a serialised field to a
  definition asset for something the Quest Board will complicate later anyway.
- **Tier backgrounds → USS, one class per tier on `.guild-root`.** `GuildScreenController`
  swaps the class on `GuildTierAdvanced`, which is a class name rather than a rule, so it
  stays on the right side of the `GuildContext` line.

**No new sprite field is needed on any asset.** That keeps Days 15–17 data-and-style
only, which matters more than usual: the architectural bet's real test is Quest Board
and Armory, and a `Sprite` field added to `QuestDefinition` this week is one more thing
that has to still be true then.

---

## 3. The palette is already decided, and generated art must not fight it

`Tokens.uss` is *"the only file in the project naming a raw colour or measurement"*, and
it has been since Day 7. It encodes a complete art direction that nobody has written
down in words:

```
surface base      rgb(24, 22, 28)     near-black, violet-leaning
surface raised    rgb(38, 34, 44)
surface overlay   rgb(48, 43, 56)
border            rgb(62, 56, 74)
text primary      rgb(240, 236, 230)  warm off-white
accent            rgb(198, 148, 66)   antique gold
gold (currency)   rgb(226, 180, 90)
reputation        rgb(126, 168, 224)  cool blue
gems              rgb(190, 130, 224)  violet
positive          rgb(122, 190, 128)
negative          rgb(212, 106, 106)
```

Rarity, which the art must never contradict because `Format.RarityClass` drives it:

```
common     rgb(168, 164, 172)   grey
uncommon   rgb(126, 194, 130)   green
rare       rgb(112, 158, 226)   blue
epic       rgb(184, 128, 224)   purple
legendary  rgb(232, 172, 78)    gold
```

**In one sentence for a prompt:** *dark violet-black fantasy interface art, antique gold
as the single accent, warm off-white highlights, muted and low-saturation, no bright
primaries, painterly rather than cel-shaded, readable at thumbnail size.*

Two rules that matter more than style:

- **A Legendary portrait must not be gold-dominant** and an Epic must not be
  purple-dominant. The rarity colour is carried by the frame and the text; a portrait
  that also carries it makes the two indistinguishable and makes the *rarity* signal
  unreadable, which is the one signal Days 10–13 spent three days trying to make legible.
- **Nothing may rely on colour alone.** Silhouette first — a building icon has to be
  identifiable in one flat colour before it is worth rendering.

---

## 4. The asset list

Reference resolution is **1080×1920, ScaleMode 2 (scale with screen size), Match 0**, so
width matches and 1 UI pixel = 1 physical pixel at 1080 wide. On a 1284-wide device
everything scales up ~1.19×, so **generate at 2× the display size** and let Unity scale
down. Never up.

### 4.1 · Building icons — data, `BuildingDefinition._icon`

| file | id it must match | display | generate |
|---|---|---|---|
| `building_tavern.png` | `tavern` | ~96px | **256×256** |
| `building_training_room.png` | `training_room` | ~96px | **256×256** |
| `building_inn.png` | `inn` | ~96px | **256×256** |

Transparent background, square, subject centred with ~10% padding. These sit in
`.card__header` beside `.card__title` on a `--color-surface-raised` card, so they must
read against `rgb(38, 34, 44)`.

Each building owns one stat and the icon should say which: Tavern = reward/hospitality,
Training Room = power, Inn = capacity/rest.

### 4.2 · Adventurer portraits — data, `AdventurerDefinition._portrait`

| file | id | generate |
|---|---|---|
| `adventurer_militia_recruit.png` | `militia_recruit` | **256×256** |
| `adventurer_hedge_knight.png` | `hedge_knight` | **256×256** |
| `adventurer_wandering_ranger.png` | `wandering_ranger` | **256×256** |
| `adventurer_arcane_battlemage.png` | `arcane_battlemage` | **256×256** |
| `adventurer_dragonsworn_champion.png` | `dragonsworn_champion` | **256×256** |

Head-and-shoulders, consistent framing and lighting across all five — they appear
side by side in the party picker, where sixteen rows are visible at once and any
inconsistency reads as a mistake. `.party-row` is 68px minimum height, so these must
survive being drawn small.

**The five must read as a ladder at a glance**, since the whole point of Days 10–13 was
making rarity legible: plainer gear and posture at Common, progressively more ornate and
more confident to Legendary. That is the signal doing work no number can do.

### 4.3 · Tier backgrounds — USS, class on `.guild-root`

| file | class | generate |
|---|---|---|
| `bg_tier_village.png` | `.guild-root--village` | **1080×1920** |
| `bg_tier_town.png` | `.guild-root--town` | **1080×1920** |
| `bg_tier_city.png` | `.guild-root--city` | **1080×1920** |
| `bg_tier_capital.png` | `.guild-root--capital` | **1080×1920** |

These sit *behind* the entire interface, including text. **Deliberately low contrast and
dark** — closer to `--color-surface-base` than to anything eye-catching, with detail
concentrated in the lower third where the tab bar and cards already cover it. A
background that competes with the treasury bar is a background that gets deleted on
Day 22.

The four must read as one place growing, not four places: same viewpoint, same
architecture, more of it each time.

### 4.4 · Quest icons — USS, class per quest

| file | class | generate |
|---|---|---|
| `quest_rat_infested_cellar.png` | `.quest-icon--rat-infested-cellar` | **128×128** |
| `quest_bandit_patrol.png` | `.quest-icon--bandit-patrol` | **128×128** |
| `quest_ruined_watchtower.png` | `.quest-icon--ruined-watchtower` | **128×128** |
| `quest_sunken_crypt.png` | `.quest-icon--sunken-crypt` | **128×128** |
| `quest_dragons_roost.png` | `.quest-icon--dragons-roost` | **128×128** |

Transparent, flat, near-monochrome — these are labels rather than illustrations, and
they must not out-shout the adventurer portraits on the same screen. Difficulty should
be legible in the silhouette: a rat, a dragon.

### 4.5 · Interface icons — USS

| file | class | generate |
|---|---|---|
| `tab_hall.png` | `.tab--hall` | **128×128** |
| `tab_quests.png` | `.tab--quests` | **128×128** |
| `tab_roster.png` | `.tab--roster` | **128×128** |
| `currency_gold.png` | `.currency--gold` | **64×64** |
| `currency_reputation.png` | `.currency--reputation` | **64×64** |
| `currency_gems.png` | `.currency--gems` | **64×64** |

`--height-tab-bar` is 132px, so tab icons display around 56–64px. **Single-colour
silhouettes**, tinted by USS through `-unity-background-image-tint-color` so `.tab` and
`.tab--active` are one asset rather than two. Generate them white-on-transparent for
that to work.

Gems are stubbed for v1 but the glyph is wanted for Day 19's IAP surfaces, and
generating it now costs nothing.

### 4.6 · App icon — `PlayerSettings`, not a sprite

`app_icon_1024.png`, **1024×1024**, and it is the one asset with hard external rules:

- **No alpha channel.** Apple rejects icons with transparency.
- **No rounded corners and no drop shadow** — the mask is applied for you, and baking
  one in reads as amateurish at every size.
- It must survive being 60px on a home screen. One shape, one accent, no text.

This is also the item on §04's checklist that gates the App Store Connect record, so it
is worth doing first rather than last in the batch.

---

## 5. Import settings

Every file lands under `Assets/_Project/Art/` in folders mirroring the list above.
Select all, then in the Inspector:

```
Texture Type       Sprite (2D and UI)
Sprite Mode        Single
Pixels Per Unit    100          (irrelevant to UI Toolkit; leave it consistent)
Generate Mip Maps  OFF          ← UI never minifies; mips cost memory and blur
Wrap Mode          Clamp
Filter Mode        Bilinear
Max Size           512 for portraits and icons, 2048 for backgrounds
Compression        Normal Quality
Format             Automatic
sRGB (Color)       ON
Alpha Is Transparency  ON       (all except the app icon)
```

**Mip maps off is the one worth checking by hand.** It is on by default, it silently
softens every icon in the game, and it is invisible in the Inspector preview.

---

## 6. Batch discipline, and the cut

§05's risk table already decided what happens if this overruns:

> *AI art generation eats more time than budgeted → reuse building silhouettes across
> tiers with palette/detail swaps rather than fully unique art per tier.*

Make that a plan rather than a rescue. **Generate in this order**, so that stopping early
still leaves a coherent game:

1. **App icon** — external deadline, gates the ASC record.
2. **Five adventurer portraits** — the rarity ladder is the game's most-looked-at screen
   and the thing three days of balance work was spent making legible.
3. **Three building icons** — the home screen.
4. **Six interface icons** — cheap, high impact, and they make the grey-box read as a
   product.
5. **Five quest icons.**
6. **Four tier backgrounds** — last, because they are the largest, the least visible, and
   **the designated cut.** One background plus three palette-and-detail variants is the
   fallback, and it is a good outcome rather than a compromise: the four are supposed to
   read as one place growing anyway.

Timebox per asset, not per set. §05's warning is about chasing perfection on individual
assets, and the defence is deciding in advance that a merely-good portrait ships.

---

## 7. Before generating — three things to settle

1. **Git LFS is wired.** Done on Day 14, but confirm `git config --get filter.lfs.process`
   returns `git-lfs filter-process` before the first PNG is committed, because git treats
   an undefined filter as a **silent** no-op and the commit will look perfectly fine.
   Verify the first art commit with `git lfs ls-files`.
2. **Wire one icon end to end first.** A `Ui.Icon` helper, `_icon` read in `BuildingCard`,
   one Tavern PNG on screen. An hour on Day 15 against a Day 17 that is currently
   carrying all of it, and it turns §2's argument into something observed.
3. **Do not rename the bundle ID yet.** It is the save directory, and Day 14's played-in
   save has to be captured as a fixture first.

---

## 8. Sizes at a glance

```
256×256    8 files   3 building icons, 5 adventurer portraits
128×128    8 files   5 quest icons, 3 tab icons
 64×64     3 files   currency glyphs
1080×1920  4 files   tier backgrounds        ← the cut, if one is needed
1024×1024  1 file    app icon, no alpha

24 assets total
```

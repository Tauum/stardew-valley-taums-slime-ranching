# Open decisions (do not silently lock these)

**Chromatic Crystal prototype:** One crystal can focus a breeder offspring on a base color and clear hybrid/exceptional traits. Decide acquisition (processing, recipes, Marnie, drops, or progression rewards), cost/rarity, and whether matching species names should stay. Verify the crystal palette against the intended art before balancing.

**Breeding trait scope (decision):** Offspring inherit color/species only. Egg icons use the same rendered color variant, and ability IDs are derived from the offspring species. Other traits use defaults; size remains individually configurable but is not inherited.

**Shiny visual prototype:** `IsShiny` now brightens Ranch Slime sprite pixels by a subtle fixed amount. Decide whether shiny is cosmetic-only or should have separate gameplay effects; avoid coupling it to rarity or size.

**Ability ID foundation:** Base and listed hybrid genomes now carry stable IDs matching the companion concept table. Ability behavior, cooldowns, size scaling, and interaction targeting remain to be implemented.

1. **Pink recipes:** Pink is outside the standard wheel. Which pairings produce Pink or Pink hybrids?
2. **Undefined breeding pairs:** Current prototype inherits one random parent's color/species identity for undefined pairings. Decide whether to keep that fallback or block undefined combinations.
3. **Primary pair naming:** Primary recipes now produce Orange, Green, and Purple (also called Violet in the design). Confirm the preferred in-game name for Blue+Red's result.
4. **Hybrid scope:** The six listed tertiary combinations are implemented. Decide whether secret hybrids should follow later.
   - Seven base genome colors currently map to the nearest Red/Orange/Yellow/Green/Blue/Purple/Pink sprite tint. Exact RGB blending for hybrids or arbitrary colors is still undecided.
5. **Exceptional acquisition:** Can Golden/Prismatic/Black/White appear from breeding, exploration, mutation, or all three? At what rates?
6. **Shiny:** Inheritance is out of scope. Decide separately whether shiny remains purely cosmetic or gains independent gameplay effects.
7. **Mutagenic Pool:** Shadow Clone is the first data-only trait. Which additional rare traits should be possible? The provisional 500-Slime cost, 5% base chance, +2 points per failure, 30% cap, one-day duration, and 15,000g shop price are configurable for playtesting; decide final values after testing.
8. **Furniture:** Marnie sells the Converter, Breeder, Incubator, trampoline, cushion, playground, slide, bed, Compost Trough, tunnel, Heated Slime Stone, and pond at configurable provisional prices. Compost Trough affects juvenile growth; the other implemented enrichments are visual-only. Review prices/progression, winter comfort, friendship effects, and whether the Playground already fully covers the separate ball-pit concept before adding duplicate furniture.
9. **Breeder:** How to obtain the basic machine; cycle-time tuning; whether parents remain in world while shown in machine. A one-day rest after egg collection is implemented provisionally; review the duration after playtesting.
10. **Eggs:** A prototype now uses a dedicated Daycare incubator, a 15-day default timer, and automatic hatching into the Daycare when space exists. Its menu displays the active egg's genome label and day-based progress. Inserting a converter Common Egg creates a fresh Green (`#00FF00`), Normal, Common, Generation 1 genome; breeder eggs keep their stored genome. Duplicate copies of one breeder genome egg ID can hatch only once. Confirm persistence/transfer behavior, the duration, and duplicate-item handling through playtesting. Keep managed eggs out of the vanilla incubator. The concept image's liquid modes and stability meter have no defined mechanics yet; do not imply effects until designed.
11. **White/Black:** Behavior at sleep, festivals and multiplayer; whether time powers stack (recommend never).
12. **Gel Refinery:** The first test recipe is provisionally 1 Broken Glasses + 10 vanilla Slime -> 1 Refined Quartz, processed over 1 in-game day; machine price defaults to 12,000g. Fuel, duration, and shop price are configurable. Review whether recycling should be profitable, then choose progression unlocks and later recipes. Cosmetic transmutation remains a separate feasibility question.
13. **Resource production:** How drops are collected, production interval, caps and exceptional outputs. In particular, should the Gel Pond produce any existing items? Choose item pool, season/species eligibility, interval, and a strict daily cap before enabling drops; the current pond is visual-only.
14. **Companion capacity:** One bound active slime at a time? Recommend one initially.
15. **Maturity:** Ranch Slimes currently mature after three in-game days, matching the hatching-as-juvenile goal. Maturity progression is now tracked in the external registry and mirrored to Stardew's native animal age field. Review duration, migration, and visual growth steps in game.
16. **Compost Trough:** Current test behavior consumes one eligible trash, algae, or monster-drop item per juvenile per day, advancing one maturity day; matching its favorite adds one more day. Growth amount, liked-food bonus, accepted items, and Marnie unlock/price are provisional; check the three-day maturity cap and feel before finalizing.
17. **Slime Core:** Review the first-slice proposal in `Features/COMPANIONS.md`: accessory slot versus inventory item, acquisition source/rate, and whether dismissal returns or consumes the binding Core. Current safe defaults are one active companion and Daycare-only binding; no follower AI should start until binding preserves the exact native animal/genome identity.
   - External-only identity-binding test commands are implemented; Core item acquisition/equip/dismiss rules and movement remain open and are not silently chosen by that prototype.

## Decision log
| Date | Decision | Status |
|---|---|---|
| 2026-10-26 | Controlled machine breeding with visible parents and egg output | Accepted |
| 2026-10-26 | Fixed recipes and dual-ability hybrids | Accepted |
| 2026-10-26 | Furniture sold by Marnie, interactive trampoline | Accepted |
| 2026-10-26 | Existing-item outputs and Slime-fueled refinery | Accepted |
| 2026-10-26 | Void inventory merged into Collector | Accepted |
| 2026-10-26 | Black +1 night hour, White +1 morning hour | Accepted concept; technical feasibility TBD |
| 2026-10-26 | Vanilla Slime Eggs remain unchanged unless placed in a separate Slime Egg Converter | Accepted |
| 2026-10-27 | Parents rest for one in-game day after collecting a breeding egg | Provisional implementation; review balance |
| 2026-10-27 | Managed eggs hatch in a separate Slime Incubator with a 15-day default cycle inside the Daycare; a full Daycare defers hatching | Prototype implemented; verify and review balance |
| 2026-10-27 | Common converter eggs receive a fresh base Common genome when inserted into the incubator | Prototype implemented; verify common egg flow |
| 2026-10-27 | Ranch Slime juveniles mature after three in-game days and cannot breed while growing | Provisional implementation; verify visual growth and balance |
| 2026-10-27 | Mutagenic Pool assigns one mature Ranch Slime for a nonlethal Shadow Clone mutation roll with per-slime pity | Prototype implemented; default one-day duration and all costs/odds/shop price are configurable and provisional |
| 2026-10-27 | Gel Refinery first recipe recycles Broken Glasses into Refined Quartz using Slime as fuel | Provisional vertical slice implemented; one-day duration and 10-Slime fuel defaults, 12,000g machine price; review balance and expand recipes |
| 2026-10-27 | Compost Trough advances a juvenile's maturity when fed an eligible item once per day; favorite food gives a bonus | Provisional implementation; +1 growth day normally, +1 favorite bonus, configurable; verify and review values |

## Current decisions locked
Full custom breeder; external-only mod state; custom companion AI; single-player first; GPT-assisted sprite production. The breeder egg ID to external genome link is prototyped but still needs transfer/reload verification. Remaining design decisions include Pink recipe rules, exceptional acquisition, time-change safeguards, precise mutation and shop balancing.

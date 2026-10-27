# Open decisions (do not silently lock these)

**Chromatic Crystal prototype:** One crystal can focus a breeder offspring on a base color and clear hybrid/exceptional traits. Decide acquisition (processing, recipes, Marnie, drops, or progression rewards), cost/rarity, and whether matching species names should stay. Verify the crystal palette against the intended art before balancing.

**Size genetics prototype:** Five size labels, weighted inheritance, and nearest-neighbor sprite scaling now exist. Review the provisional same-size and mixed-size weights in `Features/SIZE_AND_MATURITY.md`; collision, placement, and petting interaction still need in-game validation.

1. **Pink recipes:** Pink is outside the standard wheel. Which pairings produce Pink or Pink hybrids?
2. **Undefined breeding pairs:** Block the combination, return one parent's color, or offer a rare result?
3. **Primary pair recipes:** Red+Yellow→Orange, Yellow+Blue→Green, Blue+Red→Violet suggested but not explicitly confirmed.
4. **Hybrid scope:** Only six tertiary combinations initially, or secret hybrids later?
   - Seven base genome colors currently map to the nearest Red/Orange/Yellow/Green/Blue/Purple/Pink sprite tint. Exact RGB blending for hybrids or arbitrary colors is still undecided.
5. **Exceptional acquisition:** Can Golden/Prismatic/Black/White appear from breeding, exploration, mutation, or all three? At what rates?
6. **Shiny inheritance:** Random roll, inherited boost, or both? Exact probability?
7. **Mutagenic Pool:** Shadow Clone confirmed; which additional rare traits should be possible?
8. **Furniture:** The Converter, Breeder, and Incubator are in Marnie's shop at configurable provisional prices (2,500g, 10,000g, and 5,000g). A Slime Trampoline (2,500g), Slime Cushion (1,500g), Slime Playground (6,000g), Slime Slide (3,000g), and Slime Bed (1,800g) are also available at provisional prices. Their animations have no gameplay effects. Review balance and progression unlocks; other enrichment stock and any happiness effect remain undecided.
9. **Breeder:** How to obtain the basic machine; cycle-time tuning; whether parents remain in world while shown in machine. A one-day rest after egg collection is implemented provisionally; review the duration after playtesting.
10. **Eggs:** A prototype now uses a dedicated Daycare incubator, a two-day timer, and automatic hatching into the Daycare when space exists. Its menu displays the active egg's genome label and day-based progress. Inserting a converter Common Egg creates a fresh Green (`#00FF00`), Normal, Common, Generation 1 genome; breeder eggs keep their stored genome. Duplicate copies of one breeder genome egg ID can hatch only once. Confirm persistence/transfer behavior, the duration, and duplicate-item handling through playtesting. Keep managed eggs out of the vanilla incubator. The concept image's liquid modes and stability meter have no defined mechanics yet; do not imply effects until designed.
11. **White/Black:** Behavior at sleep, festivals and multiplayer; whether time powers stack (recommend never).
12. **Gel Refinery:** Conversion recipes, slime fuel costs, progression unlock and cosmetic transmutation feasibility.
13. **Resource production:** How drops are collected, production interval, caps and exceptional outputs.
14. **Companion capacity:** One bound active slime at a time? Recommend one initially.
15. **Maturity:** Ranch Slimes currently mature after three in-game days, matching the hatching-as-juvenile goal. Maturity progression is now tracked in the external registry and mirrored to Stardew's native animal age field. Review duration, migration, and visual growth steps in game.

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
| 2026-10-27 | Managed eggs hatch in a separate two-day Slime Incubator inside the Daycare; a full Daycare defers hatching | Prototype implemented; verify and review balance |
| 2026-10-27 | Common converter eggs receive a fresh base Common genome when inserted into the incubator | Prototype implemented; verify common egg flow |
| 2026-10-27 | Ranch Slime juveniles mature after three in-game days and cannot breed while growing | Provisional implementation; verify visual growth and balance |

## Current decisions locked
Full custom breeder; external-only mod state; custom companion AI; single-player first; GPT-assisted sprite production. The breeder egg ID to external genome link is prototyped but still needs transfer/reload verification. Remaining design decisions include Pink recipe rules, exceptional acquisition, time-change safeguards, precise mutation and shop balancing.

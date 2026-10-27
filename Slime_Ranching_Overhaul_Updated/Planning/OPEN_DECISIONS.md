# Open decisions (do not silently lock these)

1. **Pink recipes:** Pink is outside the standard wheel. Which pairings produce Pink or Pink hybrids?
2. **Undefined breeding pairs:** Block the combination, return one parent's color, or offer a rare result?
3. **Primary pair recipes:** Red+Yellow→Orange, Yellow+Blue→Green, Blue+Red→Violet suggested but not explicitly confirmed.
4. **Hybrid scope:** Only six tertiary combinations initially, or secret hybrids later?
5. **Exceptional acquisition:** Can Golden/Prismatic/Black/White appear from breeding, exploration, mutation, or all three? At what rates?
6. **Shiny inheritance:** Random roll, inherited boost, or both? Exact probability?
7. **Mutagenic Pool:** Shadow Clone confirmed; which additional rare traits should be possible?
8. **Furniture:** Marnie's initial stock, pricing and unlock conditions; exact happiness effect.
9. **Breeder:** How to obtain the basic machine; cycle-time tuning; whether parents remain in world while shown in machine. A one-day rest after egg collection is implemented provisionally; review the duration after playtesting.
10. **Eggs:** Distinct custom IDs are implemented for individualized breeder eggs as a prototype. Confirm persistence/transfer behavior and decide how managed eggs integrate with vanilla incubators.
11. **White/Black:** Behavior at sleep, festivals and multiplayer; whether time powers stack (recommend never).
12. **Gel Refinery:** Conversion recipes, slime fuel costs, progression unlock and cosmetic transmutation feasibility.
13. **Resource production:** How drops are collected, production interval, caps and exceptional outputs.
14. **Companion capacity:** One bound active slime at a time? Recommend one initially.

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

## Current decisions locked
Full custom breeder; external-only mod state; custom companion AI; single-player first; GPT-assisted sprite production. The breeder egg ID to external genome link is prototyped but still needs transfer/reload verification. Remaining design decisions include Pink recipe rules, exceptional acquisition, time-change safeguards, precise mutation and shop balancing.

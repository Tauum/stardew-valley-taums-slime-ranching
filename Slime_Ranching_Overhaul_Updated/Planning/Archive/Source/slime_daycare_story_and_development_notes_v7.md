# Slime Daycare — Design Restructure Notes

## Guiding vision — non-negotiable design principle

> **Aim for a mod that feels so naturally integrated into Stardew Valley that players wonder why the game never had a slime sanctuary in the first place.**

Treat this as the primary product vision and a decision-making test, **not** a claim that the feature exists in the base game.

- **Vanilla coherence:** Match Stardew Valley’s tone, pixel-art language, NPC characterisation, interface conventions, pacing, and sense of discovery. Preserve vanilla slimes and the Slime Hutch.
- **Purposeful features:** Every new system should reinforce caring for, breeding, protecting, or bonding with domesticated slimes—not add complexity merely for its own sake.
- **Low-pressure play:** The daycare should complement farming, mining, fishing, and relationships rather than impose mandatory daily chores. Breeding remains player-initiated; completed outcomes wait safely.
- **Accessible depth:** Make the first breeding experience straightforward, with optional deeper genetics, hybrids, rarity research, and collection goals.
- **Balanced companions:** One active slime companion at a time; abilities should complement, not obsolete, sprinklers, Junimo Huts, or other vanilla progression.
- **Integrated storytelling:** Use established NPC motivations and Stardew-like events; keep the Adventurer's Guild conflict nuanced rather than making established characters cartoon villains.
- **Scope filter:** When choosing between features, favour the option that makes the sanctuary feel more alive and more naturally part of Stardew over an isolated mechanic that adds friction.

**Review question for every feature:** *Would this feel like something ConcernedApe could plausibly have included in Stardew Valley, and does it make players care more about their slimes?*

## Purpose
Working design notes to merge into the existing Stardew Valley mod plan. These are proposed changes, not completed features. Preserve the already-working breeding and incubation logic while replacing the temporary Deluxe Barn presentation and potentially the placeable-machine UI.

## Current implementation and constraints
- The Deluxe Barn is only a temporary placeholder for the daycare building/interior; final exterior, interior dimensions and art are undecided.
- Breeding and incubation mechanics already work in some form: two parent slimes enter a breeder; breeding takes approximately 28 in-game days to produce an egg; incubation takes approximately 15 in-game days to hatch it.
- The breeder and incubator currently exist as placeable/pickup-able objects, but getting placement bounds and pickup/destruction behaviour right has been costly.
- Art and code are being produced with GPT assistance; consistent Stardew-style pixel art and small-scale animation remain risks.
- Vanilla slimes and the vanilla slime hutch should remain unchanged. The daycare is for a distinct domesticated slime gameplay loop.

## Recommended building layout
- Design a purpose-built daycare, with a large **main communal playroom** and small attached specialist areas on the **same interior map** (no door transitions required).
- A **breeding chamber** projects from the top wall of the main room. Initial footprint to test: **4×4 tiles**; also prototype 5×4 or 6×4 if walls/glass make the usable interior too cramped.
- Breeding chamber has a **glass front**, aquarium-style, so the two selected parent slimes are visible from the main room. Interacting with the glass or fixed breeding fixture opens the existing breeding UI.
- Parent slimes in the chamber can be lightweight **visual proxies**, driven by selected-parent data and simple idle animations rather than full wandering AI. Display an egg in a nest/tray when ready, if desired.
- A separate **incubation alcove/chamber** can use the same pattern: a fixed fixture with the existing incubation UI and visible egg state. Its footprint is still to be determined.
- Keep the underlying breeder/incubator services independent of map tiles and sprites. Replace the placeable objects' interaction adapters with fixed-position interaction points rather than rewriting game logic.
- Consider room expansions as unlocks/purchases, but do not commit to an upgrade progression until the base layout works.

## Furniture: configurable fixed stations
- Instead of unrestricted placement or purely invisible purchased upgrades, give the communal playroom a small number of **predefined activity slots**.
- Players choose which furniture to install in each slot and can swap or rearrange unlocked items without repurchasing them.
- Each slot has stable coordinates, approach/interaction points, clearance, and occupancy limits. Keep walkways clear; prevent other placements from blocking them.
- Candidate activities: trampoline (one slime bouncing), seesaw (two slimes synchronised), washing machine (spinning), swing (swinging), heated rock (multiple slimes gathering). Final list/art is not yet fixed.
- Functional equipment (breeder/incubator) is fixed to its own chamber; recreational furniture uses configurable stations.

## Slime activity behaviour (provisional)
- Slimes wander for about **2.5 in-game hours**.
- Then each has a **50% chance** to seek an available furniture activity; otherwise continue wandering.
- Prefer the nearest **reachable, compatible, unoccupied** station, rather than only nearest by straight-line distance.
- A successful activity lasts around **1.5–2 in-game hours**, then the slime resumes wandering.
- Maximum **three furniture uses per slime per day**.
- The generic slime controller handles wandering, choosing, approaching and leaving. Each station controls reservations, participants, visuals and cancellation.
- Handle full stations, unreachable stations, removed/swapped equipment, and day transitions safely. Two-slime seesaw requires reservation and synchronisation; heated rock can allow several participants.
- These timings and selection rules should be playtested, not treated as final balance.

## Suggested technical structure
- **BreedingService / IncubationService:** retain existing progress timers, parent/egg data, and save state.
- **FacilityInteraction:** map coordinate + action that opens the relevant UI.
- **ActivityStationDefinition:** slot ID, tile footprint, approach point(s), participant capacity, animation identifier, duration, eligibility.
- **ActivityManager:** discovers installed stations, handles availability/reservations and interaction lifecycle.
- **SlimeActivityState:** wandering → choosing → approaching → interacting → wandering, with recovery/cancel states.
- **VisualProxyRenderer:** draws parents in breeding chamber and egg in incubation chamber without spawning full roaming entities.
- Keep map positions and station definitions data-driven so the final map can change without rewriting behaviours.

## Development sequence
1. **Preserve working code:** branch or tag the existing breeder/incubator implementation and document current save data.
2. **Greybox the floor plan:** use placeholder tiles/rectangles, not finished artwork. Sketch the main playroom, top-wall breeding chamber, incubation chamber, corridors and provisional furniture slots.
3. **Validate the breeding chamber:** render two selected parents behind glass and open the existing breeding UI from a fixed interaction tile. Keep the 28-day breeding logic unchanged.
4. **Convert incubation:** fixed UI interaction and visual egg state; retain the 15-day timer.
5. **Build one generic station:** use a trampoline placeholder to validate navigation, reservation, activity timing and animation handoff.
6. **Add more furniture:** implement seesaw and other objects as configurations plus their specific animation controllers, not separate placement systems.
7. **Art pass:** finalise room sizes and station footprints first; then produce consistent pixel-art assets and refine animations.
8. **Expand gameplay:** companion abilities (watering, storage, debris clearing, discounts, rideable slime) should be staged separately after the core daycare loop is stable.

## Open decisions
- Final daycare exterior and interior dimensions; whether 4×4 is sufficient for the breeding chamber.
- Exact location/size of the incubation chamber and number of activity slots.
- Whether chamber upgrades are purchased and how they are unlocked.
- Whether player movement is permitted near/inside specialist chambers or only interaction from the main room.
- Art constraints: sprite/tile dimensions, palettes, animation frames and layering.
- How to handle unreachable furniture, paired interactions and saving active interactions.


## Breeding genetics and player-facing simplicity (updated discussion)
**Design intent, not necessarily implemented:** The player deliberately chooses two parent slimes. Base/purebred slime types have a recognisable colour and one associated ability. Crossing two different base types produces a **hybrid with both parents’ abilities**. Hybrids **cannot breed further** (mule-like rule); there is no multi-generation stacking of three or more abilities. Avoid describing the system as unrestricted genetics.

### Proposed type/ability examples
- **Blue:** irrigator; waters crops.
- **Red:** fighter; assists in combat.
- **Gold:** shop discount (earlier example: approximately 5–10%, not final).
- **Prismatic:** rideable companion.
- **Void:** item collection/storage companion.
- Other types and the full list of combinations remain undecided. Do not assume every colour pairing has final art or mechanics.

### Size and trade-offs
- Size is intended to modify efficiency/behaviour, **not simply rarity or power**. Example: a **large fighter** deals more damage but moves more slowly; a **small fighter** deals less damage but moves quickly.
- How size affects other abilities (especially passive shop discounts) is **undecided**. Avoid forcing an arbitrary size bonus onto every type.
- Colour, size, rarity and attributes are intended to be relevant to offspring; exact inheritance probabilities and whether rarity is inherited or derived are **not yet specified**.

### Clarity and onboarding risks
- The breeding loop differs from the familiar vanilla animal/slime systems: select two parents → wait ~28 days → collect egg → incubate ~15 days → hatch. Total is ~43 in-game days, potentially nearly two seasons. Timings are existing design values, **not validated balance**.
- The user specifically worries that **hybrids being unable to breed** may confuse players, especially younger players. Do not dismiss this as merely a documentation issue.
- Make the restriction visible **before** the player attempts breeding: hybrid status and a clear “Cannot breed” indicator in slime details and parent-selection UI; disable hybrid selection with a concise explanation. Consider a simple first-time visual example: **Blue + Red → Blue/Red hybrid (two abilities, cannot breed)**.
- Communicate parent types and expected ability inheritance in the breeding UI; keep any chance/rarity rules understandable and avoid requiring an external guide.
- Consider whether manual egg transfer adds meaningful engagement or needless friction; optional automatic transfer to an available incubator is an **idea to evaluate**, not a decided change.
- Key design goal: **depth through meaningful choices, not a large rulebook**. A player should be able to breed and raise slimes without mastering every combination.

### Outstanding breeding decisions
- Exact appearance of hybrids: blended colour, split colour, pattern, or another representation.
- Whether same-type parents produce a base slime, and whether hybrids always inherit both abilities or have exceptions.
- How offspring size, rarity and colour are rolled/inherited; how to communicate probabilities.
- Whether 28 + 15 days is enjoyable relative to the reward, and whether any upgrades shorten it.
- How to convey hybrid sterility diegetically and accessibly, without surprising players after a long wait.
- Which abilities are feasible, balanced, and visually readable at Stardew sprite scale.


## Rare-slime research laboratory and accumulating pity (latest decision)

**Status:** Proposed design agreed in discussion; not confirmed implemented.

### Facility and resource
- Add a small **research laboratory** as an upgradeable area attached to the daycare/breeding facilities, rather than a freely placeable machine.
- The player deposits the ordinary **slime resource obtained from slime balls**, not living slimes.
- Deposits may be made **at any time**, including during the 28-day breeding period; partial contributions accumulate.
- Slime is consumed when deposited. The laboratory stores the accumulated progress/bonus, not a retrievable pile of items.

### Rare outcomes
- Candidate rare slime types: **golden, void, shadow, and prismatic**. Their exact odds, availability, inheritance rules, and abilities still need design decisions.
- The laboratory increases the probability that the **offspring produced by breeding** is a rare type.
- **Illustrative tuning, not final balance:** every 100 slime deposited adds **one percentage point** to the rare-offspring chance (100 → +1%, 300 → +3%, etc.). This is a percentage-point increase, not a 1% relative multiplier.
- Make the accumulated chance visible in the laboratory and/or breeding UI. Distinguish the research bonus from any underlying base rarity chance.

### When the roll occurs
1. Select parents and begin the existing ~28-day breeding process.
2. Deposit slime into the laboratory whenever desired during the cycle.
3. **At breeding completion**, roll rarity once using the then-current accumulated chance (plus any defined base chance).
4. If the offspring is **not rare**, preserve the accumulated research bonus. The player can add more slime, increasing the chance for a future breeding attempt.
5. If the offspring **is rare**, reset the accumulated research bonus to zero.
6. The resulting egg proceeds through the existing ~15-day incubation; **rarity is already determined** and is not rolled again at hatching.

### Why this matters
- Failed attempts **do not erase invested slime or progress**: each additional contribution improves future odds, reducing the feeling of wasted effort.
- The player has a useful ongoing activity while waiting for breeding to finish.
- The reset happens **on successful rare offspring**, not after every attempt and not on a fixed timer. The bonus is therefore temporary but persists across failures.
- This is a **persistent accumulating pity/progression system**, not a permanent daycare upgrade.

### Important implementation and balance questions
- A rising chance alone does **not guarantee** a rare outcome unless the total chance eventually reaches 100% or a separate hard-pity threshold is defined. Decide whether to cap, guarantee, or otherwise handle the maximum.
- Define whether multiple simultaneous breeding chambers share one laboratory meter, and which completion consumes the bonus if several finish together.
- Persist deposited-progress counters and the resolved egg rarity in save data; avoid rerolling on reload or incubation.
- Define what happens when a breeding attempt is cancelled and whether already-deposited research progress remains.
- Clarify whether rare offspring can arise from any purebred pairing, how the rare result interacts with the two-parent hybrid rule, and whether rare types are themselves breedable.
- Reassess the proposed +1 percentage point per 100 slime against a ~28-day breeding cycle; the example is not yet playtested.

## Design principle
Prioritise the core loop — **breed → incubate → raise/bond → enjoy daycare activities → eventually unlock companions** — and separate gameplay logic from placement and rendering. The aim is a recognisable, customisable daycare with substantially fewer placement, collision and animation edge cases.

## Breeding facility upgrade progression (latest agreed design)

**Status:** Agreed design direction, not confirmed implemented. Timing values are proposed balance targets.

The basic daycare includes a functioning breeder and incubator; players do not need upgrades to complete the core breeding loop. The breeder has **three purchasable upgrades beyond the basic tier**:

| Facility stage | Breeding duration per pair | Simultaneous pairs | Visual/layout change |
| --- | ---: | ---: | --- |
| Basic | 28 days | 1 | Small, rudimentary glass-fronted breeding pen |
| Upgrade 1 | 21 days | 1 | Existing breeding apparatus grows and looks more advanced |
| Upgrade 2 | 14 days | 1 | Further enlargement and refinement of the apparatus |
| Upgrade 3 | 14 days | 2 | Add a second breeding chamber **and** enlarge the existing chamber by roughly 2–3 tiles (exact dimension and direction TBD); both pens use advanced-looking equipment |

The first two upgrades each remove seven days, i.e. 25% of the **original** 28-day duration, not 25% of the remaining duration. The third adds capacity rather than further reducing time. At full utilisation, theoretical breeding throughput rises from one egg per 28 days to two eggs per 14 days, or four times the basic throughput, before accounting for incubation limits.

### Spatial and visual requirements
- The breeding area remains a visible, glass-fronted extension of the daycare's main interior, not a separately loaded game location.
- The final upgrade should **both** introduce another pen/room and expand the original pen's footprint; do not implement it as a capacity number alone.
- Reserve the maximum intended expansion footprint during floor-plan design, while keeping unused space inaccessible or visually concealed at lower tiers.
- Keep the main approach/entrance and fixed interaction points as stable as practical across map variants. Store tier-specific coordinates in configuration rather than hardcoding them throughout breeding logic.
- Show selected parent slimes inside the pens using lightweight visual representations; reuse existing breeding logic rather than tying simulation to animated sprites.
- The larger, more sophisticated machinery should communicate progression visually. Construction timing and upgrade prices are not yet decided.

### Incubation alignment and open decisions
- A final incubator upgrade supporting **two simultaneous eggs** was discussed and positively received, to avoid bottlenecking two active breeding pens. Specific incubator tiers, incubation-time reductions, and visuals have **not** been agreed.
- Confirm whether the final layout adds two separate small chambers, one enlarged chamber with two pens, or two adjacent rooms; the current intent is a new chamber plus an enlarged existing one.
- Decide how active breeding cycles behave when an upgrade completes, particularly whether their remaining duration changes or only newly started cycles use the new tier.
- Ensure the rarity laboratory's persistent pity meter behaves deterministically if two eggs finish breeding on the same day.
- Validate that the final room expansion does not overlap fixed playroom furniture stations or block slime paths.

## Latest decisions: pacing, capacity, and coordinated facilities (28 October 2026)

**Status:** Design discussion; these supersede the earlier 28→21→14-day breeding proposal above. No code changes are implied.

### Agreed direction and proposed timing

- **Basic breeder:** 21 days per egg; **first speed upgrade:** 18 days; **second speed upgrade:** 14 days. These were accepted as preferable to 10-day breeding. The **third upgrade** adds a second simultaneous breeding pen rather than further reducing the duration. Both pens take 14 days each at this tier.
- **Incubation:** proposed **7 days**, deliberately shorter than breeding. The first complete breed-and-hatch cycle therefore takes **28 days** at basic tier, **25 days** after the first speed upgrade, and **21 days** after the second. These values remain playtest targets, not implemented settings.
- Incubation is a separate stage: an egg's properties, including rarity, are determined when breeding completes, not rerolled at hatching.
- With one breeder at 14 days and one incubator at 7 days, incubation has spare capacity. With two breeders at 14 days each, one 7-day incubator has *sufficient average throughput* for two eggs per 14 days, but simultaneous completions may require queuing. A second incubator is an attractive optional or bundled final expansion, not mathematically essential to average throughput.
- **Do not conflate first-hatch latency with steady-state throughput:** cycles can overlap after the first egg is produced. Two advanced pens yield at most two eggs per 14 days if continuously restarted; players are never obliged to restart.

### Existing mechanics to preserve

- The player **manually selects parents and starts breeding through an already implemented menu**. There is no passive or automatic restarting.
- When breeding finishes, **the parents automatically unload from the breeder**. Preserve that existing behaviour.
- Ensure completed eggs can be safely held pending incubation without loss; an egg collection tray/storage and notifications were suggested but are **not confirmed implemented or agreed**.
- No daily upkeep or punishment for delayed collection is desired. The daycare should complement farming, mining, fishing, friendships, and other Stardew activities rather than demand constant visits.

### Daycare expansion and population limits

- The user proposed **three building sizes/capacity tiers: 10 → 20 → 30 slimes**. Count **every slime owned by the daycare**, including those assigned to breeding pens, against the capacity.
- Physical daycare expansions should increase main-room floor area and **available fixed furniture stations**. Test movement, crowding, and performance with 30 placeholder slimes before finalising the art and map dimensions.
- Keep a clear distinction between **building expansions** (space, slime capacity, furniture slots, possibly an additional breeding chamber) and **breeding equipment improvements** (speed and visual sophistication). Exact dependency/bundling between the final second pen and building expansion remains to be specified.
- Breeding pens become physically larger and visually more advanced across upgrades; the final expansion adds a **second glass-fronted breeding room/pen** and enlarges the original by roughly 2–3 tiles. Reserve layout space for these changes.

### Progression and player experience

- The first usable offspring should arrive within about **one 28-day Stardew season** under the proposed baseline, not after the previous 35–43-day wait.
- Breeding is an **optional, opt-in activity**. Faster cycles offer opportunity, not obligation. Players can spend time on other game systems without losing progress.
- The user is concerned both about players forgetting excessively long cycles and about too many eggs demanding attention. Playtest perceived wait, collection friction, and late-tier egg backlog rather than assuming shorter timers always increase workload.
- The laboratory remains an **optional later facility**, accepting slime-resource deposits at any time, retaining accumulated rare-offspring probability across failed breeding attempts, and resetting the bonus after a successful rare offspring. It is separate from breeding-speed and incubation upgrades.

### Outstanding implementation decisions

1. Whether the second incubator is included with the final physical expansion, a separate upgrade, or unnecessary because one seven-day incubator has adequate average throughput.
2. Where completed eggs wait safely when incubation is busy, and what happens if the daycare population is already at capacity when an egg hatches.
3. Whether a slime in an egg counts toward the 10/20/30 cap (the user explicitly confirmed **all slimes** count, but egg handling requires a separate rule).
4. Whether breeding speed changes apply to in-progress cycles or only new ones.
5. The exact building upgrade costs, number of furniture slots per tier, room dimensions, and construction requirements.
6. How to resolve two simultaneous breeding completions against the shared laboratory pity meter deterministically.

**Documentation precedence:** This section supersedes earlier timing suggestions in this document (28/21/14-day breeder and 14/15-day incubation). Retain earlier sections for design rationale, but use **21/18/14-day breeding and 7-day incubation** as the latest proposed balancing targets.

## Upgrade system — consolidated specification (28 October 2026)

**Status:** Design proposal and agreed direction, not an implementation claim. This section consolidates the upgrade discussion and takes precedence over older upgrade examples.

| System | Starting state | Progression | Player-facing benefit |
|---|---|---|---|
| Breeding equipment | One basic glass-fronted pen; 21 days per egg | Speed upgrade 1: 18 days; speed upgrade 2: 14 days | Larger, more refined and technologically advanced pen artwork at each stage; faster breeding |
| Breeding capacity | One breeding pair at a time | Final expansion adds a **second pen** operating at 14 days, and expands the original pen/room by approximately 2–3 tiles | Two pairs breeding simultaneously, visibly separate in the facility |
| Incubation | One fixed incubator; proposed 7 days per egg | Potential second incubator alongside the final breeding expansion; exact purchase/bundle rule undecided | Avoids simultaneous-egg queues; no incubation speed upgrade currently required |
| Daycare building | Main playroom with capacity for 10 slimes | Expanded building tiers accommodate 20, then 30 slimes, with additional fixed furniture stations | More space for slimes, more play equipment, visibly expanded rooms |
| Rarity research laboratory | Optional/unavailable initially | Unlockable laboratory; deposit slime resource to increase rare-offspring chance; accumulated chance survives failures and resets on success | Long-term pursuit of golden, void, shadow, and prismatic slimes without permanent rarity upgrades |

### Upgrade architecture and dependencies

- Keep the **21 → 18 → 14 day** breeder progression distinct from daycare population-capacity upgrades, but coordinate the final **second breeding pen** with the appropriate physical building expansion.
- The final expansion should visibly add a new glass-fronted breeding chamber **and** enlarge the existing one; don't implement it as a silent capacity flag.
- The building's population limits are **10 / 20 / 30**, counting all living slimes owned by the daycare, including parents currently assigned to pens. Egg counting remains undecided.
- The second incubator is **not a settled requirement**: a single 7-day incubator can match the *average* output of two 14-day breeders, but not necessarily simultaneous egg arrivals. Bundling a second incubator with the final expansion is a candidate quality-of-life improvement.
- Incubation speed remains at **7 days** in the current proposal; do not invent additional incubator speed tiers.
- No upgrade prices, crafting materials, construction duration, prerequisite chains, exact room dimensions, or station counts have been finalised. Upgrades should require meaningful but achievable effort, without forcing players to purchase multiple expensive upgrades simultaneously just to use the daycare.
- Plan and reserve the final expansion footprint early. Store room variants and interaction coordinates as data, and keep the core breeding/incubation logic independent of artwork and furniture placement.
- Existing breeding menus and manual start remain unchanged. Parents unload automatically on completion; no automatic restart. Finished eggs must not be lost when incubators are busy.

### Recommended implementation checks

1. Test all breeding tiers and verify durations of 21, 18, and 14 days.
2. Test two 14-day pens finishing on the same day with one incubator and with two.
3. Verify upgrade construction preserves ongoing cycles, assigned parents, stored eggs, and the laboratory pity meter.
4. Enforce population caps on all living slimes, including slimes assigned to breeding, and define a safe policy for hatching at capacity.
5. Verify map expansion doesn't overlap furniture slots or invalidate slime paths.
6. Keep upgrade UI clear: display current tier, next benefit, capacity, cost, and prerequisites.



### Special overnight dream cutscene: the first slime split (new story direction)

- Present the **one-time split of the last domesticated slime** as a magical **overnight dream/cutscene**, inspired by the atmosphere and timing of Stardew Valley's fairy visiting the farm event. This is a reference for presentation, not a request to reuse the vanilla event's assets.
- After the single starter egg hatches and the as-yet-undecided trigger is met, fade into a short nighttime scene showing the domesticated slime surrounded by a gentle magical glow. It trembles or shimmers and **splits into two slimes**. Keep the moment mysterious, tender, and visually legible at Stardew's sprite scale.
- On waking, the player finds **two actual slimes** in the daycare; a short reaction or journal update directs them to **speak to the Wizard**. He is astonished and says he has never witnessed this phenomenon, establishing that it is **exceptional rather than standard slime reproduction**.
- The split should be **guaranteed once** for story progression, persist safely across saves, and not consume or modify the rarity laboratory's pity meter. Ensure both slimes are spawned exactly once and respect the daycare population rules.
- **One starter egg and one starter incubator remain sufficient**; the split supplies the second breeding parent. Normal breeding afterwards requires two slimes and follows the existing egg/incubation process.
- **Open decisions:** exact trigger (first night after hatch versus affection milestone), whether the scene is framed as a dream or an actual nighttime magical event, and how the original slime's identity, appearance, name, and affection carry over to the two resulting slimes. Avoid requiring a second parent to trigger the event.
- **Implementation note:** use a skippable event with a robust fallback if the player skips or misses the scene; never let the cinematic be the only code path that creates the second slime.

## Story revision: the last domesticated slime and the impossible split (latest decision)

**Status:** User-directed story revision. This section **supersedes the previous two-starter-eggs premise** in the story-driven onboarding section and its related pacing notes. Keep older material as historical context only, not as current implementation guidance.

- **Marnie possesses just one egg**, believed to contain **the last domesticated slime in Stardew Valley**. This makes protecting and raising it the emotional foundation of the sanctuary quest.
- After Lewis's warning, the Wizard's failed potion, the referral to Marnie, and Robin's construction of the daycare, Marnie entrusts the player with **one precious starter egg** and introduces the daycare and upstairs reference/collectibles desk.
- The **single basic incubator** hatches that one egg. **There is no need for two starter incubators** or simultaneous incubation as an onboarding workaround.
- **One-time extraordinary event:** When the resulting domesticated slime first undergoes the introductory breeding/reproduction event, **it splits into two slimes**. This is explicitly presented as **extremely rare and unprecedented**, not an ordinary rule for all slimes.
- The player reacts with surprise; quest updates to **“Speak to the Wizard.”** The Wizard examines or hears about the event and says **he has never witnessed anything like it**. His reaction should deepen the mystery rather than immediately explain it away.
- The two slimes now enable the **normal two-parent selective breeding loop** (purebreds, hybrids, eggs, incubator). The unusual split is a **story-specific exception**; subsequent breeding follows the established gameplay rules.
- **Avoid suggesting the player must already own two slimes to trigger the first split.** This special event needs its own explicit trigger or tutorial interaction so onboarding cannot deadlock.
- Update the milestones and quest flags accordingly: daycare built → Marnie hands over **one egg** → first hatch → **unique split into two** → Wizard investigation → regular first two-parent breeding → first offspring, etc.
- Rework Marnie's tutorial to avoid teaching the ordinary two-parent breeder *before* the player has a second slime, unless the game clearly frames it as something to unlock after the mysterious event.

### Decisions still to specify
- Does the split occur when the player puts the lone slime into the breeding pen, via a unique interaction, or as a scripted event after an affection/time milestone? It must not require a second parent.
- Does the split create two new slimes in place of the original, or does the original produce a second slime (visually splitting but retaining identity)? Decide how names, hearts, colour, size, traits, and population count persist.
- Is the split guaranteed as a **one-time scripted story event** (recommended for a reliable introduction), rather than an ordinary random rarity roll? If so, keep it independent of the laboratory pity meter.
- What does the Wizard's investigation unlock, and why does the last domesticated slime have this extraordinary ability?
- How do we ensure the initial population and capacity UI clearly show the change from one to two slimes?

## Story-driven onboarding: The Secret Slime Sanctuary (new preferred direction)

**Status:** User-proposed narrative and quest order; not implemented. This section **supersedes** the earlier Marnie-letter onboarding proposal. Exact dialogue, quest triggers, costs, and starter egg mechanics remain to be decided. Preserve the gameplay design elsewhere in these notes.

### Narrative premise
Mayor Lewis is concerned that continued slime hunting by adventurers is threatening the slime population in the Secret Woods. He asks the farmer to consult the Wizard. The Wizard believes slimes were once peaceful companions with hidden talents, but generations of fighting have made them defensive. His attempt to change the situation by giving a potion to a member of the Adventurer's Guild goes disastrously wrong (the precise mishap is TBD). Despondent for a moment, he recalls that Marnie has two slime eggs and Robin can construct a discreet sanctuary on the farm. The sanctuary must be kept secret from the Guild.

**Lore-fit refinement to consider:** Avoid establishing the Guild or Marlon as deliberately cruel or canonically responsible for extinction. Present the dispute as a misunderstanding between adventurers protecting villagers and the Wizard trying to restore trust; Lewis's population warning can be a local rumour or newly observed decline rather than a claim about vanilla canon. Define why the Secret Woods is at risk and why a farm building would stay hidden. The Wizard's failed potion should be a small, characterful comic incident, not necessarily a sinister act.

### Main quest sequence (proposed)
1. **Lewis visits the farm.** Concerned about the slime population in the Secret Woods and adventurers hunting them. New quest: **Speak to the Wizard**.
2. **Wizard consultation.** He recounts the slimes' formerly peaceful nature and secret talents. He devises a potion intended to influence a Guild member and stop the fighting.
3. **Potion mishap.** The plan fails spectacularly (cause and consequences to design). The Wizard briefly despairs: he cannot persuade the Guild to stop. He then remembers Marnie's two eggs and Robin's building skills. Quest updates: **Speak to Marnie**.
4. **Marnie's secret.** She agrees to entrust the player with the eggs but insists on a hidden, safe home first. Quest updates: **Ask Robin to build a Slime Daycare**.
5. **Construction.** Player purchases the **basic daycare** from Robin. Basic breeding pen and incubator are included, not separately purchased. Quest waits for construction completion. Cost and unlock prerequisites TBD.
6. **Marnie's farm visit.** After construction, Marnie visits, delivers/explains the two eggs, teaches the fundamental daycare systems, and directs the player to an **upstairs information desk**. The desk provides an in-world help/reference interface, current quest information, and a **collectibles/discovery tab**. Quest completion or follow-on task TBD.
7. **First hatch and first breeding.** Eggs must hatch before the two starter slimes can breed. With proposed 7-day incubation and 21-day base breeding, onboarding takes longer than if two living slimes were gifted; decide whether both eggs can incubate simultaneously in the basic building, or whether a special introductory hatch is warranted. Do not silently replace the user's **two eggs** with two living starter slimes.

### Proposed milestone visits and story interactions
These are suggestions to refine, not locked decisions. Prefer short, optional visits at significant achievements rather than repetitive interruptions.

- **First two starter slimes hatch:** Marnie checks on them and explains affection, daily care, and that slimes have different temperaments/abilities. The upstairs desk updates with a basic care entry.
- **First breeding attempt initiated:** Marnie explains choosing parents, waiting for an egg, and that parents return to the daycare when breeding completes. One-time contextual hint only.
- **First offspring hatches:** Marnie reacts to the newborn; the Wizard sends a brief note suggesting that inheritance may reveal long-forgotten slime talents. Unlock or point to the breeding discoveries page.
- **First hybrid discovered:** The Wizard visits or writes, excited by combined abilities; new desk entry explains the **base-type × base-type → non-breedable hybrid** rule. No requirement to discover every hybrid before progressing.
- **First rare slime obtained:** The Wizard examines the unusual result and suggests researching rare mutations. This is a natural narrative unlock for the **slime-contribution rarity laboratory**, if that laboratory is not already available. Preserve its accumulating pity and reset-on-success rules.
- **Daycare expanded to 20-slime capacity:** Robin comments on the need for more room and offers extra play stations; Marnie praises the sanctuary's growth. Avoid another lengthy tutorial.
- **Second breeding pen / final 30-slime expansion:** Robin unveils the second glass-fronted chamber and matching incubation provision. Marnie acknowledges that the sanctuary has become self-sustaining.
- **First max-heart companion:** Marnie encourages the player to take a trusted slime into the world; the Wizard hints at its hidden utility. The desk gains a companion-ability guide.
- **Optional narrative finale:** Lewis returns to see the sanctuary thriving. Consider a peaceful conversation or reconciliation with Marlon rather than permanently treating the Guild as villains. Protect the secrecy premise until the player deliberately resolves it.

### Strengths
- Strong inciting incident, identifiable characters, and a clear reason for the new building.
- Tutorial is motivated by story rather than a detached UI wall; the upstairs desk supports reference-on-demand.
- Existing Stardew roles align naturally: Lewis gives the civic concern, Wizard offers magical lore, Marnie handles creatures, Robin builds.
- Recurring visits reward player milestones and give the daycare an evolving identity.

### Weaknesses / design risks
- **Canon and character fit:** Adventurer's Guild as aggressor may conflict with players' perceptions; keep motivations sympathetic and avoid presenting invented lore as established canon.
- **Tone:** A serious extinction threat plus a bungled potion and a Wizard calling himself useless could feel tonally uneven. Keep the stakes personal/local and the joke brief.
- **Secrecy plausibility:** Explain how a large farm building and up to 30 slimes remain unknown to adventurers, and what happens when a companion travels outside.
- **Starter egg bottleneck:** Two eggs with a single seven-day incubator mean staggered starter hatches unless the base facility supports two eggs or a special initial arrangement. Make this decision explicitly.
- **Pacing:** A long quest plus construction, starter incubation, 21-day breeding, and 7-day offspring incubation may delay the core payoff. Consider completing the introduction at first starter hatch, with later breeding as a separate milestone.
- **Event burden:** Many scripted NPC visits require schedule/trigger checks, multiplayer handling, and mod compatibility. Use mail or desk updates for lower-priority milestones.
- **Player autonomy:** Story should introduce the daycare without repeatedly forcing cutscenes or making players feel obliged to maintain it.

### Implementation notes
- Model quest stages as durable flags: Lewis visit → Wizard visit → potion event → Marnie visit → building purchased → building complete → Marnie tutorial → first hatch → first breeding → first offspring. Events must be idempotent and resilient to skipped days and saves.
- NPC visits should only trigger at safe times/locations and be skippable; ensure quests remain recoverable if an event cannot run.
- The upstairs desk should be a fixed interaction with tabs for **Guide**, **Discoveries/Collectibles**, and optionally **Quest History**; avoid duplicating all tutorial dialogue.
- Consider milestone event priority and avoid multiple NPC events firing on the same day.
- Maintain separation between story unlocks and existing breeder/incubator logic, so quest edits don't corrupt breeding state.

### Open story questions
- What specifically happens when the Wizard gives the Guild member the potion, and who receives it?
- Are the two starter eggs of different base types, and can the starting incubator hold both at once?
- What unlocks Lewis's initial visit, and is the quest optional or mandatory to access construction?
- Does the rarity laboratory unlock after the first hybrid, first rare slime, or a building tier? Avoid circular prerequisites.
- How much should Marlon/the Guild know, and should the finale resolve the conflict?
- Are Marnie's milestone appearances in-person visits, letters, or a mixture?


### Narrative review and refinement context (discussion notes)

**These are editorial recommendations, not approved story changes.** Keep the original Lewis → Wizard → failed potion → Marnie → Robin → farm tutorial sequence as the working premise.

- **Why the premise works:** The daycare is motivated by an in-world conservation problem, rather than appearing as an unexplained purchasable building. The quest supplies a clear emotional reason to protect and raise slimes.
- **Character fit:** Lewis notices a community problem; the Wizard provides magical knowledge and a misguided solution; Marnie cares for the eggs and teaches husbandry; Robin handles construction. Their roles are recognisable to Stardew players.
- **Adventurer's Guild nuance:** Marlon and Gil are not normally portrayed as villains. A stronger conflict may be *competing interpretations*: the Guild believes it is defending villagers, while the Wizard believes slimes became hostile after being hunted. This is a **proposed interpretation for the mod**, not established game lore. The story can retain tension without depicting the Guild as knowingly cruel.
- **Potion scene:** This is an opportunity for memorable, character-driven comedy. Decide what the potion was supposed to accomplish, why the Wizard thought it would work, who receives it, and what harmless-but-disastrous outcome changes his plan. Keep the Wizard's self-doubt brief so he still feels capable and eccentric.
- **Tone:** A threatened population is relatively serious for the setting. Keep the threat local and the stakes hopeful, with the farmer building a sanctuary rather than confronting a cartoon villain.
- **Secrecy:** The premise needs an explanation for why a farm daycare is safer than the woods, especially once the building expands or companions follow the player. This may be a magical concealment effect, discretion among the NPCs, or a deliberate eventual reveal; **none is decided**.
- **Onboarding clarity:** Marnie's post-construction visit should teach only what is immediately actionable. The upstairs desk is the persistent reference for mechanics, discoveries, and collectibles, avoiding lengthy mandatory dialogue. Two starter **eggs**, rather than living slimes, remain the user's story choice.
- **Progressive story, not repeated tutorials:** Reserve in-person events for emotionally significant moments (initial hatching, first hybrid, first rare slime, major expansion, first companion, possible reconciliation). Use mail or desk updates for smaller milestones. Visits should be short, skippable, and not interrupt normal farming excessively.
- **Narrative payoff:** A potential finale is Lewis seeing the sanctuary flourish and the player helping the Wizard and Guild reach an understanding. This could complete the conservation theme without rewriting vanilla characters' personalities.
- **Key pacing risk:** Construction plus two starter hatches plus the first 21-day breeding and 7-day offspring incubation is a long introduction. Consider treating the first starter hatch as the onboarding completion, and the first bred offspring as a later achievement. Decide whether the initial incubator supports both starter eggs.
- **Implementation risk:** Scripted visits need persistent flags, safe triggers, multiplayer considerations, and fallback delivery so progression cannot become stuck if an event is missed.

**Next story-design decision:** Specify the potion mishap in one or two scenes; it should explain why the Wizard switches from trying to stop adventurers to protecting slimes through the daycare.

## Companion limit and vanilla-game balance (confirmed design decision)

**Core rule: The player may have only ONE active slime companion at a time.** The daycare may house many slimes (subject to its building-tier population cap), but only one can accompany the farmer and use companion abilities outside the daycare. This is a deliberate constraint to avoid replacing sprinklers, Junimo Huts, and other established Stardew Valley systems.

- Players raise a collection of specialised slimes and choose which single companion best suits their planned activity.
- Example roles: **irrigator** (limited nearby crop watering), **fighter** (combat support), **void** (portable storage / item collection), **prismatic** (rideable transportation), and **golden** (proposed shop discount). Exact effects and balance are still subject to design/testing.
- A **hybrid** may inherit two compatible base abilities, but still occupies the **single active companion slot**. Hybrids cannot breed further, per the existing breeding rules.
- The restriction creates an opportunity cost: selecting a farming helper means not simultaneously using a combat or storage helper. Individual abilities should still be bounded by range, frequency, capacity, or other constraints as appropriate.
- **Proposed refinement, not yet confirmed:** Companions can be changed only at the daycare, rather than swapped freely anywhere. This reinforces the daycare's role and makes the one-companion limit meaningful.
- **Implementation notes:** Enforce the limit centrally, including on load, multiplayer transitions, and when companions are dismissed or replaced; ensure inactive slimes do not execute field abilities. Define whether a rideable companion uses the same active slot (current recommendation: yes).
- **Player-facing clarity:** Explain the one-companion rule in the upstairs desk reference and companion selection UI; show the currently active companion clearly.

**Editorial balance assessment:** With only one active companion, the companion system fits Stardew Valley more naturally than an unrestricted collection of simultaneously active automation creatures. It remains important to balance each ability individually, particularly watering and resource collection, so it complements rather than supersedes vanilla progression.

**Continuity note:** Any older narrative discussion in this document referring to *two starter eggs* is superseded by the later **single last-domesticated-slime egg**, which hatches and subsequently splits into two during a unique overnight magical event.

## Release roadmap — phased delivery (added 2026-10-28)

**Purpose:** Deliver self-contained, playable milestones instead of treating the entire vision as a single release. Estimates are provisional for a solo software engineer using GPT assistance at roughly **15–20 hours/week**, and should be revisited after profiling actual progress. The breeding prototype is **already working in some form**, with genetics not yet fully implemented; do not treat it as unstarted work.

### Release 1 — The Sanctuary (initial public release)
**Indicative target:** ~3–5 months total for a polished initial release; revise after a vertical-slice assessment.

**Scope:**
- Lewis → Wizard → Marnie → Robin introductory quest, including construction of the initial daycare.
- Single starter egg, initial incubation, unique overnight magical splitting event, and follow-up Wizard quest.
- Fixed breeding chamber and incubator integrated into the daycare interior.
- Functional parent selection, breeding cycle, eggs, hatching, save/load persistence, and basic inheritance/colour/size rules.
- Basic collection/information desk and essential tutorials; simple, consistent visuals and limited idle animation.
- A small but complete gameplay loop with no requirement for the later rarity, companion, or playground systems.

**Exit criteria:** A player can complete the introductory story, obtain two slimes, breed and hatch offspring, save/reload safely, and understand the core mechanics without external documentation.

### Release 2 — The Living Daycare
**Indicative additional effort:** ~3–5 months after Release 1.

**Scope:**
- Building expansions, capacity progression (10 → 20 → 30 slimes), and more configurable fixed furniture stations.
- Breeder progression (proposed 21 → 18 → 14 days), visual equipment upgrades, and eventual second breeding pen.
- Incubator capacity aligned with breeding throughput; proposed seven-day incubation baseline.
- Rare-slime research laboratory with slime deposits, persistent accumulating odds across failed attempts, and reset upon rare success.
- Several recreational furniture interactions (start with one single-slime interaction before multi-slime seesaw), improved visuals, and relevant NPC milestone scenes.

**Exit criteria:** Expansions and upgrades persist correctly; slimes navigate to and interact with activity stations; no eggs or slimes are lost when equipment is busy; rarity progression behaves predictably and is explained in-game.

### Release 3 — Companionship
**Indicative additional effort:** ~4–8+ months after Release 2.

**Scope:**
- Affection and bonding progression, choosing **one active companion** at a time.
- Companion following, safe location transitions, and selected abilities (e.g., combat, irrigation, storage, riding), introduced and tested individually.
- Hybrid dual-ability combinations while retaining the single-companion limit and preventing hybrid rebreeding.
- Ability balancing against vanilla sprinklers, Junimo Huts, and other systems; more furniture and narrative payoffs as feasible.

**Exit criteria:** Companions remain reliable across saves, map transitions, combat, and farm routines; one-companion restriction is enforced; abilities feel useful without replacing vanilla progression.

### Cross-release safeguards and assumptions
- **Art and animation are the largest schedule uncertainty.** Generated assets still require manual validation for pixel scale, palette, alignment, frame consistency, and in-game readability.
- Maintain separation between breeding/genetics state, world presentation, UI, and furniture/companion behaviours; reuse the existing working prototype wherever possible.
- Treat the dates above as **planning ranges, not promises**. GPT can accelerate implementation but cannot guarantee polished art, compatibility, or defect-free integrations.
- Avoid scope creep: each release should be satisfying on its own. Defer features rather than shipping unstable or visually inconsistent systems.
- Confirm actual available weekly hours, save compatibility, multiplayer expectations, and external-mod compatibility before committing to a public schedule.

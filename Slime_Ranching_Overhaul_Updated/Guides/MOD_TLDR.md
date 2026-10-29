# Slime Ranching Overhaul — Mod TL;DR

This is a compact tour of the 2.0 mod: what each facility and furniture piece is for, how its systems connect, and which parts are still prototypes. It summarizes the project; the [2.0 development notes](../Planning/DEVELOPMENT_NOTES_2.0.md) remain the source of truth for approved rules, and the [player guide](PLAYER_GUIDE.md) and [feature walkthrough](../Planning/FEATURE_TESTING_WALKTHROUGH.md) contain player instructions and exact test steps.

## In one minute

The mod adds a separate Slime Daycare to a Stardew farm. The player raises domesticated Ranch Slimes, chooses when to breed them, incubates their individually identified eggs, researches rare offspring, and decorates a shared living space with enrichment furniture. Daycare upgrades add residents, rooms, and facilities. A read-only Registry, progression view, and Slime Wiki preview exist; the permanent Management Desk fixture and furniture editor are planned.

The current desk preview supports row paging and independent resident/Wiki detail scrolling with the mouse or controller when text exceeds its panel. Its Slime Wiki also explains the approved base/hybrid breeding outcomes, rare-slime and Crystal limits, and fixed egg result rules; the permanent fixture and Furniture Edit Mode remain pending.

The intended loop is:

1. Get Ranch Slimes established in the Daycare and learn their identities, genomes, age, and needs.
2. Choose two eligible residents and start a breeding cycle. The parents are reserved while the saved cycle runs.
3. Collect the fixed-genome egg, store it or incubate it, and use each incubator's separate Rare Lab to improve or resolve its outcome.
4. Make room for the hatchling, then continue breeding, decorating, researching mutations, and using higher-tier production facilities.
5. Let unassigned residents roam and select reachable enrichment stations for short, visual activities.

This is an in-progress 2.0 redesign. Several gameplay services and tester prototypes exist, but permanent facility placement, furniture slots and editing, the care fixtures, the full opening story, and all in-game acceptance checks are not complete. A behavior described below can be implemented in code and still need Taum's bulk in-game verification.

## Daycare progression

The Daycare remains an `AnimalHouse` but reuses Stardew's farmhouse maps and room art. Its four building states are:

| Daycare state | Native resident capacity | Facility progression |
| --- | ---: | --- |
| Basic | 10 | One 18-day Breeder cycle, one 7-day Incubator, and a broken Mutagenic Pool. No Egg CryoLab or Rare Lab. |
| First upgrade | 20 | New Breeder cycles take 14 days. Unlocks Daycare-specific farmhouse renovation choices, the Egg CryoLab, a Rare Lab per Incubator at 1% base chance, and the Robin-to-Maru Pool repair mission. |
| Second upgrade | 20 | Breeder cycles remain 14 days; no additional machine unlock is currently specified. |
| Final upgrade | 30 | Adds a second Breeder and Incubator, raises Rare Lab base chance to 5%, unlocks the Gel Refinery, and adds the cellar. |

The basic room uses `FarmHouse`, upgrade 1 uses `FarmHouse1`, and upgrades 2 and 3 use `FarmHouse2`; the final state also has the cellar. Interior renovation and wallpaper/flooring state belong to the Daycare, not the farmhouse. The saved tier and capacity behavior are implemented, but the complete building/map and facility fixture sequence still needs bulk acceptance.

## Machines and fixed facilities

| Facility | What it does | Current state and important limits |
| --- | --- | --- |
| **Slime Breeder** | Candidate rows identify each Ranch Slime's species or hybrid and its abilities. The player selects two eligible, mature Ranch Slimes; the preview shows possible offspring and abilities before starting. The cycle lasts 18 days in the basic Daycare and 14 days after the first upgrade. It reserves the parents, saves the job and resulting genome, then returns the parents to the Daycare when complete. The egg stays in the Breeder until collected. | Cycle, pairing, reservation, preview, and persistence logic are implemented. The large chamber and permanent map placement remain part of the fixed-facility integration. Current tester versions are movable. Removing a Breeder safely releases its pair and clears that machine's unfinished cycle. |
| **Slime Incubator** | Accepts a genome-specific Ranch Slime Egg, then hatches it after 7 in-game days. It tracks the egg's identity so the same egg cannot hatch twice. A full Daycare holds a ready hatch safely until space opens; the menu shows occupancy and reports the wait only while the room is full. | New jobs always use the fixed 7-day duration; existing jobs keep their saved ready day. Early cancellation requires confirmation, permanently retires that egg identity, and awards one vanilla Fried Egg. A Rare Lab result that has already been fixed cannot be canceled. The fixed machine fixture is pending. |
| **Rare Lab** | Each Incubator has its own chance, pity, contribution, and optional catalyst record. At incubation completion it rolls once and saves the rare-or-regular result before hatch creation. | It is currently linked to the Incubator as saved state; permanent adjacent lab fixtures remain pending. Chance is 0% at basic tier, 1% at upgrades 1–2, and 5% at the final tier. Contributing 10 vanilla Slime adds 1 percentage point, up to 100%. A failure adds 1 point of that lab's pity and reduces contribution by 1 point only when contribution exceeds 2%; at 2% or below, contribution carries forward. A success clears contribution and keeps pity. Without a catalyst, Gold, Void, and Prismatic are equally likely rare results. A Gold catalyst costs 10 Gold Bars, stays after a failed roll, and is consumed only by a successful rare roll. Other catalyst recipes are not approved. |
| **Egg CryoLab** | A chest-like holding facility for unused Ranch Slime Eggs. It stores eggs; it does not start incubation. The player takes an egg back out before loading it into an Incubator. | The tester version is a marked native Stardew Chest with 36 slots. It accepts only valid, unused Ranch Slime Eggs, checks duplicates across Daycares, and works only in an upgraded Daycare. Permanent fixture placement and its map position are pending. |
| **Mutagenic Pool** | After repair, it runs a nonlethal 2-day cosmetic mutation attempt on one eligible mature Ranch Slime. | The Pool begins broken. Robin can point the player to Maru after construction; the mission appears the next morning. Maru requests 1 Battery Pack, 5 Iron Bars, and 10 Refined Quartz. The Pool has a 10% base chance; each 10 Slime contributed during an attempt adds 1 point, with a 100% cap. Every failure adds 1 point of per-slime pity; success clears only contribution and retains pity. A failed attempt reduces contribution by 1 point only above 2%; canceling releases the slime without rolling or changing chance. Its chance state is independent from every Rare Lab. |
| **Gel Refinery** | Converts existing Stardew items using vanilla Slime as fuel and holds the output until collected. | It unlocks at the final Daycare tier. The current test prototype uses 1 Broken Glasses plus 10 Slime to produce 1 Refined Quartz over one in-game day; fuel and duration have GMCM/config controls. The broader recipe list and balance remain open. New batches are gated to the final tier, and the implementation uses recovery receipts around input/output changes. Permanent fixture integration remains pending. |
| **Water Bowls** | Planned fixed water amenities for residents. | Not implemented; their behavior and details remain open. They are not furniture slots. |
| **Feeding Trough** | The intended sole feeding facility, responsible for feeding and baseline happiness/heart progression. | Not implemented. It replaces the removed Compost Trough concept; there is no separate juvenile-growth station. It is a fixed amenity, not furniture. |

Chromatic Crystals are breeder inputs rather than machines or furniture. Their approved role is to focus an offspring on one of seven pure base identities; they cannot force a hybrid or rare result. They are currently a tester feature, and their normal acquisition and economy remain unresolved.

### Breeding and offspring rules

- Two matching base species produce that base species; two distinct base species produce their two-ability hybrid.
- Two parents of the exact same hybrid can produce that hybrid. A hybrid/base pair has two equally likely outcomes. Two distinct hybrids choose uniformly from their four base-contributor slots, including repeated contributors.
- Rare/exceptional species cannot breed. Chromatic Crystal focus is limited to a pure base result.
- Eggs carry their own fixed genome and unique identity. The hatching and cancellation guards prevent duplicate identities from producing duplicate slimes.

## Furniture, amenities, and animations

The intended reusable recipe catalog has three groups: Trampoline, Slide, Cushion, Seesaw, and Heated Slime Stone at Daycare unlock (2,000g each); Pond, Tunnel, Swing, and Lamp at the first building upgrade (5,000g each); Fairy Lights, Ball Pit, and Washing Machine at the final upgrade (10,000g each). Learning a recipe is meant to allow repeated placement. These prices and the **3 initial / 7 after the first upgrade / 15 after the final upgrade** fixed slots describe the approved design; the recipe and fixed-slot editor are not fully integrated. Current activity items are movable prototypes, and some extra items are tester-only.

| Furniture | Intended use and current animation/behavior |
| --- | --- |
| **Trampoline** | A slime compresses, bounces, and lands. Bounce height scales with the Ranch Slime's genome size. |
| **Cushion** | A gentle squash followed by a spring back. |
| **Slide** | A small hop and horizontal glide down the slide. |
| **Seesaw** | Paired activity. Two nearby slimes must independently pass their activity checks; they sway on opposite phases and share one activity lifetime. |
| **Ball Pit** | One slime repeatedly dips below the rim and pops back up. Tester-only movable prototype. |
| **Slime Lamp** | The placed object uses Stardew's native lamp lighting. Nearby slimes that independently select it can gather around its base together. Tester-only movable prototype. |
| **Tunnel** | A visual crawl-through, represented by a low hop/glide across its opening. |
| **Heated Slime Stone** | A low, resting pose beside the ember. A winter-comfort or friendship bonus is not active. |
| **Pond** | A short bobbing/swimming-like visual. It does not currently create drops or friendship effects. |
| **Swing** | A single slime sways on the seat. |
| **Fairy Lights** | A cosmetic light display intended to occupy one slot and affect the whole interior. A custom colored-light draw exists; exact timing and fixed placement still need acceptance/design work. |
| **Washing Machine** | A final-tier catalog concept with a proposed spin animation. Its role and animation are unresolved; it is not an active furniture item. |

All implemented activities use temporary draw offsets: Ranch Slime collision tiles and movement are not animated or repositioned. The scheduler staggers each slime's checks by 2–4 in-game hours, gives each eligible check an independent 50% activity chance, runs selected activities for 2–3 in-game hours, and caps each slime at four activities per day. A slime chooses a nearby, reachable, available compatible station; Seesaw reserves two participants together and Lamp gathering can include multiple independently selected slimes. Activities have no happiness, friendship, feeding, or breeding bonus. The scheduler respects Stardew's paused clock, uses Stardew's game-clock interval, and continues to validate/clean up interactions when a slime is reserved, removed, the station changes, the player leaves the room, or a new day/save transition occurs.

Mutagenic Pool appearances are also cosmetic: Starry twinkles; Spotted adds markings; Glowing adds a time-sensitive halo; Crystalline adds facets; Mossy adds a sprout; Bubbly releases bubbles; Mushroom adds a cap; Blooming adds a flower; Firefly adds two orbiting lights; Crowned adds a crown; Cowboy adds a hat; and Smelly releases odor wisps. The accents scale with slime size and do not grant abilities.

### Removed or legacy furniture

The separate Playground, Slime Bed, and Compost Trough are removed from active 2.0 use. Slimes sleep naturally in the Daycare, individual play pieces provide activities without a separate Playground, and the Feeding Trough owns the approved feeding role. Old item definitions/data remain registered so legacy saves can load; retained placed objects are inert. The older Compost Trough feed-day field remains readable for save compatibility.

## How the parts fit together

- **Residents:** Ranch Slimes are a separate custom farm-animal type using Stardew's native `FarmAnimal` identity and animal-house lifecycle. Each matures over 7 in-game days. The vanilla Slime Hutch, mine/wild slimes, and vanilla Slime Eggs remain untouched.
- **Jobs:** Breeding, incubation, Rare Lab state, Pool attempts, refinery batches, building progression, and story milestones are save-backed. A machine's menu is the player interface; its job record remains in save data so closing a menu or reloading does not restart a timer.
- **Identity:** Ranch Slime genome and lineage are stored with the animal in `modData`; each unique Ranch Slime Egg carries its genome and identity in item `modData`. Save-wide data refers to stable native IDs where needed. Legacy install-folder records are migration input only, protected by backup and retry-safe migration rules.
- **World behavior:** Farmhouse maps and art provide the Daycare shell while the location remains an `AnimalHouse`. Machines and furniture are custom Stardew big craftables or native object/chest forms, with custom services managing game-specific jobs. The Registry, progression view, and Wiki are a read-only Management Desk preview; permanent desk access and furniture editing are pending.
- **Player-facing progression:** Robin's building upgrades control capacity and machine unlocks; Marnie is intended to sell reusable furniture recipes and announce new recipe groups after upgrades. Maru repairs the Pool. The mandatory opening story that establishes the starter egg and two initial slimes is still incomplete, and some NPC trigger details remain undecided.
- **Companions:** A later release may add one active companion and abilities. Current ability labels and Wiki directions do not mean those effects are active in normal play.

## Designed inline as an extension of Stardew Valley

The project adds a Stardew system inside Stardew rather than replacing the game loop. It keeps the vanilla Slime Hutch and wild slime behavior intact, uses a native `AnimalHouse` with farmhouse maps, and builds Ranch Slimes on the game's farm-animal identity and persistence. Its furniture and facilities use Stardew object data, menus, item transfers, save lifecycles, time, pathfinding, lighting, and rendering conventions where those extension points fit.

SMAPI asset/data requests and game-loop/location events provide much of the integration. The activity scheduler uses Stardew's public pathfinder, on-screen helper, clock state, and real clock interval. The Lamp opts into the native lamp behavior. UI uses Stardew's clickable-menu flow. Targeted Harmony hooks also exist where data or events do not cover the integration needed by this prototype: examples include machine footprints/placement/removal presentation, breeder-parent movement and visibility, Daycare renovation/decoration, and unique egg stack handling. These hooks are scoped to specific native methods rather than changing all Slimes or animals. Decompiled game code is inspected read-only to confirm vanilla behavior; it is never modified or packaged in the mod.

The active technical boundaries are single-player for now, no global changes to vanilla Slimes, stable save migration, and separation of gameplay services from map coordinates, art, animation, and UI. Activity state is cleaned up at completion, reservation, station or resident removal, location/day change, and return to title; it is not saved as a live animation. The mod should continue to feel like a natural Stardew extension in tone, art, menus, pacing, and NPC behavior.

## Further reading

- [Canonical 2.0 development decisions](../Planning/DEVELOPMENT_NOTES_2.0.md)
- [Player guidance and tester commands](PLAYER_GUIDE.md)
- [Bulk in-game test walkthrough](../Planning/FEATURE_TESTING_WALKTHROUGH.md)

# Non-negotiable implementation contract

1. **Fully custom breeding.** Own selection, parent reservation, animation, cycle, genome outcome, egg output, hatching and cooldown. Reuse vanilla slime entities where possible but **not** vanilla reproduction rules.
2. **External mod state.** Do not write mod registry or genome data to the Stardew save or to entity/item `modData`. Normal FarmAnimal state (name, home, age, friendship and its native ID) is owned by Stardew and saved normally; the mod registry stays in the mod folder and links a Ranch Slime genome to that native ID. A distinct mod Common Slime Egg, Slime Egg Converter, and Field Guide are custom item references stored in the game save. Existing vanilla eggs are left alone; the converter consumes one only when used. Breeder eggs currently use distinct custom item IDs linked to external genome records; verify that link across stacking, transfers, and save/reload.
3. **Vanilla isolation.** The vanilla Slime Hutch and all vanilla GreenSlime monsters are untouched. Modded Ranch Slimes and their machines/breeding systems live in the separate Slime Daycare. Wild and mine slimes are never managed.
4. **Recoverability.** Snapshot preexisting hutch slimes without removing them; preserve versioned state in the installed mod folder. Users must back up that folder's `data` directory before replacing or removing the mod. Uninstall cannot execute cleanup code.
5. **Custom companion AI.** Own movement, navigation, behaviors, inventory and abilities. Do not replace with a superficial vanilla follower.
6. **AI-generated artwork.** Placeholder assets first; final generated assets must be converted into game-ready pixel sprite sheets and tested.
7. **Multiplayer deferred**, not quietly treated as supported.
8. **No feature deletion through staging.** All agreed slime types are core scope, but their powers come after systems are stable.

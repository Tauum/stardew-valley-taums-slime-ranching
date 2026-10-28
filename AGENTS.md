# Repository instructions

Before designing or implementing a mod feature, read [the 2.0 development notes](Slime_Ranching_Overhaul_Updated/Planning/DEVELOPMENT_NOTES_2.0.md). That document is the single source of product and development decisions.

For Stardew-specific behavior involving rendering, animation, pathfinding, buildings, events, or native entity lifecycles, follow its required workflow: inspect decompiled game code as read-only reference, identify vanilla behavior and supported SMAPI extension points, prefer supported APIs over Harmony, make the smallest testable change, and verify it in-game before continuing. Never modify or package the decompiled source.

Follow the notes' save-data and entity `modData` architecture, including its migration safeguards and unique-egg non-stacking rules. Keep design changes in the canonical notes rather than creating parallel design documents. Update the feature testing walkthrough with exact in-game steps for implemented features; put tester-facing commands and general player guidance in the player guide.

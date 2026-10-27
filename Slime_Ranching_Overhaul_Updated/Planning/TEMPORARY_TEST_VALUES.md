# Temporary Test Values

This file tracks temporary balance/debug values that must be removed or replaced with reviewed settings before release.

## Active

- **Ranch Slime petting friendship boost:** each successful pet temporarily grants 200 total friendship points instead of the normal small daily increase, so one full heart appears immediately in the Animals menu. This applies only to modded Ranch Slimes. Remove the patch after petting visuals and persistence are confirmed, then retest normal friendship progression.

## Removal checklist

- [ ] Remove `TemporaryPettingFriendshipBoostPatch` and its Harmony patch.
- [ ] Remove the temporary test entry from this list and restore the standard Ranch Slime friendship progression.
- [ ] Re-test that the Animals menu's petted indicator and friendship hearts persist after sleep/reload without the boost.

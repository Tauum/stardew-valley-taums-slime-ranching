# Temporary Test Values

This file tracks temporary balance/debug values that must be removed or replaced with reviewed settings before release.

## Active

None.

## Removed

- **Ranch Slime petting friendship boost** — removed in `temp/remove-visible-petting-heart-boost` after confirming the heart meter updated. It temporarily granted 200 friendship points per successful pet. Standard petting progression is restored in code.

## Removal checklist

- [x] Remove `TemporaryPettingFriendshipBoostPatch` and its Harmony patch.
- [x] Restore standard Ranch Slime friendship progression.
- [ ] Re-test that the Animals menu's petted indicator and friendship hearts persist after sleep/reload without the boost.

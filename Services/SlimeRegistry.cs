using System.Text.Json;
using StardewModdingAPI;
using StardewValley;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

internal enum SlimeResidentStatus
{
    Available,
    SelectedForBreeding,
    Breeding,
    MutagenicPool,
    IncubatorHatch,
    Companion
}

/// <summary>Domesticated Slime records linked to native FarmAnimal entities, plus active facility jobs.</summary>
internal sealed class SlimeRegistry
{
    private const int BreedingRestDays = 1;
    private const string SlimeRecordModDataKey = "Taum.SlimeRanching/record";
    private readonly IMonitor monitor;
    private readonly Dictionary<Guid, SlimeRecord> records = new();
    private readonly Dictionary<long, SlimeRecord> farmAnimalRecords = new();
    private readonly Dictionary<string, BreedingPairRecord> breedingPairs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SlimeEggRecord> eggs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IncubationRecord> incubations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RareLabRecord> rareLabs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RareLabContributionRecord> pendingRareLabContributions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RareLabCatalystTransactionRecord> pendingRareLabCatalystTransactions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DaycareFurnitureSlotRecord> furnitureSlots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MutagenicAttemptRecord> mutagenicAttempts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MutagenicContributionRecord> pendingMutagenicContributions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RefineryJobRecord> refineryJobs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RefineryTransactionRecord> pendingRefineryTransactions = new(StringComparer.Ordinal);
    private readonly HashSet<string> hatchedEggIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> destroyedEggIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IncubationRecord> pendingIncubationCancellations = new(StringComparer.Ordinal);
    private long? activeCompanionFarmAnimalId;
    private SlimeDaycareQuestRecord slimeDaycareQuest = new();
    private long nextMutagenicContributionSequence = 1;
    private long nextRareLabContributionSequence = 1;
    private long nextRareLabCatalystTransactionSequence = 1;
    private long nextRefineryTransactionSequence = 1;

    public SlimeRegistry(IMonitor monitor) => this.monitor = monitor;

    public int Count => this.records.Count;
    public IReadOnlyCollection<SlimeEggRecord> Eggs => this.eggs.Values;
    public IReadOnlyCollection<IncubationRecord> Incubations => this.incubations.Values;
    public IReadOnlyCollection<RareLabRecord> RareLabs => this.rareLabs.Values;
    public IReadOnlyCollection<RareLabContributionRecord> PendingRareLabContributions => this.pendingRareLabContributions.Values;
    public IReadOnlyCollection<RareLabCatalystTransactionRecord> PendingRareLabCatalystTransactions => this.pendingRareLabCatalystTransactions.Values;
    public IReadOnlyCollection<DaycareFurnitureSlotRecord> FurnitureSlots => this.furnitureSlots.Values;
    public IReadOnlyCollection<IncubationRecord> PendingIncubationCancellations => this.pendingIncubationCancellations.Values;
    public IReadOnlyCollection<BreedingPairRecord> BreedingPairs => this.breedingPairs.Values;
    public IReadOnlyCollection<MutagenicAttemptRecord> MutagenicAttempts => this.mutagenicAttempts.Values;
    public IReadOnlyCollection<MutagenicContributionRecord> PendingMutagenicContributions => this.pendingMutagenicContributions.Values;
    public IReadOnlyCollection<RefineryJobRecord> RefineryJobs => this.refineryJobs.Values;
    public IReadOnlyCollection<RefineryTransactionRecord> PendingRefineryTransactions => this.pendingRefineryTransactions.Values;

    public IReadOnlyCollection<long> DomesticatedSlimeFarmAnimalIds => this.farmAnimalRecords.Keys;
    public SlimeResidentStatus GetResidentStatus(long farmAnimalId, string? exceptMachineKey = null)
    {
        if (this.activeCompanionFarmAnimalId == farmAnimalId
            || (this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record) && record.IsCompanion))
            return SlimeResidentStatus.Companion;

        if (this.incubations.Values.Any(incubation => incubation.HatchAnimalId == farmAnimalId))
            return SlimeResidentStatus.IncubatorHatch;

        BreedingPairRecord? pair = this.breedingPairs.Values.FirstOrDefault(candidate =>
            candidate.OutputEggItemId is null && candidate.MachineKey != exceptMachineKey
            && (candidate.ParentAId == farmAnimalId || candidate.ParentBId == farmAnimalId));
        if (pair is not null)
            return pair.CycleReadyOnDay > 0 ? SlimeResidentStatus.Breeding : SlimeResidentStatus.SelectedForBreeding;

        if (this.mutagenicAttempts.Values.Any(attempt =>
                attempt.MachineKey != exceptMachineKey && attempt.SlimeId == farmAnimalId))
            return SlimeResidentStatus.MutagenicPool;

        return SlimeResidentStatus.Available;
    }

    public static string GetResidentStatusLabel(SlimeResidentStatus status) => status switch
    {
        SlimeResidentStatus.SelectedForBreeding => "Selected for breeding",
        SlimeResidentStatus.Breeding => "Breeding",
        SlimeResidentStatus.MutagenicPool => "Mutagenic Pool",
        SlimeResidentStatus.IncubatorHatch => "Incubator hatch",
        SlimeResidentStatus.Companion => "Companion",
        _ => "Available"
    };

    public bool IsBreedingParentReserved(long farmAnimalId) => this.breedingPairs.Values
        .Any(pair => pair.OutputEggItemId is null
            && (pair.ParentAId == farmAnimalId || pair.ParentBId == farmAnimalId));
    public bool IsInActiveBreedingCycle(long farmAnimalId) => this.breedingPairs.Values
        .Any(pair => pair.CycleReadyOnDay > 0 && pair.OutputEggItemId is null
            && (pair.ParentAId == farmAnimalId || pair.ParentBId == farmAnimalId));
    public IReadOnlyCollection<long> ReservedFarmAnimalIds => this.farmAnimalRecords.Keys
        .Where(farmAnimalId => this.IsReserved(farmAnimalId))
        .ToArray();
    public long? ActiveCompanionFarmAnimalId => this.activeCompanionFarmAnimalId;
    public SlimeDaycareQuestRecord SlimeDaycareQuest => this.slimeDaycareQuest;
    public bool IsSlimeDaycareQuestMilestoneCompleted(string milestone) =>
        SlimeDaycareQuestProgression.IsCompleted(this.slimeDaycareQuest, milestone);
    public bool TryCompleteSlimeDaycareQuestMilestone(string milestone) =>
        SlimeDaycareQuestProgression.TryComplete(this.slimeDaycareQuest, milestone);
    public bool RecoverSlimeDaycareQuestMilestoneFromDurableEvidence(string milestone) =>
        SlimeDaycareQuestProgression.RecoverFromDurableEvidence(this.slimeDaycareQuest, milestone);
    public bool RemoveSlimeDaycareQuestMilestone(string milestone) =>
        this.slimeDaycareQuest.CompletedMilestones.Remove(milestone);

    public BreedingPairRecord? GetBreedingPair(string machineKey) =>
        this.breedingPairs.TryGetValue(machineKey, out BreedingPairRecord? pair) ? pair : null;

    public int RemoveBreederPairsForMissingMachines(string locationPrefix, IEnumerable<string> placedMachineKeys)
    {
        HashSet<string> placed = placedMachineKeys.ToHashSet(StringComparer.Ordinal);
        BreedingPairRecord[] missingMachines = this.breedingPairs.Values
            .Where(pair => pair.MachineKey.StartsWith(locationPrefix, StringComparison.Ordinal)
                && !placed.Contains(pair.MachineKey))
            .ToArray();

        foreach (BreedingPairRecord pair in missingMachines)
        {
            this.breedingPairs.Remove(pair.MachineKey);
            // An egg which hasn't been collected is held by the breeder, so breaking
            // that machine discards its pending output along with the cycle.
            if (pair.OutputEggItemId is not null)
                this.eggs.Remove(pair.OutputEggItemId);
        }

        return missingMachines.Length;
    }

    public bool RemoveBreederPair(string machineKey)
    {
        if (!this.breedingPairs.Remove(machineKey, out BreedingPairRecord? pair))
            return false;

        // An uncollected egg is stored by its breeder. Removing that machine also
        // removes its pending result, while the parent genome records remain intact.
        if (pair.OutputEggItemId is not null)
            this.eggs.Remove(pair.OutputEggItemId);
        return true;
    }

    public bool TryGetSlimeRecord(long farmAnimalId, out SlimeRecord record) =>
        this.farmAnimalRecords.TryGetValue(farmAnimalId, out record!);

    public bool BindCompanion(long farmAnimalId)
    {
        if (this.activeCompanionFarmAnimalId is not null
            || !this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record)
            || !record.AgeClockInitialized
            || record.AgeDays < FarmAnimalContent.DomesticatedSlimeDaysToMature
            || this.IsReserved(farmAnimalId))
            return false;

        record.IsCompanion = true;
        this.activeCompanionFarmAnimalId = farmAnimalId;
        return true;
    }

    public bool UnbindCompanion(long farmAnimalId)
    {
        if (!this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record) || !record.IsCompanion)
            return false;

        record.IsCompanion = false;
        this.activeCompanionFarmAnimalId = null;
        return true;
    }

    public IncubationRecord? GetIncubation(string machineKey) =>
        this.incubations.TryGetValue(machineKey, out IncubationRecord? record) ? record : null;

    public RareLabRecord? GetRareLab(string machineKey) =>
        this.rareLabs.TryGetValue(machineKey, out RareLabRecord? record) ? record : null;

    public int GetRareLabChancePoints(string machineKey) =>
        RareLabRules.GetChancePoints(this.GetRareLab(machineKey));

    public bool UnlockRareLab(string machineKey, int baseChancePoints)
    {
        if (string.IsNullOrWhiteSpace(machineKey)
            || baseChancePoints is not (RareLabRules.FirstTierBaseChancePoints or RareLabRules.FinalTierBaseChancePoints))
            return false;

        return this.rareLabs.TryAdd(machineKey, new RareLabRecord
        {
            MachineKey = machineKey,
            IsUnlocked = true,
            BaseChancePoints = baseChancePoints
        });
    }

    public bool UpgradeRareLabsToFinalTier()
    {
        bool changed = false;
        foreach (RareLabRecord lab in this.rareLabs.Values)
        {
            if (lab.IsUnlocked && lab.BaseChancePoints != RareLabRules.FinalTierBaseChancePoints)
            {
                RareLabRules.SetFinalTier(lab);
                changed = true;
            }
        }

        return changed;
    }

    public bool SynchronizeRareLabs(IEnumerable<string> incubatorMachineKeys, int buildingTier)
    {
        if (buildingTier < 1)
            return false;

        bool changed = false;
        int baseChancePoints = buildingTier >= 3
            ? RareLabRules.FinalTierBaseChancePoints
            : RareLabRules.FirstTierBaseChancePoints;
        foreach (string machineKey in incubatorMachineKeys.Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(machineKey))
                continue;

            if (!this.rareLabs.TryGetValue(machineKey, out RareLabRecord? lab))
            {
                changed |= this.UnlockRareLab(machineKey, baseChancePoints);
                continue;
            }

            if (!lab.IsUnlocked)
            {
                lab.IsUnlocked = true;
                lab.BaseChancePoints = baseChancePoints;
                lab.PityPoints = 0;
                lab.ContributionPoints = 0;
                changed = true;
            }
            else if (baseChancePoints == RareLabRules.FinalTierBaseChancePoints
                && lab.BaseChancePoints != RareLabRules.FinalTierBaseChancePoints)
            {
                RareLabRules.SetFinalTier(lab);
                changed = true;
            }
        }

        if (buildingTier >= 3)
            changed |= this.UpgradeRareLabsToFinalTier();

        return changed;
    }

    public bool AddRareLabContribution(string machineKey)
    {
        return this.incubations.ContainsKey(machineKey)
            && this.rareLabs.TryGetValue(machineKey, out RareLabRecord? lab)
            && RareLabRules.AddContribution(lab);
    }

    public string? BeginRareLabContribution(string machineKey, int currentDay)
    {
        if (string.IsNullOrWhiteSpace(machineKey)
            || !this.incubations.TryGetValue(machineKey, out IncubationRecord? incubation)
            || incubation.HatchAnimalId is not null || currentDay >= incubation.ReadyOnDay
            || !this.rareLabs.TryGetValue(machineKey, out RareLabRecord? lab)
            || !lab.IsUnlocked || RareLabRules.GetChancePoints(lab) >= RareLabRules.MaximumChancePoints)
            return null;

        string transactionId = Guid.NewGuid().ToString("N");
        this.pendingRareLabContributions[transactionId] = new RareLabContributionRecord
        {
            Id = transactionId,
            MachineKey = machineKey,
            EggItemId = incubation.EggItemId,
            Sequence = this.nextRareLabContributionSequence,
            PreviousContributionPoints = lab.ContributionPoints
        };
        if (this.nextRareLabContributionSequence < long.MaxValue)
            this.nextRareLabContributionSequence++;
        return transactionId;
    }

    public bool ApplyRareLabContribution(string transactionId)
    {
        if (!this.pendingRareLabContributions.TryGetValue(transactionId, out RareLabContributionRecord? transaction)
            || transaction.ContributionApplied
            || !this.incubations.TryGetValue(transaction.MachineKey, out IncubationRecord? incubation)
            || incubation.EggItemId != transaction.EggItemId || incubation.HatchAnimalId is not null
            || !this.rareLabs.TryGetValue(transaction.MachineKey, out RareLabRecord? lab)
            || !RareLabRules.AddContribution(lab))
            return false;

        transaction.ContributionApplied = true;
        return true;
    }

    public bool RollbackRareLabContribution(string transactionId)
    {
        if (!this.pendingRareLabContributions.Remove(transactionId, out RareLabContributionRecord? transaction))
            return false;

        if (transaction.ContributionApplied
            && this.rareLabs.TryGetValue(transaction.MachineKey, out RareLabRecord? lab))
            lab.ContributionPoints = transaction.PreviousContributionPoints;
        return true;
    }

    public bool FinalizeRareLabContribution(string transactionId) =>
        this.pendingRareLabContributions.Remove(transactionId);

    public string? BeginRareLabCatalystTransaction(string machineKey, string catalystSpecies, int currentDay)
    {
        if (string.IsNullOrWhiteSpace(machineKey)
            || !RareLabRules.IsSupportedCatalystSpecies(catalystSpecies)
            || !this.rareLabs.TryGetValue(machineKey, out RareLabRecord? lab)
            || !lab.IsUnlocked || lab.CatalystSpecies is not null
            || this.pendingRareLabCatalystTransactions.Values.Any(transaction => transaction.MachineKey == machineKey))
            return null;

        if (this.incubations.TryGetValue(machineKey, out IncubationRecord? incubation)
            && (incubation.HatchAnimalId is not null || incubation.RareOutcomeResolved || currentDay >= incubation.ReadyOnDay))
            return null;

        string transactionId = Guid.NewGuid().ToString("N");
        this.pendingRareLabCatalystTransactions[transactionId] = new RareLabCatalystTransactionRecord
        {
            Id = transactionId,
            MachineKey = machineKey,
            Sequence = this.nextRareLabCatalystTransactionSequence,
            CatalystSpecies = catalystSpecies
        };
        if (this.nextRareLabCatalystTransactionSequence < long.MaxValue)
            this.nextRareLabCatalystTransactionSequence++;
        return transactionId;
    }

    public bool ApplyRareLabCatalystTransaction(string transactionId)
    {
        if (!this.pendingRareLabCatalystTransactions.TryGetValue(transactionId, out RareLabCatalystTransactionRecord? transaction)
            || transaction.CatalystApplied
            || !RareLabRules.IsSupportedCatalystSpecies(transaction.CatalystSpecies)
            || !this.rareLabs.TryGetValue(transaction.MachineKey, out RareLabRecord? lab)
            || !lab.IsUnlocked || lab.CatalystSpecies is not null
            || (this.incubations.TryGetValue(transaction.MachineKey, out IncubationRecord? incubation)
                && (incubation.HatchAnimalId is not null || incubation.RareOutcomeResolved)))
            return false;

        lab.CatalystSpecies = transaction.CatalystSpecies;
        transaction.CatalystApplied = true;
        return true;
    }

    public bool RollbackRareLabCatalystTransaction(string transactionId)
    {
        if (!this.pendingRareLabCatalystTransactions.Remove(transactionId, out RareLabCatalystTransactionRecord? transaction))
            return false;

        if (transaction.CatalystApplied
            && this.rareLabs.TryGetValue(transaction.MachineKey, out RareLabRecord? lab)
            && lab.CatalystSpecies == transaction.CatalystSpecies)
            lab.CatalystSpecies = null;
        return true;
    }

    public bool FinalizeRareLabCatalystTransaction(string transactionId) =>
        this.pendingRareLabCatalystTransactions.Remove(transactionId);

    public bool ResolveRareLabOutcome(string machineKey, bool rareOffspringProduced)
    {
        if (!this.incubations.ContainsKey(machineKey)
            || !this.rareLabs.TryGetValue(machineKey, out RareLabRecord? lab)
            || !lab.IsUnlocked)
            return false;

        RareLabRules.ApplyOutcome(lab, rareOffspringProduced);
        return true;
    }

    public bool ResolveIncubationOutcome(string machineKey, bool rareOffspringProduced, string? rareSpecies,
        out RareLabRecord? previousLab)
    {
        previousLab = null;
        if (!this.incubations.TryGetValue(machineKey, out IncubationRecord? incubation)
            || incubation.RareOutcomeResolved
            || !this.eggs.TryGetValue(incubation.EggItemId, out SlimeEggRecord? egg))
            return false;

        RareLabRecord? lab = this.GetRareLab(machineKey);
        if (rareOffspringProduced
            && (lab is not { IsUnlocked: true } || !RareLabRules.IsRareOffspringSpecies(rareSpecies)
                || (lab.CatalystSpecies is not null && lab.CatalystSpecies != rareSpecies)))
            return false;

        if (lab is { IsUnlocked: true })
        {
            previousLab = new RareLabRecord
            {
                MachineKey = lab.MachineKey,
                IsUnlocked = lab.IsUnlocked,
                BaseChancePoints = lab.BaseChancePoints,
                PityPoints = lab.PityPoints,
                ContributionPoints = lab.ContributionPoints,
                CatalystSpecies = lab.CatalystSpecies
            };
            RareLabRules.ApplyOutcome(lab, rareOffspringProduced);
        }

        incubation.RareOutcomeResolved = true;
        incubation.RareOffspringProduced = rareOffspringProduced;
        incubation.HatchGenome = rareOffspringProduced
            ? CreateRareOffspringGenome(incubation.EggGenome ?? egg.Genome, rareSpecies!)
            : null;
        return true;
    }

    public bool RollbackIncubationOutcome(string machineKey, RareLabRecord? previousLab)
    {
        if (!this.incubations.TryGetValue(machineKey, out IncubationRecord? incubation)
            || !incubation.RareOutcomeResolved)
            return false;

        incubation.RareOutcomeResolved = false;
        incubation.RareOffspringProduced = false;
        incubation.HatchGenome = null;
        if (previousLab is not null && this.rareLabs.TryGetValue(machineKey, out RareLabRecord? lab))
        {
            lab.IsUnlocked = previousLab.IsUnlocked;
            lab.BaseChancePoints = previousLab.BaseChancePoints;
            lab.PityPoints = previousLab.PityPoints;
            lab.ContributionPoints = previousLab.ContributionPoints;
            lab.CatalystSpecies = previousLab.CatalystSpecies;
        }
        return true;
    }

    public bool IsUnclaimedIncubationHatch(long farmAnimalId) =>
        this.incubations.Values.Any(incubation => incubation.HatchAnimalId == farmAnimalId);

    public string? GetGenomeSize(long farmAnimalId) =>
        this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record) ? record.Genome.Size : null;

    public bool SetGenomeSize(long farmAnimalId, string size)
    {
        if (!this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record)
            || !new[] { "Small", "Normal", "Large", "Giant" }.Contains(size, StringComparer.OrdinalIgnoreCase))
            return false;

        record.Genome.Size = new[] { "Small", "Normal", "Large", "Giant" }
            .First(value => value.Equals(size, StringComparison.OrdinalIgnoreCase));
        return true;
    }

    public bool SetGenomeShiny(long farmAnimalId, bool isShiny)
    {
        if (!this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record))
            return false;
        record.Genome.IsShiny = isShiny;
        return true;
    }

    public (string PrimarySpecies, string? SecondarySpecies, string VisualColor)? GetGenomeColorState(long farmAnimalId) =>
        this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record)
            ? (record.Genome.PrimarySpecies, record.Genome.SecondarySpecies, record.Genome.VisualColor)
            : null;

    public bool SetGenomeBaseColor(long farmAnimalId, string colorName)
    {
        if (!this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record)
            || !ChromaticCrystalContent.TryGetColorByName(colorName, out string canonicalName, out string colorHex))
            return false;

        record.Genome.PrimarySpecies = $"{canonicalName} Slime";
        record.Genome.SecondarySpecies = null;
        record.Genome.VisualColor = colorHex;
        SlimeAbilityCatalog.Refresh(record.Genome);
        return true;
    }

    public bool RestoreGenomeColorState(long farmAnimalId, (string PrimarySpecies, string? SecondarySpecies, string VisualColor) state)
    {
        if (!this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record))
            return false;

        record.Genome.PrimarySpecies = state.PrimarySpecies;
        record.Genome.SecondarySpecies = state.SecondarySpecies;
        record.Genome.VisualColor = state.VisualColor;
        return true;
    }

    public string? GetGenomeColor(long farmAnimalId) =>
        this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record) ? record.Genome.VisualColor : null;

    public bool GetGenomeIsShiny(long farmAnimalId) =>
        this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record) && record.Genome.IsShiny;

    public string? GetGenomeLikedFood(long farmAnimalId) =>
        this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record) ? record.Genome.LikedFoodItemId : null;

    public int GetMaturityAgeDays(long farmAnimalId, int fallbackAgeDays) =>
        this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record) && record.AgeClockInitialized
            ? Math.Clamp(record.AgeDays, 0, FarmAnimalContent.DomesticatedSlimeDaysToMature)
            : Math.Clamp(fallbackAgeDays, 0, FarmAnimalContent.DomesticatedSlimeDaysToMature);

    /// <summary>Sync the mod-owned age clock and mirror its value into the native animal field Stardew uses for maturity.</summary>
    public bool SynchronizeMaturity(IEnumerable<FarmAnimal> animals, int currentDay)
    {
        bool changed = false;
        foreach (FarmAnimal animal in animals)
        {
            if (animal.type.Value != FarmAnimalContent.DomesticatedSlimeTypeId
                || !this.farmAnimalRecords.TryGetValue(animal.myID.Value, out SlimeRecord? record))
                continue;

            if (!string.Equals(record.AnimalName, animal.Name, StringComparison.Ordinal))
            {
                record.AnimalName = animal.Name;
                changed = true;
            }

            if (!record.AgeClockInitialized)
            {
                record.AgeDays = Math.Clamp(animal.age.Value, 0, FarmAnimalContent.DomesticatedSlimeDaysToMature);
                record.AgeLastUpdatedDay = currentDay;
                record.AgeClockInitialized = true;
                changed = true;
            }
            else if (currentDay > record.AgeLastUpdatedDay)
            {
                int elapsedDays = currentDay - record.AgeLastUpdatedDay;
                record.AgeDays = Math.Min(FarmAnimalContent.DomesticatedSlimeDaysToMature, record.AgeDays + elapsedDays);
                record.AgeLastUpdatedDay = currentDay;
                changed = true;
            }

            int authoritativeAge = Math.Clamp(record.AgeDays, 0, FarmAnimalContent.DomesticatedSlimeDaysToMature);
            if (animal.age.Value != authoritativeAge)
            {
                animal.age.Value = authoritativeAge;
                changed = true;
            }
        }

        return changed;
    }

    public bool TryGetEgg(string itemId, out SlimeEggRecord egg) => this.eggs.TryGetValue(itemId, out egg!);

    public bool RegisterEggItem(SlimeEggRecord egg)
    {
        if (egg.Genome.Id == Guid.Empty || egg.EggId != egg.Genome.Id.ToString("N"))
            return false;
        if (this.hatchedEggIds.Contains(egg.EggId) || this.destroyedEggIds.Contains(egg.EggId))
            return false;

        egg.ItemId = SlimeEggContent.GetItemId(egg.Genome);
        if (this.eggs.TryGetValue(egg.EggId, out SlimeEggRecord? existing))
            return existing.Genome.Id == egg.Genome.Id && existing.HatchedAnimalId == egg.HatchedAnimalId;

        this.eggs.Add(egg.EggId, egg);
        return true;
    }

    public bool CanIncubate(string itemId) => this.eggs.TryGetValue(itemId, out SlimeEggRecord? egg)
        && egg.HatchedAnimalId is null
        && !this.hatchedEggIds.Contains(egg.EggId)
        && !this.destroyedEggIds.Contains(egg.EggId)
        && !this.incubations.Values.Any(record => record.EggItemId == itemId);

    public bool HasPendingIncubationCancellation(string eggId) => this.pendingIncubationCancellations.ContainsKey(NormalizeEggToken(eggId));

    public bool StartIncubation(string machineKey, string eggItemId, int currentDay)
    {
        if (string.IsNullOrWhiteSpace(machineKey) || this.incubations.ContainsKey(machineKey)
            || !this.CanIncubate(eggItemId))
            return false;

        this.incubations[machineKey] = new IncubationRecord
        {
            MachineKey = machineKey,
            EggItemId = eggItemId,
            EggGenome = this.eggs[eggItemId].Genome,
            StartedOnDay = currentDay,
            ReadyOnDay = currentDay + IncubatorRules.CycleDurationDays
        };
        return true;
    }

    public RefineryJobRecord? GetRefineryJob(string machineKey) =>
        this.refineryJobs.TryGetValue(machineKey, out RefineryJobRecord? job) ? job : null;

    public RefineryTransactionRecord? StartRefineryJob(string machineKey, int currentDay, int durationDays, int slimeFuelCount)
    {
        if (string.IsNullOrWhiteSpace(machineKey) || this.refineryJobs.ContainsKey(machineKey)
            || durationDays is < 1 or > 28
            || slimeFuelCount < GelRefineryRules.MinimumSlimeFuelCost
            || slimeFuelCount > GelRefineryRules.MaximumSlimeFuelCost)
            return null;

        var job = new RefineryJobRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            MachineKey = machineKey,
            InputItemId = "(O)170",
            InputCount = 1,
            SlimeFuelCount = slimeFuelCount,
            OutputItemId = "(O)338",
            OutputCount = 1,
            StartedOnDay = currentDay,
            ReadyOnDay = currentDay + durationDays
        };
        this.refineryJobs.Add(machineKey, job);
        return this.BeginRefineryTransaction(RefineryTransactionKind.Start, job);
    }

    public RefineryTransactionRecord? BeginRefineryCollection(string machineKey)
    {
        if (!this.refineryJobs.Remove(machineKey, out RefineryJobRecord? job))
            return null;

        return this.BeginRefineryTransaction(RefineryTransactionKind.Collect, job);
    }

    public bool ResolveRefineryTransaction(string transactionId, bool nativeSaveCommitted)
    {
        if (!this.pendingRefineryTransactions.Remove(transactionId, out RefineryTransactionRecord? transaction))
            return false;

        this.ApplyRefineryTransaction(transaction, nativeSaveCommitted);
        return true;
    }

    public bool ReconcileOrphanRefineryReceipt(RefineryTransactionRecord transaction)
    {
        if (!IsValidRefineryTransaction(transaction))
            return false;

        if (transaction.Kind == RefineryTransactionKind.Start)
        {
            if (this.refineryJobs.TryGetValue(transaction.MachineKey, out RefineryJobRecord? existing))
                return false;

            this.refineryJobs.Add(transaction.MachineKey, CloneRefineryJob(transaction.Job));
            return true;
        }

        if (this.refineryJobs.TryGetValue(transaction.MachineKey, out RefineryJobRecord? current)
            && current.Id == transaction.Job.Id)
            return this.refineryJobs.Remove(transaction.MachineKey);

        return false;
    }

    private RefineryTransactionRecord BeginRefineryTransaction(RefineryTransactionKind kind, RefineryJobRecord job)
    {
        string id = Guid.NewGuid().ToString("N");
        var transaction = new RefineryTransactionRecord
        {
            Id = id,
            Sequence = this.nextRefineryTransactionSequence,
            Kind = kind,
            MachineKey = job.MachineKey,
            Job = CloneRefineryJob(job)
        };
        if (this.nextRefineryTransactionSequence < long.MaxValue)
            this.nextRefineryTransactionSequence++;
        this.pendingRefineryTransactions.Add(id, transaction);
        return transaction;
    }

    private void ApplyRefineryTransaction(RefineryTransactionRecord transaction, bool nativeSaveCommitted)
    {
        if (transaction.Kind == RefineryTransactionKind.Start)
        {
            if (nativeSaveCommitted)
            {
                if (!this.refineryJobs.ContainsKey(transaction.MachineKey))
                    this.refineryJobs.Add(transaction.MachineKey, CloneRefineryJob(transaction.Job));
                return;
            }

            if (this.refineryJobs.TryGetValue(transaction.MachineKey, out RefineryJobRecord? current)
                && current.Id == transaction.Job.Id)
                this.refineryJobs.Remove(transaction.MachineKey);
            return;
        }

        if (nativeSaveCommitted)
        {
            if (this.refineryJobs.TryGetValue(transaction.MachineKey, out RefineryJobRecord? current)
                && current.Id == transaction.Job.Id)
                this.refineryJobs.Remove(transaction.MachineKey);
            return;
        }

        if (!this.refineryJobs.ContainsKey(transaction.MachineKey))
            this.refineryJobs.Add(transaction.MachineKey, CloneRefineryJob(transaction.Job));
    }

    private static RefineryJobRecord CloneRefineryJob(RefineryJobRecord job) => new()
    {
        Id = job.Id,
        MachineKey = job.MachineKey,
        InputItemId = job.InputItemId,
        InputCount = job.InputCount,
        SlimeFuelCount = job.SlimeFuelCount,
        OutputItemId = job.OutputItemId,
        OutputCount = job.OutputCount,
        StartedOnDay = job.StartedOnDay,
        ReadyOnDay = job.ReadyOnDay,
        ReadyNotificationSent = job.ReadyNotificationSent
    };

    private static bool IsValidRefineryJob(RefineryJobRecord? job) =>
        job is not null && Guid.TryParseExact(job.Id, "N", out _)
        && !string.IsNullOrWhiteSpace(job.MachineKey)
        && job.StartedOnDay >= 0 && job.ReadyOnDay >= job.StartedOnDay
        && job.InputItemId == "(O)170" && job.InputCount == 1 && job.SlimeFuelCount is >= 1 and <= 999_999
        && job.OutputItemId == "(O)338" && job.OutputCount == 1;

    private static bool IsValidRefineryTransaction(RefineryTransactionRecord? transaction) =>
        transaction is not null && Guid.TryParseExact(transaction.Id, "N", out _)
        && transaction.Sequence > 0
        && transaction.Kind is RefineryTransactionKind.Start or RefineryTransactionKind.Collect
        && !string.IsNullOrWhiteSpace(transaction.MachineKey)
        && IsValidRefineryJob(transaction.Job)
        && transaction.Job.MachineKey == transaction.MachineKey;

    public bool AssignHatchAnimalId(string machineKey, long animalId)
    {
        if (!this.incubations.TryGetValue(machineKey, out IncubationRecord? incubation)
            || !this.eggs.TryGetValue(incubation.EggItemId, out SlimeEggRecord? egg)
            || (incubation.HatchAnimalId is long existingId && existingId != animalId)
            || (egg.HatchedAnimalId is long hatchedId && hatchedId != animalId))
            return false;

        incubation.HatchAnimalId = animalId;
        egg.HatchedAnimalId = animalId;
        return true;
    }

    public bool ClearHatchAnimalId(string machineKey)
    {
        if (!this.incubations.TryGetValue(machineKey, out IncubationRecord? incubation)
            || !this.eggs.TryGetValue(incubation.EggItemId, out SlimeEggRecord? egg))
            return false;

        incubation.HatchAnimalId = null;
        egg.HatchedAnimalId = null;
        return true;
    }

    public int RecoverPendingHatches(IEnumerable<FarmAnimal> animals)
    {
        Dictionary<long, FarmAnimal> animalsById = animals
            .Where(animal => animal.type.Value == FarmAnimalContent.DomesticatedSlimeTypeId)
            .GroupBy(animal => animal.myID.Value)
            .ToDictionary(group => group.Key, group => group.First());
        int recovered = 0;
        foreach (IncubationRecord incubation in this.incubations.Values.ToArray())
        {
            if (incubation.HatchAnimalId is not long animalId || !animalsById.TryGetValue(animalId, out FarmAnimal? animal))
                continue;
            if (this.RegisterIncubationHatch(incubation, animal))
            {
                recovered++;
            }
        }

        return recovered;
    }

    public bool RegisterIncubationHatch(IncubationRecord incubation, FarmAnimal animal)
    {
        if (animal.type.Value != FarmAnimalContent.DomesticatedSlimeTypeId
            || incubation.HatchAnimalId != animal.myID.Value
            || !this.incubations.TryGetValue(incubation.MachineKey, out IncubationRecord? current)
            || !ReferenceEquals(current, incubation)
            || !this.eggs.TryGetValue(incubation.EggItemId, out SlimeEggRecord? egg)
            || egg.HatchedAnimalId != animal.myID.Value)
            return false;

        // Older builds could save a native hatch before storing a Rare Lab result.
        // The existing animal is authoritative, so preserve it as a regular hatch without rolling retroactively.
        if (!incubation.RareOutcomeResolved)
        {
            incubation.RareOutcomeResolved = true;
            incubation.RareOffspringProduced = false;
        }

        SlimeGenome hatchGenome = incubation.HatchGenome ?? egg.Genome;

        if (this.records.TryGetValue(hatchGenome.Id, out SlimeRecord? existing))
        {
            if (existing.FarmAnimalId != animal.myID.Value)
                return false;
        }
        else
        {
            var record = new SlimeRecord
            {
                Genome = hatchGenome,
                FarmAnimalId = animal.myID.Value,
                FarmAnimalTypeId = animal.type.Value
            };
            if (!this.TryAdd(record))
                return false;
        }

        return true;
    }

    public bool CompleteIncubation(string machineKey)
    {
        if (!this.incubations.Remove(machineKey, out IncubationRecord? record))
            return false;

        this.hatchedEggIds.Add(record.EggItemId);
        return true;
    }

    public bool CancelIncubation(string machineKey, out IncubationRecord cancelled)
    {
        cancelled = null!;
        if (!this.incubations.TryGetValue(machineKey, out IncubationRecord? record)
            || record.HatchAnimalId is not null || record.RareOutcomeResolved
            || this.hatchedEggIds.Contains(record.EggItemId)
            || this.destroyedEggIds.Contains(record.EggItemId))
            return false;

        this.incubations.Remove(machineKey);
        if (!this.destroyedEggIds.Add(record.EggItemId))
        {
            this.incubations[machineKey] = record;
            return false;
        }

        this.pendingIncubationCancellations.Add(record.EggItemId, record);
        cancelled = record;
        return true;
    }

    public bool RestoreCancelledIncubation(IncubationRecord record)
    {
        if (!this.pendingIncubationCancellations.TryGetValue(record.EggItemId, out IncubationRecord? pending)
            || !ReferenceEquals(pending, record))
            return false;

        if (this.incubations.ContainsKey(record.MachineKey)
            || !this.eggs.ContainsKey(record.EggItemId)
            || this.incubations.Values.Any(incubation => incubation.EggItemId == record.EggItemId))
            return false;

        this.pendingIncubationCancellations.Remove(record.EggItemId);
        this.destroyedEggIds.Remove(record.EggItemId);
        this.incubations.Add(record.MachineKey, record);
        return true;
    }

    public bool CommitIncubationCancellation(string eggId) =>
        this.pendingIncubationCancellations.Remove(NormalizeEggToken(eggId));

    public bool RestoreIncubation(IncubationRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.MachineKey)
            || this.incubations.ContainsKey(record.MachineKey)
            || !this.eggs.ContainsKey(record.EggItemId))
            return false;

        this.hatchedEggIds.Remove(record.EggItemId);
        this.incubations.Add(record.MachineKey, record);
        return true;
    }

    public bool IsReserved(long farmAnimalId, string? exceptMachineKey = null) =>
        this.GetResidentStatus(farmAnimalId, exceptMachineKey) != SlimeResidentStatus.Available;

    public MutagenicAttemptRecord? GetMutagenicAttempt(string machineKey) =>
        this.mutagenicAttempts.TryGetValue(machineKey, out MutagenicAttemptRecord? attempt) ? attempt : null;

    public int GetMutagenicPoolFailures(long slimeId) =>
        this.farmAnimalRecords.TryGetValue(slimeId, out SlimeRecord? record) ? Math.Clamp(record.MutagenicPoolFailures, 0, 9_999) : 0;

    public double GetMutagenicPoolContribution(long slimeId) =>
        this.farmAnimalRecords.TryGetValue(slimeId, out SlimeRecord? record)
            ? (double.IsFinite(record.MutagenicPoolContribution) ? Math.Clamp(record.MutagenicPoolContribution, 0, 1) : 0)
            : 0;

    public string? GetMutationTrait(long slimeId) =>
        this.farmAnimalRecords.TryGetValue(slimeId, out SlimeRecord? record) ? record.Genome.MutationTrait : null;

    public bool SetMutationTrait(long slimeId, string? mutationTrait)
    {
        if (!this.farmAnimalRecords.TryGetValue(slimeId, out SlimeRecord? record)
            || (mutationTrait is not null && !MutagenicPoolRules.IsApprovedMutation(mutationTrait)
                && mutationTrait != "Shadow Clone")
            || this.IsReserved(slimeId))
            return false;

        record.Genome.MutationTrait = mutationTrait;
        return true;
    }

    public bool StartMutagenicAttempt(string machineKey, long slimeId, int currentDay, int ageDays, double roll, MutagenicPoolRules rules)
    {
        if (string.IsNullOrWhiteSpace(machineKey) || this.mutagenicAttempts.ContainsKey(machineKey)
            || !this.farmAnimalRecords.TryGetValue(slimeId, out SlimeRecord? record)
            || ageDays < FarmAnimalContent.DomesticatedSlimeDaysToMature
            || !string.IsNullOrWhiteSpace(record.Genome.MutationTrait)
            || this.IsReserved(slimeId, machineKey))
            return false;

        double chance = rules.GetChance(record.MutagenicPoolFailures, record.MutagenicPoolContribution);
        this.mutagenicAttempts[machineKey] = new MutagenicAttemptRecord
        {
            MachineKey = machineKey,
            SlimeId = slimeId,
            StartedOnDay = currentDay,
            ReadyOnDay = currentDay + MutagenicPoolRules.DurationDays,
            ChanceAtStart = chance,
            Roll = Math.Clamp(roll, 0, 0.999999999)
        };
        return true;
    }

    public string? BeginMutagenicContribution(string machineKey, MutagenicPoolRules rules)
    {
        if (string.IsNullOrWhiteSpace(machineKey)
            || !this.mutagenicAttempts.TryGetValue(machineKey, out MutagenicAttemptRecord? attempt)
            || !this.farmAnimalRecords.TryGetValue(attempt.SlimeId, out SlimeRecord? record)
            || !string.IsNullOrWhiteSpace(record.Genome.MutationTrait)
            || rules.GetChance(record.MutagenicPoolFailures, record.MutagenicPoolContribution) >= MutagenicPoolRules.MaximumChance)
            return null;

        string transactionId = Guid.NewGuid().ToString("N");

        this.pendingMutagenicContributions[transactionId] = new MutagenicContributionRecord
        {
            Id = transactionId,
            MachineKey = machineKey,
            SlimeId = attempt.SlimeId,
            Sequence = this.nextMutagenicContributionSequence,
            PreviousContribution = Math.Clamp(record.MutagenicPoolContribution, 0, 1)
        };
        if (this.nextMutagenicContributionSequence < long.MaxValue)
            this.nextMutagenicContributionSequence++;
        return transactionId;
    }

    public bool ApplyMutagenicContribution(string transactionId, MutagenicPoolRules rules)
    {
        if (!this.pendingMutagenicContributions.TryGetValue(transactionId, out MutagenicContributionRecord? transaction)
            || !this.mutagenicAttempts.TryGetValue(transaction.MachineKey, out MutagenicAttemptRecord? attempt)
            || attempt.SlimeId != transaction.SlimeId
            || !this.farmAnimalRecords.TryGetValue(transaction.SlimeId, out SlimeRecord? record))
            return false;

        if (transaction.ContributionApplied)
            return false;

        record.MutagenicPoolContribution = Math.Min(1,
            record.MutagenicPoolContribution + MutagenicPoolRules.ContributionPoint);
        attempt.ChanceAtStart = rules.GetChance(record.MutagenicPoolFailures, record.MutagenicPoolContribution);
        transaction.ContributionApplied = true;
        return true;
    }

    public bool RollbackMutagenicContribution(string transactionId)
    {
        if (!this.pendingMutagenicContributions.Remove(transactionId, out MutagenicContributionRecord? transaction))
            return false;

        if (this.farmAnimalRecords.TryGetValue(transaction.SlimeId, out SlimeRecord? record))
            record.MutagenicPoolContribution = transaction.PreviousContribution;
        if (this.mutagenicAttempts.TryGetValue(transaction.MachineKey, out MutagenicAttemptRecord? attempt)
            && attempt.SlimeId == transaction.SlimeId
            && this.farmAnimalRecords.TryGetValue(transaction.SlimeId, out SlimeRecord? current))
            attempt.ChanceAtStart = MutagenicPoolRules.Instance.GetChance(current.MutagenicPoolFailures, current.MutagenicPoolContribution);
        return true;
    }

    public bool FinalizeMutagenicContribution(string transactionId) =>
        this.pendingMutagenicContributions.Remove(transactionId);

    public bool CancelMutagenicAttempt(string machineKey) =>
        !string.IsNullOrWhiteSpace(machineKey) && this.mutagenicAttempts.Remove(machineKey);

    public List<(long SlimeId, bool Success, double Chance)> ResolveReadyMutagenicAttempts(int currentDay)
    {
        var results = new List<(long SlimeId, bool Success, double Chance)>();
        MutagenicAttemptRecord[] ready = this.mutagenicAttempts.Values
            .Where(attempt => currentDay >= attempt.ReadyOnDay
                && this.farmAnimalRecords.ContainsKey(attempt.SlimeId))
            .ToArray();
        foreach (MutagenicAttemptRecord attempt in ready)
        {
            if (!this.farmAnimalRecords.TryGetValue(attempt.SlimeId, out SlimeRecord? slime))
                continue;

            bool success = attempt.Roll < attempt.ChanceAtStart;
            if (success)
            {
                double withinSuccessRange = Math.Clamp(attempt.Roll / Math.Max(attempt.ChanceAtStart, double.Epsilon), 0, 0.999999999);
                int mutationIndex = Math.Clamp((int)(withinSuccessRange * MutagenicPoolRules.MutationTraits.Count), 0, MutagenicPoolRules.MutationTraits.Count - 1);
                slime.Genome.MutationTrait = MutagenicPoolRules.MutationTraits[mutationIndex];
                slime.MutagenicPoolContribution = 0;
            }
            else
            {
                slime.MutagenicPoolFailures = Math.Min(9_999, Math.Clamp(slime.MutagenicPoolFailures, 0, 9_999) + 1);
                if (slime.MutagenicPoolContribution > 0.02)
                    slime.MutagenicPoolContribution = Math.Max(0, slime.MutagenicPoolContribution - MutagenicPoolRules.ContributionPoint);
            }
            results.Add((attempt.SlimeId, success, attempt.ChanceAtStart));
            this.mutagenicAttempts.Remove(attempt.MachineKey);
        }

        return results;
    }

    public int GetBreedingRestDays(long farmAnimalId, int currentDay) =>
        this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record)
            ? Math.Max(0, record.BreedingCooldownUntilDay - currentDay)
            : 0;

    public bool ReservePair(string machineKey, long parentAId, long parentBId, int currentDay)
    {
        if (parentAId == parentBId || string.IsNullOrWhiteSpace(machineKey)
            || !this.farmAnimalRecords.TryGetValue(parentAId, out SlimeRecord? parentA)
            || !this.farmAnimalRecords.TryGetValue(parentBId, out SlimeRecord? parentB)
            || !BreedingRules.IsBreedable(parentA.Genome) || !BreedingRules.IsBreedable(parentB.Genome)
            || this.GetBreedingRestDays(parentAId, currentDay) > 0 || this.GetBreedingRestDays(parentBId, currentDay) > 0
            || this.IsReserved(parentAId, machineKey) || this.IsReserved(parentBId, machineKey))
            return false;

        this.breedingPairs[machineKey] = new BreedingPairRecord
        {
            MachineKey = machineKey,
            ParentAId = parentAId,
            ParentBId = parentBId
        };
        return true;
    }

    public void ReleasePair(string machineKey) => this.breedingPairs.Remove(machineKey);

    public bool StartBreedingCycle(string machineKey, int currentDay, int durationDays, string? chromaticCrystalColor = null)
    {
        if (!this.breedingPairs.TryGetValue(machineKey, out BreedingPairRecord? pair) || pair.CycleReadyOnDay > 0
            || durationDays <= 0
            || (chromaticCrystalColor is not null && !ChromaticCrystalContent.IsColorName(chromaticCrystalColor)))
            return false;

        pair.CycleStartedOnDay = currentDay;
        pair.CycleReadyOnDay = currentDay + durationDays;
        pair.ChromaticCrystalColor = chromaticCrystalColor;
        return true;
    }

    public bool CancelBreedingCycle(string machineKey)
    {
        if (!this.breedingPairs.TryGetValue(machineKey, out BreedingPairRecord? pair)
            || pair.CycleReadyOnDay <= 0 || pair.OutputEggItemId is not null)
            return false;

        return this.breedingPairs.Remove(machineKey);
    }

    public SlimeEggRecord? GetOrCreateBreedingEgg(string machineKey, int currentDay, out bool created) =>
        this.GetOrCreateBreedingEgg(machineKey, currentDay, null, null, out created);

    public SlimeEggRecord? GetOrCreateBreedingEgg(string machineKey, int currentDay, string? parentAName, string? parentBName, out bool created)
    {
        created = false;
        if (!this.breedingPairs.TryGetValue(machineKey, out BreedingPairRecord? pair)
            || pair.CycleReadyOnDay <= 0 || currentDay < pair.CycleReadyOnDay)
            return null;

        if (pair.OutputEggItemId is not null)
        {
            ApplyBreedingRest(pair, pair.CycleReadyOnDay);
            if (!this.eggs.TryGetValue(pair.OutputEggItemId, out SlimeEggRecord? existing))
                return null;
            if (!string.IsNullOrWhiteSpace(parentAName) && string.IsNullOrWhiteSpace(existing.Genome.ParentAName))
                existing.Genome.ParentAName = parentAName;
            if (!string.IsNullOrWhiteSpace(parentBName) && string.IsNullOrWhiteSpace(existing.Genome.ParentBName))
                existing.Genome.ParentBName = parentBName;
            return existing;
        }

        if (!this.farmAnimalRecords.TryGetValue(pair.ParentAId, out SlimeRecord? parentA)
            || !this.farmAnimalRecords.TryGetValue(pair.ParentBId, out SlimeRecord? parentB))
            return null;

        SlimeGenome a = parentA.Genome;
        SlimeGenome b = parentB.Genome;
        IReadOnlyList<BreedingRules.Offspring> outcomes = BreedingRules.GetPossibleOffspring(a, b);
        if (outcomes.Count == 0)
            return null;
        BreedingRules.Offspring outcome = outcomes[Game1.random.Next(outcomes.Count)];
        var offspring = new SlimeGenome
        {
            Id = Guid.NewGuid(),
            ParentA = a.Id,
            ParentB = b.Id,
            ParentAName = string.IsNullOrWhiteSpace(parentAName) ? null : parentAName,
            ParentBName = string.IsNullOrWhiteSpace(parentBName) ? null : parentBName,
            Generation = Math.Max(a.Generation, b.Generation) + 1,
            PrimarySpecies = outcome.PrimarySpecies,
            SecondarySpecies = outcome.SecondarySpecies,
            VisualColor = outcome.VisualColor,
            Size = "Normal",
            IsShiny = false,
            RarityTier = "Common",
            MutationTrait = null,
            LikedFoodItemId = FoodLikeCatalog.GetRandomItemId()
        };

        if (pair.ChromaticCrystalColor is string crystalColor
            && ChromaticCrystalContent.TryGetColorByName(crystalColor, out string colorName, out string colorHex))
        {
            offspring.PrimarySpecies = $"{colorName} Slime";
            offspring.SecondarySpecies = null;
            offspring.VisualColor = colorHex;
            offspring.IsShiny = false;
            offspring.RarityTier = "Common";
            offspring.MutationTrait = null;
        }

        SlimeEggRecord egg = this.CreateEgg(offspring);
        pair.OutputEggItemId = egg.EggId;
        pair.OutputEggGenome = egg.Genome;
        ApplyBreedingRest(pair, currentDay);
        created = true;
        return egg;
    }

    private void ApplyBreedingRest(BreedingPairRecord pair, int completedOnDay)
    {
        int restingUntilDay = completedOnDay + BreedingRestDays;
        if (this.farmAnimalRecords.TryGetValue(pair.ParentAId, out SlimeRecord? parentA))
            parentA.BreedingCooldownUntilDay = Math.Max(parentA.BreedingCooldownUntilDay, restingUntilDay);
        if (this.farmAnimalRecords.TryGetValue(pair.ParentBId, out SlimeRecord? parentB))
            parentB.BreedingCooldownUntilDay = Math.Max(parentB.BreedingCooldownUntilDay, restingUntilDay);
    }

    private static string NormalizeSize(string? size) => size?.ToLowerInvariant() switch
    {
        "tiny" or "small" => "Small",
        "normal" => "Normal",
        "large" => "Large",
        "giant" => "Giant",
        _ => "Normal"
    };

    public bool FinishBreedingCycle(string machineKey, string eggItemId, int currentDay)
    {
        if (!this.breedingPairs.TryGetValue(machineKey, out BreedingPairRecord? pair)
            || pair.OutputEggItemId != eggItemId || !this.eggs.ContainsKey(eggItemId))
            return false;

        this.breedingPairs.Remove(machineKey);
        return true;
    }

    public void RestoreBreedingCycle(BreedingPairRecord pair, int currentDay)
    {
        this.breedingPairs[pair.MachineKey] = pair;
    }

    public int RegisterDomesticatedSlimes(IEnumerable<FarmAnimal> animals, SlimeGenome? genomeOverride = null)
    {
        int added = 0;
        foreach (FarmAnimal animal in animals)
        {
            if (animal.type.Value != FarmAnimalContent.DomesticatedSlimeTypeId)
                continue;

            SlimeRecord? entityRecord = ReadEntityRecord(animal);
            if (this.farmAnimalRecords.TryGetValue(animal.myID.Value, out SlimeRecord? existing))
            {
                if (entityRecord is not null && entityRecord.Genome.Id != existing.Genome.Id)
                {
                    if (this.records.TryGetValue(entityRecord.Genome.Id, out SlimeRecord? duplicate)
                        && duplicate.FarmAnimalId != animal.myID.Value)
                    {
                        this.monitor.Log($"Ignored Domesticated Slime modData for FarmAnimal {animal.myID.Value}: genome ID {entityRecord.Genome.Id:N} is already linked to another animal.", LogLevel.Warn);
                    }
                    else
                    {
                        this.records.Remove(existing.Genome.Id);
                        entityRecord.FarmAnimalId = animal.myID.Value;
                        entityRecord.FarmAnimalTypeId = animal.type.Value;
                        entityRecord.AnimalName = animal.Name;
                        this.records[entityRecord.Genome.Id] = entityRecord;
                        this.farmAnimalRecords[animal.myID.Value] = entityRecord;
                    }
                }
                else if (entityRecord is not null)
                {
                    CopyRecord(entityRecord, existing);
                }
                existing.AnimalName = animal.Name;
                if (entityRecord?.IsCompanion == true && this.activeCompanionFarmAnimalId is null)
                    this.activeCompanionFarmAnimalId = animal.myID.Value;
                continue;
            }

            if (entityRecord is not null)
            {
                entityRecord.FarmAnimalId = animal.myID.Value;
                entityRecord.FarmAnimalTypeId = animal.type.Value;
                entityRecord.AnimalName = animal.Name;
                if (entityRecord.IsCompanion && this.activeCompanionFarmAnimalId is null)
                    this.activeCompanionFarmAnimalId = animal.myID.Value;
                if (this.TryAdd(entityRecord))
                    added++;
                continue;
            }

            var record = new SlimeRecord
            {
                FarmAnimalId = animal.myID.Value,
                FarmAnimalTypeId = animal.type.Value,
                AnimalName = animal.Name,
                Genome = genomeOverride is null
                    ? new Models.SlimeGenome
                    {
                        Id = Guid.NewGuid(),
                        PrimarySpecies = "Domesticated Slime",
                        VisualColor = "#00FF00"
                    }
                    : genomeOverride
            };

            if (this.TryAdd(record))
                added++;
        }

        return added;
    }

    public void WriteEntityModData(IEnumerable<FarmAnimal> animals)
    {
        foreach (FarmAnimal animal in animals)
        {
            if (animal.type.Value != FarmAnimalContent.DomesticatedSlimeTypeId
                || !this.farmAnimalRecords.TryGetValue(animal.myID.Value, out SlimeRecord? record))
                continue;

            record.AnimalName = animal.Name;
            record.FarmAnimalId = animal.myID.Value;
            record.FarmAnimalTypeId = animal.type.Value;
            animal.modData[SlimeRecordModDataKey] = JsonSerializer.Serialize(record);
        }
    }

    private static SlimeRecord? ReadEntityRecord(FarmAnimal animal)
    {
        if (!animal.modData.TryGetValue(SlimeRecordModDataKey, out string? raw))
            return null;

        try
        {
            SlimeRecord? record = JsonSerializer.Deserialize<SlimeRecord>(raw);
            if (record?.Genome is not { Id: var id } || id == Guid.Empty)
                return null;

            record.Genome.Size = NormalizeSize(record.Genome.Size);
            record.LastCompostFeedDay = Math.Clamp(record.LastCompostFeedDay, -1, Math.Max(0, record.AgeLastUpdatedDay));
            return record;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void CopyRecord(SlimeRecord source, SlimeRecord target)
    {
        target.Genome = source.Genome;
        target.FarmAnimalId = source.FarmAnimalId;
        target.FarmAnimalTypeId = source.FarmAnimalTypeId;
        target.AnimalName = source.AnimalName;
        target.AgeDays = source.AgeDays;
        target.AgeLastUpdatedDay = source.AgeLastUpdatedDay;
        target.AgeClockInitialized = source.AgeClockInitialized;
        target.BreedingCooldownUntilDay = source.BreedingCooldownUntilDay;
        target.MutagenicPoolFailures = source.MutagenicPoolFailures;
        target.MutagenicPoolContribution = source.MutagenicPoolContribution;
        target.LastCompostFeedDay = source.LastCompostFeedDay;
        target.EnrichmentActivityDay = source.EnrichmentActivityDay;
        target.EnrichmentActivitiesToday = source.EnrichmentActivitiesToday;
        target.NextEnrichmentCheckMinute = source.NextEnrichmentCheckMinute;
        target.LastHutchName = source.LastHutchName;
        target.IsCompanion = source.IsCompanion;
    }

    /// <summary>Detach removed animals while keeping their genome archive; abandon only cycles with no saved egg.</summary>
    public (int DetachedAnimals, int CancelledCycles) ReconcileDomesticatedSlimes(IEnumerable<FarmAnimal> animals)
    {
        HashSet<long> liveIds = animals
            .Where(animal => animal.type.Value == FarmAnimalContent.DomesticatedSlimeTypeId)
            .Select(animal => animal.myID.Value)
            .ToHashSet();

        SlimeRecord[] missing = this.farmAnimalRecords
            .Where(pair => !liveIds.Contains(pair.Key))
            .Select(pair => pair.Value)
            .ToArray();
        foreach (SlimeRecord record in missing)
        {
            if (record.FarmAnimalId is long id)
                this.farmAnimalRecords.Remove(id);
            record.FarmAnimalId = null;
            record.FarmAnimalTypeId = null;
        }

        foreach (string machineKey in this.mutagenicAttempts.Values
                     .Where(attempt => !this.farmAnimalRecords.ContainsKey(attempt.SlimeId))
                     .Select(attempt => attempt.MachineKey).ToArray())
            this.mutagenicAttempts.Remove(machineKey);

        string[] abandonedCycles = this.breedingPairs.Values
            .Where(pair =>
            {
                bool missingParent = !this.farmAnimalRecords.ContainsKey(pair.ParentAId)
                    || !this.farmAnimalRecords.ContainsKey(pair.ParentBId);
                bool missingEgg = pair.OutputEggItemId is not null && !this.eggs.ContainsKey(pair.OutputEggItemId);
                return missingParent && (pair.OutputEggItemId is null || missingEgg);
            })
            .Select(pair => pair.MachineKey)
            .ToArray();
        foreach (string machineKey in abandonedCycles)
            this.breedingPairs.Remove(machineKey);

        if (this.activeCompanionFarmAnimalId is long companionId && !this.farmAnimalRecords.ContainsKey(companionId))
            this.activeCompanionFarmAnimalId = null;

        return (missing.Length, abandonedCycles.Length);
    }

    public void Load(SlimeSaveData? data, bool logSummary)
    {
        this.records.Clear();
        this.farmAnimalRecords.Clear();
        this.breedingPairs.Clear();
        this.eggs.Clear();
        this.incubations.Clear();
        this.rareLabs.Clear();
        this.pendingRareLabContributions.Clear();
        this.pendingRareLabCatalystTransactions.Clear();
        this.furnitureSlots.Clear();
        this.mutagenicAttempts.Clear();
        this.pendingMutagenicContributions.Clear();
        this.nextMutagenicContributionSequence = 1;
        this.nextRareLabContributionSequence = 1;
        this.nextRareLabCatalystTransactionSequence = 1;
        this.refineryJobs.Clear();
        this.pendingRefineryTransactions.Clear();
        this.nextRefineryTransactionSequence = 1;
        this.hatchedEggIds.Clear();
        this.destroyedEggIds.Clear();
        this.pendingIncubationCancellations.Clear();
        this.activeCompanionFarmAnimalId = data?.ActiveCompanionFarmAnimalId;
        this.slimeDaycareQuest = SlimeDaycareQuestProgression.Normalize(data?.SlimeDaycareQuest);
        foreach (string eggId in data?.HatchedEggIds ?? Enumerable.Empty<string>())
        {
            string normalizedEggId = NormalizeEggToken(eggId);
            if (Guid.TryParseExact(normalizedEggId, "N", out _))
                this.hatchedEggIds.Add(normalizedEggId);
        }
        foreach (string eggId in data?.DestroyedEggIds ?? Enumerable.Empty<string>())
        {
            string normalizedEggId = NormalizeEggToken(eggId);
            if (Guid.TryParseExact(normalizedEggId, "N", out _))
                this.destroyedEggIds.Add(normalizedEggId);
        }
        foreach (IncubationRecord record in data?.PendingIncubationCancellations ?? Enumerable.Empty<IncubationRecord>())
        {
            if (record is null || string.IsNullOrWhiteSpace(record.EggItemId) || string.IsNullOrWhiteSpace(record.MachineKey))
                continue;
            record.EggItemId = NormalizeEggToken(record.EggItemId);
            if (record.HatchAnimalId is null && this.destroyedEggIds.Contains(record.EggItemId))
                this.pendingIncubationCancellations.TryAdd(record.EggItemId, record);
        }
        if (data is not null && data.SchemaVersion != SlimeSaveData.CurrentSchemaVersion)
            throw new InvalidOperationException($"Unsupported slime data schema {data.SchemaVersion}; expected {SlimeSaveData.CurrentSchemaVersion}.");

        int assignedFoodLikes = 0;
        foreach (SlimeRecord record in data?.Slimes ?? Enumerable.Empty<SlimeRecord>())
        {
            if (record?.Genome is { } genome)
            {
                genome.Size = NormalizeSize(genome.Size);
                record.LastCompostFeedDay = Math.Clamp(record.LastCompostFeedDay, -1, Math.Max(0, record.AgeLastUpdatedDay));
            }
            bool alreadyLikedFood = FoodLikeCatalog.IsEligible(record?.Genome?.LikedFoodItemId);
            if (!this.TryAdd(record))
            {
                this.monitor.Log("Skipped a malformed or duplicate external slime record.", LogLevel.Warn);
                continue;
            }

            if (!alreadyLikedFood)
                assignedFoodLikes++;
        }

        foreach (MutagenicContributionRecord transaction in data?.PendingMutagenicContributions ?? Enumerable.Empty<MutagenicContributionRecord>())
        {
            if (transaction is null || !Guid.TryParseExact(transaction.Id, "N", out _)
                || string.IsNullOrWhiteSpace(transaction.MachineKey)
                || transaction.Sequence < 0
                || !double.IsFinite(transaction.PreviousContribution) || transaction.PreviousContribution is < 0 or > 1
                || !this.farmAnimalRecords.ContainsKey(transaction.SlimeId))
            {
                this.monitor.Log("Skipped a malformed Mutagenic Pool contribution transaction.", LogLevel.Warn);
                continue;
            }
            this.pendingMutagenicContributions.TryAdd(transaction.Id, transaction);
            if (transaction.Sequence >= this.nextMutagenicContributionSequence && transaction.Sequence < long.MaxValue)
                this.nextMutagenicContributionSequence = transaction.Sequence + 1;
        }

        foreach (BreedingPairRecord pair in data?.BreedingPairs ?? Enumerable.Empty<BreedingPairRecord>())
        {
            if (pair is null || string.IsNullOrWhiteSpace(pair.MachineKey)
                || pair.ParentAId == pair.ParentBId || this.IsReserved(pair.ParentAId, pair.MachineKey)
                || this.IsReserved(pair.ParentBId, pair.MachineKey))
                continue;
            if (pair.OutputEggItemId is not null)
                pair.OutputEggItemId = NormalizeEggToken(pair.OutputEggItemId);
            this.breedingPairs[pair.MachineKey] = pair;
        }

        foreach (SlimeEggRecord egg in data?.Eggs ?? Enumerable.Empty<SlimeEggRecord>())
        {
            if (egg?.Genome is not null)
                egg.Genome.Size = NormalizeSize(egg.Genome.Size);
            if (egg?.Genome is not { Id: var id } || id == Guid.Empty)
            {
                this.monitor.Log("Skipped a malformed or duplicate external slime egg record.", LogLevel.Warn);
                continue;
            }

            egg.EggId = id.ToString("N");
            egg.ItemId = SlimeEggContent.GetItemId(egg.Genome);
            if (egg.HatchedAnimalId is not null)
                this.hatchedEggIds.Add(egg.EggId);
            if (!this.eggs.TryAdd(egg.EggId, egg))
                this.monitor.Log("Skipped a malformed or duplicate external slime egg record.", LogLevel.Warn);
        }

        foreach (IncubationRecord incubation in data?.Incubations ?? Enumerable.Empty<IncubationRecord>())
        {
            if (incubation is null || string.IsNullOrWhiteSpace(incubation.EggItemId))
            {
                this.monitor.Log("Skipped a malformed Domesticated Slime incubation record.", LogLevel.Warn);
                continue;
            }

            incubation.EggItemId = NormalizeEggToken(incubation.EggItemId);
            if (incubation.EggGenome is null && this.eggs.TryGetValue(incubation.EggItemId, out SlimeEggRecord? migratedEgg))
                incubation.EggGenome = migratedEgg.Genome;
            if (incubation.EggGenome is not null && !this.eggs.ContainsKey(incubation.EggItemId))
            {
                var heldEgg = new SlimeEggRecord
                {
                    EggId = incubation.EggItemId,
                    ItemId = SlimeEggContent.GetItemId(incubation.EggGenome),
                    Genome = incubation.EggGenome,
                    HatchedAnimalId = incubation.HatchAnimalId
                };
                this.eggs[heldEgg.EggId] = heldEgg;
            }
            if (this.eggs.TryGetValue(incubation.EggItemId, out SlimeEggRecord? heldRecord))
                heldRecord.HatchedAnimalId = incubation.HatchAnimalId;
            if (string.IsNullOrWhiteSpace(incubation.MachineKey)
                || incubation.StartedOnDay < 0 || incubation.ReadyOnDay < incubation.StartedOnDay
                || !this.eggs.TryGetValue(incubation.EggItemId, out SlimeEggRecord? egg)
                || incubation.HatchAnimalId != egg.HatchedAnimalId
                || (incubation.RareOffspringProduced
                    && (!incubation.RareOutcomeResolved || incubation.HatchGenome is null
                        || incubation.HatchGenome.Id == Guid.Empty
                        || incubation.HatchGenome.Id == egg.Genome.Id
                        || !RareLabRules.IsRareOffspringSpecies(incubation.HatchGenome.PrimarySpecies)
                        || incubation.HatchGenome.SecondarySpecies is not null))
                || (!incubation.RareOffspringProduced && incubation.HatchGenome is not null)
                || this.incubations.ContainsKey(incubation.MachineKey)
                || this.incubations.Values.Any(other => other.EggItemId == incubation.EggItemId))
            {
                this.monitor.Log("Skipped a malformed or duplicate external Domesticated Slime incubation record.", LogLevel.Warn);
                continue;
            }

            if (incubation.HatchGenome is not null)
            {
                incubation.HatchGenome.Size = NormalizeSize(incubation.HatchGenome.Size);
                SlimeAbilityCatalog.Refresh(incubation.HatchGenome);
            }

            this.incubations.Add(incubation.MachineKey, incubation);
        }

        foreach (BreedingPairRecord pair in this.breedingPairs.Values.Where(pair => pair.OutputEggItemId is not null))
        {
            string eggId = pair.OutputEggItemId!;
            if (pair.OutputEggGenome is null && this.eggs.TryGetValue(eggId, out SlimeEggRecord? migratedEgg))
                pair.OutputEggGenome = migratedEgg.Genome;
            if (pair.OutputEggGenome is not null && !this.eggs.ContainsKey(eggId))
            {
                this.eggs[eggId] = new SlimeEggRecord
                {
                    EggId = eggId,
                    ItemId = SlimeEggContent.GetItemId(pair.OutputEggGenome),
                    Genome = pair.OutputEggGenome
                };
            }
        }

        if (this.activeCompanionFarmAnimalId is null)
            this.activeCompanionFarmAnimalId = this.records.Values.FirstOrDefault(record => record.IsCompanion)?.FarmAnimalId;

        foreach (RareLabRecord lab in data?.RareLabs ?? Enumerable.Empty<RareLabRecord>())
        {
            if (lab is null || !RareLabRules.IsValid(lab) || this.rareLabs.ContainsKey(lab.MachineKey))
            {
                this.monitor.Log("Skipped a malformed or duplicate Rare Lab record.", LogLevel.Warn);
                continue;
            }

            this.rareLabs.Add(lab.MachineKey, lab);
        }

        foreach (RareLabContributionRecord transaction in data?.PendingRareLabContributions ?? Enumerable.Empty<RareLabContributionRecord>())
        {
            string eggItemId = string.IsNullOrWhiteSpace(transaction?.EggItemId)
                ? string.Empty
                : NormalizeEggToken(transaction.EggItemId);
            if (transaction is null || !Guid.TryParseExact(transaction.Id, "N", out _)
                || string.IsNullOrWhiteSpace(transaction.MachineKey)
                || !Guid.TryParseExact(eggItemId, "N", out _)
                || transaction.Sequence < 0
                || transaction.PreviousContributionPoints is < 0 or > RareLabRules.MaximumChancePoints
                || !this.rareLabs.ContainsKey(transaction.MachineKey))
            {
                this.monitor.Log("Skipped a malformed Rare Lab contribution transaction.", LogLevel.Warn);
                continue;
            }

            transaction.EggItemId = eggItemId;
            this.pendingRareLabContributions.TryAdd(transaction.Id, transaction);
            if (transaction.Sequence >= this.nextRareLabContributionSequence && transaction.Sequence < long.MaxValue)
                this.nextRareLabContributionSequence = transaction.Sequence + 1;
        }

        foreach (RareLabCatalystTransactionRecord transaction in data?.PendingRareLabCatalystTransactions ?? Enumerable.Empty<RareLabCatalystTransactionRecord>())
        {
            if (transaction is null || !Guid.TryParseExact(transaction.Id, "N", out _)
                || string.IsNullOrWhiteSpace(transaction.MachineKey)
                || !RareLabRules.IsSupportedCatalystSpecies(transaction.CatalystSpecies)
                || transaction.Sequence < 0
                || !this.rareLabs.ContainsKey(transaction.MachineKey))
            {
                this.monitor.Log("Skipped a malformed Rare Lab catalyst transaction.", LogLevel.Warn);
                continue;
            }

            this.pendingRareLabCatalystTransactions.TryAdd(transaction.Id, transaction);
            if (transaction.Sequence >= this.nextRareLabCatalystTransactionSequence && transaction.Sequence < long.MaxValue)
                this.nextRareLabCatalystTransactionSequence = transaction.Sequence + 1;
        }

        foreach (DaycareFurnitureSlotRecord slot in data?.FurnitureSlots ?? Enumerable.Empty<DaycareFurnitureSlotRecord>())
        {
            if (slot is null || string.IsNullOrWhiteSpace(slot.DaycareBuildingId)
                || string.IsNullOrWhiteSpace(slot.SlotId)
                || string.IsNullOrWhiteSpace(slot.FurnitureQualifiedItemId))
            {
                this.monitor.Log("Skipped a malformed Slime Daycare furniture-slot record.", LogLevel.Warn);
                continue;
            }

            string key = GetFurnitureSlotKey(slot.DaycareBuildingId, slot.SlotId);
            if (!this.furnitureSlots.TryAdd(key, slot))
                this.monitor.Log("Skipped a duplicate Slime Daycare furniture-slot record.", LogLevel.Warn);
        }

        foreach (MutagenicAttemptRecord attempt in data?.MutagenicAttempts ?? Enumerable.Empty<MutagenicAttemptRecord>())
        {
            if (attempt is null || string.IsNullOrWhiteSpace(attempt.MachineKey)
                || !this.farmAnimalRecords.ContainsKey(attempt.SlimeId)
                || attempt.ReadyOnDay < attempt.StartedOnDay
                || attempt.ChanceAtStart is < 0 or > 1 || attempt.Roll is < 0 or >= 1
                || this.IsReserved(attempt.SlimeId)
                || this.mutagenicAttempts.ContainsKey(attempt.MachineKey)
                || this.mutagenicAttempts.Values.Any(other => other.SlimeId == attempt.SlimeId))
            {
                this.monitor.Log("Skipped a malformed or duplicate external Mutagenic Pool attempt.", LogLevel.Warn);
                continue;
            }
            this.mutagenicAttempts.Add(attempt.MachineKey, attempt);
        }

        foreach (RefineryJobRecord job in data?.RefineryJobs ?? Enumerable.Empty<RefineryJobRecord>())
        {
            if (job is null)
                continue;
            if (string.IsNullOrWhiteSpace(job.Id))
                job.Id = Guid.NewGuid().ToString("N");
            if (!IsValidRefineryJob(job)
                || this.refineryJobs.ContainsKey(job.MachineKey))
            {
                this.monitor.Log("Skipped a malformed or duplicate external Gel Refinery job.", LogLevel.Warn);
                continue;
            }
            this.refineryJobs.Add(job.MachineKey, job);
        }

        foreach (RefineryTransactionRecord transaction in data?.PendingRefineryTransactions ?? Enumerable.Empty<RefineryTransactionRecord>())
        {
            if (!IsValidRefineryTransaction(transaction)
                || this.pendingRefineryTransactions.ContainsKey(transaction.Id))
            {
                this.monitor.Log("Skipped a malformed or duplicate pending Gel Refinery transaction.", LogLevel.Warn);
                continue;
            }
            this.pendingRefineryTransactions.Add(transaction.Id, transaction);
            if (transaction.Sequence >= this.nextRefineryTransactionSequence && transaction.Sequence < long.MaxValue)
                this.nextRefineryTransactionSequence = transaction.Sequence + 1;
        }

        if (logSummary)
            this.monitor.Log($"Loaded {this.records.Count} Domesticated Slime record(s); assigned {assignedFoodLikes} missing or invalid food like(s). Existing vanilla GreenSlime monsters are not managed.", LogLevel.Info);
    }

    public SlimeSaveData Capture() => new()
    {
        SchemaVersion = SlimeSaveData.CurrentSchemaVersion,
        BreedingPairs = this.breedingPairs.Values.OrderBy(pair => pair.MachineKey, StringComparer.Ordinal).ToList(),
        Incubations = this.incubations.Values.OrderBy(record => record.MachineKey, StringComparer.Ordinal).ToList(),
        RareLabs = this.rareLabs.Values.OrderBy(record => record.MachineKey, StringComparer.Ordinal).ToList(),
        PendingRareLabContributions = this.pendingRareLabContributions.Values.OrderBy(record => record.Sequence).ToList(),
        PendingRareLabCatalystTransactions = this.pendingRareLabCatalystTransactions.Values.OrderBy(record => record.Sequence).ToList(),
        MutagenicAttempts = this.mutagenicAttempts.Values.OrderBy(record => record.MachineKey, StringComparer.Ordinal).ToList(),
        PendingMutagenicContributions = this.pendingMutagenicContributions.Values.OrderBy(record => record.Sequence).ToList(),
        RefineryJobs = this.refineryJobs.Values.OrderBy(record => record.MachineKey, StringComparer.Ordinal).ToList(),
        PendingRefineryTransactions = this.pendingRefineryTransactions.Values.OrderBy(record => record.Sequence).ToList(),
        SlimeDaycareQuest = SlimeDaycareQuestProgression.Normalize(this.slimeDaycareQuest),
        ActiveCompanionFarmAnimalId = this.activeCompanionFarmAnimalId,
        HatchedEggIds = this.hatchedEggIds.OrderBy(id => id, StringComparer.Ordinal).ToList(),
        DestroyedEggIds = this.destroyedEggIds.OrderBy(id => id, StringComparer.Ordinal).ToList(),
        PendingIncubationCancellations = this.pendingIncubationCancellations.Values.OrderBy(record => record.EggItemId, StringComparer.Ordinal).ToList(),
        FurnitureSlots = this.furnitureSlots.Values
            .OrderBy(record => record.DaycareBuildingId, StringComparer.Ordinal)
            .ThenBy(record => record.SlotId, StringComparer.Ordinal)
            .ToList()
    };

    public SlimeEggRecord CreateEgg(SlimeGenome genome)
    {
        if (genome.Id == Guid.Empty)
            throw new ArgumentException("An egg genome must have a unique ID.", nameof(genome));

        genome.Size = NormalizeSize(genome.Size);
        string eggId = genome.Id.ToString("N");
        string itemId = SlimeEggContent.GetItemId(genome);
        SlimeAbilityCatalog.Refresh(genome);
        var egg = new SlimeEggRecord { EggId = eggId, ItemId = itemId, Genome = genome };
        this.eggs[eggId] = egg;
        return egg;
    }

    private static string NormalizeEggToken(string value)
    {
        if (value.StartsWith(SlimeEggContent.ItemIdPrefix, StringComparison.Ordinal))
            value = value[SlimeEggContent.ItemIdPrefix.Length..];
        return Guid.TryParseExact(value, "N", out Guid id) ? id.ToString("N") : value;
    }

    private static string GetFurnitureSlotKey(string daycareBuildingId, string slotId) =>
        $"{daycareBuildingId}\u001f{slotId}";

    public bool TryAdd(SlimeRecord? record)
    {
        if (record?.Genome is not { Id: var id } || id == Guid.Empty || !this.records.TryAdd(id, record))
            return false;

        if (record.FarmAnimalId is long animalId && !this.farmAnimalRecords.TryAdd(animalId, record))
        {
            this.records.Remove(id);
            return false;
        }

        if (!FoodLikeCatalog.IsEligible(record.Genome.LikedFoodItemId))
            record.Genome.LikedFoodItemId = FoodLikeCatalog.ForSlime(id);
        record.MutagenicPoolFailures = Math.Clamp(record.MutagenicPoolFailures, 0, 9_999);
        record.MutagenicPoolContribution = double.IsFinite(record.MutagenicPoolContribution)
            ? Math.Clamp(record.MutagenicPoolContribution, 0, 1)
            : 0;
        SlimeAbilityCatalog.Refresh(record.Genome);

        return true;
    }

    private static SlimeGenome CreateRareOffspringGenome(SlimeGenome source, string rareSpecies)
    {
        var genome = new SlimeGenome
        {
            Id = Guid.NewGuid(),
            ParentA = source.ParentA,
            ParentB = source.ParentB,
            ParentAName = source.ParentAName,
            ParentBName = source.ParentBName,
            Generation = source.Generation,
            PrimarySpecies = rareSpecies,
            SecondarySpecies = null,
            VisualColor = source.VisualColor,
            Size = source.Size,
            IsShiny = source.IsShiny,
            RarityTier = "Rare",
            MutationTrait = source.MutationTrait,
            LikedFoodItemId = source.LikedFoodItemId
        };
        SlimeAbilityCatalog.Refresh(genome);
        return genome;
    }
}

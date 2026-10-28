using System.Text.Json;
using StardewModdingAPI;
using StardewValley;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>Ranch Slime records linked to native FarmAnimal entities, plus active facility jobs.</summary>
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
    private readonly Dictionary<string, MutagenicAttemptRecord> mutagenicAttempts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RefineryJobRecord> refineryJobs = new(StringComparer.Ordinal);
    private readonly HashSet<string> hatchedEggIds = new(StringComparer.Ordinal);
    private long? activeCompanionFarmAnimalId;

    public SlimeRegistry(IMonitor monitor) => this.monitor = monitor;

    public int Count => this.records.Count;
    public IReadOnlyCollection<SlimeEggRecord> Eggs => this.eggs.Values;
    public IReadOnlyCollection<IncubationRecord> Incubations => this.incubations.Values;
    public IReadOnlyCollection<BreedingPairRecord> BreedingPairs => this.breedingPairs.Values;
    public IReadOnlyCollection<MutagenicAttemptRecord> MutagenicAttempts => this.mutagenicAttempts.Values;
    public IReadOnlyCollection<RefineryJobRecord> RefineryJobs => this.refineryJobs.Values;

    public IReadOnlyCollection<long> RanchSlimeFarmAnimalIds => this.farmAnimalRecords.Keys;
    public bool IsBreedingParentReserved(long farmAnimalId) => this.breedingPairs.Values
        .Any(pair => pair.OutputEggItemId is null
            && (pair.ParentAId == farmAnimalId || pair.ParentBId == farmAnimalId));
    public bool IsInActiveBreedingCycle(long farmAnimalId) => this.breedingPairs.Values
        .Any(pair => pair.CycleReadyOnDay > 0 && pair.OutputEggItemId is null
            && (pair.ParentAId == farmAnimalId || pair.ParentBId == farmAnimalId));
    public IReadOnlyCollection<long> ReservedFarmAnimalIds => this.breedingPairs.Values
        .Where(pair => pair.OutputEggItemId is null)
        .SelectMany(pair => new[] { pair.ParentAId, pair.ParentBId })
        .Concat(this.mutagenicAttempts.Values.Select(attempt => attempt.SlimeId))
        .Distinct()
        .ToArray();
    public long? ActiveCompanionFarmAnimalId => this.activeCompanionFarmAnimalId;

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
            || record.AgeDays < FarmAnimalContent.RanchSlimeDaysToMature
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
            ? Math.Clamp(record.AgeDays, 0, FarmAnimalContent.RanchSlimeDaysToMature)
            : Math.Clamp(fallbackAgeDays, 0, FarmAnimalContent.RanchSlimeDaysToMature);

    public bool FeedJuvenileAtCompostTrough(long farmAnimalId, string foodItemId, int currentDay,
        int growthDays, int likedFoodBonusDays, out int newAgeDays, out bool isFavoriteFood)
    {
        newAgeDays = 0;
        isFavoriteFood = false;
        if (!CompostTroughRules.IsEligibleFood(foodItemId)
            || growthDays is < 1 or > 3 || likedFoodBonusDays is < 0 or > 2
            || !this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record)
            || !record.AgeClockInitialized || record.AgeDays >= FarmAnimalContent.RanchSlimeDaysToMature
            || record.LastCompostFeedDay >= currentDay || this.IsReserved(farmAnimalId))
            return false;

        isFavoriteFood = string.Equals(record.Genome.LikedFoodItemId, foodItemId, StringComparison.Ordinal);
        int daysAdvanced = growthDays + (isFavoriteFood ? likedFoodBonusDays : 0);
        record.AgeDays = Math.Min(FarmAnimalContent.RanchSlimeDaysToMature, record.AgeDays + daysAdvanced);
        record.AgeLastUpdatedDay = Math.Max(record.AgeLastUpdatedDay, currentDay);
        record.LastCompostFeedDay = currentDay;
        newAgeDays = record.AgeDays;
        return true;
    }

    /// <summary>Sync the mod-owned age clock and mirror its value into the native animal field Stardew uses for maturity.</summary>
    public bool SynchronizeMaturity(IEnumerable<FarmAnimal> animals, int currentDay)
    {
        bool changed = false;
        foreach (FarmAnimal animal in animals)
        {
            if (animal.type.Value != FarmAnimalContent.RanchSlimeTypeId
                || !this.farmAnimalRecords.TryGetValue(animal.myID.Value, out SlimeRecord? record))
                continue;

            if (!string.Equals(record.AnimalName, animal.Name, StringComparison.Ordinal))
            {
                record.AnimalName = animal.Name;
                changed = true;
            }

            if (!record.AgeClockInitialized)
            {
                record.AgeDays = Math.Clamp(animal.age.Value, 0, FarmAnimalContent.RanchSlimeDaysToMature);
                record.AgeLastUpdatedDay = currentDay;
                record.AgeClockInitialized = true;
                changed = true;
            }
            else if (currentDay > record.AgeLastUpdatedDay)
            {
                int elapsedDays = currentDay - record.AgeLastUpdatedDay;
                record.AgeDays = Math.Min(FarmAnimalContent.RanchSlimeDaysToMature, record.AgeDays + elapsedDays);
                record.AgeLastUpdatedDay = currentDay;
                changed = true;
            }

            int authoritativeAge = Math.Clamp(record.AgeDays, 0, FarmAnimalContent.RanchSlimeDaysToMature);
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
        if (this.hatchedEggIds.Contains(egg.EggId))
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
        && !this.incubations.Values.Any(record => record.EggItemId == itemId);

    public bool StartIncubation(string machineKey, string eggItemId, int currentDay, int durationDays)
    {
        if (string.IsNullOrWhiteSpace(machineKey) || this.incubations.ContainsKey(machineKey)
            || !this.CanIncubate(eggItemId) || durationDays is < 1 or > 28)
            return false;

        this.incubations[machineKey] = new IncubationRecord
        {
            MachineKey = machineKey,
            EggItemId = eggItemId,
            EggGenome = this.eggs[eggItemId].Genome,
            StartedOnDay = currentDay,
            ReadyOnDay = currentDay + durationDays
        };
        return true;
    }

    public RefineryJobRecord? GetRefineryJob(string machineKey) =>
        this.refineryJobs.TryGetValue(machineKey, out RefineryJobRecord? job) ? job : null;

    public bool StartRefineryJob(string machineKey, int currentDay, int durationDays, int slimeFuelCount)
    {
        if (string.IsNullOrWhiteSpace(machineKey) || this.refineryJobs.ContainsKey(machineKey)
            || durationDays is < 1 or > 28 || slimeFuelCount is < 1 or > 999_999)
            return false;

        this.refineryJobs.Add(machineKey, new RefineryJobRecord
        {
            MachineKey = machineKey,
            InputItemId = "(O)170",
            InputCount = 1,
            SlimeFuelCount = slimeFuelCount,
            OutputItemId = "(O)338",
            OutputCount = 1,
            StartedOnDay = currentDay,
            ReadyOnDay = currentDay + durationDays
        });
        return true;
    }

    public bool RemoveRefineryJob(string machineKey) => this.refineryJobs.Remove(machineKey);

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
            .Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId)
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
        if (animal.type.Value != FarmAnimalContent.RanchSlimeTypeId
            || incubation.HatchAnimalId != animal.myID.Value
            || !this.incubations.TryGetValue(incubation.MachineKey, out IncubationRecord? current)
            || !ReferenceEquals(current, incubation)
            || !this.eggs.TryGetValue(incubation.EggItemId, out SlimeEggRecord? egg)
            || egg.HatchedAnimalId != animal.myID.Value)
            return false;

        if (this.records.TryGetValue(egg.Genome.Id, out SlimeRecord? existing))
        {
            if (existing.FarmAnimalId != animal.myID.Value)
                return false;
        }
        else
        {
            var record = new SlimeRecord
            {
                Genome = egg.Genome,
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
        this.activeCompanionFarmAnimalId == farmAnimalId
        || this.incubations.Values.Any(incubation => incubation.HatchAnimalId == farmAnimalId)
        || this.breedingPairs.Values.Any(pair => pair.OutputEggItemId is null && pair.MachineKey != exceptMachineKey
            && (pair.ParentAId == farmAnimalId || pair.ParentBId == farmAnimalId))
        || this.mutagenicAttempts.Values.Any(attempt => attempt.MachineKey != exceptMachineKey && attempt.SlimeId == farmAnimalId);

    public MutagenicAttemptRecord? GetMutagenicAttempt(string machineKey) =>
        this.mutagenicAttempts.TryGetValue(machineKey, out MutagenicAttemptRecord? attempt) ? attempt : null;

    public int GetMutagenicPoolFailures(long slimeId) =>
        this.farmAnimalRecords.TryGetValue(slimeId, out SlimeRecord? record) ? Math.Clamp(record.MutagenicPoolFailures, 0, 9_999) : 0;

    public string? GetMutationTrait(long slimeId) =>
        this.farmAnimalRecords.TryGetValue(slimeId, out SlimeRecord? record) ? record.Genome.MutationTrait : null;

    public bool SetMutationTrait(long slimeId, string? mutationTrait)
    {
        if (!this.farmAnimalRecords.TryGetValue(slimeId, out SlimeRecord? record)
            || (mutationTrait is not null && mutationTrait != "Shadow Clone")
            || this.IsReserved(slimeId))
            return false;

        record.Genome.MutationTrait = mutationTrait;
        return true;
    }

    public bool StartMutagenicAttempt(string machineKey, long slimeId, int currentDay, int ageDays, double roll, MutagenicPoolRules rules)
    {
        if (string.IsNullOrWhiteSpace(machineKey) || this.mutagenicAttempts.ContainsKey(machineKey)
            || !this.farmAnimalRecords.TryGetValue(slimeId, out SlimeRecord? record)
            || ageDays < FarmAnimalContent.RanchSlimeDaysToMature
            || !string.IsNullOrWhiteSpace(record.Genome.MutationTrait)
            || this.IsReserved(slimeId, machineKey))
            return false;

        double chance = rules.GetChance(record.MutagenicPoolFailures);
        this.mutagenicAttempts[machineKey] = new MutagenicAttemptRecord
        {
            MachineKey = machineKey,
            SlimeId = slimeId,
            StartedOnDay = currentDay,
            ReadyOnDay = currentDay + rules.DurationDays,
            ChanceAtStart = chance,
            Roll = Math.Clamp(roll, 0, 0.999999999)
        };
        return true;
    }

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
                slime.Genome.MutationTrait = "Shadow Clone";
                slime.MutagenicPoolFailures = 0;
            }
            else
                slime.MutagenicPoolFailures = Math.Min(9_999, Math.Clamp(slime.MutagenicPoolFailures, 0, 9_999) + 1);
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
            || !this.farmAnimalRecords.ContainsKey(parentAId) || !this.farmAnimalRecords.ContainsKey(parentBId)
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

    public bool StartBreedingCycle(string machineKey, int currentDay, string? chromaticCrystalColor = null)
    {
        if (!this.breedingPairs.TryGetValue(machineKey, out BreedingPairRecord? pair) || pair.CycleReadyOnDay > 0
            || (chromaticCrystalColor is not null && !ChromaticCrystalContent.IsColorName(chromaticCrystalColor)))
            return false;

        pair.CycleStartedOnDay = currentDay;
        pair.CycleReadyOnDay = currentDay + 2;
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
        (string primarySpecies, string? secondarySpecies, string visualColor) = RollOffspringSpecies(a, b);
        var offspring = new SlimeGenome
        {
            Id = Guid.NewGuid(),
            ParentA = a.Id,
            ParentB = b.Id,
            ParentAName = string.IsNullOrWhiteSpace(parentAName) ? null : parentAName,
            ParentBName = string.IsNullOrWhiteSpace(parentBName) ? null : parentBName,
            Generation = Math.Max(a.Generation, b.Generation) + 1,
            PrimarySpecies = primarySpecies,
            SecondarySpecies = secondarySpecies,
            VisualColor = visualColor,
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

    private static (string PrimarySpecies, string? SecondarySpecies, string VisualColor) RollOffspringSpecies(SlimeGenome a, SlimeGenome b)
    {
        string? colorA = GetBaseSpeciesColor(a);
        string? colorB = GetBaseSpeciesColor(b);
        if (colorA is not null && colorB is not null)
        {
            string first = colorA;
            string second = colorB;
            if (TryGetPrimaryRecipe(first, second, out string secondaryColor))
                return ($"{secondaryColor} Slime", null, FarmAnimalContent.GetReferenceHex(secondaryColor));

            if (TryGetHybridRecipe(first, second, out string hybridPrimary, out string hybridSecondary))
            {
                Microsoft.Xna.Framework.Color firstColor = ParseGenomeColor(FarmAnimalContent.GetReferenceHex(hybridPrimary));
                Microsoft.Xna.Framework.Color secondColor = ParseGenomeColor(FarmAnimalContent.GetReferenceHex(hybridSecondary));
                string blended = $"#{(firstColor.R + secondColor.R) / 2:X2}{(firstColor.G + secondColor.G) / 2:X2}{(firstColor.B + secondColor.B) / 2:X2}";
                return ($"{hybridPrimary} Slime", $"{hybridSecondary} Slime", blended);
            }
        }

        // For undefined pairings (including Pink), retain one parent's complete species/color identity.
        SlimeGenome inherited = Game1.random.Next(2) == 0 ? a : b;
        return (inherited.PrimarySpecies, inherited.SecondarySpecies, inherited.VisualColor);
    }

    private static string? GetBaseSpeciesColor(SlimeGenome genome)
    {
        if (!string.IsNullOrWhiteSpace(genome.SecondarySpecies))
            return null;

        string species = genome.PrimarySpecies;
        string[] colors = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Violet", "Pink" };
        string? named = colors.FirstOrDefault(color => species.StartsWith(color, StringComparison.OrdinalIgnoreCase));
        if (named is not null)
            return named.Equals("Violet", StringComparison.OrdinalIgnoreCase) ? "Purple" : named;

        return FarmAnimalContent.GetClosestVariantName(genome.VisualColor);
    }

    private static bool TryGetPrimaryRecipe(string first, string second, out string result)
    {
        result = (first, second) switch
        {
            ("Red", "Yellow") or ("Yellow", "Red") => "Orange",
            ("Yellow", "Blue") or ("Blue", "Yellow") => "Green",
            ("Blue", "Red") or ("Red", "Blue") => "Purple",
            _ => ""
        };
        return result.Length > 0;
    }

    private static bool TryGetHybridRecipe(string first, string second, out string primary, out string secondary)
    {
        (primary, secondary) = (first, second) switch
        {
            ("Red", "Orange") or ("Orange", "Red") => ("Red", "Orange"),
            ("Yellow", "Orange") or ("Orange", "Yellow") => ("Yellow", "Orange"),
            ("Yellow", "Green") or ("Green", "Yellow") => ("Yellow", "Green"),
            ("Blue", "Green") or ("Green", "Blue") => ("Blue", "Green"),
            ("Blue", "Purple") or ("Purple", "Blue") => ("Blue", "Purple"),
            ("Red", "Purple") or ("Purple", "Red") => ("Red", "Purple"),
            _ => ("", "")
        };
        return primary.Length > 0;
    }

    private static Microsoft.Xna.Framework.Color ParseGenomeColor(string hex)
    {
        byte red = Convert.ToByte(hex.Substring(1, 2), 16);
        byte green = Convert.ToByte(hex.Substring(3, 2), 16);
        byte blue = Convert.ToByte(hex.Substring(5, 2), 16);
        return new Microsoft.Xna.Framework.Color(red, green, blue);
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

    public int RegisterRanchSlimes(IEnumerable<FarmAnimal> animals, SlimeGenome? genomeOverride = null)
    {
        int added = 0;
        foreach (FarmAnimal animal in animals)
        {
            if (animal.type.Value != FarmAnimalContent.RanchSlimeTypeId)
                continue;

            SlimeRecord? entityRecord = ReadEntityRecord(animal);
            if (this.farmAnimalRecords.TryGetValue(animal.myID.Value, out SlimeRecord? existing))
            {
                if (entityRecord is not null && entityRecord.Genome.Id != existing.Genome.Id)
                {
                    if (this.records.TryGetValue(entityRecord.Genome.Id, out SlimeRecord? duplicate)
                        && duplicate.FarmAnimalId != animal.myID.Value)
                    {
                        this.monitor.Log($"Ignored Ranch Slime modData for FarmAnimal {animal.myID.Value}: genome ID {entityRecord.Genome.Id:N} is already linked to another animal.", LogLevel.Warn);
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
                        PrimarySpecies = "Ranch Slime",
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
            if (animal.type.Value != FarmAnimalContent.RanchSlimeTypeId
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
        target.LastCompostFeedDay = source.LastCompostFeedDay;
        target.EnrichmentActivityDay = source.EnrichmentActivityDay;
        target.EnrichmentActivitiesToday = source.EnrichmentActivitiesToday;
        target.NextEnrichmentCheckMinute = source.NextEnrichmentCheckMinute;
        target.LastHutchName = source.LastHutchName;
        target.IsCompanion = source.IsCompanion;
    }

    /// <summary>Detach removed animals while keeping their genome archive; abandon only cycles with no saved egg.</summary>
    public (int DetachedAnimals, int CancelledCycles) ReconcileRanchSlimes(IEnumerable<FarmAnimal> animals)
    {
        HashSet<long> liveIds = animals
            .Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId)
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
        this.mutagenicAttempts.Clear();
        this.refineryJobs.Clear();
        this.hatchedEggIds.Clear();
        this.activeCompanionFarmAnimalId = data?.ActiveCompanionFarmAnimalId;
        foreach (string eggId in data?.HatchedEggIds ?? Enumerable.Empty<string>())
        {
            string normalizedEggId = NormalizeEggToken(eggId);
            if (Guid.TryParseExact(normalizedEggId, "N", out _))
                this.hatchedEggIds.Add(normalizedEggId);
        }
        if (data is not null && data.SchemaVersion != SlimeSaveData.CurrentSchemaVersion)
            throw new InvalidOperationException($"Unsupported slime data schema {data.SchemaVersion}; expected {SlimeSaveData.CurrentSchemaVersion}.");

        int assignedFoodLikes = 0;
        foreach (SlimeRecord record in data?.Slimes ?? Enumerable.Empty<SlimeRecord>())
        {
            if (record is not null)
            {
                record.Genome.Size = NormalizeSize(record.Genome.Size);
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
            if (incubation is null)
            {
                this.monitor.Log("Skipped a malformed Ranch Slime incubation record.", LogLevel.Warn);
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
                || this.incubations.ContainsKey(incubation.MachineKey)
                || this.incubations.Values.Any(other => other.EggItemId == incubation.EggItemId))
            {
                this.monitor.Log("Skipped a malformed or duplicate external Ranch Slime incubation record.", LogLevel.Warn);
                continue;
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
            if (job is null || string.IsNullOrWhiteSpace(job.MachineKey)
                || job.StartedOnDay < 0 || job.ReadyOnDay < job.StartedOnDay
                || job.InputItemId != "(O)170" || job.InputCount != 1 || job.SlimeFuelCount is < 1 or > 999_999
                || job.OutputItemId != "(O)338" || job.OutputCount != 1
                || this.refineryJobs.ContainsKey(job.MachineKey))
            {
                this.monitor.Log("Skipped a malformed or duplicate external Gel Refinery job.", LogLevel.Warn);
                continue;
            }
            this.refineryJobs.Add(job.MachineKey, job);
        }

        if (logSummary)
            this.monitor.Log($"Loaded {this.records.Count} Ranch Slime record(s); assigned {assignedFoodLikes} missing or invalid food like(s). Existing vanilla GreenSlime monsters are not managed.", LogLevel.Info);
    }

    public SlimeSaveData Capture() => new()
    {
        BreedingPairs = this.breedingPairs.Values.OrderBy(pair => pair.MachineKey, StringComparer.Ordinal).ToList(),
        Incubations = this.incubations.Values.OrderBy(record => record.MachineKey, StringComparer.Ordinal).ToList(),
        MutagenicAttempts = this.mutagenicAttempts.Values.OrderBy(record => record.MachineKey, StringComparer.Ordinal).ToList(),
        RefineryJobs = this.refineryJobs.Values.OrderBy(record => record.MachineKey, StringComparer.Ordinal).ToList(),
        ActiveCompanionFarmAnimalId = this.activeCompanionFarmAnimalId,
        HatchedEggIds = this.hatchedEggIds.OrderBy(id => id, StringComparer.Ordinal).ToList()
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
        const string legacyPrefix = "Taum.SlimeRanching_Egg_";
        if (value.StartsWith(legacyPrefix, StringComparison.Ordinal))
            value = value[legacyPrefix.Length..];
        return Guid.TryParseExact(value, "N", out Guid id) ? id.ToString("N") : value;
    }

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
        SlimeAbilityCatalog.Refresh(record.Genome);

        return true;
    }
}

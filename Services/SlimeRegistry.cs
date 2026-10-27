using StardewModdingAPI;
using StardewValley;
using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>External genome records linked to native Ranch Slime FarmAnimal IDs.</summary>
internal sealed class SlimeRegistry
{
    private const int BreedingRestDays = 1;
    private readonly IMonitor monitor;
    private readonly Dictionary<Guid, SlimeRecord> records = new();
    private readonly Dictionary<long, SlimeRecord> farmAnimalRecords = new();
    private readonly Dictionary<string, BreedingPairRecord> breedingPairs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SlimeEggRecord> eggs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IncubationRecord> incubations = new(StringComparer.Ordinal);

    public SlimeRegistry(IMonitor monitor) => this.monitor = monitor;

    public int Count => this.records.Count;
    public IReadOnlyCollection<SlimeEggRecord> Eggs => this.eggs.Values;
    public IReadOnlyCollection<IncubationRecord> Incubations => this.incubations.Values;
    public IReadOnlyCollection<BreedingPairRecord> BreedingPairs => this.breedingPairs.Values;

    public IReadOnlyCollection<long> RanchSlimeFarmAnimalIds => this.farmAnimalRecords.Keys;
    public IReadOnlyCollection<long> ReservedFarmAnimalIds => this.breedingPairs.Values
        .SelectMany(pair => new[] { pair.ParentAId, pair.ParentBId })
        .Distinct()
        .ToArray();

    public BreedingPairRecord? GetBreedingPair(string machineKey) =>
        this.breedingPairs.TryGetValue(machineKey, out BreedingPairRecord? pair) ? pair : null;

    public IncubationRecord? GetIncubation(string machineKey) =>
        this.incubations.TryGetValue(machineKey, out IncubationRecord? record) ? record : null;

    public string? GetGenomeSize(long farmAnimalId) =>
        this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record) ? record.Genome.Size : null;

    public int GetMaturityAgeDays(long farmAnimalId, int fallbackAgeDays) =>
        this.farmAnimalRecords.TryGetValue(farmAnimalId, out SlimeRecord? record) && record.AgeClockInitialized
            ? Math.Clamp(record.AgeDays, 0, FarmAnimalContent.RanchSlimeDaysToMature)
            : Math.Clamp(fallbackAgeDays, 0, FarmAnimalContent.RanchSlimeDaysToMature);

    /// <summary>Sync the mod-owned age clock and mirror its value into the native animal field Stardew uses for maturity.</summary>
    public bool SynchronizeMaturity(IEnumerable<FarmAnimal> animals, int currentDay)
    {
        bool changed = false;
        foreach (FarmAnimal animal in animals)
        {
            if (animal.type.Value != FarmAnimalContent.RanchSlimeTypeId
                || !this.farmAnimalRecords.TryGetValue(animal.myID.Value, out SlimeRecord? record))
                continue;

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

    public bool CanIncubate(string itemId) => this.eggs.TryGetValue(itemId, out SlimeEggRecord? egg)
        && egg.HatchedAnimalId is null
        && !this.incubations.Values.Any(record => record.EggItemId == itemId);

    public bool StartIncubation(string machineKey, string eggItemId, int currentDay)
    {
        if (string.IsNullOrWhiteSpace(machineKey) || this.incubations.ContainsKey(machineKey)
            || !this.CanIncubate(eggItemId))
            return false;

        this.incubations[machineKey] = new IncubationRecord
        {
            MachineKey = machineKey,
            EggItemId = eggItemId,
            StartedOnDay = currentDay,
            ReadyOnDay = currentDay + 2
        };
        return true;
    }

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
                this.CompleteIncubation(incubation.MachineKey);
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

    public bool CompleteIncubation(string machineKey) => this.incubations.Remove(machineKey);

    public bool IsReserved(long farmAnimalId, string? exceptMachineKey = null) =>
        this.breedingPairs.Values.Any(pair => pair.MachineKey != exceptMachineKey
            && (pair.ParentAId == farmAnimalId || pair.ParentBId == farmAnimalId));

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

    public bool StartBreedingCycle(string machineKey, int currentDay)
    {
        if (!this.breedingPairs.TryGetValue(machineKey, out BreedingPairRecord? pair) || pair.CycleReadyOnDay > 0)
            return false;

        pair.CycleStartedOnDay = currentDay;
        pair.CycleReadyOnDay = currentDay + 2;
        return true;
    }

    public bool CancelBreedingCycle(string machineKey)
    {
        if (!this.breedingPairs.TryGetValue(machineKey, out BreedingPairRecord? pair)
            || pair.CycleReadyOnDay <= 0 || pair.OutputEggItemId is not null)
            return false;

        pair.CycleStartedOnDay = 0;
        pair.CycleReadyOnDay = 0;
        return true;
    }

    public SlimeEggRecord? GetOrCreateBreedingEgg(string machineKey, int currentDay, out bool created)
    {
        created = false;
        if (!this.breedingPairs.TryGetValue(machineKey, out BreedingPairRecord? pair)
            || pair.CycleReadyOnDay <= 0 || currentDay < pair.CycleReadyOnDay)
            return null;

        if (pair.OutputEggItemId is not null)
            return this.eggs.TryGetValue(pair.OutputEggItemId, out SlimeEggRecord? existing) ? existing : null;

        if (!this.farmAnimalRecords.TryGetValue(pair.ParentAId, out SlimeRecord? parentA)
            || !this.farmAnimalRecords.TryGetValue(pair.ParentBId, out SlimeRecord? parentB))
            return null;

        bool chooseA() => Game1.random.Next(2) == 0;
        SlimeGenome a = parentA.Genome;
        SlimeGenome b = parentB.Genome;
        var offspring = new SlimeGenome
        {
            Id = Guid.NewGuid(),
            ParentA = a.Id,
            ParentB = b.Id,
            Generation = Math.Max(a.Generation, b.Generation) + 1,
            PrimarySpecies = chooseA() ? a.PrimarySpecies : b.PrimarySpecies,
            SecondarySpecies = chooseA() ? a.SecondarySpecies : b.SecondarySpecies,
            VisualColor = chooseA() ? a.VisualColor : b.VisualColor,
            Size = chooseA() ? a.Size : b.Size,
            IsShiny = chooseA() ? a.IsShiny : b.IsShiny,
            RarityTier = chooseA() ? a.RarityTier : b.RarityTier,
            MutationTrait = chooseA() ? a.MutationTrait : b.MutationTrait,
            LikedFoodItemId = chooseA() ? a.LikedFoodItemId : b.LikedFoodItemId
        };

        SlimeEggRecord egg = this.CreateEgg(offspring);
        pair.OutputEggItemId = egg.ItemId;
        created = true;
        return egg;
    }

    public bool FinishBreedingCycle(string machineKey, string eggItemId, int currentDay)
    {
        if (!this.breedingPairs.TryGetValue(machineKey, out BreedingPairRecord? pair)
            || pair.OutputEggItemId != eggItemId || !this.eggs.ContainsKey(eggItemId))
            return false;

        if (this.farmAnimalRecords.TryGetValue(pair.ParentAId, out SlimeRecord? parentA))
            parentA.BreedingCooldownUntilDay = Math.Max(parentA.BreedingCooldownUntilDay, currentDay + BreedingRestDays);
        if (this.farmAnimalRecords.TryGetValue(pair.ParentBId, out SlimeRecord? parentB))
            parentB.BreedingCooldownUntilDay = Math.Max(parentB.BreedingCooldownUntilDay, currentDay + BreedingRestDays);

        this.breedingPairs.Remove(machineKey);
        return true;
    }

    public void RestoreBreedingCycle(BreedingPairRecord pair, int currentDay)
    {
        this.breedingPairs[pair.MachineKey] = pair;
        if (this.farmAnimalRecords.TryGetValue(pair.ParentAId, out SlimeRecord? parentA)
            && parentA.BreedingCooldownUntilDay == currentDay + BreedingRestDays)
            parentA.BreedingCooldownUntilDay = currentDay;
        if (this.farmAnimalRecords.TryGetValue(pair.ParentBId, out SlimeRecord? parentB)
            && parentB.BreedingCooldownUntilDay == currentDay + BreedingRestDays)
            parentB.BreedingCooldownUntilDay = currentDay;
    }

    public int RegisterRanchSlimes(IEnumerable<FarmAnimal> animals)
    {
        int added = 0;
        foreach (FarmAnimal animal in animals)
        {
            if (animal.type.Value != FarmAnimalContent.RanchSlimeTypeId || this.farmAnimalRecords.ContainsKey(animal.myID.Value))
                continue;

            var record = new SlimeRecord
            {
                FarmAnimalId = animal.myID.Value,
                FarmAnimalTypeId = animal.type.Value,
                Genome = new Models.SlimeGenome
                {
                    Id = Guid.NewGuid(),
                    PrimarySpecies = "Ranch Slime",
                    VisualColor = "#00FF00"
                }
            };

            if (this.TryAdd(record))
                added++;
        }

        return added;
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

        return (missing.Length, abandonedCycles.Length);
    }

    public void Load(SlimeSaveData? data, bool logSummary)
    {
        this.records.Clear();
        this.farmAnimalRecords.Clear();
        this.breedingPairs.Clear();
        this.eggs.Clear();
        this.incubations.Clear();
        if (data is not null && data.SchemaVersion != SlimeSaveData.CurrentSchemaVersion)
            throw new InvalidOperationException($"Unsupported slime data schema {data.SchemaVersion}; expected {SlimeSaveData.CurrentSchemaVersion}.");

        int assignedFoodLikes = 0;
        foreach (SlimeRecord record in data?.Slimes ?? Enumerable.Empty<SlimeRecord>())
        {
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
            this.breedingPairs[pair.MachineKey] = pair;
        }

        foreach (SlimeEggRecord egg in data?.Eggs ?? Enumerable.Empty<SlimeEggRecord>())
        {
            if (egg?.Genome is not { Id: var id } || id == Guid.Empty
                || string.IsNullOrWhiteSpace(egg.ItemId)
                || egg.ItemId != SlimeEggContent.GetItemId(id)
                || !this.eggs.TryAdd(egg.ItemId, egg))
            {
                this.monitor.Log("Skipped a malformed or duplicate external slime egg record.", LogLevel.Warn);
            }
        }

        foreach (IncubationRecord incubation in data?.Incubations ?? Enumerable.Empty<IncubationRecord>())
        {
            if (incubation is null || string.IsNullOrWhiteSpace(incubation.MachineKey)
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

        if (logSummary)
            this.monitor.Log($"Loaded {this.records.Count} external Ranch Slime record(s); assigned {assignedFoodLikes} missing or invalid food like(s). Existing vanilla GreenSlime monsters are not managed.", LogLevel.Info);
    }

    public SlimeSaveData Capture() => new()
    {
        Slimes = this.records.Values.OrderBy(record => record.Genome.Id).ToList(),
        BreedingPairs = this.breedingPairs.Values.OrderBy(pair => pair.MachineKey, StringComparer.Ordinal).ToList(),
        Eggs = this.eggs.Values.OrderBy(egg => egg.ItemId, StringComparer.Ordinal).ToList(),
        Incubations = this.incubations.Values.OrderBy(record => record.MachineKey, StringComparer.Ordinal).ToList()
    };

    public SlimeEggRecord CreateEgg(SlimeGenome genome)
    {
        if (genome.Id == Guid.Empty)
            throw new ArgumentException("An egg genome must have a unique ID.", nameof(genome));

        string itemId = SlimeEggContent.GetItemId(genome.Id);
        var egg = new SlimeEggRecord { ItemId = itemId, Genome = genome };
        this.eggs[itemId] = egg;
        return egg;
    }

    public bool RemoveUnincubatedEgg(string itemId) =>
        !this.incubations.Values.Any(record => record.EggItemId == itemId)
        && this.eggs.Remove(itemId);

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

        return true;
    }
}

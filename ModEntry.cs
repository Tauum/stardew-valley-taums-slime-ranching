using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Monsters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using taums_slime_ranching.Models;
using taums_slime_ranching.Services;
using taums_slime_ranching.UI;

namespace taums_slime_ranching;

public sealed class ModEntry : Mod
{
    private SlimeRegistry? registry;
    private ModStateStore? stateStore;
    private ModConfig config = new();
    private bool stateReady;

    public override void Entry(IModHelper helper)
    {
        this.config = helper.ReadConfig<ModConfig>();
        this.registry = new SlimeRegistry(this.Monitor);
        this.stateStore = new ModStateStore(helper.DirectoryPath);

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.GameLoop.Saving += this.OnSaving;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.ConsoleCommands.Add("slime_ranching_inspect", "Inspect vanilla slime or Ranch Slime counts in the current Slime Hutch or Slime Daycare.", this.OnInspectCommand);
        helper.ConsoleCommands.Add("slime_ranching_spawn_ranch_slime", "Spawn one Ranch Slime in the Slime Daycare. Usage: slime_ranching_spawn_ranch_slime <exact save folder name>.", this.OnSpawnRanchSlimeCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_converter", "Give one Slime Egg Converter for testing. Usage: slime_ranching_give_converter <exact save folder name>.", this.OnGiveConverterCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_guidebook", "Give the Slime Ranching Field Guide. Usage: slime_ranching_give_guidebook <exact save folder name>.", this.OnGiveGuidebookCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_breeder", "Give one Slime Breeder for testing. Usage: slime_ranching_give_breeder <exact save folder name>.", this.OnGiveBreederCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_incubator", "Give one Slime Incubator for testing. Usage: slime_ranching_give_incubator <exact save folder name>.", this.OnGiveIncubatorCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_test_eggs", "Create two external-genome test eggs. Usage: slime_ranching_give_test_eggs <exact save folder name>.", this.OnGiveTestEggsCommand);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.stateReady = false;
        if (!Context.IsMainPlayer)
            return;

        try
        {
            string backupPath = FirstSeenSnapshot.EnsureCreated(this.Helper.DirectoryPath);
            this.Monitor.Log($"Mod-folder first-seen snapshot verified at {backupPath}.", LogLevel.Trace);
            this.registry!.Load(this.stateStore!.Load(), this.config.LogRegistrySummary);
            int recoveredHatches = this.registry.RecoverPendingHatches(GetFarmAnimals());
            if (recoveredHatches > 0)
                this.Monitor.Log($"Recovered {recoveredHatches} saved Ranch Slime hatch(es) from the external incubator registry.", LogLevel.Info);
            int registeredRanchSlimes = this.registry.RegisterRanchSlimes(GetFarmAnimals());
            if (registeredRanchSlimes > 0)
                this.Monitor.Log($"Registered {registeredRanchSlimes} Ranch Slime farm animal(s) in the external genome registry.", LogLevel.Info);
            (int detachedAnimals, int cancelledCycles) = this.registry.ReconcileRanchSlimes(GetFarmAnimals());
            this.stateReady = true;
            this.LogRegistryReconciliation(detachedAnimals, cancelledCycles);
            if ((recoveredHatches > 0 || detachedAnimals > 0 || cancelledCycles > 0) && !this.SaveExternalStateNow())
                this.Monitor.Log("Reconciled Ranch Slime records remain in memory but could not yet be written to the mod-folder registry.", LogLevel.Error);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or System.Text.Json.JsonException or NotSupportedException)
        {
            this.Monitor.Log($"External slime state is disabled for this save: {exception.Message}", LogLevel.Error);
        }
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        if (!this.stateReady || !Context.IsMainPlayer)
            return;

        try
        {
            int registeredRanchSlimes = this.registry!.RegisterRanchSlimes(GetFarmAnimals());
            if (registeredRanchSlimes > 0)
                this.Monitor.Log($"Added {registeredRanchSlimes} Ranch Slime genome record(s) to the external registry.", LogLevel.Info);
            (int detachedAnimals, int cancelledCycles) = this.registry.ReconcileRanchSlimes(GetFarmAnimals());
            this.LogRegistryReconciliation(detachedAnimals, cancelledCycles);
            this.stateStore!.Save(this.registry!.Capture());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            this.Monitor.Log($"Could not save external slime state: {exception.Message}", LogLevel.Error);
        }
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!this.stateReady || !Context.IsMainPlayer)
            return;

        var newlyReady = new List<taums_slime_ranching.Models.SlimeEggRecord>();
        foreach (taums_slime_ranching.Models.BreedingPairRecord pair in this.registry!.BreedingPairs.ToArray())
        {
            taums_slime_ranching.Models.SlimeEggRecord? egg = this.registry.GetOrCreateBreedingEgg(pair.MachineKey, Game1.Date.TotalDays, out bool created);
            if (created && egg is not null)
                newlyReady.Add(egg);
        }

        this.ProcessReadyIncubations();

        if (newlyReady.Count == 0)
            return;

        if (!this.SaveExternalStateNow())
        {
            this.Monitor.Log("A completed breeder cycle could not save its fixed egg genome; it will retry next morning.", LogLevel.Error);
            return;
        }

        foreach (taums_slime_ranching.Models.SlimeEggRecord egg in newlyReady)
        {
            Game1.addHUDMessage(new HUDMessage("A Ranch Slime Egg is ready to collect from the Slime Breeder!", HUDMessage.newQuest_type));
            this.Monitor.Log($"A Ranch Slime Egg (genome {egg.Genome.Id:N}) is ready to collect.", LogLevel.Info);
        }
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.stateReady = false;
        this.registry = new SlimeRegistry(this.Monitor);
        this.stateStore = new ModStateStore(this.Helper.DirectoryPath);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!this.stateReady || !Context.IsMainPlayer)
            return;

        IReadOnlyCollection<long> reservedIds = this.registry!.ReservedFarmAnimalIds;
        if (reservedIds.Count == 0)
            return;

        HashSet<long> reserved = reservedIds.ToHashSet();
        foreach (FarmAnimal animal in GetFarmAnimals())
        {
            if (reserved.Contains(animal.myID.Value))
                animal.StopAllActions();
        }
    }

    private void OnRenderedWorld(object? sender, StardewModdingAPI.Events.RenderedWorldEventArgs e)
    {
        if (!this.stateReady || !Context.IsMainPlayer || Game1.currentLocation != Game1.getFarm())
            return;

        BreedingPairRecord[] activeCycles = this.registry!.BreedingPairs
            .Where(pair => pair.CycleReadyOnDay > Game1.Date.TotalDays && pair.OutputEggItemId is null)
            .ToArray();
        if (activeCycles.Length == 0)
            return;

        double time = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        foreach (Building building in Game1.getFarm().buildings)
        {
            StardewValley.GameLocation? interior = building.GetIndoors();
            if (!BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                || interior is null
                || !activeCycles.Any(pair => pair.MachineKey.StartsWith(interior.NameOrUniqueName + ":", StringComparison.Ordinal)))
                continue;

            Rectangle footprint = building.GetBoundingBox();
            int frameWidth = building.buildingType.Value switch
            {
                BuildingContent.SlimeDaycareUpgrade1Id => 176,
                BuildingContent.SlimeDaycareUpgrade2Id => 192,
                _ => 160
            };
            int chimneyTop = building.buildingType.Value switch
            {
                BuildingContent.SlimeDaycareUpgrade1Id => 23,
                BuildingContent.SlimeDaycareUpgrade2Id => 30,
                _ => 39
            };
            int textureOffsetX = building.buildingType.Value == BuildingContent.SlimeDaycareBuildingId ? -8 : 0;
            int textureOffsetY = building.buildingType.Value == BuildingContent.SlimeDaycareBuildingId ? 3 : 0;
            Vector2 chimney = new(
                footprint.Left + (frameWidth * 0.82f + textureOffsetX) * 4,
                footprint.Bottom - (160 * 4) + (chimneyTop + textureOffsetY) * 4);

            for (int i = 0; i < 3; i++)
            {
                const double heartCycleSeconds = 4.0;
                const double heartSpacingSeconds = heartCycleSeconds / 3;
                double age = (time - i * heartSpacingSeconds) % heartCycleSeconds;
                if (age < 0)
                    age += heartCycleSeconds;

                float fade = age < heartCycleSeconds * (2f / 3f)
                    ? 1f
                    : (float)((heartCycleSeconds - age) / (heartCycleSeconds / 3));
                float drift = (float)Math.Sin(time * 1.2 + i * 1.8) * 10f;
                Vector2 worldPosition = chimney + new Vector2(drift, -(float)age * 24f);
                Vector2 screenPosition = Game1.GlobalToLocal(Game1.viewport, worldPosition);
                DrawLoveHearts(e.SpriteBatch, (int)screenPosition.X, (int)screenPosition.Y - 30, fade);
            }
        }
    }

    private static void DrawLoveHearts(SpriteBatch batch, int x, int y, float opacity)
    {
        string[] softShape =
        {
            "..###..###..",
            ".#####.#####",
            "############",
            "############",
            ".##########.",
            "..########..",
            "...######...",
            "....####....",
            ".....##....."
        };
        string[] brightShape =
        {
            "..##..##..",
            ".####.####.",
            "##########",
            "##########",
            ".########.",
            "..######..",
            "...####...",
            "....##...."
        };

        DrawHeart(
            batch, x - 2, y - 22, 2.4f, softShape,
            new Color(255, 207, 207) * (opacity * 0.78f),
            new Color(255, 239, 230) * (opacity * 0.78f),
            new Color(255, 237, 226) * (opacity * 0.78f));
        DrawHeart(
            batch, x - 10, y - 12, 2.4f, brightShape,
            new Color(221, 71, 130) * opacity,
            new Color(255, 151, 181) * opacity,
            new Color(139, 48, 92) * opacity);
    }

    private static void DrawHeart(SpriteBatch batch, int x, int y, float pixelSize, string[] shape, Color fill, Color highlight, Color outline)
    {
        for (int row = 0; row < shape.Length; row++)
        for (int column = 0; column < shape[row].Length; column++)
        {
            if (shape[row][column] != '#')
                continue;

            bool edge = row == 0 || row == shape.Length - 1 || column == 0 || column == shape[row].Length - 1
                || shape[Math.Max(0, row - 1)][column] != '#'
                || shape[Math.Min(shape.Length - 1, row + 1)][column] != '#'
                || shape[row][Math.Max(0, column - 1)] != '#'
                || shape[row][Math.Min(shape[row].Length - 1, column + 1)] != '#';
            Color color = edge ? outline : (row < 3 && column < 4 ? highlight : fill);
            int left = x + (int)MathF.Round(column * pixelSize);
            int top = y + (int)MathF.Round(row * pixelSize);
            int right = x + (int)MathF.Round((column + 1) * pixelSize);
            int bottom = y + (int)MathF.Round((row + 1) * pixelSize);
            batch.Draw(Game1.staminaRect, new Rectangle(left, top, right - left, bottom - top), color);
        }
    }

    private static IEnumerable<FarmAnimal> GetFarmAnimals()
    {
        Farm farm = Game1.getFarm();
        return farm.animals.Values
            .Concat(farm.buildings
                .Select(building => building.GetIndoors())
                .OfType<StardewValley.AnimalHouse>()
                .SelectMany(animalHouse => animalHouse.animals.Values))
            .GroupBy(animal => animal.myID.Value)
            .Select(group => group.First());
    }

    private bool SaveExternalStateNow()
    {
        if (!this.stateReady || !Context.IsMainPlayer)
            return false;

        try
        {
            this.stateStore!.Save(this.registry!.Capture());
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            this.Monitor.Log($"Could not save the breeder pair to external mod state: {exception.Message}", LogLevel.Error);
            return false;
        }
    }

    private bool StartIncubation(string machineKey, int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= Game1.player.Items.Count
            || Game1.player.Items[inventoryIndex] is not Item item
            || !item.QualifiedItemId.StartsWith("(O)Taum.SlimeRanching_Egg_", StringComparison.Ordinal))
            return false;

        string itemId = item.QualifiedItemId[3..];
        if (!this.registry!.CanIncubate(itemId) || !this.registry.StartIncubation(machineKey, itemId, Game1.Date.TotalDays))
            return false;

        item.Stack--;
        if (item.Stack <= 0)
            Game1.player.Items[inventoryIndex] = null;
        // Save the item removal and external incubation together at Stardew's normal Saving event.
        // If the player exits without saving, both the egg and the incubation should roll back.
        return true;
    }

    private void ProcessReadyIncubations()
    {
        if (this.registry is null || this.registry.Incubations.Count == 0)
            return;

        StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value)
            && building.GetIndoors() is StardewValley.AnimalHouse);
        if (daycare?.GetIndoors() is not StardewValley.AnimalHouse animalHouse)
            return;

        int recovered = this.registry.RecoverPendingHatches(GetFarmAnimals());
        if (recovered > 0)
        {
            this.Monitor.Log($"Confirmed {recovered} previous Ranch Slime hatch(es) against live animal IDs.", LogLevel.Info);
            if (!this.SaveExternalStateNow())
                this.Monitor.Log("The confirmed hatch could not be finalized in the external registry; its reserved hatch record will be checked again on reload.", LogLevel.Error);
            this.Helper.GameContent.InvalidateCache("Data/Objects");
        }

        foreach (IncubationRecord incubation in this.registry.Incubations
                     .Where(record => record.ReadyOnDay <= Game1.Date.TotalDays).ToArray())
        {
            if (!this.registry.TryGetEgg(incubation.EggItemId, out SlimeEggRecord egg))
                continue;
            if (animalHouse.animalsThatLiveHere.Count >= animalHouse.animalLimit.Value)
            {
                this.Monitor.Log("A Ranch Slime Egg is ready, but the Slime Daycare is full; hatching will retry after space is available.", LogLevel.Trace);
                continue;
            }

            long animalId = incubation.HatchAnimalId ?? this.Helper.Multiplayer.GetNewID();
            if (incubation.HatchAnimalId is null)
            {
                if (!this.registry.AssignHatchAnimalId(incubation.MachineKey, animalId)
                    || !this.SaveExternalStateNow())
                {
                    this.registry.ClearHatchAnimalId(incubation.MachineKey);
                    this.Monitor.Log("Could not reserve an external native ID for a ready Ranch Slime hatch; it will retry tomorrow.", LogLevel.Error);
                    continue;
                }
            }

            if (GetFarmAnimals().Any(animal => animal.myID.Value == animalId))
            {
                this.registry.ClearHatchAnimalId(incubation.MachineKey);
                this.SaveExternalStateNow();
                this.Monitor.Log($"Could not hatch genome {egg.Genome.Id:N}: reserved FarmAnimal ID {animalId} is already in use. Cleared it so the incubator can retry with a new ID.", LogLevel.Error);
                continue;
            }

            var animal = new FarmAnimal(FarmAnimalContent.RanchSlimeTypeId, animalId, Game1.player.UniqueMultiplayerID)
            {
                Name = egg.Genome.PrimarySpecies,
                home = daycare,
                homeInterior = animalHouse
            };
            animalHouse.adoptAnimal(animal);
            if (!this.registry.RegisterIncubationHatch(incubation, animal))
            {
                this.Monitor.Log($"Hatched Ranch Slime {animalId}, but its genome could not be linked; external hatch recovery will retry on reload.", LogLevel.Error);
                continue;
            }

            if (!this.SaveExternalStateNow())
                this.Monitor.Log("The animal hatched, but its updated genome link could not yet be written. The reserved external hatch record will recover it after reload.", LogLevel.Error);
            Game1.addHUDMessage(new HUDMessage($"A {animal.Name} hatched in the Slime Daycare!", HUDMessage.newQuest_type));
            this.Monitor.Log($"A Ranch Slime hatched from genome egg {egg.Genome.Id:N} (FarmAnimal ID {animalId}).", LogLevel.Info);
            this.Helper.GameContent.InvalidateCache("Data/Objects");
        }
    }

    private void LogRegistryReconciliation(int detachedAnimals, int cancelledCycles)
    {
        if (detachedAnimals == 0 && cancelledCycles == 0)
            return;

        this.Monitor.Log(
            $"Reconciled external Ranch Slime state: detached {detachedAnimals} missing animal link(s) while preserving their genomes; cancelled {cancelledCycles} cycle(s) without a saved egg.",
            LogLevel.Info);
    }

    private Item CreateEggItem(Guid genomeId)
    {
        this.Helper.GameContent.InvalidateCache("Data/Objects");
        return ItemRegistry.Create(SlimeEggContent.GetQualifiedId(genomeId));
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        ConverterContent.EditAsset(e);
        SlimeEggContent.EditAsset(e, this.Helper.DirectoryPath);
        GuidebookContent.EditAsset(e);
        FarmAnimalContent.LoadAssets(e);
        FarmAnimalContent.EditAsset(e);
        BuildingContent.EditAsset(e, this.Helper);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && Game1.currentLocation.Objects.TryGetValue(e.Cursor.GrabTile, out StardewValley.Object? incubator)
            && incubator.QualifiedItemId == ConverterContent.IncubatorQualifiedId)
        {
            StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
                BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                && building.GetIndoors() == Game1.currentLocation);
            if (daycare?.GetIndoors() is not StardewValley.AnimalHouse)
            {
                this.Monitor.Log("The Slime Incubator only works inside the Slime Daycare.", LogLevel.Warn);
                return;
            }

            this.Helper.Input.Suppress(e.Button);
            string key = $"{Game1.currentLocation.NameOrUniqueName}:{(int)incubator.TileLocation.X},{(int)incubator.TileLocation.Y}";
            Game1.activeClickableMenu = new SlimeIncubatorMenu(this.registry!, key, index => this.StartIncubation(key, index));
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && Game1.currentLocation.Objects.TryGetValue(e.Cursor.GrabTile, out StardewValley.Object? placedObject)
            && placedObject.QualifiedItemId == ConverterContent.BreederQualifiedId)
        {
            this.Helper.Input.Suppress(e.Button);
            string machineKey = $"{Game1.currentLocation.NameOrUniqueName}:{(int)placedObject.TileLocation.X},{(int)placedObject.TileLocation.Y}";
            Game1.activeClickableMenu = new SlimeBreederMenu(this.registry!, machineKey, this.SaveExternalStateNow, this.CreateEggItem);
            return;
        }

        if (!Context.IsWorldReady || Game1.activeClickableMenu is not null || !e.Button.IsUseToolButton())
            return;

        if (Game1.player.CurrentItem?.QualifiedItemId != GuidebookContent.QualifiedItemId)
            return;

        this.Helper.Input.Suppress(e.Button);
        Game1.activeClickableMenu = new StardewValley.Menus.LetterViewerMenu(GuidebookContent.Text);
    }

    private void OnGiveTestEggsCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting test eggs.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To add test eggs to the currently loaded save, use: slime_ranching_give_test_eggs {saveName}", LogLevel.Warn);
            return;
        }

        if (Game1.player.Items.Count(item => item is null) < 2)
        {
            this.Monitor.Log("Make room for two separate eggs in your inventory, then try again.", LogLevel.Warn);
            return;
        }

        Guid mintId = Guid.NewGuid();
        Guid violetId = Guid.NewGuid();
        taums_slime_ranching.Models.SlimeEggRecord[] eggs =
        {
            this.registry!.CreateEgg(new taums_slime_ranching.Models.SlimeGenome
            {
                Id = mintId, PrimarySpecies = "Ranch Slime", VisualColor = "Mint", RarityTier = "Common",
                LikedFoodItemId = FoodLikeCatalog.ForSlime(mintId)
            }),
            this.registry!.CreateEgg(new taums_slime_ranching.Models.SlimeGenome
            {
                Id = violetId, PrimarySpecies = "Ranch Slime", VisualColor = "Violet", RarityTier = "Common",
                LikedFoodItemId = FoodLikeCatalog.ForSlime(violetId)
            })
        };

        try
        {
            if (!this.SaveExternalStateNow())
                return;
            this.Helper.GameContent.InvalidateCache("Data/Objects");
            foreach (taums_slime_ranching.Models.SlimeEggRecord egg in eggs)
            {
                Item item = ItemRegistry.Create(SlimeEggContent.GetQualifiedId(egg.Genome.Id));
                if (!Game1.player.addItemToInventoryBool(item))
                    throw new InvalidOperationException("Inventory space changed while the test eggs were being added.");
            }

            this.Monitor.Log("Added two distinct genome-specific Ranch Slime Eggs. Their genome records are in the mod's external registry; inspect the items and reload the save to check persistence.", LogLevel.Info);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            this.Monitor.Log($"Could not create the test eggs: {exception.Message}", LogLevel.Error);
        }
    }

    private void OnGiveConverterCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting a converter.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give a converter to the currently loaded save, use: slime_ranching_give_converter {saveName}", LogLevel.Warn);
            return;
        }

        Item converter = ItemRegistry.Create(ConverterContent.ConverterQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(converter) || !Game1.player.addItemToInventoryBool(converter))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Egg Converter, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Egg Converter to your inventory.", LogLevel.Info);
    }

    private void OnSpawnRanchSlimeCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before spawning a Ranch Slime.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To spawn a Ranch Slime in the currently loaded save, use: slime_ranching_spawn_ranch_slime {saveName}", LogLevel.Warn);
            return;
        }

        StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value) && building.GetIndoors() is StardewValley.AnimalHouse);
        if (daycare?.GetIndoors() is not StardewValley.AnimalHouse animalHouse)
        {
            this.Monitor.Log("Build the Slime Daycare first, then try again.", LogLevel.Warn);
            return;
        }

        if (animalHouse.animalsThatLiveHere.Count >= animalHouse.animalLimit.Value)
        {
            this.Monitor.Log("The Slime Daycare is full; make room before spawning a Ranch Slime.", LogLevel.Warn);
            return;
        }

        long animalId = this.Helper.Multiplayer.GetNewID();
        var animal = new FarmAnimal(FarmAnimalContent.RanchSlimeTypeId, animalId, Game1.player.UniqueMultiplayerID)
        {
            Name = "Ranch Slime",
            home = daycare,
            homeInterior = animalHouse
        };

        animalHouse.adoptAnimal(animal);
        this.registry!.RegisterRanchSlimes(new[] { animal });
        this.Monitor.Log($"Added one Ranch Slime to the Slime Daycare (FarmAnimal ID {animalId}). Sleep to save the animal and its external genome record.", LogLevel.Info);
    }

    private void OnGiveGuidebookCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting a guidebook.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give a guidebook to the currently loaded save, use: slime_ranching_give_guidebook {saveName}", LogLevel.Warn);
            return;
        }

        Item book = ItemRegistry.Create(GuidebookContent.QualifiedItemId);
        if (!Game1.player.couldInventoryAcceptThisItem(book) || !Game1.player.addItemToInventoryBool(book))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Ranching Field Guide, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added the Slime Ranching Field Guide. Select it and use the action button to read it.", LogLevel.Info);
    }

    private void OnGiveBreederCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting a breeder.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give a breeder to the currently loaded save, use: slime_ranching_give_breeder {saveName}", LogLevel.Warn);
            return;
        }

        Item breeder = ItemRegistry.Create(ConverterContent.BreederQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(breeder) || !Game1.player.addItemToInventoryBool(breeder))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Breeder, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Breeder. Place it inside the Slime Daycare and use it to select two Ranch Slimes.", LogLevel.Info);
    }

    private void OnGiveIncubatorCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting an incubator.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give an incubator to the currently loaded save, use: slime_ranching_give_incubator {saveName}", LogLevel.Warn);
            return;
        }

        Item incubator = ItemRegistry.Create(ConverterContent.IncubatorQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(incubator) || !Game1.player.addItemToInventoryBool(incubator))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Incubator, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Incubator. Place it inside the Slime Daycare and use it with a genome-specific Ranch Slime Egg in your inventory.", LogLevel.Info);
    }

    private void OnInspectCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Load a save before inspecting ranching status.", LogLevel.Info);
            return;
        }

        if (!this.stateReady)
        {
            this.Monitor.Log("External slime state is unavailable for this save; see earlier log errors.", LogLevel.Warn);
            return;
        }

        SlimeRegistry loadedRegistry = this.registry!;
        if (Game1.currentLocation is SlimeHutch hutch)
        {
            int slimeCount = hutch.characters.OfType<GreenSlime>().Count();
            this.Monitor.Log($"This vanilla Slime Hutch contains {slimeCount} GreenSlime monster(s), all left unchanged. Ranch Slimes live in the separate Slime Daycare; the external registry contains {loadedRegistry.Count} Ranch Slime genome record(s).", LogLevel.Info);
            return;
        }

        if (Game1.currentLocation is StardewValley.AnimalHouse animalHouse)
        {
            int ranchSlimeCount = Game1.getFarm().animals.Values.Count(animal =>
                animal.type.Value == FarmAnimalContent.RanchSlimeTypeId && animal.homeInterior == animalHouse);
            this.Monitor.Log($"This animal house contains {ranchSlimeCount} Ranch Slime(s). The external genome registry contains {loadedRegistry.Count} Ranch Slime record(s).", LogLevel.Info);
            return;
        }

        this.Monitor.Log("Enter the Slime Daycare or a vanilla Slime Hutch to inspect ranching status.", LogLevel.Info);
    }

}

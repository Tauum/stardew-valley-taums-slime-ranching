using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Monsters;
using HarmonyLib;
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
    private Texture2D? incubatorTexture;
    private readonly EnrichmentInteractionService trampolineInteractions = new();
    private readonly ShadowCloneVisualService shadowCloneVisuals = new();
    private readonly List<(long SlimeId, bool Success, double Chance)> pendingMutagenicNotifications = new();

    public override void Entry(IModHelper helper)
    {
        new Harmony(this.ModManifest.UniqueID).PatchAll(typeof(ModEntry).Assembly);
        this.config = helper.ReadConfig<ModConfig>();
        BuildingContent.NormalizeConfig(this.config);
        this.registry = new SlimeRegistry(this.Monitor);
        BreederMovementPatch.SetReservationLookup(id => this.registry?.IsBreedingParentReserved(id) == true);
        BreederParentVisibilityPatch.SetActiveCycleLookup(id => this.registry?.IsInActiveBreedingCycle(id) == true);
        this.stateStore = new ModStateStore(helper.DirectoryPath);

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.GameLoop.Saving += this.OnSaving;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.World.ObjectListChanged += this.OnObjectListChanged;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.ConsoleCommands.Add("slime_ranching_inspect", "Inspect vanilla slime or Ranch Slime counts in the current Slime Hutch or Slime Daycare.", this.OnInspectCommand);
        helper.ConsoleCommands.Add("slime_ranching_backup_external_data", "Back up the current save's external mod data. Usage: slime_ranching_backup_external_data <exact save folder name>.", this.OnBackupExternalDataCommand);
        helper.ConsoleCommands.Add("slime_ranching_spawn_ranch_slime", $"Spawn one Ranch Slime at a chosen age in the Slime Daycare. Usage: slime_ranching_spawn_ranch_slime <exact save folder name> <age days 0-{FarmAnimalContent.RanchSlimeDaysToMature}>.", this.OnSpawnRanchSlimeCommand);
        helper.ConsoleCommands.Add("slime_ranching_set_size", "Set a managed Ranch Slime's test genome size. Usage: slime_ranching_set_size <exact save folder name> <animal ID> <Tiny|Small|Normal|Large|Giant>.", this.OnSetSlimeSizeCommand);
        helper.ConsoleCommands.Add("slime_ranching_set_color", "Set a managed Ranch Slime's test base color. Usage: slime_ranching_set_color <exact save folder name> <animal ID> <Red|Orange|Yellow|Green|Blue|Purple|Pink>.", this.OnSetSlimeColorCommand);
        helper.ConsoleCommands.Add("slime_ranching_set_shiny", "Set a managed Ranch Slime's test shiny flag. Usage: slime_ranching_set_shiny <exact save folder name> <animal ID> <true|false>.", this.OnSetSlimeShinyCommand);
        helper.ConsoleCommands.Add("slime_ranching_set_mutation", "Set a managed Ranch Slime's test mutation. Usage: slime_ranching_set_mutation <exact save folder name> <animal ID> <ShadowClone|none>.", this.OnSetSlimeMutationCommand);
        helper.ConsoleCommands.Add("slime_ranching_bind_companion", "Bind one mature Daycare Ranch Slime as the external companion test. Usage: slime_ranching_bind_companion <exact save folder name> <animal ID>.", this.OnBindCompanionCommand);
        helper.ConsoleCommands.Add("slime_ranching_dismiss_companion", "Dismiss the currently bound Ranch Slime companion. Usage: slime_ranching_dismiss_companion <exact save folder name>.", this.OnDismissCompanionCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_converter", "Give one Slime Egg Converter for testing. Usage: slime_ranching_give_converter <exact save folder name>.", this.OnGiveConverterCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_guidebook", "Give the Slime Ranching Field Guide. Usage: slime_ranching_give_guidebook <exact save folder name>.", this.OnGiveGuidebookCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_breeder", "Give one Slime Breeder for testing. Usage: slime_ranching_give_breeder <exact save folder name>.", this.OnGiveBreederCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_incubator", "Give one Slime Incubator for testing. Usage: slime_ranching_give_incubator <exact save folder name>.", this.OnGiveIncubatorCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_mutagenic_pool", "Give one Mutagenic Pool for testing. Usage: slime_ranching_give_mutagenic_pool <exact save folder name>.", this.OnGiveMutagenicPoolCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_gel_refinery", "Give one Gel Refinery for testing. Usage: slime_ranching_give_gel_refinery <exact save folder name>.", this.OnGiveGelRefineryCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_compost_trough", "Give one Compost Trough for testing. Usage: slime_ranching_give_compost_trough <exact save folder name>.", this.OnGiveCompostTroughCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_tunnel", "Give one Slime Tunnel for testing. Usage: slime_ranching_give_tunnel <exact save folder name>.", this.OnGiveTunnelCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_heated_stone", "Give one Heated Slime Stone for testing. Usage: slime_ranching_give_heated_stone <exact save folder name>.", this.OnGiveHeatedStoneCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_pond", "Give one Slime Pond for testing. Usage: slime_ranching_give_pond <exact save folder name>.", this.OnGivePondCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_swing", "Give one Slime Swing for testing. Usage: slime_ranching_give_swing <exact save folder name>.", this.OnGiveSwingCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_fairy_lights", "Give one set of Slime Fairy Lights for testing. Usage: slime_ranching_give_fairy_lights <exact save folder name>.", this.OnGiveFairyLightsCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_test_eggs", "Create two external-genome test eggs. Usage: slime_ranching_give_test_eggs <exact save folder name>.", this.OnGiveTestEggsCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_color_eggs", "Create seven color test eggs. Usage: slime_ranching_give_color_eggs <exact save folder name>.", this.OnGiveColorEggsCommand);
        helper.ConsoleCommands.Add("slime_ranching_give_crystals", "Give one of each Chromatic Crystal for breeder testing. Usage: slime_ranching_give_crystals <exact save folder name>.", this.OnGiveCrystalsCommand);
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
            this.ApplyRanchSlimeAppearances();
            bool maturityChanged = this.registry.SynchronizeMaturity(GetFarmAnimals(), Game1.Date.TotalDays);
            this.stateReady = true;
            this.LogRegistryReconciliation(detachedAnimals, cancelledCycles);
            if (recoveredHatches > 0 || detachedAnimals > 0 || cancelledCycles > 0 || maturityChanged)
            {
                if (!this.SaveExternalStateNow())
                    this.Monitor.Log("Reconciled Ranch Slime records remain in memory but could not yet be written to the mod-folder registry.", LogLevel.Error);
                else if (maturityChanged)
                    this.InvalidateEggObjectData();
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException or System.Text.Json.JsonException or NotSupportedException)
        {
            this.Monitor.Log($"External slime state is disabled for this save: {exception.Message}", LogLevel.Error);
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IGenericModConfigMenuApi? api = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
            return;

        api.Register(this.ModManifest, this.ResetConfig, this.SaveConfig);
        api.AddSectionTitle(this.ModManifest, () => "Ranching equipment prices");
        api.AddParagraph(this.ModManifest, () => "Changes refresh Marnie's shop after saving these settings.");
        this.AddShopPriceOption(api, "Egg Converter", "Price of the vanilla Slime Egg converter.", () => this.config.ConverterShopPrice, value => this.config.ConverterShopPrice = value);
        this.AddShopPriceOption(api, "Slime Breeder", "Price of the Slime Breeder.", () => this.config.BreederShopPrice, value => this.config.BreederShopPrice = value);
        this.AddShopPriceOption(api, "Slime Incubator", "Price of the Slime Incubator.", () => this.config.IncubatorShopPrice, value => this.config.IncubatorShopPrice = value);
        this.AddShopPriceOption(api, "Slime Trampoline", "Price of the trampoline enrichment furniture.", () => this.config.TrampolineShopPrice, value => this.config.TrampolineShopPrice = value);
        this.AddShopPriceOption(api, "Slime Cushion", "Price of the cushion enrichment furniture.", () => this.config.SlimeCushionShopPrice, value => this.config.SlimeCushionShopPrice = value);
        this.AddShopPriceOption(api, "Slime Playground", "Price of the playground enrichment furniture.", () => this.config.SlimePlaygroundShopPrice, value => this.config.SlimePlaygroundShopPrice = value);
        this.AddShopPriceOption(api, "Slime Slide", "Price of the slide enrichment furniture.", () => this.config.SlimeSlideShopPrice, value => this.config.SlimeSlideShopPrice = value);
        this.AddShopPriceOption(api, "Slime Bed", "Price of the bed enrichment furniture.", () => this.config.SlimeBedShopPrice, value => this.config.SlimeBedShopPrice = value);
        this.AddShopPriceOption(api, "Gel Refinery", "Price of the Gel Refinery.", () => this.config.GelRefineryShopPrice, value => this.config.GelRefineryShopPrice = value);
        this.AddShopPriceOption(api, "Compost Trough", "Price of the juvenile-feeding Compost Trough.", () => this.config.CompostTroughShopPrice, value => this.config.CompostTroughShopPrice = value);
        this.AddShopPriceOption(api, "Slime Tunnel", "Price of the decorative crawl-through tunnel.", () => this.config.SlimeTunnelShopPrice, value => this.config.SlimeTunnelShopPrice = value);
        this.AddShopPriceOption(api, "Heated Slime Stone", "Price of the warm resting stone.", () => this.config.HeatedSlimeStoneShopPrice, value => this.config.HeatedSlimeStoneShopPrice = value);
        this.AddShopPriceOption(api, "Slime Pond", "Price of the decorative slime pond.", () => this.config.SlimePondShopPrice, value => this.config.SlimePondShopPrice = value);
        this.AddShopPriceOption(api, "Slime Swing", "Price of the decorative slime swing.", () => this.config.SlimeSwingShopPrice, value => this.config.SlimeSwingShopPrice = value);
        this.AddShopPriceOption(api, "Slime Fairy Lights", "Price of the evening decorative lights.", () => this.config.SlimeFairyLightsShopPrice, value => this.config.SlimeFairyLightsShopPrice = value);
        api.AddSectionTitle(this.ModManifest, () => "Compost Trough");
        api.AddParagraph(this.ModManifest, () => "Growth advances are provisional. A favorite-food item grants the base days plus the favorite-food bonus.");
        api.AddNumberOption(this.ModManifest,
            () => this.config.CompostTroughGrowthDays,
            value => this.config.CompostTroughGrowthDays = value,
            () => "Growth days per feeding",
            () => "Base maturity days advanced by one accepted item.", 1, 3, 1);
        api.AddNumberOption(this.ModManifest,
            () => this.config.CompostTroughLikedFoodBonusDays,
            value => this.config.CompostTroughLikedFoodBonusDays = value,
            () => "Favorite-food bonus",
            () => "Extra maturity days when the item matches this slime's stored favorite food.", 0, 2, 1);
        api.AddSectionTitle(this.ModManifest, () => "Gel Refinery");
        api.AddParagraph(this.ModManifest, () => "The first recipe is provisional: Broken Glasses and vanilla Slime fuel become Refined Quartz.");
        api.AddNumberOption(this.ModManifest,
            () => this.config.GelRefinerySlimeFuelCost,
            value => this.config.GelRefinerySlimeFuelCost = value,
            () => "Slime fuel per batch",
            () => "Vanilla Slime consumed for one Broken Glasses to Refined Quartz batch.", 1, 999_999, 1);
        api.AddNumberOption(this.ModManifest,
            () => (float)this.config.GelRefineryDurationDays,
            value => this.config.GelRefineryDurationDays = (int)MathF.Round(value),
            () => "Processing days",
            () => "Whole in-game days per batch (1–28).", 1f, 28f, 1f,
            value => $"{MathF.Round(value):0} day(s)");
        api.AddSectionTitle(this.ModManifest, () => "Mutagenic Pool");
        api.AddParagraph(this.ModManifest, () => "Balance values are provisional. Changes apply when you save these settings; active attempts keep their stored chance and ready day.");
        api.AddBoolOption(this.ModManifest,
            () => this.config.ShowShadowCloneVisualEchoes,
            value => this.config.ShowShadowCloneVisualEchoes = value,
            () => "Show Shadow Clone echoes",
            () => "Show the temporary visual duplicate around Shadow Clone slimes in the Daycare.");
        api.AddNumberOption(this.ModManifest,
            () => this.config.MutagenicPoolShopPrice,
            value => this.config.MutagenicPoolShopPrice = value,
            () => "Shop price",
            () => "Gold price at Marnie's shop.", 0, 999_999, 100);
        api.AddNumberOption(this.ModManifest,
            () => this.config.MutagenicPoolSlimeCost,
            value => this.config.MutagenicPoolSlimeCost = value,
            () => "Slime cost",
            () => "Vanilla Slime consumed per attempt.", 1, 999_999, 25);
        api.AddNumberOption(this.ModManifest,
            () => (float)this.config.MutagenicPoolDurationDays,
            value => this.config.MutagenicPoolDurationDays = (int)MathF.Round(value),
            () => "Processing days",
            () => "Whole in-game days per attempt (1–28).", 1f, 28f, 1f,
            value => $"{MathF.Round(value):0} day(s)");
        api.AddNumberOption(this.ModManifest,
            () => (float)this.config.MutagenicPoolBaseChance,
            value => this.config.MutagenicPoolBaseChance = value,
            () => "Base chance",
            () => "Chance before this slime has any failed attempts.", 0f, 1f, 0.01f,
            value => $"{value:P0}");
        api.AddNumberOption(this.ModManifest,
            () => (float)this.config.MutagenicPoolChancePerFailure,
            value => this.config.MutagenicPoolChancePerFailure = value,
            () => "Pity increase",
            () => "Chance added after each failed attempt for that slime.", 0f, 1f, 0.01f,
            value => $"+{value:P0}");
        api.AddNumberOption(this.ModManifest,
            () => (float)this.config.MutagenicPoolMaximumChance,
            value => this.config.MutagenicPoolMaximumChance = value,
            () => "Chance cap",
            () => "Maximum chance after repeated failures.", 0f, 1f, 0.01f,
            value => $"{value:P0}");
    }

    private void AddShopPriceOption(IGenericModConfigMenuApi api, string name, string tooltip, Func<int> getValue, Action<int> setValue) =>
        api.AddNumberOption(this.ModManifest, getValue, setValue, () => name, () => tooltip, 0, 999_999, 100,
            value => $"{value:N0}g");

    private void ResetConfig()
    {
        this.config = new ModConfig();
    }

    private void SaveConfig()
    {
        MutagenicPoolRules.NormalizeConfig(this.config);
        this.Helper.WriteConfig(this.config);
        this.Helper.GameContent.InvalidateCache("Data/Shops");
        this.Helper.GameContent.InvalidateCache("Data/Buildings");
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
            this.registry.SynchronizeMaturity(GetFarmAnimals(), Game1.Date.TotalDays);
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

        bool maturityChanged = this.registry!.SynchronizeMaturity(GetFarmAnimals(), Game1.Date.TotalDays);
        var readyToNotify = new List<(taums_slime_ranching.Models.BreedingPairRecord Pair, taums_slime_ranching.Models.SlimeEggRecord Egg)>();
        foreach (taums_slime_ranching.Models.BreedingPairRecord pair in this.registry!.BreedingPairs.ToArray())
        {
            taums_slime_ranching.Models.SlimeEggRecord? egg = this.registry.GetOrCreateBreedingEgg(pair.MachineKey, Game1.Date.TotalDays, out _);
            if (egg is not null && !pair.ReadyNotificationSent)
            {
                pair.ReadyNotificationSent = true;
                readyToNotify.Add((pair, egg));
            }
        }

        List<RefineryJobRecord> refineryReady = new();
        foreach (RefineryJobRecord job in this.registry.RefineryJobs)
        {
            if (Game1.Date.TotalDays >= job.ReadyOnDay && !job.ReadyNotificationSent)
            {
                job.ReadyNotificationSent = true;
                refineryReady.Add(job);
            }
        }

        this.ProcessReadyIncubations();
        maturityChanged |= this.registry.SynchronizeMaturity(GetFarmAnimals(), Game1.Date.TotalDays);
        this.pendingMutagenicNotifications.AddRange(this.registry.ResolveReadyMutagenicAttempts(Game1.Date.TotalDays));

        if (readyToNotify.Count == 0 && refineryReady.Count == 0 && this.pendingMutagenicNotifications.Count == 0)
        {
            if (maturityChanged && !this.SaveExternalStateNow())
                this.Monitor.Log("Could not save the Ranch Slime maturity clock to the mod-folder registry; it will retry at the next save.", LogLevel.Error);
            else if (maturityChanged)
                this.InvalidateEggObjectData();
            return;
        }

        if (!this.SaveExternalStateNow())
        {
            foreach (var ready in readyToNotify)
                ready.Pair.ReadyNotificationSent = false;
            foreach (RefineryJobRecord job in refineryReady)
                job.ReadyNotificationSent = false;
            this.Monitor.Log("A completed breeding or mutation result could not be saved; notifications will retry after the next external-state save.", LogLevel.Error);
            return;
        }
        if (maturityChanged)
            this.InvalidateEggObjectData();

        foreach (var ready in readyToNotify)
        {
            taums_slime_ranching.Models.SlimeEggRecord egg = ready.Egg;
            Game1.addHUDMessage(new HUDMessage("A Ranch Slime Egg is ready to collect from the Slime Breeder!", HUDMessage.newQuest_type));
            this.Monitor.Log($"A Ranch Slime Egg (genome {egg.Genome.Id:N}) is ready to collect.", LogLevel.Info);
        }

        foreach (var result in this.pendingMutagenicNotifications)
        {
            string message = result.Success
                ? "A Ranch Slime's mutation has awakened!"
                : "The Mutagenic Pool attempt failed safely. That slime's next chance has increased.";
            Game1.addHUDMessage(new HUDMessage(message, result.Success ? HUDMessage.newQuest_type : HUDMessage.error_type));
            this.Monitor.Log($"Mutagenic Pool attempt for Ranch Slime {result.SlimeId}: {(result.Success ? "success" : "safe failure")} at {result.Chance:P0}.", LogLevel.Info);
        }
        this.pendingMutagenicNotifications.Clear();

        foreach (RefineryJobRecord job in refineryReady)
        {
            Game1.addHUDMessage(new HUDMessage("A Gel Refinery batch is ready to collect!", HUDMessage.newQuest_type));
            this.Monitor.Log($"Gel Refinery batch at {job.MachineKey} is ready: {job.OutputCount} Refined Quartz.", LogLevel.Info);
        }
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.trampolineInteractions.Clear();
        this.shadowCloneVisuals.Clear();
        this.pendingMutagenicNotifications.Clear();
        this.stateReady = false;
        this.registry = new SlimeRegistry(this.Monitor);
        this.stateStore = new ModStateStore(this.Helper.DirectoryPath);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsWorldReady && Context.IsMainPlayer)
            this.trampolineInteractions.Update(Game1.currentLocation, Game1.currentGameTime, this.registry);

        if (!this.stateReady || !Context.IsMainPlayer)
            return;

        if (e.IsMultipleOf(60) && this.DiscardBreederPairsForMissingMachines())
        {
            if (!this.SaveExternalStateNow())
                this.Monitor.Log("Removed breeder cycles remain cleared in memory, but the external registry could not be saved yet.", LogLevel.Error);
        }

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

    private void OnObjectListChanged(object? sender, StardewModdingAPI.Events.ObjectListChangedEventArgs e)
    {
        if (!this.stateReady || !Context.IsMainPlayer)
            return;

        bool removedBreederPair = false;
        string locationPrefix = e.Location.NameOrUniqueName + ":";
        foreach (KeyValuePair<Vector2, StardewValley.Object> removed in e.Removed)
        {
            if (removed.Value.QualifiedItemId != ConverterContent.BreederQualifiedId)
                continue;

            string machineKey = $"{locationPrefix}{(int)removed.Key.X},{(int)removed.Key.Y}";
            removedBreederPair |= this.registry!.RemoveBreederPair(machineKey);
        }

        if (!removedBreederPair)
            return;

        if (this.SaveExternalStateNow())
            this.Monitor.Log("The Slime Breeder was removed; its cycle and pending egg were discarded, and its parents are free to roam. A replacement starts with a blank selection.", LogLevel.Info);
        else
            this.Monitor.Log("The Slime Breeder was removed and its parents are free to roam in memory, but the cleared cycle could not yet be saved to the external registry.", LogLevel.Error);
    }

    private void OnRenderedWorld(object? sender, StardewModdingAPI.Events.RenderedWorldEventArgs e)
    {
        if (!this.stateReady || !Context.IsMainPlayer)
            return;

        this.DrawIncubators(e.SpriteBatch);

        if (this.config.ShowShadowCloneVisualEchoes)
            this.shadowCloneVisuals.Draw(Game1.currentLocation, e.SpriteBatch, this.registry!);
        this.DrawFairyLightGlow(e.SpriteBatch);
        if (Game1.currentLocation != Game1.getFarm())
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

    private void DrawFairyLightGlow(SpriteBatch batch)
    {
        if (Game1.timeOfDay < 1800 || Game1.timeOfDay >= 2600)
            return;
        if (Game1.currentLocation is not StardewValley.AnimalHouse house
            || !Game1.getFarm().buildings.Any(building =>
                BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                && ReferenceEquals(building.GetIndoors(), house)))
            return;

        Color[] colors = { new(246, 104, 133), new(112, 207, 237), new(255, 210, 102), new(175, 139, 237) };
        int[] bulbXs = { 3, 6, 9, 12 };
        foreach (StardewValley.Object lights in house.Objects.Values.Where(obj => obj.QualifiedItemId == FurnitureContent.SlimeFairyLightsQualifiedId))
        {
            for (int i = 0; i < bulbXs.Length; i++)
            {
                Vector2 world = lights.TileLocation * 64f + new Vector2(bulbXs[i] * 4f + 8f, -64f + 8f * 4f + 8f);
                Vector2 screen = Game1.GlobalToLocal(Game1.viewport, world);
                DrawGlow(batch, (int)screen.X, (int)screen.Y, colors[i]);
            }
        }
    }

    private static void DrawGlow(SpriteBatch batch, int x, int y, Color color)
    {
        Texture2D pixel = Game1.staminaRect;
        batch.Draw(pixel, new Rectangle(x - 16, y - 16, 32, 32), color * 0.10f);
        batch.Draw(pixel, new Rectangle(x - 10, y - 10, 20, 20), color * 0.18f);
        batch.Draw(pixel, new Rectangle(x - 5, y - 5, 10, 10), color * 0.28f);
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

    private void ApplyRanchSlimeAppearances()
    {
        if (this.registry is null)
            return;

        foreach (FarmAnimal animal in GetFarmAnimals())
        {
            if (animal.type.Value != FarmAnimalContent.RanchSlimeTypeId)
                continue;
            FarmAnimalContent.ApplyAppearance(animal,
                this.registry.GetGenomeColor(animal.myID.Value),
                this.registry.GetGenomeSize(animal.myID.Value),
                this.registry.GetGenomeIsShiny(animal.myID.Value));
        }
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
            || Game1.player.Items[inventoryIndex] is not Item item)
            return false;

        SlimeRegistry registry = this.registry!;
        string eggItemId;
        bool createdCommonGenome = false;
        if (item.QualifiedItemId == ConverterContent.CommonEggQualifiedId)
        {
            Guid genomeId = Guid.NewGuid();
            SlimeEggRecord commonEgg = registry.CreateEgg(new SlimeGenome
            {
                Id = genomeId,
                PrimarySpecies = "Ranch Slime",
                VisualColor = "#00FF00",
                Size = "Normal",
                RarityTier = "Common",
                Generation = 1,
                LikedFoodItemId = FoodLikeCatalog.ForSlime(genomeId)
            });
            eggItemId = commonEgg.ItemId;
            createdCommonGenome = true;
        }
        else if (item.QualifiedItemId.StartsWith("(O)Taum.SlimeRanching_Egg_", StringComparison.Ordinal))
            eggItemId = item.QualifiedItemId[3..];
        else
            return false;

        if (!registry.CanIncubate(eggItemId)
            || !registry.StartIncubation(machineKey, eggItemId, Game1.Date.TotalDays))
        {
            if (createdCommonGenome)
                registry.RemoveUnincubatedEgg(eggItemId);
            return false;
        }

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
            this.InvalidateEggObjectData();
        }

        foreach (IncubationRecord incubation in this.registry.Incubations
                     .Where(record => record.ReadyOnDay <= Game1.Date.TotalDays).ToArray())
        {
            if (!this.registry.TryGetEgg(incubation.EggItemId, out SlimeEggRecord egg))
                continue;

            if (incubation.HatchAnimalId is long recordedAnimalId)
            {
                FarmAnimal? recordedAnimal = GetFarmAnimals().FirstOrDefault(animal => animal.myID.Value == recordedAnimalId);
                if (recordedAnimal?.type.Value == FarmAnimalContent.RanchSlimeTypeId)
                {
                    this.registry.RegisterIncubationHatch(incubation, recordedAnimal);
                    continue;
                }
            }

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
            animal.age.Value = 0;
            FarmAnimalContent.ApplyAppearance(animal, egg.Genome.VisualColor, egg.Genome.Size, egg.Genome.IsShiny);
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
            this.InvalidateEggObjectData();
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
        this.InvalidateEggObjectData();
        return ItemRegistry.Create(SlimeEggContent.GetQualifiedId(genomeId));
    }

    private void InvalidateEggObjectData()
    {
        SlimeEggContent.InvalidateCache();
        this.Helper.GameContent.InvalidateCache("Data/Objects");
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        ConverterContent.LoadAssets(e);
        if (e.NameWithoutLocale.IsEquivalentTo("Mods/Taum.SlimeRanching/Machines/SlimeIncubator"))
            e.LoadFromModFile<Texture2D>("assets/Machines/SlimeIncubator.png", AssetLoadPriority.Medium);
        else if (e.NameWithoutLocale.IsEquivalentTo("Mods/Taum.SlimeRanching/Machines/SlimeBreeder"))
            e.LoadFromModFile<Texture2D>("assets/Machines/SlimeBreeder.png", AssetLoadPriority.Medium);
        BuildingContent.LoadAssets(e, this.Helper);
        ConverterContent.EditAsset(e);
        ChromaticCrystalContent.EditAsset(e);
        ShopContent.EditAsset(e, this.config);
        SlimeEggContent.LoadAssets(e);
        SlimeEggContent.EditAsset(e, this.Helper.DirectoryPath);
        GuidebookContent.EditAsset(e);
        FarmAnimalContent.LoadAssets(e);
        FarmAnimalContent.EditAsset(e);
        FurnitureContent.LoadAssets(e);
        FurnitureContent.EditAsset(e);
        GelRefineryContent.LoadAssets(e);
        GelRefineryContent.EditAsset(e);
        MutagenicPoolContent.LoadAssets(e);
        MutagenicPoolContent.EditAsset(e);
        BuildingContent.EditAsset(e, this.Helper, this.config);
    }

    private void DrawIncubators(SpriteBatch batch)
    {
        if (this.incubatorTexture is null)
            this.incubatorTexture = this.Helper.GameContent.Load<Texture2D>("Mods/Taum.SlimeRanching/Machines/SlimeIncubator");

        const int footprintWidth = 4 * 64;
        const int spriteHeight = 320;
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in Game1.currentLocation.Objects.Pairs)
        {
            if (pair.Value.QualifiedItemId != ConverterContent.IncubatorQualifiedId)
                continue;

            Vector2 worldTopLeft = new(pair.Key.X * 64, (pair.Key.Y + 2) * 64 - spriteHeight);
            Vector2 screenTopLeft = Game1.GlobalToLocal(Game1.viewport, worldTopLeft);
            Rectangle destination = new((int)screenTopLeft.X, (int)screenTopLeft.Y, footprintWidth, spriteHeight);
            batch.Draw(this.incubatorTexture, destination,
                new Rectangle(0, 0, this.incubatorTexture.Width, this.incubatorTexture.Height),
                Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.999f);
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && this.TryGetIncubatorAt(e.Cursor.GrabTile, out Vector2 incubatorAnchor, out StardewValley.Object? incubator))
        {
            if (!this.stateReady)
            {
                this.Monitor.Log("The external Ranch Slime registry is unavailable; the incubator is disabled for this save.", LogLevel.Error);
                return;
            }

            StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
                BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                && building.GetIndoors() == Game1.currentLocation);
            if (daycare?.GetIndoors() is not StardewValley.AnimalHouse)
            {
                this.Monitor.Log("The Slime Incubator only works inside the Slime Daycare.", LogLevel.Warn);
                return;
            }

            this.Helper.Input.Suppress(e.Button);
            string key = $"{Game1.currentLocation.NameOrUniqueName}:{(int)incubatorAnchor.X},{(int)incubatorAnchor.Y}";
            this.incubatorTexture ??= this.Helper.GameContent.Load<Texture2D>("Mods/Taum.SlimeRanching/Machines/SlimeIncubator");
            Game1.activeClickableMenu = new SlimeIncubatorMenu(this.registry!, key,
                index => this.StartIncubation(key, index),
                name =>
                {
                    IncubationRecord? record = this.registry!.GetIncubation(key);
                    if (record?.HatchAnimalId is not long animalId)
                        return false;
                    FarmAnimal? animal = GetFarmAnimals().FirstOrDefault(candidate => candidate.myID.Value == animalId);
                    if (animal is null)
                        return false;

                    string previousName = animal.Name;
                    animal.Name = string.IsNullOrWhiteSpace(name) ? "Ranch Slime" : name.Trim();
                    if (!this.registry.CompleteIncubation(key))
                    {
                        animal.Name = previousName;
                        return false;
                    }
                    if (this.SaveExternalStateNow())
                        return true;
                    this.registry.RestoreIncubation(record);
                    animal.Name = previousName;
                    return false;
                },
                this.incubatorTexture);
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && Game1.currentLocation.Objects.TryGetValue(e.Cursor.GrabTile, out StardewValley.Object? refinery)
            && refinery.QualifiedItemId == GelRefineryContent.QualifiedItemId)
        {
            if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer)
            {
                this.Monitor.Log("The Gel Refinery requires an available external registry in single-player.", LogLevel.Warn);
                return;
            }

            StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
                BuildingContent.IsSlimeDaycare(building.buildingType.Value) && building.GetIndoors() == Game1.currentLocation);
            if (daycare?.GetIndoors() is not StardewValley.AnimalHouse)
            {
                this.Monitor.Log("The Gel Refinery only works inside the Slime Daycare.", LogLevel.Warn);
                return;
            }

            this.Helper.Input.Suppress(e.Button);
            string machineKey = $"{Game1.currentLocation.NameOrUniqueName}:{(int)refinery.TileLocation.X},{(int)refinery.TileLocation.Y}";
            Game1.activeClickableMenu = new GelRefineryMenu(this.registry!, machineKey, GelRefineryRules.FromConfig(this.config),
                () => this.StartRefineryBatch(machineKey), () => this.CollectRefineryBatch(machineKey));
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && Game1.currentLocation.Objects.TryGetValue(e.Cursor.GrabTile, out StardewValley.Object? trough)
            && trough.QualifiedItemId == FurnitureContent.CompostTroughQualifiedId)
        {
            if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer)
            {
                this.Monitor.Log("The Compost Trough requires an available external registry in single-player.", LogLevel.Warn);
                return;
            }

            StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
                BuildingContent.IsSlimeDaycare(building.buildingType.Value) && building.GetIndoors() == Game1.currentLocation);
            if (daycare?.GetIndoors() is not StardewValley.AnimalHouse)
            {
                this.Monitor.Log("The Compost Trough only works inside the Slime Daycare.", LogLevel.Warn);
                return;
            }

            this.Helper.Input.Suppress(e.Button);
            Game1.activeClickableMenu = new CompostTroughMenu(this.registry!, this.FeedJuvenileAtCompostTrough);
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && this.TryGetBreederAt(e.Cursor.GrabTile, out Vector2 breederAnchor))
        {
            if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer)
            {
                this.Monitor.Log("The Slime Breeder requires an available external registry in single-player.", LogLevel.Error);
                return;
            }

            this.Helper.Input.Suppress(e.Button);
            string machineKey = $"{Game1.currentLocation.NameOrUniqueName}:{(int)breederAnchor.X},{(int)breederAnchor.Y}";
            if (this.DiscardBreederPairsForMissingMachines(Game1.currentLocation))
                this.SaveExternalStateNow();
            Game1.activeClickableMenu = new SlimeBreederMenu(this.registry!, machineKey, this.SaveExternalStateNow, this.CreateEggItem);
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && Game1.currentLocation.Objects.TryGetValue(e.Cursor.GrabTile, out StardewValley.Object? pool)
            && pool.QualifiedItemId == MutagenicPoolContent.QualifiedItemId)
        {
            if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer)
            {
                this.Monitor.Log("The Mutagenic Pool requires an available external registry in single-player.", LogLevel.Warn);
                return;
            }

            StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
                BuildingContent.IsSlimeDaycare(building.buildingType.Value) && building.GetIndoors() == Game1.currentLocation);
            if (daycare?.GetIndoors() is not StardewValley.AnimalHouse)
            {
                this.Monitor.Log("The Mutagenic Pool only works inside the Slime Daycare.", LogLevel.Warn);
                return;
            }

            this.Helper.Input.Suppress(e.Button);
            string machineKey = $"{Game1.currentLocation.NameOrUniqueName}:{(int)pool.TileLocation.X},{(int)pool.TileLocation.Y}";
            Game1.activeClickableMenu = new MutagenicPoolMenu(this.registry!, machineKey, MutagenicPoolRules.FromConfig(this.config));
            return;
        }

        if (!Context.IsWorldReady || Game1.activeClickableMenu is not null || !e.Button.IsUseToolButton())
            return;

        if (Game1.player.CurrentItem?.QualifiedItemId != GuidebookContent.QualifiedItemId)
            return;

        this.Helper.Input.Suppress(e.Button);
        Game1.activeClickableMenu = new StardewValley.Menus.LetterViewerMenu(GuidebookContent.Text);
    }

    private bool TryGetIncubatorAt(Vector2 clickedTile, out Vector2 anchor, out StardewValley.Object? incubator)
    {
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in Game1.currentLocation.Objects.Pairs)
        {
            if (pair.Value.QualifiedItemId != ConverterContent.IncubatorQualifiedId)
                continue;

            bool insideMachine = clickedTile.X >= pair.Key.X && clickedTile.X < pair.Key.X + 4
                && clickedTile.Y >= pair.Key.Y - 3 && clickedTile.Y < pair.Key.Y + 2;
            if (!insideMachine)
                continue;

            anchor = pair.Key;
            incubator = pair.Value;
            return true;
        }

        anchor = Vector2.Zero;
        incubator = null;
        return false;
    }

    private bool TryGetBreederAt(Vector2 clickedTile, out Vector2 anchor)
    {
        foreach (KeyValuePair<Vector2, StardewValley.Object> pair in Game1.currentLocation.Objects.Pairs)
        {
            if (pair.Value.QualifiedItemId != ConverterContent.BreederQualifiedId)
                continue;

            if (clickedTile.X < pair.Key.X || clickedTile.X >= pair.Key.X + 5
                || clickedTile.Y < pair.Key.Y - 2 || clickedTile.Y >= pair.Key.Y + 2)
                continue;

            anchor = pair.Key;
            return true;
        }

        anchor = Vector2.Zero;
        return false;
    }

    private bool DiscardBreederPairsForMissingMachines(GameLocation? onlyLocation = null)
    {
        if (this.registry is null)
            return false;

        int removed = 0;
        foreach (StardewValley.Buildings.Building building in Game1.getFarm().buildings)
        {
            if (!BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                || building.GetIndoors() is not GameLocation interior
                || (onlyLocation is not null && interior != onlyLocation))
                continue;

            string prefix = interior.NameOrUniqueName + ":";
            string[] placedMachineKeys = interior.Objects.Values
                .Where(obj => obj.QualifiedItemId == ConverterContent.BreederQualifiedId)
                .Select(obj => $"{prefix}{(int)obj.TileLocation.X},{(int)obj.TileLocation.Y}")
                .ToArray();
            removed += this.registry.RemoveBreederPairsForMissingMachines(prefix, placedMachineKeys);
        }

        if (removed == 0)
            return false;

        this.Monitor.Log($"Cleared {removed} breeder cycle(s) because their machine was removed. Parent Ranch Slimes are free to roam; a replacement breeder starts with no selection.", LogLevel.Info);
        return true;
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
                Id = mintId, PrimarySpecies = "Ranch Slime", VisualColor = "#65DDBB", RarityTier = "Common",
                LikedFoodItemId = FoodLikeCatalog.ForSlime(mintId)
            }),
            this.registry!.CreateEgg(new taums_slime_ranching.Models.SlimeGenome
            {
                Id = violetId, PrimarySpecies = "Ranch Slime", VisualColor = "#8A2BE2", RarityTier = "Common",
                LikedFoodItemId = FoodLikeCatalog.ForSlime(violetId)
            })
        };

        try
        {
            if (!this.SaveExternalStateNow())
                return;
            this.InvalidateEggObjectData();
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

    private void OnGiveColorEggsCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting color test eggs.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To add color test eggs to the currently loaded save, use: slime_ranching_give_color_eggs {saveName}", LogLevel.Warn);
            return;
        }

        const int colorCount = 7;
        if (Game1.player.Items.Count(item => item is null) < colorCount)
        {
            this.Monitor.Log("Make room for seven separate eggs in your inventory, then try again.", LogLevel.Warn);
            return;
        }

        string[] colors = { "#FF0000", "#FF9500", "#FFD60A", "#00FF00", "#0A84FF", "#BF5AF2", "#FF69B4" };
        SlimeEggRecord[] eggs = colors.Select(color =>
        {
            Guid genomeId = Guid.NewGuid();
            return this.registry!.CreateEgg(new SlimeGenome
            {
                Id = genomeId,
                PrimarySpecies = "Ranch Slime",
                VisualColor = color,
                Generation = 1,
                RarityTier = "Common",
                LikedFoodItemId = FoodLikeCatalog.ForSlime(genomeId)
            });
        }).ToArray();

        try
        {
            if (!this.SaveExternalStateNow())
                return;
            this.InvalidateEggObjectData();
            foreach (SlimeEggRecord egg in eggs)
                Game1.player.addItemToInventoryBool(ItemRegistry.Create(SlimeEggContent.GetQualifiedId(egg.Genome.Id)));

            this.Monitor.Log("Added Red, Orange, Yellow, Green, Blue, Purple, and Pink genome-specific test eggs. Hatch them in the Daycare to compare sprite colors.", LogLevel.Info);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            this.Monitor.Log($"Could not create the color test eggs: {exception.Message}", LogLevel.Error);
        }
    }

    private void OnGiveCrystalsCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer)
        {
            this.Monitor.Log("Load a single-player save before requesting Chromatic Crystals.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give crystals to the currently loaded save, use: slime_ranching_give_crystals {saveName}", LogLevel.Warn);
            return;
        }

        string[] colors = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Pink" };
        if (Game1.player.Items.Count(item => item is null) < colors.Length)
        {
            this.Monitor.Log("Make room for seven separate crystals in your inventory, then try again.", LogLevel.Warn);
            return;
        }

        foreach (string color in colors)
            Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)" + ChromaticCrystalContent.GetItemId(color)));
        this.Monitor.Log("Added one Red, Orange, Yellow, Green, Blue, Purple, and Pink Chromatic Crystal.", LogLevel.Info);
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
        if (args.Length != 2 || args[0] != saveName
            || !int.TryParse(args[1], out int ageDays)
            || ageDays < 0 || ageDays > FarmAnimalContent.RanchSlimeDaysToMature)
        {
            this.Monitor.Log($"Usage: slime_ranching_spawn_ranch_slime {saveName} <age days 0-{FarmAnimalContent.RanchSlimeDaysToMature}>.", LogLevel.Warn);
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
        // Set the requested age after adoption in case AnimalHouse initializes it.
        // Initialize the mod-owned clock now so the breeder sees the chosen age.
        animal.age.Value = ageDays;
        this.registry!.RegisterRanchSlimes(new[] { animal });
        this.registry.SynchronizeMaturity(new[] { animal }, Game1.Date.TotalDays);

        if (!this.SaveExternalStateNow())
            this.Monitor.Log($"Ranch Slime {animalId} was added, but its external genome record could not be saved yet.", LogLevel.Error);
        else
            this.Monitor.Log($"Added one {ageDays}-day-old Ranch Slime to the Slime Daycare (FarmAnimal ID {animalId}). It is {(ageDays >= FarmAnimalContent.RanchSlimeDaysToMature ? "mature and ready to select in the breeder" : "still growing") }.", LogLevel.Info);
    }

    private void OnSetSlimeSizeCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before changing a test genome.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 3 || args[0] != saveName || !long.TryParse(args[1], out long animalId))
        {
            this.Monitor.Log($"Usage: slime_ranching_set_size {saveName} <animal ID> <Tiny|Small|Normal|Large|Giant>", LogLevel.Warn);
            return;
        }

        string? oldSize = this.registry!.GetGenomeSize(animalId);
        if (oldSize is null || !this.registry.SetGenomeSize(animalId, args[2]))
        {
            this.Monitor.Log("That ID is not a managed Ranch Slime, or the requested size is invalid.", LogLevel.Warn);
            return;
        }

        if (!this.SaveExternalStateNow())
        {
            this.registry.SetGenomeSize(animalId, oldSize);
            FarmAnimal? animalToRestore = GetFarmAnimals().FirstOrDefault(candidate => candidate.myID.Value == animalId);
            if (animalToRestore is not null)
                FarmAnimalContent.ApplyAppearance(animalToRestore,
                    this.registry.GetGenomeColor(animalId), this.registry.GetGenomeSize(animalId), this.registry.GetGenomeIsShiny(animalId));
            this.Monitor.Log("Could not save external state; the original genome size was restored.", LogLevel.Error);
            return;
        }

        FarmAnimal? animalToUpdate = GetFarmAnimals().FirstOrDefault(candidate => candidate.myID.Value == animalId);
        if (animalToUpdate is not null)
            FarmAnimalContent.ApplyAppearance(animalToUpdate,
                this.registry.GetGenomeColor(animalId), this.registry.GetGenomeSize(animalId), this.registry.GetGenomeIsShiny(animalId));
        this.Monitor.Log($"Changed Ranch Slime {animalId} genome size from {oldSize} to {this.registry.GetGenomeSize(animalId)} in the mod's external registry.", LogLevel.Info);
    }

    private void OnSetSlimeShinyCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before changing a test genome.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 3 || args[0] != saveName || !long.TryParse(args[1], out long animalId)
            || !bool.TryParse(args[2], out bool isShiny))
        {
            this.Monitor.Log($"Usage: slime_ranching_set_shiny {saveName} <animal ID> <true|false>", LogLevel.Warn);
            return;
        }

        bool oldValue = this.registry!.GetGenomeIsShiny(animalId);
        if (this.registry.GetGenomeSize(animalId) is null || !this.registry.SetGenomeShiny(animalId, isShiny))
        {
            this.Monitor.Log("That ID is not a managed Ranch Slime.", LogLevel.Warn);
            return;
        }

        if (!this.SaveExternalStateNow())
        {
            this.registry.SetGenomeShiny(animalId, oldValue);
            this.RefreshRanchSlimeAppearance(animalId);
            this.Monitor.Log("Could not save external state; the original shiny flag was restored.", LogLevel.Error);
            return;
        }

        this.RefreshRanchSlimeAppearance(animalId);
        this.Monitor.Log($"Changed Ranch Slime {animalId} shiny flag to {isShiny} in the mod's external registry.", LogLevel.Info);
    }

    private void OnSetSlimeColorCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before changing a test genome.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 3 || args[0] != saveName || !long.TryParse(args[1], out long animalId))
        {
            this.Monitor.Log($"Usage: slime_ranching_set_color {saveName} <animal ID> <Red|Orange|Yellow|Green|Blue|Purple|Pink>", LogLevel.Warn);
            return;
        }

        var oldState = this.registry!.GetGenomeColorState(animalId);
        if (oldState is null || !this.registry.SetGenomeBaseColor(animalId, args[2]))
        {
            this.Monitor.Log("That ID is not a managed Ranch Slime, or the requested base color is invalid.", LogLevel.Warn);
            return;
        }

        if (!this.SaveExternalStateNow())
        {
            this.registry.RestoreGenomeColorState(animalId, oldState.Value);
            this.RefreshRanchSlimeAppearance(animalId);
            this.Monitor.Log("Could not save external state; the original species and color were restored.", LogLevel.Error);
            return;
        }

        this.RefreshRanchSlimeAppearance(animalId);
        this.Monitor.Log($"Changed Ranch Slime {animalId} base color and species to {this.registry.GetGenomeColorState(animalId)?.PrimarySpecies} in the mod's external registry.", LogLevel.Info);
    }

    private void OnSetSlimeMutationCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before changing a test genome.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 3 || args[0] != saveName || !long.TryParse(args[1], out long animalId)
            || (args[2] != "ShadowClone" && args[2] != "none"))
        {
            this.Monitor.Log($"Usage: slime_ranching_set_mutation {saveName} <animal ID> <ShadowClone|none>", LogLevel.Warn);
            return;
        }

        string? oldTrait = this.registry!.GetMutationTrait(animalId);
        if (!this.registry.SetMutationTrait(animalId, args[2] == "none" ? null : "Shadow Clone"))
        {
            this.Monitor.Log("That ID is not an unreserved managed Ranch Slime, or the requested test mutation is invalid.", LogLevel.Warn);
            return;
        }

        if (!this.SaveExternalStateNow())
        {
            this.registry.SetMutationTrait(animalId, oldTrait);
            this.Monitor.Log("Could not save external state; the original mutation trait was restored.", LogLevel.Error);
            return;
        }

        this.Monitor.Log($"Changed Ranch Slime {animalId} mutation trait to {this.registry.GetMutationTrait(animalId) ?? "none"} in the mod's external registry.", LogLevel.Info);
    }

    private void OnBindCompanionCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before binding a companion.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 2 || args[0] != saveName || !long.TryParse(args[1], out long animalId))
        {
            this.Monitor.Log($"Usage: slime_ranching_bind_companion {saveName} <animal ID>", LogLevel.Warn);
            return;
        }

        bool isInDaycare = Game1.getFarm().buildings.Any(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value)
            && building.GetIndoors() is StardewValley.AnimalHouse house
            && house.animals.Values.Any(animal => animal.myID.Value == animalId && animal.type.Value == FarmAnimalContent.RanchSlimeTypeId));
        if (!isInDaycare || !this.registry!.BindCompanion(animalId))
        {
            this.Monitor.Log("Binding failed. Choose one mature managed Ranch Slime in the Daycare that is not assigned to a ranching job; only one companion can be bound.", LogLevel.Warn);
            return;
        }

        if (!this.SaveExternalStateNow())
        {
            this.registry.UnbindCompanion(animalId);
            this.Monitor.Log("Could not save the binding to the mod folder; the slime was unbound again.", LogLevel.Error);
            return;
        }

        this.Monitor.Log($"Bound Ranch Slime {animalId} as the companion identity test. It remains in the Daycare; follower movement and the Core item are not implemented yet.", LogLevel.Info);
    }

    private void OnDismissCompanionCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before dismissing a companion.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"Usage: slime_ranching_dismiss_companion {saveName}", LogLevel.Warn);
            return;
        }

        if (this.registry!.ActiveCompanionFarmAnimalId is not long animalId || !this.registry.UnbindCompanion(animalId))
        {
            this.Monitor.Log("There is no active companion binding to dismiss.", LogLevel.Warn);
            return;
        }

        if (!this.SaveExternalStateNow())
        {
            this.registry.BindCompanion(animalId);
            this.Monitor.Log("Could not save the dismissal to the mod folder; the companion binding was restored.", LogLevel.Error);
            return;
        }

        this.Monitor.Log($"Dismissed companion Ranch Slime {animalId}. The same animal and genome remain in the Daycare.", LogLevel.Info);
    }

    private void RefreshRanchSlimeAppearance(long animalId)
    {
        FarmAnimal? animal = GetFarmAnimals().FirstOrDefault(candidate => candidate.myID.Value == animalId);
        if (animal is not null && this.registry is not null)
            FarmAnimalContent.ApplyAppearance(animal, this.registry.GetGenomeColor(animalId),
                this.registry.GetGenomeSize(animalId), this.registry.GetGenomeIsShiny(animalId));
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

    private void OnGiveMutagenicPoolCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting a Mutagenic Pool.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give a Mutagenic Pool to the currently loaded save, use: slime_ranching_give_mutagenic_pool {saveName}", LogLevel.Warn);
            return;
        }

        Item pool = ItemRegistry.Create(MutagenicPoolContent.QualifiedItemId);
        if (!Game1.player.couldInventoryAcceptThisItem(pool) || !Game1.player.addItemToInventoryBool(pool))
        {
            this.Monitor.Log("Make room in your inventory for the Mutagenic Pool, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Mutagenic Pool. Place it inside the Slime Daycare and use it with vanilla Slime in your inventory.", LogLevel.Info);
    }

    private void OnGiveGelRefineryCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting a Gel Refinery.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give a Gel Refinery to the currently loaded save, use: slime_ranching_give_gel_refinery {saveName}", LogLevel.Warn);
            return;
        }

        Item refinery = ItemRegistry.Create(GelRefineryContent.QualifiedItemId);
        if (!Game1.player.couldInventoryAcceptThisItem(refinery) || !Game1.player.addItemToInventoryBool(refinery))
        {
            this.Monitor.Log("Make room in your inventory for the Gel Refinery, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Gel Refinery. Place it inside the Slime Daycare; its provisional recipe needs Broken Glasses and vanilla Slime.", LogLevel.Info);
    }

    private void OnGiveCompostTroughCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting a Compost Trough.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give a Compost Trough to the currently loaded save, use: slime_ranching_give_compost_trough {saveName}", LogLevel.Warn);
            return;
        }

        Item trough = ItemRegistry.Create(FurnitureContent.CompostTroughQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(trough) || !Game1.player.addItemToInventoryBool(trough))
        {
            this.Monitor.Log("Make room in your inventory for the Compost Trough, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Compost Trough. Place it inside the Slime Daycare; feed it eligible trash, algae, or monster drops.", LogLevel.Info);
    }

    private void OnGiveTunnelCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting a Slime Tunnel.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give a Slime Tunnel to the currently loaded save, use: slime_ranching_give_tunnel {saveName}", LogLevel.Warn);
            return;
        }

        Item tunnel = ItemRegistry.Create(FurnitureContent.SlimeTunnelQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(tunnel) || !Game1.player.addItemToInventoryBool(tunnel))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Tunnel, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Tunnel. Place it in the Slime Daycare for nearby idle Ranch Slimes to crawl through.", LogLevel.Info);
    }

    private void OnGiveHeatedStoneCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting a Heated Slime Stone.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give a Heated Slime Stone to the currently loaded save, use: slime_ranching_give_heated_stone {saveName}", LogLevel.Warn);
            return;
        }

        Item stone = ItemRegistry.Create(FurnitureContent.HeatedSlimeStoneQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(stone) || !Game1.player.addItemToInventoryBool(stone))
        {
            this.Monitor.Log("Make room in your inventory for the Heated Slime Stone, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Heated Slime Stone. Place it in the Slime Daycare for nearby idle Ranch Slimes to rest beside it.", LogLevel.Info);
    }

    private void OnGivePondCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting a Slime Pond.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give a Slime Pond to the currently loaded save, use: slime_ranching_give_pond {saveName}", LogLevel.Warn);
            return;
        }

        Item pond = ItemRegistry.Create(FurnitureContent.SlimePondQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(pond) || !Game1.player.addItemToInventoryBool(pond))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Pond, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Pond. Place it in the Slime Daycare for nearby idle Ranch Slimes to bob in the water.", LogLevel.Info);
    }

    private void OnGiveSwingCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting a Slime Swing.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give a Slime Swing to the currently loaded save, use: slime_ranching_give_swing {saveName}", LogLevel.Warn);
            return;
        }

        Item swing = ItemRegistry.Create(FurnitureContent.SlimeSwingQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(swing) || !Game1.player.addItemToInventoryBool(swing))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Swing, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Swing. Place it in the Slime Daycare for nearby idle Ranch Slimes to sway on.", LogLevel.Info);
    }

    private void OnGiveFairyLightsCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before requesting Slime Fairy Lights.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"To give Slime Fairy Lights to the currently loaded save, use: slime_ranching_give_fairy_lights {saveName}", LogLevel.Warn);
            return;
        }

        Item lights = ItemRegistry.Create(FurnitureContent.SlimeFairyLightsQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(lights) || !Game1.player.addItemToInventoryBool(lights))
        {
            this.Monitor.Log("Make room in your inventory, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one set of Slime Fairy Lights. Place them in the Slime Daycare; their colored glow is on from 6:00 PM until 2:00 AM.", LogLevel.Info);
    }

    private bool StartRefineryBatch(string machineKey)
    {
        GelRefineryRules rules = GelRefineryRules.FromConfig(this.config);
        if (this.registry!.GetRefineryJob(machineKey) is not null
            || CountInventoryItem("(O)170") < 1 || CountInventoryItem("(O)766") < rules.FuelCost)
            return false;

        List<(int Slot, Item Item, int OldStack)> removed = ConsumeInventoryItems("(O)170", 1)
            .Concat(ConsumeInventoryItems("(O)766", rules.FuelCost)).ToList();
        if (!this.registry.StartRefineryJob(machineKey, Game1.Date.TotalDays, rules.DurationDays, rules.FuelCost))
        {
            RestoreInventoryItems(removed);
            return false;
        }

        return true;
    }

    private bool CollectRefineryBatch(string machineKey)
    {
        RefineryJobRecord? job = this.registry!.GetRefineryJob(machineKey);
        if (job is null || Game1.Date.TotalDays < job.ReadyOnDay)
            return false;

        Item output = ItemRegistry.Create(job.OutputItemId);
        if (!Game1.player.couldInventoryAcceptThisItem(output) || !Game1.player.addItemToInventoryBool(output))
            return false;

        this.registry.RemoveRefineryJob(machineKey);
        return true;
    }

    private (bool Success, bool Favorite, int NewAge) FeedJuvenileAtCompostTrough(long animalId, int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= Game1.player.Items.Count
            || Game1.player.Items[inventoryIndex] is not Item item
            || !CompostTroughRules.IsEligibleFood(item.QualifiedItemId))
            return (false, false, 0);

        bool success = this.registry!.FeedJuvenileAtCompostTrough(animalId, item.QualifiedItemId, Game1.Date.TotalDays,
            this.config.CompostTroughGrowthDays, this.config.CompostTroughLikedFoodBonusDays,
            out int newAgeDays, out bool favorite);
        if (!success)
            return (false, false, 0);

        item.Stack--;
        if (item.Stack <= 0)
            Game1.player.Items[inventoryIndex] = null;

        FarmAnimal? animal = GetFarmAnimals().FirstOrDefault(candidate => candidate.myID.Value == animalId);
        if (animal is not null)
            animal.age.Value = newAgeDays;
        return (true, favorite, newAgeDays);
    }

    private static int CountInventoryItem(string qualifiedItemId) => Game1.player.Items
        .Where(item => item is not null && item.QualifiedItemId == qualifiedItemId)
        .Sum(item => item!.Stack);

    private static List<(int Slot, Item Item, int OldStack)> ConsumeInventoryItems(string qualifiedItemId, int amount)
    {
        var removed = new List<(int Slot, Item Item, int OldStack)>();
        for (int i = 0; i < Game1.player.Items.Count && amount > 0; i++)
        {
            Item? item = Game1.player.Items[i];
            if (item is null || item.QualifiedItemId != qualifiedItemId)
                continue;

            int oldStack = item.Stack;
            int consumed = Math.Min(amount, oldStack);
            removed.Add((i, item, oldStack));
            item.Stack -= consumed;
            amount -= consumed;
            if (item.Stack <= 0)
                Game1.player.Items[i] = null;
        }
        return removed;
    }

    private static void RestoreInventoryItems(IEnumerable<(int Slot, Item Item, int OldStack)> removed)
    {
        foreach ((int slot, Item item, int oldStack) in removed)
        {
            item.Stack = oldStack;
            Game1.player.Items[slot] = item;
        }
    }

    private void OnBackupExternalDataCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available external slime state before creating a backup.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log($"Usage: slime_ranching_backup_external_data {saveName}", LogLevel.Warn);
            return;
        }

        try
        {
            string? path = this.stateStore!.CreateExternalDataBackup();
            if (path is null)
            {
                this.Monitor.Log("There is no external mod data folder to back up for this save yet.", LogLevel.Warn);
                return;
            }

            this.Monitor.Log($"Backed up this save's external mod data (registry and first-seen snapshot) to: {path}", LogLevel.Info);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            this.Monitor.Log($"Could not back up external mod data: {exception.Message}", LogLevel.Error);
        }
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
        if (args.Length > 1 || (args.Length == 1 && !long.TryParse(args[0], out _)))
        {
            this.Monitor.Log("Usage: slime_ranching_inspect [Ranch Slime animal ID]", LogLevel.Warn);
            return;
        }

        string schemaVersion = this.stateStore!.LoadedSchemaVersion?.ToString() ?? "not written yet";
        this.Monitor.Log($"External registry schema on disk: {schemaVersion}; target for this build: {SlimeSaveData.CurrentSchemaVersion}.", LogLevel.Info);

        if (args.Length == 1)
        {
            long animalId = long.Parse(args[0]);
            if (!loadedRegistry.TryGetSlimeRecord(animalId, out SlimeRecord record))
            {
                this.Monitor.Log($"FarmAnimal ID {animalId} has no active Ranch Slime genome record in this save.", LogLevel.Warn);
                return;
            }

            SlimeGenome genome = record.Genome;
            string species = genome.SecondarySpecies is null ? genome.PrimarySpecies : $"{genome.PrimarySpecies} / {genome.SecondarySpecies}";
            string mutation = genome.MutationTrait ?? "none";
            string likedFood = FoodLikeCatalog.GetDisplayName(genome.LikedFoodItemId);
            int age = record.AgeClockInitialized ? record.AgeDays : GetFarmAnimals().FirstOrDefault(animal => animal.myID.Value == animalId)?.age.Value ?? 0;
            this.Monitor.Log($"Ranch Slime {animalId}: genome {genome.Id:N}, {species}, color {genome.VisualColor}, size {genome.Size}, shiny {genome.IsShiny}, rarity {genome.RarityTier}, mutation {mutation}.", LogLevel.Info);
            this.Monitor.Log($"Generation {genome.Generation}; abilities [{string.Join(", ", genome.AbilityIds)}]; likes {likedFood}; age {age}/{FarmAnimalContent.RanchSlimeDaysToMature} days; pity {loadedRegistry.GetMutagenicPoolFailures(animalId)}; companion {record.IsCompanion}; reserved {loadedRegistry.IsReserved(animalId)}.", LogLevel.Info);
            return;
        }

        if (Game1.currentLocation is SlimeHutch hutch)
        {
            int slimeCount = hutch.characters.OfType<GreenSlime>().Count();
            this.Monitor.Log($"This vanilla Slime Hutch contains {slimeCount} GreenSlime monster(s), all left unchanged. Ranch Slimes live in the separate Slime Daycare; the external registry contains {loadedRegistry.Count} Ranch Slime genome record(s).", LogLevel.Info);
            return;
        }

        if (Game1.currentLocation is StardewValley.AnimalHouse animalHouse)
        {
            FarmAnimal[] residents = animalHouse.animals.Values
                .Where(animal => animal.type.Value == FarmAnimalContent.RanchSlimeTypeId)
                .ToArray();
            string machinePrefix = animalHouse.NameOrUniqueName + ":";
            BreedingPairRecord[] roomBreedingPairs = loadedRegistry.BreedingPairs
                .Where(pair => pair.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal))
                .ToArray();
            int activeCycles = roomBreedingPairs.Count(pair => pair.CycleReadyOnDay > 0 && pair.OutputEggItemId is null);
            int readyEggs = roomBreedingPairs.Count(pair => pair.OutputEggItemId is not null);
            int activeIncubations = loadedRegistry.Incubations.Count(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal));
            int activeMutations = loadedRegistry.MutagenicAttempts.Count(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal));
            int activeRefineryBatches = loadedRegistry.RefineryJobs.Count(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal));
            int mutatedResidents = residents.Count(animal => loadedRegistry.GetMutationTrait(animal.myID.Value) is not null);
            this.Monitor.Log($"This animal house contains {residents.Length} Ranch Slime(s), including {mutatedResidents} with mutation traits. The external genome registry contains {loadedRegistry.Count} Ranch Slime record(s).", LogLevel.Info);
            this.Monitor.Log($"Jobs in this room: {activeCycles} breeder cycle(s), {readyEggs} ready egg(s), {activeIncubations} incubation(s), {activeMutations} Mutagenic Pool attempt(s), {activeRefineryBatches} Gel Refinery batch(es).", LogLevel.Info);

            foreach (BreedingPairRecord pair in roomBreedingPairs)
            {
                string status = pair.OutputEggItemId is not null
                    ? $"egg ready ({pair.OutputEggItemId})"
                    : pair.CycleReadyOnDay > Game1.Date.TotalDays
                        ? $"cycle ready day {pair.CycleReadyOnDay}"
                        : pair.CycleReadyOnDay > 0 ? "cycle ready to collect" : "parents reserved; no active cycle";
                this.Monitor.Log($"Breeder [{pair.MachineKey}]: parents {pair.ParentAId}/{pair.ParentBId}; {status}.", LogLevel.Info);
            }

            foreach (IncubationRecord job in loadedRegistry.Incubations.Where(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal)))
                this.Monitor.Log($"Incubator [{job.MachineKey}]: egg {job.EggItemId}; ready day {job.ReadyOnDay}; reserved hatch ID {job.HatchAnimalId?.ToString() ?? "none"}.", LogLevel.Info);

            foreach (MutagenicAttemptRecord job in loadedRegistry.MutagenicAttempts.Where(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal)))
                this.Monitor.Log($"Mutagenic Pool [{job.MachineKey}]: slime {job.SlimeId}; chance {job.ChanceAtStart:P0}; ready day {job.ReadyOnDay}.", LogLevel.Info);

            foreach (RefineryJobRecord job in loadedRegistry.RefineryJobs.Where(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal)))
                this.Monitor.Log($"Gel Refinery [{job.MachineKey}]: {job.InputCount} Broken Glasses + {job.SlimeFuelCount} Slime -> {job.OutputCount} Refined Quartz; ready day {job.ReadyOnDay}; notice sent {job.ReadyNotificationSent}.", LogLevel.Info);
            return;
        }

        this.Monitor.Log("Enter the Slime Daycare or a vanilla Slime Hutch to inspect ranching status.", LogLevel.Info);
    }

}

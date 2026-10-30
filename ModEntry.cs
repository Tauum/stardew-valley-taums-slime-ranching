using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Monsters;
using StardewValley.Menus;
using StardewValley.Objects;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using taums_slime_ranching.Models;
using taums_slime_ranching.Services;
using taums_slime_ranching.UI;

namespace taums_slime_ranching;

public sealed class ModEntry : Mod
{
    private const string IncubationCancellationReceiptPrefix = "Taum.SlimeRanching/IncubationCancellation/";
    private const string MutagenicContributionReceiptPrefix = "Taum.SlimeRanching/MutagenicContribution/";
    private const string RareLabContributionReceiptPrefix = "Taum.SlimeRanching/RareLabContribution/";
    private const string RareLabCatalystReceiptPrefix = "Taum.SlimeRanching/RareLabCatalyst/";
    private const string SlimePrinterTransactionReceiptPrefix = "Taum.SlimeRanching/SlimePrinterTransaction/";

    private SlimeRegistry? registry;
    private ModStateStore? stateStore;
    private ModConfig config = new();
    private bool stateReady;
    private readonly EnrichmentInteractionService trampolineInteractions = new();
    private readonly ShadowCloneVisualService shadowCloneVisuals = new();
    private readonly SlimeMutationVisualService mutationVisuals = new();
    private readonly List<(long SlimeId, bool Success, double Chance)> pendingMutagenicNotifications = new();
    private readonly HashSet<string> mutagenicContributionReceiptsToClear = new(StringComparer.Ordinal);
    private readonly HashSet<string> rareLabContributionReceiptsToClear = new(StringComparer.Ordinal);
    private readonly HashSet<string> rareLabCatalystReceiptsToClear = new(StringComparer.Ordinal);
    private readonly HashSet<string> slimePrinterTransactionReceiptsToClear = new(StringComparer.Ordinal);
    private HashSet<string>? marnieFurnitureRecipesKnownOnShopOpen;

    public override void Entry(IModHelper helper)
    {
        new Harmony(this.ModManifest.UniqueID).PatchAll(typeof(ModEntry).Assembly);
        DaycareDecorating.Initialize(helper, this.Monitor);
        this.config = helper.ReadConfig<ModConfig>();
        if (SlimePrinterRules.NormalizeFuelCost(this.config))
        {
            helper.WriteConfig(this.config);
            this.Monitor.Log($"Normalized the configured Slime Printer fuel cost to {this.config.SlimePrinterFuelCost} Slime (supported range {SlimePrinterRules.MinimumSlimeFuelCost}-{SlimePrinterRules.MaximumSlimeFuelCost}; the minimum prevents direct sale-value arbitrage).", LogLevel.Info);
        }
        this.registry = new SlimeRegistry(this.Monitor);
        DomesticatedSlimeShadowScalePatch.SetSizeLookup(id => this.registry?.GetGenomeSize(id));
        BreederMovementPatch.SetReservationLookup(id => this.registry?.IsBreedingParentReserved(id) == true);
        BreederParentVisibilityPatch.SetActiveCycleLookup(id => this.registry?.IsInActiveBreedingCycle(id) == true);
        BreederParentVisibilityPatch.SetUnclaimedHatchLookup(id => this.registry?.IsUnclaimedIncubationHatch(id) == true);
        this.stateStore = new ModStateStore(helper.Data, helper.DirectoryPath);
        SlimeDaycareQuestContent.SetDeliveryHandler(this.DeliverMaruRepairMaterials);
        SlimeDaycareQuestContent.SetPromptHandler(this.RecordRobinPoolRepairPrompt);

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.GameLoop.Saving += this.OnSaving;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.World.ObjectListChanged += this.OnObjectListChanged;
        helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.Display.MenuChanged += this.OnMenuChanged;
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_inspect", "Inspect vanilla slime or Domesticated Slime counts in the current Slime Hutch or Slime Daycare.", this.OnInspectCommand, includeSaveFolderName: false);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_open_management_desk", "Open a read-only tester preview of the Daycare Management Desk. Usage: slime_ranching_open_management_desk.", this.OnOpenManagementDeskCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_quest_status", "Report Slime Daycare Quest milestones for the loaded save.", this.OnSlimeDaycareQuestStatusCommand, includeSaveFolderName: false);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_test_maru_repair", "On a disposable save with a completed Daycare, seed the prerequisites for testing Robin's Maru repair handoff. Usage: slime_ranching_test_maru_repair.", this.OnTestMaruRepairCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_backup_external_data", "Back up the current save's external mod data. Usage: slime_ranching_backup_external_data.", this.OnBackupExternalDataCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_spawn_domesticated_slime", $"Spawn one Domesticated Slime at a chosen age in the Slime Daycare, optionally with randomized genome traits. Usage: slime_ranching_spawn_domesticated_slime <age days 0-{FarmAnimalContent.DomesticatedSlimeDaysToMature}> [random].", this.OnSpawnDomesticatedSlimeCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_set_size", "Set a managed Domesticated Slime's test genome size. Usage: slime_ranching_set_size <animal ID> <Small|Normal|Large|Giant>.", this.OnSetSlimeSizeCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_set_rare_lab", "Set Rare Lab test chance from inside its Daycare when no egg or hatch is pending. Usage: slime_ranching_set_rare_lab <incubator number in current Daycare> <pity 0-100> <contribution 0-100>.", this.OnSetRareLabCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_set_pool_test_state", "Set a mature Domesticated Slime's Mutagenic Pool misses and contribution for testing. Usage: slime_ranching_set_pool_test_state <animal ID> <failures 0-90> <contribution 0-90; total <= 90>.", this.OnSetMutagenicPoolTestStateCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_set_color", "Set a managed Domesticated Slime's test base color. Usage: slime_ranching_set_color <animal ID> <Red|Orange|Yellow|Green|Blue|Purple|Pink>.", this.OnSetSlimeColorCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_set_shiny", "Set a managed Domesticated Slime's test shiny flag. Usage: slime_ranching_set_shiny <animal ID> <true|false>.", this.OnSetSlimeShinyCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_set_mutation", "Set a managed Domesticated Slime's test mutation. Usage: slime_ranching_set_mutation <animal ID> <Starry|Spotted|Glowing|Crystalline|Mossy|Bubbly|Mushroom|Blooming|Firefly|Crowned|Cowboy|Smelly|none>.", this.OnSetSlimeMutationCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_bind_companion", "Bind one mature Daycare Domesticated Slime as the external companion test. Usage: slime_ranching_bind_companion <animal ID>.", this.OnBindCompanionCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_dismiss_companion", "Dismiss the currently bound Domesticated Slime companion. Usage: slime_ranching_dismiss_companion.", this.OnDismissCompanionCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_guidebook", "Give the Slime Ranching Field Guide. Usage: slime_ranching_give_guidebook.", this.OnGiveGuidebookCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_breeder", "Give one Slime Breeder for testing. Usage: slime_ranching_give_breeder.", this.OnGiveBreederCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_incubator", "Give one Slime Incubator for testing. Usage: slime_ranching_give_incubator.", this.OnGiveIncubatorCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_cryolab", "Give one tester Egg CryoLab chest. Usage: slime_ranching_give_cryolab.", this.OnGiveCryoLabCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_duplicate_cryolab_egg", "Deliberately duplicate a stored egg ID for CryoLab guard testing on a disposable save. Usage: slime_ranching_duplicate_cryolab_egg.", this.OnDuplicateCryoLabEggCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_duplicate_inventory_egg", "Deliberately duplicate one unused inventory egg ID for hatch-once testing on a disposable save. Usage: slime_ranching_duplicate_inventory_egg.", this.OnDuplicateInventoryEggCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_mutagenic_pool", "Give one Mutagenic Pool for testing. Usage: slime_ranching_give_mutagenic_pool.", this.OnGiveMutagenicPoolCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_slime_printer", "Give one Slime Printer for testing. Usage: slime_ranching_give_slime_printer.", this.OnGiveSlimePrinterCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_research_lab", "Give one Research Lab fixture for testing. Place it directly beside an incubator. Usage: slime_ranching_give_research_lab.", this.OnGiveResearchLabCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_tunnel", "Give one Slime Tunnel for testing. Usage: slime_ranching_give_tunnel.", this.OnGiveTunnelCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_heated_stone", "Give one Heated Slime Stone for testing. Usage: slime_ranching_give_heated_stone.", this.OnGiveHeatedStoneCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_pond", "Give one Slime Pond for testing. Usage: slime_ranching_give_pond.", this.OnGivePondCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_swing", "Give one Slime Swing for testing. Usage: slime_ranching_give_swing.", this.OnGiveSwingCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_seesaw", "Give one Slime Seesaw for paired activity testing. Usage: slime_ranching_give_seesaw.", this.OnGiveSeesawCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_ball_pit", "Give one Slime Ball Pit for activity testing. Usage: slime_ranching_give_ball_pit.", this.OnGiveBallPitCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_lamp", "Give one Slime Lamp for activity testing. Usage: slime_ranching_give_lamp.", this.OnGiveLampCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_fairy_lights", "Give one set of Slime Fairy Lights for testing. Usage: slime_ranching_give_fairy_lights.", this.OnGiveFairyLightsCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_washing_machine", "Give one Slime Washing Machine for recipe output and sprite testing. Usage: slime_ranching_give_washing_machine.", this.OnGiveWashingMachineCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_set_furniture_slot_test", "Save a test furniture-slot assignment without placing or consuming an item. Usage: slime_ranching_set_furniture_slot_test <slot-id> <furniture-key> <tile-x> <tile-y>.", this.OnSetFurnitureSlotTestCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_clear_furniture_slot_test", "Clear a test furniture-slot assignment without removing an in-room object. Usage: slime_ranching_clear_furniture_slot_test <slot-id>.", this.OnClearFurnitureSlotTestCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_test_eggs", "Create test eggs, use 'random' for one randomized egg, 'malformed' for two deliberately invalid eggs, or pass key=value metadata for two controlled eggs. Usage: slime_ranching_give_test_eggs [random|malformed|species=Name ...].", this.OnGiveTestEggsCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_color_eggs", "Create seven color test eggs. Usage: slime_ranching_give_color_eggs.", this.OnGiveColorEggsCommand);
        this.AddSaveAwareConsoleCommand(helper, "slime_ranching_give_crystals", "Give one of each Chromatic Crystal for breeder testing. Usage: slime_ranching_give_crystals.", this.OnGiveCrystalsCommand);
    }

    private void AddSaveAwareConsoleCommand(IModHelper helper, string name, string description,
        Action<string, string[]> handler, bool includeSaveFolderName = true)
    {
        helper.ConsoleCommands.Add(name, description, (command, args) =>
        {
            if (!Context.IsWorldReady || !this.stateReady || this.registry is null)
            {
                this.Monitor.Log($"Can't run '{command}' without a loaded save. Load a save first.", LogLevel.Warn);
                return;
            }

            if (Context.IsMultiplayer)
            {
                this.Monitor.Log($"Can't run '{command}' in multiplayer. Slime Daycare commands currently support single-player saves only.", LogLevel.Warn);
                return;
            }

            if (!Context.IsMainPlayer)
            {
                this.Monitor.Log($"Can't run '{command}' because only the main player can run Slime Daycare commands.", LogLevel.Warn);
                return;
            }

            string? saveFolderName = Constants.SaveFolderName;
            if (string.IsNullOrWhiteSpace(saveFolderName))
            {
                this.Monitor.Log($"Can't run '{command}' because SMAPI hasn't identified the loaded save.", LogLevel.Warn);
                return;
            }

            string[] forwardedArgs = args;
            if (includeSaveFolderName
                && (args.Length == 0 || !string.Equals(args[0], saveFolderName, StringComparison.Ordinal)))
            {
                forwardedArgs = new string[args.Length + 1];
                forwardedArgs[0] = saveFolderName;
                Array.Copy(args, 0, forwardedArgs, 1, args.Length);
            }

            handler(command, forwardedArgs);
        });
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.stateReady = false;
        if (!Context.IsMainPlayer)
            return;

        BuildingContent.RefreshLoadedDaycareInteriors(this.Monitor);

        try
        {
            this.registry!.Load(this.stateStore!.Load(), this.config.LogRegistrySummary);
            this.RestoreFurnitureSlotProtection();
            bool needsPersistenceMigration = this.stateStore.ImportedLegacyData || this.stateStore.MigratedSaveData;
            int recoveredHatches = this.registry.RecoverPendingHatches(GetFarmAnimals());
            if (recoveredHatches > 0)
                this.Monitor.Log($"Recovered {recoveredHatches} saved Domesticated Slime hatch(es) from save data.", LogLevel.Info);
            int registeredDomesticatedSlimes = this.registry.RegisterDomesticatedSlimes(GetFarmAnimals());
            if (registeredDomesticatedSlimes > 0)
                this.Monitor.Log($"Registered {registeredDomesticatedSlimes} Domesticated Slime farm animal(s) from entity metadata.", LogLevel.Info);
            (int detachedAnimals, int cancelledCycles) = this.registry.ReconcileDomesticatedSlimes(GetFarmAnimals());
            this.ApplyDomesticatedSlimeAppearances();
            bool maturityChanged = this.registry.SynchronizeMaturity(GetFarmAnimals(), Game1.Date.TotalDays);
            this.stateReady = true;
            bool rareLabProgressionChanged = this.SynchronizeRareLabProgression();
            bool eggCryoLabRecordsChanged = this.SynchronizeEggCryoLabRecords();
            bool repairTransactionRecovered = this.ReconcileMaruRepairDelivery();
            bool incubationCancellationRecovered = this.ReconcileIncubationCancellations();
            bool mutagenicContributionRecovered = this.ReconcileMutagenicContributionTransactions();
            bool rareLabContributionRecovered = this.ReconcileRareLabContributionTransactions();
            bool rareLabCatalystRecovered = this.ReconcileRareLabCatalystTransactions();
            bool printerTransactionRecovered = this.ReconcileSlimePrinterTransactions();
            bool constructionProgressChanged = this.TryRecordCompletedDaycareConstruction();
            bool repairQuestProgressChanged = this.TryStartMaruRepairQuest();
            this.LogRegistryReconciliation(detachedAnimals, cancelledCycles);
            if (this.stateStore.ImportedLegacyData)
                this.Monitor.Log($"Imported the previous mod-folder slime registry from schema {this.stateStore.LoadedSchemaVersion?.ToString() ?? "unknown"} into save schema {SlimeSaveData.CurrentSchemaVersion}. The original legacy file was left untouched.", LogLevel.Info);
            if (this.stateStore.MigratedSaveData)
                this.Monitor.Log("Migrated the saved Domesticated Slime data to the current schema.", LogLevel.Info);
            if (needsPersistenceMigration || registeredDomesticatedSlimes > 0 || recoveredHatches > 0 || detachedAnimals > 0 || cancelledCycles > 0 || maturityChanged || rareLabProgressionChanged || eggCryoLabRecordsChanged || repairTransactionRecovered || incubationCancellationRecovered || mutagenicContributionRecovered || rareLabContributionRecovered || rareLabCatalystRecovered || printerTransactionRecovered || constructionProgressChanged || repairQuestProgressChanged)
            {
                if (!this.SaveCurrentSaveDataNow())
                    this.Monitor.Log("Imported or reconciled Domesticated Slime state remains in memory but could not yet be written to the current save.", LogLevel.Error);
                else
                {
                    if (maturityChanged)
                        this.InvalidateEggObjectData();
                    this.ClearFinalizedMutagenicContributionReceipts();
                    this.ClearFinalizedRareLabContributionReceipts();
                    this.ClearFinalizedRareLabCatalystReceipts();
                    this.ClearFinalizedSlimePrinterTransactionReceipts();
                }
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or System.IO.InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException or NotSupportedException)
        {
            this.stateReady = false;
            this.Monitor.Log($"Domesticated Slime save data is unavailable for this save: {exception.Message}", LogLevel.Error);
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IGenericModConfigMenuApi? api = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
            return;

        api.Register(this.ModManifest, this.ResetConfig, this.SaveConfig);
        api.AddSectionTitle(this.ModManifest, () => "Furniture recipe prices");
        api.AddParagraph(this.ModManifest, () => "Changes refresh Marnie's furniture recipe stock after saving these settings.");
        this.AddShopPriceOption(api, "Slime Trampoline Recipe", "Price to learn the reusable Trampoline recipe.", () => this.config.TrampolineRecipePrice, value => this.config.TrampolineRecipePrice = value);
        this.AddShopPriceOption(api, "Slime Cushion Recipe", "Price to learn the reusable Cushion recipe.", () => this.config.SlimeCushionRecipePrice, value => this.config.SlimeCushionRecipePrice = value);
        this.AddShopPriceOption(api, "Slime Slide Recipe", "Price to learn the reusable Slide recipe.", () => this.config.SlimeSlideRecipePrice, value => this.config.SlimeSlideRecipePrice = value);
        this.AddShopPriceOption(api, "Slime Seesaw Recipe", "Price to learn the reusable Seesaw recipe.", () => this.config.SlimeSeesawRecipePrice, value => this.config.SlimeSeesawRecipePrice = value);
        this.AddShopPriceOption(api, "Heated Slime Stone Recipe", "Price to learn the reusable Heated Slime Stone recipe.", () => this.config.HeatedSlimeStoneRecipePrice, value => this.config.HeatedSlimeStoneRecipePrice = value);
        this.AddShopPriceOption(api, "Slime Pond Recipe", "Price to learn the reusable Pond recipe.", () => this.config.SlimePondRecipePrice, value => this.config.SlimePondRecipePrice = value);
        this.AddShopPriceOption(api, "Slime Tunnel Recipe", "Price to learn the reusable Tunnel recipe.", () => this.config.SlimeTunnelRecipePrice, value => this.config.SlimeTunnelRecipePrice = value);
        this.AddShopPriceOption(api, "Slime Swing Recipe", "Price to learn the reusable Swing recipe.", () => this.config.SlimeSwingRecipePrice, value => this.config.SlimeSwingRecipePrice = value);
        this.AddShopPriceOption(api, "Slime Lamp Recipe", "Price to learn the reusable Lamp recipe.", () => this.config.SlimeLampRecipePrice, value => this.config.SlimeLampRecipePrice = value);
        this.AddShopPriceOption(api, "Slime Fairy Lights Recipe", "Price to learn the reusable Fairy Lights recipe.", () => this.config.SlimeFairyLightsRecipePrice, value => this.config.SlimeFairyLightsRecipePrice = value);
        this.AddShopPriceOption(api, "Slime Ball Pit Recipe", "Price to learn the reusable Ball Pit recipe.", () => this.config.SlimeBallPitRecipePrice, value => this.config.SlimeBallPitRecipePrice = value);
        this.AddShopPriceOption(api, "Slime Washing Machine Recipe", "Price to learn the reusable Washing Machine recipe.", () => this.config.SlimeWashingMachineRecipePrice, value => this.config.SlimeWashingMachineRecipePrice = value);
        api.AddSectionTitle(this.ModManifest, () => "Slime Printer");
        api.AddParagraph(this.ModManifest, () => "The first recipe is provisional: Broken Glasses and vanilla Slime fuel become Refined Quartz.");
        api.AddNumberOption(this.ModManifest,
            () => this.config.SlimePrinterFuelCost,
            value => this.config.SlimePrinterFuelCost = value,
            () => "Slime fuel per batch",
            () => "At least 10 Slime per batch; this prevents the provisional recipe from producing direct sale-value profit.",
            SlimePrinterRules.MinimumSlimeFuelCost, SlimePrinterRules.MaximumSlimeFuelCost, 1);
        api.AddNumberOption(this.ModManifest,
            () => (float)this.config.SlimePrinterDurationDays,
            value => this.config.SlimePrinterDurationDays = (int)MathF.Round(value),
            () => "Processing days",
            () => "Whole in-game days per batch (1–28).", 1f, 28f, 1f,
            value => SlimePrinterRules.FormatInGameDays((int)MathF.Round(value)));
        api.AddSectionTitle(this.ModManifest, () => "Mutagenic Pool");
        api.AddParagraph(this.ModManifest, () => "The repaired Pool uses fixed 2-day attempts, a 10% base chance, and saved per-slime pity and Slime contributions. Add 10 Slime during an attempt for +1 percentage point; canceling preserves the current chance.");
        api.AddBoolOption(this.ModManifest,
            () => this.config.ShowShadowCloneVisualEchoes,
            value => this.config.ShowShadowCloneVisualEchoes = value,
            () => "Show legacy Shadow Clone echoes",
            () => "Compatibility display for Shadow Clone traits saved by older versions; new Pool outcomes use the approved mutation list.");
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

        this.trampolineInteractions.Clear();

        try
        {
            int registeredDomesticatedSlimes = this.registry!.RegisterDomesticatedSlimes(GetFarmAnimals());
            if (registeredDomesticatedSlimes > 0)
                this.Monitor.Log($"Registered {registeredDomesticatedSlimes} Domesticated Slime entity record(s).", LogLevel.Info);
            this.registry.SynchronizeMaturity(GetFarmAnimals(), Game1.Date.TotalDays);
            (int detachedAnimals, int cancelledCycles) = this.registry.ReconcileDomesticatedSlimes(GetFarmAnimals());
            this.LogRegistryReconciliation(detachedAnimals, cancelledCycles);
            this.registry.WriteEntityModData(GetFarmAnimals());
            this.SynchronizeRareLabProgression();
            this.stateStore!.Save(this.registry!.Capture());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            this.Monitor.Log($"Could not save Domesticated Slime data to the current save: {exception.Message}", LogLevel.Error);
        }
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!this.stateReady || !Context.IsMainPlayer)
            return;

        bool repairTransactionRecovered = this.ReconcileMaruRepairDelivery();
        bool mutagenicContributionRecovered = this.ReconcileMutagenicContributionTransactions();
        bool rareLabContributionRecovered = this.ReconcileRareLabContributionTransactions();
        bool rareLabCatalystRecovered = this.ReconcileRareLabCatalystTransactions();
        bool printerTransactionRecovered = this.ReconcileSlimePrinterTransactions();
        bool rareLabProgressionChanged = this.SynchronizeRareLabProgression();
        bool eggCryoLabRecordsChanged = this.SynchronizeEggCryoLabRecords();
        bool constructionProgressChanged = this.TryRecordCompletedDaycareConstruction();
        bool repairQuestProgressChanged = this.TryStartMaruRepairQuest();
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

        List<SlimePrinterJobRecord> printerReady = new();
        foreach (SlimePrinterJobRecord job in this.registry.SlimePrinterJobs)
        {
            if (Game1.Date.TotalDays >= job.ReadyOnDay && !job.ReadyNotificationSent)
            {
                job.ReadyNotificationSent = true;
                printerReady.Add(job);
            }
        }

        this.ProcessReadyIncubations();
        maturityChanged |= this.registry.SynchronizeMaturity(GetFarmAnimals(), Game1.Date.TotalDays);
        this.pendingMutagenicNotifications.AddRange(this.registry.ResolveReadyMutagenicAttempts(Game1.Date.TotalDays));

        if (readyToNotify.Count == 0 && printerReady.Count == 0 && this.pendingMutagenicNotifications.Count == 0)
        {
            bool needsSave = maturityChanged || rareLabProgressionChanged || eggCryoLabRecordsChanged || repairTransactionRecovered || mutagenicContributionRecovered || rareLabContributionRecovered || rareLabCatalystRecovered
                || printerTransactionRecovered || constructionProgressChanged || repairQuestProgressChanged;
            if (needsSave)
            {
                if (!this.SaveCurrentSaveDataNow())
                    this.Monitor.Log("Could not save Domesticated Slime or Slime Daycare quest progress; it will retry at the next save.", LogLevel.Error);
                else
                {
                    if (maturityChanged)
                        this.InvalidateEggObjectData();
                    if (mutagenicContributionRecovered)
                        this.ClearFinalizedMutagenicContributionReceipts();
                    this.ClearFinalizedRareLabContributionReceipts();
                    this.ClearFinalizedRareLabCatalystReceipts();
                    this.ClearFinalizedSlimePrinterTransactionReceipts();
                }
            }
            else if (maturityChanged)
                this.InvalidateEggObjectData();
            return;
        }

        if (!this.SaveCurrentSaveDataNow())
        {
            foreach (var ready in readyToNotify)
                ready.Pair.ReadyNotificationSent = false;
            foreach (SlimePrinterJobRecord job in printerReady)
                job.ReadyNotificationSent = false;
            this.Monitor.Log("A completed breeding or mutation result could not be saved; notifications will retry after the next save-data write.", LogLevel.Error);
            return;
        }
        if (maturityChanged)
            this.InvalidateEggObjectData();
        if (mutagenicContributionRecovered)
            this.ClearFinalizedMutagenicContributionReceipts();
        this.ClearFinalizedRareLabContributionReceipts();
        this.ClearFinalizedRareLabCatalystReceipts();
        this.ClearFinalizedSlimePrinterTransactionReceipts();

        foreach (var ready in readyToNotify)
        {
            taums_slime_ranching.Models.SlimeEggRecord egg = ready.Egg;
            Game1.addHUDMessage(new HUDMessage("A Domesticated Slime Egg is ready to collect from the Slime Breeder!", HUDMessage.newQuest_type));
            this.Monitor.Log($"A Domesticated Slime Egg (genome {egg.Genome.Id:N}) is ready to collect.", LogLevel.Info);
        }

        foreach (var result in this.pendingMutagenicNotifications)
        {
            string message = result.Success
                ? "A Domesticated Slime's mutation has awakened!"
                : "The Mutagenic Pool attempt failed safely. That slime's next chance has increased.";
            Game1.addHUDMessage(new HUDMessage(message, result.Success ? HUDMessage.newQuest_type : HUDMessage.error_type));
            this.Monitor.Log($"Mutagenic Pool attempt for Domesticated Slime {result.SlimeId}: {(result.Success ? "success" : "safe failure")} at {result.Chance:P0}.", LogLevel.Info);
        }
        this.pendingMutagenicNotifications.Clear();

        foreach (SlimePrinterJobRecord job in printerReady)
        {
            Game1.addHUDMessage(new HUDMessage("A Slime Printer batch is ready to collect!", HUDMessage.newQuest_type));
            this.Monitor.Log($"Slime Printer batch at {job.MachineKey} is ready: {job.OutputCount} Refined Quartz.", LogLevel.Info);
        }
    }

    private bool TryRecordCompletedDaycareConstruction()
    {
        if (this.registry is null
            || !this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.MarnieSecret)
            || this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.DaycareConstruction))
            return false;

        bool daycareIsComplete = Game1.getFarm().buildings.Any(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value)
            && building.daysOfConstructionLeft.Value <= 0);
        return daycareIsComplete
            && this.registry.TryCompleteSlimeDaycareQuestMilestone(SlimeDaycareQuestProgression.DaycareConstruction);
    }

    private bool TryStartMaruRepairQuest()
    {
        if (this.registry is null)
            return false;

        if (this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.MaruRepairCompleted))
        {
            if (!Game1.player.hasQuest(SlimeDaycareQuestContent.MaruRepairQuestId))
                return false;

            // Recover the narrow crash window after the durable repair flag was
            // written but before Stardew marked the journal quest complete.
            Game1.player.completeQuest(SlimeDaycareQuestContent.MaruRepairQuestId);
            return true;
        }

        if (!this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.DaycareConstruction))
            return false;

        if (this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.MaruRepairRequested))
        {
            // The mod ledger and Stardew's journal are saved separately. Re-add
            // the active mission if a save ended between those two writes.
            if (!Game1.player.hasQuest(SlimeDaycareQuestContent.MaruRepairQuestId))
                Game1.player.addQuest(SlimeDaycareQuestContent.MaruRepairQuestId);
            return false;
        }

        if (!this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.RobinPoolPrompt))
            return false;

        if (this.registry.SlimeDaycareQuest.MaruRepairQuestAvailableOnDay is not int availableOnDay)
        {
            this.registry.SlimeDaycareQuest.MaruRepairQuestAvailableOnDay = Game1.Date.TotalDays + 1;
            return true;
        }
        if (Game1.Date.TotalDays < availableOnDay)
            return false;

        if (!Game1.player.hasQuest(SlimeDaycareQuestContent.MaruRepairQuestId))
            Game1.player.addQuest(SlimeDaycareQuestContent.MaruRepairQuestId);
        if (!Game1.player.hasQuest(SlimeDaycareQuestContent.MaruRepairQuestId))
            return false;

        return this.registry.TryCompleteSlimeDaycareQuestMilestone(SlimeDaycareQuestProgression.MaruRepairRequested);
    }

    private bool RecordRobinPoolRepairPrompt()
    {
        if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer || this.registry is null
            || !this.registry.TryCompleteSlimeDaycareQuestMilestone(SlimeDaycareQuestProgression.RobinPoolPrompt))
            return false;

        this.registry.SlimeDaycareQuest.MaruRepairQuestAvailableOnDay = Game1.Date.TotalDays + 1;
        if (this.SaveCurrentSaveDataNow())
            return true;

        this.registry.RemoveSlimeDaycareQuestMilestone(SlimeDaycareQuestProgression.RobinPoolPrompt);
        this.registry.SlimeDaycareQuest.MaruRepairQuestAvailableOnDay = null;
        return false;
    }

    private bool DeliverMaruRepairMaterials()
    {
        if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer || this.registry is null
            || !this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.MaruRepairRequested)
            || this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.MaruRepairCompleted)
            || !SlimeDaycareQuestContent.TryConsumeRepairMaterials(Game1.player, out List<SlimeDaycareQuestContent.InventorySnapshot> snapshots))
            return false;

        Game1.player.modData[SlimeDaycareQuestContent.RepairDeliveryReceiptKey] = "1";
        if (!this.registry.TryCompleteSlimeDaycareQuestMilestone(SlimeDaycareQuestProgression.MaruRepairCompleted)
            || !this.SaveCurrentSaveDataNow())
        {
            Game1.player.modData.Remove(SlimeDaycareQuestContent.RepairDeliveryReceiptKey);
            this.registry.RemoveSlimeDaycareQuestMilestone(SlimeDaycareQuestProgression.MaruRepairCompleted);
            SlimeDaycareQuestContent.RestoreRepairMaterials(Game1.player, snapshots);
            return false;
        }

        Game1.player.completeQuest(SlimeDaycareQuestContent.MaruRepairQuestId);
        return true;
    }

    private bool ReconcileMaruRepairDelivery()
    {
        if (this.registry is null)
            return false;

        bool hasVanillaSaveReceipt = Game1.player.modData.ContainsKey(SlimeDaycareQuestContent.RepairDeliveryReceiptKey);
        bool repairRecorded = this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.MaruRepairCompleted);
        if (hasVanillaSaveReceipt && !repairRecorded)
        {
            // The farmer inventory and its receipt are saved with the vanilla
            // world; recover the mod milestone if that save completed first.
            return this.registry.RecoverSlimeDaycareQuestMilestoneFromDurableEvidence(SlimeDaycareQuestProgression.MaruRepairCompleted);
        }

        if (!hasVanillaSaveReceipt && repairRecorded)
        {
            // The external ledger may have reached disk before the vanilla
            // inventory save. Roll back repair so the player keeps the items.
            return this.registry.RemoveSlimeDaycareQuestMilestone(SlimeDaycareQuestProgression.MaruRepairCompleted);
        }

        return false;
    }

    private bool ReconcileIncubationCancellations()
    {
        if (this.registry is null)
            return false;

        bool changed = false;
        foreach (IncubationRecord pending in this.registry.PendingIncubationCancellations.ToArray())
        {
            string receiptKey = IncubationCancellationReceiptPrefix + pending.EggItemId;
            bool rewardWasSaved = Game1.player.modData.ContainsKey(receiptKey);
            bool reconciled = rewardWasSaved
                ? this.registry.CommitIncubationCancellation(pending.EggItemId)
                : this.registry.RestoreCancelledIncubation(pending);
            if (!reconciled)
                continue;

            Game1.player.modData.Remove(receiptKey);
            changed = true;
        }

        foreach (string receiptKey in Game1.player.modData.Keys
                     .Where(key => key.StartsWith(IncubationCancellationReceiptPrefix, StringComparison.Ordinal))
                     .ToArray())
        {
            string eggId = receiptKey[IncubationCancellationReceiptPrefix.Length..];
            if (this.registry.HasPendingIncubationCancellation(eggId))
                continue;

            Game1.player.modData.Remove(receiptKey);
            changed = true;
        }

        return changed;
    }

    private bool ReconcileMutagenicContributionTransactions()
    {
        if (this.registry is null)
            return false;

        bool changed = false;
        foreach (MutagenicContributionRecord pending in this.registry.PendingMutagenicContributions
                     .OrderByDescending(transaction => transaction.Sequence).ToArray())
        {
            string receiptKey = MutagenicContributionReceiptPrefix + pending.Id;
            if (Game1.player.modData.ContainsKey(receiptKey))
            {
                changed |= this.registry.FinalizeMutagenicContribution(pending.Id);
                this.mutagenicContributionReceiptsToClear.Add(receiptKey);
            }
            else
                changed |= this.registry.RollbackMutagenicContribution(pending.Id);
        }

        foreach (string receiptKey in Game1.player.modData.Keys
                     .Where(key => key.StartsWith(MutagenicContributionReceiptPrefix, StringComparison.Ordinal))
                     .ToArray())
        {
            this.mutagenicContributionReceiptsToClear.Add(receiptKey);
            changed = true;
        }

        return changed;
    }

    private bool ReconcileRareLabContributionTransactions()
    {
        if (this.registry is null)
            return false;

        bool changed = false;
        foreach (RareLabContributionRecord pending in this.registry.PendingRareLabContributions
                     .OrderByDescending(transaction => transaction.Sequence).ToArray())
        {
            string receiptKey = RareLabContributionReceiptPrefix + pending.Id;
            if (Game1.player.modData.ContainsKey(receiptKey))
            {
                changed |= this.registry.FinalizeRareLabContribution(pending.Id);
                this.rareLabContributionReceiptsToClear.Add(receiptKey);
            }
            else
                changed |= this.registry.RollbackRareLabContribution(pending.Id);
        }

        foreach (string receiptKey in Game1.player.modData.Keys
                     .Where(key => key.StartsWith(RareLabContributionReceiptPrefix, StringComparison.Ordinal))
                     .ToArray())
        {
            this.rareLabContributionReceiptsToClear.Add(receiptKey);
            changed = true;
        }

        return changed;
    }

    private bool ReconcileRareLabCatalystTransactions()
    {
        if (this.registry is null)
            return false;

        bool changed = false;
        foreach (RareLabCatalystTransactionRecord pending in this.registry.PendingRareLabCatalystTransactions
                     .OrderByDescending(transaction => transaction.Sequence).ToArray())
        {
            string receiptKey = RareLabCatalystReceiptPrefix + pending.Id;
            if (Game1.player.modData.ContainsKey(receiptKey))
            {
                changed |= this.registry.FinalizeRareLabCatalystTransaction(pending.Id);
                this.rareLabCatalystReceiptsToClear.Add(receiptKey);
            }
            else
                changed |= this.registry.RollbackRareLabCatalystTransaction(pending.Id);
        }

        foreach (string receiptKey in Game1.player.modData.Keys
                     .Where(key => key.StartsWith(RareLabCatalystReceiptPrefix, StringComparison.Ordinal))
                     .ToArray())
        {
            this.rareLabCatalystReceiptsToClear.Add(receiptKey);
            changed = true;
        }

        return changed;
    }

    private bool ReconcileSlimePrinterTransactions()
    {
        if (this.registry is null)
            return false;

        bool changed = false;
        foreach (SlimePrinterTransactionRecord pending in this.registry.PendingSlimePrinterTransactions
                     .OrderByDescending(transaction => transaction.Sequence).ToArray())
        {
            string receiptKey = SlimePrinterTransactionReceiptPrefix + pending.Id;
            bool nativeSaveCommitted = Game1.player.modData.ContainsKey(receiptKey);
            changed |= this.registry.ResolveSlimePrinterTransaction(pending.Id, nativeSaveCommitted);
            if (nativeSaveCommitted)
                this.slimePrinterTransactionReceiptsToClear.Add(receiptKey);
        }

        foreach (string receiptKey in Game1.player.modData.Keys
                     .Where(key => key.StartsWith(SlimePrinterTransactionReceiptPrefix, StringComparison.Ordinal))
                     .ToArray())
        {
            string transactionId = receiptKey[SlimePrinterTransactionReceiptPrefix.Length..];
            if (this.registry.PendingSlimePrinterTransactions.Any(transaction => transaction.Id == transactionId))
                continue;

            SlimePrinterTransactionRecord? receipt = null;
            try
            {
                receipt = System.Text.Json.JsonSerializer.Deserialize<SlimePrinterTransactionRecord>(Game1.player.modData[receiptKey]);
            }
            catch (System.Text.Json.JsonException)
            {
                // A malformed receipt can't safely be applied without its external pending record.
            }

            if (receipt?.Id == transactionId)
                changed |= this.registry.ReconcileOrphanSlimePrinterReceipt(receipt);
            else
                this.Monitor.Log("Skipped a malformed saved Slime Printer transaction receipt.", LogLevel.Warn);

            this.slimePrinterTransactionReceiptsToClear.Add(receiptKey);
            changed = true;
        }

        return changed;
    }

    private void ClearFinalizedSlimePrinterTransactionReceipts()
    {
        foreach (string receiptKey in this.slimePrinterTransactionReceiptsToClear)
            Game1.player.modData.Remove(receiptKey);
        this.slimePrinterTransactionReceiptsToClear.Clear();
    }

    private void ClearFinalizedMutagenicContributionReceipts()
    {
        foreach (string receiptKey in this.mutagenicContributionReceiptsToClear)
            Game1.player.modData.Remove(receiptKey);
        this.mutagenicContributionReceiptsToClear.Clear();
    }

    private void ClearFinalizedRareLabContributionReceipts()
    {
        foreach (string receiptKey in this.rareLabContributionReceiptsToClear)
            Game1.player.modData.Remove(receiptKey);
        this.rareLabContributionReceiptsToClear.Clear();
    }

    private void ClearFinalizedRareLabCatalystReceipts()
    {
        foreach (string receiptKey in this.rareLabCatalystReceiptsToClear)
            Game1.player.modData.Remove(receiptKey);
        this.rareLabCatalystReceiptsToClear.Clear();
    }

    private bool ContributeMutagenicPoolSlime(string machineKey)
    {
        if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer || this.registry is null
            || CountVanillaSlimeItems() < MutagenicPoolRules.SlimePerContributionPoint)
            return false;

        string? transactionId = this.registry.BeginMutagenicContribution(machineKey, MutagenicPoolRules.Instance);
        if (transactionId is null)
            return false;
        if (!this.SaveCurrentSaveDataNow())
        {
            this.registry.FinalizeMutagenicContribution(transactionId);
            return false;
        }

        (Item? Item, int Stack)[] inventorySnapshot = Game1.player.Items
            .Select(item => (Item: (Item?)item, Stack: item?.Stack ?? 0))
            .ToArray();
        ConsumeVanillaSlimeItems(MutagenicPoolRules.SlimePerContributionPoint);
        bool applied = this.registry.ApplyMutagenicContribution(transactionId, MutagenicPoolRules.Instance);
        string receiptKey = MutagenicContributionReceiptPrefix + transactionId;
        if (applied)
            Game1.player.modData[receiptKey] = "1";
        if (applied && this.SaveCurrentSaveDataNow())
            return true;

        this.registry.RollbackMutagenicContribution(transactionId);
        Game1.player.modData.Remove(receiptKey);
        RestoreInventorySnapshot(inventorySnapshot);
        if (!this.SaveCurrentSaveDataNow())
            this.Monitor.Log("Could not persist the rollback after a Mutagenic Pool contribution save failure; the prior chance and inventory were restored in memory and will be reconciled on reload.", LogLevel.Error);
        return false;
    }

    private bool ContributeRareLabSlime(string machineKey)
    {
        if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer || this.registry is null
            || CountVanillaSlimeItems() < RareLabRules.SlimeUnitsPerContributionPoint)
            return false;

        string? transactionId = this.registry.BeginRareLabContribution(machineKey, Game1.Date.TotalDays);
        if (transactionId is null)
            return false;
        if (!this.SaveCurrentSaveDataNow())
        {
            this.registry.FinalizeRareLabContribution(transactionId);
            return false;
        }

        (Item? Item, int Stack)[] inventorySnapshot = Game1.player.Items
            .Select(item => (Item: (Item?)item, Stack: item?.Stack ?? 0))
            .ToArray();
        ConsumeVanillaSlimeItems(RareLabRules.SlimeUnitsPerContributionPoint);
        bool applied = this.registry.ApplyRareLabContribution(transactionId);
        string receiptKey = RareLabContributionReceiptPrefix + transactionId;
        if (applied)
            Game1.player.modData[receiptKey] = "1";
        if (applied && this.SaveCurrentSaveDataNow())
            return true;

        this.registry.RollbackRareLabContribution(transactionId);
        Game1.player.modData.Remove(receiptKey);
        RestoreInventorySnapshot(inventorySnapshot);
        if (!this.SaveCurrentSaveDataNow())
            this.Monitor.Log("Could not persist the rollback after a Rare Lab contribution save failure; the prior chance and inventory were restored in memory and will be reconciled on reload.", LogLevel.Error);
        return false;
    }

    private bool AddRareLabCatalyst(string machineKey, string catalystInputKey)
    {
        RareLabCatalystInput? input = RareLabRules.GetCatalystInput(catalystInputKey);
        if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer || this.registry is null
            || input is null
            || CountInventoryItem(input.QualifiedItemId) < input.Amount)
            return false;

        string? transactionId = this.registry.BeginRareLabCatalystTransaction(
            machineKey, input.Species, Game1.Date.TotalDays);
        if (transactionId is null)
            return false;
        if (!this.SaveCurrentSaveDataNow())
        {
            this.registry.FinalizeRareLabCatalystTransaction(transactionId);
            return false;
        }

        (Item? Item, int Stack)[] inventorySnapshot = Game1.player.Items
            .Select(item => (Item: (Item?)item, Stack: item?.Stack ?? 0))
            .ToArray();
        ConsumeInventoryItems(input.QualifiedItemId, input.Amount);
        bool applied = this.registry.ApplyRareLabCatalystTransaction(transactionId);
        string receiptKey = RareLabCatalystReceiptPrefix + transactionId;
        if (applied)
            Game1.player.modData[receiptKey] = "1";
        if (applied && this.SaveCurrentSaveDataNow())
            return true;

        this.registry.RollbackRareLabCatalystTransaction(transactionId);
        Game1.player.modData.Remove(receiptKey);
        RestoreInventorySnapshot(inventorySnapshot);
        if (!this.SaveCurrentSaveDataNow())
            this.Monitor.Log("Could not persist the rollback after a Rare Lab catalyst save failure; the catalyst and inventory were restored in memory and will be reconciled on reload.", LogLevel.Error);
        return false;
    }

    private static int CountVanillaSlimeItems() => Game1.player.Items
        .Where(item => item is not null && item.QualifiedItemId == "(O)766")
        .Sum(item => item!.Stack);

    private static void ConsumeVanillaSlimeItems(int amount)
    {
        for (int index = 0; index < Game1.player.Items.Count && amount > 0; index++)
        {
            Item? item = Game1.player.Items[index];
            if (item is null || item.QualifiedItemId != "(O)766")
                continue;

            int consumed = Math.Min(amount, item.Stack);
            item.Stack -= consumed;
            amount -= consumed;
            if (item.Stack <= 0)
                Game1.player.Items[index] = null;
        }
    }

    private bool TryHandleMaruRepairInteraction(SButton button, Vector2 clickedTile)
    {
        if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer || this.registry is null
            || !this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.MaruRepairRequested)
            || this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.MaruRepairCompleted)
            || !Game1.player.hasQuest(SlimeDaycareQuestContent.MaruRepairQuestId)
            || Game1.dialogueUp || Game1.eventUp || Game1.currentLocation.currentEvent is not null
            || Game1.player.isRidingHorse())
            return false;

        NPC? target = Game1.currentLocation.isCharacterAtTile(clickedTile)
            ?? Game1.currentLocation.isCharacterAtTile(clickedTile + Vector2.UnitY);
        if (target is null || target.Name != "Maru" || target.IsInvisible || target.IsMonster
            || !Utility.withinRadiusOfPlayer(target.StandingPixel.X, target.StandingPixel.Y, 1, Game1.player))
            return false;

        this.Helper.Input.Suppress(button);
        if (!SlimeDaycareQuestContent.HasRepairMaterials(Game1.player, out string missing))
        {
            Game1.drawObjectDialogue($"Maru still needs {missing} to repair the Mutagenic Pool.");
            return true;
        }

        Game1.currentLocation.createQuestionDialogue(
            "Would you like to give Maru the materials to repair the Mutagenic Pool?",
            new[]
            {
                new StardewValley.Response("deliver", "Hand over materials"),
                new StardewValley.Response("later", "Not yet")
            },
            "maruPoolRepairDelivery");
        return true;
    }

    private bool TryHandleRobinPoolRepairPrompt(SButton button, Vector2 clickedTile)
    {
        if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer || this.registry is null
            || !this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.DaycareConstruction)
            || this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.RobinPoolPrompt)
            || Game1.dialogueUp || Game1.eventUp || Game1.currentLocation.currentEvent is not null
            || Game1.player.isRidingHorse())
            return false;

        NPC? target = Game1.currentLocation.isCharacterAtTile(clickedTile)
            ?? Game1.currentLocation.isCharacterAtTile(clickedTile + Vector2.UnitY);
        if (target is null || target.Name != "Robin" || target.IsInvisible || target.IsMonster
            || !Utility.withinRadiusOfPlayer(target.StandingPixel.X, target.StandingPixel.Y, 1, Game1.player))
            return false;

        this.Helper.Input.Suppress(button);
        Game1.currentLocation.createQuestionDialogue(
            "Robin noticed the broken Mutagenic Pool in the Slime Daycare. Maru may know how to repair it. Will you ask her?",
            new[]
            {
                new StardewValley.Response("askMaru", "I'll talk to Maru."),
                new StardewValley.Response("later", "Later")
            },
            "robinPoolRepairPrompt");
        return true;
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.trampolineInteractions.Clear();
        this.shadowCloneVisuals.Clear();
        this.mutationVisuals.Clear();
        this.pendingMutagenicNotifications.Clear();
        this.slimePrinterTransactionReceiptsToClear.Clear();
        this.rareLabContributionReceiptsToClear.Clear();
        this.rareLabCatalystReceiptsToClear.Clear();
        this.marnieFurnitureRecipesKnownOnShopOpen = null;
        this.stateReady = false;
        this.registry = new SlimeRegistry(this.Monitor);
        this.stateStore = new ModStateStore(this.Helper.Data, this.Helper.DirectoryPath);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (this.stateReady && Context.IsWorldReady && Context.IsMainPlayer)
            this.trampolineInteractions.Update(Game1.currentLocation, Game1.currentGameTime, this.registry);
        else
            this.trampolineInteractions.Clear();

        this.UpdateMarnieFurnitureRecipeNotifications();

        if (!this.stateReady || !Context.IsMainPlayer)
            return;

        if (e.IsMultipleOf(60))
        {
            DaycareFeedingService.EnsureConfiguredForFarm(Game1.getFarm(), this.Monitor);
            if (this.DiscardBreederPairsForMissingMachines() && !this.SaveCurrentSaveDataNow())
                this.Monitor.Log("Removed breeder cycles remain cleared in memory, but save data could not be written yet.", LogLevel.Error);
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

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        this.marnieFurnitureRecipesKnownOnShopOpen = e.NewMenu is ShopMenu shop
            && string.Equals(shop.ShopId, "AnimalShop", StringComparison.Ordinal)
                ? Game1.player.craftingRecipes.Keys.ToHashSet(StringComparer.Ordinal)
                : null;
    }

    private void UpdateMarnieFurnitureRecipeNotifications()
    {
        HashSet<string>? knownOnOpen = this.marnieFurnitureRecipesKnownOnShopOpen;
        if (knownOnOpen is null)
            return;

        if (Game1.activeClickableMenu is not ShopMenu shop
            || !string.Equals(shop.ShopId, "AnimalShop", StringComparison.Ordinal))
        {
            this.marnieFurnitureRecipesKnownOnShopOpen = null;
            return;
        }

        foreach (string recipeName in Game1.player.craftingRecipes.Keys.ToArray())
        {
            if (!knownOnOpen.Add(recipeName) || !FurnitureRecipeContent.IsFurnitureRecipe(recipeName))
                continue;

            Game1.addHUDMessage(new HUDMessage(
                "A new Slime Daycare furniture recipe has been learned.",
                HUDMessage.newQuest_type));
        }
    }

    private void OnObjectListChanged(object? sender, StardewModdingAPI.Events.ObjectListChangedEventArgs e)
    {
        if (!this.stateReady || !Context.IsMainPlayer)
            return;

        if (FurnitureSlotProtection.Apply(e.Location as AnimalHouse, this.registry!))
            this.Monitor.Log("Reconciled Stardew's native tool protection for Slime Daycare slot furniture.", LogLevel.Info);

        bool rareLabProgressionChanged = this.SynchronizeRareLabProgression(e.Location as AnimalHouse);
        bool removedBreederPair = false;
        string locationPrefix = e.Location.NameOrUniqueName + ":";
        foreach (KeyValuePair<Vector2, StardewValley.Object> removed in e.Removed)
        {
            if (removed.Value.QualifiedItemId != RanchingItemContent.BreederQualifiedId)
                continue;

            string machineKey = $"{locationPrefix}{(int)removed.Key.X},{(int)removed.Key.Y}";
            removedBreederPair |= this.registry!.RemoveBreederPair(machineKey);
        }

        if (!removedBreederPair && !rareLabProgressionChanged)
            return;

        if (this.SaveCurrentSaveDataNow())
        {
            if (removedBreederPair)
                this.Monitor.Log("The Slime Breeder was removed; its cycle and pending egg were discarded, and its parents are free to roam. A replacement starts with a blank selection.", LogLevel.Info);
            if (rareLabProgressionChanged)
                this.Monitor.Log("Synchronized saved Rare Lab progression for the Daycare's placed incubators.", LogLevel.Info);
        }
        else
        {
            if (removedBreederPair)
                this.Monitor.Log("The Slime Breeder was removed and its parents are free to roam in memory, but the cleared cycle could not yet be saved.", LogLevel.Error);
            if (rareLabProgressionChanged)
                this.Monitor.Log("Rare Lab progression changed in memory but could not yet be saved.", LogLevel.Error);
        }
    }

    private void OnRenderedWorld(object? sender, StardewModdingAPI.Events.RenderedWorldEventArgs e)
    {
        if (!this.stateReady || !Context.IsMainPlayer)
            return;

        if (this.config.ShowShadowCloneVisualEchoes)
            this.shadowCloneVisuals.Draw(Game1.currentLocation, e.SpriteBatch, this.registry!);
        this.mutationVisuals.Draw(Game1.currentLocation, e.SpriteBatch, this.registry!);
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
                BuildingContent.SlimeDaycareUpgrade3Id => 192,
                _ => 160
            };
            int chimneyTop = building.buildingType.Value switch
            {
                BuildingContent.SlimeDaycareUpgrade1Id => 23,
                BuildingContent.SlimeDaycareUpgrade2Id => 30,
                BuildingContent.SlimeDaycareUpgrade3Id => 30,
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

    private void ApplyDomesticatedSlimeAppearances()
    {
        if (this.registry is null)
            return;

        foreach (FarmAnimal animal in GetFarmAnimals())
        {
            if (animal.type.Value != FarmAnimalContent.DomesticatedSlimeTypeId)
                continue;
            FarmAnimalContent.ApplyAppearance(animal,
                this.registry.GetGenomeColor(animal.myID.Value),
                this.registry.GetGenomeSize(animal.myID.Value),
                this.registry.GetGenomeIsShiny(animal.myID.Value));
        }
    }

    private void RestoreFurnitureSlotProtection()
    {
        if (this.registry is null || !Context.IsWorldReady)
            return;

        foreach (Building building in Game1.getFarm().buildings)
        {
            if (building.GetIndoors() is AnimalHouse daycare
                && FurnitureSlotProtection.Apply(daycare, this.registry))
                this.Monitor.Log("Reconciled Stardew's native tool protection for Slime Daycare slot furniture.", LogLevel.Info);
        }
    }

    private bool SynchronizeRareLabProgression()
    {
        if (this.registry is null || !Context.IsWorldReady)
            return false;

        bool changed = false;
        foreach (Building building in Game1.getFarm().buildings)
        {
            if (!BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                || building.GetIndoors() is not AnimalHouse daycare)
                continue;

            changed |= this.SynchronizeRareLabProgression(daycare, building.buildingType.Value);
        }

        return changed;
    }

    private bool SynchronizeRareLabProgression(AnimalHouse? daycare)
    {
        if (daycare?.ParentBuilding is not Building building
            || !BuildingContent.IsSlimeDaycare(building.buildingType.Value))
            return false;

        return this.SynchronizeRareLabProgression(daycare, building.buildingType.Value);
    }

    private bool SynchronizeRareLabProgression(Building building)
    {
        if (!BuildingContent.IsSlimeDaycare(building.buildingType.Value)
            || building.GetIndoors() is not AnimalHouse daycare)
            return false;

        return this.SynchronizeRareLabProgression(daycare, building.buildingType.Value);
    }

    private bool SynchronizeRareLabProgression(AnimalHouse daycare, string buildingType)
    {
        string[] incubatorMachineKeys = daycare.Objects.Pairs
            .Where(pair => pair.Value.QualifiedItemId == RanchingItemContent.IncubatorQualifiedId)
            .Select(pair => $"{daycare.NameOrUniqueName}:{(int)pair.Key.X},{(int)pair.Key.Y}")
            .ToArray();
        return this.registry!.SynchronizeRareLabs(incubatorMachineKeys, BuildingContent.GetUpgradeTier(buildingType));
    }

    private bool SaveCurrentSaveDataNow()
    {
        if (!this.stateReady || !Context.IsMainPlayer)
            return false;

        try
        {
            this.registry!.WriteEntityModData(GetFarmAnimals());
            this.stateStore!.Save(this.registry!.Capture());
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            this.Monitor.Log($"Could not save Domesticated Slime data to the current save: {exception.Message}", LogLevel.Error);
            return false;
        }
    }

    private bool StartIncubation(string machineKey, int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= Game1.player.Items.Count
            || Game1.player.Items[inventoryIndex] is not Item item)
            return false;

        SlimeRegistry registry = this.registry!;
        if (!SlimeEggContent.TryGetEggId(item, out string eggItemId))
            return false;

        if (SlimeEggContent.TryReadRecord(item, out SlimeEggRecord itemEgg)
            && !registry.RegisterEggItem(itemEgg))
            return false;

        if (!registry.CanIncubate(eggItemId)
            || !registry.StartIncubation(machineKey, eggItemId, Game1.Date.TotalDays))
        {
            return false;
        }

        item.Stack--;
        if (item.Stack <= 0)
            Game1.player.Items[inventoryIndex] = null;
        // Save the item removal and external incubation together at Stardew's normal Saving event.
        // If the player exits without saving, both the egg and the incubation should roll back.
        return true;
    }

    private bool CanCancelIncubation(string machineKey)
    {
        if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer
            || this.registry?.GetIncubation(machineKey) is not { HatchAnimalId: null })
            return false;

        return Game1.player.couldInventoryAcceptThisItem(ItemRegistry.Create("(O)194"));
    }

    private void RequestIncubationCancellation(string machineKey)
    {
        if (!this.CanCancelIncubation(machineKey))
        {
            Game1.drawObjectDialogue("Make room in your inventory for the Fried Egg before cancelling this incubation.");
            return;
        }

        Game1.activeClickableMenu = null;
        Game1.currentLocation.createQuestionDialogue(
            "Cancel incubation? The Domesticated Slime Egg will be destroyed, and you will receive one Fried Egg.",
            new[]
            {
                new StardewValley.Response("confirm", "Destroy egg"),
                new StardewValley.Response("keep", "Keep incubating")
            },
            (farmer, answer) =>
            {
                if (answer == "confirm" && !this.CancelIncubation(machineKey))
                    Game1.drawObjectDialogue("The incubation could not be cancelled. The egg and its saved progress were kept.");
            });
    }

    private bool CancelIncubation(string machineKey)
    {
        if (!this.CanCancelIncubation(machineKey) || this.registry is null)
            return false;

        (Item? Item, int Stack)[] inventorySnapshot = Game1.player.Items
            .Select(item => (Item: (Item?)item, Stack: item?.Stack ?? 0))
            .ToArray();
        Item friedEgg = ItemRegistry.Create("(O)194");
        if (!Game1.player.addItemToInventoryBool(friedEgg))
        {
            RestoreInventorySnapshot(inventorySnapshot);
            return false;
        }

        if (!this.registry.CancelIncubation(machineKey, out IncubationRecord cancelled))
        {
            RestoreInventorySnapshot(inventorySnapshot);
            return false;
        }

        string receiptKey = IncubationCancellationReceiptPrefix + cancelled.EggItemId;
        Game1.player.modData[receiptKey] = "1";
        if (this.SaveCurrentSaveDataNow())
        {
            Game1.playSound("smallSelect");
            return true;
        }

        bool restored = this.registry.RestoreCancelledIncubation(cancelled);
        Game1.player.modData.Remove(receiptKey);
        RestoreInventorySnapshot(inventorySnapshot);
        if (!this.SaveCurrentSaveDataNow())
            this.Monitor.Log("Could not persist the rollback after an incubator cancellation save failure; the incubation was restored in memory and will be retried on the next save.", LogLevel.Error);
        if (!restored)
            this.Monitor.Log($"Could not restore cancelled incubation '{machineKey}' after a save failure.", LogLevel.Error);
        return false;
    }

    private static void RestoreInventorySnapshot((Item? Item, int Stack)[] snapshot)
    {
        for (int index = 0; index < snapshot.Length && index < Game1.player.Items.Count; index++)
        {
            (Item? item, int stack) = snapshot[index];
            if (item is not null)
                item.Stack = stack;
            Game1.player.Items[index] = item;
        }
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
            this.Monitor.Log($"Confirmed {recovered} previous Domesticated Slime hatch(es) against live animal IDs.", LogLevel.Info);
            if (!this.SaveCurrentSaveDataNow())
                this.Monitor.Log("The confirmed hatch could not be finalized in save data; its reserved hatch record will be checked again on reload.", LogLevel.Error);
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
                if (recordedAnimal?.type.Value == FarmAnimalContent.DomesticatedSlimeTypeId)
                {
                    this.registry.RegisterIncubationHatch(incubation, recordedAnimal);
                    continue;
                }
            }

            if (!incubation.RareOutcomeResolved)
            {
                RareLabRecord? lab = this.registry.GetRareLab(incubation.MachineKey);
                int chancePoints = RareLabRules.GetChancePoints(lab);
                bool rareOffspringProduced = lab is { IsUnlocked: true }
                    && Game1.random.Next(RareLabRules.MaximumChancePoints) < chancePoints;
                string? rareSpecies = rareOffspringProduced
                    ? lab?.CatalystSpecies ?? RareLabRules.RareOffspringSpecies[Game1.random.Next(RareLabRules.RareOffspringSpecies.Count)]
                    : null;

                if (!this.registry.ResolveIncubationOutcome(incubation.MachineKey, rareOffspringProduced, rareSpecies,
                        out RareLabRecord? previousLab))
                {
                    this.Monitor.Log($"Could not resolve the Rare Lab outcome for incubator '{incubation.MachineKey}'. The egg will retry without changing its saved chance.", LogLevel.Error);
                    continue;
                }

                if (!this.SaveCurrentSaveDataNow())
                {
                    this.registry.RollbackIncubationOutcome(incubation.MachineKey, previousLab);
                    if (!this.SaveCurrentSaveDataNow())
                        this.Monitor.Log($"Could not persist the rollback after a Rare Lab outcome save failure for incubator '{incubation.MachineKey}'. Its chance state was restored in memory; reconciliation will retry on reload.", LogLevel.Error);
                    else
                        this.Monitor.Log($"Could not persist the Rare Lab outcome for incubator '{incubation.MachineKey}'. Its chance state was restored and the roll will retry later.", LogLevel.Error);
                    continue;
                }

                string result = rareOffspringProduced ? $"rare {rareSpecies}" : "regular offspring";
                this.Monitor.Log($"Resolved incubator '{incubation.MachineKey}' at {chancePoints}% chance: {result}.", LogLevel.Info);
            }

            if (animalHouse.isFull())
            {
                this.Monitor.Log("A Domesticated Slime Egg is ready, but the Slime Daycare is full; hatching will retry after space is available.", LogLevel.Trace);
                continue;
            }

            long animalId = incubation.HatchAnimalId ?? this.Helper.Multiplayer.GetNewID();
            if (incubation.HatchAnimalId is null)
            {
                if (!this.registry.AssignHatchAnimalId(incubation.MachineKey, animalId)
                    || !this.SaveCurrentSaveDataNow())
                {
                    this.registry.ClearHatchAnimalId(incubation.MachineKey);
                    this.Monitor.Log("Could not reserve an external native ID for a ready Domesticated Slime hatch; it will retry tomorrow.", LogLevel.Error);
                    continue;
                }
            }

            if (GetFarmAnimals().Any(animal => animal.myID.Value == animalId))
            {
                this.registry.ClearHatchAnimalId(incubation.MachineKey);
                this.SaveCurrentSaveDataNow();
                this.Monitor.Log($"Could not hatch genome {egg.Genome.Id:N}: reserved FarmAnimal ID {animalId} is already in use. Cleared it so the incubator can retry with a new ID.", LogLevel.Error);
                continue;
            }

            var animal = new FarmAnimal(FarmAnimalContent.DomesticatedSlimeTypeId, animalId, Game1.player.UniqueMultiplayerID)
            {
                Name = (incubation.HatchGenome ?? egg.Genome).PrimarySpecies,
                home = daycare,
                homeInterior = animalHouse
            };
            animal.age.Value = 0;
            SlimeGenome hatchGenome = incubation.HatchGenome ?? egg.Genome;
            FarmAnimalContent.ApplyAppearance(animal, hatchGenome.VisualColor, hatchGenome.Size, hatchGenome.IsShiny);
            animalHouse.adoptAnimal(animal);
            if (!this.registry.RegisterIncubationHatch(incubation, animal))
            {
                this.Monitor.Log($"Hatched Domesticated Slime {animalId}, but its genome could not be linked; external hatch recovery will retry on reload.", LogLevel.Error);
                continue;
            }

            if (!this.SaveCurrentSaveDataNow())
                this.Monitor.Log("The animal hatched, but its updated genome link could not yet be written. The reserved external hatch record will recover it after reload.", LogLevel.Error);
            Game1.addHUDMessage(new HUDMessage($"A {animal.Name} hatched in the Slime Daycare!", HUDMessage.newQuest_type));
            this.Monitor.Log($"A Domesticated Slime hatched from genome egg {egg.Genome.Id:N} (FarmAnimal ID {animalId}).", LogLevel.Info);
            this.InvalidateEggObjectData();
        }
    }

    private void LogRegistryReconciliation(int detachedAnimals, int cancelledCycles)
    {
        if (detachedAnimals == 0 && cancelledCycles == 0)
            return;

        this.Monitor.Log(
            $"Reconciled Domesticated Slime save data: detached {detachedAnimals} missing animal link(s) while preserving their genomes; cancelled {cancelledCycles} cycle(s) without a saved egg.",
            LogLevel.Info);
    }

    private Item CreateEggItem(Guid genomeId)
    {
        string eggId = genomeId.ToString("N");
        if (this.registry is null || !this.registry.TryGetEgg(eggId, out SlimeEggRecord egg))
            throw new InvalidOperationException($"No stored egg genome exists for {eggId}.");
        return SlimeEggContent.CreateItem(egg);
    }

    private void InvalidateEggObjectData()
    {
        SlimeEggContent.InvalidateCache();
        this.Helper.GameContent.InvalidateCache("Data/Objects");
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        MachineArtContent.LoadAssets(e);
        RanchingItemContent.LoadAssets(e);
        RanchingItemContent.EditAsset(e);
        ResearchLabContent.EditAsset(e);
        ChromaticCrystalContent.EditAsset(e);
        ShopContent.EditAsset(e, this.config);
        FurnitureRecipeContent.EditAsset(e);
        SlimeEggContent.LoadAssets(e);
        SlimeEggContent.EditAsset(e, this.Helper.DirectoryPath);
        GuidebookContent.EditAsset(e);
        FarmAnimalContent.LoadAssets(e);
        FarmAnimalContent.EditAsset(e);
        FurnitureContent.LoadAssets(e);
        FurnitureContent.EditAsset(e);
        SlimePrinterContent.EditAsset(e);
        MutagenicPoolContent.EditAsset(e);
        SlimeDaycareQuestContent.EditAsset(e);
        BuildingContent.EditAsset(e);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && Game1.currentLocation is AnimalHouse feedingDaycare
            && BuildingContent.IsSlimeDaycare(feedingDaycare.ParentBuilding?.buildingType.Value)
            && Game1.player.CurrentItem is StardewValley.Object heldItem
            && heldItem.bigCraftable.Value
            && DaycareFeedingService.IsFeedingTroughTile(feedingDaycare, (int)e.Cursor.GrabTile.X, (int)e.Cursor.GrabTile.Y))
        {
            this.Helper.Input.Suppress(e.Button);
            Game1.showRedMessage("This fixed Feeding Trough tile is reserved for Hay.");
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && Game1.currentLocation.Objects.TryGetValue(e.Cursor.GrabTile, out StardewValley.Object? legacyFurniture)
            && (legacyFurniture.QualifiedItemId == FurnitureContent.CompostTroughQualifiedId
                || legacyFurniture.QualifiedItemId == FurnitureContent.SlimePlaygroundQualifiedId
                || legacyFurniture.QualifiedItemId == FurnitureContent.SlimeBedQualifiedId))
        {
            this.Helper.Input.Suppress(e.Button);
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && MachineArtContent.TryGetMachineAt(Game1.currentLocation, e.Cursor.GrabTile,
                obj => obj is Chest chest && EggCryoLabContent.IsEggCryoLab(chest),
                out StardewValley.Object cryoLabObject, out _)
            && cryoLabObject is Chest cryoLab)
        {
            this.Helper.Input.Suppress(e.Button);
            if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer || this.registry is null)
            {
                this.Monitor.Log("The Egg CryoLab requires loaded mod save data in single-player.", LogLevel.Warn);
                return;
            }

            Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
                BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                && building.GetIndoors() == Game1.currentLocation);
            if (daycare?.GetIndoors() is not AnimalHouse || BuildingContent.GetUpgradeTier(daycare.buildingType.Value) < 1)
            {
                Game1.showRedMessage("The Egg CryoLab only works inside an upgraded Slime Daycare.");
                return;
            }

            this.RegisterEggCryoLabContents(cryoLab);
            this.OpenEggCryoLabMenu(cryoLab);
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && this.TryHandleRobinPoolRepairPrompt(e.Button, e.Cursor.GrabTile))
            return;

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && this.TryHandleMaruRepairInteraction(e.Button, e.Cursor.GrabTile))
            return;

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && MachineArtContent.TryGetMachineAt(Game1.currentLocation, e.Cursor.GrabTile,
                IncubatorPlacementGeometry.IsIncubator, out _, out Vector2 incubatorAnchor))
        {
            this.OpenIncubatorMenu(incubatorAnchor, e);
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && MachineArtContent.TryGetMachineAt(Game1.currentLocation, e.Cursor.GrabTile,
                ResearchLabContent.IsResearchLab, out _, out Vector2 researchLabAnchor))
        {
            this.Helper.Input.Suppress(e.Button);
            if (!ResearchLabContent.TryGetAdjacentIncubator(Game1.currentLocation, researchLabAnchor,
                    out Vector2 linkedIncubatorAnchor))
            {
                Game1.showRedMessage("Place the Research Lab directly beside exactly one Slime Incubator.");
                return;
            }

            this.OpenIncubatorMenu(linkedIncubatorAnchor, e);
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && MachineArtContent.TryGetMachineAt(Game1.currentLocation, e.Cursor.GrabTile,
                SlimePrinterContent.IsSlimePrinter, out StardewValley.Object printer, out Vector2 printerAnchor)
            && SlimePrinterContent.IsSlimePrinter(printer))
        {
            if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer)
            {
                this.Monitor.Log("The Slime Printer requires loaded mod save data in single-player.", LogLevel.Warn);
                return;
            }

            StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
                BuildingContent.IsSlimeDaycare(building.buildingType.Value) && building.GetIndoors() == Game1.currentLocation);
            if (daycare?.GetIndoors() is not StardewValley.AnimalHouse)
            {
                this.Monitor.Log("The Slime Printer only works inside the Slime Daycare.", LogLevel.Warn);
                return;
            }

            this.Helper.Input.Suppress(e.Button);
            string machineKey = $"{Game1.currentLocation.NameOrUniqueName}:{(int)printerAnchor.X},{(int)printerAnchor.Y}";
            Building activeDaycare = daycare;
            Game1.activeClickableMenu = new SlimePrinterMenu(this.registry!, machineKey, SlimePrinterRules.FromConfig(this.config),
                () => BuildingContent.GetUpgradeTier(activeDaycare.buildingType.Value) >= 3,
                () => this.StartSlimePrinterBatch(machineKey, activeDaycare),
                () => this.CollectSlimePrinterBatch(machineKey));
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && this.TryGetBreederAt(e.Cursor.GrabTile, out Vector2 breederAnchor))
        {
            if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer)
            {
                this.Monitor.Log("The Slime Breeder requires loaded mod save data in single-player.", LogLevel.Error);
                return;
            }

            this.Helper.Input.Suppress(e.Button);
            string machineKey = $"{Game1.currentLocation.NameOrUniqueName}:{(int)breederAnchor.X},{(int)breederAnchor.Y}";
            if (this.DiscardBreederPairsForMissingMachines(Game1.currentLocation))
                this.SaveCurrentSaveDataNow();
            Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
                BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                && building.GetIndoors() == Game1.currentLocation);
            int cycleDurationDays = BreedingRules.GetCycleDurationDays(daycare?.buildingType.Value);
            Game1.activeClickableMenu = new SlimeBreederMenu(this.registry!, machineKey, this.SaveCurrentSaveDataNow,
                this.CreateEggItem, cycleDurationDays);
            return;
        }

        if (Context.IsWorldReady && Game1.activeClickableMenu is null && e.Button.IsActionButton()
            && MachineArtContent.TryGetMachineAt(Game1.currentLocation, e.Cursor.GrabTile,
                obj => obj.QualifiedItemId == MutagenicPoolContent.QualifiedItemId,
                out StardewValley.Object pool, out Vector2 poolAnchor)
            && pool.QualifiedItemId == MutagenicPoolContent.QualifiedItemId)
        {
            if (!this.stateReady || !Context.IsMainPlayer || Context.IsMultiplayer)
            {
                this.Monitor.Log("The Mutagenic Pool requires loaded mod save data in single-player.", LogLevel.Warn);
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
            if (!this.registry!.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.MaruRepairCompleted))
            {
                Game1.drawObjectDialogue("The Mutagenic Pool is broken. Maru may be able to repair it.");
                return;
            }

            string machineKey = $"{Game1.currentLocation.NameOrUniqueName}:{(int)poolAnchor.X},{(int)poolAnchor.Y}";
            Game1.activeClickableMenu = new MutagenicPoolMenu(this.registry!, machineKey, MutagenicPoolRules.Instance,
                this.ContributeMutagenicPoolSlime);
            return;
        }

        if (!Context.IsWorldReady || Game1.activeClickableMenu is not null || !e.Button.IsUseToolButton())
            return;

        if (Game1.player.CurrentItem?.QualifiedItemId != GuidebookContent.QualifiedItemId)
            return;

        this.Helper.Input.Suppress(e.Button);
        Game1.activeClickableMenu = new StardewValley.Menus.LetterViewerMenu(GuidebookContent.Text);
    }

    private void OpenIncubatorMenu(Vector2 incubatorAnchor, ButtonPressedEventArgs e)
    {
        if (!this.stateReady || this.registry is null)
        {
            this.Monitor.Log("The external Domesticated Slime registry is unavailable; the incubator is disabled for this save.", LogLevel.Error);
            return;
        }

        Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value)
            && building.GetIndoors() == Game1.currentLocation);
        if (daycare?.GetIndoors() is not AnimalHouse)
        {
            this.Monitor.Log("The Slime Incubator only works inside the Slime Daycare.", LogLevel.Warn);
            return;
        }

        if (this.SynchronizeRareLabProgression(daycare) && !this.SaveCurrentSaveDataNow())
            this.Monitor.Log("The incubator's Rare Lab progression changed in memory but could not yet be saved.", LogLevel.Error);

        this.Helper.Input.Suppress(e.Button);
        string key = $"{Game1.currentLocation.NameOrUniqueName}:{(int)incubatorAnchor.X},{(int)incubatorAnchor.Y}";
        Game1.activeClickableMenu = new SlimeIncubatorMenu(this.registry, key,
            index => this.StartIncubation(key, index),
            name =>
            {
                IncubationRecord? record = this.registry.GetIncubation(key);
                if (record?.HatchAnimalId is not long animalId)
                    return false;
                FarmAnimal? animal = GetFarmAnimals().FirstOrDefault(candidate => candidate.myID.Value == animalId);
                if (animal is null)
                    return false;

                string previousName = animal.Name;
                animal.Name = string.IsNullOrWhiteSpace(name) ? "Domesticated Slime" : name.Trim();
                if (!this.registry.CompleteIncubation(key))
                {
                    animal.Name = previousName;
                    return false;
                }
                if (this.SaveCurrentSaveDataNow())
                    return true;
                this.registry.RestoreIncubation(record);
                animal.Name = previousName;
                return false;
            },
            () => this.RequestIncubationCancellation(key),
            this.ContributeRareLabSlime,
            this.AddRareLabCatalyst,
            IncubatorPlacementGeometry.Texture);
    }

    private bool SynchronizeEggCryoLabRecords()
    {
        if (this.registry is null || !Context.IsWorldReady)
            return false;

        bool changed = false;
        foreach (Chest chest in this.GetDaycareCryoLabChests())
        {
            changed |= this.RegisterEggCryoLabContents(chest);
        }

        return changed;
    }

    private IEnumerable<Chest> GetDaycareCryoLabChests()
    {
        foreach (Building building in Game1.getFarm().buildings)
        {
            if (!BuildingContent.IsSlimeDaycare(building.buildingType.Value)
                || building.GetIndoors() is not AnimalHouse daycare)
                continue;

            foreach (Chest chest in daycare.Objects.Values.OfType<Chest>().Where(EggCryoLabContent.IsEggCryoLab))
                yield return chest;
        }
    }

    private bool RegisterEggCryoLabContents(Chest chest)
    {
        if (this.registry is null)
            return false;

        bool changed = false;
        foreach (Item item in chest.GetItemsForPlayer())
        {
            if (item is null || !SlimeEggContent.TryReadRecord(item, out SlimeEggRecord egg)
                || !SlimeEggContent.TryGetEggId(item, out string eggId)
                || egg.EggId != eggId)
                continue;

            bool wasRegistered = this.registry.TryGetEgg(eggId, out _);
            if (this.registry.RegisterEggItem(egg))
                changed |= !wasRegistered;
        }

        if (changed && !this.SaveCurrentSaveDataNow())
            this.Monitor.Log("Stored Slime Egg records were recovered from the Egg CryoLab chest in memory but could not yet be saved.", LogLevel.Error);
        return changed;
    }

    private void OpenEggCryoLabMenu(Chest chest)
    {
        Game1.activeClickableMenu = new ItemGrabMenu(
            chest.GetItemsForPlayer(),
            reverseGrab: false,
            showReceivingMenu: true,
            highlightFunction: InventoryMenu.highlightAllItems,
            behaviorOnItemSelectFunction: (item, who) => this.StoreEggInCryoLab(chest, item, who),
            message: "Domesticated Slime Eggs only · 36 storage slots",
            behaviorOnItemGrab: (item, who) => this.TakeEggFromCryoLab(chest, item, who),
            snapToBottom: false,
            canBeExitedWithKey: true,
            playRightClickSound: true,
            allowRightClick: true,
            showOrganizeButton: false,
            source: 1,
            sourceItem: chest,
            whichSpecialButton: -1,
            context: chest);
    }

    private void StoreEggInCryoLab(Chest chest, Item item, Farmer who)
    {
        if (!this.IsEligibleCryoLabEgg(chest, item, who, out _, out string rejectionMessage))
        {
            Game1.playSound("cancel");
            Game1.showRedMessage(rejectionMessage);
            return;
        }

        Item? remainder = chest.addItem(item);
        if (remainder is not null)
        {
            Game1.playSound("cancel");
            Game1.showRedMessage("The Egg CryoLab is full.");
            if (Game1.activeClickableMenu is ItemGrabMenu fullMenu)
                fullMenu.heldItem = remainder;
            return;
        }

        if (!this.SaveCurrentSaveDataNow())
            this.Monitor.Log("An Egg CryoLab transfer is saved with the native chest; its Domesticated Slime Egg record can be rebuilt from item metadata if needed.", LogLevel.Warn);
        Game1.playSound("Ship");
        this.OpenEggCryoLabMenu(chest);
    }

    private void TakeEggFromCryoLab(Chest chest, Item item, Farmer who)
    {
        if (this.IsEligibleCryoLabEgg(chest, item, who, out _, out _))
            return;

        chest.addItem(item);
        if (Game1.activeClickableMenu is ItemGrabMenu menu)
            menu.heldItem = null;
        this.OpenEggCryoLabMenu(chest);
        Game1.playSound("cancel");
        Game1.showRedMessage("That egg is invalid or already exists outside the Egg CryoLab.");
    }

    private bool IsEligibleCryoLabEgg(Chest chest, Item item, Farmer who, out string eggId, out string rejectionMessage)
    {
        eggId = string.Empty;
        rejectionMessage = "The Egg CryoLab accepts unused Domesticated Slime Eggs only.";
        if (this.registry is null)
            return false;

        if (!SlimeEggContent.TryReadRecord(item, out SlimeEggRecord egg)
            || !SlimeEggContent.TryGetEggId(item, out eggId)
            || egg.EggId != eggId)
            return false;

        if (!this.registry.RegisterEggItem(egg))
        {
            rejectionMessage = "This Domesticated Slime Egg is invalid, already hatched, or retired.";
            return false;
        }

        string candidateEggId = eggId;
        if (!this.registry.CanIncubate(candidateEggId))
        {
            rejectionMessage = "This Domesticated Slime Egg has already been incubated or used.";
            return false;
        }

        if (this.registry.BreedingPairs.Any(pair => pair.OutputEggItemId == candidateEggId))
        {
            rejectionMessage = "Collect this egg from its Breeder before storing it.";
            return false;
        }

        if (who.Items.Any(existing => existing is not null
            && SlimeEggContent.TryGetEggId(existing, out string existingId) && existingId == candidateEggId))
        {
            rejectionMessage = "An egg with this unique ID already exists in your inventory.";
            return false;
        }

        if (this.GetDaycareCryoLabChests()
            .SelectMany(existingChest => existingChest.GetItemsForPlayer())
            .Any(existing => existing is not null
                && SlimeEggContent.TryGetEggId(existing, out string existingId) && existingId == candidateEggId))
        {
            rejectionMessage = "An egg with this unique ID is already stored in an Egg CryoLab.";
            return false;
        }

        return true;
    }

    private bool TryGetBreederAt(Vector2 clickedTile, out Vector2 anchor)
    {
        return MachineArtContent.TryGetMachineAt(Game1.currentLocation, clickedTile,
            obj => obj.QualifiedItemId == RanchingItemContent.BreederQualifiedId, out _, out anchor);
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
                .Where(obj => obj.QualifiedItemId == RanchingItemContent.BreederQualifiedId)
                .Select(obj => $"{prefix}{(int)obj.TileLocation.X},{(int)obj.TileLocation.Y}")
                .ToArray();
            removed += this.registry.RemoveBreederPairsForMissingMachines(prefix, placedMachineKeys);
        }

        if (removed == 0)
            return false;

        this.Monitor.Log($"Cleared {removed} breeder cycle(s) because their machine was removed. Parent Domesticated Slimes are free to roam; a replacement breeder starts with no selection.", LogLevel.Info);
        return true;
    }

    private void OnGiveTestEggsCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting test eggs.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length < 1 || args[0] != saveName)
        {
            this.Monitor.Log("Usage: slime_ranching_give_test_eggs [random|malformed|species=Name] [secondary=Name|none] [color=#RRGGBB] [size=Small|Normal|Large|Giant] [rarity=Tier] [shiny=true|false] [food=(O)itemId] [generation=number] [mutation=Starry|Spotted|Glowing|Crystalline|Mossy|Bubbly|Mushroom|Blooming|Firefly|Crowned|Cowboy|Smelly|none].", LogLevel.Warn);
            return;
        }

        bool randomMode = args.Skip(1).Any(argument => argument.Equals("random", StringComparison.OrdinalIgnoreCase));
        bool malformedMode = args.Skip(1).Any(argument => argument.Equals("malformed", StringComparison.OrdinalIgnoreCase));
        if ((randomMode || malformedMode) && (args.Length != 2 || randomMode && malformedMode))
        {
            this.Monitor.Log("Use either 'slime_ranching_give_test_eggs random', 'slime_ranching_give_test_eggs malformed', or the optional key=value fields, not more than one mode.", LogLevel.Warn);
            return;
        }

        Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);
        if (!randomMode && !malformedMode && !TryParseTestEggMetadata(args.Skip(1), out metadata, out string? metadataError))
        {
            this.Monitor.Log($"Invalid test egg metadata: {metadataError} Usage: slime_ranching_give_test_eggs [random|malformed|key=value ...].", LogLevel.Warn);
            return;
        }

        int requiredSlots = randomMode ? 1 : 2;
        if (Game1.player.Items.Count(item => item is null) < requiredSlots)
        {
            this.Monitor.Log($"Make room for {(requiredSlots == 1 ? "one egg" : "two eggs")} in your inventory, then try again.", LogLevel.Warn);
            return;
        }

        taums_slime_ranching.Models.SlimeEggRecord[] eggs;
        string description;
        if (randomMode)
        {
            eggs = new[] { this.CreateRandomTestEgg() };
            description = "one genome-specific Domesticated Slime Egg with randomized traits";
        }
        else if (malformedMode)
        {
            eggs = Array.Empty<taums_slime_ranching.Models.SlimeEggRecord>();
            description = "two deliberately malformed Domesticated Slime Egg items for non-stacking diagnostics";
        }
        else
        {
            Guid mintId = Guid.NewGuid();
            Guid violetId = Guid.NewGuid();
            string species = NormalizeTestGenomeName(metadata.GetValueOrDefault("species", "Domesticated Slime"));
            string? secondarySpecies = metadata.TryGetValue("secondary", out string? secondary)
                && !secondary.Equals("none", StringComparison.OrdinalIgnoreCase)
                    ? NormalizeTestGenomeName(secondary)
                    : null;
            string? chosenColor = metadata.GetValueOrDefault("color");
            string? chosenSize = metadata.GetValueOrDefault("size");
            string? chosenRarity = metadata.GetValueOrDefault("rarity");
            bool? chosenShiny = metadata.TryGetValue("shiny", out string? shinyText) ? bool.Parse(shinyText) : null;
            string? chosenFood = metadata.GetValueOrDefault("food");
            int? chosenGeneration = metadata.TryGetValue("generation", out string? generationText) ? int.Parse(generationText) : null;
            string? chosenMutation = metadata.TryGetValue("mutation", out string? mutationText)
                && !mutationText.Equals("none", StringComparison.OrdinalIgnoreCase) ? mutationText : null;
            eggs = new[]
            {
                this.CreateTestEgg(mintId, species, secondarySpecies, chosenColor ?? "#65DDBB", chosenSize, chosenRarity, chosenShiny, chosenFood, chosenGeneration, chosenMutation),
                this.CreateTestEgg(violetId, species, secondarySpecies, chosenColor ?? "#8A2BE2", chosenSize, chosenRarity, chosenShiny, chosenFood, chosenGeneration, chosenMutation)
            };
            description = $"two distinct genome-specific Domesticated Slime Eggs ({species}; {eggs[0].Genome.VisualColor}, {eggs[0].Genome.Size}, {eggs[0].Genome.RarityTier}, shiny={eggs[0].Genome.IsShiny}, generation={eggs[0].Genome.Generation}, mutation={eggs[0].Genome.MutationTrait ?? "none"})";
        }

        try
        {
            if (!this.SaveCurrentSaveDataNow())
                return;
            this.InvalidateEggObjectData();
            if (malformedMode)
            {
                for (int index = 0; index < 2; index++)
                {
                    Item malformedEgg = ItemRegistry.Create($"{SlimeEggContent.QualifiedItemIdPrefix}Red");
                    malformedEgg.modData[SlimeEggContent.EggIdModDataKey] = "not-a-guid";
                    malformedEgg.modData[SlimeEggContent.EggRecordModDataKey] = "{}";
                    if (!Game1.player.addItemToInventoryBool(malformedEgg))
                        throw new InvalidOperationException("Inventory space changed while the malformed test eggs were being added.");
                }
            }

            foreach (taums_slime_ranching.Models.SlimeEggRecord egg in eggs)
            {
                Item item = SlimeEggContent.CreateItem(egg);
                if (!Game1.player.addItemToInventoryBool(item))
                    throw new InvalidOperationException("Inventory space changed while the test eggs were being added.");
            }

            string identityMessage = malformedMode
                ? "Each item's identity metadata is intentionally invalid; do not incubate them."
                : "Egg identity is stored on each item.";
            this.Monitor.Log($"Added {description}. {identityMessage}", LogLevel.Info);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            this.Monitor.Log($"Could not create the test eggs: {exception.Message}", LogLevel.Error);
        }
    }

    private taums_slime_ranching.Models.SlimeEggRecord CreateTestEgg(Guid genomeId, string species,
        string? secondarySpecies, string color, string? size, string? rarity, bool? isShiny,
        string? likedFoodItemId, int? generation, string? mutation)
    {
        var genome = new taums_slime_ranching.Models.SlimeGenome
        {
            Id = genomeId,
            PrimarySpecies = species,
            SecondarySpecies = secondarySpecies,
            VisualColor = color,
            Size = size ?? "Normal",
            IsShiny = isShiny ?? false,
            RarityTier = rarity ?? "Common",
            MutationTrait = mutation,
            Generation = generation ?? 0,
            LikedFoodItemId = likedFoodItemId ?? FoodLikeCatalog.ForSlime(genomeId)
        };
        return this.registry!.CreateEgg(genome);
    }

    private taums_slime_ranching.Models.SlimeEggRecord CreateRandomTestEgg()
    {
        (string Name, string Hex)[] colors =
        {
            ("Red", "#FF0000"), ("Orange", "#FF9500"), ("Yellow", "#FFD60A"),
            ("Green", "#00FF00"), ("Blue", "#0A84FF"), ("Purple", "#BF5AF2"), ("Pink", "#FF69B4")
        };
        string[] sizes = { "Small", "Normal", "Large", "Giant" };
        string[] rarities = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
        var primary = colors[Game1.random.Next(colors.Length)];
        string? secondary = Game1.random.NextDouble() < 0.3
            ? $"{colors.Where(color => color.Name != primary.Name).ElementAt(Game1.random.Next(colors.Length - 1)).Name} Slime"
            : null;
        Guid id = Guid.NewGuid();
        return this.registry!.CreateEgg(new SlimeGenome
        {
            Id = id,
            PrimarySpecies = $"{primary.Name} Slime",
            SecondarySpecies = secondary,
            VisualColor = primary.Hex,
            Size = sizes[Game1.random.Next(sizes.Length)],
            RarityTier = rarities[Game1.random.Next(rarities.Length)],
            IsShiny = Game1.random.NextDouble() < 0.1,
            LikedFoodItemId = FoodLikeCatalog.GetRandomItemId(),
            Generation = Game1.random.Next(0, 7),
            MutationTrait = Game1.random.NextDouble() < 0.1
                ? MutagenicPoolRules.MutationTraits[Game1.random.Next(MutagenicPoolRules.MutationTraits.Count)]
                : null
        });
    }

    private static bool TryParseTestEggMetadata(IEnumerable<string> arguments,
        out Dictionary<string, string> metadata, out string? error)
    {
        string[] allowedKeys = { "species", "secondary", "color", "size", "rarity", "shiny", "food", "generation", "mutation" };
        metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        error = null;
        foreach (string argument in arguments)
        {
            int separator = argument.IndexOf('=');
            if (separator <= 0 || separator == argument.Length - 1)
            {
                error = $"'{argument}' must be a key=value option";
                return false;
            }

            string key = argument[..separator].Trim();
            string value = argument[(separator + 1)..].Trim();
            if (!allowedKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                error = $"unknown field '{key}'";
                return false;
            }
            if (!metadata.TryAdd(key, value))
            {
                error = $"field '{key}' was specified more than once";
                return false;
            }
        }

        if (metadata.TryGetValue("species", out string? species)
            && (NormalizeTestGenomeName(species).Length is 0 or > 40))
        {
            error = "species must be 1-40 characters";
            return false;
        }
        if (metadata.TryGetValue("secondary", out string? secondary)
            && !secondary.Equals("none", StringComparison.OrdinalIgnoreCase)
            && NormalizeTestGenomeName(secondary).Length is 0 or > 40)
        {
            error = "secondary must be none or a name of 1-40 characters";
            return false;
        }
        if (metadata.TryGetValue("color", out string? color) && !IsHexColor(color))
        {
            error = "color must use #RRGGBB format";
            return false;
        }
        if (metadata.TryGetValue("size", out string? size)
            && !new[] { "Small", "Normal", "Large", "Giant" }.Contains(size, StringComparer.OrdinalIgnoreCase))
        {
            error = "size must be Small, Normal, Large, or Giant";
            return false;
        }
        if (metadata.TryGetValue("rarity", out string? rarity) && (rarity.Length > 32 || string.IsNullOrWhiteSpace(rarity)))
        {
            error = "rarity must be 1-32 characters";
            return false;
        }
        if (metadata.TryGetValue("shiny", out string? shiny) && !bool.TryParse(shiny, out _))
        {
            error = "shiny must be true or false";
            return false;
        }
        if (metadata.TryGetValue("food", out string? food) && !FoodLikeCatalog.IsEligible(food))
        {
            error = "food must be one of the eligible monster-drop or fishing-trash IDs (e.g. (O)684 or (O)168)";
            return false;
        }
        if (metadata.TryGetValue("generation", out string? generation)
            && (!int.TryParse(generation, out int generationNumber) || generationNumber is < 0 or > 999))
        {
            error = "generation must be a number from 0 to 999";
            return false;
        }
        if (metadata.TryGetValue("mutation", out string? mutation)
            && !mutation.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            string? approvedMutation = MutagenicPoolRules.MutationTraits
                .FirstOrDefault(trait => trait.Equals(mutation, StringComparison.OrdinalIgnoreCase));
            if (approvedMutation is null)
            {
                error = "mutation must be one of the 12 approved mutation names or none";
                return false;
            }
            metadata["mutation"] = approvedMutation;
        }

        if (metadata.TryGetValue("size", out string? canonicalSize))
            metadata["size"] = new[] { "Small", "Normal", "Large", "Giant" }.First(value => value.Equals(canonicalSize, StringComparison.OrdinalIgnoreCase));
        if (metadata.TryGetValue("color", out string? canonicalColor))
            metadata["color"] = canonicalColor.ToUpperInvariant();
        return true;
    }

    private static string NormalizeTestGenomeName(string value) => value.Replace('_', ' ').Trim();

    private static bool IsHexColor(string value) => value.Length == 7 && value[0] == '#'
        && value.AsSpan(1).ToString().All(Uri.IsHexDigit);

    private void OnGiveColorEggsCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting color test eggs.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To add color test eggs to the currently loaded save, use: slime_ranching_give_color_eggs", LogLevel.Warn);
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
                PrimarySpecies = "Domesticated Slime",
                VisualColor = color,
                Generation = 1,
                RarityTier = "Common",
                LikedFoodItemId = FoodLikeCatalog.ForSlime(genomeId)
            });
        }).ToArray();

        try
        {
            if (!this.SaveCurrentSaveDataNow())
                return;
            this.InvalidateEggObjectData();
            foreach (SlimeEggRecord egg in eggs)
                Game1.player.addItemToInventoryBool(SlimeEggContent.CreateItem(egg));

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
            this.Monitor.Log("To give crystals to the currently loaded save, use: slime_ranching_give_crystals", LogLevel.Warn);
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

    private void OnSpawnDomesticatedSlimeCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before spawning a Domesticated Slime.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        bool randomTraits = args.Length == 3 && args[2].Equals("random", StringComparison.OrdinalIgnoreCase);
        if ((args.Length != 2 && !randomTraits) || args[0] != saveName
            || !int.TryParse(args[1], out int ageDays)
            || ageDays < 0 || ageDays > FarmAnimalContent.DomesticatedSlimeDaysToMature)
        {
            this.Monitor.Log($"Usage: slime_ranching_spawn_domesticated_slime <age days 0-{FarmAnimalContent.DomesticatedSlimeDaysToMature}> [random]. Add 'random' to randomize the genome traits while keeping the chosen age.", LogLevel.Warn);
            return;
        }

        StardewValley.Buildings.Building? daycare = Game1.getFarm().buildings.FirstOrDefault(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value) && building.GetIndoors() is StardewValley.AnimalHouse);
        if (daycare?.GetIndoors() is not StardewValley.AnimalHouse animalHouse)
        {
            this.Monitor.Log("Build the Slime Daycare first, then try again.", LogLevel.Warn);
            return;
        }

        if (animalHouse.isFull())
        {
            this.Monitor.Log("The Slime Daycare is full; make room before spawning a Domesticated Slime.", LogLevel.Warn);
            return;
        }

        long animalId = this.Helper.Multiplayer.GetNewID();
        var animal = new FarmAnimal(FarmAnimalContent.DomesticatedSlimeTypeId, animalId, Game1.player.UniqueMultiplayerID)
        {
            Name = "Domesticated Slime",
            home = daycare,
            homeInterior = animalHouse
        };
        animalHouse.adoptAnimal(animal);
        // Set the requested age after adoption in case AnimalHouse initializes it.
        // Initialize the mod-owned clock now so the breeder sees the chosen age.
        animal.age.Value = ageDays;
        Models.SlimeGenome? genome = randomTraits ? this.CreateRandomDomesticatedSlimeGenome() : null;
        this.registry!.RegisterDomesticatedSlimes(new[] { animal }, genome);
        this.registry.SynchronizeMaturity(new[] { animal }, Game1.Date.TotalDays);

        if (!this.SaveCurrentSaveDataNow())
            this.Monitor.Log($"Domesticated Slime {animalId} was added, but its entity metadata could not be saved yet.", LogLevel.Error);
        else
            this.Monitor.Log($"Added one {ageDays}-day-old Domesticated Slime to the Slime Daycare (FarmAnimal ID {animalId}){(randomTraits ? $" with randomized genome traits ({genome!.PrimarySpecies}, {genome.Size}, {genome.RarityTier}, shiny={genome.IsShiny}, generation={genome.Generation}, mutation={genome.MutationTrait ?? "none"})" : "")}. It is {(ageDays >= FarmAnimalContent.DomesticatedSlimeDaysToMature ? "mature and ready to select in the breeder" : "still growing") }.", LogLevel.Info);
    }

    private Models.SlimeGenome CreateRandomDomesticatedSlimeGenome()
    {
        (string Name, string Hex)[] colors =
        {
            ("Red", "#FF0000"), ("Orange", "#FF9500"), ("Yellow", "#FFD60A"),
            ("Green", "#00FF00"), ("Blue", "#0A84FF"), ("Purple", "#BF5AF2"), ("Pink", "#FF69B4")
        };
        string[] sizes = { "Small", "Normal", "Large", "Giant" };
        string[] rarities = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
        var primary = colors[Game1.random.Next(colors.Length)];
        string? secondary = Game1.random.NextDouble() < 0.3
            ? $"{colors.Where(color => color.Name != primary.Name).ElementAt(Game1.random.Next(colors.Length - 1)).Name} Slime"
            : null;

        return new Models.SlimeGenome
        {
            Id = Guid.NewGuid(),
            PrimarySpecies = $"{primary.Name} Slime",
            SecondarySpecies = secondary,
            VisualColor = primary.Hex,
            Size = sizes[Game1.random.Next(sizes.Length)],
            RarityTier = rarities[Game1.random.Next(rarities.Length)],
            IsShiny = Game1.random.NextDouble() < 0.1,
            LikedFoodItemId = Services.FoodLikeCatalog.GetRandomItemId(),
            Generation = Game1.random.Next(0, 7),
            MutationTrait = Game1.random.NextDouble() < 0.1
                ? MutagenicPoolRules.MutationTraits[Game1.random.Next(MutagenicPoolRules.MutationTraits.Count)]
                : null
        };
    }

    private void OnSetRareLabCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before changing Rare Lab test values.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 4 || args[0] != saveName
            || !int.TryParse(args[1], out int incubatorNumber)
            || !int.TryParse(args[2], out int pityPoints)
            || !int.TryParse(args[3], out int contributionPoints))
        {
            this.Monitor.Log("Usage: slime_ranching_set_rare_lab <incubator number in current Daycare> <pity 0-100> <contribution 0-100>", LogLevel.Warn);
            return;
        }

        if (Game1.currentLocation is not StardewValley.AnimalHouse daycare
            || !BuildingContent.IsSlimeDaycare(daycare.ParentBuilding?.buildingType.Value)
            || !ReferenceEquals(daycare.ParentBuilding?.GetIndoors(), daycare))
        {
            this.Monitor.Log("Enter the Slime Daycare containing the target incubator before setting Rare Lab test values.", LogLevel.Warn);
            return;
        }

        string[] incubatorKeys = daycare.Objects.Pairs
            .Where(pair => pair.Value.QualifiedItemId == RanchingItemContent.IncubatorQualifiedId)
            .OrderBy(pair => pair.Key.Y)
            .ThenBy(pair => pair.Key.X)
            .Select(pair => $"{daycare.NameOrUniqueName}:{(int)pair.Key.X},{(int)pair.Key.Y}")
            .ToArray();
        if (incubatorNumber < 1 || incubatorNumber > incubatorKeys.Length)
        {
            this.Monitor.Log($"Choose a placed incubator number from 1 to {incubatorKeys.Length}; numbers follow tile order from top-left to bottom-right.", LogLevel.Warn);
            return;
        }

        string machineKey = incubatorKeys[incubatorNumber - 1];
        RareLabRecord? lab = this.registry!.GetRareLab(machineKey);
        if (lab is not { IsUnlocked: true }
            || this.registry.GetIncubation(machineKey) is not null
            || pityPoints is < 0 or > RareLabRules.MaximumChancePoints
            || contributionPoints is < 0 or > RareLabRules.MaximumChancePoints
            || lab.BaseChancePoints + pityPoints + contributionPoints > RareLabRules.MaximumChancePoints)
        {
            this.Monitor.Log("The selected incubator must have an unlocked lab with no egg or hatch pending, and valid chance values whose total is at most 100%.", LogLevel.Warn);
            return;
        }

        int previousPity = lab.PityPoints;
        int previousContribution = lab.ContributionPoints;
        lab.PityPoints = pityPoints;
        lab.ContributionPoints = contributionPoints;
        if (!this.SaveCurrentSaveDataNow())
        {
            lab.PityPoints = previousPity;
            lab.ContributionPoints = previousContribution;
            this.Monitor.Log("Could not save the test chance; the prior Rare Lab values were restored.", LogLevel.Error);
            return;
        }

        this.Monitor.Log($"Set Rare Lab {incubatorNumber} in the current Daycare to base {lab.BaseChancePoints}%, pity {lab.PityPoints}%, contribution {lab.ContributionPoints}%, total {this.registry.GetRareLabChancePoints(machineKey)}%.", LogLevel.Info);
    }

    private void OnSetMutagenicPoolTestStateCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before setting Mutagenic Pool test values.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 4 || args[0] != saveName
            || !long.TryParse(args[1], out long animalId)
            || !int.TryParse(args[2], out int failurePoints)
            || !int.TryParse(args[3], out int contributionPoints))
        {
            this.Monitor.Log("Usage: slime_ranching_set_pool_test_state <animal ID> <failures 0-90> <contribution 0-90; total <= 90>", LogLevel.Warn);
            return;
        }

        if (failurePoints is < 0 or > 90 || contributionPoints < 0 || contributionPoints > 90 - failurePoints)
        {
            this.Monitor.Log("Failure points must be 0-90 and contribution points 0-90, with a combined total no greater than 90.", LogLevel.Warn);
            return;
        }

        SlimeRegistry? loadedRegistry = this.registry;
        if (loadedRegistry is null || !loadedRegistry.TryGetSlimeRecord(animalId, out SlimeRecord slime))
        {
            this.Monitor.Log("That ID is not a managed Domesticated Slime.", LogLevel.Warn);
            return;
        }

        FarmAnimal? animal = GetFarmAnimals().FirstOrDefault(candidate => candidate.myID.Value == animalId
            && candidate.type.Value == FarmAnimalContent.DomesticatedSlimeTypeId);
        if (animal is null
            || loadedRegistry.GetMaturityAgeDays(animalId, animal.age.Value) < FarmAnimalContent.DomesticatedSlimeDaysToMature
            || !string.IsNullOrWhiteSpace(slime.Genome.MutationTrait)
            || loadedRegistry.IsReserved(animalId))
        {
            this.Monitor.Log("Choose a mature, unmutated, unreserved Domesticated Slime before setting its Pool test state.", LogLevel.Warn);
            return;
        }

        int oldFailures = slime.MutagenicPoolFailures;
        double oldContribution = slime.MutagenicPoolContribution;
        slime.MutagenicPoolFailures = failurePoints;
        slime.MutagenicPoolContribution = contributionPoints / 100.0;
        if (!this.SaveCurrentSaveDataNow())
        {
            slime.MutagenicPoolFailures = oldFailures;
            slime.MutagenicPoolContribution = oldContribution;
            this.Monitor.Log("Could not save the Mutagenic Pool test state; the prior values were restored.", LogLevel.Error);
            return;
        }

        double chance = MutagenicPoolRules.Instance.GetChance(failurePoints, contributionPoints / 100.0);
        this.Monitor.Log($"Set Domesticated Slime {animalId} Pool test state: {failurePoints} failed attempts, {contributionPoints}% contribution, {chance:P0} chance. No attempt was started.", LogLevel.Info);
    }

    private void OnSetSlimeSizeCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before changing a test genome.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 3 || args[0] != saveName || !long.TryParse(args[1], out long animalId))
        {
            this.Monitor.Log("Usage: slime_ranching_set_size <animal ID> <Small|Normal|Large|Giant>", LogLevel.Warn);
            return;
        }

        string? oldSize = this.registry!.GetGenomeSize(animalId);
        if (oldSize is null || !this.registry.SetGenomeSize(animalId, args[2]))
        {
            this.Monitor.Log("That ID is not a managed Domesticated Slime, or the requested size is invalid.", LogLevel.Warn);
            return;
        }

        if (!this.SaveCurrentSaveDataNow())
        {
            this.registry.SetGenomeSize(animalId, oldSize);
            FarmAnimal? animalToRestore = GetFarmAnimals().FirstOrDefault(candidate => candidate.myID.Value == animalId);
            if (animalToRestore is not null)
                FarmAnimalContent.ApplyAppearance(animalToRestore,
                    this.registry.GetGenomeColor(animalId), this.registry.GetGenomeSize(animalId), this.registry.GetGenomeIsShiny(animalId));
            this.Monitor.Log("Could not save Domesticated Slime save data; the original genome size was restored.", LogLevel.Error);
            return;
        }

        FarmAnimal? animalToUpdate = GetFarmAnimals().FirstOrDefault(candidate => candidate.myID.Value == animalId);
        if (animalToUpdate is not null)
            FarmAnimalContent.ApplyAppearance(animalToUpdate,
                this.registry.GetGenomeColor(animalId), this.registry.GetGenomeSize(animalId), this.registry.GetGenomeIsShiny(animalId));
        this.Monitor.Log($"Changed Domesticated Slime {animalId} genome size from {oldSize} to {this.registry.GetGenomeSize(animalId)}.", LogLevel.Info);
    }

    private void OnSetSlimeShinyCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before changing a test genome.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 3 || args[0] != saveName || !long.TryParse(args[1], out long animalId)
            || !bool.TryParse(args[2], out bool isShiny))
        {
            this.Monitor.Log("Usage: slime_ranching_set_shiny <animal ID> <true|false>", LogLevel.Warn);
            return;
        }

        bool oldValue = this.registry!.GetGenomeIsShiny(animalId);
        if (this.registry.GetGenomeSize(animalId) is null || !this.registry.SetGenomeShiny(animalId, isShiny))
        {
            this.Monitor.Log("That ID is not a managed Domesticated Slime.", LogLevel.Warn);
            return;
        }

        if (!this.SaveCurrentSaveDataNow())
        {
            this.registry.SetGenomeShiny(animalId, oldValue);
            this.RefreshDomesticatedSlimeAppearance(animalId);
            this.Monitor.Log("Could not save Domesticated Slime save data; the original shiny flag was restored.", LogLevel.Error);
            return;
        }

        this.RefreshDomesticatedSlimeAppearance(animalId);
        this.Monitor.Log($"Changed Domesticated Slime {animalId} shiny flag to {isShiny}.", LogLevel.Info);
    }

    private void OnSetSlimeColorCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before changing a test genome.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 3 || args[0] != saveName || !long.TryParse(args[1], out long animalId))
        {
            this.Monitor.Log("Usage: slime_ranching_set_color <animal ID> <Red|Orange|Yellow|Green|Blue|Purple|Pink>", LogLevel.Warn);
            return;
        }

        var oldState = this.registry!.GetGenomeColorState(animalId);
        if (oldState is null || !this.registry.SetGenomeBaseColor(animalId, args[2]))
        {
            this.Monitor.Log("That ID is not a managed Domesticated Slime, or the requested base color is invalid.", LogLevel.Warn);
            return;
        }

        if (!this.SaveCurrentSaveDataNow())
        {
            this.registry.RestoreGenomeColorState(animalId, oldState.Value);
            this.RefreshDomesticatedSlimeAppearance(animalId);
            this.Monitor.Log("Could not save Domesticated Slime save data; the original species and color were restored.", LogLevel.Error);
            return;
        }

        this.RefreshDomesticatedSlimeAppearance(animalId);
        this.Monitor.Log($"Changed Domesticated Slime {animalId} base color and species to {this.registry.GetGenomeColorState(animalId)?.PrimarySpecies}.", LogLevel.Info);
    }

    private void OnSetSlimeMutationCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before changing a test genome.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        string? chosenMutation = args.Length == 3 && args[2].Equals("none", StringComparison.OrdinalIgnoreCase)
            ? null
            : args.Length == 3
                ? MutagenicPoolRules.MutationTraits.FirstOrDefault(trait => trait.Equals(args[2], StringComparison.OrdinalIgnoreCase))
                : null;
        if (args.Length != 3 || args[0] != saveName || !long.TryParse(args[1], out long animalId)
            || (chosenMutation is null && !args[2].Equals("none", StringComparison.OrdinalIgnoreCase)))
        {
            this.Monitor.Log("Usage: slime_ranching_set_mutation <animal ID> <Starry|Spotted|Glowing|Crystalline|Mossy|Bubbly|Mushroom|Blooming|Firefly|Crowned|Cowboy|Smelly|none>", LogLevel.Warn);
            return;
        }

        string? oldTrait = this.registry!.GetMutationTrait(animalId);
        if (!this.registry.SetMutationTrait(animalId, chosenMutation))
        {
            this.Monitor.Log("That ID is not an unreserved managed Domesticated Slime, or the requested test mutation is invalid.", LogLevel.Warn);
            return;
        }

        if (!this.SaveCurrentSaveDataNow())
        {
            this.registry.SetMutationTrait(animalId, oldTrait);
            this.Monitor.Log("Could not save Domesticated Slime save data; the original mutation trait was restored.", LogLevel.Error);
            return;
        }

        this.Monitor.Log($"Changed Domesticated Slime {animalId} mutation trait to {this.registry.GetMutationTrait(animalId) ?? "none"}.", LogLevel.Info);
    }

    private void OnBindCompanionCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before binding a companion.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 2 || args[0] != saveName || !long.TryParse(args[1], out long animalId))
        {
            this.Monitor.Log("Usage: slime_ranching_bind_companion <animal ID>", LogLevel.Warn);
            return;
        }

        bool isInDaycare = Game1.getFarm().buildings.Any(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value)
            && building.GetIndoors() is StardewValley.AnimalHouse house
            && house.animals.Values.Any(animal => animal.myID.Value == animalId && animal.type.Value == FarmAnimalContent.DomesticatedSlimeTypeId));
        if (!isInDaycare || !this.registry!.BindCompanion(animalId))
        {
            this.Monitor.Log("Binding failed. Choose one mature managed Domesticated Slime in the Daycare that is not assigned to a ranching job; only one companion can be bound.", LogLevel.Warn);
            return;
        }

        if (!this.SaveCurrentSaveDataNow())
        {
            this.registry.UnbindCompanion(animalId);
            this.Monitor.Log("Could not save the binding to mod save data; the slime was unbound again.", LogLevel.Error);
            return;
        }

        this.Monitor.Log($"Bound Domesticated Slime {animalId} as the companion identity test. It remains in the Daycare; follower movement and the Core item are not implemented yet.", LogLevel.Info);
    }

    private void OnDismissCompanionCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before dismissing a companion.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("Usage: slime_ranching_dismiss_companion", LogLevel.Warn);
            return;
        }

        if (this.registry!.ActiveCompanionFarmAnimalId is not long animalId || !this.registry.UnbindCompanion(animalId))
        {
            this.Monitor.Log("There is no active companion binding to dismiss.", LogLevel.Warn);
            return;
        }

        if (!this.SaveCurrentSaveDataNow())
        {
            this.registry.BindCompanion(animalId);
            this.Monitor.Log("Could not save the dismissal to mod save data; the companion binding was restored.", LogLevel.Error);
            return;
        }

        this.Monitor.Log($"Dismissed companion Domesticated Slime {animalId}. The same animal and genome remain in the Daycare.", LogLevel.Info);
    }

    private void RefreshDomesticatedSlimeAppearance(long animalId)
    {
        FarmAnimal? animal = GetFarmAnimals().FirstOrDefault(candidate => candidate.myID.Value == animalId);
        if (animal is not null && this.registry is not null)
            FarmAnimalContent.ApplyAppearance(animal, this.registry.GetGenomeColor(animalId),
                this.registry.GetGenomeSize(animalId), this.registry.GetGenomeIsShiny(animalId));
    }

    private void OnOpenManagementDeskCommand(string command, string[] args)
    {
        SlimeRegistry? currentRegistry = this.registry;
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady || currentRegistry is null)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before opening the Daycare Management Desk preview.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To open the Daycare Management Desk preview for the current save, use: slime_ranching_open_management_desk", LogLevel.Warn);
            return;
        }

        if (Game1.activeClickableMenu is not null)
        {
            this.Monitor.Log("Close the current menu before opening the Daycare Management Desk preview.", LogLevel.Warn);
            return;
        }

        if (Game1.currentLocation is not AnimalHouse daycare
            || !BuildingContent.IsSlimeDaycare(daycare.ParentBuilding?.buildingType.Value))
        {
            this.Monitor.Log("Enter the Slime Daycare before opening its Management Desk preview.", LogLevel.Warn);
            return;
        }

        int tier = BuildingContent.GetUpgradeTier(daycare.ParentBuilding?.buildingType.Value);
        Game1.activeClickableMenu = new DaycareManagementMenu(daycare, currentRegistry, tier,
            this.trampolineInteractions.GetActiveActivitySummary, this.config);
        this.Monitor.Log("Opened the read-only Daycare Management Desk tester preview.", LogLevel.Info);
    }

    private void OnGiveGuidebookCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a guidebook.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a guidebook to the currently loaded save, use: slime_ranching_give_guidebook", LogLevel.Warn);
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
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a breeder.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a breeder to the currently loaded save, use: slime_ranching_give_breeder", LogLevel.Warn);
            return;
        }

        Item breeder = ItemRegistry.Create(RanchingItemContent.BreederQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(breeder) || !Game1.player.addItemToInventoryBool(breeder))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Breeder, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Breeder. Place it inside the Slime Daycare and use it to select two Domesticated Slimes.", LogLevel.Info);
    }

    private void OnGiveIncubatorCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting an incubator.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give an incubator to the currently loaded save, use: slime_ranching_give_incubator", LogLevel.Warn);
            return;
        }

        Item incubator = ItemRegistry.Create(RanchingItemContent.IncubatorQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(incubator) || !Game1.player.addItemToInventoryBool(incubator))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Incubator, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Incubator. Place it inside the Slime Daycare and use it with a genome-specific Domesticated Slime Egg in your inventory.", LogLevel.Info);
    }

    private void OnGiveResearchLabCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a Research Lab.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a Research Lab to the currently loaded save, use: slime_ranching_give_research_lab", LogLevel.Warn);
            return;
        }

        Item researchLab = ItemRegistry.Create(ResearchLabContent.QualifiedItemId);
        if (!Game1.player.couldInventoryAcceptThisItem(researchLab) || !Game1.player.addItemToInventoryBool(researchLab))
        {
            this.Monitor.Log("Make room in your inventory for the Research Lab, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Research Lab. Place it directly beside a Slime Incubator; clicking either machine opens that incubator's Rare Lab controls.", LogLevel.Info);
    }

    private void OnGiveCryoLabCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting an Egg CryoLab tester chest.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give an Egg CryoLab tester chest to the currently loaded save, use: slime_ranching_give_cryolab", LogLevel.Warn);
            return;
        }

        Chest cryoLab = EggCryoLabContent.CreateTestingChest();
        if (!Game1.player.couldInventoryAcceptThisItem(cryoLab) || !Game1.player.addItemToInventoryBool(cryoLab))
        {
            this.Monitor.Log("Make room in your inventory for the Egg CryoLab tester chest, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added a tester Egg CryoLab chest. Place it in a tier-1-or-higher Slime Daycare; it stores eligible Domesticated Slime Eggs without starting incubation.", LogLevel.Info);
    }

    private void OnDuplicateCryoLabEggCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady || this.registry is null)
        {
            this.Monitor.Log("Load a disposable single-player save before creating a duplicate Egg CryoLab test item.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("This command deliberately duplicates an egg identity. Use it only on a disposable copy: slime_ranching_duplicate_cryolab_egg", LogLevel.Warn);
            return;
        }

        if (Game1.currentLocation is not AnimalHouse daycare
            || !BuildingContent.IsSlimeDaycare(daycare.ParentBuilding?.buildingType.Value)
            || BuildingContent.GetUpgradeTier(daycare.ParentBuilding?.buildingType.Value) < 1)
        {
            this.Monitor.Log("Enter a tier-1-or-higher Slime Daycare containing a tester Egg CryoLab chest first.", LogLevel.Warn);
            return;
        }

        Chest? cryoLab = daycare.Objects.Values.OfType<Chest>().FirstOrDefault(EggCryoLabContent.IsEggCryoLab);
        Item? source = cryoLab?.GetItemsForPlayer()
            .FirstOrDefault(item => item is not null
                && SlimeEggContent.TryReadRecord(item, out _)
                && SlimeEggContent.TryGetEggId(item, out string eggId)
                && this.registry.CanIncubate(eggId)
                && !Game1.player.Items.Any(existing => existing is not null
                    && SlimeEggContent.TryGetEggId(existing, out string existingId) && existingId == eggId));
        if (source is null || !SlimeEggContent.TryGetEggId(source, out string sourceEggId))
        {
            this.Monitor.Log("The current Daycare needs a stored, unused Domesticated Slime Egg with no copy already in your inventory.", LogLevel.Warn);
            return;
        }

        Item duplicate = source.getOne();
        if (!Game1.player.couldInventoryAcceptThisItem(duplicate) || !Game1.player.addItemToInventoryBool(duplicate))
        {
            this.Monitor.Log("Make room in your inventory before creating the duplicate test egg.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log($"Created a deliberate copy of stored egg ID {sourceEggId}. Try storing it in another Daycare's CryoLab; discard the duplicate after the check. Do not save this fixture on a normal save.", LogLevel.Warn);
    }

    private void OnDuplicateInventoryEggCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady || this.registry is null)
        {
            this.Monitor.Log("Load a disposable single-player save before creating a duplicate inventory egg.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("This command deliberately duplicates an egg identity. Use it only on a disposable copy: slime_ranching_duplicate_inventory_egg", LogLevel.Warn);
            return;
        }

        if (Game1.currentLocation is not AnimalHouse daycare
            || !BuildingContent.IsSlimeDaycare(daycare.ParentBuilding?.buildingType.Value))
        {
            this.Monitor.Log("Enter the Slime Daycare with one unused Domesticated Slime Egg in inventory first.", LogLevel.Warn);
            return;
        }

        Item? source = Game1.player.Items.FirstOrDefault(item => item is not null
            && item.Stack == 1
            && SlimeEggContent.TryReadRecord(item, out SlimeEggRecord egg)
            && SlimeEggContent.TryGetEggId(item, out string eggId)
            && egg.EggId == eggId
            && this.registry.CanIncubate(eggId)
            && !this.registry.BreedingPairs.Any(pair => pair.OutputEggItemId == eggId)
            && Game1.player.Items.Count(existing => existing is not null
                && SlimeEggContent.TryGetEggId(existing, out string existingId) && existingId == eggId) == 1
            && !this.GetDaycareCryoLabChests()
                .SelectMany(chest => chest.GetItemsForPlayer())
                .Any(stored => stored is not null
                    && SlimeEggContent.TryGetEggId(stored, out string storedEggId) && storedEggId == eggId));
        if (source is null || !SlimeEggContent.TryGetEggId(source, out string sourceEggId))
        {
            this.Monitor.Log("The Daycare needs exactly one unused Domesticated Slime Egg in inventory, with no copy in a Breeder or Egg CryoLab.", LogLevel.Warn);
            return;
        }

        Item duplicate = source.getOne();
        if (!Game1.player.couldInventoryAcceptThisItem(duplicate) || !Game1.player.addItemToInventoryBool(duplicate))
        {
            this.Monitor.Log("Make room in your inventory before creating the duplicate test egg.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log($"Created a deliberate second inventory copy of egg ID {sourceEggId}. Only one copy may hatch; use this duplicate on a disposable save and discard that save after the check.", LogLevel.Warn);
    }

    private void OnGiveMutagenicPoolCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a Mutagenic Pool.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a Mutagenic Pool to the currently loaded save, use: slime_ranching_give_mutagenic_pool", LogLevel.Warn);
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

    private void OnGiveSlimePrinterCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a Slime Printer.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a Slime Printer to the currently loaded save, use: slime_ranching_give_slime_printer", LogLevel.Warn);
            return;
        }

        Item printer = ItemRegistry.Create(SlimePrinterContent.QualifiedItemId);
        if (!Game1.player.couldInventoryAcceptThisItem(printer) || !Game1.player.addItemToInventoryBool(printer))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Printer, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Printer. Place it inside the Slime Daycare; its provisional recipe needs Broken Glasses and vanilla Slime.", LogLevel.Info);
    }

    private void OnGiveTunnelCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a Slime Tunnel.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a Slime Tunnel to the currently loaded save, use: slime_ranching_give_tunnel", LogLevel.Warn);
            return;
        }

        Item tunnel = ItemRegistry.Create(FurnitureContent.SlimeTunnelQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(tunnel) || !Game1.player.addItemToInventoryBool(tunnel))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Tunnel, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Tunnel. Place it in the Slime Daycare for nearby idle Domesticated Slimes to crawl through.", LogLevel.Info);
    }

    private void OnGiveHeatedStoneCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a Heated Slime Stone.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a Heated Slime Stone to the currently loaded save, use: slime_ranching_give_heated_stone", LogLevel.Warn);
            return;
        }

        Item stone = ItemRegistry.Create(FurnitureContent.HeatedSlimeStoneQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(stone) || !Game1.player.addItemToInventoryBool(stone))
        {
            this.Monitor.Log("Make room in your inventory for the Heated Slime Stone, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Heated Slime Stone. Place it in the Slime Daycare for nearby idle Domesticated Slimes to rest beside it.", LogLevel.Info);
    }

    private void OnGivePondCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a Slime Pond.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a Slime Pond to the currently loaded save, use: slime_ranching_give_pond", LogLevel.Warn);
            return;
        }

        Item pond = ItemRegistry.Create(FurnitureContent.SlimePondQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(pond) || !Game1.player.addItemToInventoryBool(pond))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Pond, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Pond. Place it in the Slime Daycare for nearby idle Domesticated Slimes to bob in the water.", LogLevel.Info);
    }

    private void OnGiveSwingCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a Slime Swing.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a Slime Swing to the currently loaded save, use: slime_ranching_give_swing", LogLevel.Warn);
            return;
        }

        Item swing = ItemRegistry.Create(FurnitureContent.SlimeSwingQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(swing) || !Game1.player.addItemToInventoryBool(swing))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Swing, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Swing. Place it in the Slime Daycare for nearby idle Domesticated Slimes to sway on.", LogLevel.Info);
    }

    private void OnGiveFairyLightsCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting Slime Fairy Lights.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give Slime Fairy Lights to the currently loaded save, use: slime_ranching_give_fairy_lights", LogLevel.Warn);
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

    private void OnGiveWashingMachineCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a Slime Washing Machine.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a Slime Washing Machine to the currently loaded save, use: slime_ranching_give_washing_machine", LogLevel.Warn);
            return;
        }

        Item machine = ItemRegistry.Create(FurnitureContent.SlimeWashingMachineQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(machine) || !Game1.player.addItemToInventoryBool(machine))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Washing Machine, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Washing Machine for sprite and placement testing. It has no washing interaction or slime effect yet.", LogLevel.Info);
    }

    private void OnSetFurnitureSlotTestCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady || this.registry is null)
        {
            this.Monitor.Log("Load a single-player save before setting a test furniture-slot assignment.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 5 || args[0] != saveName
            || !int.TryParse(args[3], out int tileX) || !int.TryParse(args[4], out int tileY))
        {
            this.Monitor.Log("Usage: slime_ranching_set_furniture_slot_test <slot-id> <furniture-key> <tile-x> <tile-y>", LogLevel.Warn);
            return;
        }

        if (Game1.currentLocation is not AnimalHouse daycare
            || !BuildingContent.IsSlimeDaycare(daycare.ParentBuilding?.buildingType.Value)
            || !ReferenceEquals(daycare.ParentBuilding?.GetIndoors(), daycare))
        {
            this.Monitor.Log("Enter the Slime Daycare before setting a test furniture-slot assignment.", LogLevel.Warn);
            return;
        }

        if (tileX < 0 || tileY < 0
            || tileX >= daycare.map.Layers[0].LayerWidth
            || tileY >= daycare.map.Layers[0].LayerHeight)
        {
            this.Monitor.Log("Choose a nonnegative tile inside the current Daycare room.", LogLevel.Warn);
            return;
        }

        if (DaycareFeedingService.IsFeedingTroughTile(daycare, tileX, tileY))
        {
            this.Monitor.Log("That tile belongs to the fixed Daycare Feeding Trough and cannot be assigned to furniture.", LogLevel.Warn);
            return;
        }

        Vector2 slotTile = new(tileX, tileY);
        if (!daycare.isTilePassable(slotTile))
        {
            this.Monitor.Log("Choose a passable floor tile inside the Daycare; wall and fixture tiles cannot anchor a furniture slot.", LogLevel.Warn);
            return;
        }

        if (daycare.Objects.TryGetValue(slotTile, out StardewValley.Object? tileObject)
            && !FurnitureSlotCatalog.TryGetFurniture(tileObject.QualifiedItemId, out _))
        {
            this.Monitor.Log("That tile is occupied by a non-catalog object. Choose a clear furniture tile.", LogLevel.Warn);
            return;
        }

        if (!FurnitureSlotCatalog.TryGetFurnitureByCommandKey(args[2], out DaycareFurnitureRecipeListing furniture))
        {
            string keys = string.Join(", ", FurnitureSlotCatalog.GetRecipeListings().Select(FurnitureSlotCatalog.GetCommandKey));
            this.Monitor.Log($"Unknown furniture key '{args[2]}'. Valid keys: {keys}.", LogLevel.Warn);
            return;
        }

        string buildingId = daycare.ParentBuilding?.id.Value.ToString("N") ?? string.Empty;
        var assignment = new DaycareFurnitureSlotRecord
        {
            DaycareBuildingId = buildingId,
            SlotId = args[1],
            FurnitureQualifiedItemId = furniture.QualifiedItemId,
            TileX = tileX,
            TileY = tileY
        };
        if (!this.registry.TryAssignFurnitureSlot(assignment, BuildingContent.GetUpgradeTier(daycare.ParentBuilding?.buildingType.Value), out string error))
        {
            this.Monitor.Log($"Couldn't save the furniture-slot test assignment: {error}", LogLevel.Warn);
            return;
        }

        FurnitureSlotProtection.Apply(daycare, this.registry);
        string displayName = ItemRegistry.Create(furniture.QualifiedItemId).DisplayName;
        this.Monitor.Log($"Saved test assignment {args[1]}: {displayName} at tile ({tileX}, {tileY}). No item was consumed or placed; clear it with slime_ranching_clear_furniture_slot_test {args[1]}.", LogLevel.Info);
    }

    private void OnClearFurnitureSlotTestCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady || this.registry is null)
        {
            this.Monitor.Log("Load a single-player save before clearing a test furniture-slot assignment.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 2 || args[0] != saveName)
        {
            this.Monitor.Log("Usage: slime_ranching_clear_furniture_slot_test <slot-id>", LogLevel.Warn);
            return;
        }

        if (Game1.currentLocation is not AnimalHouse daycare
            || !BuildingContent.IsSlimeDaycare(daycare.ParentBuilding?.buildingType.Value)
            || !ReferenceEquals(daycare.ParentBuilding?.GetIndoors(), daycare))
        {
            this.Monitor.Log("Enter the Slime Daycare before clearing a test furniture-slot assignment.", LogLevel.Warn);
            return;
        }

        string buildingId = daycare.ParentBuilding?.id.Value.ToString("N") ?? string.Empty;
        if (!this.registry.RemoveFurnitureSlot(buildingId, args[1]))
        {
            this.Monitor.Log($"No saved furniture-slot assignment exists for {args[1]} in this Daycare.", LogLevel.Warn);
            return;
        }

        FurnitureSlotProtection.Apply(daycare, this.registry);
        this.Monitor.Log($"Cleared test furniture-slot assignment {args[1]}. No in-room object was removed.", LogLevel.Info);
    }

    private void OnGiveSeesawCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a Slime Seesaw.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a Slime Seesaw to the currently loaded save, use: slime_ranching_give_seesaw", LogLevel.Warn);
            return;
        }

        Item seesaw = ItemRegistry.Create(FurnitureContent.SlimeSeesawQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(seesaw) || !Game1.player.addItemToInventoryBool(seesaw))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Seesaw, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Seesaw. Place it in the Slime Daycare; two nearby idle Domesticated Slimes can play together.", LogLevel.Info);
    }

    private void OnGiveBallPitCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a Slime Ball Pit.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a Slime Ball Pit to the currently loaded save, use: slime_ranching_give_ball_pit", LogLevel.Warn);
            return;
        }

        Item ballPit = ItemRegistry.Create(FurnitureContent.SlimeBallPitQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(ballPit) || !Game1.player.addItemToInventoryBool(ballPit))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Ball Pit, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Ball Pit. Place it in the Daycare for nearby idle Domesticated Slimes to dip into and pop back out.", LogLevel.Info);
    }

    private void OnGiveLampCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady)
        {
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before requesting a Slime Lamp.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("To give a Slime Lamp to the currently loaded save, use: slime_ranching_give_lamp", LogLevel.Warn);
            return;
        }

        Item lamp = ItemRegistry.Create(FurnitureContent.SlimeLampQualifiedId);
        if (!Game1.player.couldInventoryAcceptThisItem(lamp) || !Game1.player.addItemToInventoryBool(lamp))
        {
            this.Monitor.Log("Make room in your inventory for the Slime Lamp, then try again.", LogLevel.Warn);
            return;
        }

        this.Monitor.Log("Added one Slime Lamp. Place it in the Slime Daycare; nearby eligible Domesticated Slimes can gather around its light.", LogLevel.Info);
    }

    private bool StartSlimePrinterBatch(string machineKey, Building daycare)
    {
        SlimePrinterRules rules = SlimePrinterRules.FromConfig(this.config);
        if (BuildingContent.GetUpgradeTier(daycare.buildingType.Value) < 3
            || this.registry is null || this.registry.GetSlimePrinterJob(machineKey) is not null
            || CountInventoryItem("(O)170") < 1 || CountInventoryItem("(O)766") < rules.FuelCost)
            return false;

        SlimeRegistry registry = this.registry;
        List<(int Slot, Item Item, int OldStack)> removed = ConsumeInventoryItems("(O)170", 1)
            .Concat(ConsumeInventoryItems("(O)766", rules.FuelCost)).ToList();
        SlimePrinterTransactionRecord? transaction = registry.StartSlimePrinterJob(machineKey, Game1.Date.TotalDays, rules.DurationDays, rules.FuelCost);
        if (transaction is null)
        {
            RestoreInventoryItems(removed);
            return false;
        }

        string receiptKey = SlimePrinterTransactionReceiptPrefix + transaction.Id;
        Game1.player.modData[receiptKey] = System.Text.Json.JsonSerializer.Serialize(transaction);
        if (this.SaveCurrentSaveDataNow())
            return true;

        registry.ResolveSlimePrinterTransaction(transaction.Id, nativeSaveCommitted: false);
        Game1.player.modData.Remove(receiptKey);
        RestoreInventoryItems(removed);
        if (!this.SaveCurrentSaveDataNow())
            this.Monitor.Log("Could not persist the Slime Printer start rollback; inputs and the prior machine state were restored in memory.", LogLevel.Error);
        return false;
    }

    private bool CollectSlimePrinterBatch(string machineKey)
    {
        SlimeRegistry? registry = this.registry;
        SlimePrinterJobRecord? job = registry?.GetSlimePrinterJob(machineKey);
        if (registry is null || job is null || Game1.Date.TotalDays < job.ReadyOnDay)
            return false;

        Item output = ItemRegistry.Create(job.OutputItemId);
        if (!Game1.player.couldInventoryAcceptThisItem(output))
            return false;

        (Item? Item, int Stack)[] inventorySnapshot = Game1.player.Items
            .Select(item => (Item: (Item?)item, Stack: item?.Stack ?? 0))
            .ToArray();
        SlimePrinterTransactionRecord? transaction = registry.BeginSlimePrinterCollection(machineKey);
        if (transaction is null)
            return false;
        if (!Game1.player.addItemToInventoryBool(output))
        {
            registry.ResolveSlimePrinterTransaction(transaction.Id, nativeSaveCommitted: false);
            return false;
        }

        string receiptKey = SlimePrinterTransactionReceiptPrefix + transaction.Id;
        Game1.player.modData[receiptKey] = System.Text.Json.JsonSerializer.Serialize(transaction);
        if (this.SaveCurrentSaveDataNow())
            return true;

        registry.ResolveSlimePrinterTransaction(transaction.Id, nativeSaveCommitted: false);
        Game1.player.modData.Remove(receiptKey);
        RestoreInventorySnapshot(inventorySnapshot);
        if (!this.SaveCurrentSaveDataNow())
            this.Monitor.Log("Could not persist the Slime Printer collection rollback; the output and ready batch were restored in memory.", LogLevel.Error);
        return false;
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
            this.Monitor.Log("Load a single-player save with available Domesticated Slime save data before creating a backup.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("Usage: slime_ranching_backup_external_data", LogLevel.Warn);
            return;
        }

        try
        {
            string? path = this.stateStore!.CreateExternalDataBackup();
            if (path is null)
            {
                this.Monitor.Log("There is no external mod data to back up for this save yet.", LogLevel.Warn);
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
            this.Monitor.Log("Domesticated Slime save data is unavailable for this save; see earlier log errors.", LogLevel.Warn);
            return;
        }

        SlimeRegistry loadedRegistry = this.registry!;
        if (args.Length > 1 || (args.Length == 1 && !long.TryParse(args[0], out _)))
        {
            this.Monitor.Log("Usage: slime_ranching_inspect [Domesticated Slime animal ID]", LogLevel.Warn);
            return;
        }

        string schemaVersion = this.stateStore!.LoadedSchemaVersion?.ToString() ?? "not written yet";
        this.Monitor.Log($"SMAPI save-data schema: {schemaVersion}; target for this build: {SlimeSaveData.CurrentSchemaVersion}.", LogLevel.Info);

        if (args.Length == 1)
        {
            long animalId = long.Parse(args[0]);
            if (!loadedRegistry.TryGetSlimeRecord(animalId, out SlimeRecord record))
            {
                this.Monitor.Log($"FarmAnimal ID {animalId} has no active Domesticated Slime genome record in this save.", LogLevel.Warn);
                return;
            }

            SlimeGenome genome = record.Genome;
            string species = genome.SecondarySpecies is null ? genome.PrimarySpecies : $"{genome.PrimarySpecies} / {genome.SecondarySpecies}";
            string mutation = genome.MutationTrait ?? "none";
            string likedFood = FoodLikeCatalog.GetDisplayName(genome.LikedFoodItemId);
            FarmAnimal? resident = GetFarmAnimals().FirstOrDefault(animal => animal.myID.Value == animalId);
            int age = record.AgeClockInitialized ? record.AgeDays : resident?.age.Value ?? 0;
            string residentStatus = SlimeRegistry.GetResidentStatusLabel(loadedRegistry.GetResidentStatus(animalId));
            this.Monitor.Log($"Domesticated Slime {animalId}: genome {genome.Id:N}, {species}, color {genome.VisualColor}, size {genome.Size}, shiny {genome.IsShiny}, rarity {genome.RarityTier}, mutation {mutation}.", LogLevel.Info);
            this.Monitor.Log($"Generation {genome.Generation}; abilities [{string.Join(", ", genome.AbilityIds)}]; likes {likedFood}; age {age}/{FarmAnimalContent.DomesticatedSlimeDaysToMature} days; friendship {resident?.friendshipTowardFarmer.Value ?? 0}/1000; happiness {resident?.happiness.Value ?? 0}/255; fullness {resident?.fullness.Value ?? 0}/255; Pool pity {loadedRegistry.GetMutagenicPoolFailures(animalId)}, contribution {loadedRegistry.GetMutagenicPoolContribution(animalId):P0}; assignment {residentStatus}.", LogLevel.Info);
            int activitiesToday = record.EnrichmentActivityDay == Game1.Date.TotalDays ? record.EnrichmentActivitiesToday : 0;
            string nextActivityCheck;
            if (record.EnrichmentActivityDay != Game1.Date.TotalDays)
                nextActivityCheck = "not initialized today";
            else if (activitiesToday >= 4)
                nextActivityCheck = "daily limit reached";
            else if (record.NextEnrichmentCheckMinute < 0)
                nextActivityCheck = "not scheduled";
            else if (record.NextEnrichmentCheckMinute > 26 * 60)
                nextActivityCheck = "next day after schedule reset";
            else
                nextActivityCheck = FormatActivityClock(record.NextEnrichmentCheckMinute);
            string activeActivity = this.trampolineInteractions.GetActiveActivitySummary(animalId) ?? "none";
            this.Monitor.Log($"Enrichment today: {activitiesToday}/4 activities; next roaming check {nextActivityCheck}; live activity {activeActivity}.", LogLevel.Info);
            return;
        }

        if (Game1.currentLocation is SlimeHutch hutch)
        {
            int slimeCount = hutch.characters.OfType<GreenSlime>().Count();
            this.Monitor.Log($"This vanilla Slime Hutch contains {slimeCount} GreenSlime monster(s), all left unchanged. Domesticated Slimes live in the separate Slime Daycare; save data contains {loadedRegistry.Count} Domesticated Slime record(s).", LogLevel.Info);
            return;
        }

        if (Game1.currentLocation is StardewValley.AnimalHouse animalHouse)
        {
            bool isDaycare = BuildingContent.IsSlimeDaycare(animalHouse.ParentBuilding?.buildingType.Value);
            FarmAnimal[] residents = animalHouse.animals.Values
                .Where(animal => animal.type.Value == FarmAnimalContent.DomesticatedSlimeTypeId)
                .ToArray();
            string machinePrefix = animalHouse.NameOrUniqueName + ":";
            BreedingPairRecord[] roomBreedingPairs = loadedRegistry.BreedingPairs
                .Where(pair => pair.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal))
                .ToArray();
            int activeCycles = roomBreedingPairs.Count(pair => pair.CycleReadyOnDay > 0 && pair.OutputEggItemId is null);
            int readyEggs = roomBreedingPairs.Count(pair => pair.OutputEggItemId is not null);
            int activeIncubations = loadedRegistry.Incubations.Count(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal));
            int activeMutations = loadedRegistry.MutagenicAttempts.Count(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal));
            int activeSlimePrinterBatches = loadedRegistry.SlimePrinterJobs.Count(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal));
            RareLabRecord[] roomRareLabs = isDaycare
                ? loadedRegistry.RareLabs.Where(lab => lab.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal)).ToArray()
                : Array.Empty<RareLabRecord>();
            Chest[] roomCryoLabs = isDaycare
                ? animalHouse.Objects.Values.OfType<Chest>().Where(EggCryoLabContent.IsEggCryoLab).ToArray()
                : Array.Empty<Chest>();
            KeyValuePair<Vector2, StardewValley.Object>[] roomIncubators = isDaycare
                ? animalHouse.Objects.Pairs
                    .Where(pair => pair.Value.QualifiedItemId == RanchingItemContent.IncubatorQualifiedId)
                    .OrderBy(pair => pair.Key.Y)
                    .ThenBy(pair => pair.Key.X)
                    .ToArray()
                : Array.Empty<KeyValuePair<Vector2, StardewValley.Object>>();
            string[] roomIncubatorMachineKeys = roomIncubators
                .Select(pair => $"{animalHouse.NameOrUniqueName}:{(int)pair.Key.X},{(int)pair.Key.Y}")
                .ToArray();
            int mutatedResidents = residents.Count(animal => loadedRegistry.GetMutationTrait(animal.myID.Value) is not null);
            string residentStatuses = string.Join(", ", Enum.GetValues<SlimeResidentStatus>()
                .Select(status => (Label: SlimeRegistry.GetResidentStatusLabel(status), Count: residents.Count(animal => loadedRegistry.GetResidentStatus(animal.myID.Value) == status)))
                .Where(status => status.Count > 0)
                .Select(status => $"{status.Label}: {status.Count}"));
            this.Monitor.Log($"This animal house contains {residents.Length} Domesticated Slime(s), including {mutatedResidents} with mutation traits. Save data contains {loadedRegistry.Count} Domesticated Slime record(s).", LogLevel.Info);
            if (isDaycare)
            {
                this.Monitor.Log($"Daycare living residents: {animalHouse.animalsThatLiveHere.Count}/{animalHouse.animalLimit.Value} capacity.", LogLevel.Info);
                bool nativeAutoFeed = animalHouse.HasMapPropertyWithValue("AutoFeed");
                IReadOnlyList<Point> feedingTiles = DaycareFeedingService.GetConfiguredSlots(animalHouse);
                int activeFeedingTiles = DaycareFeedingService.GetActiveTroughSlotCount(animalHouse);
                int hayAtTrough = DaycareFeedingService.GetHayCountOnTroughs(animalHouse);
                string feedingTileCoordinates = feedingTiles.Count == 0
                    ? "none"
                    : string.Join("; ", feedingTiles.Select(tile => $"{tile.X},{tile.Y}"));
                this.Monitor.Log($"Daycare feeding prototype: native hay auto-feed {(nativeAutoFeed ? "enabled" : "not enabled")}; {feedingTiles.Count}/{animalHouse.animalLimit.Value} reserved slot(s), {activeFeedingTiles}/{animalHouse.animalsThatLiveHere.Count} active ration slot(s) for residents; {hayAtTrough} hay ration(s) currently placed.", LogLevel.Info);
                this.Monitor.Log($"Daycare Feeding Trough tile coordinates (x,y): {feedingTileCoordinates}.", LogLevel.Info);
                string buildingId = animalHouse.ParentBuilding?.id.Value.ToString("N") ?? string.Empty;
                IReadOnlyList<DaycareFurnitureSlotRecord> furnitureSlots = loadedRegistry.GetFurnitureSlots(buildingId);
                int tier = BuildingContent.GetUpgradeTier(animalHouse.ParentBuilding?.buildingType.Value);
                this.Monitor.Log($"Daycare furniture slots: {FurnitureSlotCatalog.GetSlotCountAtTier(tier)}/15 unlocked; {furnitureSlots.Count} assigned.", LogLevel.Info);
                IReadOnlyList<DaycareFurnitureSlotDefinition> unlockedSlots = FurnitureSlotCatalog.GetSlotsUnlockedAtTier(tier);
                var unlockedSlotIds = unlockedSlots.Select(slot => slot.SlotId).ToHashSet(StringComparer.Ordinal);
                var assignedById = furnitureSlots.ToDictionary(slot => slot.SlotId, StringComparer.Ordinal);
                foreach (DaycareFurnitureSlotDefinition definition in unlockedSlots)
                {
                    if (!assignedById.TryGetValue(definition.SlotId, out DaycareFurnitureSlotRecord? slot))
                    {
                        this.Monitor.Log($"Furniture slot {definition.SlotId}: empty.", LogLevel.Info);
                        continue;
                    }

                    Vector2 tile = new(slot.TileX, slot.TileY);
                    bool hasObject = animalHouse.Objects.TryGetValue(tile, out StardewValley.Object? placed);
                    bool matches = hasObject && placed!.QualifiedItemId == slot.FurnitureQualifiedItemId;
                    bool protectedFromTools = matches && placed!.fragility.Value == StardewValley.Object.fragility_Indestructable;
                    string placementStatus = !hasObject
                        ? "object missing"
                        : matches
                            ? $"placed object matches; native protection {(protectedFromTools ? "on" : "off")}"
                            : $"tile occupied by {placed!.QualifiedItemId}";
                    this.Monitor.Log($"Furniture {slot.SlotId}: {slot.FurnitureQualifiedItemId} at tile {slot.TileX},{slot.TileY}; {placementStatus}.", LogLevel.Info);
                }

                foreach (DaycareFurnitureSlotRecord lockedSlot in furnitureSlots.Where(slot => !unlockedSlotIds.Contains(slot.SlotId)))
                    this.Monitor.Log($"Furniture slot {lockedSlot.SlotId}: saved assignment is locked at the current Daycare tier.", LogLevel.Warn);
            }
            this.Monitor.Log($"Domesticated Slime assignment status: {residentStatuses}.", LogLevel.Info);
            this.Monitor.Log($"Jobs in this room: {activeCycles} breeder cycle(s), {readyEggs} ready egg(s), {activeIncubations} incubation(s), {activeMutations} Mutagenic Pool attempt(s), {activeSlimePrinterBatches} Slime Printer batch(es).", LogLevel.Info);
            if (isDaycare)
            {
                this.Monitor.Log($"Slime Incubators placed in this Daycare: {roomIncubatorMachineKeys.Length}.", LogLevel.Info);
                this.Monitor.Log($"Egg CryoLab tester chests: {roomCryoLabs.Length}; stored Domesticated Slime Eggs: {roomCryoLabs.Sum(chest => chest.GetItemsForPlayer().Count(item => item is not null))}/36 slots each.", LogLevel.Info);
                var storedEggIds = new HashSet<string>(StringComparer.Ordinal);
                for (int chestIndex = 0; chestIndex < roomCryoLabs.Length; chestIndex++)
                {
                    IList<Item> chestItems = roomCryoLabs[chestIndex].GetItemsForPlayer();
                    for (int slot = 0; slot < chestItems.Count; slot++)
                    {
                        Item? item = chestItems[slot];
                        if (item is null)
                            continue;

                        if (!SlimeEggContent.TryReadRecord(item, out SlimeEggRecord egg)
                            || !SlimeEggContent.TryGetEggId(item, out string eggId))
                        {
                            this.Monitor.Log($"Egg CryoLab {chestIndex + 1}, slot {slot + 1}: invalid or non-Ranch item {item.QualifiedItemId}; stack size {item.Stack}.", LogLevel.Info);
                            continue;
                        }

                        bool duplicate = !storedEggIds.Add(eggId);
                        bool recordMatches = loadedRegistry.TryGetEgg(eggId, out SlimeEggRecord registeredEgg)
                            && registeredEgg.Genome.Id == egg.Genome.Id;
                        string stackStatus = item.Stack == 1 ? "stack size 1" : $"INVALID stack size {item.Stack} (unique eggs must be 1)";
                        this.Monitor.Log($"Egg CryoLab {chestIndex + 1}, slot {slot + 1}: egg ID {eggId}; {egg.Genome.PrimarySpecies}; size {egg.Genome.Size}; rarity {egg.Genome.RarityTier}; {stackStatus}; registry record {(recordMatches ? "matches" : "missing or mismatched")}; incubator eligible {loadedRegistry.CanIncubate(eggId)}; duplicate ID in this Daycare {duplicate}.", LogLevel.Info);
                    }
                }

                var inventoryEggIds = new HashSet<string>(StringComparer.Ordinal);
                int inventoryEggCount = 0;
                int inventoryEggStackCount = 0;
                int validInventoryEggCount = 0;
                for (int slot = 0; slot < Game1.player.Items.Count; slot++)
                {
                    Item? item = Game1.player.Items[slot];
                    if (item is null
                        || (!item.ItemId.StartsWith(SlimeEggContent.ItemIdPrefix, StringComparison.Ordinal)
                            && !item.modData.ContainsKey(SlimeEggContent.EggIdModDataKey)
                            && !item.modData.ContainsKey(SlimeEggContent.EggRecordModDataKey)))
                        continue;

                    inventoryEggCount++;
                    inventoryEggStackCount += item.Stack;
                    string stackStatus = item.Stack == 1 ? "stack size 1" : $"INVALID stack size {item.Stack} (unique eggs must be 1)";
                    if (!SlimeEggContent.TryReadRecord(item, out SlimeEggRecord egg)
                        || !SlimeEggContent.TryGetEggId(item, out string eggId))
                    {
                        this.Monitor.Log($"Domesticated Slime Egg inventory slot {slot + 1}: invalid identity or genome metadata; item ID {item.QualifiedItemId}; {stackStatus}.", LogLevel.Info);
                        continue;
                    }

                    validInventoryEggCount++;
                    bool duplicate = !inventoryEggIds.Add(eggId);
                    bool recordMatches = loadedRegistry.TryGetEgg(eggId, out SlimeEggRecord registeredEgg)
                        && registeredEgg.Genome.Id == egg.Genome.Id;
                    bool storedInCryoLab = storedEggIds.Contains(eggId);
                    string species = egg.Genome.SecondarySpecies is null
                        ? egg.Genome.PrimarySpecies
                        : $"{egg.Genome.PrimarySpecies} / {egg.Genome.SecondarySpecies}";
                    this.Monitor.Log($"Domesticated Slime Egg inventory slot {slot + 1}: egg ID {eggId}; genome {egg.Genome.Id:N}; {species}; color {egg.Genome.VisualColor}; size {egg.Genome.Size}; rarity {egg.Genome.RarityTier}; {stackStatus}; registry record {(recordMatches ? "matches" : "missing or mismatched")}; incubator eligible {loadedRegistry.CanIncubate(eggId)}; duplicate ID in inventory {duplicate}; also stored in this Daycare's Egg CryoLab {storedInCryoLab}.", LogLevel.Info);
                }

                this.Monitor.Log($"Domesticated Slime Eggs in inventory: {inventoryEggCount} slot(s), {inventoryEggStackCount} total stack unit(s); {validInventoryEggCount} item(s) with valid metadata; {inventoryEggIds.Count} distinct valid ID(s).", LogLevel.Info);
                this.Monitor.Log($"Rare Labs saved in this Daycare: {roomRareLabs.Length}.", LogLevel.Info);
                var placedIncubatorKeys = new HashSet<string>(roomIncubatorMachineKeys, StringComparer.Ordinal);
                for (int index = 0; index < roomIncubators.Length; index++)
                {
                    KeyValuePair<Vector2, StardewValley.Object> incubator = roomIncubators[index];
                    string machineKey = roomIncubatorMachineKeys[index];
                    string location = $"Incubator {index + 1} at tile {(int)incubator.Key.X},{(int)incubator.Key.Y}";
                    RareLabRecord? lab = loadedRegistry.GetRareLab(machineKey);
                    if (lab is null)
                    {
                        this.Monitor.Log($"{location} Rare Lab [{machineKey}]: locked; no saved lab record; base 0%, pity 0%, contribution 0%, total 0%.", LogLevel.Info);
                        continue;
                    }

                    int totalChance = loadedRegistry.GetRareLabChancePoints(lab.MachineKey);
                    this.Monitor.Log($"{location} Rare Lab [{lab.MachineKey}]: {(lab.IsUnlocked ? "unlocked" : "locked")}; base {lab.BaseChancePoints}%, pity {lab.PityPoints}%, contribution {lab.ContributionPoints}%, total {totalChance}%; catalyst {lab.CatalystSpecies ?? "none"}.", LogLevel.Info);
                }

                foreach (RareLabRecord lab in roomRareLabs.Where(lab => !placedIncubatorKeys.Contains(lab.MachineKey)))
                {
                    int totalChance = loadedRegistry.GetRareLabChancePoints(lab.MachineKey);
                    this.Monitor.Log($"Saved Rare Lab [{lab.MachineKey}] has no incubator at its recorded tile; {(lab.IsUnlocked ? "unlocked" : "locked")}; base {lab.BaseChancePoints}%, pity {lab.PityPoints}%, contribution {lab.ContributionPoints}%, total {totalChance}%; catalyst {lab.CatalystSpecies ?? "none"}.", LogLevel.Info);
                }
            }

            foreach (BreedingPairRecord pair in roomBreedingPairs)
            {
                string status;
                if (pair.OutputEggItemId is string outputEggId)
                {
                    if (loadedRegistry.TryGetEgg(outputEggId, out SlimeEggRecord outputEgg))
                    {
                        SlimeGenome genome = outputEgg.Genome;
                        string species = genome.SecondarySpecies is null
                            ? genome.PrimarySpecies
                            : $"{genome.PrimarySpecies} / {genome.SecondarySpecies}";
                        status = $"egg ready; egg ID {outputEggId}; genome {genome.Id:N}; {species}; color {genome.VisualColor}; size {genome.Size}; rarity {genome.RarityTier}";
                    }
                    else
                    {
                        status = $"egg ready; egg ID {outputEggId}; genome record missing";
                    }
                }
                else
                {
                    status = pair.CycleReadyOnDay > Game1.Date.TotalDays
                        ? $"cycle ready day {pair.CycleReadyOnDay}"
                        : pair.CycleReadyOnDay > 0 ? "cycle ready to collect" : "parents reserved; no active cycle";
                }

                this.Monitor.Log($"Breeder [{pair.MachineKey}]: parents {pair.ParentAId}/{pair.ParentBId}; {status}.", LogLevel.Info);
            }

            foreach (IncubationRecord job in loadedRegistry.Incubations.Where(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal)))
            {
                string outcome = !job.RareOutcomeResolved
                    ? "outcome pending"
                    : job.RareOffspringProduced
                        ? $"fixed rare {job.HatchGenome?.PrimarySpecies ?? "unknown"}"
                        : "fixed regular offspring";
                SlimeGenome? eggGenome = job.EggGenome;
                if (eggGenome is null && loadedRegistry.TryGetEgg(job.EggItemId, out SlimeEggRecord savedEgg))
                    eggGenome = savedEgg.Genome;
                SlimeGenome? hatchGenome = job.RareOutcomeResolved ? job.HatchGenome ?? eggGenome : null;
                string hatchGenomeStatus = job.RareOutcomeResolved ? FormatGenomeSummary(hatchGenome) : "not fixed";
                this.Monitor.Log($"Incubator [{job.MachineKey}]: egg ID {job.EggItemId}; ready day {job.ReadyOnDay}; {outcome}; egg genome [{FormatGenomeSummary(eggGenome)}]; hatch genome [{hatchGenomeStatus}]; reserved hatch ID {job.HatchAnimalId?.ToString() ?? "none"}.", LogLevel.Info);
            }

            foreach (MutagenicAttemptRecord job in loadedRegistry.MutagenicAttempts.Where(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal)))
                this.Monitor.Log($"Mutagenic Pool [{job.MachineKey}]: slime {job.SlimeId}; chance {job.ChanceAtStart:P0}; ready day {job.ReadyOnDay}.", LogLevel.Info);

            foreach (SlimePrinterJobRecord job in loadedRegistry.SlimePrinterJobs.Where(job => job.MachineKey.StartsWith(machinePrefix, StringComparison.Ordinal)))
                this.Monitor.Log($"Slime Printer [{job.MachineKey}]: {job.InputCount} Broken Glasses + {job.SlimeFuelCount} Slime -> {job.OutputCount} Refined Quartz; ready day {job.ReadyOnDay}; notice sent {job.ReadyNotificationSent}.", LogLevel.Info);
            return;
        }

        this.Monitor.Log("Enter the Slime Daycare or a vanilla Slime Hutch to inspect ranching status.", LogLevel.Info);
    }

    private static string FormatActivityClock(int scheduledMinute)
    {
        int hour24 = scheduledMinute / 60;
        int hour = hour24 % 12;
        if (hour == 0)
            hour = 12;
        string meridiem = hour24 < 12 ? "AM" : "PM";
        return $"{hour}:{scheduledMinute % 60:00} {meridiem}";
    }

    private static string FormatGenomeSummary(SlimeGenome? genome)
    {
        if (genome is null)
            return "missing";

        string species = genome.SecondarySpecies is null
            ? genome.PrimarySpecies
            : $"{genome.PrimarySpecies} / {genome.SecondarySpecies}";
        return $"ID {genome.Id:N}; {species}; color {genome.VisualColor}; size {genome.Size}; rarity {genome.RarityTier}";
    }

    private void OnSlimeDaycareQuestStatusCommand(string command, string[] args)
    {
        if (args.Length != 0)
        {
            this.Monitor.Log("Usage: slime_ranching_quest_status", LogLevel.Warn);
            return;
        }

        if (!Context.IsWorldReady || !this.stateReady || this.registry is null)
        {
            this.Monitor.Log("Load a save before inspecting Slime Daycare quest progress.", LogLevel.Info);
            return;
        }

        this.Monitor.Log($"Slime Daycare quest save data schema: {this.stateStore?.LoadedSchemaVersion?.ToString() ?? "not written yet"}; target: {SlimeSaveData.CurrentSchemaVersion}.", LogLevel.Info);
        if (this.registry.SlimeDaycareQuest.MaruRepairQuestAvailableOnDay is int availableOnDay)
            this.Monitor.Log($"Maru repair mission availability day: {availableOnDay}; current day: {Game1.Date.TotalDays}.", LogLevel.Info);

        foreach (string milestone in SlimeDaycareQuestProgression.Milestones)
        {
            string status = SlimeDaycareQuestProgression.IsCompleted(this.registry.SlimeDaycareQuest, milestone)
                ? "complete"
                : SlimeDaycareQuestProgression.PrerequisitesMet(this.registry.SlimeDaycareQuest, milestone)
                    ? "prerequisites met"
                    : "waiting";
            this.Monitor.Log($"Slime Daycare quest milestone [{milestone}]: {status}.", LogLevel.Info);
        }
    }

    private void OnTestMaruRepairCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer || !this.stateReady || this.registry is null)
        {
            this.Monitor.Log("Load a single-player save with available Slime Daycare Quest data before priming the Maru repair test.", LogLevel.Warn);
            return;
        }

        string? saveName = Constants.SaveFolderName;
        if (args.Length != 1 || args[0] != saveName)
        {
            this.Monitor.Log("This test changes quest progress. Use it only on a disposable copy: slime_ranching_test_maru_repair", LogLevel.Warn);
            return;
        }

        bool hasCompletedDaycare = Game1.getFarm().buildings.Any(building =>
            BuildingContent.IsSlimeDaycare(building.buildingType.Value)
            && building.daysOfConstructionLeft.Value <= 0);
        if (!hasCompletedDaycare)
        {
            this.Monitor.Log("Build the Slime Daycare and wait for construction to finish before priming the Maru repair test.", LogLevel.Warn);
            return;
        }

        if (this.registry.IsSlimeDaycareQuestMilestoneCompleted(SlimeDaycareQuestProgression.RobinPoolPrompt))
        {
            this.Monitor.Log("The Robin-to-Maru handoff is already primed or complete in this save.", LogLevel.Info);
            return;
        }

        var previousMilestones = new HashSet<string>(this.registry.SlimeDaycareQuest.CompletedMilestones, StringComparer.Ordinal);
        int? previousAvailabilityDay = this.registry.SlimeDaycareQuest.MaruRepairQuestAvailableOnDay;
        string[] prerequisites =
        {
            SlimeDaycareQuestProgression.LewisConcern,
            SlimeDaycareQuestProgression.WizardFailedPotion,
            SlimeDaycareQuestProgression.MarnieSecret,
            SlimeDaycareQuestProgression.DaycareConstruction
        };

        foreach (string milestone in prerequisites)
        {
            if (this.registry.IsSlimeDaycareQuestMilestoneCompleted(milestone)
                || this.registry.TryCompleteSlimeDaycareQuestMilestone(milestone))
                continue;

            this.registry.SlimeDaycareQuest.CompletedMilestones = previousMilestones;
            this.registry.SlimeDaycareQuest.MaruRepairQuestAvailableOnDay = previousAvailabilityDay;
            this.Monitor.Log($"Couldn't prime the Maru repair test because prerequisite milestone '{milestone}' could not be recorded.", LogLevel.Error);
            return;
        }

        if (!this.SaveCurrentSaveDataNow())
        {
            this.registry.SlimeDaycareQuest.CompletedMilestones = previousMilestones;
            this.registry.SlimeDaycareQuest.MaruRepairQuestAvailableOnDay = previousAvailabilityDay;
            this.Monitor.Log("Couldn't save the Maru repair test setup; the previous in-memory quest progress was restored.", LogLevel.Error);
            return;
        }

        this.Monitor.Log("Maru repair test is primed. The Lewis, Wizard, Marnie, and construction ledger milestones were set on this save; talk to Robin to test the handoff.", LogLevel.Info);
    }

}

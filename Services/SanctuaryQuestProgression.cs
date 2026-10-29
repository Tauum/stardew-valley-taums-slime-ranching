using taums_slime_ranching.Models;

namespace taums_slime_ranching.Services;

/// <summary>Defines durable milestone IDs and prerequisite rules for the Sanctuary introduction.</summary>
internal static class SanctuaryQuestProgression
{
    public const string LewisConcern = "lewis_concern";
    public const string WizardFailedPotion = "wizard_failed_potion";
    public const string MarnieSecret = "marnie_secret";
    public const string DaycareConstruction = "daycare_construction";
    public const string MarnieTutorial = "marnie_tutorial";
    public const string RobinPoolPrompt = "robin_pool_prompt";
    public const string MaruRepairRequested = "maru_repair_requested";
    public const string MaruRepairCompleted = "maru_repair_completed";
    public const string StarterSplit = "starter_split";
    public const string WizardSplitReaction = "wizard_split_reaction";
    public const string FirstBreedingStarted = "first_breeding_started";

    public static IReadOnlyList<string> Milestones { get; } = new[]
    {
        LewisConcern,
        WizardFailedPotion,
        MarnieSecret,
        DaycareConstruction,
        MarnieTutorial,
        RobinPoolPrompt,
        MaruRepairRequested,
        MaruRepairCompleted,
        StarterSplit,
        WizardSplitReaction,
        FirstBreedingStarted
    };

    private static readonly IReadOnlyDictionary<string, string[]> Prerequisites =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [LewisConcern] = Array.Empty<string>(),
            [WizardFailedPotion] = new[] { LewisConcern },
            [MarnieSecret] = new[] { WizardFailedPotion },
            [DaycareConstruction] = new[] { MarnieSecret },
            [MarnieTutorial] = new[] { DaycareConstruction },
            [RobinPoolPrompt] = new[] { DaycareConstruction },
            [MaruRepairRequested] = new[] { RobinPoolPrompt },
            [MaruRepairCompleted] = new[] { MaruRepairRequested },
            [StarterSplit] = new[] { MarnieTutorial },
            [WizardSplitReaction] = new[] { StarterSplit },
            [FirstBreedingStarted] = new[] { WizardSplitReaction }
        };

    public static SanctuaryQuestRecord Normalize(SanctuaryQuestRecord? record)
    {
        var normalized = new SanctuaryQuestRecord();
        normalized.MaruRepairQuestAvailableOnDay = record?.MaruRepairQuestAvailableOnDay;
        if (record?.CompletedMilestones is null)
            return normalized;

        foreach (string? milestone in record.CompletedMilestones)
        {
            if (!string.IsNullOrWhiteSpace(milestone) && Prerequisites.ContainsKey(milestone))
                normalized.CompletedMilestones.Add(milestone);
        }

        // A durable later milestone proves its prerequisites were completed. Restore
        // missing prerequisite flags instead of discarding the player's progress.
        bool added;
        do
        {
            added = false;
            foreach (string milestone in normalized.CompletedMilestones.ToArray())
            {
                foreach (string required in Prerequisites[milestone])
                    added |= normalized.CompletedMilestones.Add(required);
            }
        }
        while (added);

        return normalized;
    }

    public static bool IsCompleted(SanctuaryQuestRecord record, string milestone) =>
        !string.IsNullOrWhiteSpace(milestone)
        && Prerequisites.ContainsKey(milestone)
        && record.CompletedMilestones.Contains(milestone);

    public static bool PrerequisitesMet(SanctuaryQuestRecord record, string milestone) =>
        !string.IsNullOrWhiteSpace(milestone)
        && Prerequisites.TryGetValue(milestone, out string[]? requirements)
        && requirements.All(required => record.CompletedMilestones.Contains(required));

    /// <summary>Mark a milestone once, only after its prerequisite milestones are durable.</summary>
    public static bool TryComplete(SanctuaryQuestRecord record, string milestone)
    {
        if (!PrerequisitesMet(record, milestone))
            return false;

        return record.CompletedMilestones.Add(milestone);
    }

    /// <summary>Recover a milestone proven by a durable vanilla-save transaction receipt.</summary>
    public static bool RecoverFromDurableEvidence(SanctuaryQuestRecord record, string milestone)
    {
        if (!Prerequisites.ContainsKey(milestone) || !record.CompletedMilestones.Add(milestone))
            return false;

        record.CompletedMilestones = Normalize(record).CompletedMilestones;
        return true;
    }
}

using SOD_CityRelations.Config;
using SOD_CityRelations.Models;

namespace SOD_CityRelations.Services;

public sealed class CityRelationsService
{
    private readonly Dictionary<int, CitizenRelationshipProfile> profiles;
    private readonly CityRelationsConfig config;
    private readonly RelationshipPersistenceService persistence;
    private readonly LieDecisionService lieDecisionService;
    private readonly ICityRelationsLogger logger;

    public event Action<CitizenRelationshipSnapshot, string, int, string>? RelationshipChanged;
    public event Action<int, string, string>? MemoryAdded;
    public event Action<LieDecision, ConversationContext>? CitizenLied;
    public event Action<LieDecision, ConversationContext>? CitizenTruthful;

    public CityRelationsService(CityRelationsConfig config, RelationshipPersistenceService persistence, ICityRelationsLogger logger)
    {
        this.config = config;
        this.persistence = persistence;
        this.logger = logger;
        lieDecisionService = new LieDecisionService(config);
        profiles = persistence.Load().ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public CitizenRelationshipProfile GetOrCreateProfile(int citizenId)
    {
        if (!profiles.TryGetValue(citizenId, out var profile))
        {
            profile = new CitizenRelationshipProfile { CitizenId = citizenId };
            profiles[citizenId] = profile;
            logger.Debug($"Created relationship profile for citizen {citizenId}.");
            persistence.QueueSave(profiles);
        }

        return profile;
    }

    public CitizenRelationshipProfile? GetProfile(int citizenId) => profiles.TryGetValue(citizenId, out var profile) ? profile : null;

    public void AddAffinity(int citizenId, int amount, string reason) => AddStat(citizenId, nameof(CitizenRelationshipProfile.Affinity), amount, reason, p => p.Affinity, (p, v) => p.Affinity = v);
    public void AddTrust(int citizenId, int amount, string reason) => AddStat(citizenId, nameof(CitizenRelationshipProfile.Trust), amount, reason, p => p.Trust, (p, v) => p.Trust = v);
    public void AddFear(int citizenId, int amount, string reason) => AddStat(citizenId, nameof(CitizenRelationshipProfile.Fear), amount, reason, p => p.Fear, (p, v) => p.Fear = v);
    public void AddGratitude(int citizenId, int amount, string reason) => AddStat(citizenId, nameof(CitizenRelationshipProfile.Gratitude), amount, reason, p => p.Gratitude, (p, v) => p.Gratitude = v);
    public void AddSuspicion(int citizenId, int amount, string reason) => AddStat(citizenId, nameof(CitizenRelationshipProfile.Suspicion), amount, reason, p => p.Suspicion, (p, v) => p.Suspicion = v);
    public void AddFamiliarity(int citizenId, int amount, string reason) => AddStat(citizenId, nameof(CitizenRelationshipProfile.Familiarity), amount, reason, p => p.Familiarity, (p, v) => p.Familiarity = v);

    public void AddMemory(int citizenId, string memoryKey, string reason)
    {
        if (string.IsNullOrWhiteSpace(memoryKey))
        {
            return;
        }

        var profile = GetOrCreateProfile(citizenId);
        if (profile.MemoryFlags.Add(memoryKey))
        {
            logger.Debug($"Citizen {citizenId} gained memory {memoryKey}: {reason}");
            MemoryAdded?.Invoke(citizenId, memoryKey, reason);
            persistence.QueueSave(profiles);
        }
    }

    public bool HasMemory(int citizenId, string memoryKey) => GetProfile(citizenId)?.MemoryFlags.Contains(memoryKey) == true;

    public void RegisterInteraction(int citizenId, string reason)
    {
        var profile = GetOrCreateProfile(citizenId);
        profile.InteractionCount = Math.Max(0, profile.InteractionCount + 1);
        profile.LastInteractionGameTime = GetBestEffortGameTime();
        if (config.EnableRelationshipChanges)
        {
            AddFamiliarity(citizenId, config.FamiliarityGainPerConversation, reason);
        }

        logger.Debug($"Registered interaction with citizen {citizenId}: {reason}");
        persistence.QueueSave(profiles);
    }

    public RelationshipTier GetRelationshipTier(int citizenId)
    {
        var profile = GetProfile(citizenId);
        return profile == null ? RelationshipTier.Unknown : CalculateTier(profile);
    }

    public float CalculateLieChance(int citizenId, ConversationContext context)
    {
        var profile = GetOrCreateProfile(citizenId);
        return lieDecisionService.CalculateLieChance(profile, context);
    }

    public LieDecision RollShouldLie(int citizenId, ConversationContext context)
    {
        var profile = GetOrCreateProfile(citizenId);
        var tier = CalculateTier(profile);
        var decision = lieDecisionService.Roll(profile, context, tier);
        logger.Debug($"Lie decision citizen={citizenId}, tier={tier}, question={context.QuestionType}, chance={decision.Chance:0.000}, shouldLie={decision.ShouldLie}, response={decision.ResponseType}");

        if (decision.ShouldLie)
        {
            CitizenLied?.Invoke(decision, context);
        }
        else
        {
            CitizenTruthful?.Invoke(decision, context);
        }

        return decision;
    }

    public CitizenRelationshipSnapshot ExportPublicSnapshot(int citizenId)
    {
        var profile = GetOrCreateProfile(citizenId);
        return new CitizenRelationshipSnapshot
        {
            CitizenId = profile.CitizenId,
            Affinity = profile.Affinity,
            Trust = profile.Trust,
            Fear = profile.Fear,
            Gratitude = profile.Gratitude,
            Suspicion = profile.Suspicion,
            Familiarity = profile.Familiarity,
            LastInteractionGameTime = profile.LastInteractionGameTime,
            InteractionCount = profile.InteractionCount,
            MemoryFlags = profile.MemoryFlags.ToArray(),
            LieBias = profile.LieBias,
            HonestyBias = profile.HonestyBias,
            Tier = CalculateTier(profile)
        };
    }

    public void AddRelationshipModifier(int citizenId, string stat, int amount, string reason)
    {
        switch (stat.Trim().ToLowerInvariant())
        {
            case "affinity": AddAffinity(citizenId, amount, reason); break;
            case "trust": AddTrust(citizenId, amount, reason); break;
            case "fear": AddFear(citizenId, amount, reason); break;
            case "gratitude": AddGratitude(citizenId, amount, reason); break;
            case "suspicion": AddSuspicion(citizenId, amount, reason); break;
            case "familiarity": AddFamiliarity(citizenId, amount, reason); break;
            default: logger.Warning($"Ignored unknown relationship stat '{stat}' for citizen {citizenId}."); break;
        }
    }

    public void SaveNow() => persistence.SaveNow(profiles);
    public void FlushSaveIfDue() => persistence.FlushIfDue(profiles);

    private void AddStat(int citizenId, string stat, int amount, string reason, Func<CitizenRelationshipProfile, int> getter, Action<CitizenRelationshipProfile, int> setter)
    {
        if (!config.EnableRelationshipChanges)
        {
            return;
        }

        var profile = GetOrCreateProfile(citizenId);
        var previous = getter(profile);
        var next = Math.Clamp(previous + amount, -100, 100);
        if (previous == next)
        {
            return;
        }

        setter(profile, next);
        logger.Debug($"Citizen {citizenId} {stat} changed {previous}->{next} ({amount:+#;-#;0}): {reason}");
        RelationshipChanged?.Invoke(ExportPublicSnapshot(citizenId), stat, amount, reason);
        persistence.QueueSave(profiles);
    }

    private static RelationshipTier CalculateTier(CitizenRelationshipProfile profile)
    {
        if (profile.Fear >= 60) return RelationshipTier.Afraid;
        if (profile.Suspicion >= 65 && profile.Affinity <= -20) return RelationshipTier.Hostile;
        if (profile.Suspicion >= 45) return RelationshipTier.Suspicious;
        if (profile.Gratitude >= 55 && profile.Trust >= 25) return RelationshipTier.OwesFavor;
        if (profile.Trust >= 60 && profile.Affinity >= 20) return RelationshipTier.Trusted;
        if (profile.Affinity >= 35 && profile.Trust >= 20) return RelationshipTier.Friendly;
        if (profile.Familiarity >= 10 || profile.InteractionCount >= 3) return RelationshipTier.Familiar;
        return RelationshipTier.Unknown;
    }

    private static float GetBestEffortGameTime() => (float)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}

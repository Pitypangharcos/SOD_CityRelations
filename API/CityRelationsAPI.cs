using SOD_CityRelations.Models;
using SOD_CityRelations.Services;

namespace SOD_CityRelations.API;

public static class CityRelationsAPI
{
    private static CityRelationsService? service;

    public static bool IsAvailable => service != null;

    public static event Action<CitizenRelationshipSnapshot, string, int, string>? OnRelationshipChanged;
    public static event Action<int, string, string>? OnMemoryAdded;
    public static event Action<LieDecision, ConversationContext>? OnCitizenLied;
    public static event Action<LieDecision, ConversationContext>? OnCitizenTruthful;

    public static void Initialize(CityRelationsService cityRelationsService)
    {
        service = cityRelationsService;
        service.RelationshipChanged += (snapshot, stat, amount, reason) => OnRelationshipChanged?.Invoke(snapshot, stat, amount, reason);
        service.MemoryAdded += (citizenId, memoryKey, reason) => OnMemoryAdded?.Invoke(citizenId, memoryKey, reason);
        service.CitizenLied += (decision, context) => OnCitizenLied?.Invoke(decision, context);
        service.CitizenTruthful += (decision, context) => OnCitizenTruthful?.Invoke(decision, context);
    }

    public static CitizenRelationshipSnapshot GetRelationship(int citizenId) => RequireService().ExportPublicSnapshot(citizenId);
    public static int GetAffinity(int citizenId) => GetRelationship(citizenId).Affinity;
    public static int GetTrust(int citizenId) => GetRelationship(citizenId).Trust;
    public static int GetFear(int citizenId) => GetRelationship(citizenId).Fear;
    public static int GetSuspicion(int citizenId) => GetRelationship(citizenId).Suspicion;
    public static RelationshipTier GetTier(int citizenId) => RequireService().GetRelationshipTier(citizenId);
    public static void AddMemory(int citizenId, string memoryKey, string sourceMod) => RequireService().AddMemory(citizenId, memoryKey, sourceMod);
    public static void AddRelationshipModifier(int citizenId, string stat, int amount, string sourceMod, string reason) => RequireService().AddRelationshipModifier(citizenId, stat, amount, $"{sourceMod}: {reason}");
    public static float CalculateLieChance(int citizenId, ConversationContext context) => RequireService().CalculateLieChance(citizenId, context);

    private static CityRelationsService RequireService() => service ?? throw new InvalidOperationException("SOD_CityRelations API is not initialized.");
}

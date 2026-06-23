namespace SOD_CityRelations.Models;

public sealed class CitizenRelationshipSnapshot
{
    public int CitizenId { get; init; }
    public int Affinity { get; init; }
    public int Trust { get; init; }
    public int Fear { get; init; }
    public int Gratitude { get; init; }
    public int Suspicion { get; init; }
    public int Familiarity { get; init; }
    public float LastInteractionGameTime { get; init; }
    public int InteractionCount { get; init; }
    public IReadOnlyCollection<string> MemoryFlags { get; init; } = Array.Empty<string>();
    public int LieBias { get; init; }
    public int HonestyBias { get; init; }
    public RelationshipTier Tier { get; init; }
}

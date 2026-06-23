namespace SOD_CityRelations.Models;

public sealed class LieDecision
{
    public int CitizenId { get; init; }
    public float Chance { get; init; }
    public bool ShouldLie { get; init; }
    public LieResponseType ResponseType { get; init; }
    public RelationshipTier Tier { get; init; }
    public string Reason { get; init; } = string.Empty;
}

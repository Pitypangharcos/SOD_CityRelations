using System.Text.Json.Serialization;

namespace SOD_CityRelations.Models;

public sealed class CitizenRelationshipProfile
{
    public int CitizenId { get; set; }
    public int Affinity { get; set; }
    public int Trust { get; set; }
    public int Fear { get; set; }
    public int Gratitude { get; set; }
    public int Suspicion { get; set; }
    public int Familiarity { get; set; }
    public float LastInteractionGameTime { get; set; }
    public int InteractionCount { get; set; }
    public HashSet<string> MemoryFlags { get; set; } = new(StringComparer.Ordinal);
    public int LieBias { get; set; }
    public int HonestyBias { get; set; }

    [JsonIgnore]
    public bool IsEmpty => Affinity == 0 && Trust == 0 && Fear == 0 && Gratitude == 0 &&
                           Suspicion == 0 && Familiarity == 0 && InteractionCount == 0 &&
                           MemoryFlags.Count == 0 && LieBias == 0 && HonestyBias == 0;
}

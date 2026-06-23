namespace SOD_CityRelations.Models;

public sealed class ConversationContext
{
    public int CitizenId { get; set; }
    public QuestionType QuestionType { get; set; } = QuestionType.Generic;
    public string Topic { get; set; } = string.Empty;
    public bool IsSensitiveTopic { get; set; }
    public bool IsCrimeRelated { get; set; }
    public bool IsPersonalQuestion { get; set; }
    public bool PlayerHasThreatenedCitizen { get; set; }
    public bool PlayerHelpedCitizen { get; set; }
    public bool BaseGameWouldRevealInfo { get; set; }
    public string DebugSource { get; set; } = string.Empty;
}

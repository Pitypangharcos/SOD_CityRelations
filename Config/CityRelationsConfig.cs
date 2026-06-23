using SOD_CityRelations.Models;

namespace SOD_CityRelations.Config;

public sealed class CityRelationsConfig
{
    public bool EnableMod { get; set; } = true;
    public bool EnableDebugLogging { get; set; }
    public bool EnableRelationshipChanges { get; set; } = true;
    public bool EnableLieSystem { get; set; } = true;
    public bool EnableDialogueManipulation { get; set; }
    public bool ProtectCriticalInfo { get; set; } = true;

    public int FamiliarityGainPerConversation { get; set; } = 1;
    public int TrustGainWhenHelpful { get; set; } = 5;
    public int FearGainWhenThreatened { get; set; } = 10;
    public int SuspicionGainWhenCaughtTrespassing { get; set; } = 10;
    public bool RelationshipDecayEnabled { get; set; }
    public int RelationshipDecayAmountPerDay { get; set; } = 1;

    public float BaseLieChance { get; set; } = 0.08f;
    public float MinLieChance { get; set; } = 0.00f;
    public float MaxLieChance { get; set; } = 0.65f;
    public float SensitiveTopicBonus { get; set; } = 0.10f;
    public float CrimeTopicBonus { get; set; } = 0.15f;
    public float IllegalTopicBonus { get; set; } = 0.20f;
    public float HighFearBonus { get; set; } = 0.15f;
    public float HighSuspicionBonus { get; set; } = 0.15f;
    public float LowTrustBonus { get; set; } = 0.10f;
    public float HighTrustReduction { get; set; } = 0.15f;
    public float HighGratitudeReduction { get; set; } = 0.10f;
    public float HighFamiliarityReduction { get; set; } = 0.05f;
    public FearMode FearMode { get; set; } = FearMode.Mixed;
    public LieSeedMode LieSeedMode { get; set; } = LieSeedMode.Random;

    public bool ExposePublicApi { get; set; } = true;
    public bool AllowReputationIntegration { get; set; } = true;
    public bool AllowHeatIntegration { get; set; } = true;

    public TimeSpan SaveThrottleInterval { get; set; } = TimeSpan.FromSeconds(10);
}

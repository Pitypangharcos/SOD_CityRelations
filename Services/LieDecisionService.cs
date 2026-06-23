using SOD_CityRelations.Config;
using SOD_CityRelations.Models;

namespace SOD_CityRelations.Services;

public sealed class LieDecisionService
{
    private readonly CityRelationsConfig config;
    private readonly Random random = new();

    public LieDecisionService(CityRelationsConfig config)
    {
        this.config = config;
    }

    public float CalculateLieChance(CitizenRelationshipProfile profile, ConversationContext context)
    {
        if (!config.EnableLieSystem)
        {
            return 0f;
        }

        var chance = config.BaseLieChance;

        if (context.IsSensitiveTopic) chance += config.SensitiveTopicBonus;
        if (context.IsCrimeRelated || context.QuestionType == QuestionType.Crime) chance += config.CrimeTopicBonus;
        if (context.QuestionType == QuestionType.IllegalActivity) chance += config.IllegalTopicBonus;
        if (context.PlayerHasThreatenedCitizen || profile.MemoryFlags.Contains("PLAYER_THREATENED_CITIZEN")) chance += config.HighFearBonus;
        if (profile.MemoryFlags.Contains("PLAYER_WAS_CAUGHT_TRESPASSING")) chance += config.HighSuspicionBonus * 0.5f;
        if (context.PlayerHelpedCitizen || profile.MemoryFlags.Contains("PLAYER_HELPED_CITIZEN")) chance -= config.HighGratitudeReduction;
        if (profile.MemoryFlags.Contains("PLAYER_RETURNED_ITEM")) chance -= config.HighGratitudeReduction;

        chance += ScalePositive(profile.Suspicion, config.HighSuspicionBonus);
        chance += ScaleLowTrust(profile.Trust, config.LowTrustBonus);
        chance -= ScalePositive(profile.Trust, config.HighTrustReduction);
        chance -= ScalePositive(profile.Gratitude, config.HighGratitudeReduction);
        chance -= ScalePositive(profile.Familiarity, config.HighFamiliarityReduction);
        chance += (profile.LieBias - profile.HonestyBias) / 100f;

        chance += config.FearMode switch
        {
            FearMode.LieMore => ScalePositive(profile.Fear, config.HighFearBonus),
            FearMode.ComplyMore => -ScalePositive(profile.Fear, config.HighFearBonus),
            _ => context.IsCrimeRelated || context.QuestionType == QuestionType.IllegalActivity
                ? ScalePositive(profile.Fear, config.HighFearBonus)
                : -ScalePositive(profile.Fear, config.HighFearBonus * 0.5f)
        };

        if (config.ProtectCriticalInfo && context.BaseGameWouldRevealInfo && context.IsCrimeRelated)
        {
            chance = Math.Min(chance, 0.20f);
        }

        return Math.Clamp(chance, config.MinLieChance, config.MaxLieChance);
    }

    public LieDecision Roll(CitizenRelationshipProfile profile, ConversationContext context, RelationshipTier tier)
    {
        var chance = CalculateLieChance(profile, context);
        var roll = NextRoll(profile, context);
        var shouldLie = roll < chance;
        return new LieDecision
        {
            CitizenId = profile.CitizenId,
            Chance = chance,
            ShouldLie = shouldLie,
            ResponseType = shouldLie ? SelectResponseType(context) : LieResponseType.None,
            Tier = tier,
            Reason = $"roll={roll:0.000}; source={context.DebugSource}"
        };
    }

    private float NextRoll(CitizenRelationshipProfile profile, ConversationContext context)
    {
        if (config.LieSeedMode == LieSeedMode.Random)
        {
            return (float)random.NextDouble();
        }

        var seedText = config.LieSeedMode == LieSeedMode.StablePerCitizen
            ? profile.CitizenId.ToString()
            : $"{profile.CitizenId}:{context.QuestionType}:{context.Topic}";
        return (StableHash(seedText) % 10000) / 10000f;
    }

    private static LieResponseType SelectResponseType(ConversationContext context) => context.QuestionType switch
    {
        QuestionType.Name => LieResponseType.VagueAnswer,
        QuestionType.Address or QuestionType.Workplace or QuestionType.SeenPerson or QuestionType.SeenItem => LieResponseType.FalseLocation,
        QuestionType.Alibi => LieResponseType.FalseAlibi,
        QuestionType.Crime => LieResponseType.Omission,
        QuestionType.IllegalActivity => LieResponseType.Misdirection,
        _ => LieResponseType.VagueAnswer
    };

    private static float ScalePositive(int value, float maxEffect) => Math.Clamp(value, 0, 100) / 100f * maxEffect;
    private static float ScaleLowTrust(int trust, float maxEffect) => Math.Clamp(-trust, 0, 100) / 100f * maxEffect;

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 23;
            foreach (var character in value)
            {
                hash = hash * 31 + character;
            }

            return Math.Abs(hash);
        }
    }
}

using SOD_CityRelations.Config;
using SOD_CityRelations.Models;
using SOD_CityRelations.Services;

namespace SOD_CityRelations.DevTools;

public static class BackendHarness
{
    public static IReadOnlyList<LieDecision> Run(TextWriter? output = null)
    {
        output ??= Console.Out;

        var baseDirectory = Path.Combine(Path.GetTempPath(), "SOD_CityRelations_BackendHarness");
        if (Directory.Exists(baseDirectory))
        {
            Directory.Delete(baseDirectory, recursive: true);
        }

        var config = new CityRelationsConfig
        {
            EnableDebugLogging = true,
            LieSeedMode = LieSeedMode.StablePerCitizenAndTopic
        };

        var logger = new ConsoleCityRelationsLogger(debugEnabled: true);
        var persistence = new RelationshipPersistenceService(baseDirectory, config.SaveThrottleInterval, logger);
        var service = new CityRelationsService(config, persistence, logger);

        const int citizenId = 1001;
        service.RegisterInteraction(citizenId, "developer harness fake conversation");
        service.AddTrust(citizenId, 25, "developer harness");
        service.AddFear(citizenId, 20, "developer harness");
        service.AddSuspicion(citizenId, 35, "developer harness");

        var contexts = new[]
        {
            new ConversationContext
            {
                CitizenId = citizenId,
                QuestionType = QuestionType.Name,
                Topic = "identity",
                DebugSource = "BackendHarness:name"
            },
            new ConversationContext
            {
                CitizenId = citizenId,
                QuestionType = QuestionType.Crime,
                Topic = "recent murder",
                IsSensitiveTopic = true,
                IsCrimeRelated = true,
                BaseGameWouldRevealInfo = true,
                DebugSource = "BackendHarness:crime"
            },
            new ConversationContext
            {
                CitizenId = citizenId,
                QuestionType = QuestionType.IllegalActivity,
                Topic = "black market item",
                IsSensitiveTopic = true,
                IsCrimeRelated = true,
                IsPersonalQuestion = true,
                DebugSource = "BackendHarness:illegal"
            },
            new ConversationContext
            {
                CitizenId = citizenId,
                QuestionType = QuestionType.Alibi,
                Topic = "whereabouts",
                PlayerHasThreatenedCitizen = true,
                DebugSource = "BackendHarness:alibi-threatened"
            }
        };

        var decisions = new List<LieDecision>();
        output.WriteLine("SOD_CityRelations backend harness");
        output.WriteLine($"Citizen {citizenId} tier: {service.GetRelationshipTier(citizenId)}");

        foreach (var context in contexts)
        {
            var decision = service.RollShouldLie(citizenId, context);
            decisions.Add(decision);
            output.WriteLine($"{context.QuestionType}: chance={decision.Chance:0.000}, shouldLie={decision.ShouldLie}, response={decision.ResponseType}, reason={decision.Reason}");
        }

        service.SaveNow();
        return decisions;
    }
}

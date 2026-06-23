using SOD_CityRelations.Models;
using SOD_CityRelations.Services;

namespace SOD_CityRelations.Patches;

public static class ConversationPatchStubs
{
    public static void LogWouldEvaluateQuestion(CityRelationsService service, int citizenId, QuestionType questionType, string debugSource)
    {
        var context = new ConversationContext
        {
            CitizenId = citizenId,
            QuestionType = questionType,
            DebugSource = debugSource
        };

        service.RollShouldLie(citizenId, context);
    }
}

namespace SOD_CityRelations.Patches.Diagnostics;

internal static class InteractionControllerDiagnosticsPatch
{
    public static void SetCurrentPlayerInteractionPostfix(object __instance, object[] __args)
    {
        if (!DiagnosticPatchContext.Config.EnableInteractionDiagnostics || !DiagnosticPatchContext.RateLimiter.TryAcquire())
        {
            return;
        }

        try
        {
            DiagnosticPatchContext.Logger.Info(
                "[Diagnostics] InteractionController.SetCurrentPlayerInteraction hit; " +
                $"key={SafeGameObjectDescriber.Describe(GetArg(__args, 0))}; " +
                $"newInteractable={SafeGameObjectDescriber.Describe(GetArg(__args, 1))}; " +
                $"newCurrentAction={SafeGameObjectDescriber.Describe(GetArg(__args, 2))}; " +
                $"fpsItem={GetArg(__args, 3) ?? "<null>"}; " +
                $"forcePriority={GetArg(__args, 4) ?? "<null>"}");
        }
        catch (Exception ex)
        {
            DiagnosticPatchContext.Logger.Error("InteractionController.SetCurrentPlayerInteraction diagnostic postfix failed safely.", ex);
        }
    }

    private static object? GetArg(object[] args, int index) => index >= 0 && index < args.Length ? args[index] : null;
}

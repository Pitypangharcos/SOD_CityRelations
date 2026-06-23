namespace SOD_CityRelations.Patches.Diagnostics;

internal static class DialogControllerOnDialogEndDiagnosticsPatch
{
    public static void Postfix(object __instance, object[] __args)
    {
        if (!DiagnosticPatchContext.Config.EnableDialogDiagnostics || !DiagnosticPatchContext.RateLimiter.TryAcquire())
        {
            return;
        }

        try
        {
            var dialog = GetArg(__args, 0);
            var dialogPresetStr = GetArg(__args, 1);
            var saysToInteractable = GetArg(__args, 2);
            var saidBy = GetArg(__args, 3);
            var jobRef = GetArg(__args, 4);

            DiagnosticPatchContext.Logger.Info(
                "[Diagnostics] DialogController.OnDialogEnd hit; " +
                $"dialogPresetStr={dialogPresetStr ?? "<null>"}; " +
                $"jobRef={jobRef ?? "<null>"}; " +
                $"dialog={SafeGameObjectDescriber.Describe(dialog)}; " +
                $"saidBy={SafeGameObjectDescriber.Describe(saidBy)}; " +
                $"saidByRole={SafeGameObjectDescriber.GuessRole(saidBy)}; " +
                $"saysToInteractable={SafeGameObjectDescriber.Describe(saysToInteractable)}");

            if (DiagnosticPatchContext.Config.EnableFamiliarityFromDialogEnd)
            {
                DiagnosticPatchContext.Logger.Warning("EnableFamiliarityFromDialogEnd is true, but relationship changes from diagnostics are intentionally not implemented in this pass.");
            }
        }
        catch (Exception ex)
        {
            DiagnosticPatchContext.Logger.Error("DialogController.OnDialogEnd diagnostic postfix failed safely.", ex);
        }
    }

    private static object? GetArg(object[] args, int index) => index >= 0 && index < args.Length ? args[index] : null;
}

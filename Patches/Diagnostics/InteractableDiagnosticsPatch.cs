namespace SOD_CityRelations.Patches.Diagnostics;

internal static class InteractableDiagnosticsPatch
{
    public static void OnInteractionKeyPostfix(object __instance, object[] __args)
    {
        Log("Interactable.OnInteraction(InteractionKey, Actor)", __instance, __args);
    }

    public static void OnInteractionActionPostfix(object __instance, object[] __args)
    {
        Log("Interactable.OnInteraction(InteractionAction, Actor, Boolean, Single)", __instance, __args);
    }

    private static void Log(string label, object __instance, object[] __args)
    {
        if (!DiagnosticPatchContext.Config.EnableInteractionDiagnostics || !DiagnosticPatchContext.RateLimiter.TryAcquire())
        {
            return;
        }

        try
        {
            DiagnosticPatchContext.Logger.Info(
                "[Diagnostics] " + label + " hit; " +
                $"instance={SafeGameObjectDescriber.Describe(__instance)}; " +
                $"arg0={SafeGameObjectDescriber.Describe(GetArg(__args, 0))}; " +
                $"actor={SafeGameObjectDescriber.Describe(GetArg(__args, 1))}");
        }
        catch (Exception ex)
        {
            DiagnosticPatchContext.Logger.Error(label + " diagnostic postfix failed safely.", ex);
        }
    }

    private static object? GetArg(object[] args, int index) => index >= 0 && index < args.Length ? args[index] : null;
}

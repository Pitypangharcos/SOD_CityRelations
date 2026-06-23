using System.Reflection;
using SOD_CityRelations.Config;
using SOD_CityRelations.Patches.Diagnostics;
using SOD_CityRelations.Services;

#if CITYRELATIONS_BEPINEX && CITYRELATIONS_SOD_INTEROP
using HarmonyLib;
#endif

namespace SOD_CityRelations.Patches;

public static class PatchRegistration
{
    public static void Register(CityRelationsConfig config, CityRelationsService service, ICityRelationsLogger logger)
    {
#if CITYRELATIONS_BEPINEX && CITYRELATIONS_SOD_INTEROP
        RegisterGameDiagnostics(config, service, logger);
#elif CITYRELATIONS_BEPINEX
        logger.Warning("BepInEx integration is compiled, but SoD interop diagnostics are not. No Harmony diagnostics are registered.");
#else
        logger.Warning("No BepInEx/Harmony integration is compiled. Backend/API will run without dialogue manipulation.");
#endif
    }

#if CITYRELATIONS_BEPINEX && CITYRELATIONS_SOD_INTEROP
    private static void RegisterGameDiagnostics(CityRelationsConfig config, CityRelationsService service, ICityRelationsLogger logger)
    {
        if (!config.EnableHarmonyPatches)
        {
            logger.Warning("Harmony diagnostics are compiled but disabled by config: EnableHarmonyPatches=false.");
            return;
        }

        try
        {
            DiagnosticPatchContext.Initialize(config, service, logger);
            var harmony = new Harmony("pitypang.sod.cityrelations.diagnostics");

            if (config.EnableDialogDiagnostics)
            {
                PatchBySignature(
                    harmony,
                    logger,
                    "DialogController",
                    "OnDialogEnd",
                    new[] { "AISpeechPreset", "String", "Interactable", "Actor", "Int32" },
                    typeof(DialogControllerOnDialogEndDiagnosticsPatch).GetMethod(nameof(DialogControllerOnDialogEndDiagnosticsPatch.Postfix), BindingFlags.Static | BindingFlags.Public));
            }
            else
            {
                logger.Info("Dialog diagnostics disabled by config.");
            }

            if (config.EnableInteractionDiagnostics)
            {
                PatchBySignature(
                    harmony,
                    logger,
                    "InteractionController",
                    "SetCurrentPlayerInteraction",
                    new[] { "InteractionKey", "Interactable", "InteractableCurrentAction", "Boolean", "Int32" },
                    typeof(InteractionControllerDiagnosticsPatch).GetMethod(nameof(InteractionControllerDiagnosticsPatch.SetCurrentPlayerInteractionPostfix), BindingFlags.Static | BindingFlags.Public));

                PatchBySignature(
                    harmony,
                    logger,
                    "Interactable",
                    "OnInteraction",
                    new[] { "InteractionKey", "Actor" },
                    typeof(InteractableDiagnosticsPatch).GetMethod(nameof(InteractableDiagnosticsPatch.OnInteractionKeyPostfix), BindingFlags.Static | BindingFlags.Public));

                PatchBySignature(
                    harmony,
                    logger,
                    "Interactable",
                    "OnInteraction",
                    new[] { "InteractionAction", "Actor", "Boolean", "Single" },
                    typeof(InteractableDiagnosticsPatch).GetMethod(nameof(InteractableDiagnosticsPatch.OnInteractionActionPostfix), BindingFlags.Static | BindingFlags.Public));
            }
            else
            {
                logger.Info("Interaction diagnostics disabled by config.");
            }

            if (config.EnableSpeechDiagnostics)
            {
                logger.Warning("EnableSpeechDiagnostics is true, but SpeechController.Speak is intentionally not patched in this pass.");
            }
        }
        catch (Exception ex)
        {
            logger.Error("Failed while registering Harmony diagnostics. Continuing without gameplay changes.", ex);
        }
    }

    private static void PatchBySignature(Harmony harmony, ICityRelationsLogger logger, string typeName, string methodName, IReadOnlyList<string> expectedParameterTypeNames, MethodInfo? postfix)
    {
        try
        {
            if (postfix == null)
            {
                logger.Warning($"Missing postfix method for {typeName}.{methodName}; patch skipped.");
                return;
            }

            var type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                logger.Warning($"Patch target type missing: {typeName}; patch skipped.");
                return;
            }

            var method = FindMethod(type, methodName, expectedParameterTypeNames);
            if (method == null)
            {
                logger.Warning($"Patch target method/signature missing: {typeName}.{methodName}({string.Join(", ", expectedParameterTypeNames)}); patch skipped.");
                return;
            }

            harmony.Patch(method, postfix: new HarmonyMethod(postfix));
            logger.Info($"Registered read-only diagnostic postfix: {type.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name))})");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to patch {typeName}.{methodName}; patch skipped.", ex);
        }
    }

    private static MethodInfo? FindMethod(Type type, string methodName, IReadOnlyList<string> expectedParameterTypeNames)
    {
        return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.Name == methodName)
            .FirstOrDefault(method =>
            {
                var parameters = method.GetParameters();
                if (parameters.Length != expectedParameterTypeNames.Count)
                {
                    return false;
                }

                for (var i = 0; i < parameters.Length; i++)
                {
                    if (!ParameterTypeMatches(parameters[i].ParameterType, expectedParameterTypeNames[i]))
                    {
                        return false;
                    }
                }

                return true;
            });
    }

    private static bool ParameterTypeMatches(Type actualType, string expectedName)
    {
        return actualType.Name == expectedName ||
               actualType.FullName == expectedName ||
               actualType.Name.EndsWith("." + expectedName, StringComparison.Ordinal);
    }
#endif
}

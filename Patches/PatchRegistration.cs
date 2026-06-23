using SOD_CityRelations.Services;

namespace SOD_CityRelations.Patches;

public static class PatchRegistration
{
    public static void Register(ICityRelationsLogger logger)
    {
        // No Harmony patch target is registered until real Shadows of Doubt assemblies are available.
        logger.Warning("No verified Shadows of Doubt patch points are registered. Backend/API will run without dialogue manipulation.");
    }
}

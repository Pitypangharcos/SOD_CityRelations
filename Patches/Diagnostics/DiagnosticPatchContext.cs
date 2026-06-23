using SOD_CityRelations.Config;
using SOD_CityRelations.Services;

namespace SOD_CityRelations.Patches.Diagnostics;

internal static class DiagnosticPatchContext
{
    public static CityRelationsConfig Config { get; private set; } = new();
    public static CityRelationsService? Service { get; private set; }
    public static ICityRelationsLogger Logger { get; private set; } = NullCityRelationsLogger.Instance;
    public static PatchRateLimiter RateLimiter { get; private set; } = new(60);

    public static void Initialize(CityRelationsConfig config, CityRelationsService service, ICityRelationsLogger logger)
    {
        Config = config;
        Service = service;
        Logger = logger;
        RateLimiter = new PatchRateLimiter(Math.Max(1, config.MaxDiagnosticsPerMinute));
    }
}

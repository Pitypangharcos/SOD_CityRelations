using SOD_CityRelations.API;
using SOD_CityRelations.Config;
using SOD_CityRelations.Patches;
using SOD_CityRelations.Services;

#if CITYRELATIONS_BEPINEX
using BepInEx;
using BepInEx.Unity.IL2CPP;
#endif

namespace SOD_CityRelations;

#if CITYRELATIONS_BEPINEX
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
#else
public sealed class Plugin
#endif
{
    public const string PluginGuid = "pitypang.sod.cityrelations";
    public const string PluginName = "SOD_CityRelations";
    public const string PluginVersion = "0.1.1";

    public static CityRelationsService? Service { get; private set; }

#if CITYRELATIONS_BEPINEX
    public override void Load()
    {
        var config = BindBepInExConfig();
        var logger = new BepInExCityRelationsLogger(Log, config.EnableDebugLogging);
        Initialize(config, Paths.ConfigPath, logger);
    }

    private CityRelationsConfig BindBepInExConfig()
    {
        return new CityRelationsConfig
        {
            EnableMod = Config.Bind("General", "EnableMod", true, "Enable or disable the mod.").Value,
            EnableDebugLogging = Config.Bind("General", "EnableDebugLogging", false, "Enable verbose debug logging.").Value,
            EnableRelationshipChanges = Config.Bind("General", "EnableRelationshipChanges", true, "Allow relationship values to change.").Value,
            EnableLieSystem = Config.Bind("General", "EnableLieSystem", true, "Enable lie chance decisions.").Value,
            EnableDialogueManipulation = Config.Bind("General", "EnableDialogueManipulation", false, "Allow verified patches to alter dialogue. Disabled for MVP unless a safe hook is implemented.").Value,
            ProtectCriticalInfo = Config.Bind("General", "ProtectCriticalInfo", true, "Avoid corrupting case-critical answers unless a safe alternate-route check exists.").Value,

            FamiliarityGainPerConversation = Config.Bind("Relationship", "FamiliarityGainPerConversation", 1, "Familiarity gained when a safe conversation interaction is observed.").Value,
            TrustGainWhenHelpful = Config.Bind("Relationship", "TrustGainWhenHelpful", 5, "Trust gained for helpful player actions.").Value,
            FearGainWhenThreatened = Config.Bind("Relationship", "FearGainWhenThreatened", 10, "Fear gained when the player threatens a citizen.").Value,
            SuspicionGainWhenCaughtTrespassing = Config.Bind("Relationship", "SuspicionGainWhenCaughtTrespassing", 10, "Suspicion gained when caught trespassing.").Value,
            RelationshipDecayEnabled = Config.Bind("Relationship", "RelationshipDecayEnabled", false, "Enable relationship decay over time.").Value,
            RelationshipDecayAmountPerDay = Config.Bind("Relationship", "RelationshipDecayAmountPerDay", 1, "Daily decay amount when decay is enabled.").Value,

            BaseLieChance = Config.Bind("Lying", "BaseLieChance", 0.08f, "Base lie chance before relationship and topic modifiers.").Value,
            MinLieChance = Config.Bind("Lying", "MinLieChance", 0.00f, "Minimum lie chance.").Value,
            MaxLieChance = Config.Bind("Lying", "MaxLieChance", 0.65f, "Maximum lie chance.").Value,
            SensitiveTopicBonus = Config.Bind("Lying", "SensitiveTopicBonus", 0.10f, "Lie chance bonus for sensitive topics.").Value,
            CrimeTopicBonus = Config.Bind("Lying", "CrimeTopicBonus", 0.15f, "Lie chance bonus for crime topics.").Value,
            IllegalTopicBonus = Config.Bind("Lying", "IllegalTopicBonus", 0.20f, "Lie chance bonus for illegal activity topics.").Value,
            HighFearBonus = Config.Bind("Lying", "HighFearBonus", 0.15f, "Maximum fear-based lie chance effect.").Value,
            HighSuspicionBonus = Config.Bind("Lying", "HighSuspicionBonus", 0.15f, "Maximum suspicion-based lie chance effect.").Value,
            LowTrustBonus = Config.Bind("Lying", "LowTrustBonus", 0.10f, "Maximum low-trust lie chance effect.").Value,
            HighTrustReduction = Config.Bind("Lying", "HighTrustReduction", 0.15f, "Maximum high-trust lie chance reduction.").Value,
            HighGratitudeReduction = Config.Bind("Lying", "HighGratitudeReduction", 0.10f, "Maximum gratitude lie chance reduction.").Value,
            HighFamiliarityReduction = Config.Bind("Lying", "HighFamiliarityReduction", 0.05f, "Maximum familiarity lie chance reduction.").Value,
            FearMode = Config.Bind("Lying", "FearMode", SOD_CityRelations.Models.FearMode.Mixed, "How fear affects lie chance: LieMore, ComplyMore, or Mixed.").Value,
            LieSeedMode = Config.Bind("Lying", "LieSeedMode", SOD_CityRelations.Models.LieSeedMode.Random, "Randomness mode for lie rolls.").Value,

            ExposePublicApi = Config.Bind("Compatibility", "ExposePublicApi", true, "Expose the static CityRelationsAPI facade.").Value,
            AllowReputationIntegration = Config.Bind("Compatibility", "AllowReputationIntegration", true, "Allow future reputation integrations to listen to events.").Value,
            AllowHeatIntegration = Config.Bind("Compatibility", "AllowHeatIntegration", true, "Allow future heat integrations to listen to events.").Value
        };
    }
#endif

    public static void Initialize(CityRelationsConfig config, string baseDirectory, ICityRelationsLogger logger)
    {
        if (!config.EnableMod)
        {
            logger.Info("SOD_CityRelations is disabled by config.");
            return;
        }

        var persistence = new RelationshipPersistenceService(baseDirectory, config.SaveThrottleInterval, logger);
        Service = new CityRelationsService(config, persistence, logger);

        if (config.ExposePublicApi)
        {
            CityRelationsAPI.Initialize(Service);
        }

        PatchRegistration.Register(logger);
        logger.Info("SOD_CityRelations backend initialized.");
    }
}

#if CITYRELATIONS_BEPINEX
internal sealed class BepInExCityRelationsLogger : ICityRelationsLogger
{
    private readonly BepInEx.Logging.ManualLogSource log;
    private readonly bool debugEnabled;

    public BepInExCityRelationsLogger(BepInEx.Logging.ManualLogSource log, bool debugEnabled)
    {
        this.log = log;
        this.debugEnabled = debugEnabled;
    }

    public void Info(string message) => log.LogInfo(message);
    public void Warning(string message) => log.LogWarning(message);
    public void Error(string message, Exception? exception = null) => log.LogError(exception == null ? message : message + " " + exception);
    public void Debug(string message)
    {
        if (debugEnabled)
        {
            log.LogDebug(message);
        }
    }
}
#endif

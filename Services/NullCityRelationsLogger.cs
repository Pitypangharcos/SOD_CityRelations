namespace SOD_CityRelations.Services;

public sealed class NullCityRelationsLogger : ICityRelationsLogger
{
    public static readonly NullCityRelationsLogger Instance = new();

    public void Info(string message) { }
    public void Warning(string message) { }
    public void Error(string message, Exception? exception = null) { }
    public void Debug(string message) { }
}

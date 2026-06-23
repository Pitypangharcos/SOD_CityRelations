namespace SOD_CityRelations.Services;

public sealed class ConsoleCityRelationsLogger : ICityRelationsLogger
{
    private readonly bool debugEnabled;

    public ConsoleCityRelationsLogger(bool debugEnabled)
    {
        this.debugEnabled = debugEnabled;
    }

    public void Info(string message) => Console.WriteLine("[SOD_CityRelations] " + message);
    public void Warning(string message) => Console.WriteLine("[SOD_CityRelations/WARN] " + message);
    public void Error(string message, Exception? exception = null) => Console.WriteLine("[SOD_CityRelations/ERROR] " + message + (exception == null ? string.Empty : " " + exception));
    public void Debug(string message)
    {
        if (debugEnabled)
        {
            Console.WriteLine("[SOD_CityRelations/DEBUG] " + message);
        }
    }
}

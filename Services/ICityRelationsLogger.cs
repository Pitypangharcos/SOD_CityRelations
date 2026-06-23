namespace SOD_CityRelations.Services;

public interface ICityRelationsLogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
    void Debug(string message);
}

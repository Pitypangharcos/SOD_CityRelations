namespace SOD_CityRelations.Patches.Diagnostics;

internal static class SafeGameObjectDescriber
{
    public static string Describe(object? value)
    {
        if (value == null)
        {
            return "<null>";
        }

        var typeName = SafeReflection.SafeTypeName(value);
        var text = SafeToString(value);
        var role = GuessRole(typeName, text);
        var ids = SafeReflection.ProbeIdLikeValues(value);
        return $"type={typeName}; role={role}; toString={text}; {ids}";
    }

    public static string GuessRole(object? value) => value == null ? "null" : GuessRole(SafeReflection.SafeTypeName(value), SafeToString(value));

    private static string GuessRole(string typeName, string text)
    {
        var haystack = typeName + " " + text;
        if (haystack.Contains("Citizen", StringComparison.OrdinalIgnoreCase)) return "citizen-like";
        if (haystack.Contains("Human", StringComparison.OrdinalIgnoreCase)) return "human-like";
        if (haystack.Contains("Player", StringComparison.OrdinalIgnoreCase)) return "player-like";
        if (haystack.Contains("Actor", StringComparison.OrdinalIgnoreCase)) return "actor-like";
        return "unknown";
    }

    private static string SafeToString(object value)
    {
        try
        {
            return value.ToString() ?? "<null>";
        }
        catch (Exception ex)
        {
            return "<toString-error:" + ex.GetType().Name + ">";
        }
    }
}

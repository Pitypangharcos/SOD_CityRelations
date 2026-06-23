using System.Reflection;

namespace SOD_CityRelations.Patches.Diagnostics;

internal static class SafeReflection
{
    private static readonly string[] IdLikeNames =
    {
        "id", "ID", "Id", "humanID", "citizenID", "actorID", "interactableID", "presetName", "name", "Name"
    };

    public static string ProbeIdLikeValues(object? value)
    {
        if (value == null)
        {
            return "ids=<null>";
        }

        try
        {
            var type = value.GetType();
            var parts = new List<string>();

            foreach (var name in IdLikeNames)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && IsSimple(field.FieldType))
                {
                    parts.Add(name + "=" + SafeValue(() => field.GetValue(value)));
                    continue;
                }

                var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.GetIndexParameters().Length == 0 && property.GetMethod != null && IsSimple(property.PropertyType))
                {
                    parts.Add(name + "=" + SafeValue(() => property.GetValue(value)));
                }
            }

            return parts.Count == 0 ? "ids=<none-found>" : "ids=[" + string.Join(", ", parts.Distinct()) + "]";
        }
        catch (Exception ex)
        {
            return "ids=<reflection-error:" + ex.GetType().Name + ">";
        }
    }

    public static string SafeTypeName(object? value)
    {
        if (value == null)
        {
            return "<null>";
        }

        try
        {
            return value.GetType().FullName ?? value.GetType().Name;
        }
        catch
        {
            return "<type-error>";
        }
    }

    private static bool IsSimple(Type type)
    {
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
    }

    private static string SafeValue(Func<object?> getter)
    {
        try
        {
            return getter()?.ToString() ?? "<null>";
        }
        catch (Exception ex)
        {
            return "<error:" + ex.GetType().Name + ">";
        }
    }
}

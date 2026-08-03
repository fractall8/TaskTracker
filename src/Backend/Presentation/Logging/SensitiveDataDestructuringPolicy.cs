using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace Presentation.Logging;

public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    // Matched as substrings, not exact names: an exact list silently missed ApiKey, and would miss the
    // next key-bearing property too. Over-redaction here is harmless; under-redaction leaks credentials.
    private static readonly string[] _sensitiveFragments =
    [
        "password",
        "secret",
        "token",
        "apikey",
        "connectionstring",
        "credential"
    ];

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        var type = value.GetType();

        if (type.IsPrimitive || type.IsValueType || type == typeof(string))
        {
            result = null!;
            return false;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var logEventProperties = new List<LogEventProperty>();

        foreach (var property in properties)
        {
            var propName = property.Name;

            if (IsSensitive(propName))
            {
                logEventProperties.Add(new LogEventProperty(propName, new ScalarValue("***")));
            }
            else
            {
                try
                {
                    var propValue = property.GetValue(value);
                    logEventProperties.Add(new LogEventProperty(
                        propName,
                        propertyValueFactory.CreatePropertyValue(propValue, destructureObjects: true)));
                }
                catch
                {
                    logEventProperties.Add(new LogEventProperty(propName, new ScalarValue("[Error reading value]")));
                }
            }
        }

        result = new StructureValue(logEventProperties, type.Name);
        return true;
    }

    private static bool IsSensitive(string propertyName) =>
        _sensitiveFragments.Any(fragment =>
            propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}

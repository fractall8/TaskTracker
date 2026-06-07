using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace Presentation.Logging;

public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private readonly string[] _sensitiveProperties = 
    [
        "Password", 
        "Token", 
        "RefreshToken", 
        "ClientSecret", 
        "AccessToken"
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

            if (_sensitiveProperties.Contains(propName, StringComparer.OrdinalIgnoreCase))
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
}
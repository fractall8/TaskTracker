using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Contracts.Notifications.BoardActions;

namespace Contracts.Serialization;

public static class PolymorphicJsonModifier
{
    public static void AddBoardActionPolymorphism(JsonTypeInfo jsonTypeInfo)
    {
        if (jsonTypeInfo.Type != typeof(BoardActionPayload))
        {
            return;
        }

        jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "$type",
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
        };

        var derivedTypes = typeof(BoardActionPayload).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(BoardActionPayload)));

        foreach (var type in derivedTypes)
        {
            // will change for e.g. TaskCreatedPayload => taskCreated
            var typeName = type.Name.Replace("Payload", "");
            var discriminator = char.ToLower(typeName[0]) + typeName[1..];

            jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(
                new JsonDerivedType(type, discriminator));
        }
    }
}

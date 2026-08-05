
using System.Reflection;
using System.Text;
using Contracts.Enums;

namespace ArchitectureTests.Ai;

public static class AiProjectionRules
{
    public const string ProjectionNamespace = "Application.Ai.Projections";

    private static readonly string[] _forbiddenPropertyNames =
    [
        "Assignee", "AssigneeId", "Reporter", "ReporterId",
        "UserId", "OwnerId", "AuthorId", "MemberId", "CreatorId",
        "CreatedBy", "UpdatedBy", "DeletedBy",
        "Email", "DisplayName", "AvatarUrl",
        "AzureAdObjectId", "AcsCommunicationUserId",
        "Description", "Body", "Content", "Comments", "Attachments"
    ];

    private static readonly Type[] _roleTypes = [typeof(BoardRoleDto), typeof(WorkspaceRoleDto)];

    public static IReadOnlyList<Type> ProductionProjections(Assembly applicationAssembly) =>
        [.. applicationAssembly
            .GetTypes()
            .Where(type => type is { IsPublic: true, IsNested: false }
                           && type.Namespace == ProjectionNamespace)
            .OrderBy(type => type.Name, StringComparer.Ordinal)];

    public static IReadOnlyList<string> DescribeSurface(IEnumerable<Type> types) =>
        [.. types
            .SelectMany(type => Properties(type)
                .Select(property => $"{type.Name}.{property.Name} : {PrettyName(property.PropertyType)}"))
            .OrderBy(line => line, StringComparer.Ordinal)];

    public static IReadOnlyList<string> SealedRecordViolations(IEnumerable<Type> types) =>
        [.. types
            .Where(type => !type.IsSealed || !IsRecord(type))
            .Select(type => $"{type.Name} must be a sealed record"
                            + (IsRecord(type) ? " (it is a record but not sealed)" : " (it is not a record)"))];

    public static IReadOnlyList<string> BaseTypeViolations(IEnumerable<Type> types) =>
        [.. types
            .Where(type => type.BaseType is not null && type.BaseType != typeof(object))
            .Select(type => $"{type.Name} must not inherit from {type.BaseType!.Name} — "
                            + "inheritance is how BaseEntity audit fields leak in")];

    public static IReadOnlyList<string> ForbiddenNameViolations(IEnumerable<Type> types) =>
        [.. from type in types
            from property in Properties(type)
            where property.Name.EndsWith("ById", StringComparison.Ordinal)
                  || _forbiddenPropertyNames.Contains(property.Name, StringComparer.Ordinal)
            select $"{type.Name}.{property.Name} is forbidden — see EPIC 3 §6"];

    public static IReadOnlyList<string> RoleNamingViolations(IEnumerable<Type> types) =>
        [.. from type in types
            from property in Properties(type)
            where _roleTypes.Contains(Unwrap(property.PropertyType))
                  && !property.Name.StartsWith("My", StringComparison.Ordinal)
            select $"{type.Name}.{property.Name} carries a role and must be named My* "
                   + "so it cannot be read as someone else's role"];

    public static IReadOnlyList<string> EntityLeakViolations(IEnumerable<Type> types) =>
        [.. from type in types
            from property in Properties(type)
            let leaked = ReferencedTypes(property.PropertyType).FirstOrDefault(IsDomainEntity)
            where leaked is not null
            select $"{type.Name}.{property.Name} exposes domain entity {leaked!.Name} — "
                   + "project the fields you need instead"];

    public static IReadOnlyList<string> MapperReferenceViolations(Assembly assembly) =>
        [.. assembly
            .GetReferencedAssemblies()
            .Where(reference => reference.Name is { } name
                                && (name.Contains("AutoMapper", StringComparison.OrdinalIgnoreCase)
                                    || name.Contains("Mapster", StringComparison.OrdinalIgnoreCase)))
            .Select(reference => $"{assembly.GetName().Name} references {reference.Name} — "
                                 + "reflex mapping copies every matching property, including audit fields")];

    public static string Format(IEnumerable<string> violations) =>
        string.Join(Environment.NewLine, violations.Select(violation => "  - " + violation));

    public static string Diff(IReadOnlyList<string> approved, IReadOnlyList<string> actual)
    {
        var added = actual.Except(approved, StringComparer.Ordinal).ToList();
        var removed = approved.Except(actual, StringComparer.Ordinal).ToList();
        var message = new StringBuilder();

        foreach (var line in added)
        {
            message.AppendLine($"  + {line}");
        }

        foreach (var line in removed)
        {
            message.AppendLine($"  - {line}");
        }

        return message.ToString();
    }

    private static IEnumerable<PropertyInfo> Properties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.Name != "EqualityContract")
            .OrderBy(property => property.Name, StringComparer.Ordinal);

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null
        || type.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    private static bool IsDomainEntity(Type type) =>
        type.Namespace?.StartsWith("Domain.Entities", StringComparison.Ordinal) == true;

    private static IEnumerable<Type> ReferencedTypes(Type type)
    {
        yield return type;

        var unwrapped = Unwrap(type);

        if (unwrapped != type)
        {
            yield return unwrapped;
        }

        if (type.IsArray && type.GetElementType() is { } element)
        {
            yield return element;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                yield return argument;
            }
        }
    }

    private static Type Unwrap(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    private static string PrettyName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } inner)
        {
            return PrettyName(inner) + "?";
        }

        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        var arguments = string.Join(", ", type.GetGenericArguments().Select(PrettyName));

        return $"{name}<{arguments}>";
    }
}

using System.Reflection;

namespace ArchitectureTests.Ai;

public static class AiToolRules
{
    public const string ToolNamespace = "Application.Features.Ai.Tools";

    // Matched by type name so the test project needs no extra package references.
    private static readonly string[] _allowedDependencies =
    [
        "IAiDataRepository",
        "IWorkspaceAccessService",
        "IBoardAccessService",
        "ICurrentUserAccessor",
        "IPlanCatalog",
        "IOptions`1",
        "ILogger`1",
        "IDateTimeProvider",
        // Reads a clock and configuration to resolve which day it is; nothing mutable.
        "IBusinessCalendar"
    ];

    // Handlers only. Request records and validators share the namespace (AD-2) but their constructor
    // parameters are query arguments, not injected collaborators.
    public static bool IsToolHandler(Type type) =>
        type is { IsClass: true, IsAbstract: false }
        && type.GetInterfaces().Any(contract =>
            contract.Name.StartsWith("IRequestHandler", StringComparison.Ordinal));

    public static IReadOnlyList<Type> ToolTypes(Assembly applicationAssembly) =>
        [.. applicationAssembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith(ToolNamespace, StringComparison.Ordinal) == true
                           && IsToolHandler(type))
            .OrderBy(type => type.Name, StringComparer.Ordinal)];

    public static IReadOnlyList<string> DependencyViolations(IEnumerable<Type> toolTypes) =>
        [.. from type in toolTypes
            from constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            from parameter in constructor.GetParameters()
            let dependency = parameter.ParameterType
            where !IsAllowed(dependency)
            select $"{type.Name} takes '{dependency.Name} {parameter.Name}'. AI tool handlers are "
                   + "read-only and may only depend on: " + string.Join(", ", _allowedDependencies)];

    private static bool IsAllowed(Type dependency) =>
        _allowedDependencies.Contains(dependency.Name, StringComparer.Ordinal);
}

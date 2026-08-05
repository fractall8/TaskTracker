using Contracts.Enums;
using Domain.Entities;

namespace ArchitectureTests.Ai.Fixtures;

// Deliberately-broken and known-good sample types, so every rule is proven to fire on every build.
internal static class RuleFixtures
{
    public sealed record GoodBoardSummary(
        Guid Id,
        string Name,
        bool IsArchived,
        BoardRoleDto? MyBoardRole,
        int TaskCount);

    public sealed record LeaksAuditColumn(Guid Id, string Title, Guid CreatedById);

    public sealed record LeaksAssignee(Guid Id, string Title, Guid AssigneeId);

    public sealed record LeaksDisplayName(Guid Id, string DisplayName);

    public sealed record LeaksDescription(Guid Id, string Title, string? Description);

    public sealed record AmbiguousRole(Guid Id, BoardRoleDto Role);

    public sealed record ExposesEntity(Guid Id, TaskItem Task);

    public sealed record ExposesEntityCollection(Guid Id, IReadOnlyList<TaskItem> Tasks);

    public record NotSealed(Guid Id, string Name);

    public sealed class NotARecord
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }

    public abstract record AuditedBase(Guid Id, Guid UpdatedById);

    public sealed record InheritsAudit(Guid Id, Guid UpdatedById, string Name) : AuditedBase(Id, UpdatedById);
}

using System.Reflection;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Interfaces.UOW;
using FluentValidation;
using MediatR;

namespace ArchitectureTests.Ai;

public class AiToolReadOnlyTests
{
    private static readonly Assembly _applicationAssembly = typeof(IFaqAssistantService).Assembly;

    [Fact]
    public void Tool_handlers_depend_only_on_read_only_collaborators()
    {
        var violations = AiToolRules.DependencyViolations(AiToolRules.ToolTypes(_applicationAssembly));

        Assert.True(
            violations.Count == 0,
            $"""
             An AI tool handler can write. A handler that is never given a unit of work or a mutating
             repository has nothing to write with — that is how read-only is enforced here.

             {AiProjectionRules.Format(violations)}
             """);
    }

    [Fact]
    public void Rule_rejects_a_handler_that_takes_a_unit_of_work()
    {
        var violations = AiToolRules.DependencyViolations([typeof(Fixtures.WritesViaUnitOfWork)]);

        Assert.Contains(violations, violation => violation.Contains("IUnitOfWork", StringComparison.Ordinal));
    }

    [Fact]
    public void Rule_rejects_a_handler_that_takes_a_mutating_repository()
    {
        var violations = AiToolRules.DependencyViolations([typeof(Fixtures.WritesViaRepository)]);

        Assert.Contains(violations, violation => violation.Contains("ITaskRepository", StringComparison.Ordinal));
    }

    [Fact]
    public void Rule_accepts_a_read_only_handler() =>
        Assert.Empty(AiToolRules.DependencyViolations([typeof(Fixtures.ReadsOnly)]));

    [Fact]
    public void Handler_is_recognised() => Assert.True(AiToolRules.IsToolHandler(typeof(Fixtures.SampleHandler)));

    [Theory]
    [InlineData(typeof(Fixtures.SampleQuery))]
    [InlineData(typeof(Fixtures.SampleValidator))]
    public void Request_record_and_validator_are_not_handlers(Type type) =>
        Assert.False(AiToolRules.IsToolHandler(type));

    private static class Fixtures
    {
        internal sealed record SampleQuery(Guid WorkspaceId) : IRequest<string>;

        internal sealed class SampleValidator : AbstractValidator<SampleQuery>;

        internal sealed class SampleHandler(IBoardAccessService boardAccessService)
            : IRequestHandler<SampleQuery, string>
        {
            public Task<string> Handle(SampleQuery request, CancellationToken ct) =>
                Task.FromResult(boardAccessService.GetType().Name);
        }

        internal sealed class WritesViaUnitOfWork(IUnitOfWork unitOfWork)
        {
            public IUnitOfWork UnitOfWork { get; } = unitOfWork;
        }

        internal sealed class WritesViaRepository(ITaskRepository taskRepository)
        {
            public ITaskRepository TaskRepository { get; } = taskRepository;
        }

        internal sealed class ReadsOnly(IBoardAccessService boardAccessService)
        {
            public IBoardAccessService BoardAccessService { get; } = boardAccessService;
        }
    }
}

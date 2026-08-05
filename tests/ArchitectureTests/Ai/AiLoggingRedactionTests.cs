using Application.Behaviors;
using Contracts.DTOs;
using Contracts.Enums;
using Infrastructure.Ai.Tools;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace ArchitectureTests.Ai;

// EPIC 3 Story 3.7: no question, answer, tool argument or tool result may reach the logs. Serilog's
// destructuring policy redacts by property name and cannot see into free text, so this is enforced here.
public class AiLoggingRedactionTests
{
    private const string _question = "SENSITIVE-QUESTION-TEXT";
    private const string _answer = "SENSITIVE-ANSWER-TEXT";

    [Fact]
    public async Task Faq_command_payload_and_response_are_not_logged()
    {
        var log = new CapturingLogger();
        var behavior = new LoggingBehavior<SensitiveRequest, FaqAnswerDto>(log);

        await behavior.Handle(
            new SensitiveRequest(_question),
            _ => Task.FromResult(new FaqAnswerDto(_answer, FaqAnswerKindDto.DataBacked, [])),
            default);

        Assert.DoesNotContain(_question, log.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(_answer, log.Text, StringComparison.Ordinal);
        Assert.Contains(nameof(SensitiveRequest), log.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_request_not_marked_sensitive_still_logs_normally()
    {
        // Guards against the marker being applied so broadly that ordinary diagnostics disappear.
        var log = new CapturingLogger();
        var behavior = new LoggingBehavior<OrdinaryRequest, string>(log);

        await behavior.Handle(new OrdinaryRequest("VISIBLE"), _ => Task.FromResult("RESULT"), default);

        Assert.Contains("VISIBLE", log.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tool_invocation_logs_carry_no_arguments_or_results()
    {
        var log = new CapturingLogger();
        var kernel = Kernel.CreateBuilder().Build();

        kernel.FunctionInvocationFilters.Add(
            new FaqToolInvocationFilter(new AiToolBudget(5), log.For<FaqToolInvocationFilter>()));

        kernel.Plugins.AddFromFunctions("probe",
        [
            KernelFunctionFactory.CreateFromMethod(
                (string secretArgument) => "SENSITIVE-RESULT-TEXT for " + secretArgument,
                "probe")
        ]);

        await kernel.InvokeAsync("probe", "probe", new KernelArguments
        {
            ["secretArgument"] = "SENSITIVE-ARGUMENT-TEXT"
        });

        Assert.DoesNotContain("SENSITIVE-ARGUMENT-TEXT", log.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("SENSITIVE-RESULT-TEXT", log.Text, StringComparison.Ordinal);
        Assert.Contains("probe", log.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_refusal_message_is_logged_by_type_not_by_content()
    {
        var log = new CapturingLogger();
        var kernel = Kernel.CreateBuilder().Build();

        kernel.FunctionInvocationFilters.Add(
            new FaqToolInvocationFilter(new AiToolBudget(5), log.For<FaqToolInvocationFilter>()));

        kernel.Plugins.AddFromFunctions("probe",
        [
            KernelFunctionFactory.CreateFromMethod(
                string () => throw new Domain.Exceptions.ForbiddenException("SENSITIVE-REFUSAL-DETAIL"),
                "probe")
        ]);

        await kernel.InvokeAsync("probe", "probe");

        Assert.DoesNotContain("SENSITIVE-REFUSAL-DETAIL", log.Text, StringComparison.Ordinal);
        Assert.Contains(nameof(Domain.Exceptions.ForbiddenException), log.Text, StringComparison.Ordinal);
    }

    private record SensitiveRequest(string Question) : IRequest<FaqAnswerDto>, ISensitivePayload;

    private record OrdinaryRequest(string Marker) : IRequest<string>;

    private sealed class CapturingLogger : ILogger<LoggingBehavior<SensitiveRequest, FaqAnswerDto>>,
        ILogger<LoggingBehavior<OrdinaryRequest, string>>
    {
        private readonly List<string> _lines = [];

        public string Text => string.Join(Environment.NewLine, _lines);

        public ILogger<T> For<T>() => new Forwarding<T>(_lines);

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new Noop();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _lines.Add(formatter(state, exception) + " " + exception);

        private sealed class Forwarding<T>(List<string> lines) : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => new Noop();

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                lines.Add(formatter(state, exception) + " " + exception);
        }

        private sealed class Noop : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}

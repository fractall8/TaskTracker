using System.Collections;
using System.Diagnostics;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Infrastructure.Ai.Tools;

public class FaqToolInvocationFilter(AiToolBudget budget, ILogger<FaqToolInvocationFilter> logger)
    : IFunctionInvocationFilter
{
    public const string BudgetExhaustedResult =
        "Tool call limit reached for this question. Answer using the information already gathered.";

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        if (!budget.TryConsume())
        {
            logger.LogWarning(
                "FAQ tool budget of {MaxCalls} exhausted; refusing {Tool}.",
                budget.MaxCalls,
                context.Function.Name);

            context.Result = new FunctionResult(context.Function, BudgetExhaustedResult);

            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);

            // Names and counts only: arguments and results carry workspace content.
            logger.LogInformation(
                "FAQ tool {Tool} returned {RowCount} row(s) in {ElapsedMs}ms ({Used}/{MaxCalls} calls used).",
                context.Function.Name,
                RowCount(context.Result),
                stopwatch.ElapsedMilliseconds,
                budget.Used,
                budget.MaxCalls);
        }
        catch (AppException ex)
        {
            // A refusal is a legitimate outcome, not a turn-ending failure — hand the model a sentence it
            // can relay instead of letting the exception abort generation.
            logger.LogInformation(
                "FAQ tool {Tool} refused after {ElapsedMs}ms: {Reason}.",
                context.Function.Name,
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name);

            context.Result = new FunctionResult(context.Function, ex.Message);
        }
        catch (Exception ex) when (IsArgumentProblem(ex))
        {
            // A bad argument is recoverable, so hand back a corrective instruction instead of a dead end —
            // the model can retry within its budget. Seen in practice when a workspace named "1" led the
            // model to pass the name where the GUID belonged.
            logger.LogWarning(
                "FAQ tool {Tool} rejected the model's arguments: {Reason}.",
                context.Function.Name,
                ex.Message);

            context.Result = new FunctionResult(
                context.Function,
                "Those arguments were not valid. Ids must be the GUID from the Id field of a previous tool "
                + "result, never a name, and optional arguments should be omitted rather than guessed. "
                + "Correct the arguments and call the tool again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "FAQ tool {Tool} failed.", context.Function.Name);

            context.Result = new FunctionResult(
                context.Function,
                "That information could not be retrieved right now.");
        }
    }

    // SK wraps marshalling failures in KernelException, so check the chain rather than the outer type.
    private static bool IsArgumentProblem(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is ArgumentException or FormatException)
            {
                return true;
            }
        }

        return false;
    }

    private static int RowCount(FunctionResult? result) => result?.GetValue<object>() switch
    {
        null => 0,
        ICollection collection => collection.Count,
        IEnumerable enumerable => enumerable.Cast<object>().Count(),
        _ => 1
    };
}

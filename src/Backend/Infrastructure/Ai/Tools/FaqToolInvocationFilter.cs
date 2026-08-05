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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "FAQ tool {Tool} failed.", context.Function.Name);

            context.Result = new FunctionResult(
                context.Function,
                "That information could not be retrieved right now.");
        }
    }

    private static int RowCount(FunctionResult? result) => result?.GetValue<object>() switch
    {
        null => 0,
        ICollection collection => collection.Count,
        IEnumerable enumerable => enumerable.Cast<object>().Count(),
        _ => 1
    };
}

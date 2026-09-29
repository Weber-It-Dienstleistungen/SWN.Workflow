namespace Swn.Workflow.Infrastructure;

public interface IExchangeEwsConnectionTester
{
    Task<ExchangeEwsConnectionTestResult>
        TestAsync(
            CancellationToken cancellationToken = default);
}

public sealed record ExchangeEwsConnectionTestResult(
    bool Succeeded,
    string Message,
    string? EwsServiceUrl);
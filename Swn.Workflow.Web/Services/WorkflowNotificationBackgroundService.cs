using Swn.Workflow.Application;

namespace Swn.Workflow.Web.Services;

public sealed class WorkflowNotificationBackgroundService
    : BackgroundService
{
    private static readonly TimeSpan DispatchInterval =
        TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory
        _scopeFactory;

    private readonly ILogger<WorkflowNotificationBackgroundService>
        _logger;

    public WorkflowNotificationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkflowNotificationBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(
            scopeFactory);

        ArgumentNullException.ThrowIfNull(
            logger);

        _scopeFactory =
            scopeFactory;

        _logger =
            logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Workflow notification background service started. " +
            "Dispatch interval: {DispatchIntervalSeconds} seconds.",
            DispatchInterval.TotalSeconds);

        await DispatchOnceAsync(
            stoppingToken);

        using var timer =
            new PeriodicTimer(
                DispatchInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(
                stoppingToken))
            {
                await DispatchOnceAsync(
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Anwendung wird regulär beendet.
        }

        _logger.LogInformation(
            "Workflow notification background service stopped.");
    }

    private async Task DispatchOnceAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope =
                _scopeFactory.CreateScope();

            var dispatcher =
                scope.ServiceProvider
                    .GetRequiredService<IWorkflowNotificationDispatcher>();

            var taskAvailableResult =
                await dispatcher
                    .DispatchPendingTaskAvailableAsync(
                        cancellationToken);

            LogDispatchResult(
                "TaskAvailable",
                taskAvailableResult);

            var workflowCompletedResult =
                await dispatcher
                    .DispatchPendingWorkflowCompletedAsync(
                        cancellationToken);

            LogDispatchResult(
                "WorkflowCompleted",
                workflowCompletedResult);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Automatic workflow notification dispatch failed.");
        }
    }

    private void LogDispatchResult(
        string notificationType,
        WorkflowNotificationDispatchResult result)
    {
        if (result.ProcessedGroupCount == 0 &&
            result.FailedNotificationCount == 0)
        {
            return;
        }

        _logger.LogInformation(
            "Automatic workflow notification dispatch completed. " +
            "Type: {NotificationType}, " +
            "groups processed: {ProcessedGroupCount}, " +
            "groups sent: {SentGroupCount}, " +
            "groups failed: {FailedGroupCount}, " +
            "notifications sent: {SentNotificationCount}, " +
            "notifications failed: {FailedNotificationCount}.",
            notificationType,
            result.ProcessedGroupCount,
            result.SentGroupCount,
            result.FailedGroupCount,
            result.SentNotificationCount,
            result.FailedNotificationCount);
    }
}
using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;
using Swn.Workflow.Domain;

namespace Swn.Workflow.Infrastructure;

public sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly IDbContextFactory<WorkflowDbContext> _dbContextFactory;

    public WorkflowEngine(
        IDbContextFactory<WorkflowDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        _dbContextFactory = dbContextFactory;
    }

    public async Task<Guid> StartAsync(
        StartWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var workflowKey = request.WorkflowKey.Trim();
        var subject = request.Subject.Trim();
        var createdByUserId = request.CreatedByUserId.Trim();

        if (string.IsNullOrWhiteSpace(workflowKey))
        {
            throw new ArgumentException(
                "Workflow key must not be empty.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException(
                "Workflow subject must not be empty.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(createdByUserId))
        {
            throw new ArgumentException(
                "Creating user must not be empty.",
                nameof(request));
        }

        await using var db =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var definition = await db.WorkflowDefinitions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Key == workflowKey &&
                     x.IsActive,
                cancellationToken);

        if (definition is null)
        {
            throw new InvalidOperationException(
                $"No active workflow definition with key '{workflowKey}' was found.");
        }

        var version = await db.WorkflowVersions
            .AsNoTracking()
            .Where(x =>
                x.WorkflowDefinitionId == definition.Id &&
                x.IsPublished)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (version is null)
        {
            throw new InvalidOperationException(
                $"No published version for workflow '{workflowKey}' was found.");
        }

        var taskDefinitions = await db.TaskDefinitions
            .AsNoTracking()
            .Where(x => x.WorkflowVersionId == version.Id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Key)
            .ToListAsync(cancellationToken);

        if (taskDefinitions.Count == 0)
        {
            throw new InvalidOperationException(
                $"Workflow '{workflowKey}' does not contain any tasks.");
        }

        var workflowInstanceId = Guid.NewGuid();

        var workflowInstance = new WorkflowInstance
        {
            Id = workflowInstanceId,
            WorkflowVersionId = version.Id,
            Subject = subject,
            ReferenceDate = request.ReferenceDate,
            Status = WorkflowStatus.Open,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        db.WorkflowInstances.Add(workflowInstance);

        if (request.Properties is not null)
        {
            foreach (var property in request.Properties)
            {
                if (string.IsNullOrWhiteSpace(property.Key))
                {
                    throw new ArgumentException(
                        "Workflow property keys must not be empty.",
                        nameof(request));
                }

                if (property.Value is null)
                {
                    throw new ArgumentException(
                        $"Workflow property '{property.Key}' must not have a null value.",
                        nameof(request));
                }

                db.WorkflowInstanceProperties.Add(
                    new WorkflowInstanceProperty
                    {
                        Id = Guid.NewGuid(),
                        WorkflowInstanceId = workflowInstanceId,
                        Key = property.Key.Trim(),
                        Value = property.Value
                    });
            }
        }

        foreach (var taskDefinition in taskDefinitions)
        {
            db.TaskInstances.Add(
                new TaskInstance
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = workflowInstanceId,
                    TaskDefinitionId = taskDefinition.Id,
                    Status = WorkflowTaskStatus.Open,
                    AssignedRoleKey = taskDefinition.AssignedRoleKey
                });
        }

        await db.SaveChangesAsync(cancellationToken);

        return workflowInstanceId;
    }
}
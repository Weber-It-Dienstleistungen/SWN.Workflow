namespace Swn.Workflow.Domain;

public enum WorkflowNotificationType
{
    TaskAvailable = 0,
    TaskReminder = 1,
    DeadlineEscalation = 2,
    WorkflowCompleted = 3
}
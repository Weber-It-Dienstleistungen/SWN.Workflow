namespace Swn.Workflow.Domain;

public class TaskTransitionDefinition
{
    public Guid Id { get; set; }

    public Guid FromTaskDefinitionId { get; set; }

    public Guid ToTaskDefinitionId { get; set; }

    public string? RequiredDecisionOutcomeKey { get; set; }
}
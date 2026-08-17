namespace BankingPlatform.Domain.Enums;

public enum ComplaintStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Closed = 3,
    Cancelled = 4
}

public enum ComplaintPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

public enum WorkflowDefinitionStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public enum WorkflowNodeType
{
    Start = 0,
    HumanTask = 1,
    End = 2
}

public enum WorkflowInstanceStatus
{
    Running = 0,
    Completed = 1,
    Cancelled = 2,
    Faulted = 3
}

public enum WorkflowTaskStatus
{
    Open = 0,
    Completed = 1,
    Cancelled = 2
}

namespace BankingPlatform.Application.DTOs;

public sealed class WorkflowNodeConfig
{
    public string AssignmentMode { get; set; } = "queue";

    public bool SendEmailNotification { get; set; }
}
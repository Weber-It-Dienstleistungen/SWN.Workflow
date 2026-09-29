namespace Swn.Workflow.Infrastructure;

public sealed class EmailConfiguration
{
    public int Id { get; set; }

    public bool Enabled { get; set; }

    public EmailTransportMode Transport { get; set; } =
        EmailTransportMode.ExchangeEws;

    public string SenderAddress { get; set; } =
        string.Empty;

    public string SenderName { get; set; } =
        "SWN Workflow";

    public string Username { get; set; } =
        string.Empty;

    public string? EncryptedPassword { get; set; }

    public string SmtpHost { get; set; } =
        string.Empty;

    public int SmtpPort { get; set; } =
        587;

    public SmtpSecurityMode SmtpSecurity { get; set; } =
        SmtpSecurityMode.StartTls;

    public string EwsMailboxAddress { get; set; } =
        string.Empty;

    public bool UseAutodiscover { get; set; } =
        true;

    public string EwsServiceUrl { get; set; } =
        string.Empty;
}
namespace Swn.Workflow.Infrastructure;

public sealed class SmtpSettings
{
    public bool Enabled { get; set; }

    public string Host { get; set; } =
        string.Empty;

    public int Port { get; set; } =
        587;

    public SmtpSecurityMode Security { get; set; } =
        SmtpSecurityMode.StartTls;

    public string FromAddress { get; set; } =
        string.Empty;

    public string FromName { get; set; } =
        "SWN Workflow";

    public string Username { get; set; } =
        string.Empty;

    public string Password { get; set; } =
        string.Empty;
}
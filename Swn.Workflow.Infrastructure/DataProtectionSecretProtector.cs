using Microsoft.AspNetCore.DataProtection;
using Swn.Workflow.Application;

namespace Swn.Workflow.Infrastructure;

public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private const string Purpose =
        "Swn.Workflow.TaskInstanceSecrets.v1";

    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(
        IDataProtectionProvider dataProtectionProvider)
    {
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);

        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Protect(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        return _protector.Protect(plainText);
    }

    public string Unprotect(string protectedText)
    {
        ArgumentNullException.ThrowIfNull(protectedText);

        return _protector.Unprotect(protectedText);
    }
}
using Microsoft.AspNetCore.Identity;

namespace Swn.Workflow.Infrastructure;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
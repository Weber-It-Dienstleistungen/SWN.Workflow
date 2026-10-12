
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;
using Swn.Workflow.Infrastructure;
using Swn.Workflow.Web.Components;
using Swn.Workflow.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Der Schluesselring muss ausserhalb der lokalen
// Entwicklungsumgebung dauerhaft gespeichert werden.
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("Swn.Workflow");

if (!builder.Environment.IsDevelopment())
{
    var keyDirectory = builder.Configuration[
        "Workflow:DataProtectionKeysPath"];

    if (string.IsNullOrWhiteSpace(keyDirectory) ||
        !Path.IsPathFullyQualified(keyDirectory))
    {
        throw new InvalidOperationException(
            "Fuer den Serverbetrieb muss " +
            "Workflow:DataProtectionKeysPath auf ein absolutes, " +
            "dauerhaftes und geschuetztes Verzeichnis zeigen.");
    }

    dataProtection.PersistKeysToFileSystem(
        new DirectoryInfo(keyDirectory));
}

builder.Services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

builder.Services.AddScoped<IWorkflowEngine, WorkflowEngine>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IWorkflowDetailService, WorkflowDetailService>();
builder.Services.AddScoped<IWorkflowTaskService, WorkflowTaskService>();
builder.Services.AddScoped<IMyTaskService, MyTaskService>();
builder.Services.AddScoped<IUserDirectoryService, UserDirectoryService>();
builder.Services.AddScoped<IUserAdministrationService, UserAdministrationService>();
builder.Services.AddScoped<IWorkflowNotificationService, WorkflowNotificationService>();
builder.Services.AddScoped<IWorkflowNotificationDispatcher, WorkflowNotificationDispatcher>();
builder.Services.AddScoped<IWorkflowPdfExportService, WorkflowPdfExportService>();

builder.Services.AddScoped<IEmailConfigurationService, EmailConfigurationService>();
builder.Services.AddScoped<IExchangeEwsConnectionTester, ExchangeEwsConnectionTester>();

builder.Services.AddScoped<SmtpEmailSender>();
builder.Services.AddScoped<ExchangeEwsEmailSender>();
builder.Services.AddScoped<IEmailSender, ConfiguredEmailSender>();

builder.Services.AddHostedService<WorkflowNotificationBackgroundService>();

var connectionString = builder.Configuration
    .GetConnectionString("WorkflowDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'WorkflowDatabase' was not found.");

builder.Services.AddDbContextFactory<WorkflowDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = false;

        // Development und Demo behalten die bisherigen
        // Passwortanforderungen fuer unsere Testbenutzer.
        options.Password.RequiredLength =
            builder.Environment.IsDevelopment() ||
            builder.Environment.IsEnvironment("Demo") ? 8 : 12;

        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<WorkflowDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Swn.Workflow.Authentication";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

    options.LoginPath = "/login";
    options.AccessDeniedPath = "/not-found";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = false;
});

// Forwarded Headers nur vom lokalen Reverse Proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
    options.ForwardLimit = 1;
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

app.UseForwardedHeaders();

// Lokal: /
// Demo-Server: /workflow
// Spaeterer Intranetserver: frei konfigurierbar
var pathBase = app.Configuration["Workflow:PathBase"]?.Trim();

if (!string.IsNullOrEmpty(pathBase) && pathBase != "/")
{
    if (!pathBase.StartsWith('/') ||
        pathBase.Contains('?') ||
        pathBase.Contains('#') ||
        pathBase.Contains("//", StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "Workflow:PathBase muss ein gueltiger URL-Pfad sein, " +
            "beispielsweise /workflow.");
    }

    app.UsePathBase(pathBase.TrimEnd('/'));
}

app.UseRouting();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// Anmeldung
app.MapPost(
    "/account/login",
    async (
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAntiforgery antiforgery) =>
    {
        var loginUrl = $"{httpContext.Request.PathBase}/login";

        if (!await antiforgery.IsRequestValidAsync(httpContext))
        {
            return Results.Redirect(loginUrl);
        }

        var form = await httpContext.Request.ReadFormAsync();

        var userName = form["userName"].ToString().Trim();
        var password = form["password"].ToString();

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password))
        {
            return Results.Redirect(loginUrl);
        }

        var user = await userManager.FindByNameAsync(userName);

        if (user is null || !user.IsActive)
        {
            return Results.Redirect(loginUrl);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Results.Redirect(loginUrl);
        }

        return Results.Redirect(
            $"{httpContext.Request.PathBase}/");
    });

// Abmeldung
app.MapPost(
    "/account/logout",
    async (
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager,
        IAntiforgery antiforgery) =>
    {
        if (!await antiforgery.IsRequestValidAsync(httpContext))
        {
            return Results.BadRequest();
        }

        await signInManager.SignOutAsync();

        return Results.Redirect(
            $"{httpContext.Request.PathBase}/login");
    });

// PDF-Export
app.MapGet(
        "/workflow/{workflowInstanceId:guid}/pdf",
        async (
            Guid workflowInstanceId,
            ClaimsPrincipal user,
            IWorkflowPdfExportService pdfExportService,
            CancellationToken cancellationToken) =>
        {
            var userId = user.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var result =
                await pdfExportService.CreateForInitiatorAsync(
                    workflowInstanceId,
                    userId,
                    cancellationToken);

            if (result is null)
            {
                return Results.NotFound();
            }

            return Results.File(
                result.Content,
                "application/pdf",
                result.FileName);
        })
    .RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Datenbank und Initialisierung
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<WorkflowDbContext>>();

    await using var db = await dbFactory.CreateDbContextAsync();

    if (!await db.Database.CanConnectAsync())
    {
        throw new InvalidOperationException(
            "Connection to the workflow database could not be established.");
    }

    // Datenbankschema muss zuvor per EF-Migration
    // erstellt worden sein.
    await WorkflowSeedData.InitializeAsync(dbFactory);
    await WorkflowGraphSeedData.InitializeAsync(dbFactory);
    await TaskFieldSeedData.InitializeAsync(dbFactory);

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    if (app.Environment.IsDevelopment() ||
        app.Environment.IsEnvironment("Demo"))
    {
        // Bisherige Testkonten bleiben in Development
        // und im expliziten Demo-Modus erhalten.
        await IdentitySeedData.InitializeAsync(
            userManager,
            roleManager);
    }
    else
    {
        // Produktivmodus: Rollen initialisieren.
        var roles = new[]
        {
            "SUPERVISOR",
            "HR",
            "IT",
            "ORGANIZATION",
            "TELENEC",
            "EXECUTIVE_SECRETARIAT",
            "TECHNICAL_MANAGEMENT",
            "WAREHOUSE",
            "INFORMATION_SECURITY",
            "CONTROL_SYSTEM",
            "USER_ADMIN"
        };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(
                    new IdentityRole(roleName));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Die Benutzerrolle '{roleName}' konnte nicht " +
                        "angelegt werden: " +
                        string.Join(
                            "; ",
                            roleResult.Errors.Select(
                                x => x.Description)));
                }
            }
        }

        // Datenbestände mit bekanntem Demo-Passwort
        // duerfen nicht produktiv verwendet werden.
        var existingUsers =
            await userManager.Users.ToListAsync();

        foreach (var existingUser in existingUsers)
        {
            if (await userManager.CheckPasswordAsync(
                existingUser,
                "Demo1234"))
            {
                throw new InvalidOperationException(
                    "Mindestens ein Benutzer hat noch das bekannte " +
                    "Demo-Initialpasswort. Produktivstart gesperrt.");
            }
        }

        var administrators =
            await userManager.GetUsersInRoleAsync(
                "USER_ADMIN");

        if (!administrators.Any(x => x.IsActive))
        {
            var bootstrapUserName = builder.Configuration[
                "SWN_WORKFLOW_BOOTSTRAP_USERNAME"];

            var bootstrapPassword = builder.Configuration[
                "SWN_WORKFLOW_BOOTSTRAP_PASSWORD"];

            if (string.IsNullOrWhiteSpace(bootstrapUserName) ||
                string.IsNullOrWhiteSpace(bootstrapPassword))
            {
                throw new InvalidOperationException(
                    "Erster Produktivstart: " +
                    "SWN_WORKFLOW_BOOTSTRAP_USERNAME und " +
                    "SWN_WORKFLOW_BOOTSTRAP_PASSWORD muessen gesetzt sein.");
            }

            if (await userManager.FindByNameAsync(
                bootstrapUserName) is not null)
            {
                throw new InvalidOperationException(
                    "Der Bootstrap-Benutzername ist bereits vergeben.");
            }

            var bootstrapAdmin = new ApplicationUser
            {
                UserName = bootstrapUserName,
                DisplayName = "Workflow-Administration",
                IsActive = true,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            var createResult =
                await userManager.CreateAsync(
                    bootstrapAdmin,
                    bootstrapPassword);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Bootstrap-Administration konnte nicht erstellt werden: " +
                    string.Join(
                        "; ",
                        createResult.Errors.Select(
                            x => x.Description)));
            }

            foreach (var role in new[]
            {
                "USER_ADMIN",
                "ORGANIZATION"
            })
            {
                var assignResult =
                    await userManager.AddToRoleAsync(
                        bootstrapAdmin,
                        role);

                if (!assignResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Rolle '{role}' konnte nicht zugewiesen werden: " +
                        string.Join(
                            "; ",
                            assignResult.Errors.Select(
                                x => x.Description)));
                }
            }
        }
    }
}

app.Run();

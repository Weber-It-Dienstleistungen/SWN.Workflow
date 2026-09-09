using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Swn.Workflow.Application;
using Swn.Workflow.Infrastructure;
using Swn.Workflow.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDataProtection()
    .SetApplicationName("Swn.Workflow");

builder.Services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

builder.Services.AddScoped<IWorkflowEngine, WorkflowEngine>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IWorkflowDetailService, WorkflowDetailService>();
builder.Services.AddScoped<IWorkflowTaskService, WorkflowTaskService>();

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

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<WorkflowDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
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

app.MapPost(
    "/account/login",
    async (
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager,
        IAntiforgery antiforgery) =>
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        var form =
            await httpContext.Request.ReadFormAsync();

        var userName =
            form["userName"].ToString().Trim();

        var password =
            form["password"].ToString();

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password))
        {
            return Results.Redirect("/login");
        }

        var result =
            await signInManager.PasswordSignInAsync(
                userName,
                password,
                isPersistent: false,
                lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Results.Redirect("/login");
        }

        return Results.Redirect("/");
    });

app.MapPost(
    "/account/logout",
    async (
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager,
        IAntiforgery antiforgery) =>
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        await signInManager.SignOutAsync();

        return Results.Redirect("/login");
    });

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<WorkflowDbContext>>();

    await using var db =
        await dbFactory.CreateDbContextAsync();

    if (!await db.Database.CanConnectAsync())
    {
        throw new InvalidOperationException(
            "Connection to the workflow database could not be established.");
    }

    await WorkflowSeedData.InitializeAsync(dbFactory);

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    await IdentitySeedData.InitializeAsync(
        userManager,
        roleManager);
}

app.Run();
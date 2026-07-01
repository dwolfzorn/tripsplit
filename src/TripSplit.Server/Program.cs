using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TripSplit.Server.Data;
using TripSplit.Server.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Turns unhandled exceptions (DB errors, JSON deserialize failures, etc.)
// into a consistent ProblemDetails (RFC 7807) JSON response instead of a raw
// 500 with no structured body, in every environment - not just Development.
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<TripDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<ITripRepository, SqliteTripRepository>();
builder.Services.AddScoped<IShareLinkRepository, SqliteShareLinkRepository>();

var dataProtectionKeyPath = builder.Configuration["DataProtection:KeyPath"]
    ?? Path.Combine(AppContext.BaseDirectory, "keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath))
    .SetApplicationName("TripSplit");

// RequireConfirmedAccount defaults to false because no email sender is
// configured yet - flipping this on later (once an IEmailSender<ApplicationUser>
// is wired up) is a one-line config change. Until then, /forgotPassword issues a
// reset token but nothing delivers it to the user - self-service password
// recovery does not work; a manual UserManager.ResetPasswordAsync call is the
// only recovery path in the meantime.
var requireConfirmedAccount = builder.Configuration.GetValue("Auth:RequireConfirmedAccount", false);

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        // Pinned explicitly (rather than left as Identity's implicit
        // defaults) so the Register page's live requirements checklist can't
        // silently drift out of sync with what the server actually enforces.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredUniqueChars = 1;
    })
    .AddEntityFrameworkStores<TripDbContext>()
    .AddApiEndpoints();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TripDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth").MapIdentityApi<ApplicationUser>();

// Identity's built-in /register only accepts email+password - it has no
// concept of FirstName/LastName. Rather than collide with MapIdentityApi's
// own /api/auth/register route (two endpoints with an identical template and
// HTTP method are an ambiguous match at request time, not a first-registered-
// wins situation), this is mapped under a distinct path and the client calls
// this one instead of the built-in /register.
app.MapPost("/api/auth/register-with-name", async (
    RegisterRequest request,
    UserManager<ApplicationUser> userManager) =>
{
    var user = new ApplicationUser
    {
        UserName = request.Email,
        Email = request.Email,
        FirstName = request.FirstName,
        LastName = request.LastName
    };
    var result = await userManager.CreateAsync(user, request.Password);
    return result.Succeeded
        ? Results.Ok()
        : Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
});

app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});
app.MapGet("/api/auth/me", async (HttpContext ctx, UserManager<ApplicationUser> userManager) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true) return Results.Unauthorized();

    var user = await userManager.GetUserAsync(ctx.User);
    return user is null
        ? Results.Unauthorized()
        : Results.Ok(new { email = user.Email, firstName = user.FirstName, lastName = user.LastName });
});

app.MapPost("/api/auth/change-password", async (
    ChangePasswordRequest request,
    HttpContext ctx,
    UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.GetUserAsync(ctx.User);
    if (user is null) return Results.Unauthorized();

    var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
    return result.Succeeded
        ? Results.Ok()
        : Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
}).RequireAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

record RegisterRequest(string Email, string Password, string FirstName, string LastName);
record ChangePasswordRequest(string CurrentPassword, string NewPassword);

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

app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

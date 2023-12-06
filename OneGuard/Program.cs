using System.Text.Json;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneGuard;
using OneGuard.Core;
using OneGuard.Core.Services;
using OneGuard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel((_, options) => { options.ListenAnyIP(7030, _ => { }); });

builder.Services.AddHealthChecks();
builder.Services.AddCors();
builder.Services.AddAuthorization();
builder.Services.AddFastEndpoints();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ISecretService, SecretService>();
builder.Services.AddScoped<IOtpRequest, OtpRequestService>();
builder.Services.TryAddSingleton<IHashService>(_ => new HmacHashingService(HashingType.HMACSHA384, 6));
builder.Services.TryAddSingleton<ExceptionHandlerMiddleware>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpClient("Bellman", c => { c.BaseAddress = new Uri(builder.Configuration.GetSection("Bellman:BaseUrl").Value ?? throw new ArgumentNullException("Enter Bellman:BaseUrl")); });

var connectionString = builder.Configuration.GetConnectionString("Default") ??
                       throw new ArgumentNullException("connectionString", "Enter 'Default' connection string in appsettings.json");
builder.Services.AddDbContextPool<ApplicationDbContext>(option => option.UseNpgsql(connectionString),poolSize:200);

builder.Services.SwaggerDocument(settings =>
{
    settings.DocumentSettings = generatorSettings =>
    {
        generatorSettings.Title = "OneGuard - WebApi";
        generatorSettings.DocumentName = "v1";
        generatorSettings.Version = "v1";
    };
    settings.EnableJWTBearerAuth = false;
    settings.MaxEndpointVersion = 1;
});


var app = builder.Build();

app.UseCors(b => b.AllowAnyHeader()
    .AllowAnyMethod()
    .SetIsOriginAllowed(_ => true)
    .AllowCredentials());
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseHealthChecks("/health");
app.UseFastEndpoints(config =>
{
    config.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    config.Endpoints.RoutePrefix = "api";
    config.Versioning.Prefix = "v";
    config.Versioning.PrependToRoute = true;
});


// if (app.Environment.IsDevelopment())
// {
app.UseOpenApi();
app.UseSwaggerUi3(s => s.ConfigureDefaults());
// }

using var serviceScope = app.Services.GetService<IServiceScopeFactory>()?.CreateScope();
if (serviceScope == null) return;
try
{
    var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}
catch (Exception e)
{
    Console.WriteLine(e);
    // ignored
}

await app.RunAsync();
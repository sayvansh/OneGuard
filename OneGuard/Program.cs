using System.Text.Json;
using Application;
using Core.Hashing;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneGuard;
using OneGuard.Hashing;
using OneGuard.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((_, options) =>
{
    options.ListenAnyIP(7030, _ => { });
    // options.ListenAnyIP(5122, listenOptions =>
    // {
    //     listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    //     listenOptions.UseHttps();
    // });
});
builder.Services.AddHealthChecks();
builder.Services.AddCors();

builder.Services.AddAuthorization();

builder.Services.AddFastEndpoints();

builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ISecretService, SecretService>();
builder.Services.TryAddSingleton<IHashService>(_ => new HmacHashingService(HashingType.HMACSHA384, 6));
builder.Services.TryAddSingleton<ExceptionHandlerMiddleware>();


builder.Services.AddDistributedMemoryCache();

builder.Services.AddHttpClient("Otp", c => { c.BaseAddress = new Uri(builder.Configuration.GetSection("Otp:BaseUrl").Value ?? throw new ArgumentNullException("Enter OtpService:BaseUrl")); });


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
app.UseMiddleware<ExceptionHandlerMiddleware>();

await app.RunAsync();
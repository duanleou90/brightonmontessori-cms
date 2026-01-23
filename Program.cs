using Microsoft.AspNetCore.HttpOverrides;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers (for reverse proxies like Azure App Service)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// CORS (primarily for the Delivery API / public APIs)
var corsAllowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var corsAllowAnyOrigin =
    builder.Configuration.GetValue("Cors:AllowAnyOrigin", builder.Environment.IsDevelopment());
var corsAllowCredentials =
    builder.Configuration.GetValue("Cors:AllowCredentials", false);

builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicApiCors", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod();

        if (corsAllowAnyOrigin)
        {
            // Note: AllowAnyOrigin cannot be combined with credentials.
            policy.AllowAnyOrigin();
        }
        else if (corsAllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsAllowedOrigins);

            if (corsAllowCredentials)
            {
                policy.AllowCredentials();
            }
        }
    });
});

var umbraco = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers();

// Enable Azure Blob storage providers only when configured.
// This keeps local Docker runs working without requiring Azure storage.
var azureBlobConnString =
    builder.Configuration["Umbraco:Storage:AzureBlob:Media:ConnectionString"];

if (!string.IsNullOrWhiteSpace(azureBlobConnString))
{
    umbraco
        .AddAzureBlobMediaFileSystem()
        .AddAzureBlobImageSharpCache();
}

umbraco.Build();

WebApplication app = builder.Build();

// Use forwarded headers (must be before other middleware)
app.UseForwardedHeaders();

// In Development, force HTTPS scheme for OpenIddict (Umbraco backoffice auth)
// This allows local Docker to work without actual TLS
if (app.Environment.IsDevelopment())
{
    app.Use((context, next) =>
    {
        context.Request.Scheme = "https";
        return next();
    });
}

await app.BootUmbracoAsync();

// Apply CORS only to public API routes (avoid opening the backoffice cross-origin)
app.UseWhen(
    ctx =>
        ctx.Request.Path.StartsWithSegments("/umbraco/delivery/api") ||
        ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseCors("PublicApiCors"));

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();

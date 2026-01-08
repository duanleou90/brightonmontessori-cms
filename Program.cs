using Microsoft.AspNetCore.HttpOverrides;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers (for reverse proxies like Azure App Service)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var umbraco = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
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

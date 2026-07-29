using SAF;

// El detalle de cada bloque vive en Extensions/ (SafBuilderExtensions y
// SafPipelineExtensions), un método por concern y en este mismo orden.
var builder = WebApplication.CreateBuilder(args);

builder.AddPresentation()
       .AddDatabases()
       .AddSharedCookieAuth()
       .AddApplicationServices()
       .AddHttpsHardening();

var app = builder.Build();

app.UseSafPipeline()
   .MapSafEndpoints()
   .StartDatabaseWarmup();

await app.RunAsync();

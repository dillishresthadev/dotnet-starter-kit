var builder = DistributedApplication.CreateBuilder(args);

// Read configuration (appsettings.*) first, then fall back to environment variables, then to sensible defaults.
var config = builder.Configuration;
var dbProvider = config["DatabaseOptions:Provider"]
                 ?? Environment.GetEnvironmentVariable("DatabaseOptions__Provider")
                 ?? "MSSQL"; // default to MSSQL for development

// Only provision containers for providers we explicitly support here. For PostgreSQL we create a container.
// For MSSQL we expect the developer to provide a connection string (local SQL server or container).

// Note: do not use dynamic types here; create postgres resource only within the branch so extension method
// dispatch remains static and extension methods work correctly.

var redis = builder.AddRedis("redis").WithDataVolume("fsh-redis-data");

// Build API project configuration depending on chosen DB provider
if (dbProvider.Equals("POSTGRESQL", StringComparison.OrdinalIgnoreCase))
{
    // Postgres container + database
    var pg = builder.AddPostgres("postgres").WithDataVolume("fsh-postgres-data").AddDatabase("fsh");

    var apiProject = builder.AddProject<Projects.Playground_Api>("playground-api")
        .WithReference(pg)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Endpoint", "https://localhost:4317")
        .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Protocol", "grpc")
        .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Enabled", "true")
        .WithEnvironment("DatabaseOptions__Provider", dbProvider)
        .WithEnvironment("DatabaseOptions__ConnectionString", pg.Resource.ConnectionStringExpression)
        .WithEnvironment("DatabaseOptions__MigrationsAssembly", "FSH.Playground.Migrations.PostgreSQL")
        .WithReference(redis)
        .WithEnvironment("CachingOptions__Redis", redis.Resource.ConnectionStringExpression)
        .WithEnvironment("CachingOptions__EnableSsl", "true")
        .WaitFor(pg)
        .WaitFor(redis);
}
else
{
    // MSSQL or other providers: do not provision Postgres container. Use provided MSSQL connection string or a sensible default.
    // Prefer connection string from appsettings.Development.json, then environment variable, then fallback default.
    var mssqConn = config["DatabaseOptions:ConnectionString"]
                   ?? Environment.GetEnvironmentVariable("DatabaseOptions__ConnectionString")
                   ?? "Server=localhost;Database=FSH_Playground;Trusted_Connection=True;TrustServerCertificate=True;";

    var apiProject = builder.AddProject<Projects.Playground_Api>("playground-api")
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Endpoint", "https://localhost:4317")
        .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Protocol", "grpc")
        .WithEnvironment("OpenTelemetryOptions__Exporter__Otlp__Enabled", "true")
        .WithEnvironment("DatabaseOptions__Provider", dbProvider)
        .WithEnvironment("DatabaseOptions__ConnectionString", mssqConn)
        .WithEnvironment("DatabaseOptions__MigrationsAssembly", dbProvider.Equals("MSSQL", StringComparison.OrdinalIgnoreCase)
            ? "FSH.Playground.Migrations.MSSQL"
            : "FSH.Playground.Migrations.PostgreSQL")
        .WithReference(redis)
        .WithEnvironment("CachingOptions__Redis", redis.Resource.ConnectionStringExpression)
        .WithEnvironment("CachingOptions__EnableSsl", "true")
        .WaitFor(redis);
}

builder.AddProject<Projects.Playground_Blazor>("playground-blazor");

await builder.Build().RunAsync();

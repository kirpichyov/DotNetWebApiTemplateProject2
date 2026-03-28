var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", "postgres_local", secret: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithImage("postgres", "18-alpine")
    .WithDataVolume("sample-project-postgres-data")
    .WithEnvironment("PGDATA", "/var/lib/postgresql/data")
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("DatabaseContext", databaseName: "sample_project_aspire");

builder.AddProject<Projects.SampleProject_Api>("api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Aspire")
    .WithReference(db)
    .WaitFor(db);

await builder.Build().RunAsync();

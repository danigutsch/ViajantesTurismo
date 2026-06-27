const string DatabaseServerResourceName = "postgres";
const string DatabaseResourceName = "eventstore";
const string PostgresImageTag = "17.6";

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddPostgres(DatabaseServerResourceName)
    .WithImageTag(PostgresImageTag);
_ = databaseServer.AddDatabase(DatabaseResourceName);

await builder.Build().RunAsync();

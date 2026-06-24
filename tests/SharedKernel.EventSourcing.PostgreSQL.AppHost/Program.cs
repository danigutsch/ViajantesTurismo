const string DatabaseServerResourceName = "postgres";
const string DatabaseResourceName = "eventstore";

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddPostgres(DatabaseServerResourceName);
_ = databaseServer.AddDatabase(DatabaseResourceName);

await builder.Build().RunAsync();

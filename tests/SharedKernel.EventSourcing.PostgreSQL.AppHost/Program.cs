using SharedKernel.EventSourcing.PostgreSQL.AppHost;

const string DatabaseServerResourceName = "postgres";
const string DatabaseResourceName = "eventstore";

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddDatabaseServer(DatabaseServerResourceName);
_ = databaseServer.AddDatabase(DatabaseResourceName);

await builder.Build().RunAsync();

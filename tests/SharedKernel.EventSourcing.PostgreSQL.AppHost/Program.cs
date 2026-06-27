const string DatabaseServerResourceName = "postgres";
const string DatabaseResourceName = "eventstore";
const string PostgresImageDigest = "00bc86618629af00d2937fdc5a5d63db3ff8450acf52f0636ec813c7f4902929";

var builder = DistributedApplication.CreateBuilder(args);

var databaseServer = builder.AddPostgres(DatabaseServerResourceName)
    .WithImageSHA256(PostgresImageDigest);
_ = databaseServer.AddDatabase(DatabaseResourceName);

await builder.Build().RunAsync();

namespace ViajantesTurismo.Admin.Infrastructure;

internal interface ISeeder
{
    Task Seed(CancellationToken ct);
}

namespace ViajantesTurismo.Admin.Infrastructure;

internal interface ISeeder
{
    void Seed();
    Task Seed(CancellationToken ct);
}

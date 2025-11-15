namespace ViajantesTurismo.Admin.Application;

/// <summary>
/// Defines a contract for data seeders to populate initial data.
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// Seeds the data asynchronously.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task Seed(CancellationToken ct);
}

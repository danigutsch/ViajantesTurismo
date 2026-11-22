using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Customers;

/// <summary>Observes the import transaction and can inject a failure around the real save.</summary>
internal sealed class CustomerImportSaveProbe(
    AdminWriteDbContext dbContext,
    Func<Task>? beforeSave = null,
    Action<int>? afterSave = null) : IUnitOfWork
{
    /// <summary>Gets whether the command was already in an explicit transaction at save time.</summary>
    public bool UsedTransaction
    {
        get; private set;
    }

    /// <summary>Gets the number of import save attempts.</summary>
    public int SaveCalls
    {
        get; private set;
    }

    /// <inheritdoc />
    public async Task SaveEntities(CancellationToken ct)
    {
        SaveCalls++;
        UsedTransaction = dbContext.Database.CurrentTransaction is not null;
        if (beforeSave is not null)
        {
            await beforeSave();
        }

        await dbContext.SaveEntities(ct);
        afterSave?.Invoke(SaveCalls);
    }
}

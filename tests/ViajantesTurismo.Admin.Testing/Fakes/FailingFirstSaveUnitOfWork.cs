using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.Testing.Fakes;

public sealed class FailingFirstSaveUnitOfWork(Exception exception, IUnitOfWork inner) : IUnitOfWork
{
    private bool failed;

    public Task SaveEntities(CancellationToken ct)
    {
        if (failed)
        {
            return inner.SaveEntities(ct);
        }

        failed = true;
        throw exception;
    }
}

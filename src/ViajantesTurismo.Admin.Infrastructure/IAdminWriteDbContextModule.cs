using Microsoft.EntityFrameworkCore;

namespace ViajantesTurismo.Admin.Infrastructure;

internal interface IAdminWriteDbContextModule
{
    void Configure(DbContextOptionsBuilder optionsBuilder);

    void Configure(ModelBuilder modelBuilder);
}

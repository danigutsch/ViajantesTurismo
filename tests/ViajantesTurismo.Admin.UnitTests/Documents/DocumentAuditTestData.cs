using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

internal static class DocumentAuditTestData
{
    public static DocumentAuditContext CreateContext() => new(
        "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
        "9a3ca841b4354928861c660a6e4e1b99");
}

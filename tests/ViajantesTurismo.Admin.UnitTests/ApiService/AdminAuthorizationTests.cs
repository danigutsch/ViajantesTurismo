using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.ApiService;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class AdminAuthorizationTests
{
    [Fact]
    public void Role_mapping_is_allowlisted_and_case_sensitive()
    {
        // Arrange
        string[] expectedRoles = ["Admin", "Operator"];

        // Act
        var permissionsByRole = AdminAuthorization.PermissionsByRole;

        // Assert
        permissionsByRole.Keys.Except(expectedRoles, StringComparer.Ordinal).ShouldBeEmpty();
        expectedRoles.Except(permissionsByRole.Keys, StringComparer.Ordinal).ShouldBeEmpty();
        permissionsByRole.ContainsKey("admin").ShouldBeFalse();
        permissionsByRole.ContainsKey("Auditor").ShouldBeFalse();
    }

    [Fact]
    public void Admin_role_receives_every_admin_api_permission()
    {
        // Arrange
        string[] expectedPermissions =
        [
            AdminAuthorization.BookingRead,
            AdminAuthorization.BookingWrite,
            AdminAuthorization.BookingDelete,
            AdminAuthorization.CustomerImport,
            AdminAuthorization.CustomerRead,
            AdminAuthorization.CustomerSensitiveRead,
            AdminAuthorization.CustomerWrite,
            AdminAuthorization.DocumentManage,
            AdminAuthorization.DocumentationRead,
            AdminAuthorization.PaymentRead,
            AdminAuthorization.PaymentWrite,
            AdminAuthorization.TourRead,
            AdminAuthorization.TourWrite
        ];

        // Act
        var actualPermissions = AdminAuthorization.PermissionsByRole["Admin"];

        // Assert
        actualPermissions.Except(expectedPermissions, StringComparer.Ordinal).ShouldBeEmpty();
        expectedPermissions.Except(actualPermissions, StringComparer.Ordinal).ShouldBeEmpty();
    }

    [Fact]
    public void Operator_role_excludes_destructive_and_customer_permissions()
    {
        // Arrange
        string[] expectedPermissions =
        [
            AdminAuthorization.BookingRead,
            AdminAuthorization.BookingWrite,
            AdminAuthorization.DocumentationRead,
            AdminAuthorization.PaymentRead,
            AdminAuthorization.PaymentWrite,
            AdminAuthorization.TourRead,
            AdminAuthorization.TourWrite
        ];

        // Act
        var actualPermissions = AdminAuthorization.PermissionsByRole["Operator"];

        // Assert
        actualPermissions.Except(expectedPermissions, StringComparer.Ordinal).ShouldBeEmpty();
        expectedPermissions.Except(actualPermissions, StringComparer.Ordinal).ShouldBeEmpty();
        actualPermissions.ShouldNotContain(AdminAuthorization.BookingDelete);
        actualPermissions.ShouldNotContain(AdminAuthorization.CustomerImport);
        actualPermissions.ShouldNotContain(AdminAuthorization.CustomerRead);
        actualPermissions.ShouldNotContain(AdminAuthorization.CustomerSensitiveRead);
        actualPermissions.ShouldNotContain(AdminAuthorization.CustomerWrite);
        actualPermissions.ShouldNotContain(AdminAuthorization.DocumentManage);
    }
}

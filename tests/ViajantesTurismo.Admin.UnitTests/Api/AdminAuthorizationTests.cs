using ViajantesTurismo.Admin.Api;

namespace ViajantesTurismo.Admin.UnitTests.Api;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
public sealed class AdminAuthorizationTests
{
    [Fact]
    public void Role_mapping_is_allowlisted_and_case_sensitive()
    {
        // Arrange
        string[] expectedRoles = ["Admin", "Operator", "Reader", "OperationsScheduleController"];

        // Act
        var permissionsByRole = AdminAuthorization.PermissionsByRole;

        // Assert
        permissionsByRole.Keys.Except(expectedRoles, StringComparer.Ordinal).ShouldBeEmpty();
        expectedRoles.Except(permissionsByRole.Keys, StringComparer.Ordinal).ShouldBeEmpty();
        permissionsByRole.ContainsKey("admin").ShouldBeFalse();
        permissionsByRole.ContainsKey("Auditor").ShouldBeFalse();
    }

    [Fact]
    public void Admin_role_can_read_and_control_the_schedule()
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
            AdminAuthorization.DocumentHold,
            AdminAuthorization.DocumentationRead,
            AdminAuthorization.OperationsRead,
            AdminAuthorization.OperationsScheduleRead,
            AdminAuthorization.OperationsScheduleControl,
            AdminAuthorization.PaymentManage,
            AdminAuthorization.PaymentRead,
            AdminAuthorization.PaymentWrite,
            AdminAuthorization.TourRead,
            AdminAuthorization.TourDelete,
            AdminAuthorization.TourWrite
        ];

        // Act
        var actualPermissions = AdminAuthorization.PermissionsByRole["Admin"];

        // Assert
        actualPermissions.Except(expectedPermissions, StringComparer.Ordinal).ShouldBeEmpty();
        expectedPermissions.Except(actualPermissions, StringComparer.Ordinal).ShouldBeEmpty();
    }

    [Fact]
    public void Schedule_controller_role_receives_only_schedule_read_and_control()
    {
        // Arrange
        string[] expectedPermissions = [AdminAuthorization.OperationsScheduleRead, AdminAuthorization.OperationsScheduleControl];

        // Act
        var actualPermissions = AdminAuthorization.PermissionsByRole["OperationsScheduleController"];

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
        actualPermissions.ShouldNotContain(AdminAuthorization.DocumentHold);
        actualPermissions.ShouldNotContain(AdminAuthorization.PaymentManage);
    }

    [Fact]
    public void Reader_role_receives_only_the_read_permissions_of_the_collection_endpoints()
    {
        // Arrange
        string[] expectedPermissions =
        [
            AdminAuthorization.BookingRead,
            AdminAuthorization.CustomerRead,
            AdminAuthorization.CustomerSensitiveRead,
            AdminAuthorization.PaymentRead,
            AdminAuthorization.TourRead
        ];

        // Act
        var actualPermissions = AdminAuthorization.PermissionsByRole["Reader"];

        // Assert
        actualPermissions.Except(expectedPermissions, StringComparer.Ordinal).ShouldBeEmpty();
        expectedPermissions.Except(actualPermissions, StringComparer.Ordinal).ShouldBeEmpty();
        actualPermissions.ShouldNotContain(AdminAuthorization.BookingWrite);
        actualPermissions.ShouldNotContain(AdminAuthorization.BookingDelete);
        actualPermissions.ShouldNotContain(AdminAuthorization.CustomerWrite);
        actualPermissions.ShouldNotContain(AdminAuthorization.CustomerImport);
        actualPermissions.ShouldNotContain(AdminAuthorization.DocumentManage);
        actualPermissions.ShouldNotContain(AdminAuthorization.DocumentHold);
        actualPermissions.ShouldNotContain(AdminAuthorization.PaymentManage);
        actualPermissions.ShouldNotContain(AdminAuthorization.PaymentWrite);
        actualPermissions.ShouldNotContain(AdminAuthorization.TourWrite);
    }
}

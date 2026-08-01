using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SharedKernel.Testing;

namespace ViajantesTurismo.Management.WebIntegrationTests;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(SharedKernelTestTraitNames.ScopeName, TestTraits.DatabaseIntegrationScope)]
[Trait(TestTraitNames.HostName, TestTraits.AspireHost)]
[Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
public sealed class ManagementSecurityPersistencePostgreSqlTests
{
    [Fact]
    public async Task Failed_initialization_disposes_the_started_application()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var app = await PostgreSqlManagementSecurityPersistenceScenario.StartApplication(ct);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        Func<Task> initialize = async () =>
            await PostgreSqlManagementSecurityPersistenceScenario.Initialize(app, cancellation.Token);
        _ = await initialize.ShouldThrowAssignableTo<OperationCanceledException>();
        Func<Task> getConnectionString = async () =>
            await PostgreSqlManagementSecurityPersistenceScenario.GetConnectionString(app, ct);
        var exception = await getConnectionString.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldBe("Aspire test application is not initialized.");
    }

    [Fact]
    public async Task Fresh_provider_restores_protected_payload_and_authentication_ticket()
    {
        // Arrange
        const string principalName = "synthetic-security-regression-principal";
        const string markerClaimType = "urn:viajantes:test:security-persistence";
        const string markerClaimValue = "provider-a";
        const string payload = "synthetic-provider-a-payload";
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlManagementSecurityPersistenceScenario.Create(ct);
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, principalName),
            new Claim(markerClaimType, markerClaimValue)
        ],
        CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) },
            CookieAuthenticationDefaults.AuthenticationScheme);

        string protectedPayload;
        string ticketKey;
        await using (var providerA = scenario.CreateHost())
        {
            protectedPayload = providerA.ProtectPayload(payload);
            ticketKey = await providerA.StoreTicket(ticket).WaitAsync(ct);
        }

        // Act
        var keyCount = await scenario.GetDataProtectionKeyCount(ct);
        var ticketCount = await scenario.GetTicketCount(ticketKey, ct);
        var containsPlaintext = await scenario.TicketContainsPlaintext(ticketKey, principalName, ct);
        string unprotectedPayload;
        AuthenticationTicket? restoredTicket;
        await using (var providerB = scenario.CreateHost())
        {
            unprotectedPayload = providerB.UnprotectPayload(protectedPayload);
            restoredTicket = await providerB.RetrieveTicket(ticketKey).WaitAsync(ct);
        }

        await using var providerC = scenario.CreateHost(
            "ViajantesTurismo.Management.WebIntegrationTests.Isolated");
        Func<string> wrongApplicationUnprotect = () => providerC.UnprotectPayload(protectedPayload);
        _ = wrongApplicationUnprotect.ShouldThrow<CryptographicException>();

        // Assert
        keyCount.ShouldBeGreaterThan(0);
        ticketCount.ShouldBe(1);
        containsPlaintext.ShouldBeFalse();
        unprotectedPayload.ShouldBe(payload);
        restoredTicket.ShouldNotBeNull();
        restoredTicket.Principal.Identity.ShouldNotBeNull();
        restoredTicket.Principal.Identity.Name.ShouldBe(principalName);
        restoredTicket.Principal.Claims.ShouldContain(
            claim => claim.Type == markerClaimType && claim.Value == markerClaimValue);
    }
}

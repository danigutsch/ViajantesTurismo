using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using PandaBear.Testing;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.HostedTests.Management.Browser;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Exercises the central customer grid through PostgreSQL, HTTP, and Management prerendering.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.Integration)]
public sealed class CustomerGridPrerenderTests(HostedTestFixture fixture)
{
    [Fact]
    public async Task Large_customer_grid_shows_the_database_total_and_reads_only_one_page()
    {
        // Arrange
        const int customerCount = 800;
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        _ = await CustomerOptionPostgreSqlScenario.Seed(database.ConnectionString, customerCount, 40, ct);
        var query = new CustomerOptionQueryProbe();
        await using var adminApi = CustomerOptionPostgreSqlScenario.Host(database.ConnectionString, query);
        using var adminHttp = adminApi.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(adminHttp, CustomerOptionPostgreSqlScenario.Audience, "Reader");
        var client = new CustomersApiClient(adminHttp, NullLogger<CustomersApiClient>.Instance);
        await using var management = await ManagementWebBrowserHost.StartWithClients(ct, customers: client);
        using var browser = new HttpClient { BaseAddress = management.BaseUri, Timeout = TimeSpan.FromSeconds(15) };

        // Act
        var html = await browser.GetStringAsync(new Uri("/customers", UriKind.Relative), ct);
        var customerTotal = html.Split("Total customers: ", StringSplitOptions.None).Last().Split('<')[0];

        // Assert
        customerTotal.ShouldBe("800");
        html.ShouldContain("<!--Blazor:", StringComparison.Ordinal);
        html.ShouldContain("choice-000@example.test", StringComparison.Ordinal);
        html.ShouldContain("choice-009@example.test", StringComparison.Ordinal);
        html.ShouldNotContain("choice-010@example.test", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(html);
        query.Reads.ShouldBe(2);
        query.LimitedReads.ShouldBe(1);
    }

    [Fact]
    public async Task Authorized_customer_page_reaches_records_beyond_the_legacy_client_cap()
    {
        // Arrange
        const int customerCount = 125;
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        _ = await CustomerOptionPostgreSqlScenario.Seed(database.ConnectionString, customerCount, 5, ct);
        var query = new CustomerOptionQueryProbe();
        await using var adminApi = CustomerOptionPostgreSqlScenario.Host(database.ConnectionString, query);
        using var adminHttp = adminApi.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(adminHttp, CustomerOptionPostgreSqlScenario.Audience, "Reader");

        // Act
        using var response = await adminHttp.GetAsync(new Uri("/api/v1/customers/page?page=11&sort=Name", UriKind.Relative), ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        text.ShouldContain("choice-100@example.test", StringComparison.Ordinal);
        text.ShouldContain("choice-109@example.test", StringComparison.Ordinal);
        text.ShouldNotContain("choice-110@example.test", StringComparison.Ordinal);
        query.LimitedReads.ShouldBe(1);
    }

    [Fact]
    public async Task Customer_grid_projects_the_same_fields_for_each_sort_and_keeps_ties_stable_across_pages()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.Databases.Create(ct);
        var customers = await CustomerGridPostgreSqlScenario.Seed(database.ConnectionString, ct);
        var query = new CustomerGridQueryProbe();
        await using var adminApi = CustomerOptionPostgreSqlScenario.Host(database.ConnectionString, query);
        using var adminHttp = adminApi.CreateClient();
        ApiTestAuthentication.ConfigureAuthenticatedClient(adminHttp, CustomerOptionPostgreSqlScenario.Audience, "Reader");
        var client = new CustomersApiClient(adminHttp, NullLogger<CustomersApiClient>.Instance);
        var size = ContractConstants.CustomerListPageSize;
        var nameOrder = customers.OrderBy(customer => customer.PersonalInfo.FirstName, StringComparer.Ordinal)
            .ThenBy(customer => customer.PersonalInfo.LastName, StringComparer.Ordinal);
        var sortCases = new (CustomerPageSort Sort, bool Descending, IOrderedEnumerable<Customer> Expected)[]
        {
            (CustomerPageSort.Name, false, nameOrder),
            (CustomerPageSort.Name, true, customers.OrderByDescending(customer => customer.PersonalInfo.FirstName, StringComparer.Ordinal)
                .ThenByDescending(customer => customer.PersonalInfo.LastName, StringComparer.Ordinal)),
            (CustomerPageSort.Email, false, customers.OrderBy(customer => customer.ContactInfo.Email.Value, StringComparer.Ordinal)),
            (CustomerPageSort.Email, true, customers.OrderByDescending(customer => customer.ContactInfo.Email.Value, StringComparer.Ordinal)),
            (CustomerPageSort.Nationality, false, customers.OrderBy(customer => customer.PersonalInfo.Nationality, StringComparer.Ordinal)),
            (CustomerPageSort.Nationality, true, customers.OrderByDescending(customer => customer.PersonalInfo.Nationality, StringComparer.Ordinal))
        };

        // Act
        var pages = new List<GetCustomerPageDto>();
        foreach (var (sort, descending, _) in sortCases)
        {
            var outcome = await client.GetCustomerPage(2, sort, descending, ct);
            pages.Add(outcome.Value.ShouldNotBeNull());
        }

        var lastPage = await client.GetCustomerPage(int.MaxValue, CustomerPageSort.Name, descending: false, ct);
        using var defaultPage = await adminHttp.GetAsync(new Uri("/api/v1/customers/page?page=malformed", UriKind.Relative), ct);
        var defaultPageBody = await defaultPage.Content.ReadFromJsonAsync<GetCustomerPageDto>(ct);

        // Assert
        for (var index = 0; index < sortCases.Length; index++)
        {
            var expected = sortCases[index].Expected.ThenBy(customer => customer.Id).Skip(size).Take(size)
                .Select(customer => customer.Id);
            pages[index].Total.ShouldBe(customers.Count);
            pages[index].Items.Select(item => item.Id).ShouldBe(expected);
        }

        var allByName = nameOrder.ThenBy(customer => customer.Id).Select(customer => customer.Id).ToArray();
        lastPage.Value.ShouldNotBeNull().Items.Select(item => item.Id).ShouldBe(allByName.Skip(2 * size));
        defaultPage.StatusCode.ShouldBe(HttpStatusCode.OK);
        defaultPageBody.ShouldNotBeNull().Items.Select(item => item.Id).ShouldBe(allByName.Take(size));
        query.LimitedReads.ShouldBe(8);
        query.SummaryReads.ShouldBe(8);
    }
}

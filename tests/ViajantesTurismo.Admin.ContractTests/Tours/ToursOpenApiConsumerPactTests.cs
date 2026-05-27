using System.Net;
using PactNet;
using Xunit;

namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Defines the consumer-driven Pact contract for the published tours OpenAPI document.
/// </summary>
public sealed class ToursOpenApiConsumerPactTests
{
    private readonly IPactBuilderV4 pactBuilder;

    public ToursOpenApiConsumerPactTests()
    {
        var pact = Pact.V4(
            "ViajantesTurismo.Admin.ContractTests",
            "ViajantesTurismo.Admin.ApiService",
            new PactConfig
            {
                PactDir = Path.Combine(GetProjectDirectory(), "Pacts")
            });

        pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task Reads_The_Tours_OpenApi_Document_Through_The_Required_Boundary_Shape()
    {
        // Arrange
        pactBuilder
            .UponReceiving("A request for the tours OpenAPI document")
            .WithRequest(HttpMethod.Get, "/openapi/Tours.json")
            .WithHeader("Accept", "application/json")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(
                new Dictionary<string, object?>
                {
                    ["openapi"] = "3.1.1",
                    ["info"] = new Dictionary<string, object?>
                    {
                        ["title"] = "ViajantesTurismo.Admin.ApiService | tours"
                    },
                    ["paths"] = new Dictionary<string, object?>
                    {
                        ["/tours"] = new Dictionary<string, object?>
                        {
                            ["get"] = new Dictionary<string, object?>
                            {
                                ["operationId"] = "GetTours"
                            },
                            ["post"] = new Dictionary<string, object?>
                            {
                                ["requestBody"] = new Dictionary<string, object?>
                                {
                                    ["content"] = new Dictionary<string, object?>
                                    {
                                        ["application/json"] = new Dictionary<string, object?>
                                        {
                                            ["schema"] = new Dictionary<string, object?>
                                            {
                                                ["$ref"] = "#/components/schemas/CreateTourDto"
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        ["/tours/{id}"] = new Dictionary<string, object?>
                        {
                            ["get"] = new Dictionary<string, object?>
                            {
                                ["operationId"] = "GetTourById"
                            },
                            ["put"] = new Dictionary<string, object?>
                            {
                                ["requestBody"] = new Dictionary<string, object?>
                                {
                                    ["content"] = new Dictionary<string, object?>
                                    {
                                        ["application/json"] = new Dictionary<string, object?>
                                        {
                                            ["schema"] = new Dictionary<string, object?>
                                            {
                                                ["$ref"] = "#/components/schemas/UpdateTourDto"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

        await pactBuilder.VerifyAsync(async context =>
        {
            // Act
            using var httpClient = new HttpClient { BaseAddress = context.MockServerUri };
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            var client = new ToursOpenApiDocumentClient(httpClient);
            var contract = await client.GetContract(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("3.1.1", contract.OpenApiVersion);
            Assert.Equal("ViajantesTurismo.Admin.ApiService | tours", contract.Title);
            Assert.Equal("GetTours", contract.ListToursOperationId);
            Assert.Equal("GetTourById", contract.GetTourByIdOperationId);
            Assert.Equal("#/components/schemas/CreateTourDto", contract.CreateTourSchemaReference);
            Assert.Equal("#/components/schemas/UpdateTourDto", contract.UpdateTourSchemaReference);
        });
    }

    private static string GetProjectDirectory()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(
                currentDirectory.FullName,
                "tests",
                "ViajantesTurismo.Admin.ContractTests",
                "ViajantesTurismo.Admin.ContractTests.csproj");

            if (File.Exists(candidatePath))
            {
                return Path.GetDirectoryName(candidatePath)!;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the Admin contract test project directory.");
    }
}

namespace SharedKernel.RepoConfig.Tests;

public sealed class CiTestProjectSelectorTests
{
    [Fact]
    public void Changed_source_selects_direct_and_transitive_test_dependents()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject("src/Core/Core.csproj");
        repository.AddProject("src/Application/Application.csproj", "src/Core/Core.csproj");
        repository.AddProject("tests/Application.Tests/Application.Tests.csproj", "src/Application/Application.csproj");
        repository.AddProject("tests/Other.Tests/Other.Tests.csproj");
        repository.WriteSlice(
            "fast-validation",
            "tests/Application.Tests/Application.Tests.csproj",
            "tests/Other.Tests/Other.Tests.csproj");

        // Act
        var selection = CiTestProjectSelector.Select(
            repository.RootPath,
            ["src/Core/Service.cs"],
            fullValidation: false);

        // Assert
        selection.BuildRequired.ShouldBeTrue();
        selection.FallbackToFullValidation.ShouldBeFalse();
        selection.SelectedProjectsBySlice["fast-validation"]
            .ShouldHaveSingleItem()
            .ShouldBe("tests/Application.Tests/Application.Tests.csproj");
    }

    [Fact]
    public void Windows_project_reference_paths_are_resolved_on_linux()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject("src/Core/Core.csproj");
        repository.AddProjectWithWindowsReferences(
            "tests/Core.Tests/Core.Tests.csproj",
            "src/Core/Core.csproj");
        repository.WriteSlice("fast-validation", "tests/Core.Tests/Core.Tests.csproj");

        // Act
        var selection = CiTestProjectSelector.Select(
            repository.RootPath,
            ["src/Core/Service.cs"],
            fullValidation: false);

        // Assert
        selection.FallbackToFullValidation.ShouldBeFalse();
        selection.SelectedProjectsBySlice["fast-validation"]
            .ShouldHaveSingleItem()
            .ShouldBe("tests/Core.Tests/Core.Tests.csproj");
    }

    [Fact]
    public void Changed_test_project_selects_only_itself()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject("tests/First.Tests/First.Tests.csproj");
        repository.AddProject("tests/Second.Tests/Second.Tests.csproj");
        repository.WriteSlice(
            "fast-validation",
            "tests/First.Tests/First.Tests.csproj",
            "tests/Second.Tests/Second.Tests.csproj");

        // Act
        var selection = CiTestProjectSelector.Select(
            repository.RootPath,
            ["tests/Second.Tests/SecondTests.cs"],
            fullValidation: false);

        // Assert
        selection.SelectedProjectsBySlice["fast-validation"]
            .ShouldHaveSingleItem()
            .ShouldBe("tests/Second.Tests/Second.Tests.csproj");
    }

    [Fact]
    public void Shared_integration_testing_changes_select_every_dependent_slice()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject("src/SharedKernel/SharedKernel.IntegrationTesting/SharedKernel.IntegrationTesting.csproj");
        repository.AddProject(
            "tests/Provider.Tests/Provider.Tests.csproj",
            "src/SharedKernel/SharedKernel.IntegrationTesting/SharedKernel.IntegrationTesting.csproj");
        repository.AddProject(
            "tests/System.Tests/System.Tests.csproj",
            "src/SharedKernel/SharedKernel.IntegrationTesting/SharedKernel.IntegrationTesting.csproj");
        repository.AddProject(
            "tests/EntityFrameworkCore.Tests/EntityFrameworkCore.Tests.csproj",
            "src/SharedKernel/SharedKernel.IntegrationTesting/SharedKernel.IntegrationTesting.csproj");
        repository.WriteSlice("admin-integration", "tests/Provider.Tests/Provider.Tests.csproj");
        repository.WriteSlice("admin-system", "tests/System.Tests/System.Tests.csproj");
        repository.WriteSlice("mediator-heavy", "tests/EntityFrameworkCore.Tests/EntityFrameworkCore.Tests.csproj");

        // Act
        var selection = CiTestProjectSelector.Select(
            repository.RootPath,
            ["src/SharedKernel/SharedKernel.IntegrationTesting/AspireTestApplication.cs"],
            fullValidation: false);

        // Assert
        selection.SelectedProjectsBySlice["admin-integration"].ShouldHaveSingleItem();
        selection.SelectedProjectsBySlice["admin-system"].ShouldHaveSingleItem();
        selection.SelectedProjectsBySlice["mediator-heavy"].ShouldHaveSingleItem();
    }

    [Fact]
    public void Global_runner_configuration_selects_every_test_project()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject("tests/Fast.Tests/Fast.Tests.csproj");
        repository.AddProject("tests/System.Tests/System.Tests.csproj");
        repository.WriteSlice("fast-validation", "tests/Fast.Tests/Fast.Tests.csproj");
        repository.WriteSlice("admin-system", "tests/System.Tests/System.Tests.csproj");

        // Act
        var selection = CiTestProjectSelector.Select(
            repository.RootPath,
            ["tests/xunit.runner.json"],
            fullValidation: false);

        // Assert
        selection.SelectedProjectsBySlice["fast-validation"].ShouldHaveSingleItem();
        selection.SelectedProjectsBySlice["admin-system"].ShouldHaveSingleItem();
        selection.FallbackToFullValidation.ShouldBeFalse();
    }

    [Fact]
    public void Unknown_non_documentation_path_fails_open_to_every_test_project()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject("tests/Fast.Tests/Fast.Tests.csproj");
        repository.AddProject("tests/System.Tests/System.Tests.csproj");
        repository.WriteSlice("fast-validation", "tests/Fast.Tests/Fast.Tests.csproj");
        repository.WriteSlice("admin-system", "tests/System.Tests/System.Tests.csproj");

        // Act
        var selection = CiTestProjectSelector.Select(
            repository.RootPath,
            ["unowned/runtime-input.json"],
            fullValidation: false);

        // Assert
        selection.FallbackToFullValidation.ShouldBeTrue();
        selection.SelectedProjectsBySlice["fast-validation"].ShouldHaveSingleItem();
        selection.SelectedProjectsBySlice["admin-system"].ShouldHaveSingleItem();
    }

    [Fact]
    public void Changed_project_without_a_test_dependency_fails_open_to_every_test_project()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject("src/Uncovered/Uncovered.csproj");
        repository.AddProject("tests/Fast.Tests/Fast.Tests.csproj");
        repository.AddProject("tests/System.Tests/System.Tests.csproj");
        repository.WriteSlice("fast-validation", "tests/Fast.Tests/Fast.Tests.csproj");
        repository.WriteSlice("admin-system", "tests/System.Tests/System.Tests.csproj");

        // Act
        var selection = CiTestProjectSelector.Select(
            repository.RootPath,
            ["src/Uncovered/Service.cs"],
            fullValidation: false);

        // Assert
        selection.FallbackToFullValidation.ShouldBeTrue();
        selection.SelectedProjectsBySlice["fast-validation"].ShouldHaveSingleItem();
        selection.SelectedProjectsBySlice["admin-system"].ShouldHaveSingleItem();
    }

    [Fact]
    public void Documentation_only_changes_select_no_test_projects()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject("tests/Fast.Tests/Fast.Tests.csproj");
        repository.WriteSlice("fast-validation", "tests/Fast.Tests/Fast.Tests.csproj");

        // Act
        var selection = CiTestProjectSelector.Select(
            repository.RootPath,
            ["docs/ci/main-workflow.md"],
            fullValidation: false);

        // Assert
        selection.BuildRequired.ShouldBeFalse();
        selection.SelectedProjectsBySlice["fast-validation"].ShouldBeEmpty();
    }

    [Fact]
    public void Openapi_api_change_selects_dynamic_tool_tests_and_windows_validation()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject("src/Shared/Shared.csproj");
        repository.AddProject(
            "src/ViajantesTurismo.Admin.ApiService/ViajantesTurismo.Admin.ApiService.csproj",
            "src/Shared/Shared.csproj");
        repository.AddProject("tests/ViajantesTurismo.OpenApi.Tool.Tests/ViajantesTurismo.OpenApi.Tool.Tests.csproj");
        repository.WriteSlice(
            "fast-validation",
            "tests/ViajantesTurismo.OpenApi.Tool.Tests/ViajantesTurismo.OpenApi.Tool.Tests.csproj");

        // Act
        var selection = CiTestProjectSelector.Select(
            repository.RootPath,
            ["src/Shared/Contract.cs"],
            fullValidation: false);

        // Assert
        selection.SelectedProjectsBySlice["fast-validation"]
            .ShouldHaveSingleItem()
            .ShouldBe("tests/ViajantesTurismo.OpenApi.Tool.Tests/ViajantesTurismo.OpenApi.Tool.Tests.csproj");
        selection.OpenApiToolWindowsRequired.ShouldBeTrue();
    }

    [Fact]
    public void Mediator_package_consumption_tests_include_dynamically_packed_projects()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject(
            "src/SharedKernel/SharedKernel.Messaging.IntegrationEvents.SourceGenerator/SharedKernel.Messaging.IntegrationEvents.SourceGenerator.csproj");
        repository.AddProject(
            "tests/SharedKernel.Mediator.PackageConsumptionTests/SharedKernel.Mediator.PackageConsumptionTests.csproj");
        repository.WriteSlice(
            "mediator-heavy",
            "tests/SharedKernel.Mediator.PackageConsumptionTests/SharedKernel.Mediator.PackageConsumptionTests.csproj");

        // Act
        var selection = CiTestProjectSelector.Select(
            repository.RootPath,
            ["src/SharedKernel/SharedKernel.Messaging.IntegrationEvents.SourceGenerator/Generator.cs"],
            fullValidation: false);

        // Assert
        selection.FallbackToFullValidation.ShouldBeFalse();
        selection.SelectedProjectsBySlice["mediator-heavy"]
            .ShouldHaveSingleItem()
            .ShouldBe("tests/SharedKernel.Mediator.PackageConsumptionTests/SharedKernel.Mediator.PackageConsumptionTests.csproj");
    }

    [Fact]
    public void Selected_projects_preserve_manifest_scheduling_order()
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        repository.AddProject("src/Shared/Shared.csproj");
        repository.AddProject("tests/Slow.Tests/Slow.Tests.csproj", "src/Shared/Shared.csproj");
        repository.AddProject("tests/Fast.Tests/Fast.Tests.csproj", "src/Shared/Shared.csproj");
        repository.WriteSlice(
            "fast-validation",
            "tests/Slow.Tests/Slow.Tests.csproj",
            "tests/Fast.Tests/Fast.Tests.csproj");

        // Act
        var selection = CiTestProjectSelector.Select(
            repository.RootPath,
            ["src/Shared/Value.cs"],
            fullValidation: false);

        // Assert
        selection.SelectedProjectsBySlice["fast-validation"][0].ShouldBe("tests/Slow.Tests/Slow.Tests.csproj");
        selection.SelectedProjectsBySlice["fast-validation"][1].ShouldBe("tests/Fast.Tests/Fast.Tests.csproj");
    }
}

namespace ViajantesTurismo.Performance.Tool.Tests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.ScopeName, SharedKernelTestTraitNames.UnitScope)]
public sealed class AdminSmokeCommandTests
{
    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("https://localhost:5001")]
    [InlineData("http://127.0.0.1:5000")]
    [InlineData("https://[::1]:5001")]
    [InlineData("http://host.docker.internal:5000")]
    public void Allows_local_admin_api_targets(string targetText)
    {
        // Arrange
        var target = targetText;

        // Act
        var exception = Record.Exception(() => AdminSmokeCommand.ValidateTarget(target));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public void Rejects_admin_api_targets_with_user_information()
    {
        // Arrange
        const string target = "http://localhost:80@evil.example";

        // Act
        Action act = () => AdminSmokeCommand.ValidateTarget(target);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_API_BASE_URL must not include user information.");
    }

    [Fact]
    public void Rejects_external_admin_api_targets_by_default()
    {
        // Arrange
        const string target = "https://example.com";

        // Act
        Action act = () => AdminSmokeCommand.ValidateTarget(target);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_API_BASE_URL must target localhost, 127.0.0.1, [::1], or host.docker.internal unless VT_K6_ALLOW_EXTERNAL=1 is set.");
    }

    [Fact]
    public void Rejects_system_environment_forwarding()
    {
        // Arrange
        string[] k6Arguments = ["--include-system-env-vars"];

        // Act
        Action act = () => AdminSmokeCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("--include-system-env-vars is not allowed for repository k6 runs.");
    }

    [Fact]
    public void Rejects_controlled_k6_environment_override()
    {
        // Arrange
        string[] k6Arguments = ["-e", "VT_API_BASE_URL=http://evil.example"];

        // Act
        Action act = () => AdminSmokeCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("Do not override repository-controlled k6 environment through -e/--env. Use documented VT_* environment variables before launching the tool.");
    }

    [Fact]
    public void Rejects_insecure_tls_bypass()
    {
        // Arrange
        string[] k6Arguments = ["--insecure-skip-tls-verify"];

        // Act
        Action act = () => AdminSmokeCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("--insecure-skip-tls-verify is not allowed for repository k6 runs.");
    }

    [Fact]
    public void Rejects_results_directory_traversal()
    {
        // Arrange
        const string resultsDirectory = "tests/performance/results/../leak";

        // Act
        Action act = () => AdminSmokeCommand.NormalizeResultsDirectory(resultsDirectory);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_K6_RESULTS_DIR must stay inside the repository root and must not contain .. segments.");
    }

    [Fact]
    public void Rejects_unpinned_docker_image_when_docker_mode_is_enabled()
    {
        // Arrange
        const string dockerImage = "grafana/k6:0.49.0";

        // Act
        Action act = () => AdminSmokeCommand.ValidateDockerImage(dockerImage, "1");

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_K6_DOCKER_IMAGE must be pinned by digest, for example grafana/k6:0.49.0@sha256:<digest>.");
    }
}

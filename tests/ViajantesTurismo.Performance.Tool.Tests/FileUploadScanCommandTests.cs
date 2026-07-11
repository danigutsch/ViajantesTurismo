namespace ViajantesTurismo.Performance.Tool.Tests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.ScopeName, SharedKernelTestTraitNames.UnitScope)]
[Collection("Performance tool environment")]
public sealed class FileUploadScanCommandTests
{
    [Theory]
    [InlineData("http://[::1]:5000", "http://host.docker.internal:5000")]
    [InlineData("http://[::]:5000", "http://host.docker.internal:5000")]
    public void Converts_ipv6_loopback_upload_targets_for_docker(string targetText, string expectedText)
    {
        // Arrange
        var target = targetText;

        // Act
        var dockerTarget = FileUploadScanCommand.ToDockerUrl(target);

        // Assert
        dockerTarget.ShouldBe(expectedText);
    }

    [Theory]
    [InlineData("--include-system-env-vars")]
    [InlineData("--include-system-env-vars=false")]
    public void Rejects_system_environment_forwarding(string argument)
    {
        // Arrange
        string[] k6Arguments = [argument];

        // Act
        Action act = () => FileUploadScanCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("--include-system-env-vars is not allowed for repository k6 runs.");
    }

    [Theory]
    [InlineData("--http-debug")]
    [InlineData("--http-debug=full")]
    public void Rejects_http_debug_by_default(string argument)
    {
        // Arrange
        using var allowHttpDebugScope = new EnvironmentVariableScope("VT_K6_ALLOW_HTTP_DEBUG", "0");
        string[] k6Arguments = [argument];

        // Act
        Action act = () => FileUploadScanCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("--http-debug can expose request and response data. Set VT_K6_ALLOW_HTTP_DEBUG=1 for explicit local debugging.");
    }

    [Fact]
    public void Allows_http_debug_when_explicitly_enabled()
    {
        // Arrange
        using var allowHttpDebugScope = new EnvironmentVariableScope("VT_K6_ALLOW_HTTP_DEBUG", "1");
        string[] k6Arguments = ["--http-debug"];

        // Act
        var exception = Record.Exception(() => FileUploadScanCommand.ValidateK6Arguments(k6Arguments));

        // Assert
        exception.ShouldBeNull();
    }

    [Theory]
    [InlineData("--out", null)]
    [InlineData("--out=json=leak.json", null)]
    [InlineData("-o", null)]
    [InlineData("-o=json=leak.json", null)]
    public void Rejects_custom_k6_outputs_by_default(string firstArgument, string? secondArgument)
    {
        // Arrange
        using var allowRemoteOutputScope = new EnvironmentVariableScope("VT_K6_ALLOW_REMOTE_OUTPUT", "0");
        string[] k6Arguments = secondArgument is null ? [firstArgument] : [firstArgument, secondArgument];

        // Act
        Action act = () => FileUploadScanCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("Custom k6 outputs are disabled by default. Set VT_K6_ALLOW_REMOTE_OUTPUT=1 after reviewing output destination and credentials.");
    }

    [Fact]
    public void Allows_custom_k6_outputs_when_explicitly_enabled()
    {
        // Arrange
        using var allowRemoteOutputScope = new EnvironmentVariableScope("VT_K6_ALLOW_REMOTE_OUTPUT", "1");
        string[] k6Arguments = ["--out", "json=leak.json"];

        // Act
        var exception = Record.Exception(() => FileUploadScanCommand.ValidateK6Arguments(k6Arguments));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public void Still_rejects_summary_export_when_custom_outputs_are_enabled()
    {
        // Arrange
        using var allowRemoteOutputScope = new EnvironmentVariableScope("VT_K6_ALLOW_REMOTE_OUTPUT", "1");
        string[] k6Arguments = ["--summary-export=leak.json"];

        // Act
        Action act = () => FileUploadScanCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("--summary-export is controlled by the repository runner. Use VT_K6_RESULTS_DIR to choose the results folder.");
    }

    [Fact]
    public void Allows_uncontrolled_k6_environment_override()
    {
        // Arrange
        string[] k6Arguments = ["--env", "K6_TAGS=profile=smoke"];

        // Act
        var exception = Record.Exception(() => FileUploadScanCommand.ValidateK6Arguments(k6Arguments));

        // Assert
        exception.ShouldBeNull();
    }

    [Theory]
    [InlineData("-e", "VT_UPLOAD_BASE_URL=http://evil.example")]
    [InlineData("--env", "VT_UPLOAD_PAYLOAD_BYTES=1")]
    [InlineData("-e=K6_NO_USAGE_REPORT=false", null)]
    [InlineData("--env=VT_K6_VUS=99", null)]
    public void Rejects_controlled_k6_environment_override(string firstArgument, string? secondArgument)
    {
        // Arrange
        string[] k6Arguments = secondArgument is null ? [firstArgument] : [firstArgument, secondArgument];

        // Act
        Action act = () => FileUploadScanCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("Do not override repository-controlled k6 environment through -e/--env. Use documented VT_* environment variables before launching the tool.");
    }

    [Theory]
    [InlineData("--insecure-skip-tls-verify")]
    [InlineData("--insecure-skip-tls-verify=true")]
    public void Rejects_insecure_tls_bypass(string argument)
    {
        // Arrange
        string[] k6Arguments = [argument];

        // Act
        Action act = () => FileUploadScanCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("--insecure-skip-tls-verify is not allowed for repository k6 runs.");
    }

    [Theory]
    [InlineData("--summary-export")]
    [InlineData("--summary-export=leak.json")]
    public void Rejects_summary_export_override(string argument)
    {
        // Arrange
        string[] k6Arguments = [argument];

        // Act
        Action act = () => FileUploadScanCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("--summary-export is controlled by the repository runner. Use VT_K6_RESULTS_DIR to choose the results folder.");
    }

    [Theory]
    [InlineData("tests/performance/results", "tests/performance/results")]
    [InlineData("tests\\performance\\results\\local", "tests/performance/results/local")]
    public void Allows_results_directories_under_performance_results(string resultsDirectory, string expected)
    {
        // Arrange
        var input = resultsDirectory;

        // Act
        var normalized = FileUploadScanCommand.NormalizeResultsDirectory(input);

        // Assert
        normalized.ShouldBe(expected);
    }

    [Fact]
    public void Rejects_results_directory_traversal()
    {
        // Arrange
        const string resultsDirectory = "tests/performance/results/../leak";

        // Act
        Action act = () => FileUploadScanCommand.NormalizeResultsDirectory(resultsDirectory);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_K6_RESULTS_DIR must stay inside the repository root and must not contain .. segments.");
    }

    [Theory]
    [InlineData("/tmp/results")]
    [InlineData("C:\\tmp\\results")]
    public void Rejects_rooted_results_directories(string resultsDirectory)
    {
        // Arrange
        var input = resultsDirectory;

        // Act
        Action act = () => FileUploadScanCommand.NormalizeResultsDirectory(input);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_K6_RESULTS_DIR must be relative to the repository root.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("tests/performance/results//local")]
    public void Rejects_empty_or_ambiguous_results_directories(string resultsDirectory)
    {
        // Arrange
        var input = resultsDirectory;

        // Act
        Action act = () => FileUploadScanCommand.NormalizeResultsDirectory(input);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_K6_RESULTS_DIR must stay inside the repository root and must not be empty.");
    }

    [Fact]
    public void Rejects_results_directory_outside_performance_results()
    {
        // Arrange
        const string resultsDirectory = "artifacts/performance";

        // Act
        Action act = () => FileUploadScanCommand.NormalizeResultsDirectory(resultsDirectory);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_K6_RESULTS_DIR must stay under tests/performance/results.");
    }

    [Theory]
    [InlineData("smoke")]
    [InlineData("load_01")]
    [InlineData("load-01")]
    public void Allows_safe_profile_names(string profile)
    {
        // Arrange
        var value = profile;

        // Act
        var exception = Record.Exception(() => FileUploadScanCommand.ValidateProfile(value));

        // Assert
        exception.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("../smoke")]
    [InlineData("smoke/local")]
    [InlineData("smoke local")]
    public void Rejects_unsafe_profile_names(string profile)
    {
        // Arrange
        var value = profile;

        // Act
        Action act = () => FileUploadScanCommand.ValidateProfile(value);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_K6_PROFILE may contain only letters, numbers, underscores, and hyphens.");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    public void Allows_explicit_docker_mode_values(string useDocker)
    {
        // Arrange
        var value = useDocker;

        // Act
        var resolved = FileUploadScanCommand.ResolveUseDocker(value);

        // Assert
        resolved.ShouldBe(value);
    }

    [Fact]
    public void Rejects_unpinned_docker_image_when_docker_mode_is_enabled()
    {
        // Arrange
        const string dockerImage = "grafana/k6:0.49.0";

        // Act
        Action act = () => FileUploadScanCommand.ValidateDockerImage(dockerImage, "1");

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_K6_DOCKER_IMAGE must be pinned by digest, for example grafana/k6:0.49.0@sha256:<digest>.");
    }

    [Fact]
    public void Allows_unpinned_docker_image_when_docker_mode_is_disabled()
    {
        // Arrange
        const string dockerImage = "grafana/k6:0.49.0";

        // Act
        var exception = Record.Exception(() => FileUploadScanCommand.ValidateDockerImage(dockerImage, "0"));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public void Allows_digest_pinned_docker_image_when_docker_mode_is_enabled()
    {
        // Arrange
        const string dockerImage = "grafana/k6:0.49.0@sha256:8cd78f9d0de5f50bc8821cceecf356d5d9e839e6611c226a3fcf13c591080fbd";

        // Act
        var exception = Record.Exception(() => FileUploadScanCommand.ValidateDockerImage(dockerImage, "1"));

        // Assert
        exception.ShouldBeNull();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("262144")]
    [InlineData("16777216")]
    public void Allows_payload_sizes_inside_limit(string payloadBytes)
    {
        // Arrange
        var value = payloadBytes;

        // Act
        var exception = Record.Exception(() => FileUploadScanCommand.ValidatePayloadBytes(value));

        // Assert
        exception.ShouldBeNull();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("16777217")]
    [InlineData("abc")]
    public void Rejects_payload_sizes_outside_limit(string payloadBytes)
    {
        // Arrange
        var value = payloadBytes;

        // Act
        Action act = () => FileUploadScanCommand.ValidatePayloadBytes(value);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_UPLOAD_PAYLOAD_BYTES must be between 1 and 16777216.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("true")]
    [InlineData("docker")]
    public void Rejects_implicit_docker_mode_values(string useDocker)
    {
        // Arrange
        var value = useDocker;

        // Act
        Action act = () => FileUploadScanCommand.ResolveUseDocker(value);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("VT_K6_USE_DOCKER must be 0 or 1. Docker mode is explicit opt-in only.");
    }
}

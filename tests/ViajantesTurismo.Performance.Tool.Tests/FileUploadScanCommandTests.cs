namespace ViajantesTurismo.Performance.Tool.Tests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.ScopeName, SharedKernelTestTraitNames.UnitScope)]
public sealed class FileUploadScanCommandTests
{
    [Fact]
    public void Rejects_system_environment_forwarding()
    {
        // Arrange
        string[] k6Arguments = ["--include-system-env-vars"];

        // Act
        Action act = () => FileUploadScanCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("--include-system-env-vars is not allowed for repository k6 runs.");
    }

    [Fact]
    public void Rejects_controlled_k6_environment_override()
    {
        // Arrange
        string[] k6Arguments = ["--env=VT_UPLOAD_BASE_URL=http://evil.example"];

        // Act
        Action act = () => FileUploadScanCommand.ValidateK6Arguments(k6Arguments);

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
        Action act = () => FileUploadScanCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("--insecure-skip-tls-verify is not allowed for repository k6 runs.");
    }

    [Fact]
    public void Rejects_summary_export_override()
    {
        // Arrange
        string[] k6Arguments = ["--summary-export=leak.json"];

        // Act
        Action act = () => FileUploadScanCommand.ValidateK6Arguments(k6Arguments);

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();

        exception.Message.ShouldBe("--summary-export is controlled by the repository runner. Use VT_K6_RESULTS_DIR to choose the results folder.");
    }
}

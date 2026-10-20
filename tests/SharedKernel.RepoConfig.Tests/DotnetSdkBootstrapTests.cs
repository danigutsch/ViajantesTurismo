namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
[Collection("Repo config tool environment")]
public sealed class DotnetSdkBootstrapTests
{
    [Theory(Skip = "SDK bootstrap fixtures run on Unix CI hosts.", SkipType = typeof(CoverageCollectorTestPlatform), SkipWhen = nameof(CoverageCollectorTestPlatform.IsWindows))]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Installed_exact_sdk_requires_no_network(bool powershell)
    {
        // Arrange
        using var scope = new DotnetSdkBootstrapScope(powershell);
        scope.SetInstalledVersion("10.0.303");

        // Act
        var result = await scope.Run(install: false, TestContext.Current.CancellationToken);

        // Assert
        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("10.0.303 is already installed", StringComparison.Ordinal);
        scope.FileExists("web.log").ShouldBeFalse();
    }

    [Theory(Skip = "SDK bootstrap fixtures run on Unix CI hosts.", SkipType = typeof(CoverageCollectorTestPlatform), SkipWhen = nameof(CoverageCollectorTestPlatform.IsWindows))]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_sdk_requires_explicit_install_opt_in(bool powershell)
    {
        // Arrange
        using var scope = new DotnetSdkBootstrapScope(powershell);

        // Act
        var result = await scope.Run(install: false, TestContext.Current.CancellationToken);

        // Assert
        result.ExitCode.ShouldNotBe(0);
        result.StandardError.ShouldContain(powershell ? "-Install" : "--install", StringComparison.Ordinal);
        scope.FileExists("web.log").ShouldBeFalse();
    }

    [Theory(Skip = "SDK bootstrap fixtures run on Unix CI hosts.", SkipType = typeof(CoverageCollectorTestPlatform), SkipWhen = nameof(CoverageCollectorTestPlatform.IsWindows))]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Verified_exact_sdk_install_preserves_existing_sdk(bool powershell)
    {
        // Arrange
        using var scope = new DotnetSdkBootstrapScope(powershell);

        // Act
        var result = await scope.Run(install: true, TestContext.Current.CancellationToken);

        // Assert
        result.ExitCode.ShouldBe(0);
        scope.FileExists("install/sdk/10.0.303/dotnet.dll").ShouldBeTrue();
        scope.FileExists("install/sdk/10.0.302/marker").ShouldBeTrue();
        var requests = await File.ReadAllTextAsync(scope.PathFor("web.log"), TestContext.Current.CancellationToken);
        requests.ShouldContain("/Sdk/10.0.303/", StringComparison.Ordinal);
    }

    [Theory(Skip = "SDK bootstrap fixtures run on Unix CI hosts.", SkipType = typeof(CoverageCollectorTestPlatform), SkipWhen = nameof(CoverageCollectorTestPlatform.IsWindows))]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Checksum_mismatch_never_installs_unverified_sdk(bool powershell)
    {
        // Arrange
        using var scope = new DotnetSdkBootstrapScope(powershell);
        scope.WriteMetadata(invalidHash: true);

        // Act
        var result = await scope.Run(install: true, TestContext.Current.CancellationToken);

        // Assert
        result.ExitCode.ShouldNotBe(0);
        result.StandardError.ShouldContain("SHA-512 verification failed", StringComparison.Ordinal);
        scope.FileExists("install/sdk/10.0.303/dotnet.dll").ShouldBeFalse();
    }

    [Theory(Skip = "SDK bootstrap fixtures run on Unix CI hosts.", SkipType = typeof(CoverageCollectorTestPlatform), SkipWhen = nameof(CoverageCollectorTestPlatform.IsWindows))]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invalid_verified_archive_never_installs_sdk(bool powershell)
    {
        // Arrange
        using var scope = new DotnetSdkBootstrapScope(powershell);
        scope.CorruptArchive();
        scope.WriteMetadata();

        // Act
        var result = await scope.Run(install: true, TestContext.Current.CancellationToken);

        // Assert
        result.ExitCode.ShouldNotBe(0);
        result.StandardError.ShouldContain("Failed to extract the verified .NET SDK archive", StringComparison.Ordinal);
        scope.FileExists("install/sdk/10.0.303/dotnet.dll").ShouldBeFalse();
    }

    [Theory(Skip = "SDK bootstrap fixtures run on Unix CI hosts.", SkipType = typeof(CoverageCollectorTestPlatform), SkipWhen = nameof(CoverageCollectorTestPlatform.IsWindows))]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Explicit_global_json_selects_its_exact_sdk_without_network(bool powershell)
    {
        // Arrange
        using var scope = new DotnetSdkBootstrapScope(powershell);
        scope.WriteAlternateGlobalJson();
        scope.SetInstalledVersion("10.0.304");

        // Act
        var result = await scope.Run(install: false, TestContext.Current.CancellationToken, "alternate.json");

        // Assert
        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("10.0.304 is already installed", StringComparison.Ordinal);
        scope.FileExists("web.log").ShouldBeFalse();
    }
}

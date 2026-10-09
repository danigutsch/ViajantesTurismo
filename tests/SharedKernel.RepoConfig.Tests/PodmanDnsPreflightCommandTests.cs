using System.Globalization;

namespace SharedKernel.RepoConfig.Tests;

[Trait(TestTraitNames.CategoryName, TestTraits.CommandLineCategory)]
public sealed class PodmanDnsPreflightCommandTests
{
    [Fact]
    public async Task Tool_help_lists_the_preflight_command()
    {
        // Arrange
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        // Act
        var exitCode = await RepoConfigToolApplication.Run(
            ["--help"],
            output,
            error,
            Environment.CurrentDirectory,
            TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(0);
        output.ToString().ShouldContain("podman-dns-preflight", StringComparison.Ordinal);
        error.ToString().ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Missing_podman_is_inapplicable()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();

        // Act
        var result = await context.Run(podmanExecutable: null);

        // Assert
        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("podman is not installed", StringComparison.Ordinal);
        result.StandardError.ShouldBe(string.Empty);
        context.Commands.ShouldBe([]);
    }

    [Fact]
    public async Task Known_internal_network_config_is_healthy_and_read_only()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext
        {
            Networks = "trip\n",
            Containers = "container-id\n"
        };
        context.WriteConfig("trip");
        context.WriteConfig("trip%int");
        context.AddDaemon(1201);
        var before = context.Snapshot();

        // Act
        var result = await context.Run();
        var after = context.Snapshot();

        // Assert
        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("1 container(s) running", StringComparison.Ordinal);
        result.StandardError.ShouldBe(string.Empty);
        after.ShouldBe(before);
        context.Commands.ShouldBe(
        [
            "--remote=false info --format {{.Host.ServiceIsRemote}}",
            "--remote=false info --format {{.Host.Security.Rootless}}",
            "--remote=false info --format {{.Store.RunRoot}}",
            "--remote=false ps --quiet",
            "--remote=false network ls --format {{.Name}}",
            "--remote=false ps --quiet",
            "--remote=false network ls --format {{.Name}}",
            "--remote=false info --format {{.Store.RunRoot}}"
        ]);
    }

    [Fact]
    public async Task Check_mode_reports_orphan_config_without_mutation()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext { Networks = "trip\n" };
        context.WriteConfig("trip%int");
        context.WriteConfig("trip-old%int");
        context.AddDaemon(1202);
        var before = context.Snapshot();

        // Act
        var result = await context.Run(["--check"]);
        var after = context.Snapshot();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("trip-old%int", StringComparison.Ordinal);
        result.StandardError.ShouldNotContain("networks Podman no longer has: trip%int", StringComparison.Ordinal);
        after.ShouldBe(before);
    }

    [Fact]
    public async Task Live_runroot_daemon_without_pid_file_fails_closed()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.AddDaemon(1203, writePidFile: false);
        var before = context.Snapshot();

        // Act
        var result = await context.Run();
        var after = context.Snapshot();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("live aardvark-dns", StringComparison.Ordinal);
        after.ShouldBe(before);
    }

    [Fact]
    public async Task Missing_refcount_fails_without_mutation()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.AddDaemon(1204, refCount: null);
        var before = context.Snapshot();

        // Act
        var result = await context.Run();
        var after = context.Snapshot();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("ref-count does not exist", StringComparison.Ordinal);
        after.ShouldBe(before);
    }

    [Fact]
    public async Task Daemon_in_different_network_namespace_is_stale()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.AddDaemon(1210);
        context.SetProcessNamespaceIdentity(1210, "1:200");
        var before = context.Snapshot();

        // Act
        var result = await context.Run();
        var after = context.Snapshot();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("different network namespace", StringComparison.Ordinal);
        after.ShouldBe(before);
    }

    [Fact]
    public async Task Missing_daemon_namespace_identity_fails_closed()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.AddDaemon(1211);
        context.DeleteProcessNamespaceIdentity(1211);

        // Act
        var result = await context.Run();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("unable to verify safely", StringComparison.Ordinal);
        result.StandardError.ShouldContain("network namespace identities", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reused_daemon_pid_during_inspection_fails_closed()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.AddDaemon(1212, startTime: 100);
        context.ChangeProcessStartTimeOnRecheck(1212, 200);

        // Act
        var result = await context.Run();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("process identity changed during inspection", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Late_namespace_replacement_fails_closed()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.AddDaemon(1213);
        context.ChangeNamespaceIdentityOnRecheck(1213, "1:200");

        // Act
        var result = await context.Run();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("network namespace identity changed during inspection", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0", "reference count is 0")]
    [InlineData("invalid", "unable to verify safely")]
    public async Task Invalid_refcount_fails_closed(string refCount, string expectedMessage)
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.AddDaemon(1205, refCount: refCount);
        var before = context.Snapshot();

        // Act
        var result = await context.Run();
        var after = context.Snapshot();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain(expectedMessage, StringComparison.Ordinal);
        after.ShouldBe(before);
    }

    [Fact]
    public async Task Same_user_daemon_for_another_run_root_is_ignored()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        var otherConfig = Path.Combine(context.RootPath, "other-runroot", "networks", "aardvark-dns");
        context.AddDaemon(1206, otherConfig, writePidFile: false, refCount: null);

        // Act
        var result = await context.Run();

        // Assert
        result.ExitCode.ShouldBe(0);
        result.StandardError.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Equals_form_config_argument_matches_exact_run_root()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.AddDaemon(1207, equalsForm: true);

        // Act
        var result = await context.Run();

        // Assert
        result.ExitCode.ShouldBe(0);
        result.StandardError.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Config_without_live_daemon_fails_closed()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext { Networks = "trip\n" };
        context.WriteConfig("trip");

        // Act
        var result = await context.Run();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("does not identify a live aardvark-dns daemon", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Malformed_aardvark_directory_path_fails_closed()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.WriteAardvarkDirectoryAsFile();

        // Act
        var result = await context.Run();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("unable to verify safely", StringComparison.Ordinal);
        result.StandardOutput.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Malformed_networks_directory_path_fails_closed()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.WriteNetworksDirectoryAsFile();

        // Act
        var result = await context.Run();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("unable to verify safely", StringComparison.Ordinal);
        result.StandardOutput.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Malformed_pid_file_fails_closed()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.WriteConfig("aardvark.pid", "not-a-pid\n");

        // Act
        var result = await context.Run();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("contains an invalid PID", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("info --format {{.Host.ServiceIsRemote}}", "remote-service mode")]
    [InlineData("info --format {{.Host.Security.Rootless}}", "rootless mode")]
    [InlineData("info --format {{.Store.RunRoot}}", "run root")]
    [InlineData("ps --quiet", "running containers")]
    [InlineData("network ls --format {{.Name}}", "aardvark-dns configuration")]
    public async Task Podman_query_failure_fails_closed(string query, string expectedMessage)
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.WriteConfig("trip");
        context.SetFailure(query);

        // Act
        var result = await context.Run();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain(expectedMessage, StringComparison.Ordinal);
        result.StandardOutput.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Remote_podman_fails_closed()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();
        context.SetResponse("info --format {{.Host.ServiceIsRemote}}", "true\n");

        // Act
        var result = await context.Run();

        // Assert
        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("remote Podman state", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_argument_is_a_usage_error()
    {
        // Arrange
        using var context = new PodmanDnsPreflightTestContext();

        // Act
        var result = await context.Run(["--repair"]);

        // Assert
        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("Unknown argument: --repair", StringComparison.Ordinal);
        context.Commands.ShouldBe([]);
    }
}

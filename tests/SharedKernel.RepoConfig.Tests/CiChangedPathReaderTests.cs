using System.ComponentModel;
using System.Diagnostics;

namespace SharedKernel.RepoConfig.Tests;

public sealed class CiChangedPathReaderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolve_executable_returns_an_existing_absolute_candidate(bool isWindows)
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        var fileName = isWindows ? "git.exe" : "git";
        repository.WriteFile(fileName, string.Empty);
        var expectedPath = Path.Combine(repository.RootPath, fileName);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                expectedPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        // Act
        var resolvedPath = CiChangedPathReader.ResolveExecutable(
            "git",
            repository.RootPath,
            isWindows);

        // Assert
        resolvedPath.ShouldBe(expectedPath);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolve_executable_ignores_relative_path_entries(bool isWindows)
    {
        // Arrange
        using var repository = new TemporaryCiTestRepository();
        var fileName = isWindows ? "git.exe" : "git";
        repository.WriteFile(fileName, string.Empty);
        var expectedPath = Path.Combine(repository.RootPath, fileName);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                expectedPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        var relativeDirectory = Path.GetRelativePath(Environment.CurrentDirectory, repository.RootPath);
        var path = $"{relativeDirectory}{Path.PathSeparator}{repository.RootPath}";

        // Act
        var resolvedPath = CiChangedPathReader.ResolveExecutable("git", path, isWindows);

        // Assert
        resolvedPath.ShouldBe(expectedPath);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolve_executable_rejects_a_relative_only_path(bool isWindows)
    {
        // Act
        var resolvedPath = CiChangedPathReader.ResolveExecutable("git", "relative", isWindows);

        // Assert
        resolvedPath.ShouldBeNull();
    }

    [Theory]
    [InlineData(UnixFileMode.UserRead, false)]
    [InlineData(UnixFileMode.UserExecute, true)]
    [InlineData(UnixFileMode.GroupExecute, true)]
    [InlineData(UnixFileMode.OtherExecute, true)]
    public void Unix_execute_permission_requires_an_execute_bit(UnixFileMode mode, bool expected)
    {
        // Act
        var hasExecutePermission = CiChangedPathReader.HasUnixExecutePermission(mode);

        // Assert
        hasExecutePermission.ShouldBe(expected);
    }

    [Fact]
    public void Start_process_wraps_an_unlaunchable_executable_for_fail_open_handling()
    {
        // Arrange
        var startInfo = new ProcessStartInfo(Path.GetFullPath("git"));
        var startFailure = new Win32Exception("The candidate is not executable.");

        // Act
        Func<Process> startProcess = () => CiChangedPathReader.StartProcess(
            startInfo,
            _ => throw startFailure);
        var exception = startProcess.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.InnerException.ShouldBeSameAs(startFailure);
    }
}

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Guards the lock file policy: a library under <c>src</c> may drop its <c>packages.lock.json</c> only when it is listed in
/// <c>lockExemptProjects</c> of <c>.config/sharedkernel-repo.json</c> and opts out in its project file; every other project is locked.
/// </summary>
public static class LockFilePolicyTests
{
    [Fact]
    public static void Exempt_project_list_names_existing_unique_source_projects_in_order()
    {
        // Arrange
        var exempt = LockFilePolicyFiles.ReadExemptProjects();
        var projects = LockFilePolicyFiles.ReadProjects();

        // Act
        var unknown = exempt.Where(project => !projects.Contains(project)).ToArray();
        var outsideSource = exempt.Where(project => !project.StartsWith("src/", StringComparison.Ordinal)).ToArray();

        // Assert
        exempt.ShouldNotBeEmpty();
        string.Join(", ", unknown).ShouldBe(string.Empty, "Every lockExemptProjects entry must be an existing project file.");
        string.Join(", ", outsideSource).ShouldBe(string.Empty, "Only libraries under src may be lock-exempt.");
        string.Join("\n", exempt).ShouldBe(
            string.Join("\n", exempt.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)),
            "Keep lockExemptProjects unique and sorted.");
    }

    [Fact]
    public static void Exempt_projects_have_no_lock_file_and_opt_out_of_locking()
    {
        // Arrange
        var exempt = LockFilePolicyFiles.ReadExemptProjects();

        // Act
        var withLock = exempt.Where(LockFilePolicyFiles.HasLockFile).ToArray();
        var notOptedOut = exempt.Where(project => LockFilePolicyFiles.ReadRestoreWithLockFile(project) != "false").ToArray();

        // Assert
        string.Join(", ", withLock).ShouldBe(string.Empty, "A lock-exempt project must not keep a packages.lock.json; NuGet reports NU1005 for it.");
        string.Join(", ", notOptedOut).ShouldBe(string.Empty, "A lock-exempt project must set RestorePackagesWithLockFile to false in its project file.");
    }

    [Fact]
    public static void Every_other_project_keeps_a_lock_file_and_does_not_opt_out()
    {
        // Arrange
        var exempt = LockFilePolicyFiles.ReadExemptProjects().ToHashSet(StringComparer.Ordinal);
        var locked = LockFilePolicyFiles.ReadProjects().Where(project => !exempt.Contains(project)).ToArray();

        // Act
        var withoutLock = locked.Where(project => !LockFilePolicyFiles.HasLockFile(project)).ToArray();
        var optedOut = locked.Where(project => LockFilePolicyFiles.ReadRestoreWithLockFile(project) == "false").ToArray();

        // Assert
        locked.ShouldNotBeEmpty();
        string.Join(", ", withoutLock).ShouldBe(string.Empty, "A project that is not in lockExemptProjects needs a packages.lock.json.");
        string.Join(", ", optedOut).ShouldBe(string.Empty, "A project that sets RestorePackagesWithLockFile to false must be listed in lockExemptProjects.");
    }

    [Fact]
    public static void Every_package_of_an_exempt_project_is_resolved_by_a_project_that_keeps_a_lock()
    {
        // Arrange
        var exempt = LockFilePolicyFiles.ReadExemptProjects();
        var withoutAssets = exempt.Where(project => !LockFilePolicyFiles.HasRestoreAssets(project)).ToArray();
        string.Join(", ", withoutAssets).ShouldBe(
            string.Empty,
            "Run 'dotnet restore ViajantesTurismo.slnx' first: the restored obj/project.assets.json of a lock-exempt project is missing.");

        var locked = LockFilePolicyFiles.ReadProjects()
            .Where(project => LockFilePolicyFiles.HasLockFile(project))
            .SelectMany(LockFilePolicyFiles.ReadLockedPackages)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Act
        var uncovered = exempt
            .SelectMany(project => LockFilePolicyFiles.ReadResolvedPackages(project).Select(package => $"{project}: {package}"))
            .Where(entry => !locked.Contains(entry[(entry.IndexOf(": ", StringComparison.Ordinal) + 2)..]))
            .ToArray();

        // Assert
        string.Join("\n", uncovered).ShouldBe(
            string.Empty,
            "A lock-exempt project may only resolve package versions that a project with a lock file also locks, or the dependency snapshot loses them.");
    }

    [Fact]
    public static void Every_library_that_keeps_a_lock_file_resolves_a_package_that_no_other_lock_covers()
    {
        // Arrange
        var locked = LockFilePolicyFiles.ReadProjects().Where(project => LockFilePolicyFiles.HasLockFile(project)).ToArray();
        var libraries = locked.Where(project => project.StartsWith("src/", StringComparison.Ordinal) && !LockFilePolicyFiles.IsExecutable(project)).ToArray();

        // Act
        var redundant = libraries
            .Where(library =>
            {
                var others = locked.Where(project => project != library).SelectMany(LockFilePolicyFiles.ReadLockedPackages).ToHashSet(StringComparer.OrdinalIgnoreCase);
                return LockFilePolicyFiles.ReadLockedPackages(library).All(others.Contains);
            })
            .ToArray();

        // Assert
        string.Join(", ", redundant).ShouldBe(
            string.Empty,
            "A library lock file that every other lock covers adds nothing: list the project in lockExemptProjects, opt it out and delete the lock. Remove one redundant library at a time in sorted order and check again.");
    }
}

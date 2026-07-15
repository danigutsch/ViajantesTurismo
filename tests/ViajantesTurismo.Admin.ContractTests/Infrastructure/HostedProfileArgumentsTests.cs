using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.ContractTests.Infrastructure;

[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ContractScope)]
public sealed class HostedProfileArgumentsTests
{
    [Fact]
    public void ToArguments_returns_the_expected_profile_arguments()
    {
        // Arrange
        var adminArguments = HostedProfile.Admin.ToArguments();
        var systemArguments = HostedProfile.System.ToArguments();
        var fullArguments = HostedProfile.Full.ToArguments();

        // Act

        // Assert
        adminArguments.Length.ShouldBe(1);
        adminArguments[0].ShouldBe("--hosted-profile=admin");
        systemArguments.Length.ShouldBe(1);
        systemArguments[0].ShouldBe("--hosted-profile=system");
        fullArguments.ShouldBeEmpty();
    }

    [Fact]
    public void FromArguments_defaults_to_full_when_no_profile_is_supplied()
    {
        // Arrange
        string[] arguments = [];

        // Act
        var profile = HostedProfileArguments.FromArguments(arguments);

        // Assert
        profile.ShouldBe(HostedProfile.Full);
    }

    [Fact]
    public void FromArguments_parses_each_explicit_profile()
    {
        // Arrange
        string[] adminArguments = ["--hosted-profile=admin"];
        string[] systemArguments = ["--hosted-profile=system"];

        // Act
        var adminProfile = HostedProfileArguments.FromArguments(adminArguments);
        var systemProfile = HostedProfileArguments.FromArguments(systemArguments);

        // Assert
        adminProfile.ShouldBe(HostedProfile.Admin);
        systemProfile.ShouldBe(HostedProfile.System);
    }

    [Fact]
    public void HostedProfileArguments_rejects_unsupported_or_duplicate_profiles()
    {
        // Arrange
        Action unsupportedEnum = () => ((HostedProfile)99).ToArguments();
        Action unsupportedArgument = () => HostedProfileArguments.FromArguments(["--hosted-profile=unknown"]);
        Action duplicateArguments = () => HostedProfileArguments.FromArguments(
            ["--hosted-profile=admin", "--hosted-profile=system"]);

        // Act
        var unsupportedEnumException = unsupportedEnum.ShouldThrow<ArgumentOutOfRangeException>();
        var unsupportedArgumentException = unsupportedArgument.ShouldThrow<ArgumentOutOfRangeException>();
        var duplicateArgumentsException = duplicateArguments.ShouldThrow<ArgumentException>();

        // Assert
        unsupportedEnumException.ParamName.ShouldBe("profile");
        unsupportedArgumentException.ParamName.ShouldBe("args");
        duplicateArgumentsException.ParamName.ShouldBe("args");
    }

    [Fact]
    public void FromArguments_rejects_null_arguments()
    {
        // Arrange
        Action parse = () => HostedProfileArguments.FromArguments(null!);

        // Act
        var exception = parse.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe("args");
    }
}

using PandaBear.Testing;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class NpgsqlPoolMetricTagTests
{
    private const string ConnectionString =
        "Host=db.internal.example;Port=5499;Database=secret_db;Username=svc_user;Password=s3cret-pw";

    [Fact]
    public void A_named_data_source_reports_its_name_as_the_pool_tag()
    {
        // Arrange
        const string name = "probe-named";

        // Act
        var tags = NpgsqlPoolMetricTagCapture.CaptureForNamedDataSource(ConnectionString, name);

        // Assert
        tags.ShouldNotBeEmpty();
        tags.ShouldContain(new KeyValuePair<string, string>("db.client.connection.pool.name", name));
    }

    [Fact]
    public void A_named_data_source_emits_no_connection_string_part_in_any_tag()
    {
        // Arrange
        const string name = "probe-secret-free";

        // Act
        var tags = NpgsqlPoolMetricTagCapture.CaptureForNamedDataSource(ConnectionString, name);

        // Assert
        var tagText = string.Join('|', tags.Select(static tag => $"{tag.Key}={tag.Value}"));
        tagText.ShouldNotContain("s3cret-pw", StringComparison.Ordinal);
        tagText.ShouldNotContain("svc_user", StringComparison.Ordinal);
        tagText.ShouldNotContain("secret_db", StringComparison.Ordinal);
        tagText.ShouldNotContain("db.internal.example", StringComparison.Ordinal);
        tagText.ShouldNotContain("5499", StringComparison.Ordinal);
    }

    [Fact]
    public void A_named_data_source_emits_only_the_approved_tag_keys()
    {
        // Arrange
        const string name = "probe-dimensions";
        string[] approvedKeys = ["db.client.connection.pool.name", "db.client.connection.state"];

        // Act
        var tags = NpgsqlPoolMetricTagCapture.CaptureForNamedDataSource(ConnectionString, name);

        // Assert
        tags.Select(static tag => tag.Key).Except(approvedKeys, StringComparer.Ordinal).ShouldBeEmpty();
    }
}

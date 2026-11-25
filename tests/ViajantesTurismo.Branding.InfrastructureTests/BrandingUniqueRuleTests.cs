using SharedKernel.EntityFrameworkCore;
using ViajantesTurismo.Branding.Infrastructure;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
public sealed class BrandingUniqueRuleTests
{
    [Fact]
    public void The_revision_index_names_the_settings_revision_rule()
    {
        // Arrange
        using var context = new BrandingDbContextDesignTimeFactory().CreateDbContext([]);

        // Act
        var index = context.Model.FindEntityType(typeof(BrandingSettingsRevisionRecord))
            .ShouldNotBeNull()
            .GetIndexes()
            .Where(candidate => candidate.IsUnique)
            .ShouldHaveSingleItem();

        // Assert
        index.FindAnnotation(UniqueRuleIndexBuilderExtensions.AnnotationName).ShouldNotBeNull().Value
            .ShouldBe(BrandingSettingsRevisionRecord.UniqueRule);
        index.Properties.Select(property => property.Name)
            .ShouldBe([nameof(BrandingSettingsRevisionRecord.SourceEpoch), nameof(BrandingSettingsRevisionRecord.SourceRevision)]);
    }
}

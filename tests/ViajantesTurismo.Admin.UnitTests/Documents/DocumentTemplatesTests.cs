using PandaBear.Testing;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentTemplatesTests
{
    [Fact]
    public void IsCurrent_accepts_only_the_registered_version()
    {
        // Act and Assert
        DocumentTemplates.IsCurrent(DocumentTemplates.TourServiceContractId, DocumentTemplates.TourServiceContractVersion)
            .ShouldBeTrue();
        DocumentTemplates.IsCurrent(DocumentTemplates.TourServiceContractId, "0").ShouldBeFalse();
    }

    [Fact]
    public void IsCurrent_rejects_an_unregistered_template()
    {
        // Act and Assert
        DocumentTemplates.IsCurrent("unknown-template", "1").ShouldBeFalse();
    }
}

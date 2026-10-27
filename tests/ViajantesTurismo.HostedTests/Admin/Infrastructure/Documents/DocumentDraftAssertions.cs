using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Documents;

/// <summary>Assertions over document drafts.</summary>
internal static class DocumentDraftAssertions
{
    /// <summary>Asserts that the draft's fields carry exactly the expected field ids, in order.</summary>
    public static void ShouldHaveFieldIdsInOrder(this DocumentDraft document, string[] expectedFieldIds)
    {
        string[] fieldIds = [.. document.Fields.Select(field => field.FieldId)];
        fieldIds.ShouldBe(expectedFieldIds);
    }
}

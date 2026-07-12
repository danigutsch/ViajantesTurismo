using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Documents;

internal static class DocumentDraftAssertions
{
    public static void ShouldHaveFieldIdsInOrder(this DocumentDraft document, string[] expectedFieldIds)
    {
        string[] fieldIds = [.. document.Fields.Select(field => field.FieldId)];
        fieldIds.ShouldBe(expectedFieldIds);
    }
}

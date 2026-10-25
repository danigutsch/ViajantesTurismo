using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Mappings;

public sealed class DocumentMapperTests
{
    [Fact]
    public void A_document_projection_lists_its_fields_in_sort_order()
    {
        // Arrange
        var document = DocumentDraftTestData.Create(new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc));

        // Act
        var dto = DocumentMapper.MapToGetDocumentDto(document);

        // Assert
        dto.Id.ShouldBe(document.Id);
        dto.BookingId.ShouldBe(document.BookingId);
        dto.Fields.Select(field => field.FieldId).ShouldBe(document.Fields.OrderBy(field => field.SortOrder).Select(field => field.FieldId));
        dto.Fields.Count.ShouldBe(2);
    }

    [Fact]
    public void A_document_projection_rejects_a_missing_document()
    {
        // Arrange
        Func<object?> map = () => DocumentMapper.MapToGetDocumentDto(null!);

        // Act
        var exception = map.ShouldThrow<ArgumentNullException>();

        // Assert
        exception.ParamName.ShouldBe("document");
    }

    [Fact]
    public void An_undefined_document_status_is_rejected_with_its_value()
    {
        // Arrange
        Func<object?> map = () => DocumentMapper.MapToDocumentStatusDto((DocumentStatus)999);

        // Act
        var exception = map.ShouldThrow<ArgumentOutOfRangeException>();

        // Assert
        exception.ParamName.ShouldBe("status");
        exception.Message.ShouldStartWith("Invalid document status value.", StringComparison.Ordinal);
    }
}

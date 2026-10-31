using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents.Holds;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentHoldHandlerTests
{
    [Fact]
    public async Task Place_stores_a_hold_scoped_to_the_lineage_with_the_actor_from_the_command()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();

        // Act
        var result = await harness.Place.Handle(harness.CreatePlaceCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var hold = harness.Store.Holds.ShouldHaveSingleItem();
        hold.Scope.Kind.ShouldBe("document-lineage");
        hold.Scope.Key.ShouldBe(harness.LineageId.ToString("N"));
        hold.PlacedBy.ShouldBe(DocumentHoldHarness.ActorId);
        hold.RestrainsAt(harness.Now).ShouldBeTrue();
        harness.UnitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Place_of_an_unknown_document_is_not_found_and_stores_nothing()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var command = harness.CreatePlaceCommand() with
        {
            DocumentId = Guid.CreateVersion7()
        };

        // Act
        var result = await harness.Place.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        harness.Store.Holds.ShouldBeEmpty();
    }

    [Fact]
    public async Task Place_rejects_a_code_with_forbidden_characters_without_echoing_it()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        const string forbidden = "jane doe@example.invalid";

        // Act
        var result = await harness.Place.Handle(harness.CreatePlaceCommand(forbidden), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        var errors = result.ErrorDetails.ShouldNotBeNull();
        var rendered = string.Concat(errors.Detail, string.Join(' ', errors.ValidationErrors.ShouldNotBeNull().SelectMany(pair => pair.Value)));
        rendered.ShouldNotContain(forbidden, StringComparison.Ordinal);
        harness.Store.Holds.ShouldBeEmpty();
    }

    [Fact]
    public async Task Place_with_an_actor_that_is_not_an_opaque_identifier_is_a_server_fault()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var command = harness.CreatePlaceCommand() with
        {
            AuditContext = DocumentHoldHarness.CreateAuditContext("Jane Doe")
        };

        // Act
        Func<Task> place = () => harness.Place.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await place.ShouldThrow<InvalidOperationException>();
        harness.Store.Holds.ShouldBeEmpty();
    }

    [Fact]
    public async Task List_returns_placed_and_released_holds_of_the_lineage()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var placed = await harness.Place.Handle(harness.CreatePlaceCommand(), TestContext.Current.CancellationToken);
        _ = await harness.Place.Handle(harness.CreatePlaceCommand("purpose.claim-002"), TestContext.Current.CancellationToken);
        _ = await harness.Release.Handle(
            harness.CreateReleaseCommand(placed.Value.Id),
            TestContext.Current.CancellationToken);

        // Act
        var result = await harness.List.Handle(new ListDocumentHoldsQuery(harness.DocumentId), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.Count(hold => hold.IsRestraining).ShouldBe(1);
    }

    [Fact]
    public async Task Release_records_the_actor_and_reason_and_stops_the_hold_restraining()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var placed = await harness.Place.Handle(harness.CreatePlaceCommand(), TestContext.Current.CancellationToken);
        var command = harness.CreateReleaseCommand(placed.Value.Id, "actor-0002");

        // Act
        var result = await harness.Release.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var hold = harness.Store.Holds.ShouldHaveSingleItem();
        hold.ReleasedBy.ShouldBe("actor-0002");
        hold.ReleaseReasonCode.ShouldBe("reason.case-closed");
        hold.RestrainsAt(harness.Now).ShouldBeFalse();
        harness.Transaction.ExecutedLockNames.Last().ShouldHaveSingleItem().ShouldContain(placed.Value.Id.ToString("N"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_second_release_is_a_conflict_and_keeps_the_first_release()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var placed = await harness.Place.Handle(harness.CreatePlaceCommand(), TestContext.Current.CancellationToken);
        var first = harness.CreateReleaseCommand(placed.Value.Id, "actor-0002");
        var second = first with
        {
            AuditContext = DocumentHoldHarness.CreateAuditContext("actor-0003"),
            ReasonCode = "reason.other"
        };
        _ = await harness.Release.Handle(first, TestContext.Current.CancellationToken);

        // Act
        var result = await harness.Release.Handle(second, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Conflict);
        harness.Store.Holds.ShouldHaveSingleItem().ReleasedBy.ShouldBe("actor-0002");
    }

    [Fact]
    public async Task Release_of_a_hold_that_belongs_to_another_lineage_is_not_found()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var placed = await harness.Place.Handle(harness.CreatePlaceCommand(), TestContext.Current.CancellationToken);
        var otherDocument = Guid.CreateVersion7();
        harness.Query.LineageByDocument[otherDocument] = Guid.CreateVersion7();
        var command = harness.CreateReleaseCommand(placed.Value.Id) with
        {
            DocumentId = otherDocument
        };

        // Act
        var result = await harness.Release.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        harness.Store.Holds.ShouldHaveSingleItem().IsReleased.ShouldBeFalse();
    }
}

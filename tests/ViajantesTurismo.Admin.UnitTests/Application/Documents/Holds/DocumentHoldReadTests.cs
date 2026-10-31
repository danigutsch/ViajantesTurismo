using PandaBear.DispositionHold;
using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents.Holds;

[Trait(SharedKernelTestTraitNames.CapabilityName, Testing.AdminTestTraitValues.GeneratedDocumentsCapability)]
public sealed class DocumentHoldReadTests
{
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

    [Fact]
    public async Task Get_returns_a_hold_of_the_lineage_of_the_document()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var hold = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30));

        // Act
        var result = await harness.Get.Handle(new GetDocumentHoldQuery(harness.DocumentId, hold.Id), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(hold.Id);
    }

    [Fact]
    public async Task Get_of_an_unknown_hold_is_not_found()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();

        // Act
        var result = await harness.Get.Handle(new GetDocumentHoldQuery(harness.DocumentId, Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Get_of_a_hold_on_another_lineage_is_not_found()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var foreign = DocumentHoldReadTestData.AddHold(harness, Guid.CreateVersion7(), harness.Now.AddDays(30));

        // Act
        var result = await harness.Get.Handle(new GetDocumentHoldQuery(harness.DocumentId, foreign.Id), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Get_of_an_unknown_document_is_not_found()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var hold = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30));

        // Act
        var result = await harness.Get.Handle(new GetDocumentHoldQuery(Guid.CreateVersion7(), hold.Id), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Overdue_review_means_unreleased_with_a_review_date_strictly_before_now()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var before = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now - OneSecond);
        _ = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now);
        _ = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now + OneSecond);
        var releasedBefore = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now - OneSecond);
        releasedBefore.Release("actor-0002", "reason.case-closed", harness.Now - OneSecond);

        // Act
        var overdue = await DocumentHoldReadTestData.List(harness, DocumentHoldState.OverdueReview);

        // Assert
        overdue.Value.Items.Select(item => item.Hold.Id).ShouldBe([before.Id]);
    }

    [Fact]
    public async Task Active_means_unreleased_with_a_review_date_at_or_after_now()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        _ = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now - OneSecond);
        var at = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now);
        var after = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now + OneSecond);
        var releasedAfter = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now + OneSecond);
        releasedAfter.Release("actor-0002", "reason.case-closed", harness.Now);

        // Act
        var active = await DocumentHoldReadTestData.List(harness, DocumentHoldState.Active);

        // Assert
        active.Value.Items.Select(item => item.Hold.Id).Order().ShouldBe(new[] { at.Id, after.Id }.Order());
    }

    [Fact]
    public async Task Every_hold_appears_in_exactly_one_state()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var ids = new List<Guid>
        {
            DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now - OneSecond).Id,
            DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now).Id,
            DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now + OneSecond).Id,
        };
        var released = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now - OneSecond);
        released.Release("actor-0002", "reason.case-closed", harness.Now - OneSecond);
        ids.Add(released.Id);

        // Act
        var active = await DocumentHoldReadTestData.List(harness, DocumentHoldState.Active);
        var overdue = await DocumentHoldReadTestData.List(harness, DocumentHoldState.OverdueReview);
        var releasedState = await DocumentHoldReadTestData.List(harness, DocumentHoldState.Released);

        // Assert
        var listed = active.Value.Items.Concat(overdue.Value.Items).Concat(releasedState.Value.Items).Select(item => item.Hold.Id);
        listed.Order().ShouldBe(ids.Order());
    }

    [Fact]
    public async Task Overdue_review_follows_the_clock()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var hold = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now + OneSecond);
        var beforeAdvance = await DocumentHoldReadTestData.List(harness, DocumentHoldState.OverdueReview);

        // Act
        harness.Clock.CurrentInstant += OneSecond + OneSecond;
        var afterAdvance = await DocumentHoldReadTestData.List(harness, DocumentHoldState.OverdueReview);

        // Assert
        beforeAdvance.Value.Items.ShouldBeEmpty();
        afterAdvance.Value.Items.ShouldHaveSingleItem().Hold.Id.ShouldBe(hold.Id);
    }

    [Fact]
    public async Task Released_since_includes_the_boundary_and_excludes_earlier_and_unreleased_holds()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var since = harness.Now.AddDays(-1);
        var earlier = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30));
        earlier.Release("actor-0002", "reason.case-closed", since - OneSecond);
        var atBoundary = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30));
        atBoundary.Release("actor-0002", "reason.case-closed", since);
        var later = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30));
        later.Release("actor-0002", "reason.case-closed", since + OneSecond);
        _ = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30));

        // Act
        var result = await harness.Overview.Handle(DocumentHoldReadTestData.Query(releasedSince: since), TestContext.Current.CancellationToken);

        // Assert
        result.Value.Items.Select(item => item.Hold.Id).Order().ShouldBe(new[] { atBoundary.Id, later.Id }.Order());
    }

    [Fact]
    public async Task Released_state_returns_only_released_holds()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        _ = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30));
        var released = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30));
        released.Release("actor-0002", "reason.case-closed", harness.Now);

        // Act
        var result = await DocumentHoldReadTestData.List(harness, DocumentHoldState.Released);

        // Assert
        result.Value.Items.ShouldHaveSingleItem().Hold.Id.ShouldBe(released.Id);
    }

    [Fact]
    public async Task The_basis_filter_keeps_only_that_basis()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        _ = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30), HoldBasis.LegalClaim);
        var investigation = DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30), HoldBasis.Investigation);

        // Act
        var result = await harness.Overview.Handle(DocumentHoldReadTestData.Query(basis: HoldBasis.Investigation), TestContext.Current.CancellationToken);

        // Assert
        result.Value.Items.ShouldHaveSingleItem().Hold.Id.ShouldBe(investigation.Id);
    }

    [Fact]
    public async Task Pages_are_bounded_stably_ordered_and_report_whether_more_follow()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        var ids = Enumerable.Range(0, 3)
            .Select(offset => DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30), placedAt: harness.Now.AddMinutes(-10 + offset)).Id)
            .ToList();

        // Act
        var first = await harness.Overview.Handle(DocumentHoldReadTestData.Query(page: 1, pageSize: 2), TestContext.Current.CancellationToken);
        var second = await harness.Overview.Handle(DocumentHoldReadTestData.Query(page: 2, pageSize: 2), TestContext.Current.CancellationToken);

        // Assert
        first.Value.Items.Select(item => item.Hold.Id).ShouldBe(ids.Take(2));
        first.Value.HasMore.ShouldBeTrue();
        second.Value.Items.Select(item => item.Hold.Id).ShouldBe(ids.Skip(2));
        second.Value.HasMore.ShouldBeFalse();
        first.Value.Items.All(item => item.LineageId == harness.LineageId).ShouldBeTrue();
    }

    [Fact]
    public async Task The_highest_accepted_page_is_valid_and_empty_when_no_hold_reaches_it()
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();
        DocumentHoldReadTestData.AddHold(harness, harness.LineageId, harness.Now.AddDays(30));

        // Act
        var result = await harness.Overview.Handle(DocumentHoldReadTestData.Query(page: 100_000, pageSize: 100), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
        result.Value.HasMore.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(100_001, 10)]
    [InlineData(int.MaxValue, 100)]
    public async Task Out_of_range_paging_is_invalid_without_echoing_the_values(int page, int pageSize)
    {
        // Arrange
        var harness = DocumentHoldHarness.Create();

        // Act
        var result = await harness.Overview.Handle(DocumentHoldReadTestData.Query(page: page, pageSize: pageSize), TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        var errors = result.ErrorDetails.ShouldNotBeNull();
        var rendered = string.Concat(errors.Detail, string.Join(' ', errors.ValidationErrors.ShouldNotBeNull().SelectMany(pair => pair.Value)));
        rendered.ShouldNotContain(pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }
}

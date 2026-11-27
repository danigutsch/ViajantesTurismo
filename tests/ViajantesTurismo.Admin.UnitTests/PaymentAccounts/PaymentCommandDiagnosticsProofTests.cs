using SharedKernel.Testing;
using SharedKernel.Testing.Logging;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

/// <summary>Proves that real payment command display protection must be kept separate from raw log state.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.CapabilityName, Testing.AdminTestTraitValues.PaymentsCapability)]
public sealed class PaymentCommandDiagnosticsProofTests
{
    [Theory]
    [InlineData(nameof(ReviewPaymentInstructionsCommand), false, false)]
    [InlineData(nameof(ReviewPaymentInstructionsCommand), false, true)]
    [InlineData(nameof(ReviewPaymentInstructionsCommand), true, false)]
    [InlineData(nameof(ReviewPaymentInstructionsCommand), true, true)]
    [InlineData(nameof(CreatePaymentAccountCommand), false, false)]
    [InlineData(nameof(CreatePaymentAccountCommand), false, true)]
    [InlineData(nameof(CreatePaymentAccountCommand), true, false)]
    [InlineData(nameof(CreatePaymentAccountCommand), true, true)]
    [InlineData(nameof(DownloadPaymentInstructionsCommand), false, false)]
    [InlineData(nameof(DownloadPaymentInstructionsCommand), false, true)]
    public void A_deliberate_whole_object_log_retains_payment_inputs_even_when_display_is_redacted(
        string commandName, bool initialized, bool destructuring)
    {
        // Arrange
        var command = PaymentCommandDiagnosticExamples.Create(commandName, initialized);
        var logger = new RecordingLogger();
        var commandField = destructuring ? "@Command" : "Command";

        // Act
        var display = $"{command}";
        PaymentCommandDiagnosticLogProbe.CaptureWholeObject(logger, command, destructuring);

        // Assert
        display.ShouldBe($"{commandName} [redacted]");
        var record = logger.Records.ShouldHaveSingleItem();
        record.Message.ShouldBe($"Command {commandName} [redacted]");
        var captured = record.State.ShouldHaveSingleItem(pair => pair.Key == commandField).Value;
        captured.ShouldBeSameAs(command);
        var key = PaymentCommandDiagnosticExamples.Key(initialized);
        switch (captured)
        {
            case ReviewPaymentInstructionsCommand review:
                review.ActorId.ShouldBe(PaymentCommandDiagnosticExamples.Actor);
                review.Target.DocumentId.ShouldBe(PaymentCommandDiagnosticExamples.DocumentId);
                review.Input.ExpectedDocumentVersion.ShouldBe(PaymentCommandDiagnosticExamples.DocumentVersion);
                review.IdempotencyKey.ShouldBe(key);
                break;
            case CreatePaymentAccountCommand account:
                account.ActorId.ShouldBe(PaymentCommandDiagnosticExamples.Actor);
                account.Input.Recipient.RecipientName.ShouldBe(PaymentCommandDiagnosticExamples.RecipientName);
                account.Input.Recipient.PixKey.ShouldBe(PaymentCommandDiagnosticExamples.RecipientKey);
                account.IdempotencyKey.ShouldBe(key);
                break;
            case DownloadPaymentInstructionsCommand download:
                download.ActorId.ShouldBe(PaymentCommandDiagnosticExamples.Actor);
                download.Target.DocumentId.ShouldBe(PaymentCommandDiagnosticExamples.DocumentId);
                break;
            default:
                throw new InvalidOperationException("Expected a captured payment command.");
        }
    }

    [Theory]
    [InlineData(nameof(ReviewPaymentInstructionsCommand), false)]
    [InlineData(nameof(ReviewPaymentInstructionsCommand), true)]
    [InlineData(nameof(CreatePaymentAccountCommand), false)]
    [InlineData(nameof(CreatePaymentAccountCommand), true)]
    [InlineData(nameof(DownloadPaymentInstructionsCommand), false)]
    public void A_generated_metadata_log_keeps_only_the_command_name_outcome_and_error_type(
        string commandName, bool initialized)
    {
        // Arrange
        var command = PaymentCommandDiagnosticExamples.Create(commandName, initialized);
        var logger = new RecordingLogger();
        var failure = new InvalidOperationException("synthetic-payment-error", new ArgumentException("synthetic-error-detail"));
        var requestName = command.GetType().Name;
        var errorType = failure.GetType().FullName;

        // Act
        PaymentCommandDiagnosticLogProbe.ReportFailure(logger, requestName, "failed", errorType ?? failure.GetType().Name);

        // Assert
        var record = logger.Records.ShouldHaveSingleItem();
        record.Message.ShouldBe($"Command {commandName} finished as failed. Error type: {typeof(InvalidOperationException).FullName}.");
        record.Exception.ShouldBeNull();
        record.State.Count.ShouldBe(4);
        record.State.ShouldHaveSingleItem(pair => pair.Key == "RequestName").Value.ShouldBeOfType<string>().ShouldBe(commandName);
        record.State.ShouldHaveSingleItem(pair => pair.Key == "Outcome").Value.ShouldBeOfType<string>().ShouldBe("failed");
        record.State.ShouldHaveSingleItem(pair => pair.Key == "ErrorType").Value.ShouldBeOfType<string>()
            .ShouldBe(typeof(InvalidOperationException).FullName);
        record.State.ShouldHaveSingleItem(pair => pair.Key == "{OriginalFormat}").Value.ShouldBeOfType<string>();
    }
}

using PandaBear.Testing;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using ViajantesTurismo.Management.Web.Components.Pages.Payments;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(SharedKernelTestTraitNames.ScopeName, TestTraits.ComponentScope)]
public sealed class PaymentPagesTests : BunitContext
{
    private PaymentPageHttpHandler Handler => Services.GetRequiredService<PaymentPageHttpHandler>();
    private readonly FakeBookingsApiClient _bookings = new();
    public PaymentPagesTests()
    {
        Services.AddSingleton<PaymentPageHttpHandler>();
        Services.AddSingleton(provider => new HttpClient(provider.GetRequiredService<PaymentPageHttpHandler>()) { BaseAddress = new Uri("https://payments.example.test") });
        Services.AddSingleton<IBookingsApiClient>(_bookings);
        Services.AddSingleton<IBookingPaymentsApiClient>(provider => new BookingPaymentsApiClient(provider.GetRequiredService<HttpClient>()));
        Services.AddSingleton<IPaymentAccountsApiClient>(provider => new PaymentAccountsApiClient(provider.GetRequiredService<HttpClient>()));
        Services.AddSingleton<IPaymentInstructionsApiClient>(provider => new PaymentInstructionsApiClient(provider.GetRequiredService<HttpClient>()));
        Services.AddSingleton(TimeProvider.System);
        var authorization = AddAuthorization();
        authorization.SetAuthorized("owner");
        authorization.SetRoles("Admin");
    }
    [Fact]
    public void Financial_forms_have_labels_and_saving_a_draft_does_not_imply_approval()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = CurrencyDto.Euro
        };
        _bookings.AddBooking(booking);

        // Act
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#schedule-obligation"));

        // Assert
        foreach (var input in page.FindAll("input[id],select[id]"))
        {
            page.FindAll($"label[for='{input.Id}']").Count.ShouldBe(1);
        }
        page.Markup.ShouldContain("Saving does not approve terms", StringComparison.Ordinal);
        Handler.Keys.Count.ShouldBe(0);
    }
    [Fact]
    public void Operators_can_record_pending_receipts_but_do_not_see_admin_approval_or_sensitive_forms()
    {
        // Arrange
        var authorization = AddAuthorization();
        authorization.SetAuthorized("operator");
        authorization.SetRoles("Operator");
        var booking = BuildBookingDto() with
        {
            Currency = CurrencyDto.Euro
        };
        _bookings.AddBooking(booking);

        // Act
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#receipt-gross"));

        // Assert
        page.FindAll("#schedule-obligation").Count.ShouldBe(0);
        page.FindAll("#quote-rate").Count.ShouldBe(0);
        page.FindAll("#refund-actual").Count.ShouldBe(0);
        page.Markup.ShouldContain("Record pending receipt", StringComparison.Ordinal);
        page.Markup.ShouldNotContain("View sensitive evidence", StringComparison.Ordinal);
    }
    [Fact]
    public void Unknown_receipt_outcome_keeps_the_same_key_and_payload_for_retry()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = CurrencyDto.Euro
        };
        _bookings.AddBooking(booking);
        Handler.FailNextSubmission = true;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#receipt-gross"));
        page.Find("#receipt-gross").Change("100");
        page.Find("#receipt-date").Change("2030-01-05");
        page.Find("#receipt-reference").Change("bank-evidence");

        // Act
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Record pending receipt").Click();
        page.WaitForAssertion(() => page.FindAll("button").Count(button => button.TextContent == "Retry same submission").ShouldBe(1));
        page.FindAll("button").ShouldHaveSingleItem(button => button.TextContent == "Retry same submission").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(2));
        Handler.Keys[0].ShouldBe(Handler.Keys[1]);
        Handler.Payloads[0].ShouldBe(Handler.Payloads[1]);
        page.Markup.ShouldNotContain("Private bank data", StringComparison.Ordinal);
    }
    [Fact]
    public void Recipient_entry_requires_labeled_details_and_explicit_independent_verification()
    {
        // Act
        var page = Render<Accounts>();
        page.WaitForAssertion(() => page.Find("#account-iban"));

        // Assert
        foreach (var input in page.FindAll("input[id],select[id]"))
        {
            page.FindAll($"label[for='{input.Id}']").Count.ShouldBe(1);
        }
        page.Markup.ShouldContain("Format validation does not prove ownership", StringComparison.Ordinal);
        page.Markup.ShouldContain("Save unverified draft", StringComparison.Ordinal);
        Handler.Keys.Count.ShouldBe(0);
    }

    [Fact]
    public void Instruction_generation_selects_sources_without_editable_financial_text()
    {
        // Act
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.Find("#instruction-account"));

        // Assert
        page.FindAll("textarea,input[type=number]").Count.ShouldBe(0);
        page.Find("label[for='instruction-account']").TextContent.ShouldContain("Verified recipient", StringComparison.Ordinal);
        page.Markup.ShouldContain("Generated financial fields cannot be edited", StringComparison.Ordinal);
        Handler.Keys.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData("Verify", "verify", 200)]
    [InlineData("Withdraw verification", "withdraw-verification", 200)]
    [InlineData("Retire", "retire", 200)]
    [InlineData("Independent review (optional)", "review", 200)]
    [InlineData("Verify", "verify", 409)]
    [InlineData("Retire", "retire", 400)]
    public void Account_actions_require_confirmation_and_submit_the_displayed_revision(string button, string operation, int status)
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.SubmissionStatus = (HttpStatusCode)status;
        var page = Render<Accounts>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Count.ShouldBe(3));
        page.FindAll("button").First(item => item.TextContent == button).Click();

        // Act
        var section = page.Find("section[aria-labelledby='account-confirm-title']");
        for (var index = 0; index < page.FindAll("section[aria-labelledby='account-confirm-title'] input[type=checkbox]").Count; index++)
        {
            page.FindAll("section[aria-labelledby='account-confirm-title'] input[type=checkbox]")[index].Change(true);
        }
        page.FindAll("section[aria-labelledby='account-confirm-title'] button").ShouldHaveSingleItem(item => item.TextContent.StartsWith("Confirm ", StringComparison.Ordinal)).Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        Handler.Paths[0].ShouldEndWith("/" + operation, StringComparison.Ordinal);
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("expectedVersion").GetInt64().ShouldBe(7);
        if (operation != "review")
        {
            payload.RootElement.GetProperty("confirmed").GetBoolean().ShouldBeTrue();
        }
        if (operation == "verify")
        {
            payload.RootElement.GetProperty("independentCheckAttested").GetBoolean().ShouldBeTrue();
        }
        if (status == 409)
        {
            page.WaitForAssertion(() => page.Markup.ShouldContain("The account changed", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("Iban", false)]
    [InlineData("Pix", false)]
    [InlineData("BrazilianBank", false)]
    [InlineData("Iban", true)]
    [InlineData("Pix", true)]
    [InlineData("BrazilianBank", true)]
    public void Recipient_drafts_submit_only_selected_rail_fields_and_edits_use_a_revision_precondition(string kind, bool edit)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(kind);
        Handler.Accounts = PaymentPageData.Accounts;
        var page = Render<Accounts>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Count.ShouldBe(3));
        if (edit)
        {
            page.FindAll("button").First(item => item.TextContent == "Edit recipient").Click();
            page.WaitForAssertion(() => page.Markup.ShouldContain("New unverified recipient revision", StringComparison.Ordinal));
        }
        page.Find("#account-label").Change("Checked account");
        page.Find("#recipient-name").Change("Checked recipient");
        page.Find("#account-kind").Change(kind);
        if (kind == "Iban")
        {
            page.Find("#account-iban").Change("DE89370400440532013000");
            page.Find("#account-bic").Change("COBADEFFXXX");
        }
        else if (kind == "Pix")
        {
            page.Find("#pix-key").Change("recipient@example.test");
        }
        else
        {
            page.Find("#bank-ispb").Change("00000000");
            page.Find("#bank-branch").Change("0001");
            page.Find("#bank-number").Change("123456");
            page.Find("#holder-tax").Change("52998224725");
        }

        // Act
        page.Find("form").Submit();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        var recipient = payload.RootElement.GetProperty("recipient");
        recipient.GetProperty("settlementCurrency").GetInt32().ShouldBe(kind == "Iban" ? 1 : 0);
        if (edit)
        {
            payload.RootElement.GetProperty("expectedVersion").GetInt64().ShouldBe(7);
        }
        payload.RootElement.TryGetProperty("confirmed", out _).ShouldBeFalse();
        if (kind != "Iban")
        {
            Handler.Payloads[0].ShouldNotContain("DE89370400440532013000", StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("Approve terms", "schedule")]
    [InlineData("Approve quote", "quote")]
    [InlineData("Approve reconciliation", "reconciliation")]
    [InlineData("Approve refund record", "refund")]
    [InlineData("Correct erroneous refund record", "correct")]
    [InlineData("Reverse erroneous receipt", "reverse")]
    [InlineData("Withdraw approval", "withdraw")]
    [InlineData("Optional independent review", "review")]
    [InlineData("Review allocation", "allocate")]
    [InlineData("Review release", "release")]
    public void Financial_actions_submit_only_after_confirming_the_current_summary(string button, string operation)
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = CurrencyDto.Euro
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.History;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == button).ShouldBeTrue());
        page.FindAll("button").First(item => item.TextContent == button).Click();
        var section = page.Find("section[aria-labelledby='financial-confirm']");
        Handler.Keys.Count.ShouldBe(0);
        section.QuerySelector("button").ShouldNotBeNull().HasAttribute("disabled").ShouldBeTrue();

        // Act
        for (var index = 0; index < page.FindAll("section[aria-labelledby='financial-confirm'] input[type=checkbox]").Count; index++)
        {
            page.FindAll("section[aria-labelledby='financial-confirm'] input[type=checkbox]")[index].Change(true);
        }
        page.FindAll("section[aria-labelledby='financial-confirm'] button").ShouldHaveSingleItem(item => item.TextContent.StartsWith("Confirm ", StringComparison.Ordinal)).Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("expectedVersion").GetInt64().ShouldBe(12);
        if (operation == "review")
        {
            payload.RootElement.GetProperty("changesRequested").GetBoolean().ShouldBeTrue();
        }
        else
        {
            payload.RootElement.GetProperty("confirmed").GetBoolean().ShouldBeTrue();
        }
        page.WaitForAssertion(() => page.FindAll("#financial-confirm").Count.ShouldBe(0));
    }

    [Theory]
    [InlineData("Save unapproved schedule", "/schedules")]
    [InlineData("Save unapproved quote", "/quotes")]
    [InlineData("Save unapproved reconciliation", "/reconciliations")]
    [InlineData("Save unapproved refund record", "/refunds")]
    public void Financial_draft_forms_preserve_calendar_dates_and_do_not_send_approval(string button, string suffix)
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = CurrencyDto.Euro
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.History;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#schedule-obligation"));
        page.Find("#schedule-obligation").Change("100");
        page.Find("#quote-agreed").Change("2030-01-05T12:00");
        page.Find("#quote-expiry").Change("2030-01-06T12:00");
        page.Find("#refund-time").Change("2030-01-05T12:00");
        page.FindAll("label").ShouldHaveSingleItem(item => item.TextContent.Contains("I checked evidence", StringComparison.Ordinal)).QuerySelector("input").ShouldNotBeNull().Change(true);

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == button).Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        Handler.Paths[0].ShouldEndWith(suffix, StringComparison.Ordinal);
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("expectedVersion").GetInt64().ShouldBe(12);
        payload.RootElement.TryGetProperty("confirmed", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Approve summary", "approve", 0)]
    [InlineData("Finalize printable HTML", "finalize", 3)]
    [InlineData("Withdraw approval", "withdraw", 3)]
    [InlineData("Void revision", "void", 4)]
    [InlineData("Independent review (optional)", "review", 4)]
    public void Instruction_actions_pin_the_displayed_document_revision_and_explicit_confirmation(string button, string operation, int documentStatus)
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.FinancialHistory = PaymentPageData.History;
        var instruction = JsonNode.Parse(PaymentPageData.Instruction).ShouldNotBeNull();
        instruction["document"].ShouldNotBeNull()["status"] = documentStatus;
        Handler.Instructions = "[" + instruction.ToJsonString() + "]";
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == button).ShouldBeTrue());
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == button).Click();
        var section = page.Find("section[aria-labelledby='instruction-confirm']");
        Handler.Keys.Count.ShouldBe(0);

        // Act
        for (var index = 0; index < page.FindAll("section[aria-labelledby='instruction-confirm'] input[type=checkbox]").Count; index++)
        {
            page.FindAll("section[aria-labelledby='instruction-confirm'] input[type=checkbox]")[index].Change(true);
        }
        page.FindAll("section[aria-labelledby='instruction-confirm'] button").ShouldHaveSingleItem(item => item.TextContent.StartsWith("Confirm ", StringComparison.Ordinal)).Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("expectedDocumentVersion").GetInt64().ShouldBe(9);
        if (operation == "review")
        {
            payload.RootElement.GetProperty("changesRequested").GetBoolean().ShouldBeTrue();
        }
        else
        {
            payload.RootElement.GetProperty("confirmed").GetBoolean().ShouldBeTrue();
        }
        page.WaitForAssertion(() => page.FindAll("#instruction-confirm").Count.ShouldBe(0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Generating_instructions_pins_account_financial_and_replacement_revisions(bool regenerate)
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.FinancialHistory = PaymentPageData.History;
        Handler.Instructions = "[" + PaymentPageData.Instruction + "]";
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.Find("#instruction-account"));
        page.Find("#instruction-account").Change("00000001-1111-1111-1111-111111111111");
        var installment = page.FindAll("fieldset").ShouldHaveSingleItem(item => item.QuerySelector("legend")?.TextContent == "Outstanding installments");
        installment.QuerySelector("input").ShouldNotBeNull().Change(true);
        if (regenerate)
        {
            page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Select for regeneration").Click();
        }

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Save generated unapproved draft").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("expectedVersion").GetInt64().ShouldBe(12);
        payload.RootElement.GetProperty("expectedAccountVersion").GetInt64().ShouldBe(7);
        payload.RootElement.GetProperty("installmentIds").GetArrayLength().ShouldBe(1);
        if (regenerate)
        {
            payload.RootElement.GetProperty("expectedDocumentVersion").GetInt64().ShouldBe(9);
        }
        payload.RootElement.TryGetProperty("confirmed", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.BadRequest)]
    public void Financial_rejection_keeps_approval_explicit_and_refreshes_a_conflicting_revision(HttpStatusCode status)
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = CurrencyDto.Euro
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.History;
        Handler.SubmissionStatus = status;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#schedule-obligation"));

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Save unapproved schedule").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        page.WaitForAssertion(() => page.FindAll("[role=alert]").Count.ShouldBeGreaterThan(0));
        Handler.Payloads[0].ShouldNotContain("confirmed", StringComparison.Ordinal);
        if (status == HttpStatusCode.Conflict)
        {
            page.Markup.ShouldContain("Financial history changed", StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.BadRequest)]
    public void Instruction_rejection_retains_the_explicit_confirmation_boundary(HttpStatusCode status)
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.FinancialHistory = PaymentPageData.History;
        Handler.Instructions = "[" + PaymentPageData.Instruction + "]";
        Handler.SubmissionStatus = status;
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == "Approve summary").ShouldBeTrue());
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Approve summary").Click();
        page.Find("section[aria-labelledby='instruction-confirm'] input[type=checkbox]").Change(true);

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Confirm approve").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        page.WaitForAssertion(() => page.FindAll("[role=alert]").Count.ShouldBeGreaterThan(0));
        if (status == HttpStatusCode.Conflict)
        {
            page.Markup.ShouldContain("Instruction or source terms changed", StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Failed_payment_reads_show_accessible_errors_and_never_expose_transport_details()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = CurrencyDto.Euro,
            FinancialVersion = 4
        };
        _bookings.AddBooking(booking);
        Handler.ReadStatus = HttpStatusCode.Forbidden;

        // Act
        var accounts = Render<Accounts>();
        var finances = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        var instructions = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, booking.Id));

        // Assert
        accounts.WaitForAssertion(() => accounts.FindAll("[role=alert]").Count.ShouldBeGreaterThan(0));
        finances.WaitForAssertion(() => finances.FindAll("[role=alert]").Count.ShouldBeGreaterThan(0));
        instructions.WaitForAssertion(() => instructions.FindAll("[role=alert]").Count.ShouldBeGreaterThan(0));
        accounts.Markup.ShouldNotContain("Private bank data", StringComparison.Ordinal);
        Handler.Keys.Count.ShouldBe(0);
    }

    [Fact]
    public void Invalid_frozen_instruction_reference_does_not_submit_a_reconciliation()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = CurrencyDto.Euro
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.History;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#reconcile-instruction"));
        page.Find("#reconcile-instruction").Change("invalid-document-id");

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Save unapproved reconciliation").Click();

        // Assert
        page.Find("[role=alert]").TextContent.ShouldContain("valid instruction ID", StringComparison.Ordinal);
        Handler.Keys.Count.ShouldBe(0);
    }

    [Fact]
    public void Unknown_instruction_approval_retries_the_same_immutable_payload_and_key()
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.FinancialHistory = PaymentPageData.History;
        Handler.Instructions = "[" + PaymentPageData.Instruction + "]";
        Handler.FailNextSubmission = true;
        var page = Render<Instructions>(parameters => parameters.Add(value => value.BookingId, Guid.CreateVersion7()));
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == "Approve summary").ShouldBeTrue());
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Approve summary").Click();
        page.Find("section[aria-labelledby='instruction-confirm'] input[type=checkbox]").Change(true);
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Confirm approve").Click();
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == "Retry same submission").ShouldBeTrue());

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Retry same submission").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(2));
        Handler.Keys[0].ShouldBe(Handler.Keys[1]);
        Handler.Payloads[0].ShouldBe(Handler.Payloads[1]);
        page.WaitForAssertion(() => page.FindAll("#instruction-confirm").Count.ShouldBe(0));
        page.Markup.ShouldNotContain("Private bank data", StringComparison.Ordinal);
    }

    [Fact]
    public void Account_revision_conflicts_and_unknown_outcomes_do_not_invent_a_second_submission()
    {
        // Arrange
        Handler.Accounts = PaymentPageData.Accounts;
        Handler.FailNextSubmission = true;
        var page = Render<Accounts>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Count.ShouldBe(3));
        page.FindAll("button").First(item => item.TextContent == "Retire").Click();
        page.Find("section[aria-labelledby='account-confirm-title'] input[type=checkbox]").Change(true);
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Confirm retire").Click();
        page.WaitForAssertion(() => page.FindAll("button").Any(item => item.TextContent == "Retry same submission").ShouldBeTrue());

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Retry same submission").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(2));
        Handler.Keys[0].ShouldBe(Handler.Keys[1]);
        Handler.Payloads[0].ShouldBe(Handler.Payloads[1]);
        page.WaitForAssertion(() => page.FindAll("#account-confirm-title").Count.ShouldBe(0));
    }

    [Fact]
    public void Reconciliation_can_select_frozen_instruction_evidence_without_silently_approving_credit()
    {
        // Arrange
        var booking = BuildBookingDto() with
        {
            Currency = CurrencyDto.Euro
        };
        _bookings.AddBooking(booking);
        Handler.FinancialHistory = PaymentPageData.History;
        var page = Render<Finances>(parameters => parameters.Add(value => value.BookingId, booking.Id));
        page.WaitForAssertion(() => page.Find("#reconcile-instruction"));
        page.Find("#reconcile-instruction").Change("00000022-1111-1111-1111-111111111111");
        page.Find("#reconcile-eur").Change("50");

        // Act
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "View sensitive evidence references (audited)").Click();
        page.FindAll("button").ShouldHaveSingleItem(item => item.TextContent == "Save unapproved reconciliation").Click();

        // Assert
        page.WaitForAssertion(() => Handler.Keys.Count.ShouldBe(1));
        using var payload = JsonDocument.Parse(Handler.Payloads[0]);
        payload.RootElement.GetProperty("instructionId").GetGuid().ShouldBe(Guid.Parse("00000022-1111-1111-1111-111111111111"));
        payload.RootElement.GetProperty("euroCredit").GetDecimal().ShouldBe(50m);
        payload.RootElement.TryGetProperty("confirmed", out _).ShouldBeFalse();
    }
}
